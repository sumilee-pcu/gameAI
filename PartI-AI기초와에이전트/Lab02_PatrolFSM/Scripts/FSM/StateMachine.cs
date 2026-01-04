using UnityEngine;
using System.Collections.Generic;

public class StateMachine
{
    private State currentState;
    private Dictionary<string, State> states = new Dictionary<string, State>();

    public string CurrentStateName => currentState?.stateName ?? "None";

    public void AddState(string stateName, State state)
    {
        if (!states.ContainsKey(stateName))
            states.Add(stateName, state);
        else
            Debug.LogWarning($"상태 '{stateName}'가 이미 존재합니다.");
    }

    public void SetInitialState(string stateName)
    {
        if (states.ContainsKey(stateName))
        {
            currentState = states[stateName];
            currentState.OnEnter();
        }
        else
            Debug.LogError($"상태 '{stateName}'를 찾을 수 없습니다.");
    }

    public void ChangeState(string newStateName)
    {
        if (!states.ContainsKey(newStateName))
        {
            Debug.LogError($"상태 '{newStateName}'를 찾을 수 없습니다.");
            return;
        }

        currentState?.OnExit();
        currentState = states[newStateName];
        currentState.OnEnter();
    }

    public void Update() => currentState?.OnUpdate();
    public void FixedUpdate() => currentState?.OnFixedUpdate();
    public void DrawGizmos() => currentState?.OnDrawGizmos();
}
