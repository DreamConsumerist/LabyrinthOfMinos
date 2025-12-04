using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerNameData : NetworkBehaviour
{
    [SerializeField] private TextMeshPro playerNameText; // Reference to the UI text showing the player's name

    // Custom struct to allow NetworkVariable of string with fixed size
    public struct NetworkString : INetworkSerializeByMemcpy
    {
        private FixedString32Bytes _info;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info); // Handles serialization for network transmission
        }

        public override string ToString() => _info.Value;

        // Implicit conversions to make assigning strings easier
        public static implicit operator string(NetworkString s) => s.ToString();
        public static implicit operator NetworkString(string s) => new NetworkString { _info = new FixedString32Bytes(s) };
    }

    // NetworkVariable to store and replicate player names
    // Readable by everyone, writable only by the server
    public NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to changes in the player's name
        playerName.OnValueChanged += OnPlayerNameChanged;

        // Immediately display the current name (useful if already set)
        playerNameText.text = playerName.Value.ToString();

        // Server sets default name once when the object spawns
        if (IsServer)
        {
            string defaultName = $"Player {OwnerClientId + 1}"; // Unique default name based on client ID
            playerName.Value = defaultName; // This automatically replicates to all clients
        }
    }

    // Called on all clients when the NetworkVariable changes
    private void OnPlayerNameChanged(NetworkString previousValue, NetworkString newValue)
    {
        playerNameText.text = newValue.ToString(); // Update the UI
    }
}
