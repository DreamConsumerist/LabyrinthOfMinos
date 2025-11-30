// VideoSettingsUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VideoSettingsUI : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Toggle vsyncToggle;
    public TMP_Dropdown qualityDropdown;

    Resolution[] resolutions;
    List<string> resLabels = new();

    // Saved snapshot
    bool savedFullscreen;
    int savedResIndex;
    bool savedVSync;
    int savedQuality;

    const string KEY_FULLSCREEN = "opt_fullscreen";
    const string KEY_RES_INDEX = "opt_res_index";
    const string KEY_VSYNC = "opt_vsync";
    const string KEY_QUALITY = "opt_quality";
    const string KEY_RES_W = "opt_res_w";
    const string KEY_RES_H = "opt_res_h";
    const string KEY_RES_HZ = "opt_res_hz"; // integer Hz is fine

    void Awake()
    {
        // Build resolution list once
        resolutions = Screen.resolutions;
        resLabels.Clear();
        var ops = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            string label = $"{resolutions[i].width} x {resolutions[i].height} @{resolutions[i].refreshRateRatio.value:0}Hz";
            if (!resLabels.Contains(label))
            {
                resLabels.Add(label);
                ops.Add(new TMP_Dropdown.OptionData(label));
            }
        }
        if (resolutionDropdown) { resolutionDropdown.options = ops; }
    }

    public void LoadSavedIntoUI()
    {
        // Load saved
        savedFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        // Default to a *smart* current/1080p/highest resolution index instead of 0
        savedResIndex = PlayerPrefs.GetInt(KEY_RES_INDEX, GetCurrentResLabelIndex());
        savedVSync = PlayerPrefs.GetInt(KEY_VSYNC, QualitySettings.vSyncCount) > 0;
        savedQuality = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());

        // Clamp
        savedResIndex = Mathf.Clamp(savedResIndex, 0, Mathf.Max(0, resLabels.Count - 1));
        savedQuality = Mathf.Clamp(savedQuality, 0, QualitySettings.names.Length - 1);

        // Push into UI (without firing events)
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        if (resolutionDropdown)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResIndex);
            resolutionDropdown.RefreshShownValue();
        }
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(savedVSync);
        if (qualityDropdown)
        {
            qualityDropdown.SetValueWithoutNotify(savedQuality);
            qualityDropdown.RefreshShownValue();
        }
    }

    public void RevertUIToSaved()
    {
        // Just restore UI widgets to snapshot
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        if (resolutionDropdown)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResIndex);
            resolutionDropdown.RefreshShownValue();
        }
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(savedVSync);
        if (qualityDropdown)
        {
            qualityDropdown.SetValueWithoutNotify(savedQuality);
            qualityDropdown.RefreshShownValue();
        }
    }

    public void ApplyAndSave()
    {
        bool fs = fullscreenToggle ? fullscreenToggle.isOn : savedFullscreen;
        int resIdx = resolutionDropdown ? resolutionDropdown.value : savedResIndex;
        bool vs = vsyncToggle ? vsyncToggle.isOn : savedVSync;
        int q = qualityDropdown ? qualityDropdown.value : savedQuality;

        resIdx = Mathf.Clamp(resIdx, 0, Mathf.Max(0, resLabels.Count - 1));

        // Map dropdown index -> a Resolution (by matching label)
        var r = Screen.currentResolution;
        if (resolutionDropdown && resLabels.Count > 0)
        {
            string chosen = resolutionDropdown.options[resIdx].text;
            int realIdx = FindResolutionIndexByLabel(chosen);
            if (realIdx >= 0) r = resolutions[realIdx];
        }

        // Apply
        Screen.fullScreen = fs;
        Screen.SetResolution(
            r.width, r.height,
            fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed,
            r.refreshRateRatio
        );
        QualitySettings.vSyncCount = vs ? 1 : 0;
        QualitySettings.SetQualityLevel(Mathf.Clamp(q, 0, QualitySettings.names.Length - 1), true);

        // Save
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fs ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RES_INDEX, resIdx);
        PlayerPrefs.SetInt(KEY_VSYNC, vs ? 1 : 0);
        PlayerPrefs.SetInt(KEY_QUALITY, q);

        // Also save the actual resolution we applied
        PlayerPrefs.SetInt(KEY_RES_W, r.width);
        PlayerPrefs.SetInt(KEY_RES_H, r.height);
        PlayerPrefs.SetInt(KEY_RES_HZ, Mathf.RoundToInt((float)r.refreshRateRatio.value));

        PlayerPrefs.Save();

        // Update snapshot
        savedFullscreen = fs;
        savedResIndex = resIdx;
        savedVSync = vs;
        savedQuality = q;
    }

    void OnEnable()
    {
        // Ensure the dropdown reflects saved settings whenever the menu is shown
        LoadSavedIntoUI();
    }
    int GetCurrentResLabelIndex()
    {
        if (resLabels.Count == 0)
            return 0;

        // 0) Prefer the resolution we actually saved in PlayerPrefs
        if (PlayerPrefs.HasKey(KEY_RES_W) && PlayerPrefs.HasKey(KEY_RES_H))
        {
            int sw = PlayerPrefs.GetInt(KEY_RES_W);
            int sh = PlayerPrefs.GetInt(KEY_RES_H);
            int shz = PlayerPrefs.GetInt(
                KEY_RES_HZ,
                Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)
            );

            // Try exact match (including Hz)
            string savedLabel = $"{sw} x {sh} @{shz:0}Hz";
            int idx = resLabels.IndexOf(savedLabel);
            if (idx >= 0)
                return idx;

            // If Hz doesn't match exactly, allow width/height only
            for (int i = 0; i < resolutions.Length; i++)
            {
                var rr = resolutions[i];
                if (rr.width == sw && rr.height == sh)
                {
                    string lbl = $"{rr.width} x {rr.height} @{rr.refreshRateRatio.value:0}Hz";
                    int idx2 = resLabels.IndexOf(lbl);
                    if (idx2 >= 0)
                        return idx2;
                }
            }
        }

        // 1) Try the current resolution as a backup
        string curLabel = $"{Screen.currentResolution.width} x {Screen.currentResolution.height} @{Screen.currentResolution.refreshRateRatio.value:0}Hz";
        int curIdx = resLabels.IndexOf(curLabel);
        if (curIdx >= 0)
            return curIdx;

        // 2) Try matching by width/height only
        for (int i = 0; i < resolutions.Length; i++)
        {
            var rr = resolutions[i];
            if (rr.width == Screen.currentResolution.width &&
                rr.height == Screen.currentResolution.height)
            {
                string lbl = $"{rr.width} x {rr.height} @{rr.refreshRateRatio.value:0}Hz";
                int idx = resLabels.IndexOf(lbl);
                if (idx >= 0)
                    return idx;
            }
        }

        // 3) Prefer 1920x1080 as generic backup
        for (int i = 0; i < resolutions.Length; i++)
        {
            var rr = resolutions[i];
            if (rr.width == 1920 && rr.height == 1080)
            {
                string lbl = $"{rr.width} x {rr.height} @{rr.refreshRateRatio.value:0}Hz";
                int idx = resLabels.IndexOf(lbl);
                if (idx >= 0)
                    return idx;
            }
        }

        // 4) Final fallback: highest resolution (last in list), not lowest
        return Mathf.Max(0, resLabels.Count - 1);
    }

    int FindResolutionIndexByLabel(string label)
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            var rr = resolutions[i];
            string l = $"{rr.width} x {rr.height} @{rr.refreshRateRatio.value:0}Hz";
            if (l == label) return i;
        }
        return -1;
    }

    // Hook these if you want to cache pending values in PlayerPrefs BEFORE Apply (not necessary).
    public void OnFullscreen(bool on) { /* no-op until Apply */ }
    public void OnResolution(int idx) { /* no-op until Apply */ }
    public void OnVsync(bool on) { /* no-op until Apply */ }
    public void OnQuality(int idx) { /* no-op until Apply */ }
}
