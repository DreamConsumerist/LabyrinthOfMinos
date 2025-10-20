using System.Collections.Generic;
using UnityEngine;

public class MinotaurMovement : MonoBehaviour
{
    private MinotaurBehaviorController minotaur;

    private Vector2Int targetPos;
    private Vector2Int prevTargetPos = new Vector2Int(-1, -1);
    private Vector2Int minotaurPos2D;

    private List<Vector2Int> currPath;

    public bool isInitialized { get; private set; } = false;

    // --------------------------
    // Initialization
    // --------------------------
    public void Initialize(MinotaurBehaviorController behaviorController)
    {
        minotaur = behaviorController;
        isInitialized = true;

        UpdateMinotaur2DPosition();
    }

    private void UpdateMinotaur2DPosition()
    {
        if (minotaur == null || minotaur.rb == null || minotaur.maze == null) return;

        minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(minotaur.rb.position.x / minotaur.maze.tileSize),
            Mathf.RoundToInt(minotaur.rb.position.z / minotaur.maze.tileSize)
        );
    }

    private void Update()
    {
        if (!isInitialized) return;
        UpdateMinotaur2DPosition();
    }

    // --------------------------
    // Public API
    // --------------------------
    public void UpdateTarget(Vector2Int target)
    {
        targetPos = target;
    }

    public void MoveToTarget(float moveSpeed, float rotationSpeed)
    {
        if (!isInitialized || minotaur.maze == null) return;

        RecalculatePath();

        if (currPath == null || currPath.Count == 0) return;

        Vector3 nextPoint = GetNextPathPoint();

        // Rotate first toward the next node
        RotateTowards(nextPoint, rotationSpeed);

        // Move forward only in the direction currently facing
        MoveForward(moveSpeed);

        AdvancePathNode();
    }

    public void FollowPatrolRoute(List<Vector2Int> patrolRoute, float moveSpeed, float rotationSpeed)
    {
        if (patrolRoute == null || patrolRoute.Count == 0) return;

        Vector3 nextPoint = new Vector3(
            patrolRoute[0].x * minotaur.maze.tileSize,
            0,
            patrolRoute[0].y * minotaur.maze.tileSize
        );

        RotateTowards(nextPoint, rotationSpeed);
        MoveForward(moveSpeed);

        if (Vector3.Distance(minotaur.rb.position, nextPoint) < 0.5f)
        {
            Vector2Int temp = patrolRoute[0];
            patrolRoute.RemoveAt(0);
            patrolRoute.Add(temp);
        }
    }

    // --------------------------
    // Path Management
    // --------------------------
    private void RecalculatePath()
    {
        if (targetPos != prevTargetPos)
        {
            var newPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, minotaur.maze.open);

            if (newPath != null && newPath.Count > 0)
            {
                if (newPath[0] == minotaurPos2D) newPath.RemoveAt(0);
                currPath = newPath;
            }
            else
            {
                currPath = new List<Vector2Int>();
            }

            prevTargetPos = targetPos;
        }
    }

    private Vector3 GetNextPathPoint()
    {
        if (currPath == null || currPath.Count == 0)
            return minotaur.rb.position;

        return new Vector3(
            currPath[0].x * minotaur.maze.tileSize,
            0,
            currPath[0].y * minotaur.maze.tileSize
        );
    }

    private void AdvancePathNode()
    {
        if (currPath != null && currPath.Count > 0 &&
            Vector3.Distance(minotaur.rb.position, GetNextPathPoint()) < 0.5f)
        {
            currPath.RemoveAt(0);
        }
    }

    // --------------------------
    // Movement Helpers
    // --------------------------
    private void MoveForward(float moveSpeed)
    {
        Vector3 forward = minotaur.rb.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 move = forward * moveSpeed * Time.fixedDeltaTime;
        minotaur.rb.MovePosition(minotaur.rb.position + move);
    }

    private void RotateTowards(Vector3 targetPos, float rotationSpeed)
    {
        Vector3 direction = (targetPos - minotaur.rb.position).normalized;
        direction.y = 0f;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        minotaur.rb.MoveRotation(Quaternion.RotateTowards(minotaur.rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    // --------------------------
    // Gizmos
    // --------------------------
    private void OnDrawGizmos()
    {
        if (currPath == null || minotaur == null || minotaur.maze == null) return;

        Gizmos.color = Color.green;
        float s = minotaur.maze.tileSize;

        for (int i = 0; i < currPath.Count - 1; i++)
        {
            Vector3 from = new Vector3(currPath[i].x * s, 0.5f, currPath[i].y * s);
            Vector3 to = new Vector3(currPath[i + 1].x * s, 0.5f, currPath[i + 1].y * s);
            Gizmos.DrawLine(from, to);
        }
    }
}
