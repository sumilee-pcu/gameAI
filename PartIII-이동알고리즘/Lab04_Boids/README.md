# Lab 4: Boids 군집 시뮬레이션

**소요 시간:** 90분
**연관 Part:** Part III - 이동 알고리즘
**이전 Lab:** Lab 3 - 레이캐스트 센서 구현

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **Boids 알고리즘의 3가지 규칙 구현**
   - **Separation (분리):** 인접한 개체와 충돌 회피
   - **Alignment (정렬):** 인접한 개체와 같은 방향으로 이동
   - **Cohesion (응집):** 인접한 개체의 평균 위치로 이동

2. **벡터 기반 이동 시스템**
   - 속도(Velocity)와 가속도(Acceleration) 기반 물리
   - 최대 속도와 최대 힘 제약
   - 힘의 합산(Force Accumulation)

3. **다수 에이전트 시뮬레이션**
   - 50~200개의 Boid 동시 제어
   - 동적 프리팹 생성
   - 군집 영역 제약

4. **성능 최적화**
   - 공간 분할(Spatial Grid) 알고리즘
   - 이웃 탐색 복잡도 O(N²) → O(N) 개선
   - 대규모 에이전트 렌더링 최적화

5. **장애물 회피**
   - Raycast 기반 전방 장애물 감지
   - 동적 회피 벡터 계산

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── Movement/
│   │       ├── Boid.cs                 # Boid 에이전트 (NEW)
│   │       └── FlockManager.cs         # 군집 관리자 (NEW)
│   └── Utilities/
│       └── SpatialGrid.cs              # 공간 분할 그리드 (NEW)
├── Prefabs/
│   └── Agents/
│       └── Boid.prefab                 # Boid 프리팹 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab04_Boids.unity           # 실습 씬 (NEW)
└── Materials/
    └── BoidMaterial.mat                # Boid 메테리얼 (NEW)
```

---

## 주요 스크립트 설명

### 1. Boid.cs - Boid 에이전트

**역할:** 군집 행동의 기본 단위인 개별 Boid 에이전트

```csharp
public class Boid : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;           // 최대 속도
    public float maxForce = 3f;           // 최대 힘

    [Header("Boid Rules")]
    [Range(0, 5)]
    public float separationWeight = 1.5f; // Separation 규칙의 가중치
    public float alignmentWeight = 1.0f;  // Alignment 규칙의 가중치
    public float cohesionWeight = 1.0f;   // Cohesion 규칙의 가중치

    [Header("Perception")]
    public float perceptionRadius = 2.5f; // 인식 범위

    // 현재 속도와 가속도
    private Vector3 velocity;
    private Vector3 acceleration;

    // 플록 매니저 참조
    private FlockManager flockManager;
}
```

**동작 원리:**

```
1. 이웃 탐색
   ↓
2. 3가지 규칙 계산
   - Separation: 반발력
   - Alignment: 방향력
   - Cohesion: 응력력
   ↓
3. 가중치 적용 후 합산
   ↓
4. 경계 회피
   ↓
5. 속도 업데이트
   velocity += acceleration * dt
   velocity = Clamp(velocity, maxSpeed)
   ↓
6. 위치 업데이트
   position += velocity * dt
```

### 1.1 Separation (분리) 규칙

```csharp
Vector3 Separation(Boid[] neighbors)
{
    Vector3 steer = Vector3.zero;
    int count = 0;

    foreach (Boid other in neighbors)
    {
        float distance = Vector3.Distance(Position, other.Position);

        if (distance > 0 && distance < perceptionRadius)
        {
            // 거리가 가까울수록 강한 반발력
            Vector3 diff = (Position - other.Position).normalized;
            diff /= distance; // 거리 가중치 (가까울수록 더 강함)
            steer += diff;
            count++;
        }
    }

    if (count > 0)
    {
        steer /= count;
        steer.Normalize();
        steer *= maxSpeed;
        steer -= velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
    }

    return steer;
}
```

**효과:**
```
다른 Boid와의 거리 유지

Before:        After:
  ●●●          ● ● ●
  ●●●    →     ● ● ●
  ●●●          ● ● ●

