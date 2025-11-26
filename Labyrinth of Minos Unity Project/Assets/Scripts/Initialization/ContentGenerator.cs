using System;
using UnityEngine;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;

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

    [Header("Key/Exit Placement Preferences")]
    [Tooltip("Require keys to try different quadrants first; will fallback if not possible.")]
    public bool preferSeparateQuadrantsForKeys = true;

    [Tooltip("Minimum tile distance used when falling back (keys should be this far from exit/other keys).")]
    [Min(0f)] public float minDistanceTilesFallback = 6f;

    // Cached maze data so other systems (like player spawner) can query tiles
    private MazeGenerator.MazeData _currentMaze;
    private float _currentTileSize;

    // Let other scripts know when maze data is ready
    public bool HasMazeData => _currentMaze != null && _currentMaze.open != null;

    // Generates objects in the maze based on MazeData
    public void Generate(MazeGenerator.MazeData maze)
    {
        if (maze == null || maze.open == null) { Debug.LogError("Maze data missing"); return; }

        // Cache for external consumers (like player spawner)
        _currentMaze = maze;
        _currentTileSize = maze.tileSize;

        int H = maze.tilesH, W = maze.tilesW;
        float s = maze.tileSize;

        // Spawn minotaur and player first (unchanged)
        Vector2Int minotaurPos2D = GetTilePosition.ClosestToCenter(maze, s);
        //Vector2Int playerPos2D = PlayerGen(maze, s);
        Vector2Int playerPos2D = default;

        // Spawn keys and exit with new weighted logic
        KeysAndExitGen(maze, s, minotaurPos2D, playerPos2D);

        StartCoroutine(SpawnMinotaurWhenPlayerExists(maze, s, minotaurPos2D));
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

    private System.Collections.IEnumerator SpawnMinotaurWhenPlayerExists(
    MazeGenerator.MazeData maze,
    float s,
    Vector2Int minotaurPos2D)
    {
        // Wait until the real networked player exists
        while (FindAnyObjectByType<FirstPersonController>() == null)
        {
            yield return null;
        }

        if (maze == null || maze.open == null)
        {
            Debug.LogError("[ContentGenerator] Maze is null in SpawnMinotaurWhenPlayerExists, aborting minotaur spawn.");
            yield break;
        }

        Vector3 minotaurPos = new Vector3(
            minotaurPos2D.x * s,
            0f,
            minotaurPos2D.y * s
        );

        var minotaurObj = Instantiate(minotaur, minotaurPos, Quaternion.identity, transform);

        var minoBehaviour = minotaurObj.GetComponent<MinotaurBehaviorController>();
        if (minoBehaviour != null)
        {
            //  Initialize maze data & sub-systems BEFORE network spawn
            minoBehaviour.Initialize(maze);
        }
        else
        {
            Debug.LogWarning("[ContentGenerator] Spawned Minotaur has no MinotaurBehaviorController.");
        }

        // Now network-spawn so OnNetworkSpawn sees a valid maze
        var netObj = minotaurObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                netObj.Spawn(true); // destroy with scene

                Debug.Log($"[ContentGenerator] Minotaur spawned AFTER player at tile {minotaurPos2D}, world {minotaurPos}");
            }
            else
            {
                Debug.LogWarning("[ContentGenerator] Not server; minotaur NetworkObject not spawned.");
            }
        }
        else
        {
            Debug.LogWarning("[ContentGenerator] Minotaur prefab has no NetworkObject component.");
        }
    }

    /*private Vector2Int MinotaurGen(MazeGenerator.MazeData maze, float s)
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
    }*/

    private void KeysAndExitGen(MazeGenerator.MazeData maze, float s, Vector2Int minotaurTile, Vector2Int playerTile)
    {
        int H = maze.tilesH, W = maze.tilesW;

        // Collect candidates
        var openTiles = CollectOpenTiles(maze.open);                 // all open tiles
        var deg = ComputeDegrees(maze.open);
        var deadEndsAll = CollectDeadEndCellsOddOdd(maze.open, deg);   // dead-end cells at odd/odd

        // Avoid using the same tile as player/minotaur
        var used = new HashSet<Vector2Int> { minotaurTile };
        if (playerTile != default)
            used.Add(playerTile);

        // ---- EXIT: nearest dead-end to center (fallback: nearest open to center) ----
        Vector2 center = new Vector2((W - 1) * 0.5f, (H - 1) * 0.5f);

        Vector2Int exitTile = NearestToCenter(deadEndsAll, center);
        if (exitTile == default && deadEndsAll.Count == 0)
        {
            // fallback to any open tile nearest to center
            exitTile = NearestToCenter(openTiles, center);
        }

        if (exitPrefab && exitTile != default)
        {
            SpawnAtTile(exitPrefab, exitTile, maze, s, "Exit");
            used.Add(exitTile);
        }
        else
        {
            Debug.LogWarning("Exit placement: no valid tile found.");
        }

        // ---- KEYS: prefer dead-ends in separate quadrants; fallback keeps distance ----
        var keyTargets = new List<(GameObject prefab, string name)>
        {
            (key1Prefab, "Key_1"),
            (key2Prefab, "Key_2"),
            (key3Prefab, "Key_3")
        };

        var chosenKeyTiles = new List<Vector2Int>(3);

        // Primary attempt: pick from dead-ends in separate quadrants
        if (preferSeparateQuadrantsForKeys && deadEndsAll.Count > 0)
        {
            // Partition by quadrants
            var (q1, q2, q3, q4) = PartitionByQuadrants(deadEndsAll, H, W);

            // Shuffle quadrant order for variety
            var quads = new List<List<Vector2Int>> { q1, q2, q3, q4 };
            Shuffle(quads);

            foreach (var q in quads)
            {
                if (chosenKeyTiles.Count >= 3) break;
                Vector2Int pick;
                if (TryPickRandomFrom(q, used, out pick))
                {
                    chosenKeyTiles.Add(pick);
                    used.Add(pick);
                }
            }
        }

        // Secondary attempt: take remaining from any dead-ends (respecting min distance to exit/keys)
        float minDistSq = minDistanceTilesFallback * minDistanceTilesFallback;
        if (chosenKeyTiles.Count < 3 && deadEndsAll.Count > 0)
        {
            var remaining = new List<Vector2Int>();
            foreach (var t in deadEndsAll) if (!used.Contains(t)) remaining.Add(t);
            var fill = PickWithMinDistance(remaining, used, minDistSq, 3 - chosenKeyTiles.Count);
            foreach (var t in fill) { chosenKeyTiles.Add(t); used.Add(t); }
        }

        // Final fallback: pick from open tiles with min distance
        if (chosenKeyTiles.Count < 3)
        {
            var remainingOpen = new List<Vector2Int>();
            foreach (var t in openTiles) if (!used.Contains(t)) remainingOpen.Add(t);
            var fill = PickWithMinDistance(remainingOpen, used, minDistSq, 3 - chosenKeyTiles.Count);
            foreach (var t in fill) { chosenKeyTiles.Add(t); used.Add(t); }
        }

        // Spawn keys (for however many we managed to choose)
        int placeCount = Mathf.Min(3, chosenKeyTiles.Count);
        for (int i = 0; i < placeCount; i++)
        {
            var (prefab, name) = keyTargets[i];
            if (!prefab) continue;
            SpawnAtTile(prefab, chosenKeyTiles[i], maze, s, name);
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

        // 2) Instantiate roughly there first (keep same parent for hierarchy).
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

        // 4) Network-spawn if we're the server and the prefab has a NetworkObject.
        var netObj = go.GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj != null)
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                // Spawn after we've set the final transform so clients get the correct position on spawn.
                netObj.Spawn(true); // true => destroy with scene if server despawns
            }
            else
            {
                // Not the server: this will be a local-only object (safe in singleplayer/editor),
                // but won't be despawnable over the network.
                // Debug.LogWarning($"SpawnAtTile: not server, '{go.name}' not network-spawned.");
            }
        }
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

    private static int[,] ComputeDegrees(bool[,] open)
    {
        int H = open.GetLength(0), W = open.GetLength(1);
        int[,] d = new int[H, W];
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };
        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                if (!open[r, c]) continue;
                int deg = 0;
                for (int k = 0; k < 4; k++)
                {
                    int nr = r + dr[k], nc = c + dc[k];
                    if (nr >= 0 && nr < H && nc >= 0 && nc < W && open[nr, nc]) deg++;
                }
                d[r, c] = deg;
            }
        }
        return d;
    }

    // Only collect dead-ends at odd/odd (cell centers); keeps picks inside cells.
    private static List<Vector2Int> CollectDeadEndCellsOddOdd(bool[,] open, int[,] deg)
    {
        int H = open.GetLength(0), W = open.GetLength(1);
        var list = new List<Vector2Int>();
        for (int r = 1; r < H; r += 2)
            for (int c = 1; c < W; c += 2)
                if (open[r, c] && deg[r, c] == 1)
                    list.Add(new Vector2Int(c, r)); // x=c, y=r
        return list;
    }

    private static (List<Vector2Int> q1, List<Vector2Int> q2, List<Vector2Int> q3, List<Vector2Int> q4)
        PartitionByQuadrants(List<Vector2Int> tiles, int H, int W)
    {
        int midR = H / 2; // Y midpoint
        int midC = W / 2; // X midpoint
        var q1 = new List<Vector2Int>(); // top-left (y < midR, x < midC)
        var q2 = new List<Vector2Int>(); // top-right (y < midR, x >= midC)
        var q3 = new List<Vector2Int>(); // bottom-left (y >= midR, x < midC)
        var q4 = new List<Vector2Int>(); // bottom-right (y >= midR, x >= midC)

        foreach (var t in tiles)
        {
            bool top = t.y < midR;
            bool left = t.x < midC;
            if (top && left) q1.Add(t);
            else if (top && !left) q2.Add(t);
            else if (!top && left) q3.Add(t);
            else q4.Add(t);
        }
        return (q1, q2, q3, q4);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool TryPickRandomFrom(List<Vector2Int> candidates, HashSet<Vector2Int> used, out Vector2Int result)
    {
        result = default;
        if (candidates == null || candidates.Count == 0) return false;

        // Build a pool excluding used
        var pool = new List<Vector2Int>(candidates.Count);
        foreach (var t in candidates) if (!used.Contains(t)) pool.Add(t);
        if (pool.Count == 0) return false;

        int idx = UnityEngine.Random.Range(0, pool.Count);
        result = pool[idx];
        return true;
    }

    private static List<Vector2Int> PickWithMinDistance(List<Vector2Int> pool, HashSet<Vector2Int> used, float minDistSq, int count)
    {
        var picks = new List<Vector2Int>(count);
        if (pool == null || pool.Count == 0 || count <= 0) return picks;

        // Shuffle pool for variety
        var shuffled = new List<Vector2Int>(pool);
        Shuffle(shuffled);

        foreach (var t in shuffled)
        {
            if (picks.Count >= count) break;
            if (used.Contains(t)) continue;

            bool ok = true;
            foreach (var u in used)
            {
                if (SqrDist(t, u) < minDistSq) { ok = false; break; }
            }
            if (!ok) continue;

            // also keep distance from already-picked in this batch
            foreach (var p in picks)
            {
                if (SqrDist(t, p) < minDistSq) { ok = false; break; }
            }
            if (!ok) continue;

            picks.Add(t);
        }

        return picks;
    }

    private static float SqrDist(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x, dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    private static Vector2Int NearestToCenter(List<Vector2Int> tiles, Vector2 center)
    {
        if (tiles == null || tiles.Count == 0) return default;
        float best = float.MaxValue;
        Vector2Int bestT = default;
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            float dx = t.x - center.x;
            float dy = t.y - center.y;
            float d2 = dx * dx + dy * dy;
            if (d2 < best)
            {
                best = d2;
                bestT = t;
            }
        }
        return bestT;
    }

    private static List<Vector2Int> PickDistinctTiles(List<Vector2Int> pool, HashSet<Vector2Int> avoid, int count)
    {
        // (Unused by the new logic, kept in case you still call it elsewhere.)
        var filtered = new List<Vector2Int>(pool.Count);
        foreach (var t in pool)
            if (!avoid.Contains(t)) filtered.Add(t);

        for (int i = filtered.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (filtered[i], filtered[j]) = (filtered[j], filtered[i]);
        }

        if (filtered.Count <= count) return filtered;
        return filtered.GetRange(0, count);
    }

    /// <summary>
    /// Returns a random open tile in WORLD space for spawning a player.
    /// Uses the same tile coordinate system as keys/exit (x = col * s, z = row * s),
    /// and accounts for this ContentGenerator's transform (Maze Root).
    /// </summary>
    public bool TryGetRandomOpenTileWorldPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (_currentMaze == null || _currentMaze.open == null)
        {
            Debug.LogWarning("ContentGenerator: No cached maze data; cannot get random tile.");
            return false;
        }

        var openTiles = CollectOpenTiles(_currentMaze.open);
        if (openTiles == null || openTiles.Count == 0)
        {
            Debug.LogWarning("ContentGenerator: No open tiles found; cannot get random tile.");
            return false;
        }

        // Pick a random open tile
        int idx = UnityEngine.Random.Range(0, openTiles.Count);
        Vector2Int tile = openTiles[idx];

        float s = _currentTileSize > 0f ? _currentTileSize : _currentMaze.tileSize;

        // Local tile center (same convention as SpawnAtTile)
        Vector3 localPos = new Vector3(tile.x * s, 0f, tile.y * s);

        // Convert to world space using the maze root's transform
        Vector3 worldCenter = transform.TransformPoint(localPos);

        // Lift up by half the player's height so we don't spawn inside the floor
        float yLift = 0.5f;
        if (player != null)
        {
            yLift = GetHalfHeight(player);
        }

        worldCenter.y += yLift;

        worldPos = worldCenter;
        return true;
    }

}
