# Lab 13: 세포 자동자 던전 생성

**소요 시간:** 90분
**난이도:** ⭐⭐⭐⭐ (상)
**연관 Part:** Part VII - 절차적 콘텐츠 생성
**이전 Lab:** Lab 12 - Perlin Noise 지형 생성

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **세포 자동자(Cellular Automata) 원리**
   - 간단한 규칙으로 복잡한 패턴 생성
   - John Conway의 Game of Life
   - 게임용 던전 생성 변형

2. **던전 및 동굴 생성**
   - 방(Room) 배치
   - 통로(Corridor) 연결
   - 자연스러운 공간 생성

3. **맵 처리 및 최적화**
   - 고립된 공간 제거
   - 연결성 확보
   - 플레이 가능한 던전 검증

4. **게임 시스템 통합**
   - 던전을 씬으로 구현
   - 플레이어 스폰 및 몬스터 배치
   - 초상화 표시

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── PCG/
│   │   ├── CellularAutomata.cs              # CA 알고리즘 (NEW)
│   │   ├── DungeonGenerator.cs              # 던전 생성기 (NEW)
│   │   ├── CaveGenerator.cs                 # 동굴 생성 (NEW)
│   │   ├── MapAnalyzer.cs                   # 맵 분석 (NEW)
│   │   └── RoomGenerator.cs                 # 방 생성 (NEW)
│   ├── Dungeon/
│   │   ├── DungeonTile.cs                   # 타일 정의 (NEW)
│   │   ├── DungeonSpawner.cs                # 스폰 관리 (NEW)
│   │   └── DungeonVisualizer.cs             # 시각화 (NEW)
│   └── Utilities/
│       └── FloodFill.cs                     # 영역 판별 (NEW)
├── Prefabs/
│   ├── Dungeon/
│   │   ├── Wall.prefab                      # 벽 타일 (NEW)
│   │   ├── Floor.prefab                     # 바닥 타일 (NEW)
│   │   └── Door.prefab                      # 문 (NEW)
│   └── Agents/
│       ├── Player.prefab                    # 플레이어 (NEW)
│       └── Monster.prefab                   # 몬스터 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab13_CellularAutomata.unity     # 실습 씬 (NEW)
└── Data/
    └── DungeonMaps/
        ├── map_0.txt                        # 생성된 맵 저장
        └── map_1.txt
```

---

## 주요 개념

### 1. 세포 자동자의 원리

**간단한 지역 규칙 → 전체적 패턴**

```
Game of Life 규칙:
1. 살아있는 셀이 2-3개의 이웃을 가지면 생존
2. 죽은 셀이 정확히 3개의 이웃을 가지면 부활
3. 그 외는 죽음

던전 생성 규칙:
1. 벽으로 시작
2. 각 셀의 이웃 벽의 개수 세기
3. 이웃이 4개 이상이면 벽 유지, 아니면 빈 공간
4. 여러 회 반복하여 자연스러운 형태 생성
```

### 2. 던전 생성 알고리즘

```
1단계: 초기화
┌─────────────┐
│ # # # # # # │  # = 벽
│ # . . . # # │  . = 빈 공간
│ # . . . # # │
│ # # # # # # │
└─────────────┘

2단계: CA 반복 (5회)
각 셀에서 이웃 벽의 개수 계산:
  벽이 4개 이상이면 벽 유지
  벽이 3개 이하이면 빈 공간으로

결과:
┌─────────────┐
│ # # # # # # │
│ # . . . # # │
│ # . . . # # │
│ # . . . # # │
└─────────────┘

3단계: 방(Room) 생성
내부에 특별한 공간 추가

4단계: 통로(Corridor) 연결
방들을 연결하는 길 생성
```

### 3. Cellular Automata 구현

```csharp
public class CellularAutomata
{
    private int width, height;
    private bool[,] grid;        // true = 벽, false = 빈 공간
    private bool[,] newGrid;

    public void InitializeRandom(float fillProbability)
    {
        grid = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 확률에 따라 벽으로 초기화
                grid[x, y] = Random.value < fillProbability;
            }
        }
    }

    public void Simulate(int iterations)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            newGrid = (bool[,])grid.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 이웃하는 벽의 개수 세기
                    int wallCount = CountWallNeighbors(x, y);

                    // 규칙 적용
                    if (wallCount >= 4)
                    {
                        newGrid[x, y] = true;   // 벽 유지
                    }
                    else if (wallCount <= 2)
                    {
                        newGrid[x, y] = false;  // 빈 공간으로
                    }
                }
            }

            grid = newGrid;
        }
    }

    private int CountWallNeighbors(int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (grid[nx, ny])
                    count++;
            }
        }

        return count;
    }
}
```

### 4. 방(Room) 생성

```csharp
public class Room
{
    public int x, y;         // 위치
    public int width, height; // 크기
    public Vector2 center;

