using NUnit.Framework;
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class ContentGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject minotaur;
    public GameObject player;
    [SerializeField] public int playerSpawnMargins = 2;

    // Generates objects in the maze based on MazeData
    public void Generate(MazeGenerator.MazeData maze)
    {
        if (maze == null || maze.open == null) { Debug.LogError("Maze data missing"); return; }

        int H = maze.tilesH, W = maze.tilesW;
        float s = maze.tileSize;

        MinotaurGen(maze, s);
        PlayerGen(maze, s);
    }

    private void PlayerGen(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int playerPos2D = GetTilePosition.WithinEdgeMargin(maze, playerSpawnMargins);
        
        Vector3 playerPos = new Vector3(
            playerPos2D.x * s,
            player.GetComponent<Renderer>().bounds.size.y / 2,
            playerPos2D.y * s
        );

        var playerObj = Instantiate(player, playerPos, Quaternion.identity, transform);
        var playerBehav = playerObj.GetComponent<AutonomousPatrol>();
        playerBehav.Initialize(maze);
    }

    private void MinotaurGen(MazeGenerator.MazeData maze, float s)
    {
        Vector2Int minotaurPos2D = GetTilePosition.ClosestToCenter(maze, s);

        // Convert 2D tile coords to 3D world position
        Vector3 minotaurPos = new Vector3(
            minotaurPos2D.x * s,
            minotaur.GetComponent<Renderer>().bounds.size.y / 2,
            minotaurPos2D.y * s
        );

        // Instantiate the minotaur at the calculated position
        var minotaurObj = Instantiate(minotaur, minotaurPos, Quaternion.identity, transform);
        var minotaurBehavior = minotaurObj.GetComponent<MinotaurBehaviorController>();
        minotaurBehavior.Initialize(maze);
    }
}
