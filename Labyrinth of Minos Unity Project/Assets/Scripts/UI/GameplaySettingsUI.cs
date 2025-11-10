using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets; // <-- add this so it can see FirstPersonController

public class GameplaySettingsUI : MonoBehaviour
{
    public Slider mouseSensitivity;        // expected range e.g., 0.10 .. 5.00
    public Toggle invertY;

    [Header("Value label (optional)")]
    public TMP_Text sensitivityValueText;

    const string KEY_SENS = "gp_sens";
    const string KEY_INVY = "gp_invy";

    float savedSens;
    bool savedInvY;

    void Awake()
    {
        if (mouseSensitivity)
            mouseSensitivity.onValueChanged.AddListener(OnSensitivityUIChanged);
    }

    public void LoadSavedIntoUI()
    {
        savedSens = PlayerPrefs.GetFloat(KEY_SENS, 1.0f);
        savedInvY = PlayerPrefs.GetInt(KEY_INVY, 0) == 1;

        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);

        OnSensitivityUIChanged(savedSens);
    }

    public void RevertUIToSaved()
    {
        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);

        OnSensitivityUIChanged(savedSens);
    }

    public void ApplyAndSave()
    {
        float sens = mouseSensitivity ? mouseSensitivity.value : savedSens;
        bool inv = invertY ? invertY.isOn : savedInvY;

        //  Push settings live to the player controller
        var fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc)
        {
            fpc.SetMouseSensitivity(sens);
            fpc.SetInvertY(inv);
        }

        // Save the preferences for next launch
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

        // Display as plain number
        sensitivityValueText.text = v.ToString("0.00");
    }
}
