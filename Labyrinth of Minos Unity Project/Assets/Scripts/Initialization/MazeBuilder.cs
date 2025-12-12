using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPrefab;

    [Tooltip("Fallback wall prefab if no section prefab is found.")]
    public GameObject wallPrefab;

    [System.Serializable]
    public class WallSectionPrefabs
    {
        [Tooltip("Optional label, e.g., 'Brick', 'Concrete'")]
        public string sectionName;

        [Tooltip("Wall prefabs used in this section (e.g., Brick_1, Brick_2)")]
        public GameObject[] wallPrefabs;
    }

    [Header("Wall Sections & Blending")]
    [Tooltip("Wall sections ordered left-to-right across the maze. For 2 sections: index 0 = left (e.g., brick), index 1 = right (e.g., concrete).")]
    public WallSectionPrefabs[] wallSections;

    [Tooltip("How many vertical sections to slice the maze into. Typically set to wallSections.Length.")]
    public int sectionCount = 2;

    [Tooltip("Width in tiles around section borders where styles can mix.")]
    [Min(0)]
    public int sectionBlendWidth = 3;

    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    public void Build(MazeGenerator.MazeData maze)
    {
        int H = maze.tilesH, W = maze.tilesW;
        float s = maze.tileSize;
        bool xz = maze.useXZPlane;

        // Seeded random so texture choices are stable per maze seed
        System.Random rnd = new System.Random(maze.seed);

        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                bool isFloor = maze.open[r, c];

                GameObject prefab;
                if (isFloor)
                {
                    prefab = floorPrefab;
                }
                else
                {
                    prefab = GetWallPrefabFor(r, c, maze, rnd);
                }

                if (!prefab) continue;

                Vector3 pos;
                if (isFloor)
                {
                    pos = xz ? new Vector3(c * s, 0f, r * s)
                             : new Vector3(c * s, r * s, 0f);
                }
                else
                {
                    // Use prefab height so taller/shorter walls still sit on the ground
                    float yHeight = prefab.GetComponent<Renderer>().bounds.size.y;
                    pos = xz ? new Vector3(c * s, yHeight / 2f, r * s)
                             : new Vector3(c * s, r * s, 0f);
                }

                var go = Instantiate(prefab, pos, Quaternion.identity, transform);

                if (!isFloor)
                {
                    // Uniform scale in XZ; tweak if some walls need custom scale
                    go.transform.localScale = new Vector3(s, s, s);
                }
                else
                {
                    // Keep floor thickness but match tile size in XZ
                    go.transform.localScale = new Vector3(s, go.transform.localScale.y, s);
                }

                go.name = (isFloor ? "Floor_" : "Wall_") + r + "_" + c;
            }
        }
    }

    
    private GameObject GetWallPrefabFor(int r, int c, MazeGenerator.MazeData maze, System.Random rnd)
    {
        // No section data? Use single wall prefab.
        if (wallSections == null || wallSections.Length == 0 || sectionCount <= 0)
        {
            return wallPrefab;
        }

        int W = maze.tilesW;
        int actualSectionCount = Mathf.Min(Mathf.Max(sectionCount, 1), wallSections.Length);

        // Base section: slice maze width into vertical bands
        float sectionWidth = (float)W / actualSectionCount;
        int baseSection = Mathf.Clamp(Mathf.FloorToInt(c / sectionWidth), 0, actualSectionCount - 1);
        int chosenSection = baseSection;

        // Blend: if near any border between sections, randomly mix neighbor styles
        if (sectionBlendWidth > 0 && actualSectionCount > 1)
        {
            // Find distance in columns to nearest section border
            float minDist = float.MaxValue;
            int nearestBorderIndex = -1;

            for (int k = 1; k < actualSectionCount; k++)
            {
                float border = k * sectionWidth;  // border between section k-1 and k
                float dist = Mathf.Abs(c - border);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestBorderIndex = k;
                }
            }

            if (minDist <= sectionBlendWidth && nearestBorderIndex != -1)
            {
                // Determine which side we're on for the nearest border
                int leftSection = nearestBorderIndex - 1;
                int rightSection = nearestBorderIndex;

                // Clamp just in case
                leftSection = Mathf.Clamp(leftSection, 0, actualSectionCount - 1);
                rightSection = Mathf.Clamp(rightSection, 0, actualSectionCount - 1);

                // Randomly choose left or right section in the blend zone
                chosenSection = (rnd.NextDouble() < 0.5) ? leftSection : rightSection;
            }
        }

        chosenSection = Mathf.Clamp(chosenSection, 0, wallSections.Length - 1);

        var set = wallSections[chosenSection];
        if (set == null || set.wallPrefabs == null || set.wallPrefabs.Length == 0)
        {
            return wallPrefab;
        }

        // Pick a random variant within that section (brick_1 vs brick_2)
        int idx = rnd.Next(set.wallPrefabs.Length);
        var prefab = set.wallPrefabs[idx];

        // fall back to generic wall prefab
        if (!prefab) return wallPrefab;

        return prefab;
    }
}
