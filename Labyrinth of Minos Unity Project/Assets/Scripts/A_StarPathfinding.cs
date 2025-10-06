using System;
using System.Collections.Generic;
using UnityEngine;

public static class A_StarPathfinding
{
    public class Node
    {
        public Vector2Int mazePos;
        public float g_cost, h_cost;
        public Node cameFrom;

        public float FCost => g_cost + h_cost;

        public Node(Vector2Int pos, float g, float h, Node parent = null)
        {
            mazePos = pos;
            g_cost = g;
            h_cost = h;
            cameFrom = parent;
        }
    }

    private class PriorityQueue
    {
        private List<Node> heap = new List<Node>();

        public int Count => heap.Count;

        public void Enqueue(Node node)
        {
            heap.Add(node);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[i].FCost >= heap[parent].FCost) break;
                Swap(i, parent);
                i = parent;
            }
        }

        public Node Dequeue()
        {
            if (heap.Count == 0) return null;
            Node root = heap[0];
            Node last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return root;

            heap[0] = last;
            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < heap.Count && heap[left].FCost < heap[smallest].FCost) smallest = left;
                if (right < heap.Count && heap[right].FCost < heap[smallest].FCost) smallest = right;
                if (smallest == i) break;

                Swap(i, smallest);
                i = smallest;
            }

            return root;
        }

        private void Swap(int a, int b)
        {
            Node temp = heap[a];
            heap[a] = heap[b];
            heap[b] = temp;
        }
    }

    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int dest, bool[,] maze)
    {
        Node endNode = FindEndNode(origin, dest, maze);
        if (endNode == null) return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Node curr = endNode;
        while (curr != null)
        {
            path.Add(curr.mazePos);
            curr = curr.cameFrom;
        }
        path.Reverse();
        return path;
    }

    public static Node FindEndNode(Vector2Int origin, Vector2Int dest, bool[,] maze)
    {
        var openSet = new PriorityQueue();
        var bestGCosts = new Dictionary<Vector2Int, float>();

        Node start = new Node(origin, 0, Heuristic(origin, dest));
        openSet.Enqueue(start);
        bestGCosts[origin] = 0f;

        Vector2Int[] directions = {
            new Vector2Int(0, 1),  // up
            new Vector2Int(0, -1), // down
            new Vector2Int(-1, 0), // left
            new Vector2Int(1, 0)   // right
        };

        int height = maze.GetLength(0);
        int width = maze.GetLength(1);

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();

            if (current.mazePos == dest)
                return current;

            foreach (var dir in directions)
            {
                int nx = current.mazePos.x + dir.x;
                int ny = current.mazePos.y + dir.y;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                if (!maze[ny, nx]) // maze[y, x]
                    continue;

                Vector2Int neighborPos = new Vector2Int(nx, ny);
                float tentativeG = current.g_cost + 1;

                if (bestGCosts.TryGetValue(neighborPos, out float existingG) && tentativeG >= existingG)
                    continue;

                bestGCosts[neighborPos] = tentativeG;
                Node neighbor = new Node(neighborPos, tentativeG, Heuristic(neighborPos, dest), current);
                openSet.Enqueue(neighbor);
            }
        }

        return null;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}


//using System.Collections.Generic;
//using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public static class A_StarPathfinding
//{
//    public class Node
//    {
//        public float g_cost, h_cost;
//        public float FCost => g_cost + h_cost;

//        public Vector2Int mazePos;

//        public Node cameFrom;
//        public Node(Vector2Int currPos, Vector2Int origin, Vector2Int dest, Node cameFrom = null) {
//            h_cost = Mathf.Abs(currPos.y - dest.y) + Mathf.Abs(currPos.x - dest.x);
//            if (currPos == origin)
//            {
//                g_cost = 0;
//            }
//            else
//            {
//                g_cost = 1 + cameFrom.g_cost;
//            }
//            mazePos = currPos;
//            this.cameFrom = cameFrom;
//        }
//    }

