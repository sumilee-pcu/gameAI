# Lab 2: FSM으로 순찰 AI 만들기

**소요 시간:** 60분
**연관 Part:** Part I - AI 기초와 에이전트
**이전 Lab:** Lab 1 - Unity 기초와 AI 프로젝트 설정

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **유한 상태 기계(FSM, Finite State Machine)의 개념** 이해
   - 상태(State)와 상태 전환(Transition)의 정의
   - FSM 프레임워크 설계 및 구현

2. **FSM 기반 AI 동작 구현**
   - Patrol(순찰) 상태: 정해진 경로를 순찰
   - Chase(추적) 상태: 플레이어 추적
   - Attack(공격) 상태: 플레이어 공격

3. **상태 전환 조건 설계**
   - 거리 기반 감지 시스템
   - 우선순위 기반 상태 전환 로직

4. **센서 시스템 기초**
   - VisionSensor(시각 센서): 시야각과 거리 기반 감지
   - 장애물과의 상호작용 (Raycast)

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   └── AI/
│       ├── FSM/
│       │   ├── State.cs                 # 상태의 추상 클래스
│       │   ├── StateMachine.cs          # 상태 기계 관리자
│       │   ├── PatrolState.cs           # 순찰 상태
│       │   ├── ChaseState.cs            # 추적 상태
│       │   └── AttackState.cs           # 공격 상태
│       ├── Sensors/
│       │   └── VisionSensor.cs          # 시각 센서
│       └── PatrolFSMAI.cs               # FSM 컨트롤러
├── Prefabs/
│   └── Agents/
│       └── Enemy.prefab                 # 적 에이전트 프리팹
├── Scenes/
│   └── Labs/
│       └── Lab02_PatrolFSM.unity        # 실습 씬
└── Materials/
    ├── RedMaterial.mat                  # 적 색상
    └── BlueMaterial.mat                 # 플레이어 색상
```

---

## 주요 스크립트 설명

### 1. State.cs - 상태의 추상 클래스

**역할:** 모든 AI 상태가 상속받는 기반 클래스

```csharp
public abstract class State
{
    protected GameObject agent;
    protected string stateName;

    // 상태 진입: 상태 시작 시 한 번 호출
    public virtual void OnEnter() { }

    // 매 프레임 실행: 상태 로직 업데이트
    public virtual void OnUpdate() { }

    // 물리 고정 시간 실행: 물리 기반 이동
    public virtual void OnFixedUpdate() { }

    // 상태 종료: 상태 전환 시 호출
    public virtual void OnExit() { }

    // 디버그 시각화
    public virtual void OnDrawGizmos() { }
}
```

**핵심 특징:**
- **생명주기 메서드**: OnEnter → OnUpdate → OnExit 순서로 실행
- **초기화와 정리**: OnEnter에서 상태별 필요한 변수 초기화, OnExit에서 정리
- **유연한 확장**: 하위 클래스에서 필요한 메서드만 오버라이드

### 2. StateMachine.cs - 상태 기계 관리자

**역할:** 상태 추가, 전환, 업데이트를 담당하는 핵심 엔진

```csharp
public class StateMachine
{
    private State currentState;
    private Dictionary<string, State> states;

    // 상태 추가
    public void AddState(string name, State state) { }

    // 초기 상태 설정
    public void SetInitialState(string name) { }

    // 상태 전환
    public void ChangeState(string newStateName) { }

    // 매 프레임 현재 상태 업데이트
    public void Update() { }

    // 현재 상태 이름 반환
    public string CurrentStateName => currentState?.stateName ?? "None";
}
```

**동작 흐름:**
```
AddState("Patrol", patrolState)
        ↓
SetInitialState("Patrol")
        ↓
patrolState.OnEnter() 호출
        ↓
매 Update 프레임마다 patrolState.OnUpdate() 호출
        ↓
ChangeState("Chase") 호출
        ↓
patrolState.OnExit() → chaseState.OnEnter() 호출
        ↓
