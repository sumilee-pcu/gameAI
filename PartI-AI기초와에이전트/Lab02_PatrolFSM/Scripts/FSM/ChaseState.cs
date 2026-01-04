using UnityEngine;

/// <summary>
/// 추적 상태
/// </summary>
public class ChaseState : State
{
    private Transform target;
    private float moveSpeed;
    private float chaseSpeedMultiplier = 1.5f; // 추적 시 더 빠르게

    public ChaseState(GameObject agent, Transform target, float moveSpeed)
        : base(agent, "Chase")
    {
        this.target = target;
        this.moveSpeed = moveSpeed;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log("플레이어 추적 시작!");
    }

    public override void OnUpdate()
    {
        if (target == null) return;

        Vector3 direction = (target.position - agent.transform.position).normalized;

        // 빠른 속도로 이동
        agent.transform.position += direction * moveSpeed * chaseSpeedMultiplier * Time.deltaTime;

        // 목표를 향해 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(
                agent.transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }
    }

    public override void OnDrawGizmos()
    {
        // 추적선 시각화
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(agent.transform.position, target.position);
        }
    }
}
