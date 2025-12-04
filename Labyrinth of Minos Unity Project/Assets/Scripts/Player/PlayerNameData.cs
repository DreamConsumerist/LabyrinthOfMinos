using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerNameData : NetworkBehaviour
{
    [SerializeField] private TextMeshPro playerNameText;

    public struct NetworkString : INetworkSerializeByMemcpy
    {
        private FixedString32Bytes _info;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public override string ToString() => _info.Value;

        public static implicit operator string(NetworkString s) => s.ToString();
        public static implicit operator NetworkString(string s) => new NetworkString { _info = new FixedString32Bytes(s) };
    }

    public NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>(default, NetworkVariableReadPermission.Everyone);

    public override void OnNetworkSpawn()
    {
    base.OnNetworkSpawn();

    // Only set default name on the owner
    if (IsOwner)
    {
        string defaultName = IsServer ? "Player 1" : $"Player {OwnerClientId + 1}";
        playerName.Value = defaultName;
    }

    // Subscribe everyone to updates
    playerName.OnValueChanged += OnPlayerNameChanged;

    // Immediately apply current value
    playerNameText.text = playerName.Value.ToString();
    }


    private void OnPlayerNameChanged(NetworkString previousValue, NetworkString newValue)
    {
        playerNameText.text = newValue;
    }
}