//    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int dest, bool[,] maze)
//    {
//        List<Vector2Int> path = new List<Vector2Int>();
//        Node end = FindEndNode(origin, dest, maze);
//        Node currNode = end;
//        while (currNode != null)
//        {
//            path.Add(currNode.mazePos);
//            currNode = currNode.cameFrom;
//        }
//        path.Reverse();
//        return path;
//    }
//    public static Node FindEndNode(Vector2Int origin, Vector2Int dest, bool[,] maze)
//    {
//        int iterations = 0;
//        int maxIterations = 10000;


//        List<Node> open = new List<Node>();
//        List<Node> closed = new List<Node>();
//        Node start = new Node(origin, origin, dest);
//        open.Add(start);

//        while (open.Count > 0)
//        {
//            // Priority queue for next node may be more performant
//            int minFCostIndex = 0;
//            float minFCost = float.MaxValue;
//            for (int i = 0; i < open.Count; i++)
//            {
//                if (open[i].FCost < minFCost)
//                {
//                    minFCostIndex = i;
//                    minFCost = open[i].FCost;
//                }
//            }
//            Node currParent = open[minFCostIndex];
//            open.RemoveAt(minFCostIndex);

//            if ((currParent.mazePos.x + 1 < maze.GetLength(0)) && (maze[currParent.mazePos.x+1, currParent.mazePos.y]))
//            {
//                Node north = new Node(new Vector2Int(currParent.mazePos.x + 1, currParent.mazePos.y), origin, dest, currParent);
//                if (north.mazePos == dest)
//                {
//                    return north;
//                }
//                else if (CheckIfValid(open, closed, north))
//                {
//                    open.Add(north);
//                }
//            }
//            if ((currParent.mazePos.y + 1 <  maze.GetLength(1)) && (maze[currParent.mazePos.x, currParent.mazePos.y + 1]))
//            {
//                Node east = new Node(new Vector2Int(currParent.mazePos.x, currParent.mazePos.y + 1), origin, dest, currParent);
//                if (east.mazePos == dest)
//                {
//                    return east;
//                }
//                if (CheckIfValid(open, closed, east))
//                {
//                    open.Add(east);
//                }
//            }
//            if ((currParent.mazePos.x - 1 >= 0) && (maze[currParent.mazePos.x - 1, currParent.mazePos.y]))
//            {
//                Node south = new Node(new Vector2Int(currParent.mazePos.x - 1, currParent.mazePos.y), origin, dest, currParent);
//                if (south.mazePos == dest)
//                {
//                    return south;
//                }
//                if (CheckIfValid(open, closed, south))
//                {
//                    open.Add(south);
//                }
//            }
//            if ((currParent.mazePos.y - 1 >= 0) && (maze[currParent.mazePos.x, currParent.mazePos.y - 1]))
//            {
//                Node west = new Node(new Vector2Int(currParent.mazePos.x, currParent.mazePos.y - 1), origin, dest, currParent);
//                if (west.mazePos == dest)
//                {
//                    return west;
//                }
//                if (CheckIfValid(open, closed, west))
//                {
//                    open.Add(west);
//                }
//            }

//            closed.Add(currParent);
//            iterations++;
//            if (iterations > maxIterations)
//            {
//                Debug.Log("Max iterations reached! size of open: " + open.Count);
//                break;
//            }
//        }
//        return null;
//    }

//    // Current method of checking validity is not performant, could try dictionaries and priority queues for higher efficiency apparently
//    private static bool CheckIfValid(List<Node> open, List<Node> closed, Node successor)
//    {
//        for (int i = 0; i < open.Count; i++)
//        {
//            if ((open[i].mazePos == successor.mazePos) && (open[i].FCost <= successor.FCost))
//            {
//                return false;
//            }
//            else if ((open[i].mazePos == successor.mazePos) && (open[i].FCost > successor.FCost))
//            {
//                open[i].cameFrom = successor.cameFrom;
//                open[i].g_cost = successor.g_cost;
//            }
//        }
//        for (int i = 0; i < closed.Count; i++)
//        {
//            if ((closed[i].mazePos == successor.mazePos) && (closed[i].FCost <= successor.FCost))
//            {
//                return false;
//            }
//        }
//        return true;
//    }
//}
