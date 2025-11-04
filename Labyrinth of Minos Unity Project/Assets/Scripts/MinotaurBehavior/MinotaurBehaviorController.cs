using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : NetworkBehaviour
{
    public MinotaurMovement movement;
    public MazeGenerator.MazeData maze;
    public List<Vector2Int> patrolPath;
    public PlayerData player;
    [SerializeField] public GameObject indicator;

    private MinotaurBaseState currentState;
    public MinotaurChaseState ChaseState = new MinotaurChaseState();
    public MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    public MinotaurPatrolState PatrolState = new MinotaurPatrolState();

    public Rigidbody rb;

    private void Awake()
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Initialize maze and AI only on server
            currentState = PatrolState;
            currentState.EnterState(this);
            movement.Initialize(this);
            player = FindAnyObjectByType<PlayerData>();
        }
        else
        {
            // Client-only setup (visuals, indicators)
            if (indicator != null) indicator.SetActive(true);
        }
    }

    //private void Update()
    //{
    //    if (!IsServer) return;
    //    currentState.UpdateState(this);
    //}

    private void FixedUpdate()
    {
        if (!IsServer) return;
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

    private void OnDrawGizmos()
    {
        if (PatrolState == null || PatrolState.patrolPath == null || maze == null)
            return;

        Gizmos.color = Color.yellow;
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
