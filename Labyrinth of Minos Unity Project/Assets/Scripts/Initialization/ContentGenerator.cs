using UnityEngine;
using Unity.Netcode;

public class ContentGenerator : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject minotaur;
    public GameObject player;
    [SerializeField] public int playerSpawnMargins = 2;

    // Generates objects in the maze based on MazeData
    public void Generate(MazeGenerator.MazeData maze)
    {
        if (!IsServer) return; // Only the server spawns objects

        if (maze == null || maze.open == null)
        {
            Debug.LogError("Maze data missing");
            return;
        }

        float s = maze.tileSize;

        SpawnMinotaur(maze, s);
        SpawnPlayer(maze, s);
    }

    private void SpawnPlayer(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int playerPos2D = GetTilePosition.WithinEdgeMargin(maze, playerSpawnMargins);
        Vector3 playerPos = new Vector3(
            playerPos2D.x * s,
            player.GetComponent<Renderer>().bounds.size.y / 2,
            playerPos2D.y * s
        );

        var playerObj = Instantiate(player, playerPos, Quaternion.identity);
        playerObj.GetComponent<NetworkObject>().Spawn();

        var playerBehav = playerObj.GetComponent<AutonomousPatrol>();
        playerBehav.Initialize(maze);
    }

    private void SpawnMinotaur(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int minotaurPos2D = GetTilePosition.ClosestToCenter(maze, s);
        Vector3 minotaurPos = new Vector3(
            minotaurPos2D.x * s,
            minotaur.GetComponent<Renderer>().bounds.size.y / 2,
            minotaurPos2D.y * s
        );

        var minotaurObj = Instantiate(minotaur, minotaurPos, Quaternion.identity);
        minotaurObj.GetComponent<NetworkObject>().Spawn();

        var minotaurBehavior = minotaurObj.GetComponent<MinotaurBehaviorController>();
        minotaurBehavior.Initialize(maze);
    }
}
