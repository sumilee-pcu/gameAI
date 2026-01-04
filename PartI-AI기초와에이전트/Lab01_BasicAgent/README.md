# Lab 1: Unity 기초와 AI 프로젝트 설정

**소요 시간:** 30분
**난이도:** ⭐ 초급
**연관 Part:** Part I - AI 기초와 에이전트

## 학습 목표

- Unity 프로젝트를 생성하고 기본 구조를 이해합니다.
- AI 개발에 필요한 폴더 구조와 네이밍 컨벤션을 설정합니다.
- 간단한 에이전트 이동과 디버그 시각화를 구현합니다.

## 파일 구조

```
Lab01_BasicAgent/
├── Scripts/
│   ├── SimpleAgent.cs              # 기본 에이전트 클래스
│   ├── PlayerControlledAgent.cs    # 키보드 제어 에이전트
│   ├── WaypointPatrol.cs           # 순찰 에이전트
│   └── MouseClickAgent.cs          # 마우스 클릭 이동 에이전트
├── Prefabs/
└── README.md
```

## 주요 스크립트 설명

### 1. SimpleAgent.cs

모든 AI 에이전트의 기반이 되는 추상 클래스입니다.

**주요 기능:**
- Rigidbody 기반 물리 이동
- 가상 메서드를 통한 상속 구조
- 디버그 시각화 (Gizmos, Debug.DrawRay)

**핵심 메서드:**
```csharp
protected virtual void UpdateAI()      // AI 로직 (오버라이드 필요)
protected virtual void ApplyMovement() // 물리 이동 적용
protected Vector3 CalculateVelocityToTarget(Vector3 targetPosition)
```

### 2. PlayerControlledAgent.cs

키보드 입력(WASD/화살표)으로 제어 가능한 에이전트입니다.

**사용법:**
- W/S 또는 ↑/↓: 전진/후진
- A/D 또는 ←/→: 좌/우 이동

### 3. WaypointPatrol.cs

미리 설정된 경로점(Waypoint)을 순환하며 순찰하는 에이전트입니다.

**설정 방법:**
1. Inspector에서 `Waypoints` 배열 크기 설정
2. 각 Element에 Transform 드래그
3. `Arrival Distance` 조정 (기본: 0.5m)

### 4. MouseClickAgent.cs

마우스 클릭한 지점으로 자동 이동하는 에이전트입니다.

**사용법:**
- 좌클릭: 목표 지점 설정
- Space키: 큰 소음 발생 (Lab 3 연계)

## Unity 씬 설정 가이드

### Step 1: 기본 환경 구성

1. **바닥 생성**
   ```
   Hierarchy → 우클릭 → 3D Object → Plane
   - Position: (0, 0, 0)
   - Scale: (5, 1, 5)
   ```

2. **에이전트 생성**
   ```
   Hierarchy → 우클릭 → 3D Object → Capsule
   - Name: Agent
   - Position: (0, 1, 0)
   - Add Component → Rigidbody
     - Constraints → Freeze Rotation: X, Z 체크
   ```

3. **스크립트 추가**
   ```
   Inspector → Add Component → Simple Agent
   또는 Player Controlled Agent
   ```

### Step 2: 순찰 경로 설정 (WaypointPatrol 사용 시)

1. **경로점 생성**
   ```
   Hierarchy → Create Empty → Waypoints
   하위에 4개의 Empty Object:
     - Waypoint_0: (5, 0, 5)
     - Waypoint_1: (5, 0, -5)
     - Waypoint_2: (-5, 0, -5)
     - Waypoint_3: (-5, 0, 5)
   ```

2. **WaypointPatrol 설정**
   - Agent에 WaypointPatrol 컴포넌트 추가
   - Waypoints 배열에 4개 Transform 할당

### Step 3: 카메라 설정

```
Main Camera
- Position: (0, 10, -10)
- Rotation: (45, 0, 0)
```

또는 Cinemachine 사용:
```
GameObject → Cinemachine → Virtual Camera
- Follow Target: Agent
```

## 테스트 방법

1. **SimpleAgent 테스트**
   - Play 버튼 클릭
   - Agent가 앞으로 이동하는지 확인
   - Scene 뷰에서 초록색(속도), 파란색(전방) 화살표 확인

2. **PlayerControlledAgent 테스트**
   - WASD 또는 화살표 키로 이동
   - 에디터 텍스트로 속도 확인 (Scene 뷰)

3. **WaypointPatrol 테스트**
   - Enemy가 사각형 경로 순찰 확인
   - 시안색 경로선, 노란색 목표선 확인

4. **MouseClickAgent 테스트**
   - Game 뷰에서 바닥 클릭
   - Agent가 클릭 지점으로 이동 확인

## 디버그 정보 확인

### Gizmos 활성화
```
Scene 뷰 → 우상단 Gizmos 버튼 클릭
```

### 표시되는 정보
- **초록색 화살표**: 현재 속도 벡터
- **파란색 화살표**: 전방 방향
- **노란색 구체**: 에이전트 위치 (선택 시)
- **시안색 선**: 순찰 경로
- **노란색 선**: 현재 목표까지의 거리

## 주요 개념

### 1. Rigidbody vs Transform 이동

**Transform 이동 (비추천):**
```csharp
transform.position += velocity * Time.deltaTime; // 물리 무시
```

**Rigidbody 이동 (권장):**
```csharp
rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime); // 충돌 고려
```

### 2. Update vs FixedUpdate

- **Update()**: AI 의사결정, 입력 처리 (프레임당 1회, 가변 간격)
- **FixedUpdate()**: 물리 연산 (고정 간격, 기본 0.02초)

### 3. 상속을 통한 확장

```csharp
public class MyCustomAgent : SimpleAgent
{
    protected override void UpdateAI()
    {
        // 커스텀 AI 로직
        velocity = CalculateVelocityToTarget(target.position);
    }
}
```

## 트러블슈팅

### Q: Agent가 계속 회전합니다
**A:** Rigidbody → Constraints → Freeze Rotation: X, Z 체크

### Q: 이동이 너무 빠릅니다
**A:** Move Speed 값을 3~5로 조정

### Q: 디버그 화살표가 보이지 않습니다
**A:** Show Debug Info 체크 확인, Gizmos 버튼 활성화

### Q: WaypointPatrol이 작동하지 않습니다
**A:** Waypoints 배열이 제대로 할당되었는지 확인

## 확장 아이디어

1. **속도 변화 추가**
   - Shift 키로 달리기 구현
   - 지형에 따라 속도 변화

2. **회전 제한**
   - 최대 회전 속도 제한
   - 부드러운 회전 (Slerp)

3. **애니메이션 연동**
   - Animator Controller 추가
   - 속도에 따라 애니메이션 블렌딩

4. **경로 자동 생성**
   - 런타임에 Waypoint 자동 배치
   - 랜덤 경로 생성

## 다음 단계

Lab 2에서는 FSM(유한 상태 기계)을 구현하여 순찰/추적/공격 상태를 가진 본격적인 AI를 만듭니다.

---

**완료 체크리스트:**
- [ ] SimpleAgent로 기본 이동 구현
- [ ] PlayerControlledAgent로 키보드 제어
- [ ] WaypointPatrol로 순찰 경로 구현
- [ ] MouseClickAgent로 클릭 이동
- [ ] 디버그 시각화 확인
