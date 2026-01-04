# Lab 6: A* 경로 탐색

**소요 시간:** 120분
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**연관 Part:** Part IV - 경로 탐색
**이전 Lab:** Lab 5 - Steering Behaviors

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **A* 알고리즘의 핵심**
   - 휴리스틱 함수(Heuristic Function)의 이해
   - Open/Closed Set 관리
   - g값(실제 비용)과 f값(예상 비용) 계산

2. **그리드 기반 경로 탐색**
   - 2D 그리드 맵 생성 및 관리
   - 이동 가능한 셀 판별
   - 이웃 셀 탐색 (4-way, 8-way)

3. **경로 최적화**
   - 경로 스무딩(Path Smoothing)
   - 단선화(Simplification)
   - 비용 함수 최적화

4. **실제 게임 적용**
   - Unity에서 A* 구현
   - 동적 장애물 처리
   - 경로 재계산 전략

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── Pathfinding/
│   │       ├── AStarPathfinder.cs     # A* 알고리즘 구현 (NEW)
│   │       ├── Node.cs                # 경로 노드 (NEW)
│   │       ├── PathfindingAgent.cs    # 경로 추적 에이전트 (NEW)
│   │       └── PathSmoothing.cs       # 경로 스무딩 (NEW)
│   └── Utilities/
│       └── GridManager.cs             # 그리드 맵 관리 (NEW)
├── Prefabs/
│   └── Agents/
│       └── PathfindingAgent.prefab    # 경로 추적 에이전트 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab06_AStar.unity          # 실습 씬 (NEW)
└── Materials/
    ├── PathMaterial.mat               # 경로 선 메테리얼 (NEW)
    └── GridMaterial.mat               # 그리드 표시 메테리얼 (NEW)
```

---

## 주요 개념

### 1. A* 알고리즘의 원리

**A* = Dijkstra + Heuristic (최단 경로 탐색 + 휴리스틱 추측)**

```
평가 함수: f(n) = g(n) + h(n)

f(n): 노드 n을 통과하는 전체 예상 비용
g(n): 시작점에서 노드 n까지의 실제 비용 (확정)
h(n): 노드 n에서 목표까지의 예상 비용 (휴리스틱)
```

#### 단계별 동작

```
1. 시작 노드를 Open Set에 추가

2. Open Set이 비워질 때까지:
   a) f값이 가장 낮은 노드 선택
   b) 그것이 목표라면 → 경로 반환
   c) Closed Set으로 이동
   d) 모든 이웃 노드 확인:
      - 이미 Closed Set에 있으면 스킵
      - Open Set에 없으면 추가
      - Open Set에 있고 새 g값이 더 작으면 업데이트

3. Open Set이 비었는데 목표를 찾지 못함 → 경로 없음
```

#### 시각화

```
상태:
■ : 벽 (이동 불가)
□ : 열린 공간 (이동 가능)
S : 시작점
G : 목표점
* : 탐색 중 (Open Set)
X : 이미 탐색 (Closed Set)

탐색 진행:
초기:           중간:           완료:
□ □ □ □ □      □ X X * □      □ → → → G
□ S □ □ □  →   □ X S * □  →   □ ↗ ↗ ↗ □
□ □ □ □ G      □ X X * G      □ ↗ ↗ ↗ □
□ □ □ □ □      □ □ □ □ □      □ ↗ ↗ ↗ □
```

### 2. 휴리스틱 함수 (Heuristic)

**남은 거리를 추정하는 함수**

#### A. Manhattan Distance (맨해튼 거리)
4방향 이동만 가능할 때 사용

```csharp
float Manhattan(Vector2Int a, Vector2Int b)
{
    return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}

// 예: (0,0)에서 (3,2)
// Manhattan = |0-3| + |0-2| = 3 + 2 = 5
```

#### B. Euclidean Distance (유클리드 거리)
8방향 이동이 가능할 때 사용

```csharp
float Euclidean(Vector2Int a, Vector2Int b)
{
    float dx = a.x - b.x;
    float dy = a.y - b.y;
    return Mathf.Sqrt(dx * dx + dy * dy);
}

