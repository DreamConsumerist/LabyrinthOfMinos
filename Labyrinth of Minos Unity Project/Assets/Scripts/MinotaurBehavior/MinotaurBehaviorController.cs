using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : MonoBehaviour
{
    // Initializing maze data and passing reference to this component to MinotaurMovement
    [SerializeField] MinotaurMovement movement;
    public MazeGenerator.MazeData maze;
    private List<Vector2Int> patrolPath;
    PlayerData player;

    MinotaurBaseState currentState;
    MinotaurChaseState ChaseState = new MinotaurChaseState();
    MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    MinotaurPatrolState PatrolState = new MinotaurPatrolState();


    // Initializing variables and data structures related to the aggro system
    //[SerializeField] float aggroDecayRate = 5f;
    //[SerializeField] float maxAggro = 100f;
    //private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };

    private void Awake()
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
    }
    void Start()
    {
        currentState = PatrolState;
        currentState.EnterState(this);
        player = UnityEngine.Object.FindAnyObjectByType<PlayerData>();
    }

    // Update is called once per frame
    void Update()
    {
        currentState.UpdateState(this);
        movement.UpdateTarget(new Vector2Int(Mathf.RoundToInt(player.transform.position.x / maze.tileSize), Mathf.RoundToInt(player.transform.position.z / maze.tileSize)));
    }

    public void Initialize(MazeGenerator.MazeData mazeObj)
    {
        maze = mazeObj;
        movement.Initialize(this);
        patrolPath = PatrolPathGeneration(maze);
    }

    private List<Vector2Int> PatrolPathGeneration(MazeGenerator.MazeData maze)
    {

        Vector2Int A = GetTilePosition.OpenInRange(maze, 0, maze.tilesW / 2, 0, maze.tilesH / 2);
        Vector2Int B = GetTilePosition.OpenInRange(maze, 0, maze.tilesW / 2, maze.tilesH / 2, maze.tilesH);
        Vector2Int C = GetTilePosition.OpenInRange(maze, maze.tilesW / 2, maze.tilesW, maze.tilesH / 2, maze.tilesH);
        Vector2Int D = GetTilePosition.OpenInRange(maze, maze.tilesW / 2, maze.tilesW, 0, maze.tilesH / 2);

        List<Vector2Int> totalPath = new List<Vector2Int>();
        List<Vector2Int> pathAB = A_StarPathfinding.FindPath(A, B, maze.open);
        List<Vector2Int> pathBC = A_StarPathfinding.FindPath(B, C, maze.open);
        List<Vector2Int> pathCD = A_StarPathfinding.FindPath(C, D, maze.open);
        List<Vector2Int> pathDA = A_StarPathfinding.FindPath(D, A, maze.open);

        totalPath.AddRange(pathAB);
        totalPath.AddRange(pathBC.Skip(1));
        totalPath.AddRange(pathCD.Skip(1));
        totalPath.AddRange(pathDA.Skip(1));
        return totalPath;
    }
}