    public Room(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.center = new Vector2(x + width / 2, y + height / 2);
    }

    public bool Overlaps(Room other)
    {
        return !(x + width < other.x || x > other.x + other.width ||
                y + height < other.y || y > other.y + other.height);
    }
}

public class RoomGenerator
{
    public List<Room> GenerateRooms(int width, int height,
                                   int maxRooms, int minRoomSize)
    {
        List<Room> rooms = new List<Room>();

        for (int i = 0; i < maxRooms; i++)
        {
            int w = Random.Range(minRoomSize, minRoomSize * 2);
            int h = Random.Range(minRoomSize, minRoomSize * 2);
            int x = Random.Range(1, width - w - 1);
            int y = Random.Range(1, height - h - 1);

            Room newRoom = new Room(x, y, w, h);

            // 다른 방과 겹치지 않는지 확인
            bool overlap = false;
            foreach (var room in rooms)
            {
                if (newRoom.Overlaps(room))
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                rooms.Add(newRoom);
            }
        }

        return rooms;
    }

    public void ApplyRoomsToGrid(bool[,] grid, List<Room> rooms)
    {
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    grid[x, y] = false;  // 빈 공간
                }
            }
        }
    }
}
```

### 5. 통로(Corridor) 연결

```csharp
public class CorridorGenerator
{
    public void CreateCorridors(bool[,] grid, List<Room> rooms)
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2 currentCenter = rooms[i].center;
            Vector2 nextCenter = rooms[i + 1].center;

            // L자형 통로
            CreateHorizontalCorridor(grid,
                                   (int)currentCenter.x,
                                   (int)nextCenter.x,
                                   (int)currentCenter.y);

            CreateVerticalCorridor(grid,
                                (int)nextCenter.x,
                                (int)currentCenter.y,
                                (int)nextCenter.y);
        }
    }

    private void CreateHorizontalCorridor(bool[,] grid, int x1, int x2, int y)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
        {
            grid[x, y] = false;
            // 주변도 약간 넓게
            if (y > 0) grid[x, y - 1] = false;
            if (y < grid.GetLength(1) - 1) grid[x, y + 1] = false;
        }
    }

    private void CreateVerticalCorridor(bool[,] grid, int x, int y1, int y2)
    {
        for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
        {
            grid[x, y] = false;
            // 주변도 약간 넓게
            if (x > 0) grid[x - 1, y] = false;
            if (x < grid.GetLength(0) - 1) grid[x + 1, y] = false;
        }
    }
}
```

### 6. 연결성 검증 (Flood Fill)

```csharp
public class FloodFill
{
    public int GetConnectedRegionSize(bool[,] grid, int startX, int startY)
    {
        if (grid[startX, startY]) return 0;  // 벽이면 0

        Queue<(int, int)> queue = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        queue.Enqueue((startX, startY));
        visited.Add((startX, startY));

        int size = 0;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            size++;

            // 4방향 확인
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < grid.GetLength(0) &&
                        ny >= 0 && ny < grid.GetLength(1) &&
                        !grid[nx, ny] && !visited.Contains((nx, ny)))
                    {
                        visited.Add((nx, ny));
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        return size;
    }

    public bool IsMapConnected(bool[,] grid)
    {
        // 첫 번째 빈 공간에서 시작
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (!grid[x, y])
                {
                    int size = GetConnectedRegionSize(grid, x, y);
                    int totalFloors = CountFloors(grid);

                    // 모든 빈 공간이 연결되어 있는가?
                    return size == totalFloors;
                }
            }
        }

        return false;
    }

    private int CountFloors(bool[,] grid)
    {
        int count = 0;
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (!grid[x, y]) count++;
            }
        }
        return count;
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab13_CellularAutomata
```

### Step 2: 기본 환경

#### 2-1. 카메라 설정
```
Main Camera
Position: (0, 30, 0)
Rotation: (90, 0, 0)  // 위에서 내려다보기

Orthographic: ✓ (2D 스타일 표현)
```

#### 2-2. 조명
```
Directional Light
Rotation: (45, -45, 0)
Intensity: 1.5
```

### Step 3: 던전 생성기 설정

#### 3-1. Generator 오브젝트
```
Hierarchy → Create Empty
이름: DungeonGenerator

Add Component → DungeonGenerator

Inspector 설정:
  - Map Width: 80
  - Map Height: 80
  - Fill Probability: 0.45 (초기 벽 확률)
  - Iterations: 5 (CA 반복 횟수)
  - Max Rooms: 10
  - Min Room Size: 5
