# Lab 3: 레이캐스트 센서 구현

**소요 시간:** 45분
**연관 Part:** Part II - 환경 인식과 불확실성
**이전 Lab:** Lab 2 - FSM으로 순찰 AI 만들기

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **청각 센서(HearingSensor) 구현**
   - 거리 감쇠를 고려한 소음 감지
   - 최소 감지 레벨 설정
   - 이벤트 기반 소음 처리

2. **이벤트 시스템 구축**
   - C# 이벤트(Event)와 대리자(Delegate) 활용
   - 플레이어 움직임 → 소음 발생 → Enemy 감지
   - 느슨한 결합(Loose Coupling) 설계

3. **Investigate 상태 추가**
   - 의심스러운 소음 위치로 이동
   - 조사 완료 판정
   - 조사 중 플레이어 발견 시 추적

4. **다중 센서 통합**
   - 시각 센서(VisionSensor)와 청각 센서(HearingSensor) 함께 사용
   - 센서 우선순위 설정
   - 더 정교한 AI 의사결정

---

## 파일 구조

실습을 완료하면 다음과 같은 파일이 추가됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   ├── FSM/
│   │   │   ├── InvestigateState.cs     # 조사 상태 (NEW)
│   │   │   └── (기존 State들...)
│   │   ├── Sensors/
│   │   │   ├── VisionSensor.cs         # (Lab 2에서)
│   │   │   └── HearingSensor.cs        # 청각 센서 (NEW)
│   │   └── PatrolFSMAI.cs              # (수정: 청각 통합)
│   └── Utilities/
│       └── NoiseEvent.cs               # 소음 이벤트 관리자 (NEW)
└── Scenes/
    └── Labs/
        └── Lab03_RaycastSensor.unity   # 실습 씬 (NEW)
```

---

## 주요 스크립트 설명

### 1. HearingSensor.cs - 청각 센서

**역할:** 거리 감쇠를 고려하여 소음을 감지하는 센서

```csharp
public class HearingSensor : MonoBehaviour
{
    [Header("Hearing Settings")]
    public float hearingRange = 15f;           // 청각 범위 (m)
    public float minimumNoiseLevel = 10f;      // 감지 최소 소음 크기

    /// <summary>
    /// 소음을 감지할 수 있는지 확인
    /// </summary>
    public bool CanHear(Vector3 noisePosition, float noiseLevel)
    {
        float distance = Vector3.Distance(transform.position, noisePosition);

        // 거리 감쇠 적용
        // 거리가 멀수록 소음이 작아짐
        float attenuatedNoise = noiseLevel * (1f - (distance / hearingRange));

        // 최소 소음 레벨 이상이면 감지
        return attenuatedNoise >= minimumNoiseLevel && distance <= hearingRange;
    }
}
```

**동작 원리:**

```
노이즈 크기 100
거리 0m:  감지됨 (100 * (1 - 0/15) = 100)
거리 7.5m: 감지됨 (100 * (1 - 7.5/15) = 50)
거리 15m: 감지 안 됨 (100 * (1 - 15/15) = 0)

예시: minimumNoiseLevel = 10인 경우
필요한 최소 소음 크기 = 10 / (1 - distance/hearingRange)
```

**거리 감쇠 그래프:**

```
소음 크기
│
100 ├─────●
    │    / \
 50 ├───/   \●
    │  /     \
  0 ├─/───────\────→ 거리
    0         15m
```

### 2. NoiseEvent.cs - 소음 이벤트 관리자

**역할:** 게임 전역에서 소음을 발생시키고, 리스너들에게 알림

```csharp
public class NoiseEvent : MonoBehaviour
{
    public static NoiseEvent Instance { get; private set; }

    // 소음 발생 이벤트
    public static event Action<Vector3, float> OnNoiseMade;

    /// <summary>
    /// 소음 발생 (어디서든 호출 가능)
    /// </summary>
    public static void MakeNoise(Vector3 position, float noiseLevel)
    {
        OnNoiseMade?.Invoke(position, noiseLevel);
        Debug.Log($"소음 발생: 위치 {position}, 레벨 {noiseLevel}");
    }
}
```

**사용 방식:**

```csharp
// 1. 리스너 등록 (Start에서)
NoiseEvent.OnNoiseMade += OnNoiseHeard;

