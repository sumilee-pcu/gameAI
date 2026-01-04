# Lab 9: GOAP 목표 지향 AI

**소요 시간:** 150분
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**연관 Part:** Part V - 고급 의사결정
**이전 Lab:** Lab 8 - Behavior Tree

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **GOAP (Goal-Oriented Action Planning) 원리**
   - 현재 상태(State) 정의
   - 목표(Goal) 설정
   - 행동(Action) 정의
   - 플래닝 알고리즘

2. **마을 주민 AI 구현**
   - 먹이/목자/광부 등 다양한 직업
   - 일과 생활의 균형 (일 → 밥 → 휴식)
   - 동적 작업 할당

3. **상태 관리 및 추론**
   - 현재 상태 추적
   - 목표 우선순위
   - 행동 계획 수립

4. **실전 AI 시스템**
   - 자원 관리 (음식, 에너지)
   - 상호작용 시스템 (채집, 조리, 섭취)
   - 일정 및 루틴

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── GOAP/
│   │       ├── GOAPPlanner.cs                 # GOAP 플래너 (NEW)
│   │       ├── GOAPAction.cs                  # 기본 액션 (NEW)
│   │       ├── GOAPGoal.cs                    # 목표 정의 (NEW)
│   │       ├── WorldState.cs                  # 게임 세계 상태 (NEW)
│   │       └── Actions/
│   │           ├── ChopWood.cs                # 나무 베기 (NEW)
│   │           ├── Cook.cs                    # 요리 (NEW)
│   │           ├── Eat.cs                     # 먹기 (NEW)
│   │           ├── Sleep.cs                   # 자기 (NEW)
│   │           └── Idle.cs                    # 대기 (NEW)
│   ├── Villager/
│   │   └── VillagerAI.cs                      # 마을 주민 AI (NEW)
│   └── Utilities/
│       ├── StateManager.cs                    # 상태 관리 (NEW)
│       └── ResourceManager.cs                 # 자원 관리 (NEW)
├── Prefabs/
│   └── Agents/
│       ├── Villager.prefab                    # 마을 주민 (NEW)
│       ├── WoodPile.prefab                    # 나무 더미 (NEW)
│       └── Fireplace.prefab                   # 모닥불 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab09_GOAP.unity                   # 실습 씬 (NEW)
└── Materials/
    ├── VillagerMaterial.mat                   # 주민 (NEW)
    └── BuildingMaterial.mat                   # 건물 (NEW)
```

---

## 주요 개념

### 1. GOAP의 원리

**목표 기반 행동 계획**

```
현재 상태              목표 상태
┌──────────────┐     ┌──────────────┐
│ hungry: true │     │ hungry: false│
│ tired: false │     │ tired: false │
│ wood: 0      │     │ wood: 5      │
└──────────────┘     └──────────────┘
        ↓                    ↑
        └── GOAP Planner ────┘
             (행동 계획)

결과: [Chop Wood] → [Cook] → [Eat]
```

### 2. 핵심 구성 요소

#### A. 상태 (State)

```csharp
public class WorldState
{
    private Dictionary<string, object> state = new Dictionary<string, object>();

    public void Set(string key, object value)
    {
        state[key] = value;
    }

    public object Get(string key)
    {
        return state.ContainsKey(key) ? state[key] : null;
    }

    // 상태 예시
    // "hungry" : true/false
    // "tired" : true/false
    // "wood" : 0~10
    // "food" : 0~10
    // "position" : Vector3
}
```

#### B. 행동 (Action)

```csharp
public abstract class GOAPAction : MonoBehaviour
{
    // 이 행동이 필요한 조건 (precondition)
    public abstract Dictionary<string, object> GetPreconditions();

    // 이 행동이 만족시키는 결과 (effect)
    public abstract Dictionary<string, object> GetEffects();

    // 이 행동이 실행 가능한가?
    public abstract bool IsValid();

    // 행동 비용 (작을수록 선호)
    public virtual float GetCost()
    {
        return 1f;
    }

    // 실제 행동 수행
    public abstract bool Perform();
}
```

#### C. 목표 (Goal)

```csharp
public class GOAPGoal
{
    public string name;
    public float priority;  // 우선순위
    public Dictionary<string, object> desiredState;

    public GOAPGoal(string name, float priority)
    {
        this.name = name;
        this.priority = priority;
        this.desiredState = new Dictionary<string, object>();
    }