// 예: (0,0)에서 (3,4)
// Euclidean = sqrt(9 + 16) = sqrt(25) = 5
```

#### C. Chebyshev Distance (체비셰프 거리)
8방향 이동의 실제 비용을 반영

```csharp
float Chebyshev(Vector2Int a, Vector2Int b)
{
    return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
}

// 예: (0,0)에서 (3,4)
// Chebyshev = max(3, 4) = 4 (대각선 + 직선)
```

**비교:**

```
시작 (0,0) → 목표 (3,4)

          거리 계산
휴리스틱   |값
----------|----
Manhattan | 7
Euclidean | 5
Chebyshev | 4

정확도: Chebyshev > Euclidean > Manhattan
속도: Manhattan > Euclidean > Chebyshev
```

### 3. 노드 구조

```csharp
public class Node
{
    public Vector2Int position;    // 그리드 위치

    public Node parent;            // 경로 추적용
    public float g;                // 시작점에서의 비용
    public float h;                // 휴리스틱 (목표까지의 예상 거리)
    public float f => g + h;       // 총 예상 비용

    public bool isWalkable;        // 이동 가능 여부

    public override bool Equals(object obj)
    {
        return obj is Node node && position == node.position;
    }
}
```

### 4. 경로 스무딩 (Path Smoothing)

**직선 경로를 부드럽게 변환**

```
원본 경로:        부드러운 경로:
□ → □            □ ↗ □
  ↓ →         =    ↓ ↗
□ ← □            □ ← □

