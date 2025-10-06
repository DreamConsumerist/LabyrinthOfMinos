using Unity.VisualScripting;
using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

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

        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                bool isFloor = maze.open[r, c];
                var prefab = isFloor ? floorPrefab : wallPrefab;
                if (!prefab) continue;
                Vector3 pos = new Vector3(r, c);
                if (isFloor)
                {
                    pos = xz ? new Vector3(c * s, 0f, r * s)
                             : new Vector3(c * s, r * s, 0f);
                }
                else
                {
                    pos = xz ? new Vector3(c * s, prefab.GetComponent<Renderer>().bounds.size.y / 2, r * s)
                             : new Vector3(c * s, r * s, 0f);
                }
                    var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                go.name = (isFloor ? "Floor_" : "Wall_") + r + "_" + c;
            }
        }
    }
}
