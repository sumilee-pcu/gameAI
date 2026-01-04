using UnityEngine;

public abstract class State
{
    protected GameObject agent;
    protected string stateName;

    public State(GameObject agent, string stateName)
    {
        this.agent = agent;
        this.stateName = stateName;
    }

    public virtual void OnEnter() => Debug.Log($"[FSM] {stateName} 상태 진입");
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnExit() => Debug.Log($"[FSM] {stateName} 상태 종료");
    public virtual void OnDrawGizmos() { }
}
