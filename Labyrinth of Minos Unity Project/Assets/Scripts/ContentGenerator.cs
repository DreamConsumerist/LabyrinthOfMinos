using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class ContentGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject minotaur;
    public GameObject player;

    // Clears all children under this GameObject
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

    // Generates objects in the maze based on MazeData
    public void Generate(MazeGenerator.MazeData maze)
    {
        if (maze == null || maze.open == null) { Debug.LogError("Maze data missing"); return; }

        int H = maze.tilesH, W = maze.tilesW;
        float s = maze.tileSize;

        int centerR = maze.tilesH / 2;
        int centerC = maze.tilesW / 2;
        Vector2Int trueCenter = new Vector2Int(centerR, centerC);
        Vector2Int minotaurPos2D = Vector2Int.zero;
        float minDist = float.MaxValue;

        // Find the open tile closest to the center
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
                        minotaurPos2D = new Vector2Int(r, c);
                    }
                }
            }
        }

        // Convert 2D tile coords to 3D world position
        Vector3 minotaurPos = new Vector3(
            minotaurPos2D.x * s,
            minotaur.GetComponent<Renderer>().bounds.size.y / 2,
            minotaurPos2D.y * s
        );

        // Instantiate the minotaur at the calculated position
        var go = Instantiate(minotaur, minotaurPos, Quaternion.identity, transform);
    }
}
