using UnityEngine;

/// <summary>
/// 공격 상태
/// </summary>
public class AttackState : State
{
    private Transform target;
    private float attackRange = 2f;
    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    public AttackState(GameObject agent, Transform target)
        : base(agent, "Attack")
    {
        this.target = target;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log("공격 시작!");
    }

    public override void OnUpdate()
    {
        if (target == null) return;

        // 목표를 향해 회전만 수행 (이동 안 함)
        Vector3 direction = (target.position - agent.transform.position).normalized;
        direction.y = 0; // 수평 회전만

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(
                agent.transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }

        // 공격 쿨다운 확인
        if (Time.time - lastAttackTime > attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    private void PerformAttack()
    {
        Debug.Log("공격 실행!");

        // 실제 게임에서는 여기서:
        // - 공격 애니메이션 재생
        // - 데미지 계산
        // - 이펙트 생성
        // 등을 수행
    }

    public override void OnDrawGizmos()
    {
        // 공격 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(agent.transform.position, attackRange);
    }
}
