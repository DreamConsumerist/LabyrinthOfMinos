using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets; // FirstPersonController

public class GameplaySettingsUI : MonoBehaviour
{
    [Header("Mouse Look")]
    public Slider mouseSensitivity;        
    public Toggle invertY;

    [Header("Field of View")]
    public Slider fovSlider;               
    public TMP_Text fovValueText;

    [Header("Value label (optional)")]
    public TMP_Text sensitivityValueText;

    private const string KEY_SENS = "gp_sens";
    private const string KEY_INVY = "gp_invy";
    private const string KEY_FOV = "gp_fov";

    float savedSens;
    bool savedInvY;
    float savedFov;

    [SerializeField] private float defaultFov = 80f; // match PlayerCameraController.defaultFov
    [SerializeField] private float minFov = 60f;
    [SerializeField] private float maxFov = 110f;

    void Awake()
    {
        // Hook live label updates
        if (mouseSensitivity)
        {
            mouseSensitivity.onValueChanged.AddListener(OnSensitivityUIChanged);
        }
        else
        {
            Debug.LogWarning("GameplaySettingsUI: mouseSensitivity slider is not assigned.");
        }

        if (fovSlider)
        {
            fovSlider.onValueChanged.AddListener(OnFovUIChanged);
        }
        else
        {
            Debug.LogWarning("GameplaySettingsUI: fovSlider is not assigned.");
        }
    }

    public void LoadSavedIntoUI()
    {
        // Load saved values (or defaults)
        savedSens = PlayerPrefs.GetFloat(KEY_SENS, 1.0f);
        savedInvY = PlayerPrefs.GetInt(KEY_INVY, 0) == 1;
        savedFov = PlayerPrefs.GetFloat(KEY_FOV, defaultFov);

        // Clamp FOV and configure slider range
        savedFov = Mathf.Clamp(savedFov, minFov, maxFov);

        if (fovSlider)
        {
            fovSlider.minValue = minFov;
            fovSlider.maxValue = maxFov;
            fovSlider.SetValueWithoutNotify(savedFov);
        }

        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);

        // Update labels
        OnSensitivityUIChanged(savedSens);
        OnFovUIChanged(savedFov);
    }

    public void RevertUIToSaved()
    {
        if (mouseSensitivity) mouseSensitivity.SetValueWithoutNotify(savedSens);
        if (invertY) invertY.SetIsOnWithoutNotify(savedInvY);
        if (fovSlider) fovSlider.SetValueWithoutNotify(savedFov);

        OnSensitivityUIChanged(savedSens);
        OnFovUIChanged(savedFov);
    }

    public void ApplyAndSave()
    {
        float sens = mouseSensitivity ? mouseSensitivity.value : savedSens;
        bool inv = invertY ? invertY.isOn : savedInvY;
        float fov = fovSlider ? fovSlider.value : savedFov;

        fov = Mathf.Clamp(fov, minFov, maxFov);

        // Push settings live to the player controller
        var fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc)
        {
            fpc.SetMouseSensitivity(sens);
            fpc.SetInvertY(inv);
        }

        // Apply FOV immediately to the main camera (for visual feedback)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.fieldOfView = fov;
        }

        // Save all preferences
        PlayerPrefs.SetFloat(KEY_SENS, sens);
        PlayerPrefs.SetInt(KEY_INVY, inv ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_FOV, fov);
        PlayerPrefs.Save();

        // Update snapshot
        savedSens = sens;
        savedInvY = inv;
        savedFov = fov;
    }

    // --- live label updates ---

    public void OnSensitivityUIChanged(float v)
    {
        if (!sensitivityValueText) return;
        sensitivityValueText.text = v.ToString("0.00");
    }

    public void OnFovUIChanged(float v)
    {
        if (!fovValueText) return;
        fovValueText.text = Mathf.RoundToInt(v).ToString();
    }
}
