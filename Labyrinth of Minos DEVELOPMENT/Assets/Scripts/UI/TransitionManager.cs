using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup fadeGroup;   
    [SerializeField] private float defaultClickDelay = 0.12f;
    [SerializeField] private float defaultFadeOut = 0.25f;
    [SerializeField] private float defaultFadeIn = 0.25f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup != null)
        {
            
            fadeGroup.alpha = Mathf.Clamp01(fadeGroup.alpha);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable = false;
            StartCoroutine(FadeInAtBoot());
        }
    }

    IEnumerator FadeInAtBoot()
    {
        // Small delay so the first frame draws, then fade in
        yield return null;
        yield return FadeTo(0f, defaultFadeIn);
        fadeGroup.blocksRaycasts = false;
    }

    public void Go(string sceneName, AudioClip uiClick = null, float? clickDelay = null, float? fadeOut = null, float? fadeIn = null)
    {
        StartCoroutine(TransitionSequence(sceneName,
            uiClick,
            clickDelay ?? defaultClickDelay,
            fadeOut ?? defaultFadeOut,
            fadeIn ?? defaultFadeIn));
    }

    private IEnumerator TransitionSequence(string sceneName, AudioClip uiClick, float clickDelay, float fadeOut, float fadeIn)
    {
        // play click (from persistent AudioManager so it won’t cut off)
        if (uiClick) AudioManager.Instance?.PlayUI(uiClick);

        // tiny delay so the click transient is heard
        if (clickDelay > 0f)
            yield return new WaitForSecondsRealtime(clickDelay);

        // block input & fade to black
        if (fadeGroup)
        {
            fadeGroup.blocksRaycasts = true;
            yield return FadeTo(1f, fadeOut);
        }

        // safety: ensure unpaused before swapping scenes
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // async load next scene
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = true;      
        while (!op.isDone) yield return null;

        // allow one frame for the new scene to render
        yield return null;

        // fade back in & unblock input
        if (fadeGroup)
        {
            yield return FadeTo(0f, fadeIn);
            fadeGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        if (fadeGroup == null || duration <= 0f)
        {
            if (fadeGroup) fadeGroup.alpha = target;
            yield break;
        }

        float start = fadeGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ignore timescale when pausing
            fadeGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        fadeGroup.alpha = target;
    }
}
