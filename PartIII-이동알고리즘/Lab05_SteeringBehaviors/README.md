# Lab 5: Steering Behaviors (조향 행동)

**소요 시간:** 60분
**난이도:** ⭐⭐⭐⭐ (상)
**연관 Part:** Part III - 이동 알고리즘
**이전 Lab:** Lab 4 - Boids 군집 시뮬레이션

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **기본 Steering Behaviors 구현**
   - **Seek (추적):** 목표 위치로 향하는 행동
   - **Flee (도주):** 위험 위치에서 멀어지는 행동
   - **Arrive (도착):** 목표 위치에서 감속하여 도착
   - **Pursue (추격):** 이동하는 목표를 예측하여 추적
   - **Evade (회피):** 이동하는 위험을 예측하여 회피
   - **Wander (배회):** 목표 없이 자연스럽게 돌아다니기

2. **행동 조합 (Behavior Blending)**
   - 여러 행동을 동시에 적용
   - 우선순위 기반 행동 선택
   - 가중치를 통한 행동 강도 조절

3. **벡터 수학 심화**
   - 거리와 방향 계산
   - 목표 위치 예측
   - 벡터 정규화와 스케일링

4. **AI 에이전트 상태 관리**
   - 상태 기반 행동 전환
   - 조건에 따른 행동 우선순위 변경

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── Steering/
│   │       ├── SteeringBehaviors.cs    # 모든 Steering 함수 (NEW)
│   │       ├── SteeringAgent.cs        # Steering 에이전트 (NEW)
│   │       └── TargetMarker.cs         # 목표 표시 (NEW)
│   └── Utilities/
│       └── VectorHelper.cs             # 벡터 유틸리티 (선택)
├── Prefabs/
│   └── Agents/
│       ├── SteeringAgent.prefab        # Steering 에이전트 프리팹 (NEW)
│       ├── Target.prefab               # 목표 프리팹 (NEW)
│       └── Obstacle.prefab             # 장애물 프리팹 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab05_SteeringBehaviors.unity # 실습 씬 (NEW)
└── Materials/
    ├── AgentMaterial.mat               # 에이전트 메테리얼 (NEW)
    ├── TargetMaterial.mat              # 목표 메테리얼 (NEW)
    └── ObstacleMaterial.mat            # 장애물 메테리얼 (NEW)
```

---

## 주요 개념

### 1. Steering Behaviors의 기본 원리

**Steering 행동 = 목표에 도달하기 위한 조향력(Steering Force)**

```
현재 위치
    ↓
원하는 속도 계산 (목표 기반)
    ↓
조향력 계산: steering = desiredVelocity - currentVelocity
    ↓
가속도 적용: acceleration += steering
    ↓
속도 업데이트: velocity += acceleration * dt
    ↓
위치 업데이트: position += velocity * dt
```

### 2. 주요 Steering Behaviors

#### A. Seek (추적)
**목표 위치로 향하는 행동**

```csharp
Vector3 Seek(Vector3 target)
{
    // 1. 원하는 속도: 목표로 향함
    Vector3 desired = (target - position).normalized * maxSpeed;

    // 2. 조향력: 원하는 속도와 현재 속도의 차이
    Vector3 steering = desired - velocity;

    // 3. 최대 힘으로 제한
    return Vector3.ClampMagnitude(steering, maxForce);
}
```

**효과:**
```
에이전트 → ← ← ← 목표
계속 가속하여 목표에 도달
```

#### B. Flee (도주)
**목표에서 멀어지는 행동**

```csharp
Vector3 Flee(Vector3 target)
{
    // Seek과 정반대
    Vector3 desired = (position - target).normalized * maxSpeed;

    Vector3 steering = desired - velocity;

    return Vector3.ClampMagnitude(steering, maxForce);
}
```

**효과:**
```
위험 ← ← ← 에이전트
계속 달아남
```

#### C. Arrive (도착)
**목표 위치에서 감속하여 정지**

```csharp
Vector3 Arrive(Vector3 target, float slowing_distance = 100f)
{
    Vector3 desired = target - position;
    float distance = desired.magnitude;

    desired.Normalize();

    // 거리에 따라 속도 조절
    if (distance < slowing_distance)
    {
        desired *= maxSpeed * (distance / slowing_distance);  // 선형 감속
    }
    else
    {
        desired *= maxSpeed;
    }

    Vector3 steering = desired - velocity;

    return Vector3.ClampMagnitude(steering, maxForce);
}
```

**효과:**
```
에이전트 ↗ ↑ ↗ → 목표 (감속하며 도착)
```

#### D. Pursue (추격)
**이동하는 목표를 예측하여 추적**

```csharp
Vector3 Pursue(Transform target)
{
    Vector3 targetOffset = target.position - position;
    float distance = targetOffset.magnitude;
    float updateTime = 0.1f;  // 예측 시간

    // 목표의 미래 위치 예측
    Vector3 futurePosition = target.position +
                            (target.GetComponent<Rigidbody>().velocity * updateTime);

    // 미래 위치로 Seek
    return Seek(futurePosition);
}
```

**효과:**
```
목표 ↓ ↓ ↓ (이동 중)
     ↖ ↖ ↖ ↖ 에이전트 (예측하여 추격)
