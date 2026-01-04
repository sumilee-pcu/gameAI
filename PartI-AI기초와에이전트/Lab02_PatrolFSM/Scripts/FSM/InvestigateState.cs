using UnityEngine;

/// <summary>
/// 조사 상태 - 의심스러운 소음 위치로 이동
/// </summary>
public class InvestigateState : State
{
    private Vector3 investigationPoint;
    private float moveSpeed;
    private float arrivalDistance = 1.5f;
    private bool hasArrived = false;

    public InvestigateState(GameObject agent, float moveSpeed)
        : base(agent, "Investigate")
    {
        this.moveSpeed = moveSpeed;
    }

    /// <summary>
    /// 조사할 위치 설정
    /// </summary>
    public void SetInvestigationPoint(Vector3 point)
    {
        investigationPoint = point;
        hasArrived = false;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log($"소음 위치 조사: {investigationPoint}");
    }

    public override void OnUpdate()
    {
        Vector3 direction = (investigationPoint - agent.transform.position).normalized;
        float distance = Vector3.Distance(agent.transform.position, investigationPoint);

        if (distance > arrivalDistance && !hasArrived)
        {
            // 이동
            agent.transform.position += direction * moveSpeed * Time.deltaTime;

            // 회전
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                agent.transform.rotation = Quaternion.Slerp(
                    agent.transform.rotation,
                    targetRotation,
                    5f * Time.deltaTime
                );
            }
        }
        else
        {
            hasArrived = true;
            // 도착 후 잠시 대기 (실제로는 타이머 사용)
        }
    }

    /// <summary>
    /// 조사 완료 여부
    /// </summary>
    public bool HasFinishedInvestigation()
    {
        return hasArrived;
    }

    public override void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(agent.transform.position, investigationPoint);
        Gizmos.DrawWireSphere(investigationPoint, arrivalDistance);
    }
}
