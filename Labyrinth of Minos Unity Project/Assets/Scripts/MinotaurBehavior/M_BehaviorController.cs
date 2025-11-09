using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.DualShock.LowLevel;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : NetworkBehaviour
{
    // Initialize variables to store references to objects and data
    public Rigidbody rb;
    public MazeGenerator.MazeData maze;
    public PlayerData player;

    // Initialize variables to store instances and outputs of helper classes
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public MinotaurMovement movement;
    public MinotaurParameters parameters;
    public MinotaurSenses senses;
    public MinotaurSenses.SenseReport currSenses;

    // Initialize variables to store instances of states
    MinotaurBaseState currentState;
    public readonly MinotaurChaseState ChaseState = new MinotaurChaseState();
    public readonly MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    public readonly MinotaurPatrolState PatrolState = new MinotaurPatrolState();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<MinotaurMovement>();
        senses = GetComponent<MinotaurSenses>();
        parameters = GetComponent<MinotaurParameters>();

        if (IsServer)
        {
            currentState = PatrolState;
            currentState.EnterState(this);
            movement.Initialize(this);
            player = FindAnyObjectByType<PlayerData>();
        }
    }

    private void Awake() // Awake is called when 
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<MinotaurMovement>();
        senses = GetComponent<MinotaurSenses>();
        parameters = GetComponent<MinotaurParameters>();
    }

    void Update() // Update is called once per frame
    {
        if (!IsServer) return;
        currSenses = senses.SensoryUpdate();
        currentState.UpdateState(currSenses);
    }

    private void FixedUpdate() // FixedUpdate is called independently of frame rate before Update, ideal for physics calculations.
    {
        if (!IsServer) return;
        currentState.FixedUpdateState();
    }

    public void Initialize(MazeGenerator.MazeData mazeObj) // Initialize is an externally called function to prepare for minotaur generation
    {
        maze = mazeObj;
        movement.Initialize(this);
        senses.Initialize(this);
    }

    public void ChangeState(MinotaurBaseState state) // ChangeState is a function called by the state classes to operate the state machine
    {
        currentState.ExitState();
        currentState = state;
        currentState.EnterState(this);
    }
}


// Initializing variables and data structures related to the aggro system (I think this will be its own storage class or stored in M_Senses to reduce bloat)
//[SerializeField] float aggroDecayRate = 5f;
//[SerializeField] float maxAggro = 100f;
//private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };