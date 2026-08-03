using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    public Slider music;
    public Slider sfx;
    public Slider ui;

    [Header("Value labels (optional)")]
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    public TMP_Text uiValueText;

    const string KEY_MUSIC = "vol_music";
    const string KEY_SFX = "vol_sfx";
    const string KEY_UI = "vol_ui";

    float savedMusic, savedSfx, savedUi;

    void Awake()
    {
        // Auto-wire listeners in case you forget to hook them in the Inspector
        if (music) music.onValueChanged.AddListener(OnMusicUIChanged);
        if (sfx) sfx.onValueChanged.AddListener(OnSfxUIChanged);
        if (ui) ui.onValueChanged.AddListener(OnUiUIChanged);
    }

    void OnEnable()
    {
        // Keep labels synced if panel is re-enabled
        UpdateAllLabels();
    }

    public void LoadSavedIntoUI()
    {
        savedMusic = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
        savedSfx = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);
        savedUi = PlayerPrefs.GetFloat(KEY_UI, 0.8f);

        if (music) music.SetValueWithoutNotify(savedMusic);
        if (sfx) sfx.SetValueWithoutNotify(savedSfx);
        if (ui) ui.SetValueWithoutNotify(savedUi);

        // Because SetValueWithoutNotify suppresses events, update labels manually:
        UpdateAllLabels();
    }

    public void RevertUIToSaved()
    {
        if (music) music.SetValueWithoutNotify(savedMusic);
        if (sfx) sfx.SetValueWithoutNotify(savedSfx);
        if (ui) ui.SetValueWithoutNotify(savedUi);

        UpdateAllLabels();
    }

    // Called by Apply button via SettingsMenu
    public void ApplyAndSave()
    {
        float m = music ? music.value : savedMusic;
        float s = sfx ? sfx.value : savedSfx;
        float u = ui ? ui.value : savedUi;

        AudioManager.Instance?.SetVolume("MusicVol", m);
        AudioManager.Instance?.SetVolume("SFXVol", s);
        AudioManager.Instance?.SetVolume("UIVol", u);

        PlayerPrefs.SetFloat(KEY_MUSIC, m);
        PlayerPrefs.SetFloat(KEY_SFX, s);
        PlayerPrefs.SetFloat(KEY_UI, u);
        PlayerPrefs.Save();

        savedMusic = m; savedSfx = s; savedUi = u;
    }

    // ----- UI change handlers (live label updates only) -----
    public void OnMusicUIChanged(float v) { UpdateLabel(musicValueText, v); }
    public void OnSfxUIChanged(float v) { UpdateLabel(sfxValueText, v); }
    public void OnUiUIChanged(float v) { UpdateLabel(uiValueText, v); }

    // ----- helpers -----
    void UpdateAllLabels()
    {
        if (music) UpdateLabel(musicValueText, music.value);
        if (sfx) UpdateLabel(sfxValueText, sfx.value);
        if (ui) UpdateLabel(uiValueText, ui.value);
    }

    void UpdateLabel(TMP_Text label, float v)
    {
        if (!label) return;
        label.text = $"{Mathf.RoundToInt(v * 100f)}%";
    }
}
