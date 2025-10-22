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
        savedResIndex = PlayerPrefs.GetInt(KEY_RES_INDEX, GetCurrentResLabelIndex());
        savedVSync = PlayerPrefs.GetInt(KEY_VSYNC, QualitySettings.vSyncCount) > 0;
        savedQuality = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());

        // Clamp
        savedResIndex = Mathf.Clamp(savedResIndex, 0, Mathf.Max(0, resLabels.Count - 1));
        savedQuality = Mathf.Clamp(savedQuality, 0, QualitySettings.names.Length - 1);

        // Push into UI (without firing events)
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        if (resolutionDropdown) { resolutionDropdown.SetValueWithoutNotify(savedResIndex); resolutionDropdown.RefreshShownValue(); }
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(savedVSync);
        if (qualityDropdown) { qualityDropdown.SetValueWithoutNotify(savedQuality); qualityDropdown.RefreshShownValue(); }
    }

    public void RevertUIToSaved()
    {
        // Just restore UI widgets to snapshot
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        if (resolutionDropdown) { resolutionDropdown.SetValueWithoutNotify(savedResIndex); resolutionDropdown.RefreshShownValue(); }
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(savedVSync);
        if (qualityDropdown) { qualityDropdown.SetValueWithoutNotify(savedQuality); qualityDropdown.RefreshShownValue(); }
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
        PlayerPrefs.Save();

        // Update snapshot
        savedFullscreen = fs;
        savedResIndex = resIdx;
        savedVSync = vs;
        savedQuality = q;

        PlayerPrefs.SetInt(KEY_RES_W, r.width);
        PlayerPrefs.SetInt(KEY_RES_H, r.height);
        PlayerPrefs.SetInt(KEY_RES_HZ, Mathf.RoundToInt((float)r.refreshRateRatio.value));

    }

    int GetCurrentResLabelIndex()
    {
        string cur = $"{Screen.currentResolution.width} x {Screen.currentResolution.height} @{Screen.currentResolution.refreshRateRatio.value:0}Hz";
        return Mathf.Max(0, resLabels.IndexOf(cur));
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
