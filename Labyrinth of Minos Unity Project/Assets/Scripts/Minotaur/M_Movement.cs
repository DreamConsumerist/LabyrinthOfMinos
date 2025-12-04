using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MinotaurMovement : MonoBehaviour
{
    private MinotaurBehaviorController controller;

    private Vector2Int targetPos;
    private Vector2Int prevTargetPos = new Vector2Int(-1, -1);
    private Vector2Int minotaurPos2D;
    private bool isStaticRotate;

    private List<Vector2Int> currPath;

    public bool isInitialized { get; private set; } = false;

    // --------------------------
    // Initialization
    // --------------------------
    public void Initialize(MinotaurBehaviorController controllerRef)
    {
        controller = controllerRef;
        isInitialized = true;

        minotaurPos2D = controller.GetMinotaurPos2D();
    }

    private void Update()
    {
        if (!isInitialized) return;
        minotaurPos2D = controller.GetMinotaurPos2D();
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
        if (!isInitialized || controller.maze == null) return;
        Vector3 chasePos = Vector3.negativeInfinity;
        
        if (controller.GetCurrState() == controller.ChaseState)
        {
            chasePos = controller.currentTarget.transform.position;

            // if close enough, ignore A* and chase directly
            if ((controller.currentTarget != null) && (Vector3.Distance(controller.rb.position, chasePos) <= controller.parameters.pointRadius + .5))
            {
                DirectChase(chasePos, moveSpeed, rotationSpeed);
                return;
            }
        }

        RecalculatePath();

        if (currPath == null || currPath.Count == 0) return;

        Vector3 nextPoint = GetNextPathPoint();

        // Rotate first toward the next node
        RotateTowards(nextPoint, rotationSpeed);

        if (!isStaticRotate)
        {
            // Move forward only in the direction currently facing, only if rotating less than the staticRotateAngle
            MoveForward(moveSpeed);
        }

        AdvancePathNode();
    }

    private void DirectChase(Vector3 targetWorldPos, float moveSpeed, float rotationSpeed)
    {
        Vector3 direction = (targetWorldPos - controller.rb.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(direction);
        controller.rb.MoveRotation(
            Quaternion.RotateTowards(controller.rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime)
        );

        Vector3 move = direction * moveSpeed * Time.fixedDeltaTime;
        controller.rb.MovePosition(controller.rb.position + move);
    }

    public void FollowPatrolRoute(List<Vector2Int> patrolRoute, float moveSpeed, float rotationSpeed)
    {
        if (patrolRoute == null || patrolRoute.Count == 0) return;

        Vector3 nextPoint = new Vector3(
            patrolRoute[0].x * controller.maze.tileSize,
            0,
            patrolRoute[0].y * controller.maze.tileSize
        );

        RotateTowards(nextPoint, rotationSpeed);
        
        if (!isStaticRotate)
        {
            MoveForward(moveSpeed);
        }

        if (Vector3.Distance(controller.rb.position, nextPoint) < controller.parameters.pointRadius)
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
            var newPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, controller.maze.open);

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
            return controller.rb.position;

        return new Vector3(
            currPath[0].x * controller.maze.tileSize,
            0,
            currPath[0].y * controller.maze.tileSize
        );
    }

    private void AdvancePathNode()
    {
        if (currPath != null && currPath.Count > 0 &&
            Vector3.Distance(controller.rb.position, GetNextPathPoint()) < controller.parameters.pointRadius)
        {
            currPath.RemoveAt(0);
        }
    }

    // --------------------------
    // Movement Helpers
    // --------------------------
    private void MoveForward(float moveSpeed)
    {
        Vector3 forward = controller.rb.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 move = forward * moveSpeed * Time.fixedDeltaTime;
        controller.rb.MovePosition(controller.rb.position + move);
    }

    private void RotateTowards(Vector3 targetPos, float rotationSpeed)
    {
        Vector3 direction = (targetPos - controller.rb.position).normalized;
        direction.y = 0f;
        if (direction == Vector3.zero) { return; }

        if (direction == controller.rb.transform.forward)
        {
            isStaticRotate = false;
        }
        float angle = Vector3.Angle(direction, controller.transform.forward);
        if (angle > controller.parameters.staticRotateAngle)
        {
            isStaticRotate = true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        controller.rb.MoveRotation(Quaternion.RotateTowards(controller.rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    // --------------------------
    // Gizmos
    // --------------------------
    private void OnDrawGizmos()
    {
        if (!(currPath == null || controller == null || controller.maze == null))
        {
            Gizmos.color = Color.green;
            float s = controller.maze.tileSize;

            for (int i = 0; i < currPath.Count - 1; i++)
            {
                Vector3 from = new Vector3(currPath[i].x * s, 0.5f, currPath[i].y * s);
                Vector3 to = new Vector3(currPath[i + 1].x * s, 0.5f, currPath[i + 1].y * s);
                Gizmos.DrawLine(from, to);
            }
        }

        if (!(controller.PatrolState.patrolPath == null || controller.maze == null))
        {
            Gizmos.color = Color.yellow; // a different color
            float s = controller.maze.tileSize;

            var path = controller.PatrolState.patrolPath;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 from = new Vector3(path[i].x * s, 0.5f, path[i].y * s);
                Vector3 to = new Vector3(path[i + 1].x * s, 0.5f, path[i + 1].y * s);
                Gizmos.DrawLine(from, to);
            }
        }

    }
}