    // 예시 목표
    // Goal: "Eat"
    // Priority: 10 (높음)
    // DesiredState: {"hungry": false}
}
```

### 3. GOAP 플래닝 알고리즘

```
1. 현재 상태: {"hungry": true, "tired": false, "wood": 0}
2. 목표: {"hungry": false}
3. 행동 리스트:
   - Eat: (precondition: food > 0, effect: hungry = false, cost: 1)
   - Cook: (precondition: wood > 0, effect: food > 0, cost: 2)
   - ChopWood: (precondition: none, effect: wood > 0, cost: 3)

4. 계획 탐색 (A* 사용):
   시작: {"hungry": true, "wood": 0}
   ├─ ChopWood → {"hungry": true, "wood": 1} (cost: 3)
   │  ├─ Cook → {"hungry": true, "food": 1} (cost: 5)
   │  │  └─ Eat → {"hungry": false} (cost: 6) ✓ 도달!
   │  └─ ...
   └─ ...

5. 최적 경로: ChopWood → Cook → Eat
```

### 4. 마을 주민 구현

```csharp
public class VillagerAI : MonoBehaviour
{
    private WorldState currentState;
    private Queue<GOAPAction> actionPlan;
    private GOAPPlanner planner;

    void Update()
    {
        // 1. 계획이 비어있으면 새로운 계획 생성
        if (actionPlan.Count == 0)
        {
            // 우선순위 목표 선택
            GOAPGoal goal = SelectTopPriorityGoal();

            // 플래닝 실행
            actionPlan = planner.Plan(currentState, goal);
        }

        // 2. 현재 행동 수행
        if (actionPlan.Count > 0)
        {
            GOAPAction currentAction = actionPlan.Peek();

            if (currentAction.Perform())
            {
                actionPlan.Dequeue();
                UpdateWorldState(currentAction);
            }
        }
    }

