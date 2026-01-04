# Lab 7: NavMesh 에이전트

**소요 시간:** 45분
**난이도:** ⭐⭐⭐ (중상)
**연관 Part:** Part IV - 경로 탐색
**이전 Lab:** Lab 6 - A* 경로 탐색

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **Unity NavMesh 시스템**
   - NavMesh Bake (네비게이션 메시 생성)
   - NavMesh Surface 및 Modifier
   - NavMeshAgent 컴포넌트 활용

2. **NavMeshAgent 기본 사용법**
   - SetDestination으로 목표 설정
   - 자동 경로 계산 및 추적
   - 에이전트 상태 확인

3. **동적 장애물 처리**
   - Carving (동적 NavMesh 수정)
   - NavMesh Link (수평 이동)
   - OffMeshLink (점프 등 특수 이동)

4. **다양한 에이전트 타입**
   - 크기가 다른 에이전트 (player, enemy, NPC)
   - 에이전트별 속도 및 가속도 설정
   - 우선순위 기반 충돌 회피

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── Navigation/
│   │       ├── NavMeshAgentController.cs      # NavMesh 에이전트 제어 (NEW)
│   │       ├── PatrolBehavior.cs              # 순찰 행동 (NEW)
│   │       └── DynamicObstacleManager.cs      # 동적 장애물 관리 (NEW)
│   └── Utilities/
│       └── NavMeshDebugger.cs                 # 디버그 시각화 (NEW)
├── Prefabs/
│   └── Agents/
│       ├── NavMeshAgent.prefab                # NavMesh 에이전트 (NEW)
│       └── DynamicObstacle.prefab             # 동적 장애물 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab07_NavMesh.unity                # 실습 씬 (NEW)
└── Materials/
    └── NavMeshMaterial.mat                    # NavMesh 시각화 (NEW)
```

---

## 주요 개념

### 1. NavMesh (Navigation Mesh)

**게임월드를 보행 가능한 다각형으로 변환**

```
원본 환경:          NavMesh:
┌─────────┐        ┌─────────┐
│ ■ ■ ■ ■ │        │╱╱╱╱╱╱╱╱│
│■ ░ ░ ░■ │   →    │╱╱░░░░╱╱│
│■░░░░░░ │        │╱░░░░░░░│
│ ■ ░ ░ ■ │        │░░░░░░░ │
└─────────┘        └─────────┘

■: 벽 (이동 불가)
░: 보행 가능
╱: 경계
```

#### 우점: Baked NavMesh
- 사전에 계산된 네비게이션
- 빠른 경로 계산
- 정적 환경에 최적

```
Bake 과정:
1. NavMesh Surface 추가
2. Walkable Layer 설정
3. Bake 클릭
4. NavMesh 생성 완료
```

### 2. NavMeshAgent의 이동 원리

```csharp
public class NavMeshAgentController : MonoBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        // 매 프레임 자동 업데이트:
        // 1. 경로 추적
        // 2. 속도 계산
        // 3. 위치 업데이트
        // 4. 장애물 회피 (steering avoidance)
    }

    public void MoveTo(Vector3 destination)
    {
        // NavMeshAgent가 자동으로 경로 계산
        agent.SetDestination(destination);
    }
}
```

#### 자동 기능

```
SetDestination() 호출
        ↓
경로 계산 (자동)
        ↓
매 프레임 이동 (자동)
        ↓
목표 도달 판별 (수동으로 확인 필요)
```

### 3. NavMeshAgent의 주요 속성

```csharp
NavMeshAgent agent = GetComponent<NavMeshAgent>();

// 이동 관련
agent.speed = 5f;                    // 원하는 속도
agent.acceleration = 8f;             // 가속도
agent.angularSpeed = 120f;           // 회전 속도

// 충돌 회피
agent.radius = 0.5f;                 // 에이전트 반경
agent.height = 2f;                   // 에이전트 높이
agent.avoidancePriority = 50;        // 우선순위 (0=최고)

