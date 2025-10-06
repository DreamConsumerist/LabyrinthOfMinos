using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MinotaurMovement))]
public class MinotaurBehaviorController : MonoBehaviour
{
    // Initializing maze data and passing reference to this component to MinotaurMovement
    [SerializeField] MinotaurMovement movement;
    public MazeGenerator.MazeData maze;
    PlayerData player;

    // Initializing variables and data structures related to the aggro system
    [SerializeField] float aggroDecayRate = 5f;
    [SerializeField] float maxAggro = 100f;
    private Dictionary<PlayerData, float> playerAggro = new Dictionary<PlayerData, float> { };

    private void Awake()
    {
        if (!movement) movement = GetComponent<MinotaurMovement>();
    }

    public void Initialize(MazeGenerator.MazeData mazeObj)
    {
        maze = mazeObj;
        movement.Initialize(this);
    }

    void Start()
    {
        player = FindObjectOfType<PlayerData>();
    }

    // Update is called once per frame
    void Update()
    {
        movement.UpdateTarget(new Vector2Int(Mathf.RoundToInt(player.transform.position.x / maze.tileSize), Mathf.RoundToInt(player.transform.position.z / maze.tileSize)));
    }
}
