using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    [Tooltip("Highest legitimate speed (m/s) this object should ever move at, e.g. sprint speed plus a small margin for network jitter. PlayerCapsule_Networked's FirstPersonController.SprintSpeed is 4.01 — tune this alongside it.")]
    [SerializeField] private float maxAllowedSpeed = 6f;

    private Vector3 _lastValidatedPosition;
    private double _lastValidatedTime;
    private bool _hasBaseline;

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);

        if (!IsServer) return;

        if (newState.IsTeleportingNextFrame)
        {
            _hasBaseline = false;
            return;
        }

        Vector3 newPosition = newState.GetPosition();
        double now = NetworkManager.ServerTime.Time;

        if (_hasBaseline)
        {
            double elapsed = now - _lastValidatedTime;
            if (elapsed > 0.001)
            {
                float impliedSpeed = Vector3.Distance(_lastValidatedPosition, newPosition) / (float)elapsed;
                if (impliedSpeed > maxAllowedSpeed)
                {
                    Debug.LogWarning($"[ClientNetworkTransform] {name} moved at {impliedSpeed:F1} m/s (cap {maxAllowedSpeed}) — possible speed hack.");
                }
            }
        }

        _lastValidatedPosition = newPosition;
        _lastValidatedTime = now;
        _hasBaseline = true;
    }
}
