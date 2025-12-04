using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(MinotaurMovement))]
[RequireComponent(typeof(MinotaurSenses))]
[RequireComponent(typeof(MinotaurParameters))]
[RequireComponent(typeof(MinotaurAggroHandler))]
public class MinotaurBehaviorController : NetworkBehaviour
{
    public static MinotaurBehaviorController Instance;

    // Initialize variables to store references to objects and data
    public Rigidbody rb;
    public MazeGenerator.MazeData maze;

    // Aggro targeting values
    public Dictionary<GameObject, float> aggroValues = new();
    public GameObject currentTarget = null;

    // Initialize variables to store instances and outputs of helper classes
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public MinotaurMovement movement;
    public MinotaurParameters parameters;
    public MinotaurAggroHandler aggro;

    public AudioSource walkSource;
    public AudioSource roarSource;
    public AudioClip[] walkSounds;
    public AudioClip roarSound;

    // Initialize variables to store instances of states
    MinotaurBaseState currentState;
    public readonly MinotaurChaseState ChaseState = new MinotaurChaseState();
    public readonly MinotaurKillsPlayerState KillsPlayerState = new MinotaurKillsPlayerState();
    public readonly MinotaurPatrolState PatrolState = new MinotaurPatrolState();
    public readonly MinotaurInvestigateState InvestigateState = new MinotaurInvestigateState();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<MinotaurMovement>();
        parameters = GetComponent<MinotaurParameters>();

        if (IsServer)
        {
            currentState = PatrolState;
            currentState.EnterState(this);
            movement.Initialize(this);
            var players = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p != null) { aggroValues.Add(p.gameObject, 0); Debug.Log("Found player " + p.name); }
            }
        }
    }
    private void OnDisable()
    {
        if (!IsServer) return;
        PlayerEvents.OnPlayerSpawned -= AddPlayerToList;
        PlayerEvents.OnPlayerExit -= RemovePlayerFromList;
    }

    private void Awake() // Awake is called when 
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<MinotaurMovement>();
        aggro = GetComponent<MinotaurAggroHandler>();
        parameters = GetComponent<MinotaurParameters>();
        Instance = this;
        if (!IsServer) return;
        PlayerEvents.OnPlayerSpawned += AddPlayerToList;
        PlayerEvents.OnPlayerExit += RemovePlayerFromList;
    }

    void Update() // Update is called once per frame
    {
        if (!IsServer) return;
        aggro.AggroUpdate();
        currentState.UpdateState();
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
        aggro.Initialize(this);
    }

    public void ChangeState(MinotaurBaseState state) // ChangeState is a function called by the state classes to operate the state machine
    {
        currentState.ExitState();
        currentState = state;
        currentState.EnterState(this);
    }

    public MinotaurBaseState GetCurrState()
    {
        return currentState;
    }

    public Vector2Int GetMinotaurPos2D()
    {
        Vector2Int minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / maze.tileSize),
            Mathf.RoundToInt(transform.position.z / maze.tileSize)); ;
        return minotaurPos2D;
    }

    private void AddPlayerToList(GameObject player)
    {
        if (!aggroValues.ContainsKey(player))
        {
            aggroValues.Add(player, 0);
            Debug.Log("New player in scene, adding to list: " + player.name);
        }
    }

    // This is not currently working although this is supposed to be the proper place for this code
    private void RemovePlayerFromList (GameObject player)
    {
        Debug.Log("Setting " + player.name + " aggro to 0");
        aggroValues[player] = 0;
        if (currentTarget == player)
        {
            currentTarget = null;
        }
        aggroValues.Remove(player);
        Debug.Log("Removing " + player.name + " from list");
    }

    // Slapdash fix here
    private void OnTriggerEnter(Collider obj)
    {
        if (!IsServer) { return; }
        Debug.Log("Ran into " + obj.gameObject.name);
        if (obj.gameObject.CompareTag("Player"))
        {
            Debug.Log("Setting " + obj.gameObject.name + " aggro to 0");
            aggroValues[obj.gameObject] = 0;
            if (currentTarget == obj.gameObject)
            {
                currentTarget = null;
            }
            aggroValues.Remove(obj.gameObject);
            Debug.Log("Removing " + obj.gameObject.name + " from list");
        }
    }
}