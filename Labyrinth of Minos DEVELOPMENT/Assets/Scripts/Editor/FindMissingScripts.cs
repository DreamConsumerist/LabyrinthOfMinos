using UnityEngine;
using UnityEditor;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void Find()
    {
        int count = 0;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None) continue;

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script on: {GetPath(go)}", go);
                    count++;
                }
            }
        }
        Debug.Log($"Found {count} missing script reference(s).");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
