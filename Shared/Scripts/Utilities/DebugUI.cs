using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면에 디버그 정보를 표시하는 UI 관리자
/// </summary>
public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }

    [Header("UI References")]
    public Text debugText;

    private string debugInfo = "";

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (debugText != null)
        {
            debugText.text = debugInfo;
        }
    }

    /// <summary>
    /// 디버그 정보 업데이트
    /// </summary>
    public void UpdateDebugInfo(string key, string value)
    {
        // 실제 구현에서는 Dictionary를 사용하여 효율적으로 관리
        debugInfo = $"[Game AI Debug]\n{key}: {value}\nFPS: {(1f / Time.deltaTime):F0}";
    }

    /// <summary>
    /// 디버그 정보 초기화
    /// </summary>
    public void ClearDebugInfo()
    {
        debugInfo = "";
    }
}