chaseState.OnUpdate() 반복...
```

### 3. PatrolState.cs - 순찰 상태

**역할:** 정해진 경로를 따라 순찰하는 행동 구현

```csharp
public class PatrolState : State
{
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log("순찰 시작!");
    }

    public override void OnUpdate()
    {
        // 현재 목표 지점
        Transform target = waypoints[currentWaypointIndex];

        // 목표로 이동
        Vector3 direction = (target.position - agent.transform.position).normalized;
        agent.transform.position += direction * moveSpeed * Time.deltaTime;

        // 목표 도착 시 다음 지점으로
        if (Vector3.Distance(agent.transform.position, target.position) < 1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }
}
```

**시각화:**
- 시안색(Cyan) 선: 경로를 따라가는 루트
- 노란색 선: 현재 목표 지점으로의 방향

### 4. ChaseState.cs - 추적 상태

**역할:** 플레이어를 감지하면 빠르게 추적

```csharp
public class ChaseState : State
{
    private Transform target;
    private float chaseSpeedMultiplier = 1.5f; // 순찰보다 50% 빠름

    public override void OnUpdate()
    {
        // 플레이어 방향 계산
        Vector3 direction = (target.position - agent.transform.position).normalized;

        // 빠른 속도로 이동
        agent.transform.position += direction * moveSpeed * chaseSpeedMultiplier * Time.deltaTime;
    }
}
```

**특징:**
- Patrol보다 1.5배 빠른 속도
- 항상 플레이어를 향해 회전

### 5. AttackState.cs - 공격 상태

**역할:** 플레이어가 충분히 가까우면 공격 수행

```csharp
public class AttackState : State
{
    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    public override void OnUpdate()
    {
        // 공격 쿨다운 확인
        if (Time.time - lastAttackTime > attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    private void PerformAttack()
    {
        Debug.Log("공격 실행!");
        // 실제 게임에서는 여기서:
        // - 애니메이션 재생
        // - 데미지 계산
        // - 이펙트 생성
    }
}
```

### 6. VisionSensor.cs - 시각 센서

**역할:** 시야각과 거리를 고려하여 플레이어 감지

```csharp
public bool CanSeeTarget(Transform target)
{
    // 1. 거리 체크
    float distance = Vector3.Distance(transform.position, target.position);
    if (distance > visionRange) return false;

    // 2. 시야각 체크
    Vector3 directionToTarget = (target.position - transform.position).normalized;
    float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
    if (angleToTarget > visionAngle / 2f) return false;

    // 3. 장애물 체크 (Raycast)
    RaycastHit hit;
    if (Physics.Raycast(transform.position, directionToTarget, out hit, visionRange))
    {
        return hit.transform == target;
    }

    return false;
}
```

**감지 조건:**
1. 거리 ≤ 시야 범위
2. 각도 ≤ 시야각의 절반
3. 중간에 장애물이 없음

### 7. PatrolFSMAI.cs - FSM 컨트롤러

**역할:** 모든 상태를 관리하고 상태 전환 조건을 구현

```csharp
public class PatrolFSMAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] waypoints;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float loseTargetRange = 15f;

    private StateMachine stateMachine;

    void Start()
    {
        // 상태 기계 초기화
        stateMachine = new StateMachine();

        // 상태 생성 및 추가
        stateMachine.AddState("Patrol", new PatrolState(...));
        stateMachine.AddState("Chase", new ChaseState(...));
        stateMachine.AddState("Attack", new AttackState(...));

        // 초기 상태 설정
        stateMachine.SetInitialState("Patrol");
    }

    void Update()
    {
        // 상태 전환 조건 확인
        CheckStateTransitions();

        // 현재 상태 업데이트
        stateMachine.Update();
    }

    void CheckStateTransitions()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        string currentState = stateMachine.CurrentStateName;

