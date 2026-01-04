using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 공간 분할 그리드 - 이웃 탐색 최적화
/// </summary>
public class SpatialGrid<T> where T : MonoBehaviour
{
    private Dictionary<Vector2Int, List<T>> grid = new Dictionary<Vector2Int, List<T>>();
    private float cellSize;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    /// <summary>
    /// 그리드 초기화
    /// </summary>
    public void Clear()
    {
        grid.Clear();
    }

    /// <summary>
    /// 객체 등록
    /// </summary>
    public void Register(T obj)
    {
        Vector2Int cell = GetCell(obj.transform.position);

        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<T>();
        }

        grid[cell].Add(obj);
    }

    /// <summary>
    /// 주변 객체 탐색
    /// </summary>
    public List<T> GetNearby(Vector3 position, float radius)
    {
        List<T> nearby = new List<T>();
        Vector2Int center = GetCell(position);

        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        // 주변 셀 검사
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                Vector2Int cell = new Vector2Int(center.x + x, center.y + z);

                if (grid.ContainsKey(cell))
                {
                    nearby.AddRange(grid[cell]);
                }
            }
        }

        return nearby;
    }

    /// <summary>
    /// 위치를 셀 좌표로 변환
    /// </summary>
    Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
}