// 2. 리스너 함수 정의
void OnNoiseHeard(Vector3 noisePosition, float noiseLevel)
{
    if (hearingSensor.CanHear(noisePosition, noiseLevel))
    {
        // 소음 감지됨! → 조사 상태로 전환
    }
}

// 3. 소음 발생 (게임의 어디서든)
NoiseEvent.MakeNoise(transform.position, 50f);

// 4. 리스너 등록 해제 (OnDestroy에서)
NoiseEvent.OnNoiseMade -= OnNoiseHeard;
```

**이벤트의 장점:**
- **느슨한 결합:** 플레이어와 Enemy가 직접 참조하지 않음
- **확장성:** 새로운 청취자 추가가 쉬움 (다른 Enemy 등)
- **명확함:** "소음이 발생했다"는 메시지 하나로 통신

### 3. InvestigateState.cs - 조사 상태

**역할:** 의심스러운 소음 위치로 이동하여 조사

```csharp
public class InvestigateState : State
{
    private Vector3 investigationPoint;
    private float moveSpeed;
    private float arrivalDistance = 1.5f;
    private bool hasArrived = false;
    private float investigationDuration = 3f;  // 조사 지속 시간
    private float investigationTimer = 0f;

    public InvestigateState(GameObject agent, float moveSpeed)
        : base(agent, "Investigate")
    {
        this.moveSpeed = moveSpeed;
    }

    /// <summary>
    /// 조사할 위치 설정 (PatrolFSMAI에서 호출)
    /// </summary>
    public void SetInvestigationPoint(Vector3 point)
    {
        investigationPoint = point;
        hasArrived = false;
        investigationTimer = 0f;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log($"[Investigate] 소음 위치로 이동: {investigationPoint}");
    }

    public override void OnUpdate()
    {
        if (!hasArrived)
        {
            // 조사 지점으로 이동
            Vector3 direction = (investigationPoint - agent.transform.position).normalized;
            float distance = Vector3.Distance(agent.transform.position, investigationPoint);

            if (distance > arrivalDistance)
            {
                // 이동
                agent.transform.position += direction * moveSpeed * Time.deltaTime;

                // 회전
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    agent.transform.rotation = Quaternion.Slerp(
                        agent.transform.rotation,
                        targetRotation,
                        5f * Time.deltaTime
                    );
                }
            }
            else
            {
                // 도착!
                hasArrived = true;
                Debug.Log("[Investigate] 조사 지점 도착");
            }
        }
        else
        {
            // 조사 중 (대기)
            investigationTimer += Time.deltaTime;
            if (investigationTimer >= investigationDuration)
            {
                Debug.Log("[Investigate] 조사 완료");
            }
        }
    }

    public override void OnDrawGizmos()
    {
        // 조사 위치 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(agent.transform.position, investigationPoint);
        Gizmos.DrawWireSphere(investigationPoint, arrivalDistance);
    }

    /// <summary>
    /// 조사 완료 여부
    /// </summary>
    public bool HasFinishedInvestigation()
    {
        return hasArrived && investigationTimer >= investigationDuration;
    }
}
```

**상태 흐름:**

```
Investigate 상태 진입
    ↓
SetInvestigationPoint() 호출 (소음 위치 설정)
    ↓
매 프레임 OnUpdate():
  - 아직 도착하지 않음: 목표로 이동
  - 도착: 조사 대기 (3초)
    ↓
HasFinishedInvestigation() = true
    ↓
PatrolFSMAI가 상태 전환 (Patrol 또는 Chase)
```

### 4. PatrolFSMAI.cs - 수정: 청각 센서 통합

**추가되는 부분:**

```csharp
public class PatrolFSMAI : MonoBehaviour
{
    private HearingSensor hearingSensor;
    private InvestigateState investigateState;
    private Vector3 lastHeardNoisePosition;