        switch (currentState)
        {
            case "Patrol":
                if (distanceToPlayer < detectionRange)
                {
                    stateMachine.ChangeState("Chase");
                }
                break;

            case "Chase":
                if (distanceToPlayer < attackRange)
                {
                    stateMachine.ChangeState("Attack");
                }
                else if (distanceToPlayer > loseTargetRange)
                {
                    stateMachine.ChangeState("Patrol");
                }
                break;

            case "Attack":
                if (distanceToPlayer > attackRange)
                {
                    stateMachine.ChangeState("Chase");
                }
                break;
        }
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

1. **File → New Scene → Basic (Built-in) 선택**
2. **File → Save As → `Assets/Scenes/Labs/Lab02_PatrolFSM`**

### Step 2: 기본 환경 구성

#### 2-1. 바닥 생성
```
Hierarchy → 우클릭 → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (5, 1, 5)
Material: 흰색 (또는 기본)
```

#### 2-2. 플레이어(Player) 생성
```
3D Object → Capsule
이름: Player
Transform:
  - Position: (0, 1, 0)
  - Rotation: (0, 0, 0)
Add Component:
  - Capsule Collider (자동)
  - Rigidbody
    - Use Gravity: ✓
    - Constraints → Freeze Rotation: X, Z
  - PlayerControlledAgent (Lab 1에서 복사)
Inspector:
  - Move Speed: 5
  - Show Debug Info: ✓
Material: 파란색
```

#### 2-3. 적(Enemy) 생성
```
3D Object → Cube
이름: Enemy
Transform:
  - Position: (10, 0.5, 0)
Add Component:
  - Box Collider (자동)
  - Rigidbody
    - Use Gravity: ✓
    - Constraints → Freeze Rotation: X, Z
  - PatrolFSMAI
  - VisionSensor
Inspector (PatrolFSMAI):
  - Player: Player 오브젝트 드래그
  - Move Speed: 3
  - Detection Range: 10
  - Attack Range: 2
  - Lose Target Range: 15
Inspector (VisionSensor):
  - Vision Range: 10
  - Vision Angle: 120
  - Show Vision Cone: ✓
Material: 빨간색
```

### Step 3: 순찰 경로 설정

```
Hierarchy → 우클릭 → Create Empty
이름: Waypoints

Waypoints 하위에 4개의 Empty Object 생성:
  - Waypoint_0: Position (10, 0, 10)
  - Waypoint_1: Position (10, 0, -10)
  - Waypoint_2: Position (-10, 0, -10)
  - Waypoint_3: Position (-10, 0, 10)
```

### Step 4: Enemy 설정 완료

```
Enemy 선택 → PatrolFSMAI 컴포넌트
Waypoints 필드:
  - Size: 4
  - Element 0: Waypoint_0 드래그
  - Element 1: Waypoint_1 드래그
  - Element 2: Waypoint_2 드래그
  - Element 3: Waypoint_3 드래그
```

### Step 5: 카메라 설정

```
Main Camera 선택
Transform:
  - Position: (0, 15, -15)
  - Rotation: (30, 0, 0)
```

### Step 6: 디버그 UI 설정 (Lab 1에서 설정했다면 복사)

```
Hierarchy → Create → UI → Canvas
Canvas 하위 → UI → Text (Legacy) → 이름: DebugText
Canvas → Add Component → DebugUI
  - Debug Text: DebugText 드래그
```

---

## 테스트 방법

### 기본 테스트 (5분)

1. **Play 버튼 클릭**
2. **Enemy의 순찰 확인**
   - Enemy가 4개의 Waypoint를 따라 사각형 경로로 순찰하는가?
   - 경로 선(시안색)이 보이는가?

3. **플레이어 접근 테스트**
   - WASD 또는 화살표 키로 Player 조종
   - Enemy의 감지 범위(노란색 구) 내로 들어가기
   - Enemy가 Chase 상태로 전환되어 플레이어를 추적하는가?

4. **공격 범위 테스트**
   - 플레이어를 더 가까이 접근
   - Enemy가 2m 이내에서 Attack 상태로 전환되는가?
   - Console에 "공격 실행!" 메시지가 반복적으로 출력되는가?

### 고급 테스트 (10분)

#### 테스트 1: 상태 전환 확인
```
Console 창에서 다음 메시지 확인:
[FSM] Patrol 상태 진입
[FSM] Chase 상태 진입 (플레이어 감지 시)
[FSM] Attack 상태 진입 (공격 범위 내)
```

#### 테스트 2: 감지 범위 시각화 확인
```
Scene 뷰에서 다음을 확인:
- 노란색 구: 감지 범위 (10m)
- 빨간색 구: 공격 범위 (2m)
- 노란색 원뿔: 시야각 (120도)
```

#### 테스트 3: 장애물 회피 테스트
1. Hierarchy → 3D Object → Cube → Cover
   - Position: (5, 1, 0)
   - Scale: (2, 2, 0.3)
2. Enemy 뒤에 숨기
3. Enemy가 감지하지 못하는지 확인

#### 테스트 4: 추격 포기 테스트
1. Enemy가 Chase 상태일 때 플레이어를 15m 이상 멀리함
2. Enemy가 Patrol 상태로 복귀하는가?

### 디버그 체크리스트

| 항목 | 예상 동작 | 확인 |
|------|----------|------|
| 초기 상태 | Patrol | ✓ |
| 감지 범위 진입 | Chase로 전환 | ✓ |
| 공격 범위 진입 | Attack으로 전환 | ✓ |
| 거리 벗어남 | Patrol로 복귀 | ✓ |
| 시야각 제약 | 뒤에서는 감지 안 함 | ✓ |
| 장애물 감지 | 뒤에 숨으면 감지 안 함 | ✓ |
| 공격 쿨다운 | 1초마다 "공격 실행!" | ✓ |

---

## 주요 개념

### 1. 유한 상태 기계(FSM, Finite State Machine)

**정의:** AI가 취할 수 있는 모든 행동을 상태로 정의하고, 각 상태 간의 전환 규칙을 명시적으로 구현하는 패턴

**구조:**
```
┌─────────────┐
│   Patrol    │←─────────────────┐
└──────┬──────┘                  │
       │ (플레이어 감지)         │ (거리 > 15m)
       ↓                         │
┌─────────────┐           ┌──────┴──────┐
│    Chase    │──────────→│    Attack   │
└─────────────┘           └─────────────┘
       ↑                         │
       │ (공격 범위 진입)        │ (공격 범위 벗어남)
       └─────────────────────────┘
```

**장점:**
- 상태별 로직이 명확하고 독립적
- 새로운 상태 추가가 용이
- 상태 전환 조건을 시각화하기 쉬움

**단점:**
- 상태 수가 많아지면 복잡도 증가
- 계층적 상태 표현이 어려움 (Hierarchical FSM으로 해결 가능)

### 2. 상태(State)의 생명주기

```
OnEnter() ┐
  ↓       │ 상태 진행 중 매프레임 반복
OnUpdate()├─ 수십~수백 번 반복
  ↓       │
OnExit()  ┘
```

**예시:**
```csharp
public class PatrolState : State
{
    private bool isInitialized = false;

    public override void OnEnter()
    {
        // 상태 시작 시 한 번만 실행
        // - 애니메이션 시작
        // - 필요한 변수 초기화
        isInitialized = true;
        currentWaypointIndex = 0;
    }

    public override void OnUpdate()
    {
        // 매 프레임 실행
        // - 이동 로직
        // - 경로 업데이트
        MoveTowardWaypoint();
    }

    public override void OnExit()
    {
        // 상태 종료 시 한 번만 실행
        // - 애니메이션 정지
        // - 리소스 정리
        isInitialized = false;
    }
}
```

### 3. 상태 전환(State Transition) 조건

**원칙:**
- 명확한 조건 (거리, 값 비교 등)
- 우선순위 지정 (어떤 상태 전환을 먼저 확인할 것인가)
- 무한 루프 방지 (A→B→A 반복되지 않도록)

**이 Lab의 상태 전환:**
```csharp
switch (currentState)
{
    case "Patrol":
        // 감지 범위 내 + 플레이어 시야각 + 장애물 없음
        if (CanSeePlayer && distanceToPlayer < detectionRange)
        {
            ChangeState("Chase");
        }
        break;

    case "Chase":
        // 우선순위 1: 공격 범위 진입
        if (distanceToPlayer < attackRange)
        {
            ChangeState("Attack");
        }
        // 우선순위 2: 타겟 상실
        else if (distanceToPlayer > loseTargetRange)
        {
            ChangeState("Patrol");
        }
        break;

    case "Attack":
        // 공격 범위 벗어남
        if (distanceToPlayer > attackRange)
        {
            ChangeState("Chase");
        }
        break;
}
```

### 4. 센서(Sensor) 시스템 기초

**VisionSensor의 3가지 검사:**

```
┌─ 거리 검사 ─┐
│ distance    │ ≤ visionRange
└────────────┘

┌─ 시야각 검사 ┐
│  각도        │ ≤ visionAngle / 2
└───────────┘

┌─ 장애물 검사 ┐
│  Raycast     │ → 충돌 객체 == 플레이어?
└───────────┘
```

**시각화:**
```
     │
     │ 시야각 (120도)
    /│\
   / │ \
  /  │  \
-/───○───\- 에이전트
  \  │  /  (거리 10m 이내)
   \ │ /
    \│/
     │
```

---

## 트러블슈팅

### 문제 1: Enemy가 이동하지 않는다

**원인:**
- PatrolFSMAI에서 waypoints가 설정되지 않음
- SimpleAgent 또는 PlayerControlledAgent 스크립트가 Enemy에 붙어있음
- Rigidbody 설정이 잘못됨

**해결책:**
```
1. Enemy 선택 → Inspector 확인
2. PatrolFSMAI 컴포넌트의 Waypoints 필드가 비어있는가?
   → Size를 4로 설정하고 Waypoint_0~3 드래그
3. Rigidbody 확인:
   - Body Type: Dynamic
   - Constraints → Freeze Rotation X, Z 체크
4. Console에서 에러 메시지 확인
```

### 문제 2: Enemy가 플레이어를 감지하지 못한다

**원인:**
- VisionSensor가 추가되지 않음
- Vision Range 설정이 너무 작음
- 플레이어가 Enemy의 시야 밖에 있음
- 장애물이 시야를 막고 있음

**해결책:**
```
1. Enemy 선택 → Add Component → Vision Sensor
2. Vision Range: 10 (충분히 큼)
3. Vision Angle: 120 (넓은 시야각)
4. Scene 뷰에서 노란색 원뿔이 보이는가?
5. 원뿔 범위 내에서 플레이어를 움직여보기
6. 장애물이 있으면 제거하거나 이동
```

### 문제 3: 상태 전환이 너무 빈번하게 발생한다

**원인:**
- 상태 전환 조건이 경계값(Boundary Value)에서 떨리는 현상
- 예: distanceToPlayer가 10.0 ↔ 10.1 반복

**해결책:**
```csharp
// 나쁜 예: 경계값이 명확하지 않음
if (distanceToPlayer < detectionRange)

// 좋은 예: 히스테리시스(Hysteresis) 적용
const float DETECTION_DISTANCE = 10f;
const float LOSE_TARGET_DISTANCE = 15f; // 더 넓은 범위

if (distanceToPlayer < DETECTION_DISTANCE)
{
    ChangeState("Chase");
}
else if (distanceToPlayer > LOSE_TARGET_DISTANCE)
{
    ChangeState("Patrol");
}
// 10 ~ 15 범위에서는 상태 유지
```

### 문제 4: Console에 여러 "상태 진입" 메시지가 출력된다

**원인:**
- Update에서 매 프레임 ChangeState()가 호출됨
- OnEnter가 여러 번 호출됨

**해결책:**
```csharp
// StateMachine.ChangeState() 개선
public void ChangeState(string newStateName)
{
    // 이미 해당 상태인가?
    if (currentState?.stateName == newStateName)
        return; // 전환 생략

    currentState?.OnExit();
    currentState = states[newStateName];
    currentState.OnEnter();
}
```

### 문제 5: 플레이어가 Camera 밖으로 나간다

**원인:**
- 카메라 설정이 플레이어를 따라가지 못함

**해결책:**
```
1. Main Camera 선택
2. Add Component → Cinemachine → Virtual Camera (권장)
   또는
3. 카메라를 PlayerControlledAgent의 자식으로 설정
   (Position: 0, 15, -10 / Rotation: 30, 0, 0)
```

---

## 확장 아이디어

### 아이디어 1: Idle 상태 추가

순찰하지 않고 한 지점에서 대기하는 상태

```csharp
public class IdleState : State
{
    private float idleTimer = 0f;
    private float idleDuration = 3f;

    public override void OnEnter()
    {
        base.OnEnter();
        idleTimer = 0f;
    }

    public override void OnUpdate()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer > idleDuration)
        {
            // 다음 상태로 (PatrolFSMAI에서 처리)
            Debug.Log("Idle 완료");
        }
    }
}
```

**상태 추가:**
```
stateMachine.AddState("Idle", new IdleState());

// Patrol → Idle → Chase 등으로 조정
```

### 아이디어 2: 소리(청각) 감지 추가

Lab 3에서 배울 청각 센서를 미리 구현

```csharp
[Header("Hearing")]
public float hearingRange = 15f;
public float minimumNoiseLevel = 30f;

void Update()
{
    // ... 기존 코드 ...

    // 플레이어가 움직이면 소리 발생
    if (player.GetComponent<Rigidbody>().velocity.magnitude > 0)
    {
        float noiseLevel = 50f;
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < hearingRange && noiseLevel > minimumNoiseLevel)
        {
            // Chase 상태로 전환
        }
    }
}
```

### 아이디어 3: 순찰 경로 동적 생성

게임 시작 시 난수로 waypoint를 생성

```csharp
public class DynamicPatrol : PatrolFSMAI
{
    public int waypointCount = 6;
    public float patrolRadius = 20f;

    void GenerateWaypoints()
    {
        waypoints = new Transform[waypointCount];

        for (int i = 0; i < waypointCount; i++)
        {
            float angle = (360f / waypointCount) * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * patrolRadius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * patrolRadius;

            GameObject wp = new GameObject($"Waypoint_{i}");
            wp.transform.position = new Vector3(x, 0, z);
            waypoints[i] = wp.transform;
        }
    }
}
```

### 아이디어 4: Enemy 다중화

같은 방식으로 여러 Enemy 생성

```csharp
[SerializeField] private int enemyCount = 3;
[SerializeField] private GameObject enemyPrefab;

void Start()
{
    for (int i = 0; i < enemyCount; i++)
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-15f, 15f),
            0.5f,
            Random.Range(-15f, 15f)
        );

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
```

### 아이디어 5: Patrol 경로 시각화 개선

경로를 3D 라인으로 더 선명하게 표시

```csharp
void OnDrawGizmos()
{
    if (waypoints == null || waypoints.Length < 2) return;

    Gizmos.color = Color.cyan;

    for (int i = 0; i < waypoints.Length; i++)
    {
        int nextIndex = (i + 1) % waypoints.Length;
        Gizmos.DrawLine(
            waypoints[i].position + Vector3.up * 0.5f,
            waypoints[nextIndex].position + Vector3.up * 0.5f
        );

        // 번호 표시
        Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
    }
}
```

---

## 다음 단계

### Lab 3으로 진행하기 전 준비

1. **현재 Lab 완료 확인**
   - ✅ FSM 구조 이해
   - ✅ PatrolFSMAI와 3가지 상태 구현
   - ✅ VisionSensor로 시야각 기반 감지
   - ✅ 모든 상태 전환이 정상 작동

2. **코드 리뷰**
   - StateMachine이 올바르게 상태를 관리하는가?
   - 각 State의 OnEnter, OnUpdate, OnExit이 제대로 호출되는가?
   - VisionSensor의 3가지 검사(거리, 각도, 장애물)가 모두 작동하는가?

3. **파일 정리**
   - Lab02_PatrolFSM 씬 저장 확인
   - 모든 스크립트가 Assets/Scripts/AI/ 폴더에 있는가?
   - Prefab 생성 (선택): Enemy를 Prefab으로 변환

### Lab 3에서 배울 내용

- **청각 센서(HearingSensor):** 거리 감쇠를 고려한 소리 감지
- **이벤트 시스템(EventSystem):** 플레이어 움직임 → 소음 발생 → Enemy 감지
- **Investigate 상태:** 의심스러운 소음 위치로 이동
- **다중 센서 통합:** 시각 + 청각 센서를 함께 사용하는 AI

### Lab 4 미리보기

- **Boids 군집 알고리즘:** 50~200개의 에이전트가 동시에 움직이는 시뮬레이션
- **Separation/Alignment/Cohesion:** 3가지 군집 규칙
- **성능 최적화:** O(N²) → O(N) 복잡도 개선
- **장애물 회피:** Raycast를 사용한 동적 회피

---

## 참고 자료

### 관련 교재 챕터

- **Part I - 3장: FSM 패턴**
  - 상태 설계 원칙
  - 상태 전환 표(State Transition Table)
  - 계층적 상태 기계(Hierarchical FSM)

- **Part II - 1장: 환경 인식과 센서**
  - VisionSensor 구현 방식
  - Raycast와 Physics.Raycast
  - 장애물과의 상호작용

### 외부 링크

- [Unity Physics.Raycast 공식 문서](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html)
- [유한 상태 기계(FSM) 위키](https://en.wikipedia.org/wiki/Finite-state_machine)
- [Game Programming Patterns - State](https://gameprogrammingpatterns.com/state.html)

### 추천 학습 순서

1. ✅ **Lab 1:** 기본 이동과 입력 처리
2. ✅ **Lab 2:** FSM과 상태 전환 (현재)
3. 📚 **Lab 3:** 센서 다양화 (청각, 조사)
4. 📚 **Lab 4:** 군집 시뮬레이션
5. 📚 **Lab 5:** Steering Behaviors
6. ... (총 13개 Lab)

---

## 완료 체크리스트

실습을 완료했으면 다음을 확인하세요:

- [ ] FSM 프레임워크 이해 (State, StateMachine)
- [ ] PatrolState, ChaseState, AttackState 구현
- [ ] PatrolFSMAI 컨트롤러 작동 확인
- [ ] VisionSensor 시야각 시각화 확인
- [ ] 상태 전환이 정상 작동
  - [ ] Patrol → Chase (감지 범위 진입)
  - [ ] Chase → Attack (공격 범위 진입)
  - [ ] Attack → Chase (공격 범위 벗어남)
  - [ ] Chase/Attack → Patrol (타겟 상실)
- [ ] 장애물 뒤에 숨으면 감지되지 않음 확인
- [ ] Console 메시지가 명확함
- [ ] Scene 뷰 시각화 확인
- [ ] 씬 저장

**축하합니다!** Lab 2를 완료했습니다. 🎉

---

## FAQ

**Q1: PatrolFSMAI와 State의 관계는?**

A: PatrolFSMAI는 "컨트롤러" 역할이고, State들은 "행동"입니다. PatrolFSMAI가 상태를 관리하고, 각 State 객체가 그 상태에서의 구체적인 행동을 정의합니다.

**Q2: OnUpdate() vs OnFixedUpdate() 차이?**

A: OnUpdate()는 게임 로직(방향 결정)용이고, OnFixedUpdate()는 물리 기반 이동용입니다. 이 Lab에서는 PatrolState 등에서 직접 transform.position을 변경하므로 OnUpdate()를 사용합니다.

**Q3: 상태가 매우 많으면 어떻게 하나?**

A: Hierarchical FSM (계층적 상태 기계)을 사용하거나, Behavior Tree로 변경하는 것을 검토하세요. Lab 8에서 다룹니다.

**Q4: AI가 일관되게 같은 경로를 순찰하는 것이 지루한데?**

A: 아이디어 3 (동적 경로 생성)이나 아이디어 1 (Idle 상태 추가)을 활용하세요.

---

**작성일:** 2024년 1월
**업데이트:** 2024년 1월
**난이도:** ⭐⭐⭐ (중상)
**예상 완료 시간:** 60분
