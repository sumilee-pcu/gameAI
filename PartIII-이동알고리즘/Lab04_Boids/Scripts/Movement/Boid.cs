using UnityEngine;

// Boid 에이전트 - 분리(Separation), 정렬(Alignment), 응집(Cohesion) 규칙
public class Boid : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 3f;

    [Header("Boid Rules")]
    [Range(0, 5)] public float separationWeight = 1.5f;
    [Range(0, 5)] public float alignmentWeight = 1.0f;
    [Range(0, 5)] public float cohesionWeight = 1.0f;

    [Header("Perception")]
    public float perceptionRadius = 2.5f;

    private Vector3 velocity;
    private Vector3 acceleration;
    private FlockManager flockManager;

    public Vector3 Velocity => velocity;
    public Vector3 Position => transform.position;

    void Start()
    {
        // 랜덤 초기 속도
        velocity = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * maxSpeed * 0.5f;
        flockManager = FindObjectOfType<FlockManager>();
    }

    void Update()
    {
        ApplyFlockingBehavior();

        // 속도 및 위치 업데이트
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        transform.position += velocity * Time.deltaTime;

        // 이동 방향으로 회전
        if (velocity.magnitude > 0.1f)
            transform.forward = velocity.normalized;

        acceleration = Vector3.zero;
    }

    void ApplyFlockingBehavior()
    {
        if (flockManager == null) return;

        Boid[] neighbors = flockManager.GetNeighbors(this, perceptionRadius);
        if (neighbors.Length == 0) return;

        // 세 가지 규칙 적용
        ApplyForce(Separation(neighbors) * separationWeight);
        ApplyForce(Alignment(neighbors) * alignmentWeight);
        ApplyForce(Cohesion(neighbors) * cohesionWeight);
        ApplyForce(AvoidBounds());
    }

    // 분리(Separation): 이웃과 거리 유지
    Vector3 Separation(Boid[] neighbors)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (Boid other in neighbors)
        {
            Vector3 diff = Position - other.Position;
            float distance = diff.magnitude;

            if (distance > 0 && distance < perceptionRadius)
            {
                diff.Normalize();
                diff /= distance;
                steer += diff;
                count++;
            }
        }

        if (count > 0)
        {
            steer /= count;
            steer.Normalize();
            steer = steer * maxSpeed - velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
        }

        return steer;
    }

    // 정렬(Alignment): 이웃과 같은 방향으로 이동
    Vector3 Alignment(Boid[] neighbors)
    {
        Vector3 avgVelocity = Vector3.zero;
        foreach (Boid other in neighbors)
            avgVelocity += other.Velocity;

        avgVelocity /= neighbors.Length;
        avgVelocity.Normalize();
        avgVelocity = avgVelocity * maxSpeed - velocity;
        return Vector3.ClampMagnitude(avgVelocity, maxForce);
    }

    // 응집(Cohesion): 이웃의 평균 위치로 이동
    Vector3 Cohesion(Boid[] neighbors)
    {
        Vector3 avgPosition = Vector3.zero;
        foreach (Boid other in neighbors)
            avgPosition += other.Position;

        avgPosition /= neighbors.Length;
        return Seek(avgPosition);
    }

    // 목표 지점으로 향하는 힘
    Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - Position).normalized * maxSpeed;
        return Vector3.ClampMagnitude(desired - velocity, maxForce);
    }

    // 경계 회피: 플록 영역을 벗어나면 중심으로 돌아옴
    Vector3 AvoidBounds()
    {
        if (flockManager == null) return Vector3.zero;

        float distance = Vector3.Distance(Position, flockManager.flockCenter);
        if (distance > flockManager.flockRadius - 2f)
            return Seek(flockManager.flockCenter) * 2f;

        return Vector3.zero;
    }

    void ApplyForce(Vector3 force)
    {
        acceleration += force;
    }
}
