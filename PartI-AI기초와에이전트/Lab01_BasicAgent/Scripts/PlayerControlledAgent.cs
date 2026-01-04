using UnityEngine;

/// <summary>
/// 키보드 입력으로 제어 가능한 에이전트
/// </summary>
public class PlayerControlledAgent : SimpleAgent
{
    protected override void UpdateAI()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            inputDirection.Normalize();
            velocity = inputDirection * moveSpeed;
        }
        else
        {
            velocity = Vector3.zero;
        }
    }
}
