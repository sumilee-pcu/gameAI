# 게임 인공지능 - Unity 실습 코드

**NPC의 움직임과 판단, 학습을 만드는 13가지 알고리즘 패턴**

Unity로 배우는 게임 AI 교재의 실습 코드 리포지토리입니다.

## 📚 교재 소개

이 리포지토리는 "게임 인공지능" 교재의 Part I ~ Part VII에서 다루는 13개 Lab의 Unity 실습 코드를 제공합니다.

### Part 구성

| Part | 제목 | Lab |
|------|------|-----|
| **Part I** | AI 기초와 에이전트 | Lab 1-2 |
| **Part II** | 환경 인지와 불확실성 | Lab 3 |
| **Part III** | 이동 알고리즘 | Lab 4-5 |
| **Part IV** | 경로 탐색 | Lab 6-7 |
| **Part V** | 고급 의사결정 | Lab 8-9 |
| **Part VI** | 강화학습과 적응형 AI | Lab 10-11 |
| **Part VII** | 절차적 콘텐츠 생성 | Lab 12-13 |

## 🎯 학습 목표

각 Lab을 완성하면서 다음을 학습할 수 있습니다:

- ✅ **FSM (Finite State Machine)**: 순찰/추적/공격 AI 구현
- ✅ **센서 시스템**: 시야각, 청각, 레이캐스트 기반 인지
- ✅ **Boids 알고리즘**: 군집 행동 시뮬레이션
- ✅ **Steering Behaviors**: Seek, Flee, Arrive, Pursue
- ✅ **A* 경로 탐색**: 그리드 기반 길찾기
- ✅ **NavMesh**: Unity 내장 경로 탐색
- ✅ **Behavior Tree**: 모듈화된 의사결정
- ✅ **GOAP**: 목표 지향 행동 계획
- ✅ **Q-Learning**: 강화학습 기초
- ✅ **Unity ML-Agents**: 딥러닝 기반 AI
- ✅ **Perlin Noise**: 절차적 지형 생성
- ✅ **Cellular Automata**: 던전 생성

## 🛠️ 환경 요구사항

### 필수 사항

| 항목 | 권장 | 최소 |
|------|------|------|
| **Unity 버전** | 2022.3 LTS 이상 | 2021.3 LTS |
| **개발 환경** | Visual Studio 2022 / Rider | VS 2019 |
| **.NET** | .NET Standard 2.1 | .NET Standard 2.0 |
| **OS** | Windows 10/11, macOS Monterey | Windows 8.1, macOS Big Sur |

### 선택 패키지

- ProBuilder (Lab 6, 7)
- Cinemachine (카메라 작업)
- Unity ML-Agents (Lab 11)

## 🚀 시작하기

### 1. Unity 프로젝트 생성

```bash
# Unity Hub에서 새 프로젝트 생성
# Template: 3D (URP 또는 Built-in)
# Project Name: GameAI_Tutorial
```

### 2. Lab 코드 가져오기

원하는 Lab의 폴더를 Unity 프로젝트의 `Assets/` 폴더에 복사합니다.

```
Unity_Project/
└── Assets/
    ├── PartI-AI기초와에이전트/
    │   └── Lab01_BasicAgent/
    └── ... (기타 Lab)
```

### 3. 씬 설정 및 테스트

각 Lab 폴더의 `README.md`를 참고하여 씬을 설정하고 테스트합니다.

## 📖 Lab 목록

### Part I: AI 기초와 에이전트

#### Lab 1: Unity 기초와 AI 프로젝트 설정 (30분)
- SimpleAgent 클래스 구현
- Rigidbody 기반 이동
- 디버그 시각화

#### Lab 2: FSM으로 순찰 AI 만들기 (60분)
- 유한 상태 기계 구현
- Patrol/Chase/Attack 상태
- 시야각 센서

### Part II: 환경 인지와 불확실성

#### Lab 3: 레이캐스트 센서 구현 (45분)
- VisionSensor (시야각)
- HearingSensor (청각)
- Investigate 상태

### Part III: 이동 알고리즘

#### Lab 4: Boids 군집 시뮬레이션 (90분)
- Separation/Alignment/Cohesion
- 장애물 회피
- 공간 분할 최적화

