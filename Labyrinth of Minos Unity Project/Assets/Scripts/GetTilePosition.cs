using System.Collections.Generic;
using UnityEngine;

public static class GetTilePosition
{
    public static Vector2Int ClosestToCenter(MazeGenerator.MazeData maze, float s)
    {
        int centerR = maze.tilesH / 2;
        int centerC = maze.tilesW / 2;
        Vector2Int trueCenter = new Vector2Int(centerR, centerC);
        Vector2Int pos2D = Vector2Int.zero;
        float minDist = float.MaxValue;
        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if (maze.open[r, c])
                {
                    float dist = Vector2Int.Distance(new Vector2Int(r, c), trueCenter);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        pos2D = new Vector2Int(r, c);
                    }
                }
            }
        }

        return pos2D;
    }

    public static Vector2Int WithinEdgeMargin(MazeGenerator.MazeData maze, int margin)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if ((maze.open[r, c]) && ((r < margin) || (c < margin) || (r >= maze.tilesH - margin) || (c >= maze.tilesW - margin)))
                {
                    options.Add(new Vector2Int(r, c));
                }
            }
        }

        Vector2Int pos2D = options[UnityEngine.Random.Range(0, options.Count)];
        return pos2D;
    }

    public static List<Vector2Int> GetOpenTiles(MazeGenerator.MazeData maze)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        for (int r = 0; r < maze.tilesH; r++)
        {
            for (int c = 0; c < maze.tilesW; c++)
            {
                if (maze.open[r, c])
                {
                    options.Add(new Vector2Int(r, c));
                }
            }
        }
        return options;
    }

    public static Vector2Int OpenInRange(MazeGenerator.MazeData maze, int rMin, int rMax, int cMax, int cMin)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        for (int r = rMin; r < rMax; r++)
        {
            for (int c = cMin; c < rMax; c++)
            {
                if (maze.open[r, c])
                {
                    options.Add(new Vector2Int(r, c));
                }
            }
        }

        Vector2Int pos2D = options[UnityEngine.Random.Range(0, options.Count)];
        return pos2D;
    }
}
