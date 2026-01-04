using UnityEngine;

/// <summary>
/// 마우스 클릭 지점으로 이동하는 에이전트
/// </summary>
public class MouseClickAgent : SimpleAgent
{
    [Header("Click Settings")]
    [Tooltip("목표 지점 표시용 프리팹")]
    public GameObject targetMarkerPrefab;

    [Tooltip("도착 거리")]
    public float arrivalDistance = 0.5f;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    private GameObject currentMarker;

    protected override void Initialize()
    {
        base.Initialize();
        targetPosition = transform.position;
    }

    void Update()
    {
        // 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0)) // 왼쪽 클릭
        {
            HandleMouseClick();
        }

        base.Update();
    }

    protected override void UpdateAI()
    {
        if (!hasTarget)
        {
            velocity = Vector3.zero;
            return;
        }

        // 목표까지의 거리
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < arrivalDistance)
        {
            // 도착!
            hasTarget = false;
            velocity = Vector3.zero;

            if (currentMarker != null)
            {
                Destroy(currentMarker);
            }

            Debug.Log("목표 지점 도착!");
        }
        else
        {
            // 이동
            velocity = CalculateVelocityToTarget(targetPosition);
        }
    }

    /// <summary>
    /// 마우스 클릭 처리
    /// </summary>
    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Plane 레이어만 검사 (레이어 설정 필요)
        if (Physics.Raycast(ray, out hit, 100f))
        {
            targetPosition = hit.point;
            hasTarget = true;

            Debug.Log($"새 목표: {targetPosition}");

            // 마커 생성
            if (currentMarker != null)
            {
                Destroy(currentMarker);
            }

            if (targetMarkerPrefab != null)
            {
                currentMarker = Instantiate(
                    targetMarkerPrefab,
                    targetPosition,
                    Quaternion.identity
                );
            }
        }
    }

    protected override void DrawDebugInfo()
    {
        base.DrawDebugInfo();

        if (hasTarget)
        {
            // 목표 지점까지의 선
            Debug.DrawLine(transform.position, targetPosition, Color.magenta);

            // 목표 지점 표시
            Debug.DrawLine(
                targetPosition + Vector3.up * 2f,
                targetPosition,
                Color.red
            );
        }
    }
}
