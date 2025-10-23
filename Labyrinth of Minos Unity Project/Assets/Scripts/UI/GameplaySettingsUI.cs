using UnityEngine;
using UnityEngine.UI;
using TMPro;  // <-- add this

public class GameplaySettingsUI : MonoBehaviour
{
    public Slider mouseSensitivity;        // expected range e.g., 0.10 .. 5.00
    public Toggle invertY;

    [Header("Value label (optional)")]
    public TMP_Text sensitivityValueText;  // <-- assign in Inspector

    const string KEY_SENS = "gp_sens";
    const string KEY_INVY = "gp_invy";

    float savedSens;
    bool savedInvY;

    void Awake()
    {
        // Ensure the label updates while you drag (no need to wire this in Inspector)
        if (mouseSensitivity)
            mouseSensitivity.onValueChanged.AddListener(OnSensitivityUIChanged);
    }

    public void LoadSavedIntoUI()
    {
        savedSens = PlayerPrefs.GetFloat(KEY_SENS, 1.0f);
        savedInvY = PlayerPrefs.GetInt(KEY_INVY, 0) == 1;

        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);

        // Because SetValueWithoutNotify suppresses events, update the label manually:
        OnSensitivityUIChanged(savedSens);
    }

    public void RevertUIToSaved()
    {
        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);

        // Refresh label to the reverted value
        OnSensitivityUIChanged(savedSens);
    }

    public void ApplyAndSave()
    {
        float sens = mouseSensitivity ? mouseSensitivity.value : savedSens;
        bool inv = invertY ? invertY.isOn : savedInvY;

        // (Optional) push to your player/controller here
        // var pc = FindFirstObjectByType<PlayerLookController>();
        // if (pc) { pc.SetMouseSensitivity(sens); pc.SetInvertY(inv); }

        PlayerPrefs.SetFloat(KEY_SENS, sens);
        PlayerPrefs.SetInt(KEY_INVY, inv ? 1 : 0);
        PlayerPrefs.Save();

        savedSens = sens;
        savedInvY = inv;
    }

    // --- live label update ---
    public void OnSensitivityUIChanged(float v)
    {
        if (!sensitivityValueText) return;

        // Choose your preferred formatting:
        // Option A: show as a plain number
        sensitivityValueText.text = v.ToString("0.00");

        // Option B: map to a percent-like display (uncomment if you prefer)
        // float pct = Mathf.InverseLerp(mouseSensitivity.minValue, mouseSensitivity.maxValue, v);
        // sensitivityValueText.text = Mathf.RoundToInt(pct * 100f) + "%";
    }
}
