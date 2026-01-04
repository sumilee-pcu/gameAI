using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Boid 군집 관리자
/// </summary>
public class FlockManager : MonoBehaviour
{
    [Header("Flock Settings")]
    public GameObject boidPrefab;
    public int boidCount = 50;

    [Header("Spawn Area")]
    public Vector3 flockCenter = Vector3.zero;
    public float flockRadius = 20f;

    private List<Boid> boids = new List<Boid>();

    void Start()
    {
        SpawnBoids();
    }

    /// <summary>
    /// Boid 생성
    /// </summary>
    void SpawnBoids()
    {
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 randomPos = flockCenter + Random.insideUnitSphere * flockRadius;
            randomPos.y = 1f; // 높이 고정

            GameObject boidObj = Instantiate(boidPrefab, randomPos, Quaternion.identity);
            boidObj.name = $"Boid_{i}";
            boidObj.transform.SetParent(transform);

            Boid boid = boidObj.GetComponent<Boid>();
            if (boid != null)
            {
                boids.Add(boid);
            }
        }

        Debug.Log($"{boidCount}개의 Boid 생성 완료");
    }

    /// <summary>
    /// 주변 Boid 탐색
    /// </summary>
    public Boid[] GetNeighbors(Boid boid, float radius)
    {
        List<Boid> neighbors = new List<Boid>();

        foreach (Boid other in boids)
        {
            if (other == boid) continue;

            float distance = Vector3.Distance(boid.Position, other.Position);

            if (distance < radius)
            {
                neighbors.Add(other);
            }
        }

        return neighbors.ToArray();
    }

    void OnDrawGizmos()
    {
        // 군집 영역 표시
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(flockCenter, flockRadius);
    }
}