불필요한 방향 전환 제거
```

#### 단선화 방법

```csharp
List<Vector3> SmoothPath(List<Vector3> rawPath)
{
    List<Vector3> smoothed = new List<Vector3>();
    smoothed.Add(rawPath[0]);

    for (int i = 1; i < rawPath.Count - 1; i++)
    {
        // 세 점이 일직선상에 있는지 확인
        Vector3 prev = rawPath[i - 1];
        Vector3 curr = rawPath[i];
        Vector3 next = rawPath[i + 1];

        Vector3 dir1 = (curr - prev).normalized;
        Vector3 dir2 = (next - curr).normalized;

        // 방향이 비슷하면 중간 점 생략
        if (Vector3.Dot(dir1, dir2) < 0.999f)
        {
            smoothed.Add(curr);
        }
    }

    smoothed.Add(rawPath[rawPath.Count - 1]);
    return smoothed;
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab06_AStar
```

### Step 2: 기본 환경 구성

#### 2-1. 바닥 생성
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (25, 1, 25)
```

#### 2-2. 카메라 설정
```
Main Camera 선택
Transform:
  - Position: (0, 20, 0)
  - Rotation: (90, 0, 0)  // 위에서 내려다보기
```

### Step 3: GridManager 설정

#### 3-1. GridManager 오브젝트 생성
```
Hierarchy → Create Empty
이름: GridManager

Add Component → GridManager

Inspector 설정:
  - Grid Width: 20
  - Grid Height: 20
  - Cell Size: 1
  - Walkable Layer: Default
```

#### 3-2. 장애물 배치
```
Hierarchy → 3D Object → Cube
이름: Wall
Transform:
  - Position: (10, 0.5, 10)
  - Scale: (2, 1, 8)  // 벽 모양

이런 식으로 장애물 3~5개 배치
```

### Step 4: PathfindingAgent 프리팹

#### 4-1. 에이전트 생성
```
Hierarchy → 3D Object → Sphere
이름: PathfindingAgent
Transform:
  - Position: (0, 0.5, 0)
  - Scale: (0.3, 0.3, 0.3)
```

#### 4-2. 스크립트 추가
```
Add Component → PathfindingAgent

Inspector 설정:
  - Target Position: (20, 20)
  - Movement Speed: 5
  - Recalculate Interval: 1 (초)
```

#### 4-3. 프리팹 생성
```
Assets/Prefabs/Agents/로 드래그
```

### Step 5: 경로 시각화

#### 5-1. 선(Line Renderer) 추가
```
PathfindingAgent 선택
Add Component → Line Renderer

Inspector 설정:
  - Positions: 동적 할당 (스크립트에서)
  - Material: 흰색 또는 초록색
  - Width: 0.1
```

---

## 구현 체크리스트

### Phase 1: 기본 A* 구현 (50분)

- [ ] Node.cs 작성
  - [ ] 위치, 부모, g/h/f값 저장
  - [ ] Equals/GetHashCode 오버라이드
- [ ] AStarPathfinder.cs 작성
  - [ ] Open/Closed Set 구현
  - [ ] 휴리스틱 함수 (Manhattan, Euclidean)
  - [ ] 주요 A* 루프
  - [ ] 경로 생성 함수
- [ ] GridManager.cs 작성
  - [ ] 2D 그리드 생성
  - [ ] 이동 가능 여부 판별
  - [ ] 이웃 노드 탐색 (4방향, 8방향)

### Phase 2: 에이전트 구현 (30분)

- [ ] PathfindingAgent.cs 작성
  - [ ] 경로 요청
  - [ ] 경로 추적
  - [ ] 목표 도달 판별
- [ ] 경로 시각화
  - [ ] Line Renderer로 경로 표시
  - [ ] Gizmos로 그리드 표시 (선택)

### Phase 3: 경로 최적화 (25분)

- [ ] PathSmoothing.cs 작성
  - [ ] 경로 단선화
  - [ ] 곡선 보간 (선택)
- [ ] 테스트
  - [ ] 스무딩 전후 비교
  - [ ] 성능 측정

### Phase 4: 고급 기능 (보너스 - 15분)

- [ ] 동적 장애물 처리
- [ ] 경로 재계산 전략
- [ ] 여러 에이전트 동시 경로 탐색
- [ ] 휴리스틱 함수 비교 (Manhattan vs Euclidean)

---

## 참고 자료

### 공식 문헌

- **"Artificial Intelligence for Games"** - Ian Millington & John Funge
  - Chapter 4: Pathfinding
- **"Programming Game AI by Example"** - Mat Buckland
  - Chapter 5: A* Pathfinding

### 온라인 자료

- **Red Blob Games - A* Pathfinding:**
  - https://www.redblobgames.com/pathfinding/a-star/

### 추천 영상

- Sebastian Lague: "A* Pathfinding"
- Brackeys: "Pathfinding"

---

## 주요 개념 심화

### 1. A* vs Dijkstra vs BFS

**비교:**

```
알고리즘    | 사용 대상       | 속도  | 최적성 | 휴리스틱
-----------|--------------|------|--------|--------
BFS        | 균일 가중치    | 빠름 | 최적   | 없음
Dijkstra   | 모든 경로 탐색 | 중간 | 최적   | 없음
A*         | 점 대 점 경로  | 빠름 | 최적   | 있음

A*가 가장 빠른 이유:
- Dijkstra: 모든 방향 탐색
- A*: 휴리스틱으로 목표 방향 우선 탐색
```

### 2. 휴리스틱 선택의 영향

```csharp
// 약한 휴리스틱 (Manhattan)
float h = Manhattan(current, goal);
// → 더 많은 노드 탐색 (정확하지만 느림)

// 강한 휴리스틱 (Euclidean)
float h = Euclidean(current, goal);
// → 더 적은 노드 탐색 (빠르지만 덜 정확함)

// 주의: 휴리스틱이 과대평가되면 최적경로를 놓칠 수 있음!
```

### 3. 비용 함수 확장

**이동 비용을 동적으로 계산**

```csharp
// 기본: 모든 이동 비용 = 1
float cost = 1f;

// 개선: 지형에 따른 비용
if (IsWater(node)) cost = 2f;      // 물은 비용 높음
if (IsRoad(node)) cost = 0.8f;     // 길은 비용 낮음
if (IsForest(node)) cost = 1.5f;   // 숲은 비용 높음

// 대각선 이동 비용
float diagonalCost = 1.414f;  // sqrt(2)
```

---

## 성능 최적화

### 1. 그리드 크기 최적화

```
그리드 크기 | 장점           | 단점
-----------|--------------|----------
1x1       | 정확한 경로    | 계산 많음 (O(N²))
2x2       | 균형잡힘      | 최적 선택
4x4       | 빠른 계산     | 경로 부정확
```

### 2. A* 최적화 기법

```csharp
// 1. Jump Point Search (JPS)
// 대각선 이동 시 직선을 뛰어넘음
// 성능: A* 대비 10배 빠름

// 2. Theta*
// 직선 경로 최적화
// 더 자연스러운 경로

// 3. Hierarchical Pathfinding
// 큰 맵을 구역별로 나누어 탐색
// 매우 큰 맵에 효율적
```

---

## 트러블슈팅

### 문제 1: 경로를 찾지 못함 (경로 없음)

**확인 사항:**
```csharp
// 1. 시작/목표 위치가 유효한가?
if (!grid.IsWalkable(start)) Debug.Log("시작 위치가 벽임");

// 2. 목표가 막혀 있는가?
if (!grid.IsWalkable(goal)) Debug.Log("목표가 막혀 있음");

// 3. 경로가 있는가?
RaycastHit[] hits = Physics.RaycastAll(start, goal - start);
// 직접 경로 확인
```

### 문제 2: 경로가 비효율적이다

**원인:** 휴리스틱이 약함

**해결책:**
```csharp
// 변경 전: Manhattan (너무 약함)
float h = Mathf.Abs(current.x - goal.x) + Mathf.Abs(current.y - goal.y);

// 변경 후: Euclidean (더 정확)
float dx = current.x - goal.x;
float dy = current.y - goal.y;
float h = Mathf.Sqrt(dx * dx + dy * dy);
```

### 문제 3: A*가 느리다

**원인:** 그리드가 너무 세밀함

**해결책:**
```csharp
// 변경 전: 셀 크기 0.5
GridManager gm = new GridManager(width, height, 0.5f);

// 변경 후: 셀 크기 2
GridManager gm = new GridManager(width, height, 2f);
```

---

## 확장 아이디어

### 아이디어 1: 동적 장애물 처리

```csharp
// 장애물이 움직일 때 경로 재계산
void Update()
{
    if (Time.time - lastPathCalcTime > recalculateInterval)
    {
        // 장애물 위치 업데이트
        grid.UpdateObstacles();

        // 새 경로 계산
        path = pathfinder.FindPath(currentPos, targetPos);

        lastPathCalcTime = Time.time;
    }
}
```

### 아이디어 2: 비용 기반 경로

```csharp
// 특정 지역의 비용을 높여서 회피
void AvoidArea(Vector2Int center, float radius, float costMultiplier)
{
    for (int x = (int)(center.x - radius); x < center.x + radius; x++)
    {
        for (int y = (int)(center.y - radius); y < center.y + radius; y++)
        {
            grid[x, y].costMultiplier = costMultiplier;
        }
    }
}
```

### 아이디어 3: 시야 기반 경로 (Visibility Graph)

```csharp
// 코너 포인트만 연결
// 더 자연스러운 경로
List<Vector3> waypoints = GetVisibilityWaypoints();
```

---

## 완료 체크리스트

- [ ] Node.cs 구현 완료
- [ ] AStarPathfinder.cs 구현 완료
- [ ] GridManager.cs 구현 완료
- [ ] PathfindingAgent.cs 구현 완료
- [ ] Lab06_AStar 씬 생성
- [ ] 경로 탐색 동작 확인
- [ ] 경로 시각화 (Line Renderer)
- [ ] 그리드 시각화 (Gizmos)
- [ ] 경로 스무딩 구현
- [ ] 여러 에이전트 경로 탐색
- [ ] 성능 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**예상 완료 시간:** 120분
