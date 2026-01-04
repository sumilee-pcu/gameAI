using UnityEngine;

public class PatrolFSMAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] waypoints;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float loseTargetRange = 15f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private StateMachine stateMachine;

    void Start()
    {
        stateMachine = new StateMachine();
        stateMachine.AddState("Patrol", new PatrolState(gameObject, waypoints, moveSpeed));
        stateMachine.AddState("Chase", new ChaseState(gameObject, player, moveSpeed));
        stateMachine.AddState("Attack", new AttackState(gameObject, player));
        stateMachine.SetInitialState("Patrol");
    }

    void Update()
    {
        CheckStateTransitions();
        stateMachine.Update();

        if (showDebugInfo && DebugUI.Instance != null)
            DebugUI.Instance.UpdateDebugInfo("Enemy State", stateMachine.CurrentStateName);
    }

    void FixedUpdate() => stateMachine.FixedUpdate();

    void CheckStateTransitions()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        string state = stateMachine.CurrentStateName;

        switch (state)
        {
            case "Patrol":
                if (dist < detectionRange)
                    stateMachine.ChangeState("Chase");
                break;
            case "Chase":
                if (dist < attackRange)
                    stateMachine.ChangeState("Attack");
                else if (dist > loseTargetRange)
                    stateMachine.ChangeState("Patrol");
                break;
            case "Attack":
                if (dist > attackRange)
                    stateMachine.ChangeState("Chase");
                break;
        }
    }

    void OnDrawGizmos()
    {
        stateMachine?.DrawGizmos();

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
