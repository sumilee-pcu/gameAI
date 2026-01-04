using UnityEngine;

/// <summary>
/// 순찰 상태
/// </summary>
public class PatrolState : State
{
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private float moveSpeed;
    private float arrivalDistance = 1f;

    public PatrolState(GameObject agent, Transform[] waypoints, float moveSpeed)
        : base(agent, "Patrol")
    {
        this.waypoints = waypoints;
        this.moveSpeed = moveSpeed;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        // 순찰 시작
    }

    public override void OnUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - agent.transform.position).normalized;

        // 이동
        agent.transform.position += direction * moveSpeed * Time.deltaTime;

        // 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(
                agent.transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }

        // 도착 확인
        float distance = Vector3.Distance(agent.transform.position, targetWaypoint.position);
        if (distance < arrivalDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    public override void OnDrawGizmos()
    {
        // 경로 시각화
        Gizmos.color = Color.cyan;
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.DrawLine(
                agent.transform.position,
                waypoints[currentWaypointIndex].position
            );
        }
    }
}