    void Start()
    {
        // ... 기존 코드 ...

        // 센서 초기화
        hearingSensor = GetComponent<HearingSensor>();

        // InvestigateState 추가
        investigateState = new InvestigateState(gameObject, moveSpeed);
        stateMachine.AddState("Investigate", investigateState);

        // 소음 이벤트 구독
        NoiseEvent.OnNoiseMade += OnNoiseHeard;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 (중요!)
        NoiseEvent.OnNoiseMade -= OnNoiseHeard;
    }

    /// <summary>
    /// 소음 감지 콜백
    /// </summary>
    void OnNoiseHeard(Vector3 noisePosition, float noiseLevel)
    {
        if (hearingSensor != null && hearingSensor.CanHear(noisePosition, noiseLevel))
        {
            lastHeardNoisePosition = noisePosition;

            Debug.Log($"[HearingSensor] 소음 감지: {noisePosition}, 레벨 {noiseLevel}");

            // 순찰 중이면 조사 상태로 전환
            if (stateMachine.CurrentStateName == "Patrol")
            {
                investigateState.SetInvestigationPoint(noisePosition);
                stateMachine.ChangeState("Investigate");
            }
            // 이미 추적 중이면 추적 계속
            // 이미 공격 중이면 공격 계속
        }
    }

    void CheckStateTransitions()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        string currentState = stateMachine.CurrentStateName;

        VisionSensor vision = GetComponent<VisionSensor>();
        bool canSeePlayer = vision != null
            ? vision.CanSeeTarget(player)
            : distanceToPlayer < detectionRange;