// 경로 추적
agent.stoppingDistance = 0.5f;       // 목표 도달 거리
agent.SetDestination(target);        // 목표 설정
agent.path;                          // 계산된 경로 확인

// 상태 확인
agent.hasPath;                       // 경로 존재 여부
agent.remainingDistance;             // 남은 거리
agent.pathPending;                   // 경로 계산 대기 중
agent.isOnNavMesh;                   // NavMesh 위치 확인
```

### 4. NavMesh Link와 OffMeshLink

#### NavMesh Link (경로 연결)
**두 NavMesh 영역을 연결**

```
영역 1          영역 2
┌────────┐    ┌────────┐
│ ░░░░░░ │    │ ░░░░░░ │
│ ░░░░░░ │←───→ ░░░░░░ │  NavMeshLink
│ ░░░░░░ │    │ ░░░░░░ │
└────────┘    └────────┘

계단으로 이어진 방들 사이 이동
```

#### OffMeshLink (특수 이동)
**보행이 아닌 특수 이동 (점프, 텔레포트 등)**

```
높은 곳                낮은 곳
  ■  ━━━━━→  ■

에이전트가 OffMeshLink를 따라
점프로 이동
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab07_NavMesh
```

### Step 2: 기본 환경 구성

#### 2-1. 바닥 생성
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (10, 1, 10)
```

#### 2-2. 벽 추가
```
Hierarchy → 3D Object → Cube
이름: Wall_1
Transform:
  - Position: (2, 1, 0)
  - Scale: (1, 2, 5)

같은 방식으로 3~4개의 벽 배치
```

#### 2-3. 카메라 설정
```
Main Camera 선택
Transform:
  - Position: (0, 10, -5)
  - Rotation: (45, 0, 0)
```

### Step 3: NavMesh Bake

#### 3-1. NavMesh Surface 추가
```
1. Hierarchy → 3D Object → NavMesh Surface
   (또는 GameObject → AI → NavMesh Surface)

이름: NavMesh

2. Inspector 설정:
   - Agent Type: Humanoid
   - Agent Radius: 0.5
   - Agent Height: 2
   - Max Slope: 45
   - Step Height: 0.3
```

#### 3-2. NavMesh Bake
```
1. NavMesh Surface 선택
2. Inspector → Bake 버튼 클릭
3. Scene 뷰에서 파란색 메시 확인
   (보행 가능 영역)
```

### Step 4: NavMeshAgent 생성

#### 4-1. 에이전트 오브젝트
```
Hierarchy → 3D Object → Capsule
이름: Agent

Transform:
  - Position: (-4, 0, 0)
  - Scale: (1, 1, 1)

Remove Capsule Collider (NavMeshAgent가 자체 콜라이더 사용)
```

#### 4-2. NavMeshAgent 컴포넌트 추가
```
Agent 선택
Add Component → Nav Mesh Agent

Inspector 설정:
  - Agent Type: Humanoid
  - Speed: 4
  - Angular Speed: 120
  - Acceleration: 8
  - Stopping Distance: 0.5
  - Auto Braking: ✓
  - Obstacle Avoidance: ✓
  - Avoidance Priority: 50
```

#### 4-3. 컨트롤러 스크립트 추가
```
Add Component → NavMeshAgentController

Inspector 설정:
  - Move Speed: 4
  - Rotate Speed: 5
```

#### 4-4. 프리팹 생성
```
Agent를 Assets/Prefabs/Agents/로 드래그
Hierarchy에서 Agent 삭제
```

### Step 5: 목표 표시 (선택)

```
Hierarchy → 3D Object → Sphere
이름: TargetMarker
Transform:
  - Position: (4, 0, 4)
  - Scale: (0.3, 0.3, 0.3)

Material: 초록색
```

### Step 6: 테스트

