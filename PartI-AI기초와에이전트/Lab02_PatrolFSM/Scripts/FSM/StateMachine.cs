using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 유한 상태 기계
/// </summary>
public class StateMachine
{
    private State currentState;
    private Dictionary<string, State> states = new Dictionary<string, State>();

    /// <summary>
    /// 현재 상태 이름
    /// </summary>
    public string CurrentStateName => currentState?.stateName ?? "None";

    /// <summary>
    /// 상태 추가
    /// </summary>
    public void AddState(string stateName, State state)
    {
        if (!states.ContainsKey(stateName))
        {
            states.Add(stateName, state);
        }
        else
        {
            Debug.LogWarning($"상태 '{stateName}'가 이미 존재합니다.");
        }
    }

    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    public void SetInitialState(string stateName)
    {
        if (states.ContainsKey(stateName))
        {
            currentState = states[stateName];
            currentState.OnEnter();
        }
        else
        {
            Debug.LogError($"상태 '{stateName}'를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 상태 전환
    /// </summary>
    public void ChangeState(string newStateName)
    {
        if (!states.ContainsKey(newStateName))
        {
            Debug.LogError($"상태 '{newStateName}'를 찾을 수 없습니다.");
            return;
        }

        // 현재 상태 종료
        if (currentState != null)
        {
            currentState.OnExit();
        }

        // 새 상태 진입
        currentState = states[newStateName];
        currentState.OnEnter();
    }

    /// <summary>
    /// 현재 상태 업데이트
    /// </summary>
    public void Update()
    {
        if (currentState != null)
        {
            currentState.OnUpdate();
        }
    }

    /// <summary>
    /// 현재 상태 물리 업데이트
    /// </summary>
    public void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.OnFixedUpdate();
        }
    }

    /// <summary>
    /// 디버그 정보 그리기
    /// </summary>
    public void DrawGizmos()
    {
        if (currentState != null)
        {
            currentState.OnDrawGizmos();
        }
    }
}
