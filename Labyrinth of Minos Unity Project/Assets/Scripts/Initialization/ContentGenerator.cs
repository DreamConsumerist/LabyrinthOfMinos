using System;
using UnityEngine;
using System.Collections.Generic;

public class ContentGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject minotaur;
    public GameObject player;
    [SerializeField] public int playerSpawnMargins = 2;

    [Header("Keys & Exit (spawn on any open tile)")]
    public GameObject key1Prefab;
    public GameObject key2Prefab;
    public GameObject key3Prefab;
    public GameObject exitPrefab;

    // Generates objects in the maze based on MazeData
    public void Generate(MazeGenerator.MazeData maze)
    {
        if (maze == null || maze.open == null) { Debug.LogError("Maze data missing"); return; }

        int H = maze.tilesH, W = maze.tilesW;
        float s = maze.tileSize;

        // Spawn minotaur and player first (unchanged)
        Vector2Int minotaurPos2D = MinotaurGen(maze, s);
        Vector2Int playerPos2D = PlayerGen(maze, s);

        // Spawn keys and exit on any other open nodes
        KeysAndExitGen(maze, s, minotaurPos2D, playerPos2D);
    }

    private Vector2Int PlayerGen(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int playerPos2D = GetTilePosition.WithinEdgeMargin(maze, playerSpawnMargins);

        Vector3 playerPos = new Vector3(
            playerPos2D.x * s,
            GetHalfHeight(player),
            playerPos2D.y * s
        );

        var playerObj = Instantiate(player, playerPos, Quaternion.identity, transform);
        var playerBehav = playerObj.GetComponent<AutonomousPatrol>();
        if (playerBehav) playerBehav.Initialize(maze);

        return playerPos2D;
    }

    private Vector2Int MinotaurGen(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int minotaurPos2D = GetTilePosition.ClosestToCenter(maze, s);

        Vector3 minotaurPos = new Vector3(
            minotaurPos2D.x * s,
            0f,
            minotaurPos2D.y * s
        );

        var minotaurObj = Instantiate(minotaur, minotaurPos, Quaternion.identity, transform);
        var minotaurBehavior = minotaurObj.GetComponent<MinotaurBehaviorController>();
        if (minotaurBehavior) minotaurBehavior.Initialize(maze);

        return minotaurPos2D;
    }

    private void KeysAndExitGen(MazeGenerator.MazeData maze, float s, Vector2Int minotaurTile, Vector2Int playerTile)
    {
        // Collect all open tiles
        var openTiles = CollectOpenTiles(maze.open);

        // Avoid using the same tile as player/minotaur
        var used = new HashSet<Vector2Int> { minotaurTile, playerTile };

        // Pick 4 distinct open tiles for Key_1, Key_2, Key_3, Exit
        var picks = PickDistinctTiles(openTiles, used, 4);

        if (picks.Count == 0)
        {
            Debug.LogWarning("No open tiles available for keys/exit.");
            return;
        }

        // Spawn Key_1
        if (key1Prefab && picks.Count >= 1)
        {
            SpawnAtTile(key1Prefab, picks[0], maze, s, "Key_1");
            used.Add(picks[0]);
        }
        // Spawn Key_2
        if (key2Prefab && picks.Count >= 2)
        {
            SpawnAtTile(key2Prefab, picks[1], maze, s, "Key_2");
            used.Add(picks[1]);
        }
        // Spawn Key_3
        if (key3Prefab && picks.Count >= 3)
        {
            SpawnAtTile(key3Prefab, picks[2], maze, s, "Key_3");
            used.Add(picks[2]);
        }
        // Spawn Exit
        if (exitPrefab && picks.Count >= 4)
        {
            SpawnAtTile(exitPrefab, picks[3], maze, s, "Exit");
        }
    }

    // ---- Helpers ----

    private static float GetHalfHeight(GameObject prefab)
    {
        if (!prefab) return 0f;
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.size.y / 2f;
        var col = prefab.GetComponentInChildren<Collider>();
        if (col) return col.bounds.extents.y;
        return 0f;
    }

    // Get world-space bounds of the instantiated object (all children).
    private static bool TryGetWorldBounds(GameObject go, out Bounds b)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return true;
        }

        var cols = go.GetComponentsInChildren<Collider>(true);
        if (cols.Length > 0)
        {
            b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return true;
        }

        b = new Bounds(go.transform.position, Vector3.zero);
        return false;
    }

    private void SpawnAtTile(GameObject prefab, Vector2Int tile, MazeGenerator.MazeData maze, float s, string nameOverride = null)
    {
        // 1) Target tile center on XZ (floor at y=0 in your scene).
        Vector3 tileCenter = new Vector3(tile.x * s, 0f, tile.y * s);

        // 2) Instantiate roughly there first.
        var go = Instantiate(prefab, tileCenter, Quaternion.identity, transform);
        if (!string.IsNullOrEmpty(nameOverride)) go.name = nameOverride;

        // 3) Re-center to the tile using the instance's world bounds.
        if (TryGetWorldBounds(go, out var b))
        {
            // Shift horizontally so bounds center sits exactly on the tile center.
            Vector3 deltaXZ = new Vector3(tileCenter.x - b.center.x, 0f, tileCenter.z - b.center.z);

            // Lift vertically so the bottom of the object is on the floor, plus a hair.
            float liftY = (0f - b.min.y) + 0.2f;

            go.transform.position += deltaXZ + new Vector3(0f, liftY, 0f);
        }
        else
        {
            // No bounds found; give a small lift anyway
            go.transform.position += new Vector3(0f, 0.02f, 0f);
        }

        // Optional: make sure physics won't kick it around on spawn
        var rb = go.GetComponentInChildren<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }
    }


    private static List<Vector2Int> CollectOpenTiles(bool[,] open)
    {
        int H = open.GetLength(0), W = open.GetLength(1);
        var list = new List<Vector2Int>(H * W / 2);
        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                if (open[r, c]) list.Add(new Vector2Int(c, r)); // x=c, y=r
            }
        }
        return list;
    }

    private static List<Vector2Int> PickDistinctTiles(List<Vector2Int> pool, HashSet<Vector2Int> avoid, int count)
    {
        // Make a filtered, randomly-ordered pool
        var filtered = new List<Vector2Int>(pool.Count);
        foreach (var t in pool)
            if (!avoid.Contains(t)) filtered.Add(t);

        // Fisher–Yates shuffle
        for (int i = filtered.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (filtered[i], filtered[j]) = (filtered[j], filtered[i]);
        }

        if (filtered.Count <= count) return filtered;

        return filtered.GetRange(0, count);
    }
}
