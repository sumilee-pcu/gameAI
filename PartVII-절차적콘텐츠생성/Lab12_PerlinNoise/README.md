# Lab 12: Perlin Noise 지형 생성

**소요 시간:** 60분
**난이도:** ⭐⭐⭐ (중상)
**연관 Part:** Part VII - 절차적 콘텐츠 생성
**이전 Lab:** Lab 11 - Unity ML-Agents

---

## 학습 목표

이 실습을 완료하면 다음을 이해하고 구현할 수 있습니다:

1. **Perlin Noise 이해**
   - 노이즈 함수의 원리
   - 연속성 있는 난수 생성
   - 주파수와 진폭 조절

2. **지형 생성**
   - 높이맵 기반 지형 생성
   - 메시 생성 및 최적화
   - 텍스처 맵핑

3. **다양한 환경 구성**
   - 산맥, 계곡, 평야
   - 물(Water) 레벨 설정
   - 생물군계(Biome) 구분

4. **절차적 생성의 응용**
   - 랜덤 시드를 통한 재현성
   - 계층적 노이즈(Fractional Brownian Motion)
   - 실시간 지형 생성

---

## 파일 구조

실습을 완료하면 다음과 같은 파일 구조가 생성됩니다:

```
Assets/
├── Scripts/
│   ├── PCG/
│   │   ├── PerlinNoiseGenerator.cs          # Perlin Noise 구현 (NEW)
│   │   ├── TerrainGenerator.cs              # 지형 생성기 (NEW)
│   │   ├── HeightMapGenerator.cs            # 높이맵 생성 (NEW)
│   │   └── MeshGenerator.cs                 # 메시 생성 (NEW)
│   └── Visualization/
│       ├── HeightMapVisualizer.cs           # 높이맵 시각화 (NEW)
│       └── BiomeColorizer.cs                # 생물군계 색상화 (NEW)
├── Prefabs/
│   └── Terrain/
│       └── ProceduralTerrain.prefab         # 절차적 지형 (NEW)
├── Scenes/
│   └── Labs/
│       └── Lab12_PerlinNoise.unity          # 실습 씬 (NEW)
├── Textures/
│   ├── HeightMap.png                        # 높이맵 텍스처 (생성)
│   └── BiomeMap.png                         # 생물군계 텍스처 (생성)
└── Materials/
    └── TerrainMaterial.mat                  # 지형 메테리얼 (NEW)
```

---

## 주요 개념

### 1. Perlin Noise의 원리

**부드러운 난수를 생성하는 함수**

```
Random (무작위):        Perlin Noise (부드러운):
│                       │
├─ 0.3                  ├─ 0.4
├─ 0.8                  ├─ 0.5
├─ 0.1 (급격함)         ├─ 0.52 (부드러움)
├─ 0.9                  ├─ 0.7
└─ 0.4                  └─ 0.8

높이맥핑:
Random → 울퉁불퉁한 지형
Perlin Noise → 자연스러운 산과 계곡
```

#### Ken Perlin의 알고리즘

```
1. 격자점(Grid Points)에 임의의 기울기 벡터 배정
2. 입력 좌표 주변 4개 격자점 찾기
3. 각 격자점에서의 거리와 기울기 곱 계산
4. Fade 함수로 보간(Interpolation)
5. 최종 값 반환
```

### 2. Perlin Noise 구현

```csharp
public class PerlinNoiseGenerator
{
    private int[] permutation;
    private int[] p;

    public PerlinNoiseGenerator(int seed)
    {
        // 시드 기반 난수표 생성
        Random.InitState(seed);
        permutation = new int[256];

        for (int i = 0; i < 256; i++)
            permutation[i] = i;

        // Fisher-Yates Shuffle
        for (int i = 255; i > 0; i--)
        {
            int k = Random.Range(0, i + 1);
            (permutation[i], permutation[k]) = (permutation[k], permutation[i]);
        }

        // 두 배로 확장 (순환)
        p = new int[512];
        System.Array.Copy(permutation, 0, p, 0, 256);
        System.Array.Copy(permutation, 0, p, 256, 256);
    }

    public float Noise(float x, float y)
    {
        // 정수와 소수 부분 분리
        int xi = (int)x & 255;
        int yi = (int)y & 255;

        float xf = x - (int)x;
        float yf = y - (int)y;

        // 주변 4개 격자점의 해시값
        int aa = p[p[xi] + yi];
        int ab = p[p[xi] + yi + 1];
        int ba = p[p[xi + 1] + yi];
        int bb = p[p[xi + 1] + yi + 1];

        // 페이드 함수 (보간 계수)
        float u = Fade(xf);
        float v = Fade(yf);

        // 각 점에서의 값 계산 (기울기와 거리의 곱)
        float n00 = Grad(aa, xf, yf);
        float n10 = Grad(ba, xf - 1, yf);
        float n01 = Grad(ab, xf, yf - 1);
        float n11 = Grad(bb, xf - 1, yf - 1);

        // 보간
        float nx0 = Mathf.Lerp(n00, n10, u);
        float nx1 = Mathf.Lerp(n01, n11, u);
        return Mathf.Lerp(nx0, nx1, v);
    }

    private float Fade(float t)
    {
        // 부드러운 보간 곡선
        // y = 3t² - 2t³
        return t * t * (3 - 2 * t);
    }

    private float Grad(int hash, float x, float y)
    {
        // 기울기 벡터 선택 및 곱셈
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 8 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
```

### 3. 높이맵 생성

```csharp
public class HeightMapGenerator
{
    public float[,] GenerateHeightMap(int width, int height,
                                     float scale, int seed)
    {
        float[,] heightMap = new float[width, height];
        PerlinNoiseGenerator noiseGen = new PerlinNoiseGenerator(seed);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 정규화된 좌표
                float sampleX = x / scale;
                float sampleY = y / scale;

                // Perlin Noise 샘플링
                float noise = noiseGen.Noise(sampleX, sampleY);

                // 범위: -1 ~ 1 → 0 ~ 1
                heightMap[x, y] = (noise + 1f) / 2f;
            }
        }

        return heightMap;
    }
}
```

### 4. 계층적 노이즈 (Fractional Brownian Motion)

**여러 주파수의 노이즈를 조합하여 자연스러운 형태 생성**

```
단일 Perlin Noise:        FBM (3 옥타브):
│╱╲╱╲╱╲╱╲                 │╱╲╱╱╲╱╱╲╱
│                         │(더 많은 세부사항)

코드:
float FBM(float x, float y, int octaves, float persistence)
{
    float total = 0;
    float amplitude = 1;
    float frequency = 1;
    float maxValue = 0;

    for (int i = 0; i < octaves; i++)
    {
        // 주파수를 2배씩 증가
        total += Noise(x * frequency, y * frequency) * amplitude;

        maxValue += amplitude;

        // 진폭을 감소
        amplitude *= persistence;
        frequency *= 2;
    }

    return total / maxValue;
}

파라미터:
- octaves: 3~5 (세부사항의 깊이)
- persistence: 0.5 (진폭 감소 비율)
  * 0.5: 높은 옥타브가 낮은 영향
  * 0.7: 균형잡힌 세부사항
```

### 5. 생물군계(Biome) 정의

```csharp
public enum Biome
{
    Water,      // 높이 < 0.4
    Sand,       // 0.4 ~ 0.45
    Grass,      // 0.45 ~ 0.6
    Forest,     // 0.6 ~ 0.8
    Mountain,   // 0.8 ~ 1.0
}

public class BiomeColorizer
{
    private Dictionary<Biome, Color> biomeColors = new Dictionary<Biome, Color>
    {
        { Biome.Water, new Color(0.2f, 0.5f, 1f) },    // 파란색
        { Biome.Sand, new Color(1f, 0.9f, 0.6f) },     // 모래색
        { Biome.Grass, new Color(0.3f, 0.8f, 0.3f) },  // 초록색
        { Biome.Forest, new Color(0.1f, 0.5f, 0.1f) }, // 진녹색
        { Biome.Mountain, new Color(0.7f, 0.7f, 0.7f) }, // 회색
    };

    public Biome GetBiome(float height)
    {
        if (height < 0.4f) return Biome.Water;
        if (height < 0.45f) return Biome.Sand;
        if (height < 0.6f) return Biome.Grass;
        if (height < 0.8f) return Biome.Forest;
        return Biome.Mountain;
    }

    public Color GetColor(float height)
    {
        return biomeColors[GetBiome(height)];
    }
}
```

---

## Unity 씬 설정 가이드

### Step 1: 새 씬 생성

```
1. File → New Scene → Basic (Built-in)
2. File → Save As → Assets/Scenes/Labs/Lab12_PerlinNoise
```

### Step 2: 기본 구성

#### 2-1. 조명
```
Directional Light
Rotation: (45, -45, 0)
Intensity: 1.5
```

#### 2-2. 카메라
```
Main Camera
Position: (50, 30, 50)
Rotation: (30, 45, 0)

Perspective 또는 Orthographic
```

### Step 3: 지형 생성기 설정

#### 3-1. TerrainGenerator 오브젝트
```
Hierarchy → Create Empty
이름: TerrainGenerator

Add Component → TerrainGenerator

Inspector 설정:
  - Width: 256 (지형 가로 크기)
  - Height: 256 (지형 세로 크기)
  - Scale: 50 (노이즈 스케일, 작을수록 상세함)
  - Seed: 12345 (재현성 확보)
  - Height Multiplier: 20 (높이 배수)
```

#### 3-2. 메시 생성
```
Inspector → Generate 버튼 클릭

결과:
- Mesh 생성
- Texture 생성
- Material 적용
```

### Step 4: 고급 설정

#### 4-1. FBM 파라미터
```
Octaves: 4 (노이즈 층의 개수)
Persistence: 0.5 (세부사항 강도)
Frequency: 1.0 (기본 주파수)
```

#### 4-2. 물 평면 추가
```
Hierarchy → 3D Object → Plane
이름: Water
Position: (128, height값, 128)
Scale: (128, 1, 128)

Height 값 = (Water Level * Height Multiplier)
예: 0.4 * 20 = 8
```

### Step 5: 시각화

#### 5-1. 높이맵 텍스처 저장
```
TerrainGenerator 스크립트에서:
- SaveHeightMap("Assets/Textures/HeightMap.png")
- SaveBiomeMap("Assets/Textures/BiomeMap.png")
```

#### 5-2. 결과 확인
```
Project 창에서 생성된 텍스처 확인
Asset Store에서 다운로드한 텍스처 적용 (선택)
```

---

## 구현 체크리스트

### Phase 1: Perlin Noise 기초 (20분)

- [ ] PerlinNoiseGenerator.cs 구현
  - [ ] 난수 시드 처리
  - [ ] Noise() 함수
  - [ ] Fade() 함수
  - [ ] Grad() 함수
- [ ] 테스트: 1D 노이즈 출력 확인

### Phase 2: 높이맵 생성 (15분)

- [ ] HeightMapGenerator.cs 구현
  - [ ] 2D 높이맵 생성
  - [ ] 범위 정규화 (0 ~ 1)
- [ ] 높이맵 이미지로 저장

### Phase 3: 메시 생성 (15분)

- [ ] MeshGenerator.cs 구현
  - [ ] 정점 생성
  - [ ] 인덱스 생성 (삼각형)
  - [ ] UV 매핑
- [ ] 메시 적용 및 시각화

### Phase 4: 생물군계 및 색상화 (10분)

- [ ] BiomeColorizer.cs 구현
- [ ] 높이에 따른 색상 맵핑
- [ ] 텍스처 생성 및 저장

---

## 참고 자료

### 공식 문헌

- **"Perlin Noise" - Ken Perlin (1985)**
  - 원본 논문
- **"Texturing and Modeling: A Procedural Approach"**
  - 절차적 생성의 기초

### 온라인 자료

- **Red Blob Games - Perlin Noise:**
  - https://www.redblobgames.com/articles/noise/
- **Daniel Shiffman - The Nature of Code:**
  - https://www.youtube.com/watch?v=BtS2iR0DedI

---

## 주요 개념 심화

### 1. Perlin Noise vs Raw Noise

```
Raw Random Noise (짐스 균등 분포):
0.2, 0.8, 0.1, 0.9, 0.3, ...
→ 완전히 무작위, 자연스럽지 않음

Perlin Noise:
0.4, 0.45, 0.5, 0.52, 0.7, ...
→ 이웃 값과 유사, 부드러운 변화
→ 자연 지형 재현 가능
```

### 2. FBM (Fractional Brownian Motion)

```
단일 노이즈:
sin(x) 같은 단순한 파동

FBM:
sin(x) + 0.5*sin(2x) + 0.25*sin(4x) + ...

결과:
복잡하면서도 자연스러운 패턴
```

### 3. 노이즈 파라미터 영향

```
Scale (작을수록 상세함):
  scale = 10  → 큰 산과 계곡
  scale = 50  → 중간 크기 지형
  scale = 200 → 넓고 완만한 평야

Height Multiplier:
  5  → 평평함
  20 → 일반적 산악 지형
  50 → 극단적 높이 차이

Octaves (세부사항):
  1 → 매우 부드러움
  3 → 일반적 지형
  6 → 매우 상세함 (계산 비용 증가)
```

---

## 트러블슈팅

### 문제 1: 메시 생성이 너무 느림

**원인:** 해상도가 너무 높음

**해결책:**
```csharp
// 변경 전
width = 512;
height = 512;

// 변경 후
width = 256;
height = 256;

// 또는 LOD (Level of Detail) 사용
// 먼 거리에서는 낮은 해상도 사용
```

### 문제 2: 지형이 너무 울퉁불퉁하다

**원인:** Octaves가 너무 많거나 Persistence가 너무 높음

