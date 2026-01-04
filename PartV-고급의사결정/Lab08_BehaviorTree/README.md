# Lab 8: Behavior Tree 경비병 AI

**소요 시간:** 120분
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**연관 Part:** Part V - 고급 의사결정
**이전 Lab:** Lab 7 - NavMesh 에이전트

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **Behavior Tree (행동 트리) 구조**
   - 트리의 기본 구조와 노드 타입
   - Composite 노드 (Selector, Sequence)
   - Task 노드 (Action)
   - Condition 노드 (Decorator)

2. **경비병 AI 구현**
   - 순찰 (Patrol)
   - 경계 (Alert)
   - 추격 (Chase)
   - 공격 (Attack)

3. **상태 관리와 전환**
   - 동적 우선순위 변경
   - 이벤트 기반 상태 전환
   - 메모리 상태 유지

4. **실전 AI 구현**
   - 시각 시스템 (Line of Sight)
   - 청각 시스템 (Hearing)
   - 메모리 시스템 (Remember last seen)

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   ├── BehaviorTree/
│   │   │   ├── BTNode.cs                      # 기본 노드 (NEW)
│   │   │   ├── Composite/
│   │   │   │   ├── Selector.cs                # Selector 노드 (NEW)
│   │   │   │   └── Sequence.cs                # Sequence 노드 (NEW)
│   │   │   ├── Task/
│   │   │   │   ├── Patrol.cs                  # 순찰 태스크 (NEW)
│   │   │   │   ├── Chase.cs                   # 추격 태스크 (NEW)
│   │   │   │   └── Attack.cs                  # 공격 태스크 (NEW)
│   │   │   └── Decorator/
│   │   │       └── UntilSuccess.cs            # Until Success (NEW)
│   │   └── Perception/
│   │       ├── Vision.cs                      # 시각 시스템 (NEW)
│   │       ├── Hearing.cs                     # 청각 시스템 (NEW)
│   │       └── Memory.cs                      # 메모리 시스템 (NEW)
│   ├── Guard/
│   │   └── GuardAI.cs                         # 경비병 AI 메인 (NEW)
│   └── Utilities/
│       └── BTDebugger.cs                      # 디버그 뷰어 (NEW)
├── Prefabs/
│   └── Agents/
│       ├── Guard.prefab                       # 경비병 프리팹 (NEW)
│       └── Intruder.prefab                    # 침입자 프리팹 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab08_BehaviorTree.unity           # 실습 씬 (NEW)
└── Materials/
    ├── GuardMaterial.mat                      # 경비병 (NEW)
    ├── IntruderMaterial.mat                   # 침입자 (NEW)
    └── VisionMaterial.mat                     # 시야 범위 (NEW)
```

---

## 주요 개념

### 1. Behavior Tree 구조

**트리형 의사결정 구조**

```
                    [Root: Selector]
                    /              \
        [Patrol]  [Selector: Alert?]
                   /            \
            [See Enemy?]    [Hear Enemy?]
             /        \       /      \
        [Chase]    [Attack] [Search] [Wait]
```

#### 노드 타입

**1. Composite 노드 (분기 결정)**

```csharp
// Selector (OR): 첫 번째 성공 노드를 반환
// 왼쪽부터 순서대로 실행
// 하나라도 성공하면 성공 반환

if (child1.Execute() == Success)
    return Success;
if (child2.Execute() == Success)
    return Success;
return Failure;


// Sequence (AND): 모든 자식이 성공해야 성공
// 하나라도 실패하면 실패 반환

if (child1.Execute() != Success)
    return Failure;
if (child2.Execute() != Success)
    return Failure;
return Success;
```

**2. Task 노드 (실행)**

```csharp
public class PatrolTask : BTNode
{
    private NavMeshAgent agent;
    private Vector3[] patrolPoints;
    private int currentPointIndex = 0;

    public override NodeState Execute()
    {
        // 다음 순찰 포인트로 이동
        if (Vector3.Distance(agent.position, patrolPoints[currentPointIndex]) < 1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        }

        agent.SetDestination(patrolPoints[currentPointIndex]);

        return NodeState.Running;  // 계속 실행
    }
}
```

**3. Decorator 노드 (조건)**

```csharp
public class HasLineOfSight : BTNode
{
    private Transform target;
    private Vision vision;