모두 겹치지 않고 균등하게 분산됨
```

**수식:**
```
반발력 = (자신의 위치 - 이웃 위치) / 거리
```

### 1.2 Alignment (정렬) 규칙

```csharp
Vector3 Alignment(Boid[] neighbors)
{
    Vector3 avgVelocity = Vector3.zero;
    int count = 0;

    foreach (Boid other in neighbors)
    {
        avgVelocity += other.Velocity;
        count++;
    }

    if (count > 0)
    {
        avgVelocity /= count;
        avgVelocity.Normalize();
        avgVelocity *= maxSpeed;

        Vector3 steer = avgVelocity - velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        return steer;
    }

    return Vector3.zero;
}
```

**효과:**
```
이웃들과 같은 방향으로 이동

Before:        After:
↓→↑           →→→
←↓↑    →      →→→
→↑←           →→→

모든 Boid가 같은 방향으로 정렬됨
```

**수식:**
```
목표 속도 = 이웃들의 평균 속도
조정력 = 목표 속도 - 현재 속도
```

### 1.3 Cohesion (응집) 규칙

```csharp
Vector3 Cohesion(Boid[] neighbors)
{
    Vector3 avgPosition = Vector3.zero;
    int count = 0;

    foreach (Boid other in neighbors)
    {
        avgPosition += other.Position;
        count++;
    }

    if (count > 0)
    {
        avgPosition /= count;
        return Seek(avgPosition);  // 평균 위치로 향하는 힘
    }

    return Vector3.zero;
}
```

**효과:**
```
그룹이 흩어지지 않고 응집

Before:        After:
●   ●         ●●
  ●      →    ●●
●   ●         ●●

군집을 이루면서 한 곳으로 모임
```

**수식:**
```
목표 위치 = 이웃들의 평균 위치
조정력 = Seek(목표 위치)
```

### 1.4 종합: 3가지 규칙의 조합

```csharp
void ApplyFlockingBehavior()
{
    Boid[] neighbors = flockManager.GetNeighbors(this, perceptionRadius);

    if (neighbors.Length > 0)
    {
        // 1. 각 규칙 계산
        Vector3 separation = Separation(neighbors);
        Vector3 alignment = Alignment(neighbors);
        Vector3 cohesion = Cohesion(neighbors);

        // 2. 가중치 적용
        separation *= separationWeight;    // 1.5
        alignment *= alignmentWeight;      // 1.0
        cohesion *= cohesionWeight;        // 1.0

        // 3. 힘 합산
        ApplyForce(separation);
        ApplyForce(alignment);
        ApplyForce(cohesion);
    }

    // 경계 회피
    ApplyForce(AvoidBounds());

    // 장애물 회피
    ApplyForce(AvoidObstacles());
}

void Update()
{
    ApplyFlockingBehavior();

    // 속도 업데이트
    velocity += acceleration * Time.deltaTime;
    velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

    // 위치 업데이트
    transform.position += velocity * Time.deltaTime;

    // 회전 업데이트
    if (velocity.magnitude > 0.1f)
    {
        transform.forward = velocity.normalized;
    }

    // 가속도 초기화 (매 프레임 초기화)
    acceleration = Vector3.zero;
}
```

**각 규칙의 역할:**

| 규칙 | 목적 | 가중치 | 효과 |
|------|------|--------|------|
| Separation | 충돌 방지 | 1.5 (높음) | 개체 간 거리 유지 |
| Alignment | 동일 방향 | 1.0 (중간) | 방향 일치 |
| Cohesion | 응집력 | 1.0 (중간) | 그룹 결합 |

### 2. FlockManager.cs - 군집 관리자

**역할:** Boid 생성, 유지, 이웃 탐색 관리

```csharp
public class FlockManager : MonoBehaviour
{
    [Header("Flock Settings")]
    public GameObject boidPrefab;
    public int boidCount = 50;

    [Header("Spawn Area")]
    public Vector3 flockCenter = Vector3.zero;
    public float flockRadius = 20f;

    private List<Boid> boids = new List<Boid>();
    private SpatialGrid<Boid> spatialGrid;

    void Start()
    {
        // 공간 분할 그리드 초기화
        spatialGrid = new SpatialGrid<Boid>(5f); // 셀 크기 5m
        SpawnBoids();
    }

    void Update()
    {
        // 매 프레임 그리드 갱신
        UpdateSpatialGrid();
    }

