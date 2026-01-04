using UnityEngine;

/// <summary>
/// Boid 에이전트 - 군집 행동
/// </summary>
public class Boid : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 3f;

    [Header("Boid Rules")]
    [Range(0, 5)]
    public float separationWeight = 1.5f;

    [Range(0, 5)]
    public float alignmentWeight = 1.0f;

    [Range(0, 5)]
    public float cohesionWeight = 1.0f;

    [Header("Perception")]
    public float perceptionRadius = 2.5f;

    // 현재 속도
    private Vector3 velocity;

    // 가속도
    private Vector3 acceleration;

    // 플록 매니저 참조
    private FlockManager flockManager;

    public Vector3 Velocity => velocity;
    public Vector3 Position => transform.position;

    void Start()
    {
        // 랜덤 초기 속도
        velocity = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized * maxSpeed * 0.5f;

        flockManager = FindObjectOfType<FlockManager>();
    }

    void Update()
    {
        // Boid 규칙 적용
        ApplyFlockingBehavior();

        // 속도 업데이트
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // 위치 업데이트
        transform.position += velocity * Time.deltaTime;

        // 회전 업데이트
        if (velocity.magnitude > 0.1f)
        {
            transform.forward = velocity.normalized;
        }

        // 가속도 초기화
        acceleration = Vector3.zero;
    }

    /// <summary>
    /// 군집 행동 규칙 적용
    /// </summary>
    void ApplyFlockingBehavior()
    {
        if (flockManager == null) return;

        // 주변 Boid 탐색
        Boid[] neighbors = flockManager.GetNeighbors(this, perceptionRadius);

        if (neighbors.Length > 0)
        {
            Vector3 separation = Separation(neighbors);
            Vector3 alignment = Alignment(neighbors);
            Vector3 cohesion = Cohesion(neighbors);

            // 가중치 적용
            separation *= separationWeight;
            alignment *= alignmentWeight;
            cohesion *= cohesionWeight;

            // 힘 합산
            ApplyForce(separation);
            ApplyForce(alignment);
            ApplyForce(cohesion);
        }

        // 경계 회피
        ApplyForce(AvoidBounds());
    }

    /// <summary>
    /// 분리 (Separation): 가까운 이웃과 거리 유지
    /// </summary>
    Vector3 Separation(Boid[] neighbors)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (Boid other in neighbors)
        {
            float distance = Vector3.Distance(Position, other.Position);

            if (distance > 0 && distance < perceptionRadius)
            {
                // 거리가 가까울수록 강한 반발력
                Vector3 diff = Position - other.Position;
                diff.Normalize();
                diff /= distance; // 거리 가중치
                steer += diff;
                count++;
            }
        }

        if (count > 0)
        {
            steer /= count;
            steer.Normalize();
            steer *= maxSpeed;
            steer -= velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
        }

        return steer;
    }

    /// <summary>
    /// 정렬 (Alignment): 이웃과 같은 방향으로 이동
    /// </summary>
    Vector3 Alignment(Boid[] neighbors)
    {
        Vector3 avgVelocity = Vector3.zero;
        int count = 0;

        foreach (Boid other in neighbors)
        {
            avgVelocity += other.Velocity;
            count++;
        }

        if (count > 0)
        {
            avgVelocity /= count;
            avgVelocity.Normalize();
            avgVelocity *= maxSpeed;

            Vector3 steer = avgVelocity - velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
            return steer;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 응집 (Cohesion): 이웃의 평균 위치로 이동
    /// </summary>
    Vector3 Cohesion(Boid[] neighbors)
    {
        Vector3 avgPosition = Vector3.zero;
        int count = 0;

        foreach (Boid other in neighbors)
        {
            avgPosition += other.Position;
            count++;
        }

        if (count > 0)
        {
            avgPosition /= count;
            return Seek(avgPosition);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 목표 지점으로 향하는 힘
    /// </summary>
    Vector3 Seek(Vector3 target)
    {
        Vector3 desired = target - Position;
        desired.Normalize();
        desired *= maxSpeed;

        Vector3 steer = desired - velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        return steer;
    }

    /// <summary>
    /// 경계 회피
    /// </summary>
    Vector3 AvoidBounds()
    {
        Vector3 steer = Vector3.zero;
        float margin = 2f;

        if (flockManager != null)
        {
            Vector3 center = flockManager.flockCenter;
            float radius = flockManager.flockRadius;

            float distance = Vector3.Distance(Position, center);

            if (distance > radius - margin)
            {
                // 중심으로 되돌리는 힘
                steer = Seek(center);
                steer *= 2f; // 강한 힘
            }
        }

        return steer;
    }

    /// <summary>
    /// 힘 적용
    /// </summary>
    void ApplyForce(Vector3 force)
    {
        acceleration += force;
    }

    void OnDrawGizmosSelected()
    {
        // 인식 범위 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        // 속도 벡터 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, velocity);
    }
}
