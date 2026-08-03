using System.Collections.Generic;
using UnityEngine;

public static class GetTilePosition
{
    // Returns the open tile closest to the center of the maze
    public static Vector2Int ClosestToCenter(MazeGenerator.MazeData maze, float s)
    {
        int centerRow = maze.tilesH / 2;
        int centerCol = maze.tilesW / 2;
        Vector2Int trueCenter = new Vector2Int(centerCol, centerRow); // x=col, y=row

        Vector2Int closest = Vector2Int.zero;
        float minDist = float.MaxValue;

        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if (maze.open[r, c])
                {
                    float dist = Vector2Int.Distance(new Vector2Int(c, r), trueCenter);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = new Vector2Int(c, r); // x=col, y=row
                    }
                }
            }
        }

        return closest;
    }

    public static Vector2Int FromRangeClosestToCenter(List<Vector2Int> tiles, Vector2 center)
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

    // Returns a random open tile near the edges
    public static Vector2Int WithinEdgeMargin(MazeGenerator.MazeData maze, int margin)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if (maze.open[r, c] && (r < margin || c < margin || r >= maze.tilesH - margin || c >= maze.tilesW - margin))
                {
                    options.Add(new Vector2Int(c, r)); // x=col, y=row
                }
            }
        }

        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    // Returns a list of all open tiles
    public static List<Vector2Int> GetOpenTiles(MazeGenerator.MazeData maze)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if (maze.open[r, c])
                {
                    options.Add(new Vector2Int(c, r)); // x=col, y=row
                }
            }
        }

        return options;
    }

    // Returns a random open tile within a sub-rectangle of the maze
    public static Vector2Int OpenInRange(MazeGenerator.MazeData maze, int rMin, int rMax, int cMin, int cMax)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        for (int r = rMin; r < rMax; r++)
        {
            for (int c = cMin; c < cMax; c++)
            {
                if (maze.open[r, c])
                {
                    options.Add(new Vector2Int(c, r)); // x=col, y=row
                }
            }
        }

        if (options.Count == 0)
        {
            Debug.LogWarning("No open tiles found in the specified range!");
            return Vector2Int.zero;
        }

        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    // Spawns a prefab indicator at the tile's world position
    public static void SpawnIndicator(Vector2Int tile, Color color, GameObject prefab, float tileSize = 1f)
    {
        Vector3 worldPos = new Vector3(tile.x * tileSize, 0, tile.y * tileSize);
        GameObject indicator = Object.Instantiate(prefab, worldPos, Quaternion.identity);
        indicator.GetComponent<Renderer>().material.color = color;
    }
    public static List<Vector2Int> PickDistinctTiles(List<Vector2Int> pool, HashSet<Vector2Int> avoid, int count)
    {
        
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
}
