# Lab 11: Unity ML-Agents 기계 학습

**소요 시간:** 120분
**난이도:** ⭐⭐⭐⭐ (상)
**연관 Part:** Part VI - 강화학습과 적응형 AI
**이전 Lab:** Lab 10 - Q-Learning 미로 학습

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **Unity ML-Agents 설정**
   - 설치 및 환경 구성
   - Python 환경 준비
   - 패키지 관리

2. **학습 환경 구축**
   - Agent 컴포넌트 구현
   - Academy/Manager 설정
   - 상태 입력 및 행동 출력

3. **모델 훈련**
   - PPO (Proximal Policy Optimization) 알고리즘
   - 하이퍼파라미터 튜닝
   - 훈련 모니터링

4. **훈련된 모델 적용**
   - 게임에서 모델 사용
   - 추론(Inference) 실행
   - 성능 평가

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── ML-Agents/
│   ├── Agents/
│   │   ├── LearningAgent.cs                  # ML-Agent (NEW)
│   │   └── AgentController.cs                # 에이전트 제어 (NEW)
│   ├── Academy/
│   │   └── TrainingAcademy.cs                # 훈련 아카데미 (NEW)
│   └── Environments/
│       ├── SimpleEnvironment.cs              # 단순 환경 (NEW)
│       └── ComplexEnvironment.cs             # 복잡한 환경 (NEW)
├── Prefabs/
│   └── Agents/
│       └── MLAgent.prefab                    # ML-Agent 프리팹 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab11_MLAgents.unity              # 실습 씬 (NEW)
└── Config/
    └── trainer_config.yaml                   # 훈련 설정 (NEW)
```

---

## 주요 개념

### 1. Unity ML-Agents 아키텍처

**Python 기반 학습, Unity에서 실행**

```
┌─────────────────────┐
│   Unity Editor      │
│  ┌───────────────┐  │
│  │   Agents      │  │
│  ├───────────────┤  │
│  │ • Observation │  │
│  │ • Action      │  │
│  │ • Reward      │  │
│  └───────────────┘  │
└──────────┬──────────┘
           │ (Protocol Buffer)
           ↓
