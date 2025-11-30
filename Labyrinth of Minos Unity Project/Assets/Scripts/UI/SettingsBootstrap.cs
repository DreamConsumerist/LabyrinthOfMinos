// SettingsBootstrap.cs
using System.Collections.Generic;
using UnityEngine;

public class SettingsBootstrap : MonoBehaviour
{
    const string KEY_FULLSCREEN = "opt_fullscreen";
    const string KEY_VSYNC = "opt_vsync";
    const string KEY_QUALITY = "opt_quality";
    const string KEY_RES_W = "opt_res_w";
    const string KEY_RES_H = "opt_res_h";
    const string KEY_RES_HZ = "opt_res_hz";
    const string KEY_RES_INDEX = "opt_res_index";

    void Start()
    {
        // --- AUDIO ---
        AudioManager.Instance?.ApplySavedVolumesOnBoot();

        // --- VIDEO SETTINGS LOADING ---
        bool fs = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        int vs = PlayerPrefs.GetInt(KEY_VSYNC, 1);
        int q = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());

        // Load whatever is saved (or fall back to current res)
        int w = PlayerPrefs.GetInt(KEY_RES_W, Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt(KEY_RES_H, Screen.currentResolution.height);
        int hz = PlayerPrefs.GetInt(
            KEY_RES_HZ,
            Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)
        );

        // --- SANITIZE / UPGRADE RESOLUTION IF NEEDED ---
        Resolution[] allRes = Screen.resolutions ?? new Resolution[0];

        if (allRes.Length > 0)
        {
            // Build labels the same way VideoSettingsUI does
            List<string> resLabels = new List<string>();
            for (int i = 0; i < allRes.Length; i++)
            {
                string label = $"{allRes[i].width} x {allRes[i].height} @{allRes[i].refreshRateRatio.value:0}Hz";
                if (!resLabels.Contains(label))
                    resLabels.Add(label);
            }

            // Try to find the saved resolution in the list
            int savedIdx = -1;
            Resolution savedRes = allRes[0];

            for (int i = 0; i < allRes.Length; i++)
            {
                var rr = allRes[i];
                int rrHz = Mathf.RoundToInt((float)rr.refreshRateRatio.value);
                if (rr.width == w && rr.height == h && rrHz == hz)
                {
                    savedIdx = i;
                    savedRes = rr;
                    break;
                }
            }

            bool looksBad = false;

            if (savedIdx < 0)
            {
                // Saved resolution doesn't exist in the list anymore -> bad
                looksBad = true;
            }
            else
            {
                // Condition 1: it's literally the lowest index and there are better options
                if (savedIdx == 0 && allRes.Length > 1)
                    looksBad = true;

                // Condition 2: resolution is tiny while there are significantly bigger ones
                bool biggerExists = false;
                for (int i = 0; i < allRes.Length; i++)
                {
                    if (allRes[i].width >= 1280 && allRes[i].height >= 720 &&
                        (allRes[i].width > savedRes.width || allRes[i].height > savedRes.height))
                    {
                        biggerExists = true;
                        break;
                    }
                }

                if (biggerExists && (savedRes.width < 1280 || savedRes.height < 720))
                    looksBad = true;
            }

            if (looksBad)
            {
                // --- Choose a better resolution ---

                // Default: highest available
                Resolution chosen = allRes[allRes.Length - 1];

                // Prefer 1920x1080 if available (any refresh rate)
                for (int i = 0; i < allRes.Length; i++)
                {
                    if (allRes[i].width == 1920 && allRes[i].height == 1080)
                    {
                        chosen = allRes[i];
                        break;
                    }
                }

                w = chosen.width;
                h = chosen.height;
                hz = Mathf.RoundToInt((float)chosen.refreshRateRatio.value);

                // Compute the dropdown index to match VideoSettingsUI labels
                string chosenLabel = $"{chosen.width} x {chosen.height} @{chosen.refreshRateRatio.value:0}Hz";
                int idx = resLabels.IndexOf(chosenLabel);
                if (idx < 0)
                {
                    // Fallback: highest index instead of lowest
                    idx = Mathf.Max(0, resLabels.Count - 1);
                }

                // Save upgraded resolution back to PlayerPrefs
                PlayerPrefs.SetInt(KEY_RES_W, w);
                PlayerPrefs.SetInt(KEY_RES_H, h);
                PlayerPrefs.SetInt(KEY_RES_HZ, hz);
                PlayerPrefs.SetInt(KEY_RES_INDEX, idx);
                PlayerPrefs.Save();
            }
        }

        // --- APPLY QUALITY, VSYNC, RESOLUTION ---
        QualitySettings.vSyncCount = vs > 0 ? 1 : 0;
        QualitySettings.SetQualityLevel(
            Mathf.Clamp(q, 0, QualitySettings.names.Length - 1),
            true
        );

        var mode = fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        var freq = new RefreshRate { numerator = (uint)hz, denominator = 1 };
        Screen.SetResolution(w, h, mode, freq);
    }
}
