using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    public AudioMixer masterMixer;                           // Assign MasterMixer asset
    public AudioMixerGroup musicGroup;                       // Mixer group: Music
    public AudioMixerGroup uiGroup;                          // Mixer group: UI
    public AudioMixerGroup gameSfxGroup;                     // Mixer group: SFX/GameSFX (3D)
    public AudioMixerGroup ambienceGroup;                    // Mixer group: Ambience (3D, optional)

    [Header("Built-in Sources (2D)")]
    public AudioSource uiSfxSource;                          // 2D, ignoreListenerPause = true, Output = uiGroup
    public AudioSource musicA;                               // 2D, loop, Output = musicGroup
    public AudioSource musicB;                               // 2D, loop, Output = musicGroup
    public AudioSource globalSfx2D;                          // 2D, Output = gameSfxGroup (optional)

    [Header("3D Defaults (applied to spawned/attached sources)")]
    [Range(0f, 1f)] public float spatialBlend3D = 1f;         // 1 = fully 3D
    [Range(0f, 5f)] public float dopplerLevel = 0f;           // 0 = off (usually best for UI-ish/arcade SFX)
    public float minDistance = 1.5f;                         // where it starts to attenuate
    public float maxDistance = 30f;                          // inaudible beyond
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [Range(0, 360)] public int spread = 0;                    // stereo narrowing at distance (0=mono point source)
    [Range(0f, 1.1f)] public float reverbZoneMix = 1f;

    // Internals
    private AudioSource activeMusic, idleMusic;

    // Track attached loopers for clean stop/destroy
    private readonly Dictionary<Transform, AudioSource> attachedLoopers = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2D source hygiene
        if (uiSfxSource)
        {
            uiSfxSource.ignoreListenerPause = true;
            uiSfxSource.spatialBlend = 0f;
            uiSfxSource.dopplerLevel = 0f;
            if (uiGroup) uiSfxSource.outputAudioMixerGroup = uiGroup;
        }
        if (globalSfx2D)
        {
            globalSfx2D.spatialBlend = 0f;
            globalSfx2D.dopplerLevel = 0f;
            if (gameSfxGroup) globalSfx2D.outputAudioMixerGroup = gameSfxGroup;
        }
        if (musicA && musicGroup) musicA.outputAudioMixerGroup = musicGroup;
        if (musicB && musicGroup) musicB.outputAudioMixerGroup = musicGroup;

        activeMusic = musicA;
        idleMusic = musicB;
    }

    // AudioManager.cs
    public void ApplySavedVolumesOnBoot()
    {
        SetVolume("MusicVol", PlayerPrefs.GetFloat("vol_music", 0.8f));
        SetVolume("SFXVol", PlayerPrefs.GetFloat("vol_sfx", 0.8f));
        SetVolume("UIVol", PlayerPrefs.GetFloat("vol_ui", 0.8f));
    }

    void Start()
    {
        // After Awake sets up sources, apply volumes from last save:
        ApplySavedVolumesOnBoot();
    }

    // ---------------- UI & 2D ----------------
    public void PlayUI(AudioClip clip, float vol = 1f)
    { if (clip && uiSfxSource) uiSfxSource.PlayOneShot(clip, vol); }

    public void Play2D(AudioClip clip, float vol = 1f)
    { if (clip && globalSfx2D) globalSfx2D.PlayOneShot(clip, vol); }

    // ---------------- 3D HELPERS ----------------
    /// <summary>Configure an existing AudioSource on a 3D object to the global 3D defaults and route to the mixer.</summary>
    public void Setup3DSource(AudioSource src, bool routeAsAmbience = false)
    {
        if (!src) return;
        src.spatialBlend = spatialBlend3D;
        src.dopplerLevel = dopplerLevel;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = rolloffMode;
        src.spread = spread;
        src.reverbZoneMix = reverbZoneMix;
        src.bypassListenerEffects = false;
        src.bypassEffects = false;
        src.playOnAwake = false;
        src.loop = false;
        src.outputAudioMixerGroup = routeAsAmbience && ambienceGroup ? ambienceGroup : gameSfxGroup;
    }

    /// <summary>Play a one-shot 3D clip at a world position (auto-destroys).</summary>
    public void PlayAt(AudioClip clip, Vector3 pos, float vol = 1f, bool routeAsAmbience = false)
    {
        if (!clip) return;
        var go = new GameObject("SFX3D_" + clip.name);
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        Setup3DSource(src, routeAsAmbience);
        src.clip = clip;
        src.volume = vol;
        src.Play();
        Destroy(go, clip.length + 0.05f);
    }

    /// <summary>Attach (or reuse) a looping 3D SFX to a Transform (e.g., torch, minotaur breath).</summary>
    public AudioSource PlayAttached(Transform target, AudioClip loopClip, float vol = 1f, bool routeAsAmbience = false)
    {
        if (!target || !loopClip) return null;

        AudioSource src;
        if (!attachedLoopers.TryGetValue(target, out src) || src == null)
        {
            var go = new GameObject("SFX3D_Attached_" + loopClip.name);
            go.transform.SetParent(target, false);
            go.transform.localPosition = Vector3.zero;
            src = go.AddComponent<AudioSource>();
            Setup3DSource(src, routeAsAmbience);
            src.loop = true;
            attachedLoopers[target] = src;
        }

        src.outputAudioMixerGroup = routeAsAmbience && ambienceGroup ? ambienceGroup : gameSfxGroup;
        src.clip = loopClip;
        src.volume = vol;
        if (!src.isPlaying) src.Play();
        return src;
    }

    /// <summary>Stop and remove a previously attached looping SFX.</summary>
    public void StopAttached(Transform target)
    {
        if (!target) return;
        if (attachedLoopers.TryGetValue(target, out var src) && src)
        {
            src.Stop();
            if (src.gameObject) Destroy(src.gameObject);
        }
        attachedLoopers.Remove(target);
    }

    // ---------------- MUSIC (crossfade) ----------------
    public void PlayMusic(AudioClip clip, float fade = 0.8f, float targetVol = 1f)
    {
        if (!clip) return;

        var newActive = (activeMusic == musicA) ? musicB : musicA;
        var newIdle = (activeMusic == musicA) ? musicA : musicB;
        activeMusic = newActive; idleMusic = newIdle;

        if (activeMusic)
        {
            activeMusic.clip = clip;
            activeMusic.volume = 0f;
            activeMusic.loop = true;
            if (musicGroup) activeMusic.outputAudioMixerGroup = musicGroup;
            activeMusic.Play();
        }

        StopAllCoroutines();
        StartCoroutine(Crossfade(fade, targetVol));
    }

    IEnumerator Crossfade(float dur, float targetVol)
    {
        float idleStart = idleMusic ? idleMusic.volume : 0f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (activeMusic) activeMusic.volume = Mathf.Lerp(0f, targetVol, k);
            if (idleMusic) idleMusic.volume = Mathf.Lerp(idleStart, 0f, k);
            yield return null;
        }
        if (idleMusic) { idleMusic.Stop(); idleMusic.clip = null; }
    }

    // ---------------- VOLUMES / SNAPSHOTS ----------------
    /// <summary>value01: 0..1 linear slider mapped to dB (exposed param must exist in mixer).</summary>
    public void SetVolume(string exposedParam, float value01)
    {
        value01 = Mathf.Clamp01(value01);
        float dB = value01 > 0.0001f ? Mathf.Lerp(-30f, 0f, value01) : -80f; // gentle bottom
        masterMixer.SetFloat(exposedParam, dB);
    }

    public void ApplySnapshot(string name, float blend = 0.1f)
    {
        var snap = masterMixer ? masterMixer.FindSnapshot(name) : null;
        if (snap) snap.TransitionTo(blend);
    }
}