```
1. Play 버튼 클릭
2. Scene 뷰 또는 Game 뷰에서 마우스 클릭
3. 에이전트가 클릭 위치로 이동
4. 벽을 우회하여 이동하는지 확인
```

---

## 구현 체크리스트

### Phase 1: NavMesh 설정 (15분)

- [ ] 씬에 벽 및 장애물 배치
- [ ] NavMesh Surface 추가
- [ ] Bake 실행 (파란색 메시 생성)
- [ ] NavMesh 시각화 확인

### Phase 2: NavMeshAgent 구현 (15분)

- [ ] NavMeshAgentController.cs 작성
  - [ ] SetDestination으로 이동
  - [ ] 목표 도달 판별
  - [ ] 상태 확인 (pathPending, hasPath 등)
- [ ] 마우스 클릭으로 목표 설정

### Phase 3: 동작 확인 (10분)

- [ ] 에이전트가 목표로 이동하는가?
- [ ] 벽을 우회하는가?
- [ ] 목표에 도달하면 정지하는가?
- [ ] 다시 클릭하면 새로 이동하는가?

### Phase 4: 고급 기능 (보너스 - 5분)

- [ ] 여러 에이전트 배치
- [ ] 에이전트 간 충돌 회피
- [ ] PatrolBehavior 구현 (순찰)
- [ ] OffMeshLink 추가 (선택)

---

## 참고 자료

### 공식 문서

- **Unity NavMesh Documentation:**
  - https://docs.unity3d.com/Manual/nav-NavigationSystem.html
- **NavMeshAgent API:**
  - https://docs.unity3d.com/ScriptReference/AI.NavMeshAgent.html

### 추천 영상

- Unity Learn: "Introduction to the Navigation System"
- Brackeys: "NavMesh AI"

---

## 주요 개념 심화

### 1. NavMeshAgent 상태 관리

```csharp
public class NavMeshAgentController : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isMoving = false;

    void Update()
    {
        // 경로 계산 대기 중
        if (agent.pathPending)
        {
            Debug.Log("경로 계산 중...");
            return;
        }

        // NavMesh 위에 있는가?
        if (!agent.isOnNavMesh)
        {
            Debug.Log("NavMesh 위에 없음!");
            return;
        }

        // 목표에 도달했는가?
        if (!agent.hasPath)
        {
            isMoving = false;
            Debug.Log("경로 없음");
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                isMoving = false;
                Debug.Log("목표 도달!");
            }
        }
        else
        {
            isMoving = true;
        }
    }

    public void SetDestination(Vector3 target)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(target);
            isMoving = true;
        }
    }
}
```

### 2. 여러 에이전트의 우선순위

```csharp
// 에이전트 타입별 우선순위
// 0 = 최고 우선순위 (먼저 움직임)
// 31 = 최하 우선순위 (마지막에 움직임)

NavMeshAgent agent1 = player.GetComponent<NavMeshAgent>();
NavMeshAgent agent2 = enemy.GetComponent<NavMeshAgent>();

agent1.avoidancePriority = 10;  // 플레이어 (높은 우선순위)
agent2.avoidancePriority = 50;  // 적 (낮은 우선순위)
```

### 3. 동적 NavMesh (Carving)

**런타임에 NavMesh 수정**

```csharp
// 동적 장애물 추가
public class DynamicObstacle : MonoBehaviour
{
    void Start()
    {
        // NavMeshObstacle 컴포넌트 추가
        NavMeshObstacle obstacle = gameObject.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;  // 동적으로 NavMesh 파기
        obstacle.carvingRadius = 1f;
        obstacle.carvingTimeToStationary = 0.3f;
    }
}

// 장애물이 움직이면 자동으로 NavMesh 수정됨
```

### 4. OffMeshLink (특수 이동)

