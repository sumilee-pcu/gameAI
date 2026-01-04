# Lab 10: Q-Learning 미로 학습

**소요 시간:** 90분
**난이도:** ⭐⭐⭐⭐ (상)
**연관 Part:** Part VI - 강화학습과 적응형 AI
**이전 Lab:** Lab 9 - GOAP 목표 지향 AI

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **강화학습(Reinforcement Learning) 기초**
   - 에이전트와 환경의 상호작용
   - 상태-행동-보상 루프
   - Markov Decision Process (MDP)

2. **Q-Learning 알고리즘**
   - Q-Table 구현 및 관리
   - Q-값 업데이트 공식
   - ε-greedy 정책 (탐색 vs 활용)

3. **게임 환경 구축**
   - 미로 환경 설정
   - 상태 정의 및 변환
   - 보상 함수 설계

4. **학습 및 평가**
   - 에이전트 훈련
   - 학습 곡선 시각화
   - 최적 정책 추출

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── Reinforcement Learning/
│   │   ├── QLearningAgent.cs                 # Q-Learning 에이전트 (NEW)
│   │   ├── QLearningTrainer.cs               # 훈련 루프 (NEW)
│   │   └── QTable.cs                         # Q-Table 관리 (NEW)
│   ├── Environment/
│   │   ├── MazeEnvironment.cs                # 미로 환경 (NEW)
│   │   ├── Cell.cs                           # 셀 정의 (NEW)
│   │   └── RewardCalculator.cs               # 보상 함수 (NEW)
│   └── Visualization/
│       ├── TrainingVisualizer.cs             # 학습 시각화 (NEW)
│       └── PolicyVisualizer.cs               # 정책 시각화 (NEW)
├── Prefabs/
│   └── Agents/
│       ├── LearningAgent.prefab              # 학습 에이전트 (NEW)
│       └── MazeCell.prefab                   # 미로 셀 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab10_QLearning.unity             # 실습 씬 (NEW)
└── Data/
    ├── QTable.json                          # 저장된 Q-Table
    └── TrainingLog.csv                      # 훈련 로그
```

---

## 주요 개념

### 1. 강화학습의 기본 원리

```
에이전트와 환경의 상호작용:

환경         에이전트
  ↑            ↑
  │            │
상태 ← ─────────┤
  │            │
  │─→ 행동 ────→
  │            │
보상 ←─────────┘

