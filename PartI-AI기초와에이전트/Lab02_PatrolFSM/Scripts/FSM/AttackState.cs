using UnityEngine;

public class AttackState : State
{
    private Transform target;
    private const float ATTACK_RANGE = 2f;
    private const float ATTACK_COOLDOWN = 1f;
    private const float ROTATION_SPEED = 10f;
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

        Vector3 direction = (target.position - agent.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetRot, ROTATION_SPEED * Time.deltaTime);
        }

        if (Time.time - lastAttackTime > ATTACK_COOLDOWN)
        {
            Debug.Log("공격 실행!");
            lastAttackTime = Time.time;
        }
    }

    public override void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(agent.transform.position, ATTACK_RANGE);
    }
}