┌─────────────────────────────┐
│  Python Training Process    │
│  ┌─────────────────────────┐│
│  │ PPO Algorithm           ││
│  │ • 신경망 훈련             ││
│  │ • 하이퍼파라미터 조정      ││
│  │ • 모델 저장/로드           ││
│  └─────────────────────────┘│
└─────────────────────────────┘
```

### 2. Agent 구현 기본

```csharp
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class LearningAgent : Agent
{
    // 관찰(Observation): 상태 정보
    // 행동(Action): 에이전트의 선택
    // 보상(Reward): 행동에 대한 평가

    public override void OnEpisodeBegin()
    {
        // 에피소드 시작 시 초기화
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 상태 정보 수집
        // 예: 위치, 속도, 거리 등
        sensor.AddObservation(transform.position);
        sensor.AddObservation(rb.velocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 행동 처리
        // actions.ContinuousActions[0] : 첫 번째 연속 행동 (-1 ~ 1)
        // actions.DiscreteActions[0] : 첫 번째 이산 행동 (0 ~ n)

        float moveForward = actions.ContinuousActions[0];
        float moveRight = actions.ContinuousActions[1];

        rb.velocity = new Vector3(moveRight * speed, rb.velocity.y, moveForward * speed);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 플레이어 제어 (수동 테스트용)
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    private void Update()
    {
        // 보상 계산
        if (Vector3.Distance(transform.position, goal) < 1f)
        {
            SetReward(+1f);
            EndEpisode();
        }

        if (transform.position.y < -5f)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}
```

### 3. 훈련 설정 (YAML)

```yaml
# trainer_config.yaml
behaviors:
  LearningAgent:
    trainer_type: ppo
    framework: onnx
    hyperparameters:
      batch_size: 64
      buffer_size: 10240
      learning_rate: 0.0003
      beta: 0.01
      epsilon: 0.2
      lambd: 0.99
      num_epoch: 3
      shared_critic: false
    network_settings:
      hidden_units: 128
      num_layers: 2
      activation: relu
      memory:
        sequence_length: 64
        memory_size: 256
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    checkpoint_interval: 500000
    max_steps: 1000000
    time_horizon: 64
    summary_freq: 10000
```

### 4. 에피소드 루프

```
┌─────────────────────────────┐
│  Episode Start              │
│  OnEpisodeBegin()           │
│  (위치 초기화 등)           │
└────────────┬────────────────┘
             │
             ↓
    ┌──────────────────┐
    │ Step Loop        │
    │                  │
    │ 1. Observation   │
    │    수집          │
    │                  │
    │ 2. Action 선택   │
    │    (신경망)      │
    │                  │
    │ 3. 행동 수행     │
    │    물리 시뮬레이션
    │                  │
    │ 4. Reward 계산   │
    │    AddReward()   │
    │                  │
    │ 5. Episode 종료? │
    │    EndEpisode()  │
    │                  │
    └──────────┬───────┘
               │
               ↓
    Episode 완료 (다시 반복)
```

---

## 설치 및 환경 구성

### Step 1: Python 설치 (Windows)

```
1. Python 3.8 또는 3.9 다운로드
   https://www.python.org/downloads/

2. 설치 시 "Add Python to PATH" 체크

3. 터미널에서 확인:
   python --version
   pip --version
```

### Step 2: ML-Agents Package 설치

```
Unity 버전: 2021.3 LTS 이상

1. Package Manager에서 설치:
   Window → TextEditor Manager
   "ml-agents" 검색
   버전 20.0.0 이상 설치

2. 또는 수동 설치:
   git clone https://github.com/Unity-Technologies/ml-agents.git
   cd ml-agents
   pip install -e ./ml-agents-envs
   pip install -e ./ml-agents
```

### Step 3: 훈련 도구 설정

```
PyTorch 설치 (GPU):
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118

PyTorch 설치 (CPU):
pip install torch torchvision torchaudio
```

### Step 4: 프로젝트 구성

```
Assets/
├── ML-Agents/          (ML-Agents 패키지 자동)
├── Scripts/
│   └── ML-Agents/
├── Scenes/
│   └── Training/       (훈련용 씬)
└── Models/             (훈련된 모델 저장)

프로젝트 루트:
├── trainer_config.yaml (훈련 설정)
└── results/            (훈련 결과)
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab11_MLAgents
```

### Step 2: 기본 환경 구성

#### 2-1. 지형 생성
```
Hierarchy → 3D Object → Plane
이름: Ground
Transform:
  - Position: (0, 0, 0)
  - Scale: (20, 1, 20)
```

#### 2-2. 에이전트 생성
```
Hierarchy → 3D Object → Sphere
이름: LearningAgent
Transform:
  - Position: (0, 1, 0)
  - Scale: (0.5, 0.5, 0.5)

Add Component:
  - Rigidbody
  - Collider
  - LearningAgent (스크립트)
```

#### 2-3. 목표 배치
```
Hierarchy → 3D Object → Cube
이름: Goal
Transform:
  - Position: (5, 1, 5)
  - Scale: (0.5, 0.5, 0.5)
Material: 초록색 (목표임을 표시)
```

#### 2-4. 카메라 설정
```
Main Camera
Position: (0, 5, -5)
Rotation: (30, 0, 0)
```

### Step 3: ML-Agent 설정

#### 3-1. Agent 컴포넌트
```
LearningAgent 선택
Inspector → LearningAgent 스크립트

설정:
  - Max Step: 5000
  - Behavior Parameters 할당
```

#### 3-2. Behavior Parameters
```
LearningAgent에 자동 생성됨

설정:
  - Vector Observation Space:
    * Space Type: Continuous
    * Space Size: 6 (position x3 + velocity x3)
  - Action Space:
    * Type: Continuous
    * Space Size: 2 (forward, right movement)
```

### Step 4: 훈련 설정

#### 4-1. trainer_config.yaml 생성
```
프로젝트 루트에 생성

behaviors:
  LearningAgent:
    trainer_type: ppo
    hyperparameters:
      learning_rate: 0.0003
      batch_size: 64
      buffer_size: 10240
      num_epoch: 3
    network_settings:
      hidden_units: 128
      num_layers: 2
    max_steps: 100000
```

#### 4-2. 훈련 실행
```
터미널에서:
cd <Project_Root>

mlagents-learn trainer_config.yaml --run-id=lab11_first_run

--run-id: 훈련 이름 (결과 폴더)
```

#### 4-3. 훈련 중 Unity 실행
```
1. 터미널에서 "mlagents-learn" 명령 실행
2. Unity Editor → Play 버튼 클릭
3. 에이전트가 움직이면서 훈련 진행

Ctrl+C로 훈련 중단
```

---

## 구현 체크리스트

### Phase 1: 환경 설정 (20분)

- [ ] Python 설치 및 확인
- [ ] ML-Agents 패키지 설치
- [ ] PyTorch 설치
- [ ] mlagents-learn 명령 확인

### Phase 2: 씬 구성 (20분)

- [ ] 기본 지형 생성
- [ ] 에이전트 오브젝트 생성
- [ ] 목표 오브젝트 생성
- [ ] 카메라 설정

### Phase 3: Agent 스크립트 (30분)

- [ ] LearningAgent.cs 작성
  - [ ] OnEpisodeBegin() 구현
  - [ ] CollectObservations() 구현
  - [ ] OnActionReceived() 구현
  - [ ] Heuristic() 구현 (수동 제어)
- [ ] Behavior Parameters 설정

### Phase 4: 훈련 설정 (15분)

- [ ] trainer_config.yaml 작성
- [ ] 하이퍼파라미터 설정
- [ ] mlagents-learn 명령 실행

### Phase 5: 훈련 및 모니터링 (25분)

- [ ] 훈련 프로세스 시작
- [ ] 에이전트 동작 확인
- [ ] TensorBoard로 훈련 진행도 모니터링
- [ ] 수렴 확인 (학습 곡선 평탄화)

### Phase 6: 모델 적용 (10분)

- [ ] 훈련된 모델 로드
- [ ] 추론 테스트
- [ ] 게임에서 사용

---

## 참고 자료

### 공식 문서

- **Unity ML-Agents Documentation:**
  - https://github.com/Unity-Technologies/ml-agents
- **ML-Agents GitHub Wiki:**
  - 튜토리얼 및 예제

### 추천 자료

- **"Practical Deep Reinforcement Learning"**
- **OpenAI Gym 튜토리얼**

---

## 주요 개념 심화

### 1. Observation 설계

```csharp
public override void CollectObservations(VectorSensor sensor)
{
    // 좋은 Observation:
    // - 필요한 정보만 포함 (노이즈 최소화)
    // - 정규화된 값 (-1 ~ 1 범위)

    // 위치 정규화 (공간이 -10 ~ 10이라고 가정)
    sensor.AddObservation(transform.position.x / 10f);
    sensor.AddObservation(transform.position.z / 10f);

    // 목표와의 거리
    Vector3 directionToGoal = (goal.position - transform.position).normalized;
    sensor.AddObservation(directionToGoal);

    // 속도
    sensor.AddObservation(rb.velocity.x / maxSpeed);
    sensor.AddObservation(rb.velocity.z / maxSpeed);
}
```

### 2. 보상 설계

```csharp
private void Update()
{
    // 거리 기반 보상 (목표에 가까워질수록 보상)
    float distanceToGoal = Vector3.Distance(transform.position, goal.position);
    float prevDistance = Vector3.Distance(prevPosition, goal.position);

    if (distanceToGoal < prevDistance)
    {
        AddReward(0.01f);  // 접근 시 작은 보상
    }

    // 목표 도달
    if (distanceToGoal < 1f)
    {
        SetReward(+1f);    // 큰 보상
        EndEpisode();
    }

    // 떨어짐 (경계 밖)
    if (transform.position.y < -5f)
    {
        SetReward(-1f);    // 페널티
        EndEpisode();
    }

    // 매 스텝 비용 (빨리 도달하도록)
    AddReward(-0.001f);

    prevPosition = transform.position;
}
```

### 3. 하이퍼파라미터 튜닝

```yaml
# 보수적 학습 (안정성 우선)
behaviors:
  Agent:
    hyperparameters:
      learning_rate: 0.0001
      beta: 0.05
      epsilon: 0.2
      buffer_size: 20480
      num_epoch: 3

# 공격적 학습 (빠른 수렴)
behaviors:
  Agent:
    hyperparameters:
      learning_rate: 0.001
      beta: 0.01
      epsilon: 0.3
      buffer_size: 5120
      num_epoch: 1
```

---

## 트러블슈팅

### 문제 1: mlagents-learn이 실행되지 않음

**확인:**
```
터미널에서:
mlagents-learn --help

설치되지 않았으면:
pip install mlagents
```

### 문제 2: "No Agents" 오류

**원인:** 에이전트가 Academy 또는 환경에 연결되지 않음

**해결책:**
```csharp
// LearningAgent 스크립트에서
void Start()
{
    // Behavior Parameters가 할당되어 있는지 확인
    if (behaviorParameters == null)
        Debug.LogError("Behavior Parameters not assigned!");
}
```

### 문제 3: 훈련이 진행되지 않음

**원인:** Play 버튼을 누르지 않음

**해결책:**
```
1. 터미널: mlagents-learn 실행
2. 대기 메시지 표시
3. Unity Editor에서 Play 버튼 클릭
4. 훈련 시작
```

### 문제 4: 에이전트가 목표를 찾지 못함

**원인:** 보상 신호가 약함 또는 탐색 부족

**해결책:**
```csharp
// 목표 도달 보상 증가
if (distanceToGoal < 1f)
{
    SetReward(+10f);  // 1f → 10f
    EndEpisode();
}

// 또는 trainer_config에서 탐색 증가
hyperparameters:
  beta: 0.1  # 엔트로피 증가 (탐색 강화)
```

---

## 확장 아이디어

### 아이디어 1: 여러 에이전트 훈련

```csharp
// 동시에 여러 에이전트 훈련
// (병렬 학습으로 수렴 가속)

public class MultiAgentTrainer : MonoBehaviour
{
    public LearningAgent[] agents;

    void Start()
    {
        // 각 에이전트가 독립적으로 훈련
        foreach (var agent in agents)
        {
            agent.gameObject.SetActive(true);
        }
    }
}
```

### 아이디어 2: 점진적 학습 (Curriculum Learning)

```yaml
# 단계별로 난이도 증가
behaviors:
  Agent:
    curriculum:
      config:
        - name: easy
          initial_value: 0.0
          final_value: 0.0
        - name: medium
          initial_value: 0.0
          final_value: 0.5
        - name: hard
          initial_value: 0.0
          final_value: 1.0
```

### 아이디어 3: 복합 보상

```csharp
// 여러 목표를 동시에 학습
public override void Update()
{
    float reward = 0f;

    // 목표 도달
    if (Vector3.Distance(transform.position, goal) < 1f)
        reward += 1f;

    // 에너지 효율 (느린 이동)
    reward += (maxSpeed - rb.velocity.magnitude) * 0.001f;

    // 안정성 (넘어지지 않음)
    if (transform.rotation.x < 0.3f)
        reward += 0.01f;

    AddReward(reward);
}
```

---

## 완료 체크리스트

- [ ] Python 및 필요한 패키지 설치
- [ ] ML-Agents 패키지 설치 완료
- [ ] 기본 훈련 씬 생성
- [ ] LearningAgent.cs 구현
- [ ] Behavior Parameters 설정
- [ ] trainer_config.yaml 작성
- [ ] 첫 훈련 실행 (mlagents-learn)
- [ ] TensorBoard로 진행도 모니터링
- [ ] 100,000 스텝 이상 훈련
- [ ] 수렴 확인 (학습 곡선 평탄화)
- [ ] 훈련된 모델 적용
- [ ] 최종 성능 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 120분

---

## 추가 주의사항

### GPU 사용 (선택)

```
CUDA Toolkit 설치 (NVIDIA 그래픽 카드):
https://developer.nvidia.com/cuda-toolkit

cuDNN 설치:
https://developer.nvidia.com/cudnn

더 빠른 훈련 가능 (CPU 대비 5-10배)
```

### 모델 저장 및 로드

```csharp
// 훈련된 모델 로드
public LearningAgent : Agent
{
    void Start()
    {
        // Model.onnx를 Behavior Parameters에 할당
        // (또는 자동으로 results 폴더에서 로드)
    }
}

// 결과 폴더 구조:
// results/
//   └─ lab11_first_run/
//       ├─ LearningAgent.onnx (훈련된 모델)
//       ├─ checkpoint.pt
//       ├─ events.out.tfevents...
//       └─ run_logs/
```

### TensorBoard로 훈련 모니터링

```
터미널에서:
tensorboard --logdir=results

브라우저에서:
http://localhost:6006

그래프:
- Cumulative Reward (누적 보상)
- Episode Length (에피소드 길이)
- Loss (손실값)
```
