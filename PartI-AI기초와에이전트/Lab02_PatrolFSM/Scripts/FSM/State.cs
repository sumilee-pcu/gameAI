using UnityEngine;

/// <summary>
/// FSM 상태의 추상 클래스
/// </summary>
public abstract class State
{
    protected GameObject agent;
    protected string stateName;

    public State(GameObject agent, string stateName)
    {
        this.agent = agent;
        this.stateName = stateName;
    }

    /// <summary>
    /// 상태 진입 시 호출
    /// </summary>
    public virtual void OnEnter()
    {
        Debug.Log($"[FSM] {stateName} 상태 진입");
    }

    /// <summary>
    /// 매 프레임 실행
    /// </summary>
    public virtual void OnUpdate()
    {
        // 하위 클래스에서 구현
    }

    /// <summary>
    /// 물리 업데이트 시 실행
    /// </summary>
    public virtual void OnFixedUpdate()
    {
        // 하위 클래스에서 구현
    }

    /// <summary>
    /// 상태 종료 시 호출
    /// </summary>
    public virtual void OnExit()
    {
        Debug.Log($"[FSM] {stateName} 상태 종료");
    }

    /// <summary>
    /// 디버그 정보 그리기
    /// </summary>
    public virtual void OnDrawGizmos()
    {
        // 선택적 구현
    }
}
