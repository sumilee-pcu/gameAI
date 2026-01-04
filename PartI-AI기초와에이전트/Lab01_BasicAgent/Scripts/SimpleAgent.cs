using UnityEngine;

/// <summary>
/// 기본 에이전트 클래스 - 모든 AI의 기반
/// </summary>
public class SimpleAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("에이전트의 이동 속도 (m/s)")]
    public float moveSpeed = 5f;

    [Tooltip("에이전트의 회전 속도 (degree/s)")]
    public float rotationSpeed = 120f;

    [Header("Debug")]
    [Tooltip("디버그 정보 표시 여부")]
    public bool showDebugInfo = true;

    // 컴포넌트 참조
    private Rigidbody rb;

    // 현재 이동 방향
    protected Vector3 velocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}에 Rigidbody 컴포넌트가 필요합니다!");
        }
    }

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 에이전트 초기화 (상속받은 클래스에서 오버라이드 가능)
    /// </summary>
    protected virtual void Initialize()
    {
        Debug.Log($"{gameObject.name} 에이전트 초기화 완료");
    }

    void Update()
    {
        // AI 로직 업데이트
        UpdateAI();

        // 디버그 정보 표시
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    void FixedUpdate()
    {
        // 물리 기반 이동
        ApplyMovement();
    }

    /// <summary>
    /// AI 의사결정 로직 (하위 클래스에서 구현)
    /// </summary>
    protected virtual void UpdateAI()
    {
        // 기본 구현: 앞으로 이동
        velocity = transform.forward * moveSpeed;
    }

    /// <summary>
    /// 물리 엔진을 통한 실제 이동 적용
    /// </summary>
    protected virtual void ApplyMovement()
    {
        if (rb != null)
        {
            // Rigidbody를 사용한 이동 (물리 충돌 고려)
            Vector3 newPosition = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);

            // 이동 방향으로 회전
            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity);
                rb.rotation = Quaternion.RotateTowards(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }
    }

    /// <summary>
    /// 디버그 시각화
    /// </summary>
    protected virtual void DrawDebugInfo()
    {
        // 속도 벡터 표시
        Debug.DrawRay(transform.position, velocity, Color.green);

        // 전방 방향 표시
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);
    }

    /// <summary>
    /// 목표 위치로 이동하는 속도 계산
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    /// <returns>이동 속도 벡터</returns>
    protected Vector3 CalculateVelocityToTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        return direction * moveSpeed;
    }

    /// <summary>
    /// Gizmos를 사용한 에디터 시각화
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 에이전트 위치를 구체로 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
