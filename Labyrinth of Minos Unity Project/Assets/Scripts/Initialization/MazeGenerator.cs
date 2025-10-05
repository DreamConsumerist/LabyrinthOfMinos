using System;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Cells (logical)")]
    public int cellsW = 40;
    public int cellsH = 40;

    [Header("Dead-end target (% of cells)")]
    [Range(0f, 0.5f)] public float targetDeadEndLow = 0.03f;
    [Range(0f, 0.5f)] public float targetDeadEndHigh = 0.05f;

    [Header("Loop shaping")]
    public bool avoid2x2Squares = true;   // avoid full-open 2x2
    public bool avoid3x3Donuts = true;    // avoid ring donuts

    [Header("Seeding")]
    public bool useFixedSeed = true;
    public int fixedSeed = 12345;

    [Header("Build Parameters (for consumers)")]
    public float tileSize = 1f;
    public bool useXZPlane = true; // else XY

    [Serializable]
    public class MazeData
    {
        public bool[,] open;              // false=wall, true=floor (tiles)
        public int tilesH, tilesW;        // = 2*cellsH+1, 2*cellsW+1
        public int cellsH, cellsW;
        public Vector2Int start;          // tile coords (odd,odd)
        public Vector2Int end;            // tile coords (odd,odd)
        public float tileSize;
        public bool useXZPlane;
    }

    public MazeData LastMaze { get; private set; }

    [ContextMenu("Generate Now")]
    public void GenerateNow()
    {
        LastMaze = Generate();
        Debug.Log($"Maze generated: {LastMaze.tilesW}x{LastMaze.tilesH}");
    }

    public MazeData Generate()
    {
        int H = 2 * cellsH + 1;
        int W = 2 * cellsW + 1;
        var open = new bool[H, W]; // all walls false by default

        int seed = useFixedSeed ? fixedSeed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var rnd = new System.Random(seed);

        // --- Randomized Prim's (frontier) on odd,odd cells ---
        int sr = 2 * rnd.Next(cellsH) + 1;
        int sc = 2 * rnd.Next(cellsW) + 1;
        open[sr, sc] = true;

        var frontier = new List<(int r, int c, int r2, int c2, int wr, int wc)>();
        foreach (var e in NeighCells(sr, sc, H, W)) frontier.Add((sr, sc, e.r2, e.c2, e.wr, e.wc));

        while (frontier.Count > 0)
        {
            int i = rnd.Next(frontier.Count);
            var e = frontier[i]; frontier.RemoveAt(i);
            if (!open[e.r2, e.c2])
            {
                open[e.wr, e.wc] = true; // carve the wall
                open[e.r2, e.c2] = true; // open the new cell
                foreach (var n in NeighCells(e.r2, e.c2, H, W))
                    if (!open[n.r2, n.c2])
                        frontier.Add((e.r2, e.c2, n.r2, n.c2, n.wr, n.wc));
            }
        }

        // --- Braid to target dead-end range with donut guards ---
        BraidToDeadEndTarget(open, rnd);

        // choose start/end (odd,odd corners are guaranteed cells)
        var start = new Vector2Int(1, 1);
        var end = new Vector2Int(W - 2, H - 2);
        if (!open[end.y, end.x])
        {
            // PATCH: ensure we don't open rim walls; connect end via interior-only wall to interior-open cell
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            for (int k = 0; k < 4; k++)
            {
                int wr = end.y + dr[k], wc = end.x + dc[k];       // wall adjacent to end
                int rr = end.y + 2 * dr[k], cc = end.x + 2 * dc[k]; // neighbor cell beyond
                if (rr <= 0 || rr >= H - 1 || cc <= 0 || cc >= W - 1) continue; // interior cell only
                if (!IsBorder(wr, wc, H, W)                        // don't punch the rim
                    && !open[wr, wc]
                    && open[rr, cc]                                 // connect to interior open
                    && (!avoid2x2Squares || !WouldMake2x2(open, wr, wc))
                    && (!avoid3x3Donuts || !WouldMakeDonut(open, wr, wc)))
                {
                    open[wr, wc] = true;
                    break;
                }
            }
            open[end.y, end.x] = true;
        }

        return LastMaze = new MazeData
        {
            open = open,
            tilesH = H,
            tilesW = W,
            cellsH = cellsH,
            cellsW = cellsW,
            start = start,
            end = end,
            tileSize = tileSize,
            useXZPlane = useXZPlane
        };
    }

    // --- Helpers ---
    IEnumerable<(int r2, int c2, int wr, int wc)> NeighCells(int r, int c, int H, int W)
    {
        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };
        for (int k = 0; k < 4; k++)
        {
            int r2 = r + dr[k], c2 = c + dc[k];
            if (r2 > 0 && r2 < H - 1 && c2 > 0 && c2 < W - 1)
            {
                int wr = (r + r2) / 2, wc = (c + c2) / 2;
                yield return (r2, c2, wr, wc);
            }
        }
    }

    IEnumerable<(int wr, int wc)> AdjWalls(int r, int c, int H, int W)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };
        for (int k = 0; k < 4; k++)
        {
            int wr = r + dr[k], wc = c + dc[k];
            if (wr >= 0 && wr < H && wc >= 0 && wc < W) yield return (wr, wc);
        }
    }

    // PATCH: helper to identify perimeter tiles
    bool IsBorder(int r, int c, int H, int W)
    {
        return (r == 0 || c == 0 || r == H - 1 || c == W - 1);
    }

    int Degree(bool[,] g, int r, int c)
    {
        int d = 0, H = g.GetLength(0), W = g.GetLength(1);
        if (r > 0 && g[r - 1, c]) d++;
        if (r + 1 < H && g[r + 1, c]) d++;
        if (c > 0 && g[r, c - 1]) d++;
        if (c + 1 < W && g[r, c + 1]) d++;
        return d;
    }

    List<(int r, int c)> DeadEnds(bool[,] g)
    {
        var list = new List<(int, int)>();
        int H = g.GetLength(0), W = g.GetLength(1);
        for (int r = 1; r < H; r += 2)
            for (int c = 1; c < W; c += 2)
                if (g[r, c] && Degree(g, r, c) == 1) list.Add((r, c));
        return list;
    }

    bool WouldMake2x2(bool[,] g, int wr, int wc)
    {
        if (g[wr, wc]) return false;
        int H = g.GetLength(0), W = g.GetLength(1);
        for (int dr = -1; dr <= 0; dr++)
            for (int dc = -1; dc <= 0; dc++)
            {
                int r0 = wr + dr, c0 = wc + dc;
                if (r0 < 0 || c0 < 0 || r0 + 1 >= H || c0 + 1 >= W) continue;
                int open = 0;
                for (int rr = 0; rr < 2; rr++)
                    for (int cc = 0; cc < 2; cc++)
                        if (g[r0 + rr, c0 + cc]) open++;
                if (!g[wr, wc]) open++;
                if (open == 4) return true;
            }
        return false;
    }

    bool WouldMakeDonut(bool[,] g, int wr, int wc)
    {
        if (g[wr, wc]) return false;
        int H = g.GetLength(0), W = g.GetLength(1);
        for (int r0 = wr - 2; r0 <= wr; r0++)
            for (int c0 = wc - 2; c0 <= wc; c0++)
            {
                if (r0 < 0 || c0 < 0 || r0 + 2 >= H || c0 + 2 >= W) continue;
                int open = 0;
                for (int rr = 0; rr < 3; rr++)
                    for (int cc = 0; cc < 3; cc++)
                        if (g[r0 + rr, c0 + cc]) open++;
                if (!g[wr, wc]) open++;
                if (open == 9) return true;
            }
        return false;
    }

    void BraidToDeadEndTarget(bool[,] open, System.Random rnd)
    {
        int H = open.GetLength(0), W = open.GetLength(1);
        int targetLow = (int)((H / 2) * (W / 2) * targetDeadEndLow);
        int targetHigh = (int)((H / 2) * (W / 2) * targetDeadEndHigh);

        var deadEnds = DeadEnds(open);
        while (deadEnds.Count > targetHigh)
        {
            int i = rnd.Next(deadEnds.Count);
            var (r, c) = deadEnds[i];
            deadEnds.RemoveAt(i);

            // attempt to remove dead-end by carving
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            var candidates = new List<(int wr, int wc)>();
            for (int k = 0; k < 4; k++)
            {
                int wr = r + dr[k], wc = c + dc[k];
                if (wr <= 0 || wr >= H - 1 || wc <= 0 || wc >= W - 1) continue;
                if (!open[wr, wc]) candidates.Add((wr, wc));
            }

            if (candidates.Count > 0)
            {
                var pick = candidates[rnd.Next(candidates.Count)];
                if ((!avoid2x2Squares || !WouldMake2x2(open, pick.wr, pick.wc))
                    && (!avoid3x3Donuts || !WouldMakeDonut(open, pick.wr, pick.wc)))
                    open[pick.wr, pick.wc] = true;
            }

            deadEnds = DeadEnds(open);
            if (deadEnds.Count <= targetLow) break;
        }
    }
}