```

#### 3-2. 타일 프리팹 준비
```
벽 타일 (Wall.prefab):
  - Cube (검은색)
  - Scale: (0.5, 0.5, 0.5)

바닥 타일 (Floor.prefab):
  - Plane (회색)
  - Scale: (0.5, 1, 0.5)
```

#### 3-3. 생성 실행
```
Inspector → Generate Dungeon 버튼 클릭

결과:
- 자동으로 타일 배치
- 플레이어 스폰 포인트 설정
- 몬스터 배치
```

### Step 4: 플레이어 설정

#### 4-1. 플레이어 오브젝트
```
Hierarchy → 3D Object → Capsule
이름: Player

Add Component:
  - Rigidbody
  - CharacterController
  - PlayerController (스크립트)

DungeonGenerator에서:
  - Player Prefab: Player 할당
```

#### 4-2. 몬스터 설정
```
Monster.prefab 생성:
  - Sphere (빨간색)
  - Rigidbody
  - AIController (스크립트)

DungeonGenerator에서:
  - Monster Prefab: Monster 할당
  - Monster Count: 5
```

### Step 5: 시각화

#### 5-1. 맵 레이어 추가
```
Hierarchy → Create Empty
이름: MapLayer (부모)

자동 생성된 타일들이 여기 속함
```

#### 5-2. 높이 조정
```
Wall: Y = 0.5
Floor: Y = 0
Player: Y = 1
Monster: Y = 0.5

→ 시각적 구분
```

---

## 구현 체크리스트

### Phase 1: CA 기본 구현 (25분)

- [ ] CellularAutomata.cs
  - [ ] 그리드 초기화
  - [ ] CountWallNeighbors() 구현
  - [ ] Simulate() 구현 (여러 회 반복)
- [ ] 기본 CA 테스트

### Phase 2: 방 및 통로 생성 (25분)

- [ ] RoomGenerator.cs
  - [ ] 방 생성 및 겹침 검사
  - [ ] ApplyRoomsToGrid()
- [ ] CorridorGenerator.cs
  - [ ] L자형 통로 생성
  - [ ] 방 연결

### Phase 3: 맵 검증 (15분)

- [ ] FloodFill.cs
  - [ ] Flood Fill 알고리즘
  - [ ] 연결성 검증
  - [ ] 고립된 영역 제거

### Phase 4: 씬 구현 (15분)

- [ ] DungeonGenerator.cs (통합)
- [ ] 타일 생성 및 배치
- [ ] 플레이어 스폰
- [ ] 몬스터 배치

### Phase 5: 테스트 및 최적화 (10분)

- [ ] 던전 생성 확인
- [ ] 플레이 가능성 검증
- [ ] 다양한 파라미터로 실험
- [ ] 성능 최적화

---

## 참고 자료

### 공식 문헌

- **"Cellular Automata for Generating Dungeons"** - Traditional Game Dev
- **"Procedural Content Generation for Games"** - Noor Shaker et al.

### 온라인 자료

- **Red Blob Games - Cellular Automata:**
  - https://www.redblobgames.com/articles/noise/
- **Roguelike Development Tutorial**

---

## 주요 개념 심화

### 1. CA 파라미터의 영향

```
Fill Probability:
  - 0.3: 거의 모두 빈 공간
  - 0.45: 균형잡힌 던전
  - 0.6: 벽이 많음

Iterations:
  - 1: 매우 복잡한 형태
  - 5: 자연스러운 동굴
  - 10: 매우 부드러운 형태
```

### 2. 방 배치 전략

```
Random Placement:
- 장점: 다양성
- 단점: 큰 방이 겹칠 수 있음

Grid-Based:
- 장점: 겹침 없음
- 단점: 정형적 배치

BSP (Binary Space Partition):
- 장점: 효율적 공간 활용
- 단점: 복잡함
```

### 3. 통로 연결 방식

```
L자형 (Manhattan):
    ┌─┐
    │ │
────┼─┘
    │

대각선:
    ┌─┐
    │ │
────┘ │

복잡한 경로:
    ┌─┐
    │ │
────╋─┘
    │

각각 게임 플레이 감각이 다름
```

---

## 트러블슈팅

### 문제 1: 고립된 공간 생성

**원인:** CA 반복 후 연결성 확인 안 함

**해결책:**
```csharp
// Flood Fill로 고립 영역 찾기
while (!mapAnalyzer.IsMapConnected(grid))
{
    // 고립된 영역을 벽으로 채우기
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (!grid[x, y])
            {
                int regionSize = floodFill.GetConnectedRegionSize(grid, x, y);
                if (regionSize < minRegionSize)
                {
                    // 작은 영역 채우기
                    FillRegion(grid, x, y);
                }
            }
        }
    }

    // 다시 CA 반복
    ca.Simulate(1);
}
```

### 문제 2: 방이 너무 크거나 작음

**원인:** Min/Max Room Size 설정 오류

**해결책:**
```csharp
// 변경 전
minRoomSize = 3;  // 3x3 = 너무 작음