```

#### E. Evade (회피)
**이동하는 위협을 예측하여 회피**

```csharp
Vector3 Evade(Transform threat)
{
    Vector3 threatOffset = threat.position - position;
    float distance = threatOffset.magnitude;
    float updateTime = 0.1f;  // 예측 시간

    // 위협의 미래 위치 예측
    Vector3 futurePosition = threat.position +
                            (threat.GetComponent<Rigidbody>().velocity * updateTime);

    // 미래 위치에서 도주
    return Flee(futurePosition);
}
```

**효과:**
```
위협 ↓ ↓ ↓ (이동 중)
     → → → → 에이전트 (예측하여 회피)
```

#### F. Wander (배회)
**무작위로 배회하면서 자연스러운 이동**

```csharp
Vector3 Wander()
{
    // 원형 경로 상에서 무작위 각도 선택
    wanderAngle += Random.Range(-Change, Change);

    // 원 위의 점 계산
    Vector3 circleCenter = velocity.normalized * wanderDistance;
    Vector3 displacement = new Vector3(
        Mathf.Cos(wanderAngle) * wanderRadius,
        0,
        Mathf.Sin(wanderAngle) * wanderRadius
    );

    Vector3 desiredVelocity = circleCenter + displacement;

    return Vector3.ClampMagnitude(desiredVelocity, maxForce);
}
```

**효과:**
```
↗ ↓ ↙ ← ↖ ↓ ↙ ← ↖
↑ ↘ ↙ ↑ → ↑ ↘ ↙ ↑ →
(자연스러운 배회)
```

### 3. 행동 조합 (Behavior Blending)

**여러 행동을 동시에 사용하여 복잡한 행동 구현**

```csharp
Vector3 steering = Vector3.zero;

// 각 행동을 계산하고 가중치 적용
steering += Seek(target) * seekWeight;
steering += Avoid(obstacles) * avoidWeight;
steering += Separation(neighbors) * separationWeight;