```csharp
// 점프로 이동하는 OffMeshLink 설정
public class JumpLink : MonoBehaviour
{
    void Start()
    {
        OffMeshLink link = gameObject.AddComponent<OffMeshLink>();
        link.startTransform = jumpStart;
        link.endTransform = jumpEnd;
        link.costOverride = -1f;  // 기본 비용 사용
    }

    // NavMeshAgent가 자동으로 점프 수행
    // 또는 사용자 정의 애니메이션 추가
}
```

---

## 트러블슈팅

### 문제 1: NavMesh가 생성되지 않음

**확인:**
```csharp
1. Ground가 Walkable Layer인가?
2. NavMesh Surface가 선택되어 있는가?
3. Bake 버튼을 눌렀는가?
4. Scene 뷰에서 파란색 메시가 보이는가?
```

### 문제 2: 에이전트가 NavMesh 밖으로 나간다

**원인:** 에이전트가 초기 위치에서 NavMesh 위에 없음

**해결책:**
```csharp
void Start()
{
    // NavMesh 위의 가장 가까운 점으로 이동
    NavMeshHit hit;
    if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
    {
        agent.Warp(hit.position);  // 강제 이동
    }
}
```

### 문제 3: 에이전트가 길을 찾지 못함

**원인:** 두 영역이 연결되어 있지 않음

**해결책:**
```csharp
1. NavMesh Surface 재 Bake
2. 영역 사이의 gaps 제거
3. NavMesh Link 수동 추가

// 또는 다른 Agent Type 시도
// Humanoid → Agent 또는 Large
```

### 문제 4: 충돌 회피가 작동하지 않음

**원인:** Obstacle Avoidance 비활성화

**해결책:**
```csharp
agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQuality;
// HighQuality (더 정확, 느림)
// MediumQuality
// LowQuality (빠름, 덜 정확)
```

---

## 확장 아이디어

### 아이디어 1: Patrol (순찰)

```csharp
public class PatrolBehavior : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3[] patrolPoints;
    private int currentPointIndex = 0;

    void Update()
    {
        // 현재 지점에 도달했는가?
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // 다음 지점으로 이동
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPointIndex]);
        }
    }
}
```

### 아이디어 2: Chase & Evade (추격과 도주)

```csharp
public class ChaseAndEvade : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform target;
    private float chaseDistance = 10f;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < chaseDistance)
        {
            // 추격
            agent.SetDestination(target.position);
        }
        else
        {
            // 도주
            Vector3 awayDirection = (transform.position - target.position).normalized;
            agent.SetDestination(transform.position + awayDirection * 20f);
        }
    }
}
```

### 아이디어 3: 커버 시스템 (Cover System)

```csharp
// 특정 위치를 덮개로 설정
// 위협 발생 시 가장 가까운 커버로 이동

public class CoverPoint : MonoBehaviour
{
    public bool isAvailable = true;
}

public class CoverSeeker : MonoBehaviour
{
    void FindCover(Vector3 threat)
    {
        CoverPoint[] covers = FindObjectsOfType<CoverPoint>();
        CoverPoint bestCover = null;
        float maxDistance = 0f;

        foreach (var cover in covers)
        {
            if (!cover.isAvailable) continue;

            float distanceFromThreat = Vector3.Distance(cover.position, threat);
            if (distanceFromThreat > maxDistance)
            {
                maxDistance = distanceFromThreat;
                bestCover = cover;
            }
        }

        if (bestCover != null)
        {
            agent.SetDestination(bestCover.position);
        }
    }
}
```

---

## 완료 체크리스트

- [ ] NavMesh Surface 추가 및 Bake
- [ ] NavMesh 시각화 확인
- [ ] NavMeshAgent 컴포넌트 추가
- [ ] NavMeshAgentController.cs 구현
- [ ] 마우스 클릭으로 이동 테스트
- [ ] 벽 우회 경로 확인
- [ ] 다중 에이전트 배치
- [ ] 에이전트 간 충돌 회피 확인
- [ ] PatrolBehavior 구현 (선택)
- [ ] OffMeshLink 추가 (선택)
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐ (중상)
**예상 완료 시간:** 45분