매 스텝마다:
1. 에이전트가 상태 S를 관찰
2. 행동 A를 선택
3. 환경이 보상 R과 새로운 상태 S'를 반환
4. 에이전트가 (S, A, R, S')로부터 학습
```

### 2. Q-Learning 알고리즘

**핵심: Q(S, A) = 상태 S에서 행동 A의 기댓값**

```
Q-값 업데이트 공식:
Q(s,a) ← Q(s,a) + α[r + γ·max(Q(s',a')) - Q(s,a)]

α: 학습률 (0.1 ~ 0.5)
γ: 할인율 (0.9 ~ 0.99) - 미래 보상의 중요도
r: 즉시 보상
max(Q(s',a')): 다음 상태의 최대 Q-값
```

#### 단계별 예시

```
미로 학습:
현재 상태 s = (2,2)
가능한 행동: [위, 아래, 좌, 우]

Q(2,2,위) = -10 (벽)
Q(2,2,아래) = -5  (유효)
Q(2,2,좌) = -5  (유효)
Q(2,2,우) = 100  (목표 근처)

ε-greedy로 우를 선택:
- 확률 (1-ε)로 최고값 행동 선택 (우)
- 확률 ε로 무작위 행동 선택 (탐색)

결과:
보상 r = 50 (목표에 더 가까움)
다음 상태 s' = (2,3)
max(Q(2,3,a')) = 100 (우)

Q(2,2,우) ← Q(2,2,우) + 0.1[50 + 0.99·100 - 100]
         ← 100 + 0.1[50 + 99 - 100]
         ← 100 + 0.1[49]
         ← 104.9
```

### 3. Q-Table 구조

```
             행동
          좌   우   위   아래
상태 (0,0) [10, 15,  8,  5]
     (0,1) [20, 30, 12,  8]
     (0,2) [50, 80, 40, 20]
     ...

이 테이블을 학습을 통해 채우는 과정
```

### 4. 정책 (Policy)

```
학습된 Q-Table에서 정책 추출:

π(s) = argmax_a Q(s, a)

의미: 각 상태에서 최고 Q-값을 가진 행동을 선택
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab10_QLearning
```

### Step 2: 미로 환경 생성

#### 2-1. 미로 그리드 생성
```
Hierarchy → Create Empty
이름: Maze

Maze 내부에 5x5 또는 10x10 그리드:
for x in 0..9:
    for y in 0..9:
        Plane 생성
        위치: (x, 0, y)
        색상: 흰색 (이동 가능) 또는 검은색 (벽)
```

#### 2-2. 시작점과 목표점
```
시작점 (Start):
  Sphere (초록색)
  위치: (1, 0.5, 1)

목표점 (Goal):
  Cube (빨간색)
  위치: (8, 0.5, 8)
```

#### 2-3. 장애물 배치
```
벽: 일부 셀을 검은색으로 칠함
→ 미로 경로 복잡화
```

### Step 3: 에이전트 설정

#### 3-1. 학습 에이전트
```
Hierarchy → 3D Object → Sphere
이름: LearningAgent
Position: (1, 0.5, 1) (시작점)
Scale: (0.5, 0.5, 0.5)
Material: 파란색

Add Component → QLearningAgent
```

#### 3-2. 에이전트 설정
```
Inspector:
  - Grid Size: 10
  - Learning Rate (α): 0.1
  - Discount Rate (γ): 0.99
  - Epsilon (ε): 0.1
```

### Step 4: 훈련 시스템

#### 4-1. 훈련 매니저
```
Hierarchy → Create Empty
이름: TrainingManager

Add Component → QLearningTrainer

Inspector:
  - Agent: LearningAgent
  - Episodes: 1000
  - Max Steps Per Episode: 100
```

#### 4-2. 시각화
```
Add Component → TrainingVisualizer

이것이 학습 곡선을 표시할 예정
```

### Step 5: 카메라 설정

```
Main Camera
Position: (4.5, 10, 4.5)
Rotation: (90, 0, 0)

위에서 내려다보기로 미로 전체 볼 수 있음
```

---

## 구현 체크리스트

### Phase 1: Q-Learning 기본 (30분)

- [ ] QTable.cs 구현
  - [ ] 2D 배열 또는 Dictionary로 Q-값 저장
  - [ ] Q(s, a) 접근 메서드
  - [ ] Q-값 업데이트
- [ ] QLearningAgent.cs
  - [ ] 상태-행동 정의
  - [ ] ε-greedy 정책 구현
  - [ ] Q-값 업데이트 공식 구현

### Phase 2: 환경 및 보상 (20분)

- [ ] MazeEnvironment.cs
  - [ ] 미로 맵 생성
  - [ ] 이동 가능/불가능 판별
  - [ ] 상태 전환 (에이전트 이동)
- [ ] RewardCalculator.cs
  - [ ] 목표 도달: +100
  - [ ] 한 스텝: -1 (빨리 도달하도록)
  - [ ] 벽 충돌: -10

### Phase 3: 훈련 루프 (25분)

- [ ] QLearningTrainer.cs
  - [ ] Episode 루프
  - [ ] Step 루프
  - [ ] 상태 초기화
  - [ ] 행동 선택
  - [ ] 보상 계산
  - [ ] Q-값 업데이트

### Phase 4: 시각화 및 테스트 (15분)

- [ ] TrainingVisualizer
  - [ ] 학습 곡선 표시
  - [ ] 평균 보상 추적
- [ ] PolicyVisualizer
  - [ ] 학습된 정책 시각화
  - [ ] 각 셀의 최적 행동 표시
- [ ] 에이전트 테스트
  - [ ] 훈련 전: 무작위 행동
  - [ ] 훈련 후: 최적 경로 추적

---

## 참고 자료

### 공식 문헌

- **"Reinforcement Learning: An Introduction"** - Sutton & Barto
  - Chapter 6: Q-Learning
- **"Deep Reinforcement Learning Hands-On"** - Maxim Lapan

### 온라인 자료

- **ArXiv 논문: Q-Learning**
  - Watkins & Dayan (1992)

### 추천 영상

- DeepMind의 강화학습 강의
- Andrej Karpathy의 RL 튜토리얼

---

## 주요 개념 심화

### 1. Q-Learning 구현

```csharp
public class QLearningAgent : MonoBehaviour
{
    private QTable qTable;
    private float learningRate = 0.1f;
    private float discountRate = 0.99f;
    private float epsilon = 0.1f;

    void Start()
    {
        qTable = new QTable(gridSize, gridSize, actionCount);
    }

    public int SelectAction(Vector2Int state)
    {
        // ε-greedy 정책
        if (Random.value < epsilon)
        {
            // 탐색: 무작위 행동
            return Random.Range(0, actionCount);
        }
        else
        {
            // 활용: 최고 Q-값 행동
            return qTable.GetBestAction(state);
        }
    }

    public void Learn(Vector2Int state, int action,
                     float reward, Vector2Int nextState)
    {
        // Q-값 업데이트
        float currentQ = qTable.Get(state, action);
        float maxNextQ = qTable.GetMaxValue(nextState);

        float newQ = currentQ + learningRate *
                    (reward + discountRate * maxNextQ - currentQ);

        qTable.Set(state, action, newQ);
    }
}
```

### 2. 탐색 vs 활용 (Exploration vs Exploitation)

```
ε-greedy 정책:
- ε = 0.1 (10% 탐색, 90% 활용) ← 균형잡힌 선택
- ε = 0.0 (0% 탐색, 100% 활용) ← 완전히 활용
- ε = 1.0 (100% 탐색, 0% 활용) ← 완전히 탐색

권장: 초반에는 높은 ε, 시간이 지나면서 감소
ε = 0.1 - (episode / max_episodes) * 0.09
```

### 3. 보상 설계

```csharp
private float CalculateReward(Vector2Int position,
                             Vector2Int goal,
                             int stepCount)
{
    // 목표 도달
    if (position == goal)
        return 100f;

    // 벽에 부딪힘
    if (!IsWalkable(position))
        return -10f;

    // 한 스텝의 비용
    float stepPenalty = -1f;

    // 목표까지의 거리 (보너스)
    float distanceReward = -Vector2Int.Distance(position, goal);

    return stepPenalty + distanceReward * 0.1f;
}
```

### 4. 학습 곡선 분석

```
훈련 진행:

Episode 1-100:    급격한 성능 향상
                  보상: -50 → -10

Episode 100-500:  점진적 개선
                  보상: -10 → -5

Episode 500-1000: 수렴
                  보상: -5 (안정적)

학습 완료: 에이전트가 최단 경로 발견
```

---

## 트러블슈팅

### 문제 1: Q-값이 발산 (계속 커짐)

**원인:** 학습률이 너무 높음 또는 보상이 너무 큼

**해결책:**
```csharp
// 학습률 감소
learningRate = 0.01f;  // 0.1 → 0.01

// 또는 Q-값 정규화
newQ = Mathf.Clamp(newQ, -100f, 100f);
```

### 문제 2: 에이전트가 목표에 도달하지 못함

**원인:**
- 보상 함수가 나쁨 (목표 도달의 보상이 너무 작음)
- 탐색이 부족함 (ε가 너무 작음)

**해결책:**
```csharp
// 목표 도달 보상 증가
if (position == goal)
    return 1000f;  // 100 → 1000

// 탐색 증가
epsilon = 0.3f;  // 0.1 → 0.3
```

### 문제 3: 학습이 너무 느림

**원인:** 미로가 너무 크거나 에피소드가 부족

**해결책:**
```csharp
// 더 작은 미로부터 시작
gridSize = 5;  // 10 → 5

// 더 많은 에피소드
maxEpisodes = 5000;  // 1000 → 5000

// 또는 학습률 증가 (위험성 있음)
learningRate = 0.2f;  // 0.1 → 0.2
```

---

## 확장 아이디어

### 아이디어 1: 여러 에이전트

```csharp
// 두 에이전트가 동시에 학습
// 서로를 피하면서 목표 도달

public class MultiAgentQLearning
{
    private QLearningAgent[] agents;

    void Train()
    {
        for (int episode = 0; episode < maxEpisodes; episode++)
        {
            Vector2Int[] positions = new Vector2Int[agents.Length];

            for (int step = 0; step < maxSteps; step++)
            {
                // 각 에이전트가 동시에 행동
                for (int i = 0; i < agents.Length; i++)
                {
                    int action = agents[i].SelectAction(positions[i]);
                    Vector2Int nextPos = MoveAgent(positions[i], action);

                    // 충돌 확인
                    if (positions.Contains(nextPos))
                        nextPos = positions[i];  // 움직이지 않음

                    float reward = CalculateReward(nextPos, agent.goal);
                    agents[i].Learn(positions[i], action, reward, nextPos);

                    positions[i] = nextPos;
                }
            }
        }
    }
}
```

### 아이디어 2: 여러 목표

```csharp
// 여러 목표점을 순서대로 방문

public class MultiGoalMaze
{
    private Vector2Int[] goals;
    private int currentGoalIndex = 0;

    public float CalculateReward(Vector2Int position, int stepCount)
    {
        Vector2Int currentGoal = goals[currentGoalIndex];

        if (position == currentGoal)
        {
            currentGoalIndex++;
            if (currentGoalIndex >= goals.Length)
                return 10000f;  // 모든 목표 달성!
            return 100f;  // 현재 목표 달성
        }

        return -1f;
    }
}
```

### 아이디어 3: 깊은 Q-Learning (DQN)

```csharp
// 신경망으로 Q-함수 근사
// 입력: 상태 (이미지 또는 벡터)
// 출력: 각 행동의 Q-값

public class DeepQLearningAgent : MonoBehaviour
{
    private NeuralNetwork qNetwork;

    public int SelectAction(float[] state)
    {
        float[] qValues = qNetwork.Forward(state);
        return System.Array.IndexOf(qValues, qValues.Max());
    }

    public void Learn(float[] state, int action,
                     float reward, float[] nextState)
    {
        float[] nextQValues = qNetwork.Forward(nextState);
        float targetQ = reward + discountRate * nextQValues.Max();

        // 신경망 훈련
        qNetwork.Train(state, action, targetQ);
    }
}
```

---

## 완료 체크리스트

- [ ] QTable.cs 구현
- [ ] QLearningAgent.cs 구현 (ε-greedy 포함)
- [ ] MazeEnvironment.cs 구현
- [ ] RewardCalculator.cs 구현
- [ ] QLearningTrainer.cs 구현
- [ ] 미로 씬 생성
- [ ] 기본 훈련 루프 테스트
- [ ] 100 에피소드 훈련 후 성능 확인
- [ ] 학습 곡선 시각화
- [ ] 최종 정책 시각화
- [ ] 다양한 미로 크기로 테스트
- [ ] 씬 저장

---

## 성능 비교

### 훈련 전후

```
훈련 전 (ε=1.0, 완전 탐색):
- 평균 에피소드 길이: 250+ 스텝
- 도달율: 30%

훈련 중 (1000 에피소드):
- Episode 100: 길이 150 스텝, 도달율 70%
- Episode 500: 길이 30 스텝, 도달율 95%
- Episode 1000: 길이 15 스텝, 도달율 100%

훈련 후 (ε=0.0, 완전 활용):
- 평균 에피소드 길이: 15 스텝 (최단 경로)
- 도달율: 100%
```

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 90분
