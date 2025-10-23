/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : MonoBehaviour
{
    // Initializing maze data and passing reference to this component to MinotaurMovement
    public MinotaurMovement movement;
    public MazeGenerator.MazeData maze;
    public List<Vector2Int> patrolPath;
    public PlayerData player;
    [SerializeField] public GameObject indicator;

    MinotaurBaseState currentState;
    public MinotaurChaseState ChaseState = new MinotaurChaseState();
    public MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    public MinotaurPatrolState PatrolState = new MinotaurPatrolState();

    public Rigidbody rb;

    private void Awake()
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
        rb = GetComponent<Rigidbody>();
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
    }

    private void FixedUpdate()
    {
        currentState.FixedUpdateState(this);
    }

    public void Initialize(MazeGenerator.MazeData mazeObj)
    {
        maze = mazeObj;
        movement.Initialize(this);
    }

    public void ChangeState(MinotaurBaseState state)
    {
        currentState.ExitState(this);
        currentState = state;
        currentState.EnterState(this);
    }

    void OnDrawGizmos()
    {
        if (PatrolState == null || PatrolState.patrolPath == null || maze == null)
            return;

        Gizmos.color = Color.yellow; // a different color
        float s = maze.tileSize;

        var path = PatrolState.patrolPath;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 from = new Vector3(path[i].x * s, 0.5f, path[i].y * s);
            Vector3 to = new Vector3(path[i + 1].x * s, 0.5f, path[i + 1].y * s);
            Gizmos.DrawLine(from, to);
        }
    }
}


// Initializing variables and data structures related to the aggro system
//[SerializeField] float aggroDecayRate = 5f;
//[SerializeField] float maxAggro = 100f;
//private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };*/