// SettingsBootstrap.cs
using UnityEngine;

public class SettingsBootstrap : MonoBehaviour
{
    void Start()
    {
        // --- AUDIO (belt-and-suspenders; AudioManager also applies in Start) ---
        AudioManager.Instance?.ApplySavedVolumesOnBoot();

        // --- VIDEO ---
        bool fs = PlayerPrefs.GetInt("opt_fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        int vs = PlayerPrefs.GetInt("opt_vsync", 1);
        int q = PlayerPrefs.GetInt("opt_quality", QualitySettings.GetQualityLevel());

        int w = PlayerPrefs.GetInt("opt_res_w", Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt("opt_res_h", Screen.currentResolution.height);
        int hz = PlayerPrefs.GetInt("opt_res_hz", Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));

        // Apply quality & vsync first (cheap)
        QualitySettings.vSyncCount = vs > 0 ? 1 : 0;
        QualitySettings.SetQualityLevel(Mathf.Clamp(q, 0, QualitySettings.names.Length - 1), true);

        // Apply fullscreen + resolution (use FullScreenWindow for compatibility)
        var mode = fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        var freq = new RefreshRate { numerator = (uint)hz, denominator = 1 }; // or use refreshRateRatio if you prefer
        Screen.SetResolution(w, h, mode, freq);
    }
}
