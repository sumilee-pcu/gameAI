using UnityEngine;

/// <summary>
/// 청각 센서 - 소음 감지
/// </summary>
public class HearingSensor : MonoBehaviour
{
    [Header("Hearing Settings")]
    [Tooltip("청각 범위 (m)")]
    public float hearingRange = 15f;

    [Tooltip("최소 감지 소음 크기")]
    public float minimumNoiseLevel = 10f;

    [Header("Debug")]
    public bool showHearingRange = true;

    /// <summary>
    /// 소음을 감지할 수 있는지 확인
    /// </summary>
    /// <param name="noisePosition">소음 발생 위치</param>
    /// <param name="noiseLevel">소음 크기</param>
    public bool CanHear(Vector3 noisePosition, float noiseLevel)
    {
        float distance = Vector3.Distance(transform.position, noisePosition);

        // 거리 감쇠 적용
        float attenuatedNoise = noiseLevel * (1f - (distance / hearingRange));

        // 최소 소음 레벨 이상이면 감지
        return attenuatedNoise >= minimumNoiseLevel && distance <= hearingRange;
    }

    void OnDrawGizmos()
    {
        if (!showHearingRange) return;

        // 청각 범위 표시
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
