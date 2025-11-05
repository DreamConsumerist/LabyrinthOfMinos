using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.DualShock.LowLevel;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : MonoBehaviour
{
    // Initialize variables to store references to objects and data
    public Rigidbody rb;
    public MazeGenerator.MazeData maze;
    public PlayerData player;
    [SerializeField] public GameObject indicator;

    // Initialize variables to store instances and outputs of helper classes
    public MinotaurMovement movement;
    public MinotaurSenses senses;
    public MinotaurSenses.SenseReport currentKnowledge;

    // Initialize variables to store instances of states
    MinotaurBaseState currentState;
    public MinotaurChaseState ChaseState = new MinotaurChaseState();
    public MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    public MinotaurPatrolState PatrolState = new MinotaurPatrolState();

    private void Awake() // Awake is called when 
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
        if (!senses) senses = GetComponent<MinotaurSenses>();
        rb = GetComponent<Rigidbody>();
    }
    void Start() // Start is called when 
    {
        currentState = PatrolState;
        currentState.EnterState(this);
        player = UnityEngine.Object.FindAnyObjectByType<PlayerData>();
    }

    void Update() // Update is called once per frame
    {
        currentKnowledge = senses.SensoryUpdate();
        currentState.UpdateState(currentKnowledge);
    }

    private void FixedUpdate() // FixedUpdate is called once per frame after Update has completed, except on initialization where it goes first.
    {
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

    private void OnDrawGizmos()
    {
        if (currentState != null)
        {
            currentState.DrawGizmos(); // If problems occur, may be because controller isn't initialized.
        }
    }
}


// Initializing variables and data structures related to the aggro system (I think this will be its own storage class or stored in M_Senses to reduce bloat)
//[SerializeField] float aggroDecayRate = 5f;
//[SerializeField] float maxAggro = 100f;
//private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };