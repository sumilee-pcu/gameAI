using UnityEngine;

public class ChaseState : State
{
    private Transform target;
    private float moveSpeed;
    private const float CHASE_SPEED_MULT = 1.5f;
    private const float ROTATION_SPEED = 10f;

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
        agent.transform.position += direction * moveSpeed * CHASE_SPEED_MULT * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetRot, ROTATION_SPEED * Time.deltaTime);
        }
    }

    public override void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(agent.transform.position, target.position);
        }
    }
}
