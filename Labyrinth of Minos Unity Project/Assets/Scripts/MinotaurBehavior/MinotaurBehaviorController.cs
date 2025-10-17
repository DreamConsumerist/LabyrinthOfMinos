using System;
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

    MinotaurBaseState currentState;
    MinotaurChaseState ChaseState = new MinotaurChaseState();
    MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    MinotaurPatrolState PatrolState = new MinotaurPatrolState();

    public Rigidbody rb;

    // Initializing variables and data structures related to the aggro system
    //[SerializeField] float aggroDecayRate = 5f;
    //[SerializeField] float maxAggro = 100f;
    //private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };

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
}