#### Lab 5: Steering Behaviors로 추적/회피 (60분)
- Seek, Flee, Arrive
- Pursue, Evade
- Wander

### Part IV: 경로 탐색

#### Lab 6: A* 경로 탐색 구현 (120분)
- 그리드 기반 A* 알고리즘
- 휴리스틱 함수
- 경로 스무딩

#### Lab 7: NavMesh 에이전트 활용 (45분)
- Unity NavMesh 설정
- NavMeshAgent 활용
- 동적 장애물

### Part V: 고급 의사결정

#### Lab 8: Behavior Tree 경비병 AI (120분)
- BT 노드 구조 (Selector/Sequence)
- 경비병 AI 구현
- Decorator 패턴

#### Lab 9: GOAP 목표 지향 AI (150분)
- GOAP 플래너
- Action/Goal 시스템
- 마을 주민 AI

### Part VI: 강화학습과 적응형 AI

#### Lab 10: Q-Learning 미로 학습 (90분)
- Q-Table 구현
- 보상 시스템
- 미로 탈출 학습

#### Lab 11: Unity ML-Agents 시작하기 (120분)
- ML-Agents 설치
- 학습 환경 구성
- 모델 훈련 및 추론

### Part VII: 절차적 콘텐츠 생성

#### Lab 12: Perlin Noise 지형 생성 (60분)
- Perlin Noise 알고리즘
- 지형 생성 및 시각화
- 다층 노이즈

#### Lab 13: 세포 자동자 던전 생성 (90분)
- Cellular Automata
- 동굴/던전 생성
- 연결성 보장

## 📁 리포지토리 구조

```
gameAI/
├── PartI-AI기초와에이전트/
│   ├── Lab01_BasicAgent/
│   └── Lab02_PatrolFSM/
├── PartII-환경인지와불확실성/
├── PartIII-이동알고리즘/
├── PartIV-경로탐색/
├── PartV-고급의사결정/
├── PartVI-강화학습과적응형AI/
├── PartVII-절차적콘텐츠생성/
├── Shared/             # 공통 유틸리티
└── Docs/               # 문서
```

자세한 구조는 [REPOSITORY_STRUCTURE.md](REPOSITORY_STRUCTURE.md)를 참고하세요.

## 💡 학습 가이드

### 권장 학습 순서

1. **기초 단계** (Lab 1-3): FSM과 센서 시스템 이해
2. **이동 단계** (Lab 4-5): 군집 행동과 조향 행동
3. **경로 단계** (Lab 6-7): 경로 탐색 알고리즘
4. **의사결정 단계** (Lab 8-9): BT와 GOAP
5. **학습 단계** (Lab 10-11): 강화학습
6. **생성 단계** (Lab 12-13): PCG

### 학습 팁

- 각 Lab은 이전 Lab의 내용을 기반으로 합니다
- 코드를 직접 타이핑하면서 학습하는 것을 권장합니다
- Scene 뷰에서 Gizmos를 활성화하여 디버그 정보를 확인하세요
- 각 Lab의 "실습" 섹션을 완료하여 심화 학습하세요

## 🔧 트러블슈팅

### 일반적인 문제

**Q: 스크립트 컴파일 에러가 발생합니다**
```
A: .NET Standard 2.1 설정 확인
   Edit → Project Settings → Player → Other Settings → Api Compatibility Level
```

**Q: Rigidbody가 계속 회전합니다**
```
A: Constraints → Freeze Rotation: X, Z 체크
```

**Q: NavMesh가 생성되지 않습니다**
```
A: Window → AI → Navigation → Bake 클릭
   Static 체크박스 확인
```

더 많은 문제 해결은 [Docs/TroubleShooting.md](Docs/TroubleShooting.md)를 참고하세요.

## 📝 라이선스

MIT License

본 프로젝트의 모든 코드는 교육 목적으로 자유롭게 사용, 수정, 배포할 수 있습니다.

## 🤝 기여하기

버그 리포트, 개선 제안, Pull Request를 환영합니다!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 문의

이슈가 있으시면 GitHub Issues에 등록해주세요.

## 🙏 감사의 말

이 교재와 실습 코드가 게임 AI 학습에 도움이 되기를 바랍니다!

---

**Happy Coding! 🎮**