    public override NodeState Execute()
    {
        if (vision.CanSee(target))
            return child.Execute();  // 자식 실행
        else
            return NodeState.Failure;  // 조건 실패
    }
}
```

### 2. 경비병 AI 구조

```
경비병 상태 머신:

Idle (순찰)
   ↓↑
Alert (경계)
   ↓↑
Chase (추격)
   ↓↑
Attack (공격)

Behavior Tree로 구현:
┌─ Selector: 최고 우선순위 찾기
│  ├─ Sequence: 공격 조건
│  │  ├─ IsEnemyVisible?
│  │  ├─ IsEnemyNear?
│  │  └─ Attack Task
│  ├─ Sequence: 추격 조건
│  │  ├─ HasEnemyPosition?
│  │  └─ Chase Task
│  └─ Patrol Task
└─
```

### 3. 인식 시스템

#### A. Vision (시각)

```csharp
public class Vision : MonoBehaviour
{
    public float visionRange = 10f;
    public float visionAngle = 60f;

    public bool CanSee(Transform target)
    {
        // 거리 확인
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > visionRange) return false;

        // 각도 확인
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > visionAngle / 2f) return false;

        // Line of Sight 확인
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, distance))
        {
            return hit.collider.transform == target;
        }

        return true;
    }
}
```

#### B. Hearing (청각)

```csharp
public class Hearing : MonoBehaviour
{
    public float hearingRange = 5f;

    public bool CanHear(Vector3 soundPosition)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        return distance < hearingRange;
    }
}
```

#### C. Memory (메모리)

```csharp
public class Memory : MonoBehaviour
{
    private Vector3 lastSeenPosition;
    private Transform lastSeenTarget;
    private float lastSeenTime;
    private float forgetTime = 5f;

    public void RememberTarget(Transform target)
    {
        lastSeenTarget = target;
        lastSeenPosition = target.position;
        lastSeenTime = Time.time;
    }

    public Vector3 GetLastSeenPosition()
    {
        return lastSeenPosition;
    }

