using UnityEngine;

/// <summary>
/// 정해진 경로를 순찰하는 에이전트
/// </summary>
public class WaypointPatrol : SimpleAgent
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float arrivalDistance = 0.5f;

    private int currentWaypointIndex = 0;

    protected override void Initialize()
    {
        base.Initialize();

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{gameObject.name}: 순찰 경로가 설정되지 않았습니다!");
            enabled = false;
        }
    }

    protected override void UpdateAI()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        float distanceToTarget = Vector3.Distance(transform.position, targetWaypoint.position);

        if (distanceToTarget < arrivalDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        velocity = CalculateVelocityToTarget(targetWaypoint.position);
    }
}