// 누적된 힘 제한
steering = Vector3.ClampMagnitude(steering, maxForce);
```

**예시: 추격하며 동시에 장애물 회피**

```csharp
Vector3 ComputeSteering()
{
    Vector3 steering = Vector3.zero;

    // 목표 추적 (90% 가중치)
    steering += Pursue(target) * 0.9f;

    // 장애물 회피 (10% 가중치)
    steering += AvoidObstacles() * 0.1f;

    return Vector3.ClampMagnitude(steering, maxForce);
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab05_SteeringBehaviors
```

### Step 2: 기본 환경 구성

#### 2-1. 바닥 생성
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (20, 1, 20)
```

#### 2-2. 카메라 설정
```
Main Camera 선택
Transform:
  - Position: (0, 15, -10)
  - Rotation: (45, 0, 0)
```

### Step 3: SteeringAgent 프리팹 생성

#### 3-1. 에이전트 오브젝트 생성
```
Hierarchy → 3D Object → Sphere
이름: SteeringAgent
Transform:
  - Position: (0, 0.5, 0) (임시)
  - Scale: (0.5, 0.5, 0.5)
```

#### 3-2. Material 생성 및 적용
```
Project → Assets/Materials → Create → Material
이름: AgentMaterial
설정:
  - Shader: Standard
  - Albedo Color: 파란색 (0.2, 0.5, 1)

SteeringAgent의 Mesh Renderer에 드래그
```

#### 3-3. SteeringAgent 스크립트 추가
```
SteeringAgent 선택 → Add Component → SteeringAgent

Inspector 설정:
  - Max Speed: 5
  - Max Force: 3
  - Max Acceleration: 2
```

#### 3-4. 프리팹으로 변환
```
SteeringAgent를 Assets/Prefabs/Agents/로 드래그
Hierarchy의 SteeringAgent 삭제
```

### Step 4: Target (목표) 오브젝트 설정

#### 4-1. 목표 오브젝트 생성
```
Hierarchy → 3D Object → Sphere
이름: Target
Transform:
  - Position: (10, 0.5, 10)
  - Scale: (0.7, 0.7, 0.7)
```

#### 4-2. 목표 Material 생성
```
Create → Material
이름: TargetMaterial
설정:
  - Albedo Color: 녹색 (0.2, 1, 0.2)

Target의 Mesh Renderer에 드래그
```

#### 4-3. Target 프리팹 생성
```
Target를 Assets/Prefabs/로 드래그
```

### Step 5: 장애물 추가 (선택)

```
Hierarchy → 3D Object → Cube
이름: Obstacle
Transform:
  - Position: (5, 0.5, 5)
  - Scale: (1, 1, 1)
Material: 회색
```

### Step 6: 테스트 씬 구성

#### 6-1. SceneManager 오브젝트 생성
```
Hierarchy → Create Empty
이름: SceneManager

Add Component → Lab05SceneManager (새로 만들 스크립트)
```

#### 6-2. 프리팹 할당
```
Inspector:
  - Steering Agent Prefab: SteeringAgent
  - Target Prefab: Target
  - Initial Agent Count: 5
```

---

## 구현 체크리스트

### Phase 1: 기본 Steering 함수 구현 (20분)

- [ ] SteeringBehaviors.cs 작성
  - [ ] Seek() 구현
  - [ ] Flee() 구현
  - [ ] Arrive() 구현
  - [ ] Pursue() 구현 (보너스)
  - [ ] Evade() 구현 (보너스)
  - [ ] Wander() 구현

### Phase 2: SteeringAgent 구현 (15분)

- [ ] SteeringAgent.cs 작성
  - [ ] 속도, 가속도, 위치 관리
  - [ ] 현재 행동 상태 저장
  - [ ] 행동 업데이트 (ComputeSteering)
  - [ ] 위치 업데이트

### Phase 3: 씬 설정 및 테스트 (15분)

- [ ] 씬 구성
  - [ ] 지형, 카메라, 조명
  - [ ] 에이전트, 목표, 장애물 배치
- [ ] 기본 동작 테스트
  - [ ] Seek 테스트
  - [ ] Flee 테스트
  - [ ] Arrive 테스트
  - [ ] Wander 테스트
- [ ] 행동 조합 테스트

### Phase 4: 고급 기능 (보너스 - 10분)

- [ ] 여러 에이전트 동시 제어
- [ ] 키보드로 목표 이동
- [ ] 행동 전환 (F 키로 Flee, A 키로 Arrive 등)
- [ ] Pursue/Evade 테스트

---

## 참고 자료

### 공식 문헌

- **"Steering Behaviors for Autonomous Characters"** - Craig Reynolds
  - http://www.red3d.com/cwr/steering/
- **"Programming Game AI by Example"** - Mat Buckland
  - Chapter 2-3: Steering Behaviors

### 온라인 자료

- **Daniel Shiffman의 자연 시뮬레이션:**
  - https://www.youtube.com/watch?v=JIz2L4yCl7I (Steering Behaviors)
  - https://github.com/shiffman/The-Nature-of-Code

### 추천 영상

- Brackeys: "AI Steering Behaviors"
- Sebastian Lague: "AI Movement"

---

## 주요 개념 설명

### 1. 조향력 (Steering Force)

**Steering Force = 원하는 속도 - 현재 속도**

```
예시: 에이전트가 (0,0)에서 (10,0)으로 이동

t=0s:
  현재 위치: (0, 0)
  현재 속도: (0, 0)
  원하는 속도: (5, 0) (정규화된 방향 * maxSpeed)
  조향력: (5, 0) - (0, 0) = (5, 0) → 가속

t=1s (속도 5m/s에 도달):
  현재 속도: (5, 0)
  원하는 속도: (5, 0)
  조향력: (5, 0) - (5, 0) = (0, 0) → 더 이상 가속 없음 (등속 이동)

t=2s (목표 근처):
  Arrive() 사용 시:
  원하는 속도: (2, 0) (거리에 따라 감속)
  조향력: (2, 0) - (5, 0) = (-3, 0) → 감속
```

### 2. 목표 예측 (Pursuit)

**미래 위치 계산으로 더 정확한 추격**

```
t=0s:
목표 위치: (10, 0)
목표 속도: (2, 0)

t=0.1s (예측 시간):
예측 위치: (10, 0) + (2, 0) * 0.1 = (10.2, 0)

에이전트: (10.2, 0)로 Seek → 더 정확한 추격
```

### 3. 행동 우선순위

**일부 행동이 다른 행동보다 중요한 경우**

```csharp
// 방법 1: 우선순위 기반 (한 번에 하나)
if (threatNearby)
{
    steering = Evade(threat);  // 생존 우선
}
else if (targetNearby)
{
    steering = Pursue(target);  // 사냥
}
else
{
    steering = Wander();  // 배회
}

// 방법 2: 가중치 기반 (동시에 여러 개)
Vector3 steering = Vector3.zero;
steering += Evade(threat) * 2.0f;   // 회피는 강하게
steering += Pursue(target) * 0.8f;  // 추격은 약하게
steering = Vector3.ClampMagnitude(steering, maxForce);
```

---

## 트러블슈팅

### 문제 1: 에이전트가 목표를 지나쳐버린다 (Seek)

**원인:** Arrive() 대신 Seek() 사용

**해결책:**
```csharp
// 변경 전
steering = Seek(target);

// 변경 후
steering = Arrive(target, 2.0f);  // 2m 범위에서 감속
```

### 문제 2: 추격(Pursue)이 부정확하다

**원인:** 예측 시간이 너무 짧음

**해결책:**
```csharp
// 변경 전
float updateTime = 0.01f;  // 너무 짧음

// 변경 후
float updateTime = 0.1f;  // 0.1초 예측
// 또는 거리에 따라 동적 조절
float updateTime = distance / (target.speed + 0.1f);
```

### 문제 3: Wander가 너무 무작위다

**원인:** wanderRadius나 wanderDistance가 너무 크거나 작음

**해결책:**
```csharp
// 변경 전
wanderRadius = 5f;
wanderDistance = 10f;
changeSpeed = 1f;  // 각도 변화가 급격함

// 변경 후
wanderRadius = 1f;      // 작은 반경
wanderDistance = 5f;    // 적당한 거리
changeSpeed = 0.1f;    // 천천히 각도 변화 (부드러운 배회)
```

---

## 확장 아이디어

### 아이디어 1: 상태 머신 (State Machine)

```csharp
public enum AgentState { Idle, Seeking, Fleeing, Pursuing, Evading, Wandering }

void Update()
{
    switch (currentState)
    {
        case AgentState.Seeking:
            steering = Seek(target);
            if (Vector3.Distance(transform.position, target) < 0.5f)
                currentState = AgentState.Idle;
            break;
        case AgentState.Fleeing:
            steering = Flee(threat);
            if (Vector3.Distance(transform.position, threat) > safeDistance)
                currentState = AgentState.Idle;
            break;
        // ... 나머지 상태 ...
    }
}
```

### 아이디어 2: 경로 추적 (Path Following)

```csharp
Vector3 FollowPath(Vector3[] path)
{
    // 경로 상의 가장 가까운 점 찾기
    Vector3 closestPoint = GetClosestPointOnPath(path);

    // 그 점보다 앞의 점으로 Seek
    Vector3 targetAhead = closestPoint + (direction * lookAhead);

    return Seek(targetAhead);
}
```

### 아이디어 3: 그룹 이동 (Flocking + Steering)

```csharp
Vector3 ComputeSteering()
{
    Vector3 steering = Vector3.zero;

    // 목표 추적
    steering += Seek(leaderPosition) * 0.6f;

    // 분리 (충돌 방지)
    steering += Separation() * 0.3f;

    // 배회 (자연스러움)
    steering += Wander() * 0.1f;

    return Vector3.ClampMagnitude(steering, maxForce);
}
```

---

## 다음 단계

Lab 5 완료 후:

1. **이해 확인**
   - [ ] 6가지 Steering Behaviors의 원리 이해
   - [ ] 조향력(Steering Force) 개념 이해
   - [ ] 행동 조합의 장점 인식

2. **추가 구현**
   - [ ] Pursue와 Evade 구현
   - [ ] 상태 머신 추가
   - [ ] 여러 에이전트의 복합 행동

3. **다음 Lab으로의 전이**
   - Lab 6: A* 경로 탐색
   - 여러 개의 이동 가능한 행동을 조합하여 더 복잡한 경로 탐색 학습

---

## 성능 최적화

### 1. 계산 최소화

```csharp
// 거리 계산은 비쌈 (sqrt 포함)
// 필요한 경우만 사용
float distance = Vector3.Distance(a, b);  // 피하기

// 거리 제곱으로 비교 (sqrt 없음)
float sqrDistance = (a - b).sqrMagnitude;
if (sqrDistance < maxDistance * maxDistance)  // OK
{
    // 정확한 거리 필요할 때만
    float distance = Mathf.Sqrt(sqrDistance);
}
```

### 2. 벡터 재사용

```csharp
// 비효율적
Vector3 direction = (target - position).normalized;
float distance = Vector3.Distance(target, position);

// 효율적
Vector3 offset = target - position;
float distance = offset.magnitude;
Vector3 direction = offset / distance;  // 0으로 나누기 확인
```

---

## 완료 체크리스트

- [ ] SteeringBehaviors.cs 구현 완료
- [ ] SteeringAgent.cs 구현 완료
- [ ] Lab05_SteeringBehaviors 씬 생성
- [ ] Seek 동작 확인
- [ ] Flee 동작 확인
- [ ] Arrive 동작 확인 (감속)
- [ ] Wander 동작 확인
- [ ] Pursue와 Evade 구현 (보너스)
- [ ] 여러 에이전트 동시 제어
- [ ] 행동 조합 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 60분
