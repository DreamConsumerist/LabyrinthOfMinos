using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : NetworkBehaviour
{
    [SerializeField] MinotaurMovement movement;
    public MazeGenerator.MazeData maze;
    private PlayerData player;

    private void Awake()
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
    }

    public void Initialize(MazeGenerator.MazeData mazeObj)
    {
        maze = mazeObj;
        movement.Initialize(this);
    }

    [System.Obsolete]
    void Start()
    {
        // Only the server controls targeting logic
        if (!IsServer) return;

        // Find the first PlayerData in the scene (you can adjust this for multi-client support)
        player = FindObjectOfType<PlayerData>();
    }

    void Update()
    {
        // Only the server drives the AI's behavior
        if (!IsServer || player == null || maze == null) return;

        var playerPos = player.transform.position;
        var target = new Vector2Int(
            Mathf.RoundToInt(playerPos.x / maze.tileSize),
            Mathf.RoundToInt(playerPos.z / maze.tileSize)
        );

        movement.UpdateTarget(target);
    }
}
