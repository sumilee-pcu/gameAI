using UnityEngine;
using System;

/// <summary>
/// 소음 이벤트 관리자
/// </summary>
public class NoiseEvent : MonoBehaviour
{
    public static NoiseEvent Instance { get; private set; }

    // 소음 발생 이벤트
    public static event Action<Vector3, float> OnNoiseMade;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 소음 발생
    /// </summary>
    public static void MakeNoise(Vector3 position, float noiseLevel)
    {
        OnNoiseMade?.Invoke(position, noiseLevel);

        Debug.Log($"소음 발생: 위치 {position}, 레벨 {noiseLevel}");

        // 시각화
        Debug.DrawRay(position, Vector3.up * 3f, Color.red, 1f);
    }
}
