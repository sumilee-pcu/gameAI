using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면에 디버그 정보를 표시하는 UI 관리자
/// </summary>
public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }

    public Text debugText;
    private string debugInfo = "";

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

    void Update()
    {
        if (debugText != null)
        {
            debugText.text = debugInfo + $"\nFPS: {(1f / Time.deltaTime):F0}";
        }
    }

    public void UpdateDebugInfo(string key, string value)
    {
        debugInfo = $"[{key}]: {value}";
    }
}