    public bool HasMemory()
    {
        return Time.time - lastSeenTime < forgetTime;
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab08_BehaviorTree
```

### Step 2: 환경 구성

#### 2-1. 기본 지형
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (20, 1, 20)
```

#### 2-2. 건물/벽 배치
```
Hierarchy → 3D Object → Cube
이름: Building

Transform:
  - Position: (0, 2, 0)
  - Scale: (8, 4, 8)
Material: 회색

벽으로 "방" 형태 만들기 (4-5개)
```

#### 2-3. 카메라
```
Main Camera
Transform:
  - Position: (0, 15, -15)
  - Rotation: (45, 0, 0)
```

### Step 3: NavMesh 설정

```
1. Hierarchy → Create → NavMesh Surface
2. Bake 클릭
3. Scene에서 파란색 메시 확인
```

### Step 4: 경비병(Guard) 프리팹

#### 4-1. 경비병 오브젝트
```
Hierarchy → 3D Object → Capsule
이름: Guard

Transform:
  - Position: (-5, 1, -5)
  - Scale: (0.8, 2, 0.8)
Remove Capsule Collider
```

#### 4-2. 컴포넌트 추가
```
Add Component → Nav Mesh Agent
  - Speed: 3.5
  - Stopping Distance: 0.5

Add Component → GuardAI
Add Component → Vision
Add Component → Hearing
Add Component → Memory
```

#### 4-3. 시각화 (선택)
```
경비병에 자식 오브젝트:
Hierarchy → 3D Object → Sphere
이름: VisionIndicator
Transform:
  - Scale: (0.3, 0.3, 0.3)
```

#### 4-4. 프리팹 생성
```
Assets/Prefabs/Agents/로 드래그
```

### Step 5: 침입자(Intruder) 프리팹

#### 5-1. 침입자 오브젝트
```
Hierarchy → 3D Object → Sphere
이름: Intruder

Transform:
  - Position: (5, 0.5, 5)
  - Scale: (0.7, 0.7, 0.7)

Material: 빨간색
```

#### 5-2. 이동 스크립트 추가
```
Add Component → IntruderController

Inspector:
  - Move Speed: 4
```

#### 5-3. 프리팹 생성
```
Assets/Prefabs/Agents/로 드래그
```

### Step 6: 순찰 포인트 설정

```
경비병의 GuardAI 스크립트에 할당:
1. 각 "방"의 중심을 순찰 포인트로 설정
2. 배열에 4-6개 포인트 저장
3. 경비병이 자동으로 순회
```

---

## 구현 체크리스트

### Phase 1: Behavior Tree 기본 (40분)

- [ ] BTNode.cs (기본 클래스)
  - [ ] NodeState enum (Success, Failure, Running)
  - [ ] virtual Execute() 메서드
- [ ] Selector.cs (OR 노드)
  - [ ] 자식 노드 순서대로 실행
  - [ ] 하나 성공하면 성공 반환
- [ ] Sequence.cs (AND 노드)
  - [ ] 모든 자식이 성공해야 성공
  - [ ] 하나 실패하면 실패 반환

### Phase 2: 인식 시스템 (30분)

- [ ] Vision.cs (시각)
  - [ ] visionRange, visionAngle
  - [ ] Line of Sight 체크
- [ ] Hearing.cs (청각)
  - [ ] 음원 감지
- [ ] Memory.cs (메모리)
  - [ ] 마지막 위치 기억
  - [ ] 시간 경과에 따른 망각

### Phase 3: 경비병 Task 구현 (35분)

- [ ] PatrolTask
  - [ ] 순찰 포인트 순회
- [ ] ChaseTask
  - [ ] 마지막 알려진 위치로 이동
  - [ ] 목표 재발견 시 변경
- [ ] AttackTask
  - [ ] 근처 적 공격
  - [ ] 거리 확인
- [ ] AlertTask (선택)

### Phase 4: 통합 및 테스트 (15분)

- [ ] GuardAI.cs에서 BT 구축
- [ ] 경비병 작동 확인
  - [ ] 순찰 중: 침입자 미발견
  - [ ] 보임: 추격 시작
  - [ ] 가까워짐: 공격

### Phase 5: 디버깅 및 최적화 (보너스)

- [ ] BTDebugger로 트리 시각화
- [ ] 여러 경비병 배치
- [ ] 경비병 간 커뮤니케이션 (선택)

---

## 참고 자료

### 공식 문헌

- **"Behavioral Mathematics for Game AI"** - Dave Mark
- **"Game Coding Complete"** - Mike McShaffry
  - Chapter on Behavior Trees

### 온라인 자료

- **Red Blob Games - Behavior Trees:**
  - https://www.redblobgames.com/behavior-trees/
- **GDC Vault - Behavior Trees in Game AI**

### 추천 라이브러리

- **BehaviorDesigner** (상용)
- **NodeCanvas** (상용)
- **NaughtyAttributes** (오픈소스 트리 시각화)

---

## 주요 개념 심화

### 1. BehaviorTree 실행 흐름

```csharp
public class GuardAI : MonoBehaviour
{
    private BTNode rootNode;

    void Start()
    {
        // BT 구축
        rootNode = BuildBehaviorTree();
    }

    void Update()
    {
        // 매 프레임 트리 실행
        rootNode.Execute();
    }

    private BTNode BuildBehaviorTree()
    {
        // 최상위: Selector (최우선 조건부터 확인)
        Selector root = new Selector(new List<BTNode>
        {
            // 1. 공격 가능한가? (가장 높은 우선순위)
            new Sequence(new List<BTNode>
            {
                new CanAttack(enemy, attackDistance),
                new AttackTask(agent, enemy)
            }),

            // 2. 추격 중인가?
            new Sequence(new List<BTNode>
            {
                new HasMemory(memory),
                new ChaseTask(agent, memory)
            }),

            // 3. 그 외: 순찰 (항상 성공)
            new PatrolTask(agent, patrolPoints)
        });

        return root;
    }
}
```

### 2. Decorator 패턴

**조건을 추가하는 데코레이터**

```csharp
public class Decorator : BTNode
{
    protected BTNode child;

    public Decorator(BTNode child)
    {
        this.child = child;
    }

    public override NodeState Execute()
    {
        return CheckCondition() ? child.Execute() : NodeState.Failure;
    }

    protected virtual bool CheckCondition()
    {
        return true;
    }
}

// 구현 예
public class CanSeeEnemy : Decorator
{
    private Vision vision;
    private Transform enemy;

    protected override bool CheckCondition()
    {
        return vision.CanSee(enemy);
    }
}
```

### 3. 병렬 Selector (Parallel)

```csharp
// 여러 조건을 동시에 확인
// 하나라도 성공하면 즉시 반환

public class ParallelSelector : Selector
{
    public override NodeState Execute()
    {
        foreach (var child in children)
        {
            // 모든 자식을 동시에 체크
            if (child.Execute() == NodeState.Success)
                return NodeState.Success;
        }

        return NodeState.Failure;
    }
}
```

---

## 트러블슈팅

### 문제 1: 경비병이 움직이지 않음

**원인:**
- BT가 Running만 반환
- NavMeshAgent에 목표가 설정되지 않음

**해결책:**
```csharp
// ChaseTask에서 목표를 계속 설정
public class ChaseTask : BTNode
{
    public override NodeState Execute()
    {
        // 매 프레임 목표 갱신
        agent.SetDestination(memory.GetLastSeenPosition());

        // 목표 도달 판별
        if (agent.remainingDistance < 0.5f)
            return NodeState.Success;

        return NodeState.Running;
    }
}
```

### 문제 2: 경비병이 침입자를 발견하지 못함

**원인:** Vision 설정 오류

**확인:**
```csharp
void OnDrawGizmos()
{
    // Vision range 시각화
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, visionRange);

    // Vision direction
    Gizmos.color = Color.blue;
    Gizmos.DrawRay(transform.position, transform.forward * 10f);
}
```

### 문제 3: 메모리가 너무 오래 지속

**원인:** forgetTime이 너무 김

**해결책:**
```csharp
// 메모리 클래스에서
public float forgetTime = 2f;  // 2초

// 또는 마지막 추격 거리에 따라 동적 조절
public void RememberTarget(Transform target, float distance)
{
    forgetTime = Mathf.Clamp(distance / 5f, 1f, 5f);
}
```

---

## 확장 아이디어

### 아이디어 1: 경비원 통신

```csharp
// 한 경비병이 침입자를 발견하면 다른 경비병에 알림
public class GuardNetwork : MonoBehaviour
{
    public static void AlertAllGuards(Vector3 intruderPos)
    {
        Guard[] guards = FindObjectsOfType<Guard>();
        foreach (var guard in guards)
        {
            guard.memory.RememberTarget(intruderPos);
        }
    }
}
```

### 아이디어 2: 경비병 강화 (Patrol → Alert)

```csharp
// 침입자가 발견되면 경비 강화
public class AlertLevel : MonoBehaviour
{
    private int alertCount = 0;

    public void IncrementAlert()
    {
        alertCount++;
        if (alertCount >= 2)
        {
            // 모든 경비병이 최고 경계 상태로
            GuardNetwork.EnterLockdown();
        }
    }
}
```

### 아이디어 3: 경비병 학습 (메모리 개선)

```csharp
// 침입자의 패턴 분석
// 자주 나타나는 장소에 더 자주 순찰

public class SmartPatrol : MonoBehaviour
{
    private Dictionary<Vector3, int> sightings = new Dictionary<Vector3, int>();

    public void RecordSighting(Vector3 position)
    {
        // 근처 기존 데이터 확인
        // 있으면 카운트 증가, 없으면 새로 추가
    }

    public Vector3[] GenerateSmartPatrolPoints()
    {
        // 자주 목격되는 곳을 더 자주 순찰
        return sightings.OrderByDescending(x => x.Value)
                       .Select(x => x.Key)
                       .ToArray();
    }
}
```

---

## 완료 체크리스트

- [ ] BTNode 기본 클래스 구현
- [ ] Selector 노드 구현
- [ ] Sequence 노드 구현
- [ ] Task 노드들 구현 (Patrol, Chase, Attack)
- [ ] Vision 시스템 구현
- [ ] Hearing 시스템 구현
- [ ] Memory 시스템 구현
- [ ] GuardAI 메인 루프 구현
- [ ] BT 구축 (BuildBehaviorTree)
- [ ] 경비병 순찰 확인
- [ ] 침입자 발견 시 추격 확인
- [ ] 가까워지면 공격 확인
- [ ] BTDebugger 구현 (선택)
- [ ] 여러 경비병 배치 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**예상 완료 시간:** 120분