**해결책:**
```csharp
// 변경 전
octaves = 8;
persistence = 0.8f;

// 변경 후
octaves = 3;
persistence = 0.5f;  // 낮은 옥타브 무시
```

### 문제 3: 같은 지형이 반복된다

**원인:** Seed 값이 동일함

**해결책:**
```csharp
// 매번 다른 지형 생성
int randomSeed = Random.Range(0, 100000);
terrainGenerator.seed = randomSeed;
```

### 문제 4: 물과 육지의 경계가 명확하지 않음

**원인:** Water Level이 부정확함

**해결책:**
```csharp
// Water Level 조정
waterLevel = 0.4f;  // 높이맵에서 0.4 미만 = 물

// 또는 부드러운 전환
if (height < 0.42f) return Biome.Water;
if (height < 0.46f) return Biome.Sand;  // 해변 영역
if (height < 0.5f) return Biome.Grass;
```

---

## 확장 아이디어

### 아이디어 1: 실시간 지형 생성

```csharp
// 카메라 주변만 생성하여 메모리 절약
public class StreamingTerrain : MonoBehaviour
{
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks;
    private Vector2Int lastChunkCoord;
    private int chunkSize = 64;

    void Update()
    {
        Vector2Int currentChunk = GetChunkCoord(transform.position);

        if (currentChunk != lastChunkCoord)
        {
            // 새로운 청크 로드
            LoadTerrainChunk(currentChunk);

            // 먼 청크 언로드
            UnloadDistantChunks(currentChunk);

            lastChunkCoord = currentChunk;
        }
    }
}
```

### 아이디어 2: 침식 시뮬레이션 (Erosion)

```csharp
// 높이맵에 침식 효과 적용 (물의 흐름 시뮬레이션)
public class ErosionSimulator
{
    public float[,] ApplyErosion(float[,] heightMap)
    {
        // 빗방울 입자가 높이맵을 따라 흐르며 침식
        // 자연스러운 계곡 형성

        for (int iteration = 0; iteration < erosionIterations; iteration++)
        {
            Vector2Int pos = GetRandomPosition(heightMap);
            Particle water = new Particle(pos);

            while (water.IsActive())
            {
                Vector2Int nextPos = water.FindDownslope(heightMap);
                heightMap[nextPos.x, nextPos.y] -= erosionStrength;
                water.Move(nextPos);
            }
        }

        return heightMap;
    }
}
```

### 아이디어 3: 여러 노이즈 조합

```csharp
// 여러 종류의 노이즈를 조합하여 다양한 지형 특성
public class HybridNoise
{
    public float GetNoise(float x, float y)
    {
        float perlinNoise = perlinGen.Noise(x, y);
        float worleyNoise = worleyGen.Noise(x, y);  // Voronoi
        float simplexNoise = simplexGen.Noise(x, y);

        // 조합
        return perlinNoise * 0.5f + worleyNoise * 0.3f + simplexNoise * 0.2f;
    }
}
```

---

## 완료 체크리스트

- [ ] PerlinNoiseGenerator.cs 구현 완료
- [ ] HeightMapGenerator.cs 구현 완료
- [ ] MeshGenerator.cs 구현 완료
- [ ] BiomeColorizer.cs 구현 완료
- [ ] TerrainGenerator.cs 통합
- [ ] 기본 지형 생성 테스트
- [ ] 높이맵 저장 및 시각화
- [ ] 생물군계별 색상 적용
- [ ] 물 평면 추가
- [ ] 카메라 최적 위치 설정
- [ ] 여러 시드로 다양한 지형 생성 테스트
- [ ] 다양한 파라미터로 실험
- [ ] 씬 저장

---

**작성일:** 2024년 1월
**난이도:** ⭐⭐⭐ (중상)
**예상 완료 시간:** 60분

---

## 성능 최적화 팁

### 메모리 사용량 감소

```csharp
// 전체 높이맵 계산 후 저장
float[,] heightMap = new float[width, height];

// vs

// 필요할 때마다 계산 (메모리 절약, 계산 느림)
float GetHeight(int x, int y)
{
    return noiseGen.Noise(x * scale, y * scale);
}
```

### 계산 속도 개선

```csharp
// 1. 캐싱
Dictionary<(int, int), float> noiseCache;

public float GetNoiseCached(int x, int y)
{
    if (noiseCache.TryGetValue((x, y), out float value))
        return value;

    float newValue = Noise(x, y);
    noiseCache[(x, y)] = newValue;
    return newValue;
}

// 2. 병렬 처리
Parallel.For(0, width, x =>
{
    for (int y = 0; y < height; y++)
    {
        heightMap[x, y] = Noise(x * scale, y * scale);
    }
});
```
