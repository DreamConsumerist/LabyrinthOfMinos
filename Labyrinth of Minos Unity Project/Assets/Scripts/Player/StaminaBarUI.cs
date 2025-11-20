using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class StaminaBarUI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] StaminaSystem stamina;   // Player's stamina
    [SerializeField] Canvas staminaCanvas;    // The HUD canvas (from the prefab)
    [SerializeField] Image fillImage;         // The fill image (StaminaBarFill)

    [Header("Colors")]
    [SerializeField] Color normalColor = Color.green;
    [SerializeField] Color lowColor = Color.yellow;

    void Awake()
    {
        // If not assigned in Inspector, try to auto-grab from the same GameObject
        if (stamina == null)
            stamina = GetComponent<StaminaSystem>();
    }

    public override void OnNetworkSpawn()
    {
        // Only show the HUD for the local player
        if (!IsOwner)
        {
            if (staminaCanvas != null) staminaCanvas.enabled = false;
            enabled = false;   // no need to keep updating
            return;
        }

        // Make sure our canvas is enabled for the owner
        if (staminaCanvas != null)
            staminaCanvas.enabled = true;
    }

    void Update()
    {
        if (stamina == null || fillImage == null) return;

        // GetStamina() already returns 0–1 in your system
        float value = stamina.GetStamina();
        fillImage.fillAmount = value;

        // Change color when below the sprint threshold
        if (stamina.CurrentStaminaValue < stamina.MinSprintStaminaValue)
        {
            fillImage.color = lowColor;
        }
        else
        {
            fillImage.color = normalColor;
        }
    }
}
