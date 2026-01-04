using UnityEngine;

/// <summary>
/// 시야각 기반 시각 센서
/// </summary>
public class VisionSensor : MonoBehaviour
{
    [Header("Vision Settings")]
    [Tooltip("시야 거리 (m)")]
    public float visionRange = 10f;

    [Tooltip("시야각 (degree)")]
    [Range(0, 360)]
    public float visionAngle = 120f;

    [Tooltip("감지 대상 레이어")]
    public LayerMask targetLayer;

    [Header("Debug")]
    public bool showVisionCone = true;

    /// <summary>
    /// 목표물이 시야 안에 있는지 확인
    /// </summary>
    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // 1. 거리 체크
        if (distanceToTarget > visionRange)
            return false;

        // 2. 각도 체크
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > visionAngle / 2f)
            return false;

        // 3. 장애물 체크 (Raycast)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, visionRange))
        {
            // 맞은 물체가 목표물인가?
            if (hit.transform == target)
            {
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmos()
    {
        if (!showVisionCone) return;

        // 시야 원뿔 그리기
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);

        Vector3 forward = transform.forward * visionRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + forward);
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // 시야 호 그리기
        Vector3 prevPoint = transform.position + rightBoundary;
        for (int i = 0; i <= 20; i++)
        {
            float angle = -visionAngle / 2f + (visionAngle / 20f) * i;
            Vector3 point = transform.position + Quaternion.Euler(0, angle, 0) * forward;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