    private GOAPGoal SelectTopPriorityGoal()
    {
        // 우선순위: 생존(먹이) > 건강(수면) > 일(생산)
        if ((bool)currentState.Get("hungry"))
            return new GOAPGoal("Eat", 10);
        if ((bool)currentState.Get("tired"))
            return new GOAPGoal("Sleep", 8);
        return new GOAPGoal("Work", 5);
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab09_GOAP
```

### Step 2: 마을 환경 구성

#### 2-1. 지형
```
Hierarchy → 3D Object → Plane
이름: Village
Transform:
  - Position: (0, 0, 0)
  - Scale: (30, 1, 30)
```

#### 2-2. 건물들
```
나무 더미:
  Hierarchy → 3D Object → Cube
  이름: WoodPile
  Position: (5, 0.5, 5)
  Scale: (1, 1, 1)

모닥불:
  이름: Fireplace
  Position: (0, 0.5, 0)
  Scale: (1.5, 1.5, 1.5)

침대:
  이름: Bed
  Position: (-5, 0.5, -5)
  Scale: (2, 1, 2)
```

#### 2-3. 카메라
```
Main Camera
Position: (0, 20, -20)
Rotation: (45, 0, 0)
```

### Step 3: 마을 주민(Villager) 프리팹

#### 3-1. 주민 오브젝트
```
Hierarchy → 3D Object → Capsule
이름: Villager
Position: (0, 1, 0)
Scale: (0.8, 2, 0.8)
Remove Capsule Collider
```

#### 3-2. 컴포넌트 추가
```
Add Component → Nav Mesh Agent
  - Speed: 3
  - Stopping Distance: 0.5

Add Component → VillagerAI
  - Job Type: Farmer (또는 Woodcutter, Miner)

Add Component → StateManager
  - Initial Hunger: 0.5
  - Initial Energy: 0.8
```

#### 3-3. 프리팹 생성
```
Assets/Prefabs/Agents/로 드래그
```

### Step 4: 작업 스테이션 설정

```
각 스테이션에 대상 위치 할당:

WoodPile:
  - Add Component → WorkStation
  - Work Type: ChopWood

Fireplace:
  - Add Component → WorkStation
  - Work Type: Cook

Bed:
  - Add Component → WorkStation
  - Work Type: Sleep
```

### Step 5: 여러 주민 배치

```
1. Villager 프리팹을 씬에 여러 번 Instantiate
2. 다양한 직업 할당
3. 서로 다른 상태로 시작 설정
```

---

## 구현 체크리스트

### Phase 1: 기본 구조 (40분)

- [ ] WorldState.cs 구현
  - [ ] Dictionary 기반 상태 저장
  - [ ] Get/Set 메서드
- [ ] GOAPAction 추상 클래스
  - [ ] GetPreconditions()
  - [ ] GetEffects()
  - [ ] IsValid()
  - [ ] Perform()
- [ ] GOAPGoal 클래스
  - [ ] 목표 이름 및 우선순위
  - [ ] 원하는 상태

### Phase 2: 액션 구현 (40분)

- [ ] ChopWood 액션
  - [ ] 나무 더미로 이동
  - [ ] 나무 베기 (애니메이션)
  - [ ] 나무 개수 증가
- [ ] Cook 액션
  - [ ] 모닥불로 이동
  - [ ] 요리 (애니메이션)
  - [ ] 음식 생성
- [ ] Eat 액션
  - [ ] 음식 소비
  - [ ] 배고픔 해소
- [ ] Sleep 액션
  - [ ] 침대로 이동
  - [ ] 자기 (애니메이션)
  - [ ] 에너지 회복
- [ ] Idle 액션

### Phase 3: 플래너 구현 (40분)

- [ ] GOAPPlanner.cs
  - [ ] A* 기반 계획 수립
  - [ ] 상태 노드 생성
  - [ ] 경로 탐색
  - [ ] 행동 시퀀스 반환

### Phase 4: 주민 AI 통합 (20분)

- [ ] VillagerAI.cs
  - [ ] 현재 상태 초기화
  - [ ] 목표 선택 로직
  - [ ] 계획 실행
  - [ ] 상태 업데이트

### Phase 5: 테스트 및 최적화 (10분)

- [ ] 단일 주민 테스트
- [ ] 여러 주민 테스트
- [ ] 자원 부족 상황 테스트
- [ ] 성능 최적화

---

## 참고 자료

### 공식 문헌

- **"Artificial Intelligence for Games"** - Ian Millington
  - Chapter on Planning and Decisions
- **GDC 2005: Goal Oriented Action Planning** - Jeff Orkin
  - Original GOAP paper

### 온라인 자료

- **Jeff Orkin's GOAP Tutorial:**
  - https://alumni.media.mit.edu/~jorkin/gdc2005_orkin_jeff_fear.pdf

### 추천 구현 참고

- **FEAR AI (2006):** GOAP의 유명한 게임 구현
- **The Sims:** 캐릭터 목표 기반 행동

---

## 주요 개념 심화

### 1. A* 기반 계획 수립

```csharp
public class GOAPPlanner
{
    public Queue<GOAPAction> Plan(WorldState start, GOAPGoal goal)
    {
        // 1. 시작 노드 생성
        PlanNode startNode = new PlanNode(start, null, null);

        // 2. A* 탐색
        List<PlanNode> openSet = new List<PlanNode> { startNode };
        List<PlanNode> closedSet = new List<PlanNode>();

        while (openSet.Count > 0)
        {
            // 가장 유망한 노드 선택
            PlanNode current = openSet.OrderBy(n => n.f).First();

            // 목표 도달?
            if (StateContains(current.state, goal.desiredState))
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // 모든 가능한 액션 시도
            foreach (var action in GetValidActions(current.state))
            {
                WorldState newState = ApplyAction(current.state, action);
                float cost = current.g + action.GetCost();

                PlanNode neighbor = new PlanNode(newState, action, current);
                neighbor.g = cost;
                neighbor.h = HeuristicDistance(newState, goal);
                neighbor.f = neighbor.g + neighbor.h;

                if (openSet.Any(n => StateEquals(n.state, newState)) ||
                    closedSet.Any(n => StateEquals(n.state, newState)))
                    continue;

                openSet.Add(neighbor);
            }
        }

        // 계획 없음
        return new Queue<GOAPAction>();
    }
}
```

### 2. 휴리스틱 함수

```csharp
private float HeuristicDistance(WorldState current, GOAPGoal goal)
{
    int differences = 0;

    foreach (var kvp in goal.desiredState)
    {
        if (!current.Get(kvp.Key).Equals(kvp.Value))
        {
            differences++;
        }
    }

    return differences;  // 만족해야 할 조건의 개수
}
```

### 3. 동적 우선순위

```csharp
// 시간에 따라 우선순위 변경
public class DynamicGoalPriority
{
    public float CalculatePriority(string goal, VillagerState state)
    {
        switch (goal)
        {
            case "Eat":
                // 배고픔이 높을수록 우선순위 상승
                return 10f * state.hunger;

            case "Sleep":
                // 피로도가 높을수록 우선순위 상승
                return 8f * state.tiredness;

            case "Work":
                // 시간에 따른 우선순위
                float timeOfDay = (Time.time % 24f) / 24f;
                if (timeOfDay > 0.8f || timeOfDay < 0.2f)  // 밤
                    return 0f;  // 일 안 함
                return 5f * (1f - state.tiredness);

            default:
                return 0f;
        }
    }
}
```

---

## 트러블슈팅

### 문제 1: 플래너가 계획을 수립하지 못함

**원인:** 액션의 precondition과 effect가 맞지 않음

**확인:**
```csharp
// 각 액션이 다른 액션의 precondition을 만족하는지 확인

// Cook의 precondition: {"wood": > 0}
// ChopWood의 effect: {"wood": +1}
// → ChopWood 후 Cook 가능

// 만약 Cook의 precondition이 {"food": > 0}이면
// (먹이가 필요함)
// 비순환 경로 없음 → 계획 실패
```

### 문제 2: 같은 계획 반복

**원인:** 상태 업데이트 안 됨

**해결책:**
```csharp
private void UpdateWorldState(GOAPAction action)
{
    Dictionary<string, object> effects = action.GetEffects();
    foreach (var effect in effects)
    {
        currentState.Set(effect.Key, effect.Value);
    }

    // 시간 경과로 상태 자동 변화
    currentState.Set("hunger", (float)currentState.Get("hunger") + 0.01f);
}
```

### 문제 3: 주민이 막혀있음

**원인:** NavMesh 경로 없음

**해결책:**
```csharp
// 행동 중 길을 찾지 못하면 실패 처리
public override bool Perform()
{
    agent.SetDestination(targetPosition);

    // 일정 시간 동안 이동 못하면 실패
    if (!agent.hasPath || agent.remainingDistance > moveDistance)
    {
        failureCounter++;
        if (failureCounter > 100)
        {
            return true;  // 포기하고 다음 액션으로
        }
    }

    return false;  // 계속 수행 중
}
```

---

## 확장 아이디어

### 아이디어 1: 사회성 추가

```csharp
// 주민끼리 상호작용
public class SocialGoal : GOAPGoal
{
    public VillagerAI targetVillager;

    public SocialGoal(VillagerAI target) : base("Socialize", 3f)
    {
        targetVillager = target;
        desiredState["lonely"] = false;
    }
}

public class ChatAction : GOAPAction
{
    public override bool Perform()
    {
        // 다른 주민 근처에서 대기
        // 시간 경과 시 상태 개선
    }
}
```

### 아이디어 2: 성격/기질

```csharp
public class VillagerPersonality
{
    public float laziness;      // 게으름 (작업 회피)
    public float sociability;   // 사교성 (상호작용 선호)
    public float industriousness; // 근면함 (작업 선호)

    public float AdjustGoalPriority(string goal, float basePriority)
    {
        switch (goal)
        {
            case "Work":
                return basePriority * (1f - laziness) * industriousness;
            case "Socialize":
                return basePriority * sociability;
            default:
                return basePriority;
        }
    }
}
```

### 아이디어 3: 계절 변화

```csharp
// 계절에 따라 필요한 작업 변경
public class SeasonalGoals
{
    public Queue<GOAPGoal> GetSeasonalGoals(Season season)
    {
        var goals = new Queue<GOAPGoal>();

        switch (season)
        {
            case Season.Spring:
                goals.Enqueue(new GOAPGoal("Plant", 7f));
                break;
            case Season.Summer:
                goals.Enqueue(new GOAPGoal("Tend", 7f));
                break;
            case Season.Fall:
                goals.Enqueue(new GOAPGoal("Harvest", 9f));
                goals.Enqueue(new GOAPGoal("Store", 8f));
                break;
            case Season.Winter:
                goals.Enqueue(new GOAPGoal("Rest", 6f));
                goals.Enqueue(new GOAPGoal("Maintain", 5f));
                break;
        }

        return goals;
    }
}
```

---

## 완료 체크리스트

- [ ] WorldState.cs 구현
- [ ] GOAPAction 추상 클래스 구현
- [ ] GOAPGoal 클래스 구현
- [ ] ChopWood, Cook, Eat, Sleep, Idle 액션 구현
- [ ] GOAPPlanner.cs (A* 기반 계획) 구현
- [ ] VillagerAI.cs 메인 루프 구현
- [ ] 마을 환경 구성
- [ ] 단일 주민 테스트
- [ ] 여러 주민 동시 작동 테스트
- [ ] 동적 우선순위 테스트
- [ ] 자원 관리 시스템 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐⭐ (최상)
**예상 완료 시간:** 150분
