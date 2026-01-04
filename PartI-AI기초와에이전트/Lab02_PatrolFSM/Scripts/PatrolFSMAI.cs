using UnityEngine;

/// <summary>
/// FSM 기반 순찰 AI
/// </summary>
public class PatrolFSMAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("플레이어 Transform")]
    public Transform player;

    [Tooltip("순찰 경로")]
    public Transform[] waypoints;

    [Header("Settings")]
    [Tooltip("이동 속도")]
    public float moveSpeed = 3f;

    [Tooltip("플레이어 감지 거리")]
    public float detectionRange = 10f;

    [Tooltip("공격 거리")]
    public float attackRange = 2f;

    [Tooltip("추적 포기 거리")]
    public float loseTargetRange = 15f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private StateMachine stateMachine;

    void Start()
    {
        InitializeStateMachine();
    }

    void InitializeStateMachine()
    {
        stateMachine = new StateMachine();

        // 상태들 생성 및 추가
        PatrolState patrolState = new PatrolState(gameObject, waypoints, moveSpeed);
        ChaseState chaseState = new ChaseState(gameObject, player, moveSpeed);
        AttackState attackState = new AttackState(gameObject, player);

        stateMachine.AddState("Patrol", patrolState);
        stateMachine.AddState("Chase", chaseState);
        stateMachine.AddState("Attack", attackState);

        // 초기 상태 설정
        stateMachine.SetInitialState("Patrol");
    }

    void Update()
    {
        // 상태 전환 조건 확인
        CheckStateTransitions();

        // 현재 상태 업데이트
        stateMachine.Update();

        // 디버그 UI 업데이트
        if (showDebugInfo && DebugUI.Instance != null)
        {
            DebugUI.Instance.UpdateDebugInfo(
                "Enemy State",
                stateMachine.CurrentStateName
            );
        }
    }

    void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 상태 전환 조건 검사
    /// </summary>
    void CheckStateTransitions()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        string currentState = stateMachine.CurrentStateName;

        switch (currentState)
        {
            case "Patrol":
                // 플레이어가 감지 범위 안에 들어오면 추적
                if (distanceToPlayer < detectionRange)
                {
                    stateMachine.ChangeState("Chase");
                }
                break;

            case "Chase":
                // 공격 거리 안에 들어오면 공격
                if (distanceToPlayer < attackRange)
                {
                    stateMachine.ChangeState("Attack");
                }
                // 너무 멀어지면 순찰로 복귀
                else if (distanceToPlayer > loseTargetRange)
                {
                    stateMachine.ChangeState("Patrol");
                }
                break;

            case "Attack":
                // 공격 거리를 벗어나면 다시 추적
                if (distanceToPlayer > attackRange)
                {
                    stateMachine.ChangeState("Chase");
                }
                break;
        }
    }

    void OnDrawGizmos()
    {
        // FSM 상태 시각화
        if (stateMachine != null)
        {
            stateMachine.DrawGizmos();
        }

        // 감지 범위
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 공격 범위
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
