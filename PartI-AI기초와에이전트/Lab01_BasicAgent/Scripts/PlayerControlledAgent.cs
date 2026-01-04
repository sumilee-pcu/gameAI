using UnityEngine;

/// <summary>
/// 키보드 입력으로 제어 가능한 에이전트
/// </summary>
public class PlayerControlledAgent : SimpleAgent
{
    protected override void UpdateAI()
    {
        // 키보드 입력 받기
        float horizontal = Input.GetAxis("Horizontal"); // A/D 또는 ←/→
        float vertical = Input.GetAxis("Vertical");     // W/S 또는 ↑/↓

        // 입력을 3D 방향 벡터로 변환
        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);

        // 입력이 있을 때만 이동
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            inputDirection.Normalize();
            velocity = inputDirection * moveSpeed;
        }
        else
        {
            velocity = Vector3.zero; // 입력 없으면 정지
        }
    }

    protected override void DrawDebugInfo()
    {
        base.DrawDebugInfo();

        // 현재 속도 표시
        Vector3 textPos = transform.position + Vector3.up * 2.5f;

        #if UNITY_EDITOR
        // 에디터에서만 텍스트 표시
        UnityEditor.Handles.Label(
            textPos,
            $"Speed: {velocity.magnitude:F2} m/s"
        );
        #endif
    }
}
