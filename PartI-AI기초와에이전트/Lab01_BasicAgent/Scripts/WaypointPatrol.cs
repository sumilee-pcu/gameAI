using UnityEngine;

/// <summary>
/// 정해진 경로를 순찰하는 에이전트
/// </summary>
public class WaypointPatrol : SimpleAgent
{
    [Header("Patrol Settings")]
    [Tooltip("순찰할 경로 지점들")]
    public Transform[] waypoints;

    [Tooltip("도착으로 판정하는 거리 (m)")]
    public float arrivalDistance = 0.5f;

    // 현재 목표 지점 인덱스
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

        // 현재 목표 지점
        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 목표 지점까지의 거리
        float distanceToTarget = Vector3.Distance(
            transform.position,
            targetWaypoint.position
        );

        // 도착했는가?
        if (distanceToTarget < arrivalDistance)
        {
            // 다음 지점으로 이동
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Debug.Log($"Waypoint {currentWaypointIndex} 도착!");
        }

        // 목표 지점으로 이동
        velocity = CalculateVelocityToTarget(targetWaypoint.position);
    }

    protected override void DrawDebugInfo()
    {
        base.DrawDebugInfo();

        // 경로 시각화
        if (waypoints != null && waypoints.Length > 1)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                int nextIndex = (i + 1) % waypoints.Length;
                Debug.DrawLine(
                    waypoints[i].position,
                    waypoints[nextIndex].position,
                    Color.cyan
                );
            }
        }

        // 현재 목표까지의 선
        if (waypoints.Length > 0)
        {
            Debug.DrawLine(
                transform.position,
                waypoints[currentWaypointIndex].position,
                Color.yellow
            );
        }
    }

    void OnDrawGizmos()
    {
        // 경로 지점을 Gizmos로 표시
        if (waypoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawWireSphere(waypoint.position, arrivalDistance);
                }
            }
        }
    }
}