// 변경 후
minRoomSize = 6;  // 6x6 ~ 12x12 = 적절함
maxRoomSize = minRoomSize * 2;
```

### 문제 3: 통로가 끊어짐

**원인:** 통로 너비가 너무 좁음

**해결책:**
```csharp
// CreateHorizontalCorridor에서 주변도 함께 처리
for (int x = minX; x <= maxX; x++)
{
    for (int dy = -1; dy <= 1; dy++)  // 넓이 3으로 확대
    {
        if (y + dy >= 0 && y + dy < height)
            grid[x, y + dy] = false;
    }
}
```

---

## 확장 아이디어

### 아이디어 1: 보물 배치

```csharp
public class TreasureSpawner
{
    public void SpawnTreasures(bool[,] grid, List<Room> rooms)
    {
        foreach (var room in rooms)
        {
            if (Random.value < 0.7f)  // 70% 확률
            {
                Vector2Int center = new Vector2Int(
                    room.x + room.width / 2,
                    room.y + room.height / 2
                );

                SpawnTreasure(center);
            }
        }
    }
}
```

### 아이디어 2: 여러 층의 던전

```csharp
public class MultiLevelDungeon
{
    public List<bool[,]> GenerateMultipleLevels(int levelCount)
    {
        List<bool[,]> levels = new List<bool[,]>();

        for (int i = 0; i < levelCount; i++)
        {
            // 각 레벨마다 독립적으로 생성
            int seed = Random.Range(0, 100000);
            Random.InitState(seed);

            bool[,] level = GenerateSingleLevel();
            levels.Add(level);

            // 계단 추가
            AddStairs(level, i < levelCount - 1);
        }

        return levels;
    }
}
```

### 아이디어 3: 난이도 조정

```csharp
public class DifficultyAdjustment
{
    public void AdjustForDifficulty(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                fillProbability = 0.35f;  // 덜 복잡함
                iterations = 3;
                monsterCount = 2;
                break;

            case Difficulty.Normal:
                fillProbability = 0.45f;
                iterations = 5;
                monsterCount = 5;
                break;

            case Difficulty.Hard:
                fillProbability = 0.55f;  // 더 복잡함
                iterations = 7;
                monsterCount = 10;
                break;
        }
    }
}
```

---

## 완료 체크리스트

- [ ] CellularAutomata.cs 구현 완료
- [ ] RoomGenerator.cs 구현 완료
- [ ] CorridorGenerator.cs 구현 완료
- [ ] FloodFill.cs 구현 완료
- [ ] DungeonGenerator.cs (통합)
- [ ] 기본 던전 생성 테스트
- [ ] 방이 생성되는지 확인
- [ ] 통로가 연결되는지 확인
- [ ] 플레이어 스폰 위치 설정
- [ ] 몬스터 배치 (5마리)
- [ ] 연결성 검증
- [ ] 여러 번 생성하여 다양성 확인
- [ ] 플레이 테스트
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐⭐ (상)
**예상 완료 시간:** 90분

---

## 게임 통합 예제

```csharp
public class DungeonGameManager : MonoBehaviour
{
    void Start()
    {
        // 던전 생성
        DungeonGenerator dungeonGen = GetComponent<DungeonGenerator>();
        dungeonGen.GenerateDungeon();

        // 플레이어 스폰
        Vector3 playerSpawn = dungeonGen.GetPlayerSpawnPoint();
        Instantiate(playerPrefab, playerSpawn, Quaternion.identity);

        // 몬스터 배치
        List<Vector3> monsterSpawns = dungeonGen.GetMonsterSpawnPoints(5);
        foreach (var spawnPoint in monsterSpawns)
        {
            Instantiate(monsterPrefab, spawnPoint, Quaternion.identity);
        }

        // 보물 배치
        List<Vector3> treasureSpawns = dungeonGen.GetTreasureSpawns();
        foreach (var spawnPoint in treasureSpawns)
        {
            Instantiate(treasurePrefab, spawnPoint, Quaternion.identity);
        }
    }
}
```

---

## 성능 고려사항

```
맵 크기별 생성 시간:
- 50x50: <10ms
- 100x100: 20-50ms
- 200x200: 200-500ms

권장:
- 게임 시작 시: 100x100 이하
- 대기 중: 200x200 가능
- 실시간 생성 불가능한 크기: 400x400+
```