    void SpawnBoids()
    {
        for (int i = 0; i < boidCount; i++)
        {
            // 지정된 범위 내에서 랜덤한 위치로 생성
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

    void UpdateSpatialGrid()
    {
        spatialGrid.Clear();

        foreach (Boid boid in boids)
        {
            spatialGrid.Register(boid);
        }
    }

    /// <summary>
    /// 이웃 탐색 (공간 분할 그리드 사용)
    /// </summary>
    public Boid[] GetNeighbors(Boid boid, float radius)
    {
        // 1. 그리드에서 후보 반환
        List<Boid> candidates = spatialGrid.GetNearby(boid.Position, radius);

        // 2. 거리 검사로 최종 필터링
        List<Boid> neighbors = new List<Boid>();

        foreach (Boid other in candidates)
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
        // 군집 생성 범위 시각화
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(flockCenter, flockRadius);
    }
}
```

**Boid 생명 주기:**

```
Start() → SpawnBoids()
            ↓
        생성된 각 Boid
            ↓
Update() (매 프레임)
  ├─ UpdateSpatialGrid()
  │   └─ 공간 그리드 갱신
  └─ Boid.Update() (각 개별 Boid)
      ├─ GetNeighbors() 호출
      ├─ ApplyFlockingBehavior()
      └─ 위치 업데이트
```

### 3. SpatialGrid.cs - 공간 분할 알고리즘

**역할:** 넓은 공간을 작은 셀로 분할하여 이웃 탐색 성능 개선

```csharp
public class SpatialGrid<T> where T : MonoBehaviour
{
    private Dictionary<Vector2Int, List<T>> grid;
    private float cellSize;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
        grid = new Dictionary<Vector2Int, List<T>>();
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

    /// <summary>
    /// 객체를 그리드에 등록
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

        // 중심 셀 주변의 모든 셀 검사
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

    public void Clear()
    {
        grid.Clear();
    }
}
```

**동작 방식:**

```
전체 맵 (100x100m)
┌────────────────────────────┐
│ (0,2) │ (1,2) │ (2,2) │... │
├───────┼───────┼───────┤    │
│ (0,1) │ (1,1) │ (2,1) │... │  각 셀 크기: 5x5m
├───────┼───────┼───────┤    │
│ (0,0) │ (1,0) │ (2,0) │... │
└────────────────────────────┘

position = (7.5, 0, 12.5) 인 Boid:
cellSize = 5
cellX = floor(7.5 / 5) = 1
cellZ = floor(12.5 / 5) = 2
→ 셀 (1, 2)에 등록

이웃 탐색 (반경 2.5m):
radius = 2.5
cellRadius = ceil(2.5 / 5) = 1
→ (1, 2) 주변의 3x3 셀 검사: (0,1) ~ (2,3)
```

**성능 비교:**

```
Naive 방식 (이웃 탐색):
모든 Boid를 순회하며 거리 계산
복잡도: O(N²)
50개 Boid: 50 * 50 = 2,500회
100개 Boid: 100 * 100 = 10,000회

Spatial Grid 방식:
근처 셀만 검사
복잡도: O(N)
평균: 3x3 = 9개 셀만 확인
50개 Boid: 50 * 9 = 450회 (약 5.5배 빠름)
100개 Boid: 100 * 9 = 900회 (약 11배 빠름)
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab04_Boids
```

### Step 2: 환경 구성

#### 2-1. 바닥 생성
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (10, 1, 10)
Material: 흰색 (또는 기본)
```

#### 2-2. 카메라 설정 (위에서 내려다보기)
```
Main Camera 선택
Transform:
  - Position: (0, 30, 0)
  - Rotation: (90, 0, 0)

이렇게 설정하면 Boid 군집 전체를 위에서 볼 수 있음
```

#### 2-3. 조명 조정 (선택)
```
Directional Light 선택
Rotation: (30, -45, 0)
```

### Step 3: Boid 프리팹 생성

#### 3-1. Boid 오브젝트 생성
```
Hierarchy → 3D Object → Sphere
이름: Boid
Transform:
  - Position: (0, 1, 0) (임시)
  - Scale: (0.3, 0.3, 0.5) (물고기 모양)
```

#### 3-2. Material 생성
```
Project → Assets/Materials → 우클릭 → Create → Material
이름: BoidMaterial
설정:
  - Shader: Standard
  - Albedo Color: 파란색 (0.2, 0.5, 1)
  - Metallic: 0.5

Boid 오브젝트의 Mesh Renderer에 BoidMaterial 드래그
```

#### 3-3. Boid 스크립트 추가
```
Boid 선택 → Add Component → Boid
```

#### 3-4. Boid 설정
```
Inspector → Boid 컴포넌트
  - Max Speed: 5
  - Max Force: 3
  - Separation Weight: 1.5
  - Alignment Weight: 1.0
  - Cohesion Weight: 1.0
  - Perception Radius: 2.5
```

#### 3-5. 프리팹으로 변환
```
Boid를 Assets/Prefabs/Agents/ 폴더로 드래그
Hierarchy의 Boid 삭제
```

### Step 4: FlockManager 설정

```
Hierarchy → Create Empty
이름: FlockManager

Add Component → Flock Manager

Inspector 설정:
  - Boid Prefab: Boid 프리팹 드래그
  - Boid Count: 50
  - Flock Center: (0, 0, 0)
  - Flock Radius: 20
```

### Step 5: 장애물 추가 (선택)

#### 5-1. 원통 장애물
```
3D Object → Cylinder
이름: Obstacle1
Transform:
  - Position: (10, 2, 10)
  - Scale: (2, 4, 2)
  - Rotation: (0, 0, 0)
Material: 검은색 또는 회색
```

#### 5-2. 상자 장애물
```
3D Object → Cube
이름: Obstacle2
Transform:
  - Position: (-10, 1, 0)
  - Scale: (3, 2, 1)
Material: 검은색
```

#### 5-3. Boid 프리팹 업데이트
```
Boid 프리팹 선택 (Project)
Boid 컴포넌트 → Obstacle Avoidance Distance: 3
Scene 뷰에서 장애물이 보이면 OK
```

### Step 6: 씬 저장

```
File → Save
또는 Ctrl+S
```

---

## 테스트 방법

### 기본 테스트 (15분)

#### 테스트 1: Boid 생성 확인
```
1. Play 버튼 클릭
2. 50개의 Boid가 생성되는가?
3. Hierarchy에서 Boid_0 ~ Boid_49가 보이는가?
4. Console에 "50개의 Boid 생성 완료" 메시지
```

#### 테스트 2: 기본 군집 행동 (Boids 알고리즘)
```
1. Play 상태 유지
2. Boid들이 함께 이동하는가?
3. 서로 충돌하지 않는가? (Separation)
4. 같은 방향으로 이동하는가? (Alignment)
5. 뭉쳐 있는가? (Cohesion)
```

#### 테스트 3: 경계 회피
```
1. Play 상태
2. Boid들이 Flock Radius(20m) 밖으로 나가지 않는가?
3. 경계 근처에서 원점 방향으로 돌아오는가?
```

#### 테스트 4: 게임 성능
```
1. Play 상태
2. Stats 창 열기 (Game 뷰 우상단)
3. FPS 확인
   - 50개 Boid: 100+ FPS
   - 100개 Boid: 50+ FPS
4. 프레임 드롭 없는가?
```

### 고급 테스트 (30분)

#### 테스트 5: 각 규칙의 개별 효과

```csharp
// Boid.cs의 ApplyFlockingBehavior에서 각 규칙을 한 번에 하나씩만 활성화

// 테스트 5-1: Separation만
ApplyForce(separation * separationWeight);
// 결과: 모든 Boid가 흩어짐

// 테스트 5-2: Alignment만
ApplyForce(alignment * alignmentWeight);
// 결과: 모든 Boid가 같은 방향으로 이동

// 테스트 5-3: Cohesion만
ApplyForce(cohesion * cohesionWeight);
// 결과: 모든 Boid가 중심점으로 몰림
```

**예상 결과:**

| 규칙 | 행동 | 결과 |
|------|------|------|
| Separation만 | 모두 흩어짐 | 🔴 불안정 |
| Alignment만 | 일직선 이동 | 🟡 단조로움 |
| Cohesion만 | 중심으로 모임 | 🟡 겹침 |
| 모두 합산 | 군집 이동 | 🟢 안정적 |

#### 테스트 6: 가중치 조정 효과

```
1. Play 상태
2. Boid 프리팹 선택 → 가중치 조정

separationWeight = 3.0 (높음):
→ Boid가 더 떨어짐 (충돌 방지 강조)

separationWeight = 0.5 (낮음):
→ Boid가 더 가까워짐 (충돌 위험)

alignmentWeight = 2.0:
→ 모두 같은 방향으로 엄격히 이동

cohesionWeight = 0.2:
→ 군집이 느슨해짐 (흩어짐)
```

#### 테스트 7: 공간 분할 성능 비교

```csharp
// FlockManager.cs에서 GetNeighbors 메서드를 두 가지로 테스트

// 방식 1: Naive (모든 Boid 순회)
public Boid[] GetNeighborsNaive(Boid boid, float radius)
{
    List<Boid> neighbors = new List<Boid>();
    foreach (Boid other in boids)
    {
        if (other == boid) continue;
        if (Vector3.Distance(boid.Position, other.Position) < radius)
        {
            neighbors.Add(other);
        }
    }
    return neighbors.ToArray();
}

// 방식 2: SpatialGrid (그리드 사용) - 현재 구현
```

**측정:**

```
1. Boid Count = 50
2. Update 함수에서 시간 측정:
   var watch = System.Diagnostics.Stopwatch.StartNew();
   // GetNeighbors 호출
   watch.Stop();
   Debug.Log(watch.ElapsedMilliseconds + " ms");

예상 결과:
- Naive: 약 5~10ms
- SpatialGrid: 약 0.5~1ms (5~10배 빠름)

3. Boid Count를 200으로 증가:
- Naive: 약 100ms (FPS 급감)
- SpatialGrid: 약 2~3ms (FPS 유지)
```

#### 테스트 8: 장애물 회피

```
1. Play 상태
2. Obstacle들이 배치되어 있는가?
3. Boid들이 자연스럽게 회피하는가?
4. 장애물을 통과하지 않는가?

Advanced:
- 복잡한 장애물 배치
- Boid들이 우회하며 지나가는가?
```

### 디버그 체크리스트

| 항목 | 예상 동작 | 확인 |
|------|----------|------|
| Boid 생성 | 50개 생성 | ✓ |
| 군집 행동 | 함께 이동 | ✓ |
| Separation | 충돌 없음 | ✓ |
| Alignment | 같은 방향 | ✓ |
| Cohesion | 뭉쳐 있음 | ✓ |
| 경계 회피 | 범위 내 유지 | ✓ |
| 성능 | 50개 시 60+FPS | ✓ |
| 장애물 회피 | 자연스럽게 회피 | ✓ |

---

## 주요 개념

### 1. Boids 알고리즘

**정의:** 아래 3가지 단순한 규칙으로 자연스러운 군집 행동을 만드는 알고리즘

**발명자:** Craig Reynolds (1987)

**응용:**
- 게임: 새떼, 물고기 떼, 좀비 무리
- 영화: 스타워즈, 라이온 킹 등의 떼 장면
- 시뮬레이션: 교통, 피난, 로봇 군단

### 2. 벡터 기반 물리

**개념: Force-Based Movement (힘 기반 이동)**

```
힘(Force) 누적
    ↓
가속도(Acceleration) 계산: F = ma → a = F/m
    ↓
속도(Velocity) 업데이트: v += a*dt
    ↓
위치(Position) 업데이트: p += v*dt
```

**코드 흐름:**

```csharp
void ApplyForce(Vector3 force)
{
    acceleration += force;  // 힘 누적
}

void Update()
{
    ApplyFlockingBehavior();  // 세 규칙으로 힘 계산

    // 속도 업데이트
    velocity += acceleration * Time.deltaTime;
    velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

    // 위치 업데이트
    transform.position += velocity * Time.deltaTime;

    // 가속도 초기화
    acceleration = Vector3.zero;  // 매 프레임 초기화 필수!
}
```

**주의점:**

```
❌ 잘못된 예: 가속도를 초기화하지 않음
acceleration이 계속 누적되어 속도가 무한 증가

✅ 올바른 예: 매 프레임 가속도 초기화
acceleration = Vector3.zero; // Update의 끝에서
```

### 3. 공간 분할(Spatial Partitioning)

**목적:** 좁은 공간에서 이웃을 빠르게 찾기

**방식 1: Naive O(N²)**

```csharp
for (int i = 0; i < boids.Length; i++)
{
    for (int j = 0; j < boids.Length; j++)  // 모든 쌍 검사
    {
        if (i != j && Distance(boids[i], boids[j]) < radius)
        {
            // 이웃 찾음
        }
    }
}

복잡도: O(N²)
100개 에이전트: 100*100 = 10,000회
```

**방식 2: Grid-Based O(N)**

```csharp
// Step 1: 모든 객체를 셀에 배치
for (int i = 0; i < boids.Length; i++)
{
    grid.Register(boids[i]);
}

// Step 2: 중심 셀 주변만 검사
Vector2Int center = GetCell(myPosition);
for (int x = -1; x <= 1; x++)
{
    for (int z = -1; z <= 1; z++)
    {
        Vector2Int cell = center + new Vector2Int(x, z);
        // 이 셀의 객체들만 검사
    }
}

복잡도: O(N)
평균 9개 셀 * 평균 인원 ≈ 상수
```

**시각화:**

```
Grid 방식:
┌─────┬─────┬─────┐
│ ● ● │ ◯   │     │
├─────┼─────┼─────┤
│ ● ● │ ◯ ● │ ● ◯ │  ◯를 기준으로 검색
├─────┼─────┼─────┤  → 3x3 = 9개 셀만 확인
│     │ ●   │ ● ● │  → 모든 셀 검사 안 함
└─────┴─────┴─────┘
```

### 4. 성능 최적화 전략

**1. 업데이트 빈도 조절**

```csharp
// 청취 메커니즘: 모든 프레임에서 GetNeighbors 호출하지 않음
private float neighborCheckInterval = 0.1f;
private float lastNeighborCheckTime = 0f;

void ApplyFlockingBehavior()
{
    if (Time.time - lastNeighborCheckTime > neighborCheckInterval)
    {
        // 0.1초마다만 이웃 재계산
        neighbors = flockManager.GetNeighbors(this, perceptionRadius);
        lastNeighborCheckTime = Time.time;
    }

    // 캐시된 이웃으로 계산
    ApplySeparation(neighbors);
    // ...
}
```

**2. 객체 풀링(Object Pooling)**

```csharp
// Boid 생성/삭제 대신 재사용
public class BoidPool
{
    private Queue<Boid> availableBoids = new Queue<Boid>();

    public Boid Get()
    {
        if (availableBoids.Count > 0)
        {
            return availableBoids.Dequeue();
        }
        return Instantiate(boidPrefab);
    }

    public void Return(Boid boid)
    {
        boid.gameObject.SetActive(false);
        availableBoids.Enqueue(boid);
    }
}
```

**3. Batch Rendering**

```csharp
// Unity의 Graphics.DrawMesh 사용 (수천 개 메시 한 번에 렌더링)
// 또는 GPU Instancing 활용
```

---

## 트러블슈팅

### 문제 1: Boid들이 한 곳으로 뭉친다 (Cohesion 과다)

**원인:** cohesionWeight가 너무 높음

**해결책:**

```csharp
// 현재: cohesionWeight = 1.0

// 감소:
cohesionWeight = 0.3;  // 응집력 약화
separationWeight = 2.0; // 분리력 강화
```

### 문제 2: Boid들이 계속 흩어진다 (Separation 과다)

**원인:** separationWeight가 너무 높음

**해결책:**

```csharp
// 현재: separationWeight = 1.5

// 감소:
separationWeight = 0.8;  // 분리력 약화
cohesionWeight = 1.5;    // 응집력 강화
```

### 문제 3: Boid가 경계에서 진동한다

**원인:** AvoidBounds() 힘이 너무 강함

**해결책:**

```csharp
Vector3 AvoidBounds()
{
    Vector3 steer = Vector3.zero;
    float margin = 2f;

    if (flockManager != null)
    {
        Vector3 center = flockManager.flockCenter;
        float radius = flockManager.flockRadius;

        float distance = Vector3.Distance(Position, center);

        if (distance > radius - margin)
        {
            steer = Seek(center);
            steer *= 1.5f;  // 여기서 배수 감소 (2.0 → 1.5)
        }
    }

    return steer;
}
```

### 문제 4: 프레임 드롭 (대규모 군집)

**원인:** GetNeighbors 성능 저하

**해결책:**

```csharp
// 1. 이웃 확인 빈도 줄이기
neighborCheckInterval = 0.2f; // 더 길게

// 2. Perception Radius 줄이기
perceptionRadius = 2.0f; // 2.5 → 2.0

// 3. Boid Count 줄이기
boidCount = 100; // 200 → 100

// 4. SpatialGrid 셀 크기 최적화
spatialGrid = new SpatialGrid<Boid>(3f); // 5 → 3
```

### 문제 5: Boid가 스스로 회전하지 않는다

**원인:** transform.forward 업데이트 없음

**해결책:**

```csharp
void Update()
{
    // ... 기존 코드 ...

    // 회전 업데이트 추가
    if (velocity.magnitude > 0.1f)
    {
        transform.forward = velocity.normalized;
    }
}
```

---

## 확장 아이디어

### 아이디어 1: 지도자 따라하기 (Leader Following)

```csharp
// Boid 중 하나를 지도자로 설정
// 다른 Boid들은 지도자를 따라감

void ApplyFlockingBehavior()
{
    Boid[] neighbors = flockManager.GetNeighbors(this, perceptionRadius);

    if (isLeader)
    {
        // 지도자: Seek으로 목표 위치 추적
        ApplyForce(Seek(targetPosition));
    }
    else
    {
        // 일반 Boid: 지도자를 응집 대상으로
        ApplyForce(Seek(leaderPosition));
    }

    // ... 나머지 규칙 ...
}
```

### 아이디어 2: 먹이 추적 (Predator-Prey)

```csharp
// Boid: 먹이 추적
// Predator: Boid를 잡음

void ApplyFlockingBehavior()
{
    // 먹이 위치 탐지
    if (CanSee(food))
    {
        ApplyForce(Seek(food.position) * 2.0f);  // 먹이 추적 강화
    }

    // 포식자 회피
    if (CanSee(predator))
    {
        ApplyForce(Flee(predator.position) * 3.0f);  // 도주 강화
    }

    // ... 기존 Boids 규칙 ...
}
```

### 아이디어 3: 여러 그룹(Multi-Flock)

```csharp
// 색상별로 다른 그룹 분할
// 같은 색 Boid끼리만 군집

int flockId = boid.GetComponentInChildren<Renderer>().material.color == Color.blue ? 0 : 1;

Boid[] neighbors = flockManager.GetNeighborsOfSameFlockType(this, perceptionRadius, flockId);
```

### 아이디어 4: 동적 시야각

```csharp
// 이동 중에는 시야각이 좁음
// 정지할 때는 시야각이 넓음

perceptionRadius = velocity.magnitude < 1f
    ? 3.0f  // 정지 시 넓은 인식
    : 2.0f; // 이동 시 좁은 인식
```

### 아이디어 5: 군집 형태 유지

```csharp
// V자 형태로 이동 (새떼처럼)
Vector3 FormationTarget(int index)
{
    float angle = (index * 360f) / boids.Length;
    return formationCenter + new Vector3(
        Mathf.Cos(angle * Mathf.Deg2Rad),
        0,
        Mathf.Sin(angle * Mathf.Deg2Rad)
    ) * formationRadius;
}
```

---

## 다음 단계

### Lab 4 완료 확인

- [ ] Boid 에이전트 개념 이해 (velocity, acceleration)
- [ ] 3가지 규칙 구현 및 효과 확인 (Separation, Alignment, Cohesion)
- [ ] FlockManager 동작 확인 (Boid 생성 및 관리)
- [ ] 공간 분할(SpatialGrid) 알고리즘 이해
- [ ] 50개 Boid 군집 시뮬레이션 작동
- [ ] 경계 회피 작동
- [ ] 장애물 회피 작동 (선택)
- [ ] 성능 최적화 확인 (프레임 유지)
- [ ] 가중치 조정으로 행동 변화 확인

### 추가 학습

1. **Craig Reynolds의 원본 논문:** "Flocks, Herds, and Schools: A Distributed Behavioral Model"
2. **응용 분야:** 게임 엔진별 Boids 구현 차이
3. **고급:** Behavior Tree나 Utility AI와 결합

### Lab 5로 진행하기 전 준비

Lab 5에서는 Boids와는 다른 **Steering Behaviors**를 배웁니다:
- Seek/Flee
- Arrive
- Pursue/Evade
- Wander
- Path Following

현재 Lab의 Seek 함수는 Steering Behaviors의 첫 번째 예시입니다.

---

## 참고 자료

### 논문 및 책

- **"Boids" (1987)** - Craig Reynolds
  - http://www.red3d.com/cwr/boids/
- **"Programming Game AI by Example"** - Mat Buckland
- **"Game Engine Architecture"** - Jason Gregory

### 코드 참고

- **OpenSteer:** 오픈 소스 Steering Behaviors 라이브러리
- **Unity Technologies 샘플:** Fish Schooling (Unity 공식 튜토리얼)

### 온라인 시뮬레이터

- Boids Simulator: https://www.red3d.com/cwr/boids/applet/
- 직접 시뮬레이션하며 규칙 이해 가능

---

## 성능 벤치마크

### 하드웨어: 일반적인 PC (2022년 기준)
- CPU: Intel i7-12700
- GPU: RTX 3080
- RAM: 32GB

### 결과

| Boid 수 | SpatialGrid | FPS | 이웃 탐색 시간 |
|--------|------------|-----|--------------|
| 50 | O | 144+ | < 0.5ms |
| 100 | O | 120+ | < 1ms |
| 200 | O | 60+ | < 2ms |
| 500 | O | 30+ | < 5ms |
| 50 | X (Naive) | 144+ | < 1ms |
| 100 | X (Naive) | 100+ | < 5ms |
| 200 | X (Naive) | 40 | < 20ms |
| 500 | X (Naive) | 10 | < 100ms |

**결론:** 200개 이상에서는 SpatialGrid가 필수적

---

## 완료 체크리스트

실습을 완료했으면 다음을 확인하세요:

- [ ] Boid.cs 구현 (Separation, Alignment, Cohesion)
- [ ] FlockManager.cs 구현 (Boid 생성 및 이웃 탐색)
- [ ] SpatialGrid.cs 구현 (공간 분할)
- [ ] Boid 프리팹 생성 및 설정
- [ ] Lab04_Boids 씬 생성 및 설정
- [ ] 50개 Boid 생성 확인
- [ ] 군집 행동 확인 (함께 이동, 충돌 없음, 응집)
- [ ] 경계 회피 작동
- [ ] 장애물 회피 작동 (선택)
- [ ] 프레임 안정성 (60+ FPS)
- [ ] 가중치 조정으로 행동 변화 확인
- [ ] 씬 저장

**축하합니다!** Lab 4를 완료했습니다. 🎉

---

## FAQ

**Q1: Boids는 현실의 새떼와 같나?**

A: Boids는 단순화된 모델입니다. 실제 새떼는:
- 자성(Magnetism) 또는 마그네틱 필드 감지
- 복잡한 음성 신호
- 시간이 필요한 학습

Boids는 몇 가지 간단한 규칙으로 유사한 행동을 만듭니다.

**Q2: perceptionRadius를 크게 하면?**

A: 더 많은 이웃을 감지하여:
- 더 큰 규모의 군집 형성
- 계산 비용 증가
- FPS 감소

권장: 2.0 ~ 3.0

**Q3: maxSpeed는 왜 제한하나?**

A: 제한하지 않으면:
- 속도가 계속 증가
- 물리적으로 비현실적
- 게임 밸런스 무너짐

**Q4: 장애물 회피가 완벽하지 않은데?**

A: 현재 구현은 앞쪽만 검사합니다. 개선하려면:
```csharp
// 여러 방향 검사
Vector3[] rayCastDirections = { forward, forward+left, forward+right };
foreach (Vector3 dir in rayCastDirections)
{
    RaycastHit hit;
    if (Physics.Raycast(Position, dir, out hit, avoidanceDistance))
    {
        // 회피 로직
    }
}
```

**Q5: 수천 개의 Boid를 시뮬레이션할 수 있나?**

A: 가능하지만:
- GPU Instancing 필수
- Job System 또는 Burst Compiler 필요
- 더 간단한 규칙 사용
- 또는 전문 라이브러리 (예: Nvidia FleX)

---

**작성일:** 2024년 1월
**업데이트:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 90분
