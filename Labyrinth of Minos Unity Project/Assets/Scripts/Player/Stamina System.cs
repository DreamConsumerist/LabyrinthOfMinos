using UnityEngine;
using Unity.Netcode;

public class StaminaSystem : NetworkBehaviour
{
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float drainRate = 15f;
    [SerializeField] float regenRate = 10f;
    float currentStamina;

    void Start() => currentStamina = maxStamina;

    void Update()
    {
        if (!IsOwner) return;
        bool sprinting = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

        if (sprinting) currentStamina -= drainRate * Time.deltaTime;
        else currentStamina += regenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public bool CanSprint() => currentStamina > 0;
    public float GetStamina() => currentStamina / maxStamina;
}
