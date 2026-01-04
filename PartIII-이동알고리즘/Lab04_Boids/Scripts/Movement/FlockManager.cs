using UnityEngine;
using System.Collections.Generic;

// Boid 군집 관리 및 이웃 탐색
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
        // Boid 생성
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 randomPos = flockCenter + Random.insideUnitSphere * flockRadius;
            randomPos.y = 1f;

            GameObject boidObj = Instantiate(boidPrefab, randomPos, Quaternion.identity);
            boidObj.name = $"Boid_{i}";
            boidObj.transform.SetParent(transform);

            Boid boid = boidObj.GetComponent<Boid>();
            if (boid != null)
                boids.Add(boid);
        }

        Debug.Log($"{boidCount}개의 Boid 생성 완료");
    }

    // 인식 범위 내 이웃 탐색 (O(n) 거리 검사)
    public Boid[] GetNeighbors(Boid boid, float radius)
    {
        List<Boid> neighbors = new List<Boid>();

        foreach (Boid other in boids)
        {
            if (other != boid && Vector3.Distance(boid.Position, other.Position) < radius)
                neighbors.Add(other);
        }

        return neighbors.ToArray();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(flockCenter, flockRadius);
    }
}
