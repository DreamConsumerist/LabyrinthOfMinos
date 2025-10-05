using UnityEngine;
using System.Collections.Generic;

public static class A_StarPathfinding
{
    public class Node
    {
        float g_cost, h_cost;
        float FCost => g_cost + h_cost;

        Node cameFrom;
        public Node() { }
    }
    public static List<Vector2Int> FindPath(Vector2Int currentPos, Vector2Int targetPos, bool[,] maze)
    {
        
        
        List<Vector2Int> path = new List<Vector2Int>();
        return path;
    }
}
