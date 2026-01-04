using UnityEngine;

/// <summary>
/// 기본 에이전트 클래스 - 모든 AI의 기반
/// </summary>
public class SimpleAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    protected Rigidbody rb;
    protected Vector3 velocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}에 Rigidbody가 필요합니다!");
        }
    }

    void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        Debug.Log($"{gameObject.name} 초기화 완료");
    }

    void Update()
    {
        UpdateAI();

        if (showDebugInfo)
        {
            Debug.DrawRay(transform.position, velocity, Color.green);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    protected virtual void UpdateAI()
    {
        velocity = transform.forward * moveSpeed;
    }

    protected virtual void ApplyMovement()
    {
        if (rb != null)
        {
            Vector3 newPosition = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);

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

    protected Vector3 CalculateVelocityToTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        return direction * moveSpeed;
    }
}