        switch (currentState)
        {
            case "Patrol":
                if (canSeePlayer)
                {
                    stateMachine.ChangeState("Chase");
                }
                break;

            case "Investigate":
                // 조사 중 플레이어 발견
                if (canSeePlayer)
                {
                    stateMachine.ChangeState("Chase");
                }
                // 조사 완료
                else if (investigateState.HasFinishedInvestigation())
                {
                    stateMachine.ChangeState("Patrol");
                }
                break;

            case "Chase":
                if (distanceToPlayer < attackRange)
                {
                    stateMachine.ChangeState("Attack");
                }
                else if (!canSeePlayer && distanceToPlayer > loseTargetRange)
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

### 5. PlayerControlledAgent.cs - 수정: 소음 발생

**추가되는 부분:**

```csharp
public class PlayerControlledAgent : SimpleAgent
{
    [Header("Noise Settings")]
    public float walkingNoiseLevel = 40f;      // 걸을 때 소음
    public float runningNoiseLevel = 80f;      // 달릴 때 소음
    public float noiseEmissionInterval = 0.5f; // 소음 발생 간격 (초)

    private float lastNoiseTime = 0f;

    protected override void UpdateAI()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            inputDirection.Normalize();
            velocity = inputDirection * moveSpeed;

            // 이동 중 소음 발생
            if (Time.time - lastNoiseTime > noiseEmissionInterval)
            {
                NoiseEvent.MakeNoise(transform.position, walkingNoiseLevel);
                lastNoiseTime = Time.time;
            }
        }
        else
        {
            velocity = Vector3.zero;
        }

        // Space키로 큰 소음 (점프)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NoiseEvent.MakeNoise(transform.position, runningNoiseLevel);
        }
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: Lab 2 씬 복사

Lab 2에서 만든 Lab02_PatrolFSM 씬을 기반으로 새 씬 생성:

```
1. Assets/Scenes/Labs/Lab02_PatrolFSM 선택
2. Ctrl+D (또는 우클릭 → Duplicate)
3. 이름을 Lab03_RaycastSensor로 변경
```

### Step 2: Enemy에 청각 센서 추가

```
Enemy 선택 → Add Component → Hearing Sensor

Inspector 설정:
  - Hearing Range: 15
  - Minimum Noise Level: 10
  - Show Hearing Range: ✓
```

### Step 3: 전역 소음 이벤트 관리자 생성

```
Hierarchy → Create Empty → 이름: NoiseEventManager

Add Component → Noise Event

(Manager의 구체적인 설정은 불필요)
```

### Step 4: Player에 소음 생성 기능 추가

```
Player 선택 → PlayerControlledAgent 컴포넌트 수정

PlayerControlledAgent.cs를 위의 코드로 업데이트
```

### Step 5: Investigate State 활성화

```
PatrolFSMAI.cs를 위의 코드로 업데이트
```

### Step 6: 장애물 추가 (선택)

```
3D Object → Cube → Cover1
  - Position: (5, 1, 0)
  - Scale: (2, 2, 0.3)

3D Object → Cylinder → Obstacle1
  - Position: (-5, 2, 0)
  - Scale: (1, 2, 1)
```

---

## 테스트 방법

### 기본 테스트 (15분)

#### 테스트 1: 청각 센서 기본 동작
```
1. Play 버튼 클릭
2. Player를 움직이지 않고 유지
3. Scene 뷰에서 Enemy 주변의 시안색 구(청각 범위) 확인
4. Enemy가 Patrol 상태 유지 (소음 없음)
```

#### 테스트 2: 소음 감지 및 Investigate 상태 전환
```
1. Play 상태 계속 유지
2. Player를 WASD로 움직이기
   - Enemy의 청각 범위(15m) 내에 있어야 함
3. Enemy가 소음을 감지하는가?
   - Console: "[HearingSensor] 소음 감지: ..." 메시지
   - State: "Investigate" 상태로 전환
4. Enemy가 Player의 위치로 이동하는가?
5. Enemy가 도착 후 대기하는가? (3초)
6. 조사 완료 후 Patrol로 복귀하는가?
```

#### 테스트 3: 청각 범위 밖에서는 소음을 들을 수 없음
```
1. Play 버튼
2. Player를 Enemy에서 20m 이상 멀리 배치
3. Player 움직이기
4. Enemy가 Patrol 상태 유지 (소음 무시)
```

### 고급 테스트 (20분)

#### 테스트 4: 조사 중 플레이어 발견
```
1. Play 상태
2. Player를 소음으로 Enemy를 조사 상태로 유도
3. Enemy가 조사 위치로 이동 중
4. Player를 Enemy의 시야 범위 내로 이동
5. Enemy가 즉시 Chase 상태로 전환하는가?
6. Investigate 대기 시간 무시하는가?
```

#### 테스트 5: 거리 감쇠 확인
```
변수 설정:
  - noiseLevel: 100
  - hearingRange: 15
  - minimumNoiseLevel: 10

거리별 감지 여부:
  - 거리 0m: 100 * (1-0/15) = 100 > 10 → 감지 ✓
  - 거리 7.5m: 100 * (1-7.5/15) = 50 > 10 → 감지 ✓
  - 거리 12.5m: 100 * (1-12.5/15) = 16.67 > 10 → 감지 ✓
  - 거리 13.5m: 100 * (1-13.5/15) = 10 = 10 → 감지 △
  - 거리 14m: 100 * (1-14/15) = 6.67 < 10 → 감지 ✗

검증:
1. Player를 거리별로 배치
2. 각 거리에서 움직이기
3. 위의 감지 여부와 일치하는지 확인
```

#### 테스트 6: 시각 + 청각 센서 우선순위
```
상황: Enemy가 Patrol 중, Player가 근처

시나리오 1 - 청각 먼저:
  1. Player를 소음이 들리는 거리에서 움직이기
  2. Enemy: Patrol → Investigate
  3. 소음 위치로 이동

시나리오 2 - 시각이 우선 (시각 > 청각):
  1. Enemy의 시야각 내로 Player 이동
  2. Enemy: Patrol → Chase (Investigate 스킵)
  3. 플레이어 추적

결론: 시각이 먼저 감지되면 청각은 무시됨
```

### 디버그 체크리스트

| 항목 | 예상 동작 | 콘솔 메시지 | 확인 |
|------|----------|-----------|------|
| Player 움직임 | 소음 발생 | "소음 발생: ..." | ✓ |
| Enemy 청각 | 소음 감지 | "[HearingSensor] 소음 감지: ..." | ✓ |
| 상태 전환 | Patrol → Investigate | "[FSM] Investigate 상태 진입" | ✓ |
| 목표 위치 | 소음 위치로 이동 | "[Investigate] 소음 위치로 이동: ..." | ✓ |
| 도착 확인 | 목표 지점 도착 | "[Investigate] 조사 지점 도착" | ✓ |
| 조사 완료 | 3초 대기 후 복귀 | "[Investigate] 조사 완료" | ✓ |
| 시각 우선 | 조사 중 플레이어 발견 | "[FSM] Chase 상태 진입" | ✓ |

---

## 주요 개념

### 1. 거리 감쇠(Distance Attenuation)

**정의:** 거리가 멀어질수록 신호(소음)가 약해지는 물리적 현상

**수식:**
```
감지된 소음 크기 = 원본 소음 * (1 - 거리 / 최대범위)
```

**예시:**
```
원본 소음 = 100
최대 청각 범위 = 15m

거리 0m: 100 * (1 - 0/15) = 100  (100% 감지)
거리 3m: 100 * (1 - 3/15) = 80   (80% 감지)
거리 7m: 100 * (1 - 7/15) = 53   (53% 감지)
거리 12m: 100 * (1 - 12/15) = 20 (20% 감지)
거리 15m: 100 * (1 - 15/15) = 0  (0% 감지)
```

**최소 감지 레벨:**
```
minimumNoiseLevel = 10인 경우:

감지 조건: attenuatedNoise >= 10
필요한 거리 = 원본 소음에 따라 달라짐

원본 소음 50인 경우:
50 * (1 - d/15) >= 10
(1 - d/15) >= 0.2
d/15 <= 0.8
d <= 12m (최대 감지 거리)
```

### 2. 이벤트 기반 통신

**패턴:**

```
Publisher (발행자)       Event         Subscriber (구독자)
┌──────────────────┐    ┌─────────┐   ┌──────────────────┐
│  PlayerControl   │───→│OnNoise  │───→│  Enemy AI        │
│  MakeNoise()     │    │Made     │   │  OnNoiseHeard()  │
└──────────────────┘    └─────────┘   └──────────────────┘
                             ↓
                        ┌──────────────┐
                        │ Other Listern│
                        │ (예: 경보음)  │
                        └──────────────┘
```

**장점:**
1. **느슨한 결합:** Publisher가 Subscriber를 모름
2. **확장성:** Subscriber 추가/제거가 자유로움
3. **재사용성:** 같은 이벤트를 여러 리스너가 처리

**코드:**

```csharp
// Step 1: 이벤트 선언 (NoiseEvent.cs)
public static event Action<Vector3, float> OnNoiseMade;

// Step 2: 이벤트 발행 (PlayerControlledAgent.cs)
NoiseEvent.MakeNoise(transform.position, 50f);

// Step 3: 리스너 등록 (PatrolFSMAI.cs의 Start)
NoiseEvent.OnNoiseMade += OnNoiseHeard;

// Step 4: 리스너 함수 정의
void OnNoiseHeard(Vector3 noisePosition, float noiseLevel)
{
    // 소음 처리
}

// Step 5: 리스너 등록 해제 (PatrolFSMAI.cs의 OnDestroy)
NoiseEvent.OnNoiseMade -= OnNoiseHeard;
```

### 3. 상태 전환 우선순위

이 Lab의 상태 전환 로직:

```
시각 센서          청각 센서         결과
  (높음)            (낮음)
─────────────────────────────────────────
감지               감지      →  Chase (시각 우선)
감지               미감지    →  Chase (시각 우선)
미감지             감지      →  Investigate (청각)
미감지             미감지    →  Patrol (대기)
```

**구현:**

```csharp
switch (currentState)
{
    case "Patrol":
        // 우선순위 1: 시각 센서
        if (canSeePlayer)
        {
            stateMachine.ChangeState("Chase");
        }
        break;

    case "Investigate":
        // 우선순위 1: 시각 센서 (조사 중도 상관없음)
        if (canSeePlayer)
        {
            stateMachine.ChangeState("Chase");
        }
        // 우선순위 2: 조사 완료
        else if (investigateState.HasFinishedInvestigation())
        {
            stateMachine.ChangeState("Patrol");
        }
        break;
}
```

### 4. 다중 센서 통합

**센서 체인:**

```
플레이어 움직임
    ↓
소음 발생 (NoiseEvent.MakeNoise)
    ↓
청각 센서 감지 (HearingSensor.CanHear)
    ↓
Investigate 상태 전환
    ↓
소음 위치로 이동
    ↓
시각 센서 감지 (VisionSensor.CanSeeTarget)
    ↓
Chase 상태로 전환
```

---

## 트러블슈팅

### 문제 1: Enemy가 소음을 감지하지 못한다

**원인 및 해결책:**

```
1. NoiseEventManager가 없는가?
   → Hierarchy에 NoiseEventManager 생성
   → Add Component → Noise Event

2. HearingSensor가 추가되지 않았는가?
   → Enemy 선택 → Add Component → Hearing Sensor

3. PlayerControlledAgent에서 소음이 발생하지 않는가?
   → Console에서 "소음 발생: ..." 메시지 확인
   → PlayerControlledAgent 코드 재확인

4. 거리가 청각 범위 밖인가?
   → Hearing Range를 더 크게 설정 (20m)
   → Player를 더 가깝게 이동

5. OnDestroy에서 이벤트 구독 해제를 안 했는가?
   → PatrolFSMAI의 OnDestroy 코드 추가:
   ```csharp
   void OnDestroy()
   {
       NoiseEvent.OnNoiseMade -= OnNoiseHeard;
   }
   ```
```

### 문제 2: Enemy가 계속 같은 위치를 조사한다

**원인:** investigateState의 HasFinishedInvestigation()이 제대로 작동하지 않음

**해결책:**

```csharp
// InvestigateState.cs 확인
public bool HasFinishedInvestigation()
{
    return hasArrived && investigationTimer >= investigationDuration;
}

// Debug 추가
public override void OnUpdate()
{
    // ...
    if (hasArrived)
    {
        investigationTimer += Time.deltaTime;
        Debug.Log($"[Investigate] 타이머: {investigationTimer:F2}s / {investigationDuration}s");
    }
}
```

### 문제 3: 조사 중 플레이어가 발견되어도 반응하지 않는다

**원인:** CheckStateTransitions()에서 Investigate 케이스가 없거나 잘못됨

**해결책:**

```csharp
void CheckStateTransitions()
{
    // ... 기존 코드 ...

    switch (currentState)
    {
        // ... 다른 케이스 ...

        case "Investigate":  // 이 케이스가 있는지 확인!
            if (canSeePlayer)
            {
                stateMachine.ChangeState("Chase");
            }
            else if (investigateState.HasFinishedInvestigation())
            {
                stateMachine.ChangeState("Patrol");
            }
            break;
    }
}
```

### 문제 4: Console에 대리자 에러 메시지가 나온다

**예시:** "NullReferenceException: Object reference not set to an instance of an object"

**원인:** NoiseEvent의 OnNoiseMade가 null이거나 구독자가 이미 해제됨

**해결책:**

```csharp
// NoiseEvent.MakeNoise() 개선
public static void MakeNoise(Vector3 position, float noiseLevel)
{
    if (OnNoiseMade != null)  // Null 체크!
    {
        OnNoiseMade.Invoke(position, noiseLevel);
    }
}

// 또는 null 조건부 연산자 사용
OnNoiseMade?.Invoke(position, noiseLevel);
```

---

## 확장 아이디어

### 아이디어 1: 다양한 소음 타입

```csharp
public enum NoiseType
{
    Footstep,      // 발소리
    Jump,          // 점프음
    Gunshot,       // 총소리
    Explosion,     // 폭발음
    Alarm          // 경보음
}

// 변경된 이벤트
public static event Action<Vector3, float, NoiseType> OnNoiseMade;

// 노이즈 타입별 반응
void OnNoiseHeard(Vector3 position, float level, NoiseType type)
{
    switch (type)
    {
        case NoiseType.Gunshot:
            // 강한 반응
            stateMachine.ChangeState("Chase");
            break;
        case NoiseType.Footstep:
            // 약한 반응
            if (stateMachine.CurrentStateName == "Patrol")
            {
                stateMachine.ChangeState("Investigate");
            }
            break;
    }
}
```

### 아이디어 2: 시각 및 청각의 동적 조정

```csharp
// Enemy의 경각심 시스템
[SerializeField] private float alertness = 0f; // 0 ~ 1

void OnNoiseHeard(Vector3 position, float level)
{
    // 소음을 들었으면 경각심 증가
    alertness = Mathf.Min(1f, alertness + 0.3f);
}

void Update()
{
    // 경각심에 따라 센서 성능 향상
    hearingSensor.hearingRange = 15f + alertness * 10f;
    visionSensor.visionRange = 10f + alertness * 5f;

    // 경각심 감소 (안정화)
    alertness -= 0.01f * Time.deltaTime;
}
```

### 아이디어 3: 다중 Enemy의 소음 전파

```csharp
// 한 Enemy가 소음을 내면 다른 Enemy도 들음
void OnNoiseHeard(Vector3 position, float level)
{
    // ... 기존 조사 로직 ...

    // 다른 Enemy들에게도 알림 (위험 신호)
    if (level > 70f) // 큰 소음
    {
        // 근처 Enemy들을 경계 상태로
        Collider[] nearbyEnemies = Physics.OverlapSphere(
            transform.position,
            30f
        );

        foreach (Collider col in nearbyEnemies)
        {
            PatrolFSMAI otherEnemy = col.GetComponent<PatrolFSMAI>();
            if (otherEnemy != null && otherEnemy != this)
            {
                otherEnemy.OnAlertSignal(position);
            }
        }
    }
}

void OnAlertSignal(Vector3 alertPosition)
{
    // 경계 상태로 전환
    lastHeardNoisePosition = alertPosition;
    investigateState.SetInvestigationPoint(alertPosition);
    stateMachine.ChangeState("Investigate");
}
```

### 아이디어 4: 시간에 따른 감각 변화

```csharp
// 밤에는 더 잘 들음, 낮에는 잘 보는 AI
[SerializeField] private float timeOfDay = 0f; // 0 ~ 24

void UpdateSensorPerformance()
{
    if (timeOfDay >= 6f && timeOfDay < 18f) // 낮
    {
        visionSensor.visionRange = 15f;
        hearingSensor.hearingRange = 10f;
    }
    else // 밤
    {
        visionSensor.visionRange = 8f;
        hearingSensor.hearingRange = 20f;
    }
}
```

### 아이디어 5: 조사 강도 표시

```csharp
// Investigate 상태 시각화 개선
void OnDrawGizmos()
{
    // 기본: 시안색
    Gizmos.color = Color.cyan;

    // 조사 진행도에 따라 색상 변경
    if (hasArrived)
    {
        float progress = investigationTimer / investigationDuration;
        Gizmos.color = Color.Lerp(Color.yellow, Color.green, progress);
    }

    Gizmos.DrawWireSphere(investigationPoint, 1.5f);
}
```

---

## 다음 단계

### Lab 3 완료 확인

- [ ] HearingSensor 구현 및 거리 감쇠 이해
- [ ] NoiseEvent 이벤트 시스템 작동 확인
- [ ] InvestigateState 상태 전환 정상 작동
- [ ] 소음 감지 → 조사 → 조사 완료 → Patrol 복귀 확인
- [ ] 시각 + 청각 센서 우선순위 확인
- [ ] 모든 상태 전환이 명확함
- [ ] Console 메시지가 예상과 일치

### Lab 4로 진행하기 전 준비

1. **현재 Lab 복습**
   - 거리 감쇠 개념 이해
   - 이벤트 기반 통신 이해
   - 다중 센서 우선순위 이해

2. **코드 리뷰**
   - 이벤트 구독/구독 해제가 제대로 되는가?
   - InvestigateState의 생명주기가 명확한가?
   - 상태 전환 조건이 겹치지는 않는가?

3. **확장 실습**
   - 아이디어 1~2 중 하나라도 구현해보기
   - 추가 Enemy 2개 생성하여 동시 조사 테스트

### Lab 4 미리보기

- **Boids 군집 알고리즘:** 다수의 에이전트가 군집을 이루며 이동
- **3가지 규칙:** Separation (분리), Alignment (정렬), Cohesion (응집)
- **성능 최적화:** 공간 분할(Spatial Grid)로 O(N²) → O(N) 복잡도 개선
- **응용:** 새떼, 물고기 떼, 좀비 무리 등 게임 제작

---

## 참고 자료

### 관련 교재 챕터

- **Part II - 1장: 환경 인식**
  - 센서 설계 원칙
  - 거리 감쇠 공식
  - Raycast 활용

- **Part II - 2장: 불확실성 처리**
  - 확률 기반 감지
  - 신뢰도(Confidence) 설정

### 외부 링크

- [Unity Events 공식 문서](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html)
- [C# Delegates and Events](https://docs.microsoft.com/en-us/dotnet/csharp/delegates-events)
- [Audio Distance Model](https://en.wikipedia.org/wiki/Acoustic_attenuation)

### 추천 학습 순서

1. ✅ **Lab 1:** 기본 이동과 입력 처리
2. ✅ **Lab 2:** FSM과 상태 전환
3. ✅ **Lab 3:** 센서 다양화 (청각, 조사) (현재)
4. 📚 **Lab 4:** 군집 시뮬레이션
5. 📚 **Lab 5:** Steering Behaviors
6. ... (총 13개 Lab)

---

## 완료 체크리스트

실습을 완료했으면 다음을 확인하세요:

- [ ] HearingSensor 이해 및 구현
- [ ] 거리 감쇠 공식 이해
- [ ] NoiseEvent 이벤트 시스템 작동
- [ ] Player 움직임 시 소음 발생 확인
- [ ] Enemy 청각 범위 시각화 (시안색 구)
- [ ] Patrol → Investigate 상태 전환 확인
- [ ] Investigate에서 목표 위치로 이동 확인
- [ ] 조사 완료 후 Patrol 복귀 확인
- [ ] 조사 중 플레이어 발견 시 Chase 전환 확인
- [ ] 시각과 청각의 우선순위 정확함
- [ ] 모든 상태 전환이 명확하고 중복 없음
- [ ] Console 메시지가 예상과 일치
- [ ] 씬 저장

**축하합니다!** Lab 3을 완료했습니다. 🎉

---

## FAQ

**Q1: 거리 감쇠가 선형(Linear)이 아닐 수도 있지 않을까?**

A: 맞습니다! 현실의 소리는 역제곱 법칙(Inverse Square Law)을 따릅니다:
```
intensity = source_intensity / (4 * π * distance²)
```
Lab에서는 간단히 선형으로 구현했지만, 더 사실적으로 하려면:
```csharp
float attenuatedNoise = noiseLevel / (1f + distance * distance);
```

**Q2: 소음이 벽을 통과할 수도 있나?**

A: 현재 구현은 벽을 무시합니다. 더 사실적으로 하려면 Raycast로 벽 검사:
```csharp
RaycastHit hit;
if (Physics.Raycast(noisePosition, transform.position - noisePosition, out hit))
{
    // 벽이 소음을 반사/흡수하는 로직
}
```

**Q3: 여러 Enemy가 같은 소음을 들으면 모두 조사하나?**

A: 네, 현재 구현은 그렇습니다. 리소스 절약을 위해 아이디어 3처럼 조정할 수 있습니다.

**Q4: Investigate 시간을 동적으로 조정할 수 있을까?**

A: 가능합니다!
```csharp
void SetInvestigationPoint(Vector3 point, float duration = 3f)
{
    investigationPoint = point;
    investigationDuration = duration;
}
```

---

**작성일:** 2024년 1월
**업데이트:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 45분
