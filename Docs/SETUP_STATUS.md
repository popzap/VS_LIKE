# VS_LIKE — 프로젝트 현황

> Unity 6.3 LTS (6000.3.8f1) / 2D 뱀서라이크
>
> **2026-08-26 — 작업 전제 변경 (사용자 지시)**
> 기존의 「C# 스크립트는 완성 단계」라는 전제와 「요청 없이 코드 건드리지 말 것」 규칙이 **해제됨**.
> 이제 게임 완성을 위해 C# 스크립트 신규 작성·수정이 허용된다.
>
> **최종 갱신:** 2026-09-05 (96차 — 체력 재생, D74)
>
> 🔀 **25차부터 이슈 번호가 `세션 접두어 + 번호` 다** — `D`(DEV) · `C`(CONTENT) · `B`(버그 공용).
> 병렬 2세션 체제로 바뀌었기 때문이다 (D1). 과거 `I-1`~`I-61` 은 그대로 둔다.
> 규칙 전문 [`Parallel/SESSION_PROMPT.md`](Parallel/SESSION_PROMPT.md) · 현황판 [`Parallel/BOARD.md`](Parallel/BOARD.md)
> 검증 방식: Unity MCP + Play 모드 스모크 테스트 + YAML 직접 파싱
> **검증 기준 파일:** `Assets/Scenes/SampleScene.unity`
>
> **현재 상태: 메인메뉴 → 스테이지맵 → 웨이브 → 클리어 → 게임오버 전체 루프 런타임 검증 완료 (18/18 PASS), 콘솔 에러 0 / 경고 0.**
> [`ROADMAP.md`](ROADMAP.md) §8 의 **1·2·3단계 완료** (I-43~I-49) — HUD 정보 · 타격 반응 · 오디오 ·
> 병렬 소환 · 적 행동 분화 · 보물상자/자석. **"조용한 프로토타입" 단계는 끝났다.**
> 원격 동기화: `popzap/VS_LIKE` `main` @ **`bd2f4ce`** (2026-08-31 — 47차 `D24` 까지 푸시됨).
> 🔴 **로컬이 5커밋 앞서 있다** — `4e60fdd`(C26) · `e2dda0e`(48차 D25) · `4379d6c`·`f3d144f`·`f09558e`(C27) 는 **커밋만 되고 아직 미푸시**다.
>
> 🎯 **16차는 처음으로 "직접 플레이해서 나온" 버그 보고에서 출발했다** (I-50).
> 로그로만 검증하던 단계에서는 절대 발견할 수 없는 종류였다 — 자세한 건 2-20.
>
> ⚠️ **지금 가장 필요한 것은 코드가 아니라 "직접 플레이"다.**
> 최근 6개 작업(I-44·I-46~I-49)은 전부 **로그로만** 검증했다. 로그가 증명하는 것은
> "동작한다"까지이고, **체감(넉백 세기 · 적 밀도 · 음량 밸런스)은 눈과 귀로만 판단할 수 있다**
> → [`TODO.md`](TODO.md) §1
> Shop / Event / Pause / Retry 버그 5건(I-14~I-18)은 **코드 수정 완료**, 런타임 재검증은 미완 → [`TODO.md`](TODO.md) §1
> 밸런스 수치는 **CSV가 원본**이다. 고치는 법은 [`BALANCE.md`](BALANCE.md).
> **단, 오디오 음량만은 CSV가 아니라 `AudioLibrary.asset` 이다** (I-49).
>
> 🔤 **폰트는 이제 Static 이다** (I-60) — `Pretendard SDF.asset` 의 문자표는 **ASCII 32~126 + 기호 20개 = 115자**로
> 고정됐다. **여기 없는 문자를 UI 에 쓰면 빈칸으로 나온다.** 새 기호가 필요하면 폰트를 다시 구울 것 → [`BALANCE.md`](BALANCE.md)

---

## 0. 툴체인 상태  ✅ 연결됨

| 항목 | 상태 |
|---|---|
| Claude Code ↔ `unity-mcp` (relay `--mcp`) | ✅ |
| `com.unity.ai.assistant` 2.18.0-pre.2 (릴레이 제공) | ✅ |
| Unity Editor (6000.3.8f1) | ✅ 실행 중 |

MCP 초기화는 `McpInitializer.cs`의 `[InitializeOnLoadMethod]`로 에디터 로드 시 자동 실행되며
**별도 토글이 없음**. `Unity not detected` 에러가 나면 원인은 사실상 "에디터 미실행" 하나.
에디터를 닫으면 릴레이도 약 2분 뒤 종료(`--shutdown-delay 120`)되므로 작업 중에는 계속 열어둘 것.

---

## 0-1. Input 처리 방식  🟡 (나) 완료 / (가) 대기

`ProjectSettings.activeInputHandler: 1` (= Input System 전용)인데 스크립트가 레거시
`UnityEngine.Input`을 써서 **플레이어 이동이 아예 동작하지 않던 블로커**가 있었음.

**(나) 스크립트를 Input System으로 이관 — ✅ 완료 (컴파일 클린)**
`.inputactions` 애셋 배선 없이 동작하도록 디바이스 직접 읽기(`Keyboard.current` / `Mouse.current`)로 1:1 치환.
Inspector 추가 연결 불필요.

| 파일 | 변경 |
|---|---|
| `Player/PlayerController.cs` | `GetAxisRaw` → WASD/방향키 직접 읽기, `GetKeyDown(B)` → `bKey.wasPressedThisFrame`, `GetMouseButtonDown(0)` → `leftButton.wasPressedThisFrame`, `mousePosition` → `Mouse.current.position.ReadValue()` |
| `UI/PauseMenuUI.cs` | `GetKeyDown(Escape)` → `escapeKey.wasPressedThisFrame` |
| `Building/BuildingManager.cs` | `mousePosition` → `Mouse.current.position.ReadValue()` (+ null 가드) |
| `UI/StageClearUI.cs` | `GetMouseButtonDown(0)` → `leftButton.wasPressedThisFrame` |

**(가) Active Input Handling = `Both` — ⏳ 대기 (에디터 재시작 필요)**
(나)만으로 기능은 해결되므로, 재시작이 MCP 릴레이를 끊는 것을 피하기 위해
**마지막 Play 검증 직전에 적용**할 예정.

---

## 1. 씬 하이어라키 (현재)

```
SampleScene
├─ EventSystem
├─ Global Light 2D
├─ AudioManager                      ← 씬 루트 (2026-08-26 추가)
├─ Ground  [Grid cellSize 1×1]       ← 9차 추가
│   └─ GroundTilemap  [Tilemap, TilemapRenderer(order -100), GroundTiler]
├─ (매니저 프리팹 인스턴스, 전부 루트)
│   GameManager / WaveManager / LevelUpManager / ExperienceManager
│   DamagePopupManager / BuildingManager / ShopManager / EventManager
│   MetaProgressionManager / StageMapManager / ObjectPool / Player / Camera
└─ UI Canvas  [HUDManager, PauseMenuUI, ShopUI,
    │          MainMenuUI, StageMapUI, RunEndUI, StageClearUI, StateVisibilityBinder]
    ├─ MainMenuPanel     <INACTIVE>
    │   └─ DimBG, TitleText, CurrencyText,
    │      StartButton / OptionButton / QuitButton  (각각 → Label)
    ├─ StageMapPanel     <INACTIVE>
    │   └─ DimBG, HeaderText, CurrencyText,
    │      MapScrollView → Viewport → Content   ← 노드/라인이 런타임 생성됨
    ├─ StageClearPanel   <INACTIVE>
    │   └─ DimBG, Card (StageTitleText, XpGainText, CurrencyGainText,
    │                   KillCountText, TimeText, StatChangeContainer,
    │                   ItemContainer, ContinueHint, ContinueButton)
    ├─ RunEndPanel       <INACTIVE>
    │   └─ DimBG, Card (TitleText, SummaryText, RetryButton, MainMenuButton)
    ├─ HUD                               ← StateVisibilityBinder가 Wave/Shop 등에서만 표시
    │   ├─ PortraitGroup
    │   │   ├─ PortraitImage → StatusDot
    │   │   ├─ HPSlider → Fill
    │   │   └─ HPText
    │   ├─ XPGroup (LevelText, XPText, XPSlider → Fill)
    │   ├─ CurrencyText
    │   └─ OptionsButton
    ├─ LevelUpPanel                      <INACTIVE>
    │   ├─ DimBG
    │   ├─ TitleText
    │   └─ PanelBG
    │       ├─ CardContainer
    │       │   ├─ ItemCard_0  (150x200)
    │       │   ├─ ItemCard_1  (150x200)
    │       │   └─ ItemCard_2  (150x200)
    │       └─ RerollArea
    │           ├─ RerollButton → RerollBtnText
    │           └─ RerollCostText
    ├─ PausePanel        <INACTIVE>  [CanvasGroup]
    │   ├─ DimBG
    │   └─ Card (TitleText, ResumeButton, OptionButton, QuitButton)
    ├─ OptionSubPanel    <INACTIVE>  [OptionPanel]
    │   ├─ DimBG
    │   └─ Card (TitleText, BGMLabel/BGMSlider, SFXLabel/SFXSlider, CloseButton)
    └─ ShopRoot          <INACTIVE>  [CanvasGroup]
        ├─ DimBG
        ├─ LeftPanel   — RemoveHintText, RemoveScrollView → Viewport → Content
        ├─ CenterPanel — CardContainer, RerollButton, RerollCostText, CloseButton
        └─ RightPanel  — NpcDialogueText, KillCountText, ElapsedTimeText,
                         PlayerLevelText, WaveProgressText
```

> 방치 프리팹 인스턴스 10개(루트 7 + UI Canvas 하위 3)는 **B단계에서 삭제 완료**.

`UI Canvas.prefab` 자체는 자식이 없는 빈 Canvas이고, 위 UI 트리는 전부 씬에서 추가된 것.

> `OptionPanel` 컴포넌트는 원래 UI Canvas에 붙어 있었음. `closeButton`이
> `gameObject.SetActive(false)`를 호출하므로 **Canvas 전체가 꺼지는 버그**가 있어
> C단계에서 `OptionSubPanel`로 옮김.

---

## 2. 작업 현황

### 1단계 — Inspector 연결  ✅ 완료

| 대상 | 필드 | 상태 |
|---|---|---|
| LevelUpManager | levelUpPanel | ✅ LevelUpPanel |
| | cards[3] | ✅ ItemCard_0 / 1 / 2 |
| | rerollButton | ✅ RerollButton |
| | rerollCostText | ✅ RerollCostText |
| | allItems | ✅ **17개** — 2차에서 `SceneWiring.csv` 로 자동 배선 (원래 3개) |
| WaveManager | normalWaves | ✅ **Normal1~3** — 2차 `SceneWiring.csv` (원래 Normal1 1개, 내용도 비어 있었음 → I-12) |
| | eliteWaves | ✅ **Elite1~2** — 동일 |
| | bossWave | ✅ Boss1 — 동일 |
| | enemyPool | ✅ ObjectPool |
| | playerTransform | ✅ Player |
| Camera (CameraController) | target | ✅ Player |
| ExperienceManager | expPool | ✅ ObjectPool |
| | expDropPrefab | ✅ `ExpDrop_Small.prefab` (B단계 수정) |
| DamagePopupManager | pool | ✅ ObjectPool |
| | popupPrefab | ✅ `DamagePopup.prefab` (B단계 수정) |
| BuildingManager | buildingPool | ✅ ObjectPool |
| Player (WeaponManager) | weaponPool | ✅ ObjectPool (4단계에서 누락 발견 → 연결) |
| ShopUI | shopCardPrefab | ✅ `Prefab_ShopCard.prefab` (B단계 수정) |
| | removeRowPrefab | ✅ `Prefab_ShopRemoveRow.prefab` (B단계 수정) |

**ObjectPool `warmUpEntries` (5개) — 전부 프리팹 애셋 참조 ✅**

| # | Prefab | Count |
|---|---|---|
| 0 | `Assets/Prefabs/Enemy_Goblin.prefab` | 20 |
| 1 | `Assets/Prefabs/Proj_Bullet.prefab` | 30 |
| 2 | `Assets/Prefabs/Proj_Aoe(Boom).prefab` | 10 |
| 3 | `Assets/Prefabs/ExpDrop_Small.prefab` | 30 |
| 4 | `Assets/Prefabs/DamagePopup.prefab` | 20 |

> **(해결됨) 씬 인스턴스 참조 문제**: 프리팹 애셋이 아니라 씬에 놓인 오브젝트를 참조하고 있어
> 풀이 씬 오브젝트를 복제하고 원본이 씬에 그대로 남아 있었음.
> → B단계에서 참조 7건을 애셋으로 교체하고 방치 인스턴스 10개를 삭제, 씬 저장 후 재검증 완료.

### 2단계 — LevelUpPanel UI  ✅ 완료

- LevelUpPanel 비활성 상태 ✅
- CardContainer에 ItemCard_0/1/2 배치, 각 150x200 ✅
- RerollArea(RerollButton + RerollCostText) 구성 ✅
- RerollButton `onClick` → `LevelUpManager.OnRerollClicked` ✅

### 3단계 — UI 전체 (HUD + Pause + Option + Shop)  ✅ 완료

UI 패널 4종을 `Unity_RunCommand`(C# 동적 실행)로 신규 구축하고 필드를 전부 배선함.

| 컴포넌트 | 연결 | 비고 |
|---|---|---|
| `HUDManager` | 9 / 15 | 나머지 6개는 **선택 필드** — 아래 표 참조 |
| `PauseMenuUI` | 6 / 6 | ✅ |
| `OptionPanel` | 3 / 3 | ✅ (`OptionSubPanel`로 이동 후 연결) |
| `ShopUI` | 16 / 16 | ✅ |

**HUDManager 미연결 6개 — 전부 애셋이 없어 비워둔 선택 필드 (코드상 null-safe)**

| 필드 | 이유 |
|---|---|
| `faceHealthy` / `faceNormal` / `faceWorried` / `faceCritical` | 초상화 표정 Sprite 애셋 없음. `UpdatePortrait`가 `portraitImage == null` 조기 반환 + 스프라이트 null 허용 |
| `levelUpAnimator` | 레벨업 Animator 없음. `levelUpAnimator?.SetTrigger` |
| `levelUpEffect` | 레벨업 파티클 없음. `if (levelUpEffect)` 가드 |

> 검증: 씬 저장 후 `SampleScene.unity`의 `m_Modifications` 직접 파싱
> (UI Canvas가 프리팹 인스턴스라 MonoBehaviour 필드가 오버라이드로 기록됨).
> MCP `get_component`는 **비활성 오브젝트를 찾지 못하므로** YAML 검증이 필수였음.

### 4단계 — 프리팹 / SO 내부 연결  ✅ 완료

| 애셋 | 필드 | 상태 |
|---|---|---|
| `ItemData/Sword` | Category=Weapon(0), WeaponRef | ✅ WeaponData/Sword |
| `ItemData/House` | Category=Building(1), BuildingRef | ✅ BuildingData/House |
| `ItemData/Speed` | Category=Passive(2), PassiveRef | ✅ PassiveData/MoveSpeed |
| `WeaponData/Sword` | WeaponPrefab | ✅ `Weapon_Sword.prefab` (`5d7c717e…`) |
| `WeaponData/Sword` | ProjectilePrefab | ✅ `Proj_Bullet.prefab` (`b302c4b4…`) |
| `EnemyData/Goblin` | Prefab | ✅ `Enemy_Goblin.prefab` (`35bc07b6…`) |
| `BuildingData/House` | Prefab | ✅ `Building_Turret.prefab` (`be6065fc…`) |

`Assets/Prefabs/Weapon_Sword.prefab` — **신규 생성 완료**. 빈 GameObject + `ProjectileWeapon`
(`WeaponBase`는 abstract, 구현체는 `ProjectileWeapon` / `AoeWeapon` 2종).
`WeaponManager.AddOrUpgradeWeapon`이 풀에서 꺼내 Player 자식으로 붙이므로 별도 씬 배치 불필요.

### 5단계 — Play 모드 콘솔 검증  ✅ 완료 (콘솔 클린)

Play 진입 → 정지를 3회 반복하며 에러를 하나씩 제거함.

| 회차 | 결과 |
|---|---|
| 1회 | `NullReferenceException: ShopUI.Start() (ShopUI.cs:84)` → **I-8** |
| 2회 | `NullReferenceException: MetaProgressionManager.GetStatBonus() (:131)` ← `PlayerStats.Start()` → **I-9** |
| 3회 | **에러 0 / 경고 0** ✅ |

최종 콘솔 출력:
```
[Meta] No save found. Fresh start.
[GameManager] State → MainMenu
```
> `[Adaptive Performance] Initialization of Provider was not successful` 는
> Log 레벨이고 Adaptive Performance 패키지 기본 동작. 무해.

**I-8 — 실행 순서**
`GameManager`는 `Start()`에서 `FindFirstObjectByType`으로 매니저 참조를 채우는데,
Unity는 서로 다른 컴포넌트의 `Start()` 순서를 보장하지 않아 `ShopUI.Start()`가 먼저 실행됨.
→ **코드 수정 없이** Script Execution Order로 해결.

| 스크립트 | Execution Order |
|---|---|
| `GameManager` | `0` → **`-100`** |

**I-9 — upgrades 배열**
`MetaProgressionManager.upgrades`가 `size=1`인데 원소가 `NULL`이었음.
프로젝트에 `UpgradeDefinition` 애셋이 **하나도 없어서** 채울 값도 없음 → **`size = 0`으로 정리**.
(빈 배열이면 `GetStatBonus()`가 보너스 없는 `StatBlock`을 정상 반환)

**씬 전체 빈 참조 스캔 결과 — 남은 2건은 모두 코드에서 null 가드된 선택 필드**

| 대상 | 빈 필드 | 영향 |
|---|---|---|
| `BuildingManager` | `placementCursorPrefab` | 배치 커서 프리팹 미제작. `ShowCursor()`에 `if (placementCursorPrefab)` 가드 → 커서만 안 보임 |
| `HUDManager` | 표정 4 + `levelUpAnimator` + `levelUpEffect` | 애셋 미제작. 전부 null 가드 |

---

## 2-2. ✅ 게임 루프 완성 (I-10 / I-11 해결)

이전에는 `GameManager.StartRun()` 호출자가 0개라 **`MainMenu`에서 게임이 멈춰** 있었음.
전제 해제(2026-08-26) 후 누락된 UI 스크립트를 신규 작성해 루프를 연결함.

**신규 스크립트 5종 — `Assets/Scripts/UI/`**

| 스크립트 | 역할 |
|---|---|
| `GameStatePanel` | 공통 베이스. `GameState`를 구독해 자기 담당 상태일 때만 자식 패널을 켠다. `Awake`에서 구독하므로 **항상 활성인 `UI Canvas`에 붙이고 자식 패널을 토글**하는 구조 |
| `MainMenuUI` | Start / Continue / Quit. Start → `GameManager.StartRun()` |
| `StageMapUI` | `StageMapManager.Layers`를 읽어 `Prefab_StageNode` / `Prefab_MapLine`을 스크롤뷰에 배치, 노드 클릭 → `StageMapManager.SelectNode()` |
| `RunEndUI` | GameOver / Victory 결과창. Retry → `GameManager.ReloadScene(true)`, Main Menu 복귀 |
| `StateVisibilityBinder` | HUD처럼 상태별 표시/숨김만 필요한 오브젝트용 경량 바인더 |

**신규 프리팹 4종 — `Assets/Prefabs/`**

`Prefab_StageNode` · `Prefab_MapLine` · `Prefab_ItemChip` · `Prefab_StatChangeRow`

**신규 씬 패널 4종 — `UI Canvas` 하위**

`MainMenuPanel` · `StageMapPanel` · `StageClearPanel` · `RunEndPanel`

**동반 수정**

| 항목 | 내용 |
|---|---|
| `DontDestroyOnLoad` 제거 | 씬이 1개뿐인데 `DontDestroyOnLoad`가 걸려 있어 재시작 시 매니저가 중복 생성됨. 제거하고 `GameManager.ReloadScene(bool)` 로 정리 |
| Event 노드 순서 버그 | `ChangeState` 호출 순서가 어긋나 Event 노드에서 패널이 잘못 뜨던 문제 수정 |

**I-11 해결** — `AudioManager`를 재작성해 씬에 배치, `StageClearUI`는
`TakeSnapshot()` / `AddKill()` / `Tick()` / `AddItem()` / `Show()` 를 `WaveManager`에서 호출하도록 연결.

---

## 2-3. ✅ 진행 차단 버그 수정 + 콘텐츠 투입 (2026-08-26, 2차)

전수 코드 감사에서 발견된 I-14~I-18을 수정하고, 프로토타입 분량이던 콘텐츠를
**CSV 기반 데이터 파이프라인**으로 교체했다.

### 파이프라인 신설 — 왜

밸런스 수치를 인스펙터에서 SO 하나씩 여는 방식은 표로 비교가 안 돼 밸런싱이 불가능했다.
`Assets/Game/Balance/*.csv` 를 **원본(authoring source)** 으로 삼고 SO는 산출물로 취급한다.

| 신규 파일 | 역할 |
|---|---|
| `Assets/Scripts/Balance/CsvTable.cs` | CSV 파서 + 타입별 행 접근자. 헤더 기반, `#` 주석, `\|` 배열, 따옴표 이스케이프 |
| `Assets/Editor/BalanceImporter.cs` | `Game/Balance/` 메뉴 2개 (Import / Export). SO 생성·갱신 + 씬 컴포넌트 직접 기록 |
| `Assets/Game/Balance/*.csv` | 9장 — Enemies / Weapons / Buildings / Passives / Items / Waves / Events / Economy / SceneWiring |

`Economy.csv` / `SceneWiring.csv` 는 SO가 아니라 **씬 컴포넌트의 직렬화 필드**를 `SerializedProperty`
경로로 직접 쓴다. 덕분에 `LevelUpManager.allItems`(17개)·`WaveManager.normalWaves` 같은
배열을 인스펙터 드래그 없이 CSV로 관리한다.

상세는 [`BALANCE.md`](BALANCE.md).

### 투입된 콘텐츠

| 종류 | 수 | 내용 |
|---|---|---|
| `EnemyData` | 1 → **6** | Slime / Goblin / Zombie / Wolf / Demon / Ogre |
| `WeaponData` | 1 → **5** | Sword / Bow / Gun / **Fireball** / **Bomb** (뒤 2종은 `AoeWeapon`) |
| `BuildingData` | 1 → **3** | House / Turret / Bombard |
| `PassiveData` | 1 → **9** | Damage / MoveSpeed / AttackSpeed / MaxHp / Armor / PickupRadius / XpGain / GoldGain / CritChance |
| `ItemData` | 3 → **17** | 위 무기·건물·패시브 1:1 대응 |
| `WaveData` | 3 → **6** | Normal1~3 / Elite1~2 / Boss1 |
| `EventManager.events` | 0 → **5** | 보상형 3 + 웨이브 유발형 2 |
| 신규 프리팹 | 1 | `Weapon_Aoe.prefab` (`AoeWeapon` + `explosionRadius 3`) |

> 적 6종은 **`Enemy_Goblin.prefab` 하나를 공유**하고 `EnemyData.Tint` / `SizeScale` 로만 구분한다.
> 스프라이트가 준비되면 `Enemies.csv` 의 `Sprite` 열만 바꾸면 된다.

### 동반 코드 변경

| 파일 | 변경 |
|---|---|
| `Player/StatBlock.cs` | `XpGain` / `GoldGain` 필드 추가 (기본 1.0 배율) |
| `Player/PlayerStats.cs` | `static Current` 캐시 추가. `RecalculateStats`에서 `XpGain`/`GoldGain` 은 base·meta 양쪽이 1.0 기준이라 `-1f` 로 중복 제거 |
| `Passive/PassiveData.cs` · `PassiveEffect.cs` | `BonusXpGain` / `BonusGoldGain` 지원 |
| `Experience/ExperienceManager.cs` | 획득 XP에 `Final.XpGain` 곱 적용 |
| `Core/GameManager.cs` | `GrantGold(int)` 신설 — **게임플레이 골드는 전부 이 함수를 거치며 `GoldGain` 배율이 여기서 적용**된다. 클리어 보상을 `[SerializeField]` 3개로 분리 |
| `Enemy/EnemyData.cs` | `Tint` / `SizeScale` 추가 |
| `Enemy/EnemyBase.cs` | `OnInitialized`에서 sprite·color·scale을 **매번 덮어쓰도록** 변경. 기존엔 엘리트/보스일 때만 색을 칠해서 **풀에서 재사용될 때 이전 적의 보라/빨강이 남는 잠복 버그**가 있었음 |
| `Meta/MetaProgressionManager.cs` | `RegisterRunResult(kills)` 신설 + `ApplyStatKey`에 `XpGain`/`GoldGain` 추가 |
| `Wave/WaveData.cs` | **신규 파일.** `WaveManager.cs` 에서 분리 (아래 I-19) |

---

## 2-4. ✅ 피격 반응 — 넉백 / 무적 / 연출 (2026-08-26, 3차)

"적에 닿으면 체력이 계속 줄어든다"는 보고에서 출발해 접촉 피해 구조를 바꿨다 (I-20).

**지속 피해 → 한 방 + 무적 시간**

| 파일 | 변경 |
|---|---|
| `Enemy/EnemyBase.cs` | `OnTriggerStay2D` 가 `ContactDamage` **전액**을 `PlayerStats.TryTakeHit` 으로 넘긴다. `Stay` 로 둔 이유는 계속 붙어 있으면 무적이 풀리는 즉시 다시 맞아야 하기 때문 |
| `Player/PlayerStats.cs` | `invincibleTime`(0.6초) + `IsInvincible` + `TryTakeHit(raw, sourcePos)` 신설. 무적 중이면 `false` 반환하고 아무 일도 없다. `TakeDamage` 는 이제 데미지 팝업도 띄운다 |
| `Player/PlayerController.cs` | `PlayHitFeedback(dir, invincibleTime)` 신설 — 넉백 + 붉은 플래시 + 깜빡임 + 카메라 흔들기 |

> **넉백이 안 보이던 이유**: `PlayerController.Update` 가 **매 프레임** `rb.linearVelocity` 를
> 이동 입력으로 덮어쓴다. 그래서 `_knockbackTimer` 가 도는 동안에는 그 대입을 건너뛰고
> 속도를 감쇠만 시킨다. 이 처리를 안 하면 넉백이 한 프레임도 보이지 않는다.

**연출 순서** — 맞은 순간 붉게(0.12초) → 무적이 끝날 때까지 알파 깜빡임(0.07초 주기) → 원래 색 복귀.
색 복원은 `TickHitFeedback()` 이 `Update` **맨 앞**에서 처리한다. 입력이 막히거나(레벨업 패널)
죽은 뒤에도 색이 붉은 채로 굳지 않게 하기 위함.

**수치는 전부 `Economy.csv` 에 있다** (`invincibleTime` / `knockbackForce` / `knockbackTime` /
`hitFlashTime` / `blinkInterval` / `shakeMagnitude`). 자세한 건 [`BALANCE.md`](BALANCE.md) §3-4.

**검증** — 임시 `HitSmokeTest` 로 0.1초마다 `TryTakeHit` 을 24회 호출:

| 확인 | 결과 |
|---|---|
| 피해가 들어간 시각 | `t=0.0 / 0.6 / 1.2` — 무적 0.6초 간격 정확 |
| 그 사이 호출 | 전부 `landed=False`, HP 변화 없음 |
| 넉백 속도 | 가해자 반대 방향으로 `-9.00` → 0.2초 내 `0.00` 감쇠 |

> 검증 후 `HitSmokeTest.cs` 와 씬 오브젝트 모두 삭제 완료. 콘솔 에러 0 / 경고 0.

---

## 2-5. ✅ 플레이어 스탯 2배 버그 (I-21, 2026-08-26 3차)

I-20 검증 로그에 `hp=200` 이 찍혀서 발견했다. `Economy.csv` 의 `baseStats.MaxHp` 는 100이다.

`StatBlock` 은 인스펙터에서 `baseStats` 를 처음 만들 때 쓸 **기본값**을 필드 초기값으로 갖고 있다
(`MaxHp = 100f`, `MoveSpeed = 4f`, `Damage = 1f` …). 그런데 `GetStatBonus()` 가 그걸
**보너스 블록**의 시작점으로 썼다. `PlayerStats.RecalculateStats()` 는 `base + meta` 를 하므로 전부 2배.

| 스탯 | 의도 | 버그 시 |
|---|---|---|
| MaxHp | 100 | 200 |
| MoveSpeed | 4 | 8 |
| Damage | 1 | 2 |
| AttackSpeed | 1 | 2 → **공격 속도 절반** (쿨다운 배율) |
| ProjectileSize | 1 | 2 |
| PickupRadius | 2 | 4 |
| CritChance | 0.05 | 0.1 |
| CritMultiplier | 1.5 | 3.0 |

> `XpGain`/`GoldGain` 에만 `RecalculateStats()` 안에 `-1f` 땜질이 있었다.
> **그 땜질 자체가 이 버그의 증상이었는데 두 필드에만 적용돼 있었다.**

**수정** — `StatBlock.Zero()` (전 필드 0) 팩토리를 추가하고

| 위치 | 변경 |
|---|---|
| `Player/StatBlock.cs` | `static StatBlock Zero()` 신설. 왜 기본 생성자를 보너스로 쓰면 안 되는지 주석으로 남김 |
| `Meta/MetaProgressionManager.cs` | `GetStatBonus()` 의 `new StatBlock()` → `StatBlock.Zero()` |
| `Player/PlayerStats.cs` | `?? new StatBlock()` 폴백 → `?? StatBlock.Zero()`, `XpGain`/`GoldGain` 의 `-1f` 땜질 제거 |

**검증** — 임시 `StatSmokeTest` 로 Play 직후 `Final` 을 덤프:

```
[STAT] hp=100 MaxHp=100 MoveSpeed=4 Damage=1 AttackSpeed=1 ProjSize=1
       Pickup=2 Crit=0.05 CritMul=1.5 Armor=0 XpGain=1 GoldGain=1
```

`Economy.csv` 와 11개 항목 전부 일치. 검증 후 스크립트·씬 오브젝트 삭제 완료.

> ⚠️ 이 수정으로 **체력·이동속도·픽업 반경이 절반, 공격 속도는 2배**가 됐다.
> [`BALANCE.md`](BALANCE.md) 의 수치는 원래 CSV 값 기준으로 쓰여 있어서 이제야 문서와 실제가 맞는다.
> 대신 **체감 난이도가 크게 올라갔을 것**이므로 플레이 후 재조정이 필요하다.

---

## 2-6. ✅ 직업(Class) 시스템 — 시작 무기 지급 (I-22, 2026-08-26 4차)

**플레이어가 맨손으로 시작했다.** `AddOrUpgradeWeapon()` 의 호출자가 레벨업 카드(`LevelUpManager.cs:96`)
하나뿐이라, 첫 레벨업까지 아무도 공격할 수 없었다. 이걸 직업 데이터로 메웠다.

### 데이터

`Assets/Scripts/Player/CharacterClassData.cs` (SO. I-19 때문에 **같은 이름의 파일**에 단독으로 둔다)

| 그룹 | 필드 |
|---|---|
| 기본 | `ClassName` / `Description` |
| 시작 무기 | `StartingWeapon`(WeaponData) / `StartingWeaponLevel` |
| 스탯 | `BonusMaxHp` … `BonusGoldGain` 10종 — **차이값**이라 0이 기본 |
| 연출 (빈 슬롯) | `Portrait` / `BodySprite` / `ModelPrefab` |
| 해금 (미사용) | `UnlockedByDefault` / `UnlockCost` |

기본 스탯 전체가 아니라 **보너스(차이값)** 로 둔 이유: 직업이 `MaxHp` 원본을 들고 있으면
`Economy.csv` 의 `baseStats` 와 원본이 둘로 갈라진다. I-21 과 같은 부류의 사고를 미리 막았다.

| Id | 시작 무기 | 성격 |
|---|---|---|
| Warrior | Sword | HP +30 / 이속 −0.3 / 방어 +2 |
| Ranger | Bow | HP −15 / 이속 +0.6 / 공속 −0.1 / 흡수 +0.5 / 치명 +0.05 |
| Mage | Fireball | HP −25 / 피해 +0.2 / 투사체 +0.2 / 방어 −1 / XP +0.1 |

### 코드 변경

| 위치 | 변경 |
|---|---|
| `Player/CharacterClassData.cs` | 신규. `ApplyBonus(StatBlock)` 포함 |
| `Player/PlayerStats.cs` | `Class` 프로퍼티 + `ApplyClass()` 신설. `RecalculateStats()` 합산이 `base + meta + class + passive` 로 바뀜 |
| `Player/PlayerController.cs` | `ApplyBodySprite(Sprite)` — 직업 스프라이트 교체용 (지금은 애셋이 없어 호출돼도 무시됨) |
| `Core/GameManager.cs` | `classes[]` / `defaultClassIndex` 필드, `SelectedClass` / `SelectClass(int)` / `ApplySelectedClass()`. `StartRun()` 이 이걸 호출한다 |
| `Editor/BalanceImporter.cs` | `Classes.csv` ↔ `Assets/Game/ClassData/*.asset` 임포트/익스포트 |
| `Balance/Classes.csv` | 신규 (3행) |
| `Balance/SceneWiring.csv` | `GameManager,classes` 행 추가 → 적용 수 4/4 → **5/5** |

`_selectedClassIndex` 는 `_autoStartRunOnLoad` 와 같은 이유로 **static** 이다.
Retry 는 씬을 다시 로드하므로 인스턴스 필드로 두면 고른 직업이 날아간다.

### 검증

임시 `ClassSmokeTest` 로 Play 1초 뒤 `StartRun()` 을 호출하고 상태를 덤프:

```
[GameManager] 직업 'Warrior' — 시작 무기 Sword Lv1
[CLS] class=Warrior hp=130/130 spd=3.7 armor=2
[CLS] weapons=1 : Sword
[CLS] child weapon obj=Weapon_Sword(Clone) active=True
```

`base(100/4/0) + Warrior(+30/−0.3/+2)` 와 정확히 일치하고 무기 오브젝트도 살아 있다.
검증 후 스크립트·씬 오브젝트 삭제 완료. 콘솔 0 에러 / 0 경고.

> 직업 선택 UI 는 이어지는 5차 작업에서 완성했다 → **2-7**.

---

## 2-7. ✅ 직업 선택 UI (I-23, 2026-08-26 5차)

### 원인

I-22 로 직업 데이터는 생겼지만 **고르는 화면이 없어서** `defaultClassIndex` 로만 고정됐다.
Ranger/Mage 를 보려면 인스펙터에서 인덱스를 바꿔야 했다.

### 화면 구성 (사용자 스케치 기준)

| 위치 | 요소 | 오브젝트 |
|---|---|---|
| 좌상단 | 직업 칸 그리드 (4열 `GridLayoutGroup`, 셀 200×200) | `ClassSelectPanel/CardGrid` |
| 우측 | 선택한 직업의 일러스트 패널 (520×800) | `ClassSelectPanel/PortraitPanel` |
| 좌하단 | `Description` 라벨 + 설명 바 (설명 + 스탯 요약) | `ClassSelectPanel/DescBar` |
| 하단 | Back / Start | `ClassSelectPanel/BackButton`·`StartButton` |

일러스트가 없으면 `PortraitPlaceholder`(`No Illustration`)가 대신 뜬다.
`Classes.csv` 의 `Portrait` 열을 채우면 자동으로 교체된다 — **코드 수정 불필요.**

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Core/GameManager.cs` | `GameState.ClassSelect` 를 enum **맨 끝**에 추가 · `SelectedClassIndex` getter |
| `UI/ClassSelectUI.cs` | 신규. 카드 생성 · 선택 · 일러스트/설명 갱신 · Start/Back |
| `UI/ClassCardUI.cs` | 신규. 칸 하나 (초상화 · 이름 · 선택 테두리 · 잠금 오버레이) |
| `UI/MainMenuUI.cs` | Start → `ChangeState(ClassSelect)` (직업 목록이 비면 예전대로 즉시 `StartRun()`) |
| `Prefabs/Prefab_ClassCard.prefab` | 신규 |
| `Scenes/SampleScene.unity` | `UI Canvas/ClassSelectPanel` 신설 + `ClassSelectUI` 컴포넌트 배선 |

> ⚠️ **`GameState` 에 새 값을 중간에 끼워 넣지 말 것.** enum 은 씬에 **정수**로 직렬화된다
> (`StateVisibilityBinder.visibleStates`). 중간 삽입 시 뒤 값이 전부 한 칸 밀려 기존 배선이 조용히 깨진다.
> 이번에 실제로 `MainMenu` 뒤에 넣었다가 되돌렸다.

### 검증

임시 `ClassSelectSmokeTest` 로 버튼을 코드에서 눌러 흐름 전체를 덤프:

```
[CS] 1 state=MainMenu
[GameManager] State → ClassSelect
[CS] 2 state=ClassSelect panelActive=True cards=3
[CS] 3 default name=Warrior placeholder=True
[CS] 3 stat=Weapon: Sword  HP +30  Speed -0.3  Armor +2
[CS] 4 picked name=Ranger
[CS] 4 desc=Fast skirmisher. Starts with a Bow.
[CS] 4 stat=Weapon: Bow  HP -15  Speed +0.6  Attack Speed +0.1  Pickup +0.5  Crit +0.05
[CS] 5 card0 name=Warrior sel=False lock=False
[CS] 5 card1 name=Ranger  sel=True  lock=False
[CS] 5 card2 name=Mage    sel=False lock=False
[GameManager] 직업 'Ranger' — 시작 무기 Bow Lv1
[CS] 6 state=StageMap panelActive=False
[CS] 6 class=Ranger hp=85/85 spd=4.6 atkSpd=0.9
[CS] 6 weapons=Bow
```

`base(100/4/1) + Ranger(−15/+0.6/−0.1)` 과 정확히 일치한다.
`Attack Speed` 는 쿨다운 배율이라 **표시만 부호를 뒤집어** `+0.1`(빨라짐)로 보여준다.
게임뷰 캡처로 레이아웃도 육안 확인했다. 검증 후 스크립트·씬 오브젝트·캡처 파일 삭제 완료.
콘솔 0 에러 / 0 경고.

---

## 2-8. ✅ 레벨업 카드가 안 뜸 + 리롤 1회 제한 (I-24, 2026-08-26 6차)

### 원인

**`?.` 는 Unity 의 "가짜 null" 을 걸러내지 못한다.**

`HUDManager.OnLevelUp` 이 `levelUpAnimator?.SetTrigger("LevelUp")` 을 호출했는데,
`levelUpAnimator` 는 씬에서 **미할당** 상태였다. 미할당 직렬화 필드는 C# 기준 `null` 이 아니라
접근 시 `UnassignedReferenceException` 을 던지는 **가짜 null** 이라, `?.` 가 통과시켜 버린다.

그 예외가 이벤트 핸들러 밖으로 전파되면서 호출 사슬이 통째로 끊겼다:

```
ExperienceManager.CollectXp()
  └ OnLevelUp?.Invoke()  →  HUDManager.OnLevelUp()  ← 💥 여기서 예외
    TriggerLevelUp()                                  ← 실행 안 됨
      └ LevelUpManager.ShowLevelUpPanel()             ← 그래서 패널이 안 뜬다
```

`LevelUpManager` 배선(`cards[3]`, `allItems[17]`)과 패널 계층은 **처음부터 정상**이었다.
카드가 안 뜬 게 아니라 **패널을 켜는 코드까지 도달하지 못한 것**이다.

> ⚠️ 프로젝트 전체 `?.` 48곳을 훑어 **직렬화 Unity Object 필드에 쓴 곳은 여기 하나뿐**임을 확인했다.
> 앞으로 직렬화 필드는 반드시 `if (field != null)` 로 검사할 것 → `CLAUDE.md` §3 에 규칙 추가.

### 리롤 1회 제한

기존엔 골드만 있으면 무한 리롤이 가능했다. 레벨업 **1회당 딱 한 번**으로 바꿨다.

| | 전 | 후 |
|---|---|---|
| 제한 | 없음 (골드만 있으면 무한) | 레벨업 1회당 1번 (`_rerollUsed` 플래그) |
| 비용 | `rerollCost` + 리롤할 때마다 `rerollCostIncrease` 만큼 증가 | `rerollCost` 고정 (1회뿐이라 증가가 무의미) |
| 버튼 표시 | `Reroll (nG)` | `Reroll (1G)  x1` → 쓰고 나면 `Reroll used` + 비활성 |

`_rerollUsed` 는 `ShowLevelUpPanel()` 에서 초기화되므로 **레벨업마다 리롤 1회가 새로 주어진다.**

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `UI/HUDManager.cs` | `OnLevelUp` 의 `?.` → `if (!= null)`. `levelUpEffect`/`PlayerController` 도 널 가드 |
| `LevelUp/LevelUpManager.cs` | `_currentRerollCost`·`rerollCostIncrease` 제거 → `_rerollUsed` bool. `ShowLevelUpPanel`/`OnRerollClicked`/`RefreshPanel`/`ResetRunState` 수정 |
| `Game/Balance/Economy.csv` | `LevelUpManager,rerollCostIncrease` 행 **삭제** (필드가 없어져 임포터가 잡을 대상이 없음). `ShopManager,rerollCostIncrease` 는 그대로 유효 |

### 검증

임시 `LevelUpSmokeTest` 로 메인메뉴 → 직업선택 → 웨이브 → `CollectXp(999)` 까지 태운 뒤 덤프:

```
[GameManager] State → LevelUp
[LU] 1 state=LevelUp panelActive=True
[LU] 2 card0 active=True name='Fireball'
[LU] 2 card1 active=True name='Armor'
[LU] 2 card2 active=True name='Attack Speed'
[LU] 3 before reroll: text='Reroll (1G)  x1' interactable=True gold=52
[LU] 4 after reroll#1: text='Reroll used' interactable=False gold=51 cards=[Damage Up Bombard Gun]
[LU] 5 after reroll#2: gold=51 (blocked=True)
```

수정 전에는 `[LU] 1 state=Wave` 에서 멈추고 `UnassignedReferenceException: The variable
levelUpAnimator of HUDManager has not been assigned.` 이 떴다 — `[LU] 2` 는 아예 안 찍혔다.

2회차 리롤은 골드가 **깎이지 않고** 막혔다(`blocked=True`). 게임뷰 캡처로 카드 3장과
회색 처리된 Reroll 버튼도 육안 확인. 검증 후 스크립트·씬 오브젝트·캡처 삭제 완료.
콘솔 0 에러 / 0 경고.

> 콘솔을 `[LU]` 로 필터링했다면 예외를 못 봤을 것이다.
> **`Types:["All"]` + 필터 없이** 읽어야 원인이 보인다.

### 레벨업 패널 확대 (뱀서 스타일)

카드가 뜨게 되고 나서 보니 패널이 **560×320** — 1920×1080 기준 화면 폭의 **29%** 에
폰트가 10~14pt 라 한눈에 안 들어왔다. 뱀파이어 서바이벌처럼 화면을 채우도록 키웠다.

| 대상 | 전 | 후 |
|---|---|---|
| `PanelBG` | 560 × 320 | **1560 × 900** (화면의 81% × 83%), y = −40 |
| `CardContainer` | 520 × 220 · 간격 16 | **1400 × 560** · 간격 **40** |
| 카드 1장 | 150 × 200 | **440 × 560** |
| 아이콘 | 64 | **180** |
| 이름 / 설명 / 태그 | 14 / 10 / 10 pt | **44 / 26 / 26** pt |
| `SelectButton` | 130 × 30 · 13pt | **340 × 78** · **34**pt |
| `RerollButton` | 140 × 32 · 13pt | **320 × 80** · **34**pt |
| `TitleText` | 24pt, 화면 최상단 | **60pt, 패널 안쪽 위** |

> ⚠️ `TitleText` 는 `PanelBG` 가 아니라 **`LevelUpPanel`(전체화면) 직속**이다.
> 처음에 화면 최상단(y = −78)에 뒀더니 **HUD 의 XP 바와 겹쳤다.**
> 패널을 y = −40 으로 내리고 타이틀을 y = −195 로 옮겨 패널 안에 넣어 해결.

캡처로 겹침 없음 확인. `HorizontalLayoutGroup` 이 카드 폭을 강제하지 않으므로
(`childControlWidth = false`) 카드 `sizeDelta` 를 직접 주면 그대로 반영된다.

---

## 2-9. ✅ 스프라이트 26종 생성 + 전면 배선 (I-25, 2026-08-27 7차)

### 원인

콘텐츠 공백이었지 버그가 아니다. 적 6종이 **`goblin.png` 한 장을 공유**하며 `Tint`(색 곱셈)로만
구분됐고, 무기·건물·패시브 17종의 아이콘은 `sword.png` / `speed.png` / `house.png` /
`turret.png` / `Bullet.png` **5장을 돌려 쓰고 있었다.** 직업 3종은 일러스트가 아예 없어
선택 화면에 `No Illustration` 자리표시가 떴다.

전제 조건이 **Unity AI 구독**이었다. 구독 전에는 생성 요청이 서버에서
`NoSubscription` 으로 반려됐다 (`ModelSelectorSuperProxyActions.cs:495`).
사용자가 14일 체험을 구독하면서 해금 → 모델 37종 사용 가능.

### 생성

| | |
|---|---|
| 모델 | **`gpt-image-1-5`** (`Unity_AssetGeneration_GenerateAsset` / `GenerateSprite`) |
| 산출 | 1024×1024 **RGBA** (알파 있음), 26장 |
| 소모 | **69 포인트** (1000 중) = 장당 3 포인트 |
| 스타일 | 픽셀아트 — 청키 픽셀 · 진한 아웃라인 · 제한 팔레트 · 투명 배경 |

프롬프트 템플릿 (26장 전부 이 틀을 공유해서 톤이 일관된다):

```
Pixel art game sprite of <SUBJECT>, single character/item centered, <POSE>,
chunky visible pixels, dark outline, limited color palette, simple shading,
transparent background, no text, no ground shadow, retro 16-bit RPG <CONTEXT> style
```

| 폴더 | 개수 | 목록 |
|---|---|---|
| `Sprites/Enemies/` | 6 | Slime · Goblin · Zombie · Wolf · Demon · Ogre |
| `Sprites/Weapons/` | 5 | Sword · Bow · Gun · Fireball · Bomb |
| `Sprites/Passives/` | 9 | Damage · MoveSpeed · AttackSpeed · MaxHp · Armor · PickupRadius · XpGain · GoldGain · CritChance |
| `Sprites/Buildings/` | 3 | House · Turret · Bombard |
| `Sprites/Classes/` | 3 | Warrior · Ranger · Mage |

> ⚠️ **모델 선택이 중요하다.** 파일럿에서 `game-ui-essentials-2` 로 뽑은 Sword 는
> **알파 없는 RGB에 흰 배경**이 박혀 나왔다 (`corner=(255,253,254)`). 프롬프트에
> `transparent background` 를 넣어도 소용없다. `gpt-image-1-5` 만 RGBA 를 준다.
> 그래서 파일럿 3장만 먼저 뽑아 확인한 뒤 나머지 23장을 돌렸다.
>
> ⚠️ **같은 경로로 재생성하면 덮어쓰지 않고 `Sword 1.png` 를 만든다.** 기존 파일을
> 지우고 새로 생성하거나, 생성 후 `.png` + `.meta` 를 같이 rename 해야 한다.

### 임포트 설정 (픽셀아트)

26장 전부에 일괄 적용. **`Filter Mode = Bilinear`(기본값) 이면 픽셀이 뭉개진다.**

| 항목 | 값 | 이유 |
|---|---|---|
| `filterMode` | **Point** | 픽셀아트 보간 끔 |
| `textureCompression` | **Uncompressed** | 압축 아티팩트가 아웃라인을 갉아먹음 |
| `mipmapEnabled` | false | 2D 라 불필요 |
| `alphaIsTransparency` | true | 아웃라인 가장자리 검은 테 방지 |
| `spritePixelsPerUnit` | 512 | 1024px → 월드 2유닛 |
| `maxTextureSize` | 512 | 1024 는 과함 |

### 배선

CSV 가 원본이므로 **CSV 의 경로 열만 고치고 Import 를 돌렸다.** SO 를 직접 건드리지 않았다.

| CSV | 열 | 변경 |
|---|---|---|
| `Enemies.csv` | `Sprite` | `ICON/goblin.png` ×6 → 종별 고유 스프라이트 |
| `Enemies.csv` | `Tint` | `#6FE06F` 등 5종 → **전부 `#FFFFFF`** |
| `Weapons.csv` | `Icon` | 5행 |
| `Buildings.csv` | `Icon` | 3행 |
| `Items.csv` | `Icon` | **17행** (레벨업 카드 · 상점에 뜨는 아이콘) |
| `Classes.csv` | `Portrait`, `BodySprite` | 3행 × 2열 (기존 공란) |

> **`Tint` 를 흰색으로 되돌린 이유:** `Tint` 는 스프라이트에 **곱해지는** 색이다.
> 한 장을 공유하던 시절엔 종을 구분하는 유일한 수단이었지만, 이제 색이 있는 고유
> 스프라이트가 생겼으므로 그 위에 또 색을 곱하면 **탁해진다.**
> 앞으로 `Tint` 는 피격 점멸·상태이상 같은 **연출용**으로만 쓴다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Game/Sprites/**/*.png` | **신규 26장** (+ `.meta`) |
| `Assets/Game/Balance/Enemies.csv` | `Sprite` 6행, `Tint` 6행, 주석 |
| `Assets/Game/Balance/Weapons.csv` | `Icon` 5행 |
| `Assets/Game/Balance/Buildings.csv` | `Icon` 3행 |
| `Assets/Game/Balance/Items.csv` | `Icon` 17행 |
| `Assets/Game/Balance/Classes.csv` | `Portrait` / `BodySprite` 3행 |

### 검증 로그

```
[알파]   26/26  RGBA (1024,1024) alpha 최소값 = 0   ← 전부 투명 영역 있음
[임포트] Applied to 26 textures
[Import] Weapons 5 / Buildings 3 / Passives 9 / Enemies 6 / Items 17 / Classes 3
[배선]   sprite fields: 37 total, 0 null
         Enemies 6 · Items 17 · Weapons 5 · Buildings 3 · Classes 3(Portrait+BodySprite=6)
[잔여]   CSV 내 Assets/Game/ICON/ 참조 = 0건
[포인트] 989 → 920
```

37개 필드가 **전부 `Sprite` 타입으로 연결됐고 null 이 하나도 없다.**
컨택트 시트를 만들어 눈으로도 확인했다 — 26장 모두 아웃라인·팔레트·픽셀 크기가 일관됨.
(확인 후 임시 시트 파일은 삭제)

---

## 2-10. ✅ 적 연출 — 등급 외곽선 + 걷기 바운스 + 피격 플래시 (I-26, 2026-08-27 8차)

I-25 로 종별 고유 스프라이트가 들어간 **직후에 드러난 두 문제**를 함께 고쳤다.

### 원인

**(1) 엘리트/보스가 원화를 죽인다**

`EnemyBase.OnInitialized()` 가 등급에 따라 `SpriteRenderer.color` 를 덮어썼다.

```csharp
sr.color = IsBoss  ? new Color(1f, 0.2f, 0.1f)   // 보스 = 빨강
         : IsElite ? new Color(0.8f, 0.3f, 1f)   // 엘리트 = 보라
         : Data.Tint;
```

`sr.color` 는 **곱셈**이다. 적 6종이 전부 회색 실루엣 한 장을 공유하던 시절엔
등급을 알리는 유일한 수단이었지만, 색이 있는 고유 스프라이트가 생긴 뒤로는
**녹색 좀비 × 보라 ≈ 검정**이 된다. 등급은 알아보되 원화는 건드리지 않아야 한다.

**(2) 적이 미끄러진다**

생성한 26장이 전부 1프레임 정지 이미지다. `Rb.linearVelocity` 로 이동만 하니
발도 안 딛고 좌우도 안 보고 평행이동한다.

### 왜 이 방법인가

| 결정 | 이유 |
|---|---|
| 색 곱셈 → **알파 팽창 외곽선** | 원화 픽셀을 하나도 건드리지 않는다. 사용자가 3안 중 이걸 선택 |
| 자식 `SpriteRenderer` 오라 ✗ → **셰이더** | 오라는 오브젝트 수가 2배가 되고 스프라이트가 바뀔 때마다 동기화해야 한다 |
| 바운스를 `transform.localScale` ✗ → **버텍스 셰이더** | 루트를 늘이면 `CapsuleCollider2D` 까지 늘어나 **판정이 변한다.** 정점만 흔들면 물리와 완전히 분리된다 |
| `sr.material` ✗ → **`MaterialPropertyBlock`** | 풀에서 수백 마리가 돌아간다. `sr.material` 접근은 개체마다 머티리얼을 복제한다 |
| `Sprite-Lit-Default` ✗ → **Unlit 커스텀** | 씬의 `Light2D` 가 Global/흰색/intensity 1 **하나뿐**이라 결과가 같다 (조명을 쓰게 되면 이 셰이더도 손봐야 한다) |
| 개체마다 **랜덤 위상** (`_AnimPhase`) | 같은 프레임에 스폰된 무리가 한 몸처럼 출렁이는 걸 막는다 |

### 함정 두 개

**`Mesh Type = Tight` 는 외곽선을 잘라먹는다.** 기본값 Tight 는 스프라이트 메시가
알파에 딱 붙어서 팽창시킬 여백이 없다. 적 6종을 **`FullRect`** 로 다시 임포트했다.

**8방향 샘플은 폭이 커지면 네모나진다.** 대각선 샘플이 √2 만큼 멀리 찍혀
모서리가 부풀기 때문이다. 첫 캡처에서 보스가 빨간 사각 덩어리로 나왔다.
**반지름 위 16방향 원형 샘플**로 바꿔 해결 (각도가 컴파일 타임 상수라 `sin`/`cos` 는 폴딩됨).

**`_MainTex_TexelSize` 는 2D SRP Batcher 를 끈다.**
> Material 'SpriteOutline' has _TexelSize / _ST texture properties which are not supported by 2D SRP Batcher.

스프라이트가 전부 512px 로 통일돼 있으므로 `_OutlineTexSize` 프로퍼티로 대체했다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Game/Shaders/SpriteOutline.shader` | **신규.** URP Unlit. 외곽선(16방향 알파 팽창) + 피격 플래시 + 버텍스 스쿼시/바운스 |
| `Assets/Game/Materials/SpriteOutline.mat` | **신규.** 기본값 외곽선 0 (일반 몹) |
| `Assets/Scripts/Enemy/EnemyVisual.cs` | **신규.** 좌우 `flipX`(데드존 0.15) · 바운스 파라미터 · 피격 플래시 감쇠 |
| `Assets/Scripts/Enemy/EnemyBase.cs` | 색 곱셈 제거 → `ApplyRankOutline()` · `Visual` 캐시 · `TakeDamage` 에서 `Visual.Flash()` |
| `Assets/Prefabs/Enemy_Goblin.prefab` | `EnemyVisual` 추가 · `SpriteRenderer.material` `Sprite-Lit-Default` → `SpriteOutline` |
| `Assets/Game/Sprites/Enemies/*.png` (6장) | `spriteMeshType` `Tight` → **`FullRect`** |
| `Assets/Game/Sprites/Enemies/Wolf.png` | 좌우 반전 (원본이 왼쪽을 봐서 `flipX` 규약과 어긋났다) |

### 수치

| | 일반 | 엘리트 | 보스 |
|---|---|---|---|
| `_OutlineWidth` (텍스처 px) | 0 | 14 | **12** |
| 색 | — | 보라 `(0.75, 0.35, 1)` | 빨강 `(1, 0.25, 0.15)` |
| 오브젝트 스케일 | ×1 | ×1.3 | ×2 |

> 보스가 엘리트보다 **숫자가 작은 이유**: 폭은 텍스처 픽셀 단위라 오브젝트 스케일에
> 같이 곱해진다. 보스는 2배로 커지므로 20 을 그대로 두면 화면상 두께가 엘리트의
> 2.2배가 되어 다리 사이가 메워진다.

바운스: `_AnimSpeed = bounceSpeed(9) × clamp(MoveSpeed, 0.5, 3)`,
`_SquashAmt = 0.07`, `_BobAmt = 0.04`, `_AnimPhase` 는 개체마다 `Random(0, 2π)`.
`_Time.y` 는 `timeScale` 을 따르므로 **일시정지·레벨업 중에는 같이 멈춘다** (의도한 동작).

### 검증 로그

```
[셰이더]  isSupported=True  messages=0
[머티리얼] shader=VS_LIKE/SpriteOutline  outlineTexSize=512  outlineWidth=0
[프리팹]  SpriteRenderer.mat=SpriteOutline   EnemyVisual=True

플레이 모드 — 같은 EnemyData(Goblin) 로 3마리 스폰
  Normal  color=(1,1,1,1)  outlineW=0   animSpeed=19.80  animPhase=4.2371  scale=1.0
  Elite   color=(1,1,1,1)  outlineW=14  animSpeed=23.76  animPhase=0.0085  scale=1.3
  Boss    color=(1,1,1,1)  outlineW=12  animSpeed=15.84  animPhase=3.4447  scale=2.0

[캡처] 일반=외곽선 없음 · 엘리트=보라 · 보스=빨강, 원화 색 그대로
[버텍스] 강제로 sin=+1 주입 → 세로로 늘고 위로 떠오름 (버텍스 경로 동작 확인)
[정리] 콘솔 0건 · [TEST] 오브젝트 0개
```

> **바운스는 스크린샷으로 검증할 수 없다.** `Unity_SceneView_Capture2DScene` 은
> `_Time` 이 고정된 상태로 오프스크린 렌더하기 때문에 몇 초를 띄워 두 장을 찍어도
> **바이트 단위로 동일한 이미지**가 나온다. `_AnimPhase` 에 `π/2` 를 강제로 넣어
> `sin=+1` 을 만든 뒤 변형이 나타나는지로 확인했다.

---

## 2-11. ✅ 바닥 타일맵 + 플레이어 크기·걷기 연출 (I-27~I-31, 2026-08-27 9차)

사용자 요청 네 가지 — **플레이어 크기 맞추기 · 맵 타일 10종 랜덤 배치 · 눈 안 아프게 ·
캐릭터가 이동할 때 몸이 움직이게** — 를 한 묶음으로 처리하면서 그 과정에서
씬의 버그 두 건(I-30 / I-31)을 함께 찾아 고쳤다.

### 원인

**(1) 플레이어만 작았다 — PPU 함정 (I-27)**

`maxTextureSize` 가 원본보다 작으면 Unity 는 텍스처를 줄이면서 **`spritePixelsPerUnit` 은 그대로 둔다.**
`bounds = rect / ppu` 이므로 rect 만 반토막 나고 **스프라이트가 그만큼 작아진다.**

| | rect | ppu | 월드 크기 |
|---|---|---|---|
| 적 6종 | 512 | 1024 | **0.50** |
| 직업 정지그림 (수정 전) | 512 | 1024 | 0.50 → 화면상 적과 같아 플레이어가 안 보였다 |
| 직업 정지그림 (수정 후) | 512 | **512** | **1.00** |
| 걷기 프레임 | 256 | 256 | **1.00** |
| 바닥 타일 | 256 | 256 | **1.00** (= Grid `cellSize` 1) |

**(2) 바닥이 아예 없었다 (I-28)**

카메라가 `Skybox` 클리어로 URP 기본 파랑(#314D79)을 그대로 비추고 있었다.
바닥 애셋도 타일맵도 0. 밝은 파랑이라 눈도 아팠다.

**(3) 플레이어가 미끄러진다 (I-29)**

I-26 은 **적**만 고쳤다. 플레이어는 `PlayerController` 의 `sr.flipX` 한 줄이 전부라
좌우로 갈 때 그림이 뒤집히기만 하고 위아래로 갈 때는 아무 일도 안 일어났다.

### 왜 이 방법인가

| 결정 | 이유 |
|---|---|
| 유한 타일맵 ✗ → **카메라 추종 무한 타일러** | `CameraController.useBounds = false` 이고 적은 플레이어 주변 `SpawnRadius` 에서 나온다. **아레나 경계가 없어** 미리 깔아 두면 언젠가 바닥이 끊긴다 |
| `Random` ✗ → **좌표 해시** | 창이 지나갔다 되돌아와도 같은 칸에 같은 그림이 나와야 한다. 난수를 쓰면 되돌아갈 때마다 바닥이 깜빡인다 |
| 칸마다 `SetTile` ✗ → **`SetTilesBlock`** | 한 칸 이동마다 수천 번 호출된다 |
| `GenerateSprite` ✗ → **`GenerateImage` + `gemini-3.1-flash-texture`** | `SupportsTileable` 이라 이음매가 없고 배경 제거가 안 걸린다 (바닥 텍스처에 배경 제거는 재앙) |
| 새 캐릭터 생성 ✗ → **`referenceImageInstanceId`** | AI 생성은 매번 독립이라 같은 캐릭터가 안 나온다. 기존 원화를 레퍼런스로 넣어 디자인을 유지했다 |
| `transform.rotation` ✗ → **버텍스 전단(shear)** | 루트를 돌리면 `Rigidbody2D` 와 싸운다. 정점만 밀면 물리와 완전히 분리된다 (I-26 과 같은 이유) |

### 함정 세 개

**`GenerateSpritesheet` 출력에는 알파가 아예 없다.** 48프레임 전부 `opaque=65536`.
흰 배경이 진짜 불투명 픽셀이라 그대로 쓰면 플레이어가 **흰 사각형**으로 렌더된다.
프레임마다 테두리에 닿은 연결 성분만 지우는 방식(`scipy.ndimage.label`,
조건 `diff≤30 & sat≤24 & bri≥150`)으로 제거 → 투명 비율 Warrior 62.5% / Ranger 74.1% / Mage 78.0%.

**타일 10장을 그냥 깔면 퀼트(조각보)가 된다.** 독립 생성이라 평균 휘도가
0.164 ~ 0.325 로 **2배**까지 벌어져 첫 캡처가 알록달록한 체크무늬였다.
타일마다 `Tile.color` 배율로 평균을 한 목표색에 맞춰 눌렀다.
`Tile.color` 는 정점 스트림의 `Color32` 라 **1을 넘는 배율은 잘린다** → 목표는 가장 어두운 타일 이하로 잡아야 한다.

**`GetComponent<T>() ?? AddComponent<T>()` 는 동작하지 않는다.** Unity 의 "가짜 null" 이
`??` 를 그냥 통과해 `MissingComponentException` 이 났다. CLAUDE.md 의 I-24 규칙과 같은 함정.
`if (c == null)` 로 명시 검사하는 헬퍼로 교체.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Game/Sprites/Tiles/Tile_*.png` (10장) | **신규.** 이음매 없는 바닥 텍스처. Single/FullRect/max 256/ppu 256/Bilinear/무압축 |
| `Assets/Game/Tiles/Tile_*.asset` (10개) | **신규.** `UnityEngine.Tilemaps.Tile`. `colliderType = None`, `color` = 평준화 배율 |
| `Assets/Scripts/Stage/GroundTiler.cs` | **신규.** 카메라 추종 무한 타일러. 좌표 해시 + 가중치 추첨 + `SetTilesBlock` |
| `Assets/Game/Sprites/Classes/Walk/{Warrior,Ranger,Mage}_Walk.png` | **신규.** 4×4 = 16프레임 걷기 시트. Multiple/FullRect/max 1024/ppu 256/Point. 배경 제거 후처리 완료 |
| `Assets/Scripts/Player/PlayerVisual.cs` | **신규.** 걷기 프레임 재생 + 바운스 + 진행 방향 기울이기 |
| `Assets/Game/Shaders/SpriteOutline.shader` | `_LeanAmt` 프로퍼티 + 버텍스 전단 `pos.x += pos.y * _LeanAmt` |
| `Assets/Scripts/Player/CharacterClassData.cs` | `Sprite[] WalkFrames` 추가 |
| `Assets/Editor/BalanceImporter.cs` | `WalkSheet` 열 임포트(`LoadSpriteSheet`/`FrameIndex`) + 익스포트 |
| `Assets/Game/Balance/Classes.csv` | `WalkSheet` 열 신설. `BodySprite` 를 걷기 시트로 교체 |
| `Assets/Scripts/Player/PlayerStats.cs` | `ApplyClass` 에서 `PlayerVisual.SetWalkFrames` 호출 |
| `Assets/Game/Sprites/Classes/*.png` (3장) | `spritePixelsPerUnit` 1024 → **512** (I-27) |
| `Assets/Scenes/SampleScene.unity` | `Ground`(Grid) + `GroundTilemap`(`TilemapRenderer` sortingOrder **-100**, `GroundTiler`) 신규 · 카메라 `Skybox`→`SolidColor` / #314D79→**#161815** · Player 머티리얼 `SpriteOutline` + `PlayerVisual` · Camera 태그 `MainCamera`(I-30) |
| `Assets/Prefabs/Enemy_Goblin.prefab` | `CapsuleCollider2D` 0.5×1.0 → **0.34×0.36** (I-31) |

### 수치

`GroundTiler`: `margin = 4`, `seed = 1337`. 타일 색 배율 = `clip(목표 / 평균, 0, 1)`,
목표 = `(0.155, 0.166, 0.116)` (가장 어두운 `Tile_MossyCobble` 기준).

| 타일 | 색 배율 (R, G, B) | 비중 |
|---|---|---|
| `Tile_Grass` | 0.987, 0.892, 0.946 | **40** |
| `Tile_GrassPebble` | 0.673, 0.675, 0.528 | 18 |
| `Tile_Weeds` | 0.708, 0.833, 0.778 | 13 |
| `Tile_Dirt` | 0.585, 0.803, 0.743 | 10 |
| `Tile_Gravel` | 0.594, 0.708, 0.556 | 7 |
| `Tile_Roots` | 0.787, 1.000, 0.911 | 5 |
| `Tile_CrackedEarth` | 0.570, 0.705, 0.618 | 3 |
| `Tile_MossyCobble` | 0.912, 0.971, 1.000 | 2 |
| `Tile_StoneSlab` | 0.537, 0.575, 0.420 | 1 |
| `Tile_Flagstone` | 0.462, 0.511, 0.386 | 1 |

> **평범한 타일(Grass)에 비중 40 을 몰아준 이유**: 10종을 균등하게 뿌리면 평준화를 해도
> 바닥이 산만해진다. 특징이 강한 돌바닥류는 1~3 으로 눌러 "가끔 눈에 띄는" 정도로만 남겼다.

`PlayerVisual`: `framesPerSecond = 12`(이동 속도에 `clamp(speed/4, 0.5, 2)` 비례),
`moveDeadzone = 0.15`, `bounceSpeed = 13`, `leanAmount = 0.10`, `leanResponse = 12`.

### 검증 로그

```
[크기]  적 6종      rect=512 ppu=1024 → bounds 0.50
        걷기 프레임 rect=256 ppu=256  → bounds 1.00
        직업 정지   rect=512 ppu=512  → bounds 1.00
        바닥 타일   rect=256 ppu=256  → bounds 1.00 (= cellSize 1)
[씬]    Camera tag=MainCamera clear=SolidColor bg=RGBA(0.086,0.094,0.082) ortho=10
        Enemy_Goblin capsule=(0.34, 0.36) Vertical
[알파]  걷기 시트 투명 비율 Warrior 62.5% / Ranger 74.1% / Mage 78.0% (마젠타 합성 확인, 후광 없음)

플레이 모드 — SelectClass(0) → StartRun()
  class=Warrior  WalkFrames=16  BodySprite=frame_0
  이동 중 sprite  frame_0 → frame_7 → frame_14     (프레임 실제로 넘어감)
  _LeanAmt=+0.1  _AnimSpeed=13  flipX=False  vel=(4,0)  → 진행 방향으로 기움
[캡처] 퀼트 사라짐. 어두운 올리브 바닥 위에 기사 실루엣 선명
[정리] 콘솔 에러 0 · PlayerController.enabled 복구 · 플레이 모드 종료
```

> **한 번 헛짚었던 것**: 첫 검증에서 프레임이 `frame_0` 에 붙어 있었다.
> **플레이 중에 스크립트를 고쳐 컴파일이 뒤로 밀린 세션**이라 `ApplyClass` 의 새 코드가
> 아직 안 돌고 있었던 것이고, 코드 문제가 아니었다. 플레이 모드를 껐다 켜니 정상 동작.
> **플레이 중 수정한 C# 은 그 세션에서 검증하지 말 것.**

---

## 2-12. ✅ 건물 5종 + Z 즉시 설치 + 패시브 누적 버그 (I-32~I-37, 2026-08-27 10차)

사용자 요청 6건을 한 묶음으로 처리했다.
경험치 오브 크기 / 레벨업 후보 편중 / 클리어 화면 / Z 설치 / 건물 기능 / 건물 쿨다운 패시브.

### 원인

**① 패시브가 레벨업할 때마다 "누적"됐다 (I-32) — 가장 깊은 버그**

`Passives.csv` 는 값이 **"그 레벨일 때의 총 보너스"** 라고 못박고 있는데 런타임은 정반대였다.

```
LevelUpManager.ApplyPassive() →  new PassiveEffect(data, level) 를 매번 새로 만들어 AddPassive()
PlayerStats.RecalculateStats() → _activePassives 를 전부 순회하며 합산
PassiveEffect.UpgradeTo(int)   → 존재하지만 호출자 0건
```

같은 패시브를 5번 고르면 Lv1~Lv5 값이 **전부** 더해진다.
`Damage` Lv5 는 의도한 +0.55 대신 **+1.57**, `BuildingCooldown` 은 배수라서
`1 + (-0.1-0.18-0.26-0.34-0.42) = **-0.30**` 으로 **음수로 뒤집혔다.**
`BuildingBase.Cooldown` 의 `Mathf.Max(0.1f, mult)` 하한에 걸려 0.1 로 잘리는 바람에
**모든 건물이 10배 빨라졌다** (식당 60초 → 6초). 그래서 이 세션에서 새로 만든
`BuildingCooldown` 패시브를 검증할 수가 없었다.

> 부수 효과로 `RemovePassiveByData`(상점 아이템 제거)도 스택 중 **한 개만** 지우고 있었다.

**② 경험치 오브가 너무 컸다 (I-33)** — `Exp_Orb.gif` 의 `spritePixelsToUnits` 가 작아
오브가 플레이어만 했다. 화면이 오브로 뒤덮여 적이 안 보였다.

**③ 리롤을 안 돌리면 같은 아이템만 나왔다 (I-34)** — 예전 `PickCandidates` 는
**보유 아이템 업그레이드로 슬롯을 먼저 다 채우고** 남은 자리에만 신규를 넣었다.
최대레벨이 아닌 아이템 3개만 들고 있으면 카드 3장이 **영구히 그 3개로 고정**된다.

**④ 클리어 화면에 성장 정보가 없었다 (I-35)** — 킬 수/시간만 있고 무엇을 얼마나 키웠는지가 안 보였다.

**⑤ 건물을 설치할 방법이 사실상 없었다 (I-36)** — `BuildingManager` 는 마우스 배치 모드였는데
`SelectBuildingForPlacement()` 의 **호출자가 0개**라 `_selectedBuildingData` 가 늘 null 이었다.
**건물 아이템을 먹어도 아무 일도 일어나지 않았다.**

**⑥ 건물이 전부 "때리는 것"뿐이었다 (I-37)** — `Turret` / `Bombard` / `House` 3종인데
`House` 는 스크립트가 없어 세워도 아무것도 안 했다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Player/PlayerStats.cs` | **`AddOrUpgradePassive(PassiveData, int)` 신규.** 같은 `Data` 가 있으면 `UpgradeTo(level)` 만 하고, 없을 때만 새로 추가한다. 함정이던 `AddPassive(PassiveEffect)` 는 **삭제** (I-32) |
| `LevelUp/LevelUpManager.cs` | `ApplyPassive` 가 `AddOrUpgradePassive` 를 부르게 수정 (I-32) · `PickCandidates` 를 **가중치 비복원 추첨**으로 교체 — 보유 아이템 가중치 `OwnedWeight = 2`, 신규는 1 (I-34) |
| `Player/PlayerController.cs` | `Z` 키(`kb.zKey.wasPressedThisFrame`) → `BuildingManager.PlaceNext(transform.position)` (I-36) · `BuildingManager` 참조를 `Awake` 캐시 → **첫 사용 시점 지연 조회**로 (I-38) |
| `Building/BuildingManager.cs` | 마우스 배치 모드 제거 → **FIFO 설치 대기열**. `UnlockBuilding` 이 `MaxCount` 만큼 대기열에 쌓고, `PlaceNext` 가 맨 앞(=가장 먼저 얻은 것)을 꺼낸다. `TryFindSpot` 은 플레이어 중심 동심원 3링 × 8방향을 훑는다 (I-36) |
| `Building/BuildingBase.cs` | `OnCooldownElapsed` 훅 분리(비전투 건물용) · `CooldownActive` 훅(식당이 타이머를 얼릴 때) · `Cooldown` 에 `Final.BuildingCooldown` 배율 반영 (I-37) |
| `Building/VillageBuilding.cs` | **신규** — 쿨다운마다 `ExperienceManager.CollectXp(Output)` |
| `Building/FarmBuilding.cs` | **신규** — 쿨다운마다 `MetaProgression.AddCurrency(Output × GoldGain)` |
| `Building/RestaurantBuilding.cs` | **신규** — 힐템을 떨구고 `CooldownActive => _pending == null` 로 **회수 전까지 타이머를 얼린다** |
| `Building/HealPickup.cs` | **신규** — 경험치와 달리 **끌려오지 않는다.** 직접 밟아야 회복되고, 회수 콜백으로 식당의 잠금을 푼다 |
| `UI/StageClearUI.cs` | 보유 아이템을 아이콘 + `"{ItemName} Lv.{CurrentLevel}"` 로 나열 (I-35) |
| `Game/ICON/Exp_Orb.gif.meta` | `spritePixelsToUnits` → **460** (I-33) |
| `Balance/Buildings.csv` | 3종 → **5종**(Turret/Bombard/Village/Farm/Restaurant). **`Output` 열 신규** (쿨다운 1회당 산출량) |
| `Balance/Passives.csv` | **`BonusBuildingCooldown` 열 신규.** `BuildingCooldown` 행 = `-0.1|-0.18|-0.26|-0.34|-0.42` |
| `Balance/Items.csv` | `House` 제거, `Village`/`Farm`/`Restaurant`/`BuildingCooldown` 추가 → **20종** |
| `Balance/SceneWiring.csv` | `LevelUpManager,allItems` 20개로 갱신 · `BuildingManager,placementBlockLayer = Building` |
| 프리팹 / 스프라이트 | `Building_Village` / `Building_Farm` / `Building_Restaurant` / `HealPickup` 프리팹, `Farm.png` / `Restaurant.png` 스프라이트 신규 (Village 는 기존 `House.png` 재사용) |

건물 물리 (사용자 요청: "너무 쉽게 밀리지만 않는 느낌"):
`Rigidbody2D` = **Dynamic · mass 5 · linearDamping 10 · gravityScale 0 · freezeRotation**,
`CircleCollider2D`(non-trigger), layer `Building`.

### 검증 로그

```
[패시브] BuildingCooldown 을 Lv1→Lv5 로 다섯 번 획득
  Lv1 → Final.BuildingCooldown = 0.9     (기대 1-0.10)
  Lv2 → 0.82                             (기대 1-0.18)
  Lv3 → 0.74                             (기대 1-0.26)
  Lv4 → 0.66                             (기대 1-0.34)
  Lv5 → 0.58                             (기대 1-0.42)   ← 수정 전에는 -0.30
  Damage 는 이 사이 내내 1 로 고정 (다른 패시브에 새지 않음)

[식당] Restaurant Lv1(60s) × 0.58 = 34.8s 기대
  배치 t=24.1 → t=60.4 (경과 36.3s) heals=1
  플레이어를 힐템 위로 이동해 회수 t=78.7
    t=106.6 (경과 27.9s) heals=0     ← 수정 전(6초)이면 이미 1
    t=125.4 (경과 46.8s) heals=1
  → 34.8초 근처에서 정확히 한 번만 재소환. 회수 전에는 안 나옴

[Z 설치] place0..4 = True, place5 = False (MaxCount 초과), 순서 = 획득 순서
         실제 키 입력 주입(InputSystem.QueueStateEvent(Key.Z)) → pending 1→0,
         Building_Turret(Clone) 생성 확인                             (I-38 수정 후)
[마을]   VillageBuilding.OnCooldownElapsed → CollectXp → TriggerLevelUp
[농장]   25초에 currency 97 → 99 (쿨 12s × Output 1 = 정확히 2틱)
[타워/포대] 플레이어 무기를 전부 제거(weapons=0)한 상태에서 kills 7 → 14
[웨이브]  웨이브 중 배치 건물 5개 전부 유지 (ObjectPool 자식, act=True)
[물리]   Building_Restaurant rb=Dynamic mass:5 drag:10 grav:0 freezeRot:True layer=Building
[컴파일] 에러 0 · 경고 0
```

> **헛짚었던 것**: "웨이브 중에 건물이 사라진다"고 의심해 `OnDisable`/`OnDestroy` 에
> 스택 트레이스 로그를 심었는데, 트레이스에 **엔진 프레임만** 있었고 콘솔에
> `PauseMenuUI.Update → Open` (ESC) → `PauseMenuUI.QuitGame` 이 찍혀 있었다.
> **사람이 Game 뷰에서 ESC 를 누르고 Quit 을 눌러 플레이 모드가 끝난 것**이지 버그가 아니었다.
> 진단용 로그는 확인 후 전부 제거했다. → 테스트 중에는 Game 뷰를 만지지 말 것.

---

## 2-13. ✅ 건물 확대 + 곡사포 폭탄 투사체 + 폭발 애니메이션 (I-39~I-41, 2026-08-27 11차)

사용자 요청 4건: 건물 크기 확대 / 곡사포가 폭탄을 **날려서** 터뜨리게 / 터지는 애니메이션 / 폭발 잔상 버그.

### 원인

**① 폭발이 맵에 영구히 쌓였다 (I-39) — 사용자가 말한 "폭발 잔상"**

`BombardBuilding.Attack` 이 두 가지를 잘못하고 있었다.

```csharp
Physics2D.OverlapCircleAll(...)          // 날아가는 시간 없이 그 자리에서 즉시 피해
Instantiate(aoeEffectPrefab, ...)        // 풀을 안 거치고 새로 만들고, 아무도 안 지운다
```

`Proj_Aoe(Boom)` 를 풀로 돌려보내는 코드는 `AoeProjectile.Explode()` 코루틴 안에 있는데,
그 코루틴은 **`Initialize()` 가 시작시킨다.** `Instantiate` 만 하면 `Initialize` 를 안 부르므로
코루틴이 영영 시작되지 않는다 → 폭발 스프라이트가 **런이 끝날 때까지 그 자리에 남는다.**
곡사포가 3초마다 쏘므로 웨이브 하나에 수십 장이 겹쳐 쌓였다.

**② 폭발 스프라이트가 회색 네모였다 (I-40)**

`Assets/Game/ICON/폭발 애니메이션 시퀀스.png` 는 6×5=30컷 몽타주인데
**배경이 불투명하게 구워져 있었고**, 슬라이스도 4장만 엉뚱한 좌표에 잡혀 있었다.
그중 한 장(100×100)이 프리팹에 물려 있어서 화면에는 회색 사각형이 떴다.

**③ AI 가 만든 새 시트도 투명이 아니었다 (I-41) — 함정**

`GenerateSpritesheet` 결과물은 겉보기엔 투명해 보이는데 **알파 채널이 전부 255** 였다.
모델이 "투명"을 표현하려고 **체커보드 무늬를 RGB 에 그대로 그려 넣은** 것이다.
게임에 넣으면 폭발 뒤에 흰/회색 체커 사각형이 그대로 보인다.

> 확인 방법: `PIL.Image.getchannel('A').getextrema()` 가 `(255, 255)` 면 가짜 투명이다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Building/BombProjectile.cs` | **신규** — 목표 지점까지 포물선으로 날아가고(`arcHeight 1.2`, `spinSpeed 540`), 도착하면 폭발을 풀에서 꺼내 `Initialize` 하고 자기도 풀로 돌아간다. 목표를 `Transform` 이 아니라 **좌표로 굳혀서** 받는다 — 비행 중 적이 죽으면 그 `Transform` 은 풀에서 재사용돼 엉뚱한 자리로 옮겨간다 |
| `Building/BombardBuilding.cs` | 즉시 `OverlapCircleAll` + `Instantiate` → **`Pool.Get(bombPrefab)` + `BombProjectile.Initialize`** (I-39). `bombSpeed` / `bombPrefab` 필드 신규 |
| `Weapon/AoeProjectile.cs` | 스프라이트 프레임 재생 추가(`frames` / `frameRate` / `fadeOutPortion`) · **피해를 연출 전에 먼저** 넣는다(터진 뒤 0.15초 사이에 폭심을 빠져나간 적이 살아남던 문제) · `spriteRadiusAtScaleOne` 으로 **실제 폭발 반경에 맞춰 자동 확대** |
| `Game/Sprites/Effects/Explosion.png` | **신규** — Unity AI `GenerateSpritesheet`(`video-seedance-1-pro`) 1024×1024 / 4×4 / 16프레임. ppu 256 · Point 필터 · 무압축 |
| `Prefabs/Proj_Bomb.prefab` | **신규** — 기존 `Sprites/Weapons/Bomb.png`(투명 배경 픽셀아트) 재사용, scale 0.225 → 0.45유닛 |
| `Prefabs/Proj_Aoe(Boom).prefab` | 스프라이트를 새 시트 `frame_0` 으로 교체, `sortingOrder 20`, `frames` 16장 배선 |
| `Prefabs/Building_*.prefab` (5종) | `m_LocalScale` **1.8 → 2.6** (스프라이트 0.5유닛 × 2.6 = **1.3유닛**, 플레이어 1.0유닛) |
| `Balance/Economy.csv` | `BuildingManager,placeDistance` **1.2 → 1.8**, `placeClearRadius` **0.5 → 0.6** (건물이 커져 이웃 칸이 겹쳤다) |

**가짜 투명 제거 방법 (I-41)** — 배경만 골라 알파를 파야 한다.

1. 후보 = 밝고(`max>150`) 채도 낮은(`max-min<40`) 픽셀 → 체커 두 색 + **흰 폭심**이 함께 잡힌다
2. 씨앗 = 어두운 체커 회색(`~184,184,181`) 픽셀. **폭발 색에는 이 회색이 없다**
3. 씨앗이 속한 덩어리만 flood fill 로 배경 판정 → 연기 사이에 **갇힌 체커 조각**까지 지워지고
   흰 폭심은 (184 회색과 이어져 있지 않아) 살아남는다

> 테두리에서만 flood fill 하면 연기 사이 갇힌 조각이 흰 얼룩으로 남는다. 실제로 1차 시도에서 남았다.

### 수치

| 항목 | 값 | 근거 |
|---|---|---|
| 건물 크기 | 1.3유닛 (scale 2.6) | 플레이어 1.0유닛보다 확실히 크되 화면을 안 막는 선 |
| 건물 콜라이더 실반경 | 0.578 (지름 1.156) | `m_Radius 0.22222224 × 2.6` |
| `placeDistance` | 1.8 | 링 위 이웃 간격 `0.765 × 1.8 = 1.38` > 건물 지름 1.156 |
| `placeClearRadius` | 0.6 | 실반경 0.578 보다 약간 크게 |
| 폭탄 속도 | 6 유닛/초 | 곡사포 사거리 3.5 를 약 0.6초에 — 날아가는 게 보이되 답답하지 않다 |
| 폭발 프레임 | 16장 @ 24fps = **0.667초** | 뒤 40% 는 알파 페이드 (마지막 프레임이 연기 덩어리라 그냥 끄면 툭 사라진다) |
| `spriteRadiusAtScaleOne` | 0.4 | 프레임 1.0유닛 중 불덩이가 덮는 반경. 반경 2 → scale 5 |

### 검증 로그

```
[크기]  Turret/Bombard/Village/Farm/Restaurant scale=2.60 월드=1.30 콜라이더반경=0.578
        Player 월드=1.00
        placeDistance=1.8  placeClearRadius=0.6      (Economy.csv 31/31 적용)

[폭탄]  Pool.Get(Proj_Bomb) → Initialize((-3,0)→(3,0), speed 6)
        1초 뒤 → Proj_Bomb active=False  pos=(3.00, 0.00)     ← 정확히 목표에 도달 후 회수
                 Explosion active=False  pos=(3.00, 0.00) scale=5.00  frame=frame_15
                                                              ← 반경 2 ÷ 0.4 = 5 ✓

[곡사포] Building_Bombard Lv1 (사거리 3.5 / 쿨 3s / 피해 20) + 적 배치
        4초 뒤 → Proj_Bomb 활성=0 / Explosion 활성=1 (재생 중)
                 풀 밖 폭발 오브젝트(잔상) = 0            ← I-39 수정 확인
        적이 실제로 AoeProjectile.Explode 에서 피해를 받고 Die() 진입

[투명]  Explosion.png alpha min/max: 생성 직후 (255,255) → 처리 후 (0,255)
        프레임별 불투명 비율 46→67→54→32% (팽창 후 소멸과 일치, 잔여 체커 없음)
        게임 카메라 캡처: 배경 완전 투명, 흰 얼룩 0

[컴파일] 에러 0 · 경고 0
```

> **헛짚었던 것**: 캡처에 흰 원이 찍혀서 시트 잔여물인 줄 알았는데,
> 씬의 `SpriteRenderer` 를 훑어 보니 **메인메뉴라 직업 스프라이트가 아직 안 붙은 플레이어**
> (기본 `Circle` 스프라이트)였다.
>
> 테스트로 `Enemy_Goblin` 을 raw `Instantiate` 했더니 `EnemyBase.Die()` 에서
> `NullReferenceException` 이 났다. `Rb` 는 `Awake` 가 아니라 **`Initialize()` 에서** 잡히므로
> 정상 스폰 경로에서는 안 나는, 테스트가 만든 예외였다.

---

## 2-14. ✅ 폭발 반경 확정 + 미사용 애셋 정리 (I-42, 2026-08-27 11차 후속)

### 원인 — 그림을 정직하게 만들자 숨어 있던 불일치가 드러났다 (I-42)

2-13 에서 폭발 그림이 **실제 피해 반경에 맞춰 확대**되도록 바꿨다
(`AoeProjectile` 이 `scale = 반경 ÷ spriteRadiusAtScaleOne`).
그전까지는 반경과 무관하게 항상 scale 1(0.5유닛)이라 **피해 범위와 그림이 따로 놀았다.**

그 결과 `Weapon_Aoe.prefab` 의 `explosionRadius = 3` 이 화면에 그대로 드러났다 —
**지름 6유닛**, 화면(약 17×10유닛)의 3분의 1이다. 곡사포(반경 2)보다 큰 폭발을
Lv1 무기가 매 발 쏘고 있었던 셈이다.

사용자 결정: **(A) 반경을 낮춘다** → 곡사포와 같은 **2**.

> 면적이 `π3² → π2²` 로 **56% 줄었다.** 단일 대상 DPS 는 그대로지만
> 다수 적 상황의 실효 DPS 는 눈에 띄게 떨어진다. 특히 Fireball 로 시작하는 **Mage 가 약해졌다.**
> 보정이 필요하면 `Weapons.csv` 의 `Damage` 로 → [`TODO.md`](TODO.md) §1

### 미사용 애셋 전수 조사

`.meta` 의 `guid` 를 `.prefab` / `.unity` / `.asset` / `.mat` / `.controller` 전체에서 grep 해
참조 수가 0인 것만 골랐다.

> ⚠️ **CSV 참조만 세면 안 된다.** `Bullet.png` 와 `goblin.png` 는 CSV 에는 안 나오지만
> `Proj_Bullet.prefab` / `Enemy_Goblin.prefab` 의 인스펙터에 직접 물려 있다.
> TODO 에 "자리표시 아이콘 6장"으로 묶여 있던 것이 실제로는 **4장**이었다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Weapon/AoeWeapon.cs` | `explosionRadius` 기본값 `3f` → `2f` + 이유 주석 |
| `Assets/Prefabs/Weapon_Aoe.prefab` | `explosionRadius: 3` → `2` |
| `Assets/Prefabs/Building_Turret.prefab` | 미사용 `ObjectPool` 컴포넌트 제거 (씬의 공용 풀을 쓴다) |
| `Assets/Scripts/Meta/MetaProgressionManager.cs` | `SaveProgress()` 제거 — `Save()` 의 별칭, 호출자 0 |
| `Assets/Game/ICON/house·speed·sword·turret.png` | **삭제** (+ `.meta`) — 참조 0 |
| `Assets/Game/ICON/폭발 애니메이션 시퀀스.png` | **삭제** (+ `.meta`) — 불투명 배경 30컷 몽타주. I-40 에서 대체됨 |
| `Assets/Scripts/{Enemy/EnemySystem,Weapon/WeaponSystem,Passive/PassiveSystem}.cs` | **삭제** (+ `.meta`) — 내용이 주석뿐인 껍데기 |

### 검증 로그

```
[VERIFY] Weapon_Aoe.explosionRadius = 2
[VERIFY] Building_Turret components: Transform SpriteRenderer CircleCollider2D TurretBuilding Rigidbody2D
         (ObjectPool 사라짐 · <MISSING> 없음)
[VERIFY] house/speed/sword/turret.png       => 삭제됨
[VERIFY] EnemySystem/WeaponSystem/PassiveSystem.cs => 삭제됨
[VERIFY] ICON 폴더 텍스처 수 = 3 (Bullet · goblin · Exp_Orb — 전부 프리팹이 물고 있음)

[컴파일] 에러 0 · 경고 0 (Assets/Refresh 후 콘솔 로그 0건)
```

> **남긴 것과 이유**: `Assets/Fonts` 는 실제로 쓰는 게
> `public/static/alternative/Pretendard-Regular.ttf` 한 장뿐이지만,
> 나머지 굵기는 나중에 UI 강조용으로 쓸 수 있고 `LICENSE.txt` 는 재배포 조건이라 보류했다.
> `Assets/_Recovery/0.unity`(512KB) 는 크래시 복구본으로 보여 **백업 가치를 확인하기 전엔 손대지 않았다.**
> → [`TODO.md`](TODO.md) §5

---

## 2-15. ✅ GitHub 연동 (2026-08-27 11차 후속)

이때까지 이 프로젝트는 **버전 관리가 전혀 없었다.** 되돌릴 방법이 없어서
애셋을 지울 때마다 사용자에게 확인을 받아야 했다.

**저장소: [`popzap/VS_LIKE`](https://github.com/popzap/VS_LIKE) (Private) · 기본 브랜치 `main`**

### 커밋 범위를 정한 근거

**프로젝트 폴더는 실측 3.1GB 였다.** 그대로 올리면 **되돌릴 수 없는 히스토리**가 되므로
첫 커밋 전에 무엇을 뺄지부터 정했다.

```
3.1G  전체
├ 2.8G  Library/          ← 에디터 캐시. 이것만으로 GitHub 권장치(1GB)를 3배 넘긴다
├ 147M  Assets/
├ 100M  GeneratedAssets/
├  21M  Logs/
└ 9.9M  Temp/
```

| 뺀 것 | 용량 | 이유 |
|---|---|---|
| `Library/` | **2.8GB** | 에디터가 임포트 결과를 캐시하는 곳. 지워도 다시 만들어진다 |
| `Temp/` `Logs/` `UserSettings/` | 31MB | 실행 중 산출물 · 개인 에디터 설정 |
| `*.csproj` `*.slnx` | — | IDE 가 재생성한다 |
| `GeneratedAssets/` | **약 100MB** | Unity AI 생성 캐시. `Assets/` 밖이고 게임 동작에 필요 없다. **결과물은 `Assets/Game/Sprites` 에 따로 들어가 있다** |
| `Assets/Fonts/web/` | **24MB** | woff/woff2/css — **Unity 가 아예 못 읽는다.** 지워도 잃을 게 없다 (삭제) |
| `Assets/Game/ClassData 1`·`2` | 0 | 빈 폴더. Id 변경 때 생긴 껍데기 (삭제) |

**결과: 677 파일 / 압축 83MB.**

> `Assets/Fonts/public/**` 의 안 쓰는 굵기 18개(약 41MB)는 **남겼다.**
> Bold·SemiBold 는 나중에 UI 강조용으로 쓸 수 있고, 지우면 다시 받아야 한다.
> → [`TODO.md`](TODO.md) §5

### 변경한 파일

| 파일 | 내용 |
|---|---|
| `.gitignore` | 위 표 + IDE·빌드·OS 부산물 |
| `.gitattributes` | `.unity`/`.prefab`/`.asset` 을 **텍스트(YAML)** 로, `.png`/`.ttf` 를 **binary** 로 못박음 |
| `CLAUDE.md` | **§5 버전 관리** 신설 — 커밋 단위·메시지 형식·금지 사항 |

> ⚠️ **`.gitattributes` 가 핵심이다.** Unity 의 씬/프리팹은 YAML 이라 텍스트로 잡아야
> diff 가 보이고 병합이 된다. 반대로 `.png`/`.ttf` 를 binary 로 못박지 않으면
> **줄바꿈 정규화가 파일을 조용히 망가뜨린다.**

### 앞으로

**한 작업(`n차` 한 절) = 커밋 하나.** 코드·애셋·문서를 같이 담는다.
커밋은 작업 끝에 자동으로, **푸시는 물어보고** 한다 ([`CLAUDE.md`](../CLAUDE.md) §5).

> 이제 애셋 삭제가 되돌릴 수 있는 일이 됐다. 다만 **히스토리에는 영구히 남으므로
> 큰 바이너리를 넣었다 지우는 건 여전히 피해야 한다.**

---

## 2-16. ✅ 장르 완성도 갭 분석 — `ROADMAP.md` 신설 (2026-08-28 12차)

### 왜

"완전한 뱀서라이크가 되려면 뭐가 더 필요한가"를 **직접 코드·씬·애셋을 읽어** 정리했다.
`TODO.md` 는 *버그와 미검증*을 담는 문서라 "장르로서 비어 있는 칸"을 넣기에 맞지 않았다.
그래서 네 번째 문서를 만들고 역할을 갈랐다.

### 결론 (요지)

**뼈대는 다 섰고 살이 없다.** 런 루프는 전 구간 동작하지만
**오디오 파일이 0개**, 적 6종이 **행동은 1종**, HUD 에 **타이머·킬·골드가 없다.**

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Docs/ROADMAP.md` | **신규.** 갭 분석 + 5단계 권장 순서 |
| `CLAUDE.md` §1 | 문서 표에 `ROADMAP.md` 역할·갱신 시점 추가 |
| `Docs/TODO.md` | §0 에 오디오/완성도 행 추가, 헤더에 역할 분리 명시, 적 AI·적 6종 항목에 원인·해법 보강, 해결된 `Building_Turret` 행 제거 |

### 조사로 **기각한** 회귀 의심 3건

사전 조사에서 아래 주장이 나왔으나 **전부 사실이 아니었다.** 문서가 맞다. 고친 것 없음.

| 주장 | 실제 |
|---|---|
| CanvasScaler 가 Constant Pixel Size 800×600 으로 회귀 (I-4 깨짐) | ❌ 씬 오버라이드에 `m_UiScaleMode: 1`(ScaleWithScreenSize) · `1920×1080` · match 0.5. **정상** |
| `HUDManager` 직렬화 필드가 **전부** 미할당 → HUD 무력화 | ❌ `UI Canvas.prefab` 에서는 `{fileID: 0}` 이지만 **씬 오버라이드로 10개가 배선**돼 있다. 빈 것은 표정 4장 + 레벨업 연출 2개뿐 — `TODO.md` §4 기재와 일치 |
| 무기가 `Assets/Resources/Data/Weapons/` 의 Staff·Pistol·Flamethrower | ❌ `Assets/Resources/` **폴더 자체가 없다.** 무기는 `Assets/Game/WeaponData/` 의 Sword·Bow·Gun·Fireball·Bomb 5종 |

> **교훈:** 프리팹의 `{fileID: 0}` 만 보고 "미할당"이라 단정하면 안 된다.
> 씬의 `m_Modifications` 오버라이드를 같이 봐야 실제 배선을 알 수 있다.

### 검증 로그

- `find Assets -iname "*.wav" -o -iname "*.mp3" -o -iname "*.ogg"` → **0건**
- `grep -rn "Evolv" Assets/Scripts` → **0건** (무기 진화 없음)
- `grep -rn "ParticleSystem" Assets/Scripts` → **0건**
- `grep -rn "\.Shake(" Assets/Scripts` → **1건** (`PlayerController.cs:128`, 플레이어 피격 전용)
- `EnemyBase.OnDeath()` (`:152`) → `{ }` 빈 함수
- `EnemyVisual.Flash()` **존재** — 적 피격 플래시는 I-26 으로 이미 완료. 초안에 "없음"으로 적었다가 정정
- `GameState` 실제 항목 10개: MainMenu · StageMap · Wave · LevelUp · Shop · Event · Paused · GameOver · Victory · MetaScreen

---

## 2-17. ✅ ROADMAP 1단계 — HUD 정보 3종 · 타격 반응 · 오디오 배관 (I-43~I-45, 2026-08-28 13차)

[`ROADMAP.md`](ROADMAP.md) §8 의 1단계("**한 판이 재미있어지는 최소치**")를 통째로 실행했다.
셋 다 "없어서 안 보이던 것"이라 눈에 띄는 변화가 크다.

### 원인

**I-43 — 플레이어가 자기 상황을 볼 수 없었다**
HUD 에 HP 와 경험치뿐이었다. **언제 끝나는지 · 몇 마리 잡았는지 · 돈이 얼마인지**가 전부 화면 밖.
`WaveManager` 는 남은 시간을 `TimerRoutine` 의 **지역 변수**로만 들고 있어 밖에서 읽을 방법조차 없었다.
게다가 `HUD/CurrencyText` 는 **씬에 존재하는데 아무 스크립트도 굴리지 않는 죽은 UI** 였다.

**I-44 — 때려도 맞은 티가 안 났다**
적은 피격 시 흰색으로 번쩍일 뿐(I-26) **밀리지 않고**, 죽을 때는 그 자리에서 **한 프레임에 사라졌다**
(`OnDeath()` 가 빈 함수 `{ }`, `ForceDespawn()` 직행). 화면 흔들기는 `Shake()` API 가 있는데
호출자가 `PlayerController.cs:128`(플레이어 **피격**) **하나뿐**이었다 — 적을 죽일 때는 아무 반응도 없었다.

**I-45 — SFX 볼륨을 0 으로 내리면 BGM 도 같이 꺼졌다**
`AudioManager.SetSFXVolume()` 이 `AudioListener.volume`(**전역 마스터**)을 건드렸다.
BGM 도 그 마스터를 통과하므로 최종 음량이 `bgmSource.volume × SfxVolume` 이 되어,
SFX 를 끄면 BGM 이 같이 죽었다. 애초에 **효과음을 재생하는 함수 자체가 없었다** (`PlaySfx` 0건).

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Wave/WaveManager.cs` | `WaveRemainingTime` / `IsWaveActive` 프로퍼티 노출. 킬 클리어 웨이브는 `-1` 로 두어 HUD 가 타이머를 숨기게 함 |
| `UI/HUDManager.cs` | `timerText`/`killText`/`currencyText` 필드 + `RefreshWaveInfo()`. 초·값이 **바뀔 때만** 문자열 생성 |
| `Core/GameManager.cs` | `using System.Collections` + `DoHitstop(float)` / `HitstopRoutine` |
| `Enemy/EnemyBase.cs` | `TakeDamage(float, Vector2? from)` 넉백 · `DeathPopRoutine` 사망 연출 · `PlayDeathImpact()` 흔들기+히트스톱 · `Initialize` 에 풀 재사용 초기화 |
| `Weapon/ProjectileBase.cs` · `Weapon/AoeProjectile.cs` · `Building/BuildingBase.cs` | 넉백 기준점(`transform.position`) 전달 |
| `UI/AudioManager.cs` | **전면 재작성.** 볼륨 계통 분리 · `PlaySfx`/`PlayBgm`/`StopBgm` · 보이스 풀 16 · 중복 컷 |
| `Assets/Scenes/SampleScene.unity` | `TimerText`/`KillText` 신규 + `CurrencyText` 재배치, HUDManager 3필드 배선 |

### 함정 세 개

**1. HUD 는 `WaveManager` 를 구독하지 않고 폴링한다.**
`OnTimerUpdated` 이벤트가 이미 있지만 쓰지 않았다. `GameManager.Instance.WaveManager` 는
`GameManager.Start()` 에서 채워지는데 **모든 `Awake` 가 모든 `Start` 보다 먼저** 돌고
`Start` 끼리의 순서는 보장되지 않는다. `Start` 에서 구독하면 조용히 null 을 잡고 **기능만 죽는다**
— I-8 · I-38 과 정확히 같은 함정이다. 매 프레임 폴링 + 변경 감지가 안전하고 비용도 사실상 0.

**2. 넉백은 `MoveTowardsPlayer()` 보다 먼저 `return` 해야 한다.**
`MoveTowardsPlayer()` 가 `Rb.linearVelocity` 를 **통째로 덮어쓴다.** 넉백 속도를 넣어도
다음 물리 프레임에 즉시 지워진다. `FixedUpdate` 맨 앞에서 `_knockbackTimer` 를 보고 빠져나가야 한다.

**3. 히트스톱이 `Time.timeScale` 을 함부로 `1f` 로 되돌리면 일시정지가 저절로 풀린다.**
이 프로젝트는 `WaveManager.PauseWave()`(0) · 일시정지 · 레벨업 · 스테이지 결과창이 전부
같은 `Time.timeScale` 을 공유한다. 그래서 3중 가드를 걸었다 —
(1) `CurrentState == Wave` 일 때만 시작 (2) 이미 `1f` 가 아니면 **손대지 않음**
(3) 복구 시점에 **다시** `Wave` 인지 확인. 대기는 `WaitForSecondsRealtime`.

### ROADMAP 과 다르게 간 것 두 가지

| 항목 | ROADMAP | 실제 | 이유 |
|---|---|---|---|
| F-5 처치 흔들림 | "적 처치 시 미세 `Shake()`" | **엘리트/보스만** | 잡몹은 초당 수십 마리가 죽는다. 매번 흔들면 타격감이 아니라 멀미다 |
| A-1 AudioMixer | Master/BGM/SFX 3그룹 믹서 | **믹서 없이 계통 분리** | ① 오디오 파일이 0개라 라우팅할 소리가 없다 ② 실제 버그는 믹서 없이 완전히 해결된다 ③ `.mixer` 를 스크립트로 만들려면 `AudioMixerController`(에디터 내부 API)에 `System.Reflection` 이 필요한데 **금지 사항**이고, YAML 수기 작성은 깨질 위험이 크다 |

> 믹서가 실제로 필요한 시점은 **덕킹·필터 같은 DSP** 를 넣을 때다.
> 그때 사람이 에디터에서 믹서를 만들고 `AudioManager` 에 `AudioMixerGroup` 필드만 꽂으면 되도록
> 구조는 열어 뒀다. 재개 조건은 [`TODO.md`](TODO.md) 에 적었다.

### 사망 연출을 파티클로 안 한 이유

`ParticleSystem` 사용처가 프로젝트 전체에 **0건**이고 파티클 애셋도 없다.
대신 **스케일 팝**(1 → 1.25 → 0, 0.14초)으로 대체했다. 이게 안전한 이유:
`EnemyVisual` 은 셰이더 **정점**을 흔들 뿐 `localScale` 을 건드리지 않고,
`OnInitialized()` 가 풀 재사용 때마다 `localScale` 을 다시 세팅하므로 잔재가 남지 않는다.
연출 중에는 `Collider2D` 를 전부 꺼서 **시체에 부딪혀 피해를 입는 일**을 막았다.

### 수치

| 항목 | 값 | 근거 |
|---|---|---|
| 넉백 세기 / 지속 | `6.0` / `0.10s` | 잡몹 이동속도(1.6)의 약 4배. 확실히 밀리되 대열이 무너지진 않는 선 |
| 넉백 저항 | 잡몹 `1.0` · 엘리트 `0.4` · 보스 `0.0` | 밀리는 보스는 위압감이 없고, **벽 없는 아레나**라 무한히 밀려나 도망가 버린다 |
| 사망 팝 | `0.14s` (앞 30% 팽창 → 70% 수축) | 더 길면 시체가 쌓여 보이고, 더 짧으면 안 보인다 |
| 히트스톱 | 엘리트 `0.05s` · 보스 `0.09s` | 0.1s 를 넘기면 "멈췄다"가 아니라 "렉"으로 느껴진다 |
| 처치 흔들기 | 엘리트 `(0.20, 0.25)` · 보스 `(0.45, 0.50)` | 플레이어 피격(`shakeMagnitude`)보다 약하게 — 내가 맞은 게 더 중요하다 |
| 타이머 경고색 | `≤ 10초` 부터 빨강 | |
| SFX 보이스 | `16` | |
| 중복 컷 창 | `0.04s` | 60fps 기준 약 2.4프레임. 같은 클립이 겹치면 위상이 뭉개져 "찢어지는" 소리가 난다 |

### 검증 로그

**HUD (임시 `HudSmokeTest`)**

```
[HUDTEST] state=Wave waveActive=True remain=86.0 kills=0 gold=164
[HUDTEST] TimerText='1:26' KillText='0 Kills' CurrencyText='164 G'
```

레이아웃 겹침 없음 수치 확인: `CurrencyText` x[-250,-70] vs `OptionsButton` x[-52,4].
`Unity_Camera_Capture` 는 **씬 뷰**를 찍으므로 Screen Space Overlay 캔버스가 안 보인다 → 로그로 검증.

**게임 필 (임시 `FeelTest`)**

```
[FEELTEST] F-2 before=1.60 after=6.00 dirX=-6.00   ← 오른쪽에서 때려 왼쪽으로 밀림
[FEELTEST] F-2 0.3s 후 vel=1.60                    ← 추적 속도로 복귀
[FEELTEST] F-4 scale(전)=1 → (중)=0 → (후)=1        ← 히트스톱 진입·복구
[FEELTEST] F-3 0.05s scale=0.95 active=True        ← 팝 애니메이션 진행 중
[FEELTEST] F-3 0.20s active=False                  ← 풀 반환 완료
```

**오디오 (임시 `AudioTest`, 절차 생성 사인파 사용)**

```
[AUDIOTEST] A-3 보이스 풀 = 16개, AudioListener.volume=1
[AUDIOTEST] A-4 같은 프레임 5회 요청 → 울리는 보이스 = 1개
[AUDIOTEST] A-4 0.1s 뒤 재요청 → 울리는 보이스 = 2개
[AUDIOTEST] A-1 SFX=0 일 때 BgmVolume=0.8 bgmSource.volume=0.8 listener=1   ← BGM 생존
[AUDIOTEST] A-1 BGM=0 일 때 SfxVolume=1 bgmSource.volume=0                  ← SFX 생존
[AUDIOTEST] A-3 서로 다른 클립 30회 → 울리는 보이스 = 16개                   ← 상한 동작
```

임시 스크립트 3종(`HudSmokeTest` · `FeelTest` · `AudioTest`)과 씬 오브젝트 **전부 삭제**,
`Assets/Refresh` 후 **콘솔 0건**, `File/Save` 완료.

---

## 2-18. ✅ ROADMAP 3단계 — 병렬 소환 · 적 행동 분화 · 보물상자/자석 (I-46~I-48, 2026-08-28 14차)

[`ROADMAP.md`](ROADMAP.md) §8 의 3단계("**루프에 재미를 넣는다**") 세 항목을 통째로 실행했다.
2단계(오디오 파일 확보)는 **음원 생성 수단이 정해지지 않아** 사용자 지시로 건너뛰었다 —
ROADMAP §8 주석대로 2·3단계는 서로 의존하지 않는다.

### 원인

**I-46 — 웨이브에 적이 섞여 나오지 않고, 뒤쪽 적은 아예 안 나왔다**
`SpawnRoutine()` 이 `foreach (var entry in Spawns)` 로 **한 항목을 다 뿌린 뒤 다음으로** 넘어갔다.
그래서 `(마리수 × 간격)` 의 합이 `SurvivalTime` 을 넘으면 **뒤쪽 항목이 등장하지 못한 채 웨이브가 끝났다.**
`Normal3` 가 (36×0.8)+(24×0.9)+(8×1.5)+(40×0.6) = **86.4초**인데 생존 시간이 90초라
마지막 Goblin 40마리가 사실상 안 나왔다. 사용자가 말한 "몬스터가 전 종류 안 나온다"의 실제 원인이다.
데이터가 아니라 **소환기 설계** 문제였다.

동시에 **적 수 상한이 없었다.** 순차일 때는 한 번에 한 종류만 나와 터지지 않았지만,
병렬로 바꾸는 순간 여러 항목이 동시에 쏟아진다. 게다가 넉백(I-44)으로 밀려나거나
플레이어가 한 방향으로 계속 달리면 **적이 화면 밖에 줄줄이 남아** 상한만 잡아먹는다.

**I-47 — 적 6종이 전부 같은 행동을 했다**
`MoveTowardsPlayer()` 직선 추격 **하나**가 전부였다. Ogre 는 큰 고블린, Wolf 는 빠른 고블린이다.
게다가 적 콜라이더가 전부 **트리거**라 물리 반발이 없어 **다 겹쳐서 한 덩어리로 뭉쳤다.**

**I-48 — 필드 픽업이 2종뿐이었다**
`ExpDrop_Small` 과 `HealPickup`. 엘리트를 잡아도 보상이 경험치뿐이라 **처치의 무게가 없었고**,
화면 구석에 흩어진 경험치를 회수할 방법이 걸어가는 것밖에 없었다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Wave/WaveData.cs` | `WaveSpawnEntry` 에 `StartTime`/`EndTime` · `WaveData` 에 `MaxAlive`/`EliteTime`/`BossTime` |
| `Wave/WaveManager.cs` | **소환기 재작성.** 항목마다 코루틴 하나(`SpawnEntryRoutine`) = 병렬. `WaitForSpawnSlot` 상한 대기 · `MaintainRoutine`(0.25초 주기) · `PruneAlive` · `RecycleFarEnemies`. `_enemiesAlive`(int) → `_alive`(List) |
| `Enemy/EnemyData.cs` | `EnemyAI` enum(Chaser/Ranged/Charger) + 원거리 5필드 · 돌진 6필드 |
| `Enemy/EnemyBase.cs` | `FixedUpdate` 의 `switch (Data.AI)` · `TickRanged`/`FireProjectile` · `TickCharger`(`ChargeState`) · `Steer`/`GetSeparation` 무리 분리 · `Reposition(Vector2)` · `IsElite`/`IsBoss` 노출 · `SharedPool` 캐시 |
| `Enemy/EnemyProjectile.cs` | **신규.** 적탄. 콜라이더가 아니라 **거리**로 명중 판정 |
| `Pickup/WorldPickup.cs` | **신규.** 보물상자·자석 공용. `PickupKind` enum |
| `Experience/ExperienceManager.cs` | `SpawnChest`/`RollMagnetDrop`/`GrantChestReward` + `chestPrefab`/`magnetPrefab`/`magnetDropChance` |
| `Experience/ExpDrop.cs` | `PullAllToPlayer()` static + `_forcePull`/`magnetSpeed 22`. 미사용 `_pickupRadius` 제거 |
| `Balance/CsvTable.cs` | `CsvRow.Enum<T>` — 대소문자 무시. 오타는 경고 후 기본값 |
| `Editor/BalanceImporter.cs` | `Enemies.csv` 의 AI 12열 임포트/익스포트 |
| `Game/Balance/Enemies.csv` | AI 12열 신설. Wolf→Charger · Demon→Ranged |
| `Game/Balance/Waves.csv` | `Spawns` 에 `:시작-종료` 시간창 · `MaxAlive`/`EliteTime`/`BossTime` 열 · 6웨이브 전면 재구성 |
| `Game/Balance/SceneWiring.csv` | `ExperienceManager` 3행(상자·자석 프리팹, 드랍 확률) |
| `Prefabs/Proj_EnemyBolt.prefab` | **신규.** 적탄 (붉은 총알, sortingOrder 5, 콜라이더 없음) |
| `Prefabs/Pickup_Chest.prefab` · `Pickup_Magnet.prefab` | **신규.** `sortingOrder 1` (경험치 오브보다 위) |
| `Game/Sprites/Pickups/Chest.png` · `Magnet.png` | **신규.** Unity AI `gpt-image-1-5` |

### ROADMAP 과 다르게 간 것 네 가지

| 항목 | ROADMAP | 실제 | 이유 |
|---|---|---|---|
| 적 행동 구현 | `RangedEnemy : EnemyBase` **상속** | **`EnemyAI` enum (데이터 주도)** | 상속으로 나누려면 **행동마다 프리팹이 따로 있어야 한다** — 컴포넌트 타입은 런타임에 못 바꾼다. 적 6종이 `Enemy_Goblin.prefab` **하나**를 공유하고 CSV 로만 구분하는 구조라, 프리팹을 5개로 늘리는 비용이 더 크다. `MoveTowardsPlayer` 는 여전히 `virtual` 이라 나중에 갈라도 된다 |
| 무리 분리 | "**Goblin**" | **전 종류** | ROADMAP §6 은 겹침을 프로젝트 전체 문제로 적어 놨다. 한 종만 떼어 놓으면 나머지가 여전히 한 덩어리다 |
| 적탄 충돌 | (언급 없음) | **거리 판정** (`HitRadius 0.45`) | Projectile 레이어는 **플레이어를 때리라고 만든 게 아니다.** 레이어 충돌 행렬을 건드리면 기존 무기 판정까지 흔들린다. 화면에 몇 발 없는 적탄 쪽을 거리 검사로 처리하는 게 싸고 안전하다 |
| 보물상자 UI | "열면 무기 진화 / 다중 레벨업" | **레벨업 패널 재사용** | 무기 진화는 4단계(2-3) 미구현이고, 플레이어가 얻는 건 결국 같은 아이템 카드다. 화면이 하나 더 생기면 조작만 헷갈린다. **레벨은 오르지 않는다** — 경험치가 아니라 보상이다 |

### 함정 다섯 개

**1. 살아 있는 적을 `int` 카운터로 세면 안 된다.**
적이 사라지는 경로가 **둘**이다 — 처치(`OnEnemyKilled`)와 `ForceDespawn`(웨이브 종료).
한쪽에서만 빼면 수가 어긋나고, 상한이 걸린 순간 **소환이 영원히 막힌다.**
그래서 `List<EnemyBase>` 를 두고 `PruneAlive()` 가 "오브젝트가 실제로 켜져 있는가"를 매번 다시 본다.
게다가 I-44 의 사망 팝 연출 때문에 **죽은 시점과 꺼지는 시점이 다르다.**

**2. 멀어진 적은 지우면 안 된다.** 경험치·골드가 증발한다. `RecycleFarEnemies()` 가
소환 반경의 **1.9배**를 넘은 적을 **죽이지 않고** 플레이어 주변으로 옮긴다.
단 **보스는 제외** — 갑자기 등 뒤에 나타나면 반칙처럼 느껴진다.

**3. 엘리트/보스는 소환 상한을 무시해야 한다.** 잡몹이 상한을 채운 상태에서 보스가 대기하면
**킬 클리어 웨이브가 영영 끝나지 않는다.** `OverrideSpawnRoutine()` 은 `WaitForSpawnSlot` 을 안 탄다.

**4. 시간창은 `KillTarget` 을 깨뜨릴 수 있다.** 시간창이나 `MaxAlive` 때문에 소환 수가 줄면
보스 웨이브의 `KillTarget=19` 를 못 채운다. 그래서 보스 웨이브만 시간창 안에 마리수가
다 들어가도록 맞췄고(12×4=48<50, 6×8=48<53−5) 타이머 240초를 안전망으로 유지했다.

**5. 적 콜라이더는 트리거라 물리로 못 떼어 놓는다.**
`Physics2D.OverlapCircle` 이 트리거를 보게 하려면 `ContactFilter2D.useTriggers = true` 가 **필수**다.
분리는 물리 반발이 아니라 **조향(steering)** 으로 넣었고, 매 프레임 이웃 검색은 비싸므로
**4번째 물리 스텝마다** 개체별 랜덤 위상으로 흩어 돌린다. 버퍼는 `static Collider2D[12]` 재사용.

### 자석을 static 플래그로 만들지 않은 이유

`PullAllToPlayer()` 는 지속 시간이나 static 상태를 두지 않고 **그 순간 살아 있는 구슬에만**
`_forcePull` 표시를 남긴다. 자석은 "지금 화면에 있는 걸 쓸어 담는" 물건이지
**잠시 흡수 반경이 넓어지는 버프가 아니고**, static 상태는 플레이 모드를 다시 켰을 때 남는 사고가 잦다.
풀 재사용 대비로 `Initialize()` 에서 `_forcePull = false` 를 명시한다.

`WorldPickup.Update()` 는 `CurrentState == LevelUp` 이면 즉시 `return` 한다 —
`timeScale = 0` 이어도 `Update` 는 계속 돌기 때문에, 막지 않으면 상자를 밟은 순간
**패널이 떠 있는 상태에서 또 열려** 상태가 꼬인다.

### 수치

| 항목 | 값 | 근거 |
|---|---|---|
| `MaxAlive` | Normal 60/80/110 · Elite 110/130 · Boss 90 | 웨이브가 뒤로 갈수록 올린다. 보스 웨이브는 보스에 집중하도록 오히려 낮춘다 |
| 재배치 거리 | 소환 반경 × **1.9** | 소환 반경(20) 바로 밖이면 정상적으로 다가오는 중일 수 있다. 38유닛이면 확실히 버려진 적 |
| 유지보수 주기 | `0.25s` | 매 프레임 돌 이유가 없다 |
| 분리 반경 / 가중치 | `0.85` × `localScale` / `0.9` | 반경은 적 몸통(0.5유닛)보다 약간 크게. 가중치가 1을 넘으면 추격보다 분리가 이겨 적이 안 다가온다 |
| 분리 갱신 | **4스텝마다** (개체별 위상) | 50Hz 기준 초당 12.5회. 이웃 검색이 가장 비싸다 |
| 이웃 버퍼 | `12` | 넘치면 잘리지만 12마리에게 밀리는 것으로 충분하다 |
| Demon `PreferredRange`/`AttackCooldown` | `7` / `2.5s` | 화면 절반. 무기 사거리(3.5~6) 밖에서 쏜다 |
| Demon 탄속/피해 | `6` / `12` | 접촉 피해(20)보다 낮게 — 원거리는 안전하니까 |
| 적탄 수명 | `PreferredRange × 1.6 ÷ 탄속` | 사거리의 1.6배까지 날아가고 사라진다. 자동 계산이라 CSV 열이 없다 |
| Wolf 돌진 | 예고 `0.45s` → 돌진 `0.4s` ×**3.0** 배속 → 경직 `0.6s`, 쿨 `3s` | 4.2×3×0.4 ≈ **5유닛** 돌진. 예고 0.45초는 피할 수 있는 최소치. ROADMAP 계획(3.5배)에서 낮춘 이유는 6유닛을 넘으면 사실상 회피 불가 |
| 자석 드랍 확률 | `0.006` | 웨이브당 200~300마리가 죽으므로 **1~2개** 나온다 |
| 자석 회수 속도 | `22` | 평소 흡수 속도(6)의 약 4배. 화면 끝에서 오는 것도 있다 |
| 상자/자석 터치 반경 | `0.75` / `0.65` | 경험치 오브(0.3)보다 넉넉하게 — 놓치면 짜증난다 |
| 상자 드랍 | 엘리트/보스 **확정** | 확률로 하면 "엘리트를 잡았다"는 사건이 흐려진다. 대신 엘리트는 **자석을 안 떨군다** (겹치면 뭘 먹었는지 모른다) |

### 검증 로그

**웨이브 병렬 소환 + 상한 + 재배치 (임시 `WaveTest`)**

```
[WAVETEST] t=  0s alive=  1 peak=  1 종류=Slime:1
[WAVETEST] t= 10s alive=  5 peak=  5 종류=Slime:5
[WAVETEST] t= 20s alive= 16 peak= 16 종류=Goblin:6 Slime:10
[WAVETEST] 재배치 전 거리=200 → 1초 후 거리=18 (소환 반경 20 근처로 돌아와야 정상)
[WAVETEST] t= 30s alive= 28 peak= 28 종류=Goblin:13 Slime:15
[WAVETEST] t= 40s alive= 40 peak= 40 종류=Goblin:20 Slime:20
[WAVETEST] t= 50s alive= 52 peak= 52 종류=Slime:25 Goblin:27
[WaveManager] Wave Cleared!
[WAVETEST] 웨이브 종료 t=60s peak=60 (MaxAlive 는 Normal1=60 / Normal2=80 / Normal3=110)
```

t=20s 부터 **두 종류가 동시에** 잡힌다 — 순차였다면 Slime 30마리(60초)를 다 뿌릴 때까지
Goblin 이 한 마리도 안 나왔다. peak 가 `MaxAlive`(60)에서 정확히 멈춘다.

**적 행동 분화 (임시 `AITest`)**

```
[AITEST] data goblin=Chaser demon=Ranged wolf=Charger
[AITEST] 분리 t=0.0s 평균간격=0.050
[AITEST] 분리 t=1.5s 평균간격=2.007  (커져야 정상)
[AITEST] 원거리 t=1s 거리=11.20  탄=0
[AITEST] 원거리 t=2s 거리= 8.40  탄=0
[AITEST] 원거리 t=3s 거리= 6.95  탄=1
[AITEST] 원거리 t=5s 거리= 7.00  탄=1
[AITEST] 원거리 t=8s 거리= 6.95  탄=1
[AITEST] 돌진 t=0.9s 속도= 4.20 (추격) → t=1.2s 0.00 (예고) → t=1.8s 12.60 (돌진) → t=2.1s 0.00 (경직) → t=2.7s 4.20
[AITEST] 돌진 최고속도=12.60, 정지프레임=20/70
```

한 점에 겹쳐 놓은 적들이 1.5초 만에 **평균 간격 0.05 → 2.01** 로 벌어졌다.
Demon 은 `PreferredRange`(7)에 **정확히 수렴**해 머문다. Wolf 는 4.2 → 0 → 12.6 → 0 → 4.2 로
**예고·돌진·경직 4상태**가 전부 관측됐다 (12.6 = 4.2 × 3.0).

**보물상자 / 자석 (임시 `PickupTest`)**

```
[PICKTEST] 자석 전   구슬=10 평균거리=16.12
[PICKTEST] 자석 1.0s 구슬=0 평균거리=0.00
[PICKTEST] 자석 3.0s 구슬=0 (0 이어야 정상)
[PICKTEST] 픽업 배치 구슬=6 픽업활성=True
[PICKTEST] 픽업 소멸=True 남은구슬=0 (둘 다 0/True 여야 정상)
[PICKTEST] 상자 소멸=True state=MainMenu→LevelUp timeScale=0 레벨=3→3
[PICKTEST] 패널 닫은 뒤 state=MainMenu timeScale=1
[PICKTEST] 완료
```

16유닛 밖의 구슬 10개가 **1초 만에 전부** 회수됐다. 상자는 `LevelUp` 상태로 전환하고
`timeScale` 을 0 으로 내리되 **레벨은 3→3 으로 그대로**다 (의도된 동작).
패널을 닫으면 상태와 `timeScale` 이 정상 복구된다.

> ⚠️ 이 테스트를 처음 돌렸을 때 **첫 줄만 찍히고 멈췄다.** 원인은 `XpToNext` 가 레벨 1에서 **5** 라
> 구슬 10개를 먹으면 레벨업 패널이 떠 `timeScale = 0` 이 되고, 그러면 `WaitForSeconds` 가
> 영원히 안 끝나기 때문이다. 테스트를 `WaitForSecondsRealtime` + 패널 자동 닫기로 고쳤다.
> **레벨업이 시간을 멈춘다는 사실은 앞으로 모든 런타임 검증 코루틴에 영향을 준다.**

임시 스크립트 3종(`WaveTest` · `AITest` · `PickupTest`)과 씬 오브젝트 **전부 삭제**,
`Assets/Refresh` 후 **콘솔 0건**, `File/Save` 완료.

---

## 2-19. ✅ ROADMAP 2단계 — 게임에 소리가 붙었다 (I-49, 2026-08-28 15차)

[`ROADMAP.md`](ROADMAP.md) §8 의 **2단계("소리를 붓는다")** — A-5(BGM 4곡) + A-6(SFX 세트 + 호출부 연결).
배관(A-1~A-4)은 13차의 I-45 로 이미 끝나 있었고, 이번에는 **클립과 호출부**를 채웠다.

### 원인

**I-49 — 배관은 다 깔렸는데 아무 소리도 안 났다**
I-45 로 `PlaySfx`/`PlayBgm`/`StopBgm` 과 보이스 풀 16개, 중복 컷까지 만들었지만
`Assets/` 안에 **오디오 파일이 0개**였고 `PlaySfx` 를 **부르는 곳도 0곳**이었다.
즉 소리를 낼 능력은 있는데 **낼 것도, 낼 이유도 없는** 상태였다.

막혀 있던 건 파일이 아니라 **결정 하나**였다 — 클립 참조를 어디에 둘 것인가.

| 안 | 방식 | 문제 |
|---|---|---|
| (A) | 각 `WeaponData`/`EnemyData`/`BuildingData` 에 `AudioClip` 필드 추가 | **UI·시스템 소리는 갈 곳이 없다.** 그리고 무엇이 비었는지 보려면 애셋을 전부 열어야 한다 |
| **(B)** | **SO 하나에 키→클립 표를 모으고 `AudioManager` 가 들고 있는다** | 이름 오타가 런타임에만 드러난다 → **문자열 대신 enum 으로 막았다** |

**사용자 결정: (B)** (2026-08-28). (B) 의 유일한 약점이던 "이름 오타"는 키를 `string` 이 아니라
`SfxId`/`BgmId` **enum** 으로 만들어 컴파일 타임에 잡히게 했다.

> ⚠️ 그래서 클립은 CSV 파이프라인에 **넣지 않았다.** `AudioLibrary.asset` 은 CSV 임포터가
> 건드리지 않는 애셋이라 **인스펙터에서 직접 고쳐도 되는 예외**다 (→ [`BALANCE.md`](BALANCE.md)).

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Audio/AudioId.cs` | **신규** — `SfxId`(14종) · `BgmId`(4곡) enum |
| `Assets/Scripts/Audio/AudioLibrary.cs` | **신규** — 키→클립 표 SO. 클립별 `Volume`·`PitchJitter` 포함 + `DescribeMissing()` |
| `Assets/Scripts/UI/AudioManager.cs` | `library` 필드 + `AudioManager.Play(SfxId)` / `PlayMusic(BgmId, fade)` 정적 진입점 |
| `Assets/Scripts/Core/GameManager.cs` | `ChangeState` 끝에 `UpdateBgm(newState)` — **BGM 전환의 유일한 지점** |
| `Assets/Scripts/Wave/WaveManager.cs` | `IsBossWave` 프로퍼티 노출 + 클리어 징글 |
| `Assets/Scripts/Enemy/EnemyBase.cs` | 피격·사망 (죽는 타격은 피격음 **생략**) |
| `Assets/Scripts/Player/PlayerStats.cs` | 피격·사망 (동일 규칙) |
| `Assets/Scripts/Weapon/ProjectileWeapon.cs` | 발사 — **볼리당 1회** |
| `Assets/Scripts/Weapon/AoeWeapon.cs` · `AoeProjectile.cs` | 시전 · 폭발 |
| `Assets/Scripts/Experience/ExperienceManager.cs` | 구슬 획득 · 레벨업 팡파레 |
| `Assets/Scripts/Pickup/WorldPickup.cs` | 상자 · 자석 |
| `Assets/Scripts/Building/BuildingManager.cs` | 설치 — **성공 경로에서만** |
| `Assets/Scripts/LevelUp/ItemCardUI.cs` · `UI/ShopCardUI.cs` · `UI/ClassCardUI.cs` · `UI/StageMapUI.cs` | 선택음 |
| `Assets/Game/Audio/` | **신규 22MB** — BGM 4 + SFX 14 (`.wav`, 전부 모노) + `AudioLibrary.asset` |

### 설계에서 일부러 정한 것들

**enum 값을 명시적으로 박았다** (`WeaponFire = 1`, `EnemyHit = 10`, …).
직렬화된 `AudioLibrary.asset` 은 이름이 아니라 **정수**를 저장한다. 중간에 항목을 끼워 넣으면
그 아래 매핑이 **통째로 한 칸씩 밀린다.** 새 항목은 반드시 끝에 추가한다.

**"소리 뭉개짐"을 코드 구조로 막았다** — 나중에 튜닝으로 고치기 어려운 것들이라 처음부터 위치를 잡았다.

| 규칙 | 이유 |
|---|---|
| 죽는 타격에는 **피격음을 안 낸다** | 같은 프레임에 사망음과 겹쳐 뭉갠다 (적·플레이어 양쪽) |
| 다발 발사는 **한 번만** 운다 | 투사체 5개면 같은 클립이 5중으로 겹쳐 위상 간섭이 난다 |
| 레벨업 팡파레는 `while` 루프 **안**에 | `TriggerLevelUp` 은 **보물상자 보상도 재사용**한다. 상자는 레벨이 오르는 게 아니라 카드만 한 번 더 고르는 것이라 소리가 달라야 한다 |
| 건물 설치음은 **성공 경로에서만** | 자리를 못 찾아 실패한 것과 세워진 것을 **소리로 구분**할 수 있어야 한다 |
| UI 선택음은 **확정 지점 4곳만** | 버튼 20개에 다 붙이면 화면 넘길 때마다 딸깍거린다 |

**BGM 전환을 `GameManager.ChangeState` 한 곳에 몰았다.** 모든 화면 전환이 반드시 지나는 길목이라
화면마다 흩어 놓으면 반드시 빠뜨리는 곳이 생긴다. 단 `LevelUp`·`Paused` 는 웨이브 위에 **끼어드는**
상태라 곡을 바꾸지 않는다 — 레벨업이 뜰 때마다 음악이 끊기면 전투의 흐름이 매번 잘린다.

> ⚠️ **16차에 `Paused` 는 이 판단에서 빠졌다** (I-51). 사용자가 직접 플레이해 보고
> "ESC 눌렀을 때 BGM 정지"를 요구했다. 레벨업(0.5~2초)과 달리 일시정지는 **플레이어가 게임을
> 손에서 놓는 시간**이라 성격이 다르다. `LevelUp` 만 여전히 곡을 유지한다. → 2-20

**임포트 설정을 용도별로 갈랐다.**

| 용도 | 설정 | 이유 |
|---|---|---|
| BGM | `Streaming` + Vorbis q0.7 + `loadInBackground` | 30초 스테레오 PCM 한 곡이 메모리에 **약 5MB** 상주한다 |
| SFX | `DecompressOnLoad` + ADPCM | 초당 수십 번 울린다. **디코드 지연을 감당할 수 없다** |
| 공통 | `forceToMono` | `spatialBlend = 0`(2D)이라 스테레오가 의미 없다. 용량 절반 |

### 파일 크기에 대해

`.wav` 총 **22MB**. ffmpeg 이 이 머신에 없어 **wav → ogg 트랜스코딩은 불가**했다.
대신 `forceToMono` + `Unity_AudioClip_Edit(TrimSilence)` 로 원본 대비 **42~97%** 까지 줄였다.
(`TrimSilence` 는 제자리 편집이 아니라 `"<원본> 1.wav"` 를 **새로 만든다** — 원본을 지우고 이름을 되돌렸다.)
게임에 들어가는 실제 용량은 임포트 압축 후라 이보다 훨씬 작다.

### 검증 로그

```
[AUDIOTEST] library=True bgmVol=0.80 sfxVol=1.00
[AUDIOTEST] missing = (none)
[AUDIOTEST] AudioSource 총 17개 (보이스 16 + bgm 1 = 17 이어야 정상)
[AUDIOTEST] WeaponFire     clip=SFX_WeaponFire       len=0.45s vol=0.35 울리는보이스=2
[AUDIOTEST] WeaponCast     clip=SFX_WeaponCast       len=0.53s vol=0.45 울리는보이스=3
[AUDIOTEST] Explosion      clip=SFX_Explosion        len=0.57s vol=0.55 울리는보이스=3
[AUDIOTEST] EnemyHit       clip=SFX_EnemyHit         len=0.48s vol=0.28 울리는보이스=3
[AUDIOTEST] EnemyDie       clip=SFX_EnemyDie         len=0.45s vol=0.40 울리는보이스=4
[AUDIOTEST] PlayerHit      clip=SFX_PlayerHit        len=1.02s vol=0.70 울리는보이스=4
[AUDIOTEST] PlayerDie      clip=SFX_PlayerDie        len=1.36s vol=0.90 울리는보이스=3
[AUDIOTEST] XpPickup       clip=SFX_XpPickup         len=0.49s vol=0.22 울리는보이스=4
[AUDIOTEST] LevelUp        clip=SFX_LevelUp          len=1.02s vol=0.85 울리는보이스=5
[AUDIOTEST] ChestOpen      clip=SFX_ChestOpen        len=1.01s vol=0.80 울리는보이스=4
[AUDIOTEST] Magnet         clip=SFX_Magnet           len=0.69s vol=0.70 울리는보이스=5
[AUDIOTEST] BuildingPlace  clip=SFX_BuildingPlace    len=0.47s vol=0.60 울리는보이스=6
[AUDIOTEST] WaveClear      clip=SFX_WaveClear        len=1.17s vol=0.85 울리는보이스=5
[AUDIOTEST] UiSelect       clip=SFX_UiSelect         len=0.13s vol=0.55 울리는보이스=3
[AUDIOTEST] 중복컷 20회 연타 → 새로 울린 보이스=1 (1 이어야 정상)
[AUDIOTEST] state=MainMenu  bgm=BGM_MainMenu     playing=True vol=0.80
[AUDIOTEST] state=StageMap  bgm=BGM_Shop         playing=True vol=0.80
[AUDIOTEST] state=Wave      bgm=BGM_WaveNormal   playing=True vol=0.80
[AUDIOTEST] state=GameOver  bgm=(none)           playing=False vol=0.80
[AUDIOTEST] 같은 곡 재요청 t=2.01 → 2.50 (되감기지 않고 늘어나야 정상)
[AUDIOTEST] 완료
```

가장 중요한 두 줄은 **중복 컷**과 **재요청**이다.

- `중복컷 20회 연타 → 1` — 경험치 구슬은 자석을 먹으면 **한 프레임에 수십 개**가 들어온다.
  0.04초 창이 없으면 같은 클립이 20중으로 겹쳐 굉음이 된다. 실제로 걸러진다는 걸 확인했다.
- `같은 곡 재요청 2.01 → 2.50` — 스테이지맵 ↔ 상점 ↔ 이벤트는 **같은 곡**을 쓴다.
  전환할 때마다 되감기면 곡이 영영 도입부만 반복한다. 재생 위치가 유지된다.

임포트 설정 확인:

```
missing after wiring = (none)
SFX 14종 len=0.13~1.36s ch=1 / BGM 4곡 len=26.20·30.77·30.77·30.77s ch=1
AudioManager active=True library=True bgmSource=True
bgmSource loop=True playOnAwake=False spatialBlend=0
```

임시 스크립트 `AudioTest.cs` 와 씬 오브젝트 `__AudioTest` **삭제**,
`Assets/Refresh` 후 **콘솔 0건**, `File/Save` 완료.

> ⚠️ **로그로 검증한 것은 "울린다"까지다.** 음량 밸런스·곡의 어울림은 귀로만 판단할 수 있다
> → [`TODO.md`](TODO.md) §1

---

## 2-20. ✅ 직접 플레이에서 나온 첫 버그 3건 (I-50~I-52, 2026-08-29 16차)

15차 문서에 "지금 가장 필요한 것은 코드가 아니라 직접 플레이다"라고 적어 뒀는데,
**실제로 플레이하자마자 로그로는 절대 안 잡히는 버그가 나왔다.** 그 기록이다.

### 원인

**I-50 — 첫 전투를 시작하면 옵션창이 화면에 튀어나온다**

정적 분석으로는 아무 문제가 없었다. 씬의 `OptionSubPanel` 은 **비활성**이고,
버튼 배선도 전부 정상이며(`MainMenuUI`·`PauseMenuUI`·`HUDManager` 사이에 교차 배선 없음),
씬 전체를 `SerializedObject` 로 훑어 `OptionSubPanel` 을 참조하는 컴포넌트를 찾아도
**`MainMenuUI.optionSubPanel` 과 `PauseMenuUI.optionSubPanel` 딱 둘뿐**이었다.
스크립트로 `onClick.Invoke()` 를 눌러 메인메뉴→직업선택→맵→전투까지 재현해도 `opt=False` 였다.

놓친 것은 **재현 경로 자체**였다. 원인이 둘 겹쳐 있었다.

| | 문제 | 왜 안 보였나 |
|---|---|---|
| (a) | `UI Canvas` 자식 10개가 **전부 전체화면 1920×1080** 이고 `Canvas` 정렬 오버라이드가 없다 → **형제 순서 = 그리는 순서**. `OptionSubPanel` 이 **3번**인데 `MainMenuPanel` **5**, `StageMapPanel` **6**, `ClassSelectPanel` **9** | 메뉴에서 옵션을 켜면 **패널 뒤에 깔려 안 보인다** |
| (b) | `MainMenuUI.OnOptionClicked()` 은 `SetActive(true)` 만 한다. **상태가 바뀔 때 옵션창을 닫는 코드가 어디에도 없었다** | 켜진 채로 계속 남는다 |

합치면 — **메인메뉴에서 Option 을 누른다 → 뒤에 깔려 안 보인다 → 그냥 Start 를 누른다 →
전투 진입에서 메뉴 패널이 전부 꺼진다 → 켜져 있던 옵션창만 화면에 남는다.**
"Option 을 눌러 봤는데 아무 반응이 없더라"는 **사용자만 아는 사실**이라 로그에는 흔적이 없었다.

**I-51 — ESC 로 멈춰도 음악은 계속 나온다**

15차(I-49)에 `LevelUp`·`Paused` 를 묶어 "웨이브 위에 끼어드는 상태라 곡을 안 바꾼다"고
**일부러** 정했던 판단이다. 직접 플레이해 보니 둘은 성격이 달랐다 —
레벨업은 0.5~2초 만에 끝나지만, 일시정지는 **플레이어가 게임을 손에서 놓는 시간**이다.
`LevelUp` 은 그대로 두고 `Paused` 만 뺐다.

**I-52 — 파이어볼이 날아오지 않고 적 발밑에서 그냥 터진다**

`AoeWeapon` 은 `FindNearestEnemy()` 위치에 폭발을 바로 꺼내 놓았다.
곡사포(`BombardBuilding`)는 11차에 이미 `BombProjectile` 로 "날아가는 시간"을 얻었는데
무기 쪽은 안 되어 있었다 — [`TODO.md`](TODO.md) §3 에 재사용 경로까지 적혀 있던 항목이다.

걸림돌은 **Fireball 과 Bomb 이 같은 `Weapon_Aoe.prefab` 을 공유한다**는 점이었다.
프리팹에 `bombPrefab` 필드를 달면 **폭탄까지 같이 날아간다.** 요청은 파이어볼만이었다.
→ 프리팹이 아니라 **데이터로 갈랐다**: `WeaponData.TravelPrefab` (CSV 열) 을 신설해
Fireball 만 채우고 Bomb 은 비워 뒀다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | `PausePanel` → 8번, `OptionSubPanel` → 9번 (오버레이를 **맨 위**로) |
| `Assets/Scripts/UI/MainMenuUI.cs` | `OnHidden()` 추가 — 메인메뉴를 떠날 때 옵션창을 닫는다 |
| `Assets/Scripts/UI/PauseMenuUI.cs` | `Close()` 에서 `CloseOption()` 호출 — 옵션창을 연 채 Resume 을 눌러도 안 남는다 |
| `Assets/Scripts/UI/AudioManager.cs` | `PauseBgm()`/`ResumeBgm()` + static 래퍼, `_bgmPaused` 플래그. `PlayBgm` 이 "멈춰 있던 같은 곡"이면 이어서 재생 |
| `Assets/Scripts/Core/GameManager.cs` | `UpdateBgm` 에서 `Paused` 를 `LevelUp` 과 분리 → `PauseBgm()` |
| `Assets/Scripts/Weapon/WeaponData.cs` | `TravelPrefab` 필드 신설 |
| `Assets/Scripts/Weapon/AoeWeapon.cs` | `LaunchTravel()` / `Explode()` 로 분리. TravelPrefab 이 있고 속도>0 이면 날려 보낸다 |
| `Assets/Editor/BalanceImporter.cs` | `TravelPrefab` 열 임포트 **+ 익스포트 헤더에도 추가** |
| `Assets/Game/Balance/Weapons.csv` | `TravelPrefab` 열 추가. Fireball 속도 `0 → 11` |
| `Assets/Prefabs/Proj_Fireball.prefab` | **신규.** `Proj_Bomb` 복제 → Fireball 스프라이트, `arcHeight 0`·`spinSpeed 0` |
| `Assets/Game/WeaponData/*.asset` | CSV 임포트 산출물 5개 |

### 설계에서 일부러 정한 것들

**옵션창은 코드와 씬 양쪽에서 막았다.** `OnHidden()` 만 고치면 지금 이 경로는 막히지만,
**새 전체화면 패널을 추가할 때마다 같은 함정을 다시 밟는다.** 오버레이(`PausePanel`·
`OptionSubPanel`)를 캔버스 맨 뒤로 옮겨 **"오버레이는 항상 맨 위"** 를 구조로 만들었다.

**`StopBgm` 이 아니라 `PauseBgm` 이다.** `StopBgm` 은 `bgmSource.clip` 을 `null` 로 만든다.
그대로 쓰면 ESC 를 누를 때마다 **곡의 도입부만 반복해서 듣게 된다.**
`AudioSource` 에는 "지금 일시정지 중인가"를 묻는 프로퍼티가 없어(`Pause()` 해도 `isPlaying` 은
그냥 `false`) `_bgmPaused` 를 직접 들고 있다. 이게 없으면 `Close()` → `ChangeState(Wave)` →
`PlayMusic(WaveNormal)` 이 "`isPlaying == false` 니까 새로 틀자"고 판단해 **처음부터 다시 튼다.**

**`LevelUp` 은 그대로 뒀다.** 레벨업 카드는 몇 초 만에 닫히는데 그때마다 곡이 끊기면
전투의 흐름이 매번 잘린다. 일시정지와 레벨업은 **길이가 다르다.**

**AoE 무기의 비행체를 프리팹이 아니라 `WeaponData` 에 뒀다.** Fireball·Bomb 이 프리팹을
공유하므로 프리팹 필드로 만들면 갈라낼 수가 없다. CSV 열 하나면 **앞으로 새 AoE 무기마다
즉발/비행을 데이터로 고를 수 있다.**

**`TravelPrefab` 은 `ProjectileSpeed > 0` 일 때만 발동한다.** 프리팹만 꽂고 속도를 0 으로 두면
목표에 영원히 도달하지 못한다. 두 조건을 함께 걸어 그 상태를 만들 수 없게 했다.

### 검증 로그

임시 `PauseDbg` 로 **사용자가 실제로 했을 경로**(메인메뉴에서 옵션을 켠 채 시작)를 재현했다.

```
[PAUSEDBG] 클릭 → MainMenu.Option (state=MainMenu)
[PAUSEDBG] ① 옵션 연 직후 opt=True (기대: True)
[PAUSEDBG] 클릭 → MainMenu.Start (state=MainMenu)
[PAUSEDBG] ① 메인메뉴 떠난 뒤 opt=False (기대: False)
[PAUSEDBG] ① 전투 진입 opt=False pause=False state=Wave (기대: False/False/Wave)

[PAUSEDBG] ② 일시정지 전 playing=True time=1.98 clip=BGM_WaveNormal
[GameManager] State → Paused
[PAUSEDBG] ② 일시정지 중 playing=False time=1.98 state=Paused (기대: False/Paused)
[GameManager] State → Wave
[PAUSEDBG] ② 재개 후 playing=True time=2.97 (기대: True, time 이 1.98 부근에서 이어짐 — 0 이면 처음부터 다시 튼 것)

[PAUSEDBG] ③ Fireball 지급
[PAUSEDBG] ③ 날아다니는 Proj_Fireball 최대 동시 개수=1 (기대: 1 이상)
```

②의 `time` 이 **0 이 아니라 2.97** 인 것이 핵심이다 — 멈춘 지점(1.98)에서 이어졌다는 뜻이다.
③은 쿨다운 2.2초 · 속도 11 이라 동시 1개가 정상이다.

데이터 배선도 따로 확인했다:

```
[WPN] Fireball travel=Proj_Fireball explode=Proj_Aoe(Boom) speed=11
[WPN] Bomb     travel=(없음)        explode=Proj_Aoe(Boom) speed=0    ← 즉발 유지
[WPN] Sword    travel=(없음)        explode=Proj_Bullet     speed=8
[WPN] Proj_Fireball arc=0 spin=0 sprite=Fireball layer=8
```

임시 스크립트 `PauseDbg.cs` 와 씬 오브젝트 `__PauseDbg` **삭제**,
`Assets/Refresh` 후 **콘솔 0건**, `File/Save` 완료.

> ⚠️ **여기서도 로그가 증명한 것은 "동작한다"까지다.** 파이어볼의 비행 속도 11 이
> 답답한지 빠른지, 옵션창이 이제 제대로 보이는지는 **눈으로만** 판단할 수 있다
> → [`TODO.md`](TODO.md) §1

---

## 2-23. ✅ 직업 승급 — 무기 + 건물 → 직업 (I-56, 2026-08-29 19차)

### 왜 했나

17차(I-54)에서 진화를 `패시브 · 무기 · 건물` 3조합으로 열어 뒀는데, 그 셋이 **전부 무기를 뱉었다.**
그러면 건물이 재료로 들어가는 이유가 "제단이 필요해서"뿐이고, 조합마다 **결과가 달라지지 않는다.**

사용자 지시로 조합을 결과별로 갈랐다.

| 조합 | 결과 | 전달 경로 | 표 |
|---|---|---|---|
| 무기 + 무기 · 무기 + 패시브 | **진화 무기** | 보물상자 | `Evolutions.csv` |
| 무기 + **건물** | **직업 (T2·T3…)** | **그 건물 앞 `E`** | **`ClassEvolutions.csv`** (신설) |

기존 `Sentinel`·`Doomsday` 두 레시피가 정확히 "무기 + 건물"이었다 — 무기에서 **직업으로 옮겼다.**

### 설계 결정 4가지 (사용자 답변)

| 질문 | 답 | 구현 |
|---|---|---|
| ① 보너스는 누적인가 교체인가 | **누적** | `PlayerStats._classChain` — 직업을 하나가 아니라 **목록**으로 든다 |
| ② 소지 상한도 승급으로 느나 | **직업마다 다르게 추가** | `Max*Slots` → **`Bonus*Slots`** 개명. 사슬 전체를 합산 |
| ③ 캐릭터 이미지도 바뀌나 | **바뀌어야 한다** | `ApplyClassVisual` 을 승급에서도 태운다. **그림은 아직 없음** → `TODO.md` §4 |
| ④ 레시피 관리 방식 | **별도 CSV** | `ClassEvolutions.csv` + `ClassEvolutionData` SO |

### 왜 사슬(List)인가 — 교체하면 승급이 손해가 된다

직업을 **하나만 들고 교체**하면 T2 로 올라가는 순간 T1 의 보너스와 소지 칸이 **사라진다.**
`Warrior`(건물 5칸) 가 `Sentinel`(건물 +1) 로 갈아타면 건물이 **5 → 1** 이 되어,
승급하면 할수록 약해지는 구조가 된다. 그래서 `Class` 필드를 `List<CharacterClassData>` 로 바꾸고
`RecalculateStats`·`SlotLimit` 이 **사슬 전체를 합산**하게 했다.

같은 이유로 `Bonus*Slots` 는 **총량이 아니라 더하는 값**이다. 총량으로 두면 승급 행에 "총 4"를
적었을 때 `3 + 4 = 7` 이 되는 함정이 생긴다 — 데이터만 보고는 눈치챌 수 없는 종류다.

### 무기 진화와 다른 세 가지

- **재료를 소모하지 않는다.** 무기 진화는 무기를 먹고 더 센 무기를 돌려주니 교환이 성립하지만,
  승급은 돌려주는 게 직업이라 재료까지 가져가면 **순수한 손해**다
- **체력을 가득 채우지 않는다.** 승급은 필드에서 전투 중에 일어난다 — 완전 회복이 붙으면
  "위험할 때 승급을 아껴 두는" 이상한 운용이 생긴다. 대신 **늘어난 최대치만큼은 얹는다**
  (안 그러면 최대 체력이 올라도 현재 체력이 그대로라 승급이 눈에 안 띈다)
- **제단이 필수다.** 무기 진화는 건물 재료가 없으면 보물상자로 새지만, 승급은 "건물 앞에서"가
  규칙이라 건물이 없는 행은 **어디서도 발동 못 하는 죽은 레시피**다 → 임포트 시 `!` 경고

### `FromClass` 는 "현재"가 아니라 "거쳐 왔는가"

`ps.HasClass(evo.FromClass)` 로 **사슬 어디에든** 있으면 통과시킨다.
끝(현재 직업)만 보면 같은 T2 에서 T3 두 갈래가 갈릴 때, 한쪽을 타는 순간 다른 쪽이 영영 막힌다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Scripts/Evolution/ClassEvolutionData.cs` | **신규.** 승급 레시피 SO. 제단은 `EvolutionData` 와 같은 방식으로 재료에서 파생 |
| `Scripts/Evolution/EvolutionManager.cs` | `classEvolutions` 배열 · `IsClassSatisfied` · `GetReadyClassEvolutions` · `FindAltarClassEvolution` · `EvolveClass`. `TryEvolveAtAltar` 가 **승급을 먼저** 본다 |
| `Scripts/Player/PlayerStats.cs` | `Class` 단일 필드 → `_classChain` 목록. `BaseClass`/`ClassChain`/`HasClass`/`EvolveClass` 추가. `RecalculateStats`·`SlotLimit` 이 사슬 합산. `ApplyClassVisual` 추출 |
| `Scripts/Player/CharacterClassData.cs` | `Max*Slots` → `Bonus*Slots` (더하는 값). `BonusSlots(ItemCategory)` 추가 |
| `Scripts/UI/EvolutionPromptUI.cs` | `[E] PROMOTE` / `PROMOTION READY` 분기 추가. **판정 순서를 `TryEvolveAtAltar` 와 일치**시켰다 |
| `Scripts/Weapon/WeaponManager.cs` | 주석만 (사슬 합산으로 문구 수정) |
| `Editor/BalanceImporter.cs` | `ImportClassEvolutions` + Export. 슬롯 3열 개명 반영 |
| `Game/Balance/ClassEvolutions.csv` | **신규.** 승급 레시피 4종 |
| `Game/Balance/Classes.csv` | 슬롯 3열 개명, T2 3종 + T3 1종 추가 |
| `Game/Balance/Evolutions.csv` | `Sentinel`·`Doomsday` **제거** (5 → 3) |
| `Game/Balance/Items.csv` · `Weapons.csv` | 위 2종의 무기·아이템 행 제거 |
| `Game/Balance/SceneWiring.csv` | `allEvolutions` 3개로 축소, `classEvolutions` 행 추가 |
| `Game/{Evolution,Item,Weapon}Data/` | `Sentinel`·`Doomsday` `.asset`+`.meta` 6쌍 삭제 (고아 애셋) |

### 콘텐츠

| Id | From | 재료 | 제단 | 결과 | 슬롯 보너스 |
|---|---|---|---|---|---|
| Sentinel | *(아무 직업)* | Gun Lv5 + **Turret Lv3** | Turret | Sentinel (T2) | W+0 P+1 B+1 |
| Doomlord | *(아무 직업)* | Bomb Lv5 + **Bombard Lv3** | Bombard | Doomlord (T2) | W+1 P+1 B+0 |
| Warden | *(아무 직업)* | Sword Lv5 + **Village Lv3** | Village | Warden (T2) | W+0 P+1 B+2 |
| Aegis | **Sentinel** | Turret Lv5 + Armor Lv3 | Turret | Aegis (T3) | W+1 P+1 B+1 |

> 수치는 **자리표시값**이다. 실사격 전 감으로 잡았다 → [`TODO.md`](TODO.md) §3

### 검증 로그

`Assets/Refresh` 후 **콘솔 0건**. CSV Import 도 경고 0건:

```
Weapons: 8  Items: 23  Classes: 7  Evolutions: 3  ClassEvos: 4  SceneWiring: 11/11
```

Play 모드에서 Warrior 로 런을 시작하고 `Unity_RunCommand` 로 재료를 지급한 뒤 승급:

```
[PROMO] before  class=Warrior  chain=1  maxHp=130  hp=130  armor=2  slots W3/P5/B5
[PROMO] satisfied Sentinel=True  Aegis=False      ← Aegis 는 FromClass=Sentinel
[PROMO] EvolveClass=True
[PROMO] after   class=Sentinel chain=2  maxHp=150  hp=150  armor=3  slots W3/P6/B6
[PROMO] base(T1)=Warrior   재승급 차단=True
[PROMO] turret=5 armor=3   Aegis 가능=True
[PROMO] EvolveClass(Aegis)=True  chain=3  maxHp=210  armor=9  slots W4/P7/B7
```

- **누적 확인** — T1 Warrior 의 `+30 HP` / `+2 방어` / `3-5-5 칸`이 승급 후에도 살아 있다
- **HP 는 완전 회복이 아니다** — 130 → 150 (늘어난 최대치 +20 만큼만)
- **`FromClass` 게이트 동작** — Aegis 는 Sentinel 승급 전 `false`, 후 `true`
- **재승급 차단** — 사슬에 이미 있으면 조건 불성립
- 방어 9 는 `Armor` **패시브 Lv3** 이 같이 들어간 값이다 (6 + 3)

> ⚠️ **건물 앞 `E` 실조작은 아직 미검증이다.** 위는 `EvolveClass` 를 직접 부른 것이고,
> `FindAltarClassEvolution` 은 이미 검증된 `FindAltarEvolution` 과 같은 로직이지만
> **실제로 터렛을 세우고 다가가서 눌러 본 적은 없다** → [`TODO.md`](TODO.md) §1

---

## 2-100. ✅ 체력 재생 — **판정이 아니었으면 0 인 채로 넘어갔다** (D74, 2026-09-05 96차)

**한 줄:** 사용자 요구 `11`. 그리고 그 자리에서 **같은 함정에 두 번째로 걸린 자국**(`B13`)을 찾았다.

> 사용자: *"기본 체력 재생 추가 (아이템도 추가 체력 재생 추가)"*

### 왜 지금 필요했나

`D73` 이 보스 패턴을 둘 늘려 **압박만 커졌는데** 회복 수단은 상점 휴식(`D71`)뿐이었다.
**깎이기만 하고 차지 않으면** 한 번 크게 맞은 판은 그 뒤로 계속 조심만 하게 된다.

| 무엇 | 값 |
|---|---|
| 기본 재생 | **0.4/s** (`Economy.csv` · `baseStats.HpRegen`) — 분당 24, 웨이브 하나(60~100초)에 24~40 |
| 패시브 `Regeneration` | **0.4 / 0.8 / 1.3 / 1.9 / 2.6** /s |

🔴 **`StatBlock.HpRegen` 의 기본값은 0 이다.** `XpGain`/`GoldGain` 처럼 배율이 아니라
**더해지는 양**이라, 1 로 두면 아무것도 안 먹은 플레이어가 초당 1 씩 찬다.
`Zero()` 에도 넣었다 (`I-21`).

🔴 **죽은 뒤에는 안 돈다.** `Heal` 은 `IsDead` 를 안 보므로 여기서 막지 않으면 **시체가 체력을 채운다.**
🔴 **이미 만피면 아예 안 돈다.** `Heal` 을 매 프레임 부르면 나중에 회복 연출을 붙일 때
**가만히 서 있어도 이펙트가 터진다.**

### 🔴 판정에서 걸렸다 — `Final.HpRegen` 이 0 이었다

`StatBlock` 에 필드를 더하고, `PassiveData`·`PassiveEffect`·임포터·CSV 를 전부 고치고
Import 까지 마쳤는데 **첫 측정이 `HpRegen=0.00`** 이었다.

`PlayerStats.RecalculateStats` 가 `Final` 을 **필드별 나열**로 만든다.

```csharp
Final = new StatBlock { MaxHp = ..., MoveSpeed = ..., /* HpRegen 이 없다 */ };
```

⇒ 새 필드는 **기본 생성자 값(0)** 이 그대로 남는다. 한 줄 추가로 고쳤다.

### 🔴 그 자리에서 같은 누락을 하나 더 찾았다 — `B13`

**`Luck` 도 그 나열에 없다.** `base`/`meta` 의 `Luck` 이 죽는다.
패시브의 `Luck` 은 뒤이어 `p.Apply(Final)` 이 더하므로 **패시브만 작동**해서 지금까지 안 걸렸다.

🟡 **아직 안 터진다** — 메타 업그레이드에 `Luck` 이 없고 `baseStats.Luck` 행도 없다.
**둘 중 하나만 생기면 그 순간 터진다.** 고치지 않고 [`BUGS.md`](Parallel/BUGS.md) `B13` 에 등재했다.

🔑 **이 나열은 지금까지 두 번 사람을 속였다**(`Luck` · `HpRegen`).
`StatBlock.Add(a, b)` 같은 합산 메서드로 묶는 게 맞아 보이지만,
스탯 합산은 게임 전체가 매달린 자리라 **바꾸면 전 스탯을 다시 재야 한다** — 사용자 결정 대기.

### 판정 4/4

```
[기본 재생]  경과 3.59s · HP 32.27 (기대 30.83 + 0.4×3.59 = 32.27) · 오차 0.001
[패시브]     기본 0.40 → Lv1 0.80 (0.4+0.4) → Lv2 1.20 (0.4+0.8)
[만피]       130.00/130 에서 더 안 오른다
[Import]     Passives 11→12 · Items 28→29 · Economy 74/74
```

🟡 **시험을 한 번 오염시켰다** — 적을 끄고 쟀는데 **웨이브가 계속 소환**해서
`timeScale 8` 로 빠르게 맞았고 HP 가 67 → **31.83** 으로 줄었다.
무적을 걸어 피해만 막고 다시 쟀다(`TickRegen` 은 무적과 무관하게 돈다).

### ⚠️ 그림이 없다

패시브 아이콘을 **`MaxHp.png`(하트)로 임시 재사용**했다.
`Pickup_Swift` 와 같은 상황이다 — CONTENT 에 넘겼다(`요청-51`).

---

## 2-99. ✅ 보스 패턴 2종 — **소환과 돌진을 묶은 것이 핵심이다** (D73, 2026-09-05 95차)

**한 줄:** 사용자 요구 `B-1`. 보스가 내려찍기·소환 둘뿐이라 *"패턴을 좀 더 추가해야 할 것 같다"* 였다.

> 사용자(`B-1`): *"쫄 소환 + 돌진 패턴, 예고 범위 일직선 공격 이렇게 추가해"*

### ① 쫄 소환 + 돌진 — 🔑 **둘을 하나로 묶은 게 설계의 전부다**

따로 두면 각각 *"가끔 일어나는 일"* 인데, 묶으면 **쫄이 나타난 것 자체가 돌진의 예고**가 된다.
그리고 쫄이 길을 막아 **피할 자리를 좁힌다** — 두 기술이 서로를 돕는다.

```
① 쫄 소환 (예고)  →  ② 멈춰 서서 노려보기 0.75s  →  ③ 돌진 11 × 0.55s  →  ④ 경직 0.9s
```

🔴 **돌진 방향은 노려보기가 끝나는 순간 한 번만 정한다.** 매 프레임 다시 겨누면
유도 미사일이 되어 **피할 수 없는 공격**이 된다.

🔴 **경직(`ChargeRecover`)이 없으면 계속 밀리기만 한다** — 플레이어의 반격 자리다.
임포터가 `0` 이면 경고한다.

### ② 예고 범위 일직선 — 🔑 **새 시스템을 안 만들었다**

`BossSlam`(예고 → 폭발 → 풀 반환)을 **줄 세워** 놓는다.
`D37` 의 지뢰밭이 같은 부품을 썼고 **이미 검증된 코드**다 —
직선 판정을 새로 짜면 예고 그림·풀 반환·피해 판정을 전부 다시 만들어야 한다.

🔴 마디가 서로 겹치므로 **한 번에 여러 마디에 맞을 수 있다.** `LineDamage`(14)를
`SlamDamage`(18)보다 낮게 둔 이유다.

### 이동을 잠시 내주는 창구

`EnemyBase` 에 `AiSuspended` 와 `DriveVelocity` 를 냈다.
🔴 **넉백과 같은 이유로 `return` 이 필요하다** — 아래 `Tick` 들이 `linearVelocity` 를
통째로 덮어써서, 그 줄이 없으면 **돌진 속도가 다음 물리 프레임에 지워진다.**

🔴 **`Initialize` 에서 반드시 끈다.** 돌진 중에 죽은 보스가 풀로 돌아갔다가
잡몹으로 나오면 **그 잡몹이 안 움직인다.**

### 판정 7/7

```
[일직선]  예고 7개 · 직선에서 벗어난 최대 거리 0.000
          마디 간격 1.71~1.71 (= 12/7, 균일) · 양끝 10.29 (= 12 − 12/7)
[노려보기] AiSuspended=True · 속도 0.00
[돌진]     6.16 유닛 이동 (기대 11 × 0.55 = 6.05)
[경직]     AiSuspended=True · 속도 0.00
[복귀]     AiSuspended=False · 속도 1.08 (평상시 추격)
[소환]     돌진과 함께 쫄이 나왔다
```

🔑 **일직선은 "7개가 나왔다" 로는 부족하다.** 직선 이탈 **0.000** 과
간격이 **전부 1.71** 인 것이 *"줄 세웠다"* 의 증거다.

### 🔴 판정 중에 결함을 하나 찾았고, 그 결함으로 판정했다

`EnterPhase` 가 `_chargeCd`·`_lineCd` 를 안 채워서 **둘 다 0 으로 시작**했다.
⇒ **보스가 나타난 첫 프레임에 일직선과 돌진이 동시에 나갔다.**
등장음이 울리기도 전에 맞으면 그건 패턴이 아니라 사고다.

🔑 **그런데 그 결함 덕분에 기술을 즉시 발동시켜 잴 수 있었다** — 쿨다운 8~9초를
기다리는 대신 보스를 다시 부르면 됐다. 재고 나서 고쳤다.

고친 뒤: 등장 직후 **예고 0개 · `AiSuspended=False`** (일직선 유예 3.1s · 돌진 유예 9s).

---

## 2-98. 🔴 맵 화면 정정 3건 — **`D70`·`D71` 을 사용자가 화면으로 잡았다** (D72, 2026-09-05 94차)

**한 줄:** 앞 두 작업이 **숫자로는 다 통과했는데 화면이 틀렸다.**
사용자가 스크린샷으로 세 가지를 짚었다.

> 사용자: *"화면 배열 이상해졌어 확인해 / 줄이너무 많이 연결되서 난잡해 박스 테두리 까지만 줄 그어 /
> 상점에서 rest 뜨라고 한건 더이상 구매를 못하는 상황에서 뜨게 의도한거야"*

### ① 세로 정렬 — 반 칸씩 어긋나 있었다

```
(i - nodeCount * 0.5f) * ySpacing        ← 예전
  2칸 층 → -150,   0
  3칸 층 → -225, -75,  75
```

**0 을 중심으로 대칭이 아니다.** 두 층이 서로 반 칸씩 밀려 화면이 들쭉날쭉했다.

```
(i - (nodeCount - 1) * 0.5f) * ySpacing  ← 지금
  2칸 층 →  -75,  75
  3칸 층 → -150,   0, 150
```

🟡 **`D70` 이 만든 결함은 아니다** — 원래 이랬는데, `D70` 이 층별 노드 수 분포를 바꾸면서
눈에 띄게 됐을 뿐이다. 그래도 사용자가 보기 전까지 **나는 못 봤다.**

### ② 연결선 — 아무 칸이나 잇고 있었다

```csharp
indices.Add(Random.Range(0, next.Count));   // 예전 — 다음 층의 아무 칸이나
```

맨 위 노드가 맨 아래로 가는 선이 예사로 생겼다. 3×3 구간에서는 선 여섯 줄이 엇갈려
**어디로 이어지는지 눈으로 못 따라간다.**

🔑 **자기 자리에 대응하는 칸과 그 이웃**에만 잇는다(단조). 위/아래 순서가 뒤집히지 않으므로
교차가 거의 사라진다 — 슬레이 더 스파이어의 맵이 읽히는 이유가 이것이다.

### ③ 선이 박스 <b>밑을</b> 지나고 있었다

중심 → 중심으로 그어서 칸 하나에 선이 네다섯 줄 겹쳤다. 그게 *"난잡하다"* 의 절반이다.

🔑 잘라 내는 길이를 **고정값이 아니라 사각형과의 교점**으로 구한다 —
반지름으로 자르면 가로로 긴 선은 덜 잘리고 대각선은 더 잘려 들쭉날쭉해진다.
(`BossOffscreenArrowUI` 가 화면 테두리를 찾는 것과 **같은 식**이다.)

### ④ `D71` 의 휴식 칸이 항상 떠 있었다 — **의도를 잘못 읽었다**

`D71` 은 휴식을 *"항상 한 칸"* 으로 만들고 아이템 칸을 3 → 2 로 줄였다.
🔴 **사용자 의도는 *"더이상 구매 못하는 상황에서"*** 였다.

⇒ 아이템이 `shopSlotCount` 만큼 다 차면 **서비스 칸은 안 나온다.**
빈자리가 생길 때만 **첫 칸 휴식 · 나머지 전환**으로 채운다
(체력이 없으면 골드도 의미가 없어서 회복을 먼저 둔다).

### 판정 5/5

```
[맵 200판] 교차 판당 1.0 · 먼 점프 0
[세로]     3칸 층 -150 0 150 · 2칸 층 -75 75      ← 0 을 중심으로 대칭
[선 길이]  최소 90.0 = 층 간격 200 − 칸 크기 110  ← 양쪽 테두리에서 정확히 잘렸다
           최대 153.9 = 세로차 225 연결의 y축 교점 (계산과 일치)
[상점] 살 게 있을 때  Item Item Item
       살 게 없을 때  Heal(54G) Exchange(100G) Exchange(100G)
```

🔑 **선 길이 90.0 이 이 판정의 핵심이다.** 중심에서 중심이면 200 이고,
반지름으로 잘랐다면 200 − 2×55 = 90 이 우연히 같아 보이지만,
**대각선 153.9 가 사각형 교점 계산과 맞는 것**이 방식까지 맞다는 증거다.

### 🔴 배운 것 — 세 번째다

`D56`(도감 실루엣) · `D67`(오라 굵기)에 이어 **숫자가 통과한 것을 화면이 잡은 세 번째**다.
앞의 둘은 내가 캡처해서 잡았고, **이번엔 못 잡아서 사용자가 잡았다.**
🔑 `ScreenSpaceOverlay` 캔버스는 `SubmitRenderRequest` 로 못 찍는다 —
**UI 는 내가 볼 수 있는 수단이 없다.** 그래서 좌표·길이를 숫자로 재는 데서 멈췄고,
*"화면에서 어떻게 보이나"* 는 그 숫자에 안 들어 있었다.

---

## 2-97. ✅ 상점 휴식 · 골드 전환 — **`B12` 를 닫았다** (D71, 2026-09-05 93차)

**한 줄:** 사용자 요구 `6`(상점에서 휴식)과 `B12`(꽉 차면 골드만 사라진다)가
**같은 물건**이었다. 사용자 답이 그 둘을 하나로 묶었다.

> 사용자: *"아이템 슬롯 제한에 걸려서 더이상 구매 못하면 힐템이나 골드 이전(런 골드 전환) 만 뜨는식으로 하면되"*

### 🔑 앞서 적어 둔 세 안이 전부 틀린 방향이었다

`B12` 에 A(진열에서 거른다) · B(버튼을 막는다) · C(실패 시 환불) 를 적어 뒀는데,
**셋 다 *"못 사게 막는다"*** 다. 막기만 하면 **상점 노드를 밟은 게 통째로 헛수고**가 된다.

사용자 답은 방향이 다르다 — **살 게 없으면 다른 걸 판다.** 상점이 계속 쓸모 있다.

### 만든 것

`ShopSlot` 이 지금까지 **`ItemData` 만** 들 수 있었다. 종류를 붙였다.

| 종류 | 무엇 | 언제 |
|---|---|---|
| `Item` | 지금까지의 그것 | 살 수 있는 아이템이 있을 때 |
| `Heal` | 골드로 최대 체력 40 % 회복 | **항상 한 칸** (사용자 요구 6) |
| `Exchange` | 런 골드 100 → 메타 골드 40 | 아이템이 모자란 만큼 |

⇒ 평상시 `Item · Item · Heal`, 꽉 차면 `Exchange · Exchange · Heal`.

🔴 **`CanAcquire` 만으로는 부족했다.** 그 함수는 *"이미 가진 것"* 에 무조건 `true` 를 돌려주므로
**만렙까지 통과한다.** 살 수 있다 = `CanAcquire(i) && !i.IsMaxLevel` 이다.

🔴 **`Purchase` 의 순서를 뒤집었다** — 그게 `B12` 의 나머지 절반이다.
예전에는 골드를 **먼저** 빼고 `ApplyItem` 이 조용히 거절해도 **성공 로그**를 찍었다.
이제 **줄 수 있는지 먼저 묻고**, 못 주면 골드에 손대지 않는다.

🔴 **전환은 `GrantMetaGold` 를 안 쓴다.** 그 함수는 `GoldGain` 배율을 곱하는데,
런 골드는 **벌 때 이미 그 배율을 받았다** — 전환은 그 돈을 옮기는 것뿐이다.
`AddPendingMetaGold`(배율 없음)를 새로 냈다. `D68` 의 Excalibur 특전(골드 2배)이 붙으면
차이가 그대로 두 배로 벌어진다.

### 판정 6/6

```
[평상시]     Item(Village 83G) · Item(Octopus 92G) · Heal(54G, +52HP)   층 3
[꽉 찬 상태] 무기 3/3 · 패시브 5/5 · 건물 5/5 · 살 수 있는 아이템 0종
             → Exchange(100G,+40) · Exchange(100G,+40) · Heal(54G,+122)
🔴 [B12 대조군] 만렙 'Sword' 구매 시도
             경고 "구매 거절 — 골드는 그대로다" · 골드 2000 → 2000 · IsSold=False
[휴식]       HP 54 → 106 (+52 = MaxHp 130 × 0.4) · 골드 −60 (35 × (1+0.18×4))
[전환]       런 −100 → 메타 +40
🔴 [대조군]  GoldGain 2.00 으로 올린 뒤에도 메타 **+40 그대로** (80 이면 두 번 곱한 것)
```

🔑 **대조군 둘이 이 작업의 전부다.** 골드가 안 빠지는 것과, 배율이 두 번 안 곱해지는 것.

🟡 **시험을 한 번 오염시켰다** — 휴식을 재려고 `TakeDamage(200)` 했더니 **플레이어가 죽어**
정산이 돌고 골드가 0 이 됐다. 플레이 모드를 껐다 켜고 `MaxHp × 0.6` 으로 다시 쟀다.

### ⚠️ 아이템 칸이 3 → 2 로 줄었다

`shopSlotCount` 는 **3 그대로**인데 한 칸을 휴식이 가져갔다.
**`Economy.csv` 에서 4 로 올리면 아이템 3 + 휴식 1** 이 되지만,
카드가 4장 들어갈 자리가 있는지는 **화면을 봐야 안다** — 지금 건드리지 않았다.

---

## 2-96. ✅ 노드 배치 규칙 — **통계가 결함을 두 번 잡았다** (D70, 2026-09-05 92차)

**한 줄:** 사용자 요구 `5`. 굴림의 단위를 **노드에서 층으로** 올렸다.
그리고 400판 통계가 **내가 만든 결함을 두 번** 잡아냈다.

> 사용자: *"shop 2연속으로 안되거나 타당성있게 노드들이 배치되게 배치 알고리즘 찾아서 선정"*

### 🔑 노드 하나씩 굴리면 규칙을 걸 수 없다

*"이 층에 상점이 있나"* · *"다 같은 종류인가"* 는 **층을 다 보고서야** 답할 수 있는 질문이다.
`RollStageType(layer)` 를 노드마다 부르던 것을 `AssignLayerTypes(층)` 으로 바꿨다.

| 규칙 | 근거 |
|---|---|
| ① 상점은 **연속한 두 층에 못 나온다** | 사용자 요구 그대로 |
| ② **자비** — `shopPityLayers`(4) 층 못 만나면 강제 | 🔴 `D61` 이 9층 도는 동안 상점 **0회**(기대 1.35회) |
| ③ 엘리트는 `eliteMinLayer`(2) 층부터 | 레벨 2~3 이 엘리트를 만나면 갈림길이 아니라 **벽**이다 |
| ④ 한 층이 **전부 같은 특수 노드가 되지 않는다** | 상점 셋뿐인 층은 **갈림길이 아니다** |
| ⑤ 보스 직전 층은 상점을 **선호** (①과 부딪히면 ①이 이긴다) | 마지막으로 채비할 자리 |

🔑 **막힌 종류의 가중치를 0 으로 만들고 남은 것끼리 다시 정규화한다.**
빼기만 하고 정규화를 안 하면 그만큼이 통째로 폴백(노말)으로 흘러
*"엘리트를 막았더니 이벤트도 줄었다"* 가 된다.

### 🔴 통계가 결함을 두 번 잡았다

**1차 (300판)** — 규칙 위반은 전부 0 이었다. **그런데 상점 평균이 4.03(최소 4)이었다.**
너무 많다 싶어 층별로 다시 재 보니:

```
🔴 0층이 노말이 아닌 경우 300 / 300
층별 상점 등장: 0:300  1:0  2:92  3:208 ...
```

**첫 층이 매판 상점이었다.** `RollStageType` 이 0층에 `Normal` 을 돌려주는 것만으로는 부족했다 —
**그 뒤에 오는 강제 규칙이 결과를 갈아 끼운다.** 1차 판정에서 *"0층은 노말"* 을 안 본 게 원인이다.

**2차 (400판)** — 0층은 고쳤고 위반 0. **그런데 `1층: 400/400`.**
시작값을 `-shopPityLayers`("충분히 오래 못 만났다")로 둔 탓에 1층이 바로 자비에 걸렸다.
**매판 같은 자리면 그건 갈림길이 아니라 정해진 길**이고, 1층은 돈이 없어 상점 값어치도 낮다.
⇒ `_lastShopLayer = 0`(*"0층에 상점이 있었다고 친다"*)으로 바꿔 ①이 1층을 막게 했다.

### 판정 — 400판

```
위반 — 0층 비노말 0 · 상점 연속 0 · 이른 엘리트 0 · 전부 같은 특수 0
상점 판당 평균 2.65 (최소 2 · 최대 4) · 0회 판 0
분포   2:165  3:212  4:23
층별   0:0  1:0  2:130  3:88  4:237  5:51  6:152  7:109  8:291  9:0
보스 직전 상점 291/400 (73 %)
```

🔑 **`D61` 의 "9층 0회" 가 400판 중 0건이 됐다.**
🔑 **어느 층도 400/400 이 아니다** — 그게 두 번의 정정이 노린 지점이다.

---

## 2-95. ✅ 상점 가격 스케일 — **모순처럼 보인 요구가 실측 두 개를 가리키고 있었다** (D69, 2026-09-05 91차)

**한 줄:** 사용자 요구 `2`. *"너무 비싸다"* 와 *"비싸지게"* 가 한 문장에 있었는데,
`D61` 의 두 판이 그 둘을 **따로** 가리키고 있었다.

> 사용자: *"상점이 너무 비쌈 (해당 아이템의 현재 레벨, 스테이지 단계에 따라 비싸지는 방식으로)"*

### 🔑 모순이 아니었다 — 초반과 후반이 정반대다

| `D61` 실측 | 남은 런 골드 `M` | 무엇을 말하나 |
|---|---:|---|
| 판1 — 3노드 사망 | **89** | 초반엔 **돈이 없어 못 산다** (`A-1` *"빈손으로 나왔다"*) |
| 판2 — 9노드 완주 | **1168** | 후반엔 **남아돈다** |

**고정가 하나로는 둘 다 못 맞춘다.** 그래서 **기준가를 내리고(×0.6) 층·레벨로 올린다.**

```
가격 = ShopPrice × 0.6 × (1 + 0.35×보유레벨) × (1 + 0.18×층)
```

| 상황 | Sword(70) |
|---|---:|
| 0층 · 미보유 | **42** ← 판1의 `M=89` 로도 두 개 산다 |
| 0층 · Lv2 보유 | **71** |
| 8층 · Lv4 보유 | **246** ← 판2의 `M=1168` 을 여기서 뺀다 |

🔑 **레벨 배율은 *"한 무기만 올리기"* 를 공짜가 아니게 만든다.** 안 가진 것은 레벨 0 이라
배율이 1 — *"처음 사는 건 싸다"* 가 저절로 성립한다.

🔴 **환급도 지금 가격의 절반으로 바꿨다.** 고정가로 두면 깊은 층에서 **비싸게 산 것을 싸게 되팔고**,
초반에는 반대로 산 값보다 비싸게 팔린다.

### 🔴 배선 순서를 읽다 결함을 하나 찾았다 — 상점이 한 층 뒤처진다

처음엔 `LayerScaling.Layer` 를 쓰려 했다. 그런데 그 값은 **`WaveManager.StartWave` 에서만** 갱신된다.

```
GameManager.OnStageNodeSelected
  ├ Normal/Elite/Boss → WaveManager.StartWave(node)  → LayerScaling.Set(node.Layer, ...)
  └ Shop              → ShopMgr.OpenShop(node)        → (아무도 Set 을 안 부른다)
```

⇒ **상점 노드는 웨이브를 안 돌아서, 3층 상점이 2층 가격으로 팔린다.**
지금 들어와 있는 노드(`_currentNode.Layer`)를 직접 본다 — 남의 부수효과에 기대지 않는다.

### 판정 8/8

```
[식]  Sword 70 → 0층 Lv0 42 · 0층 Lv2 71 · 8층 Lv4 246 · 환급 123 · 되돌려 42
[진열] 상점 노드 층=4 · LayerScaling.Layer=0  ← 일부러 0 으로 두었다
       Sword  기본 70 Lv1 → 98  (기대 98)  OK
       Turret 기본 90 Lv0 → 93  (기대 93)  OK
       Crit   기본 60 Lv0 → 62  (기대 62)  OK
```

🔑 **대조군이 `LayerScaling.Layer = 0` 이다.** 상점이 그 전역값을 본다면
층 4 가 반영될 수 없다 — 세 줄이 다 맞았다는 것은 **노드의 층을 직접 본다**는 뜻이다.

### 🟡 `B12` 는 여전히 열려 있다

상점 코드를 만진 김에 다시 적는다 — **칸이 꽉 찬 카테고리의 새 아이템을 사면
골드만 사라지고 성공 로그가 찍힌다.** 가격이 싸진 만큼 **구매가 늘면 터질 확률도 늘어난다.**
고치는 방법 3안은 [`Parallel/BUGS.md`](Parallel/BUGS.md) `B12` 에 있고, **사용자 결정 대기 중**이다.

---

## 2-94. ✅ 진화 특전 3종 + 이속 픽업 — **그리고 `D67` 의 굵기 9 는 안 먹고 있었다** (D68, 2026-09-05 90차)

**한 줄:** 사용자 요구 `12`. 진화가 **수치만 올리던 것**에서 **빌드가 갈리는 것**으로 바뀌었다.
그리고 바로 앞 작업(`D67`)이 **씬 값에 덮여 있던 것**을 여기서 잡았다.

> 사용자: *"진화 능력 너가 적당히 분배해 (투사체 두배, 필드 드랍아이템(무적, 공속증가,
> 이속증가(추가해)) 지속시간 두배, +@)"*

### 나눈 것 — 진화마다 하나씩

| 진화 | 재료 | 특전 | 왜 |
|---|---|---|---|
| **Devastator** | Gun + Fireball | `DoubleProjectiles` | *"터지는 총알"* 이라 탄막이 두 배가 되는 게 가장 맞는다 |
| **Windforce** | Bow + CritChance | `DoubleBuffDuration` | 바람 = 신속. 필드 드랍(무적·공속·이속) 지속 2배 |
| **Excalibur** | Sword + Damage | `DoubleGold` | **+@ 는 내가 골랐다** — 사용자 불만 `2`(상점이 너무 비쌈)와 맞물린다 |

🔑 **결과 무기의 수치와 따로 둔다.** 수치는 `Weapons.csv` 가 이미 올려 주고 있고 그건 *"세졌다"* 로만 읽힌다.
특전은 **어느 진화를 골랐는지가 판 전체에 남는** 쪽이다.

### 새로 만든 것 — 이속 픽업

`PickupKind.Swift` · `Pickup_Swift.prefab`(Haste 를 복제해 연두로) · `PlayerStats.GrantSwift`.
드랍 확률 `0.008` — 공속(0.01)보다 조금 낮다. **이속은 도망에도 쓰여서 체감이 더 크다.**

오라는 `D67` 에 **세 번째 색**으로 얹었다 (연두). 우선순위 **무적 → 공속 → 이속** —
지금 죽지 않는다는 사실이 제일 중요하고, 그다음이 공격, 그다음이 이동이다.

🔴 `enum` 두 개(`PickupKind`·`EvolutionPerk`)를 **뒤에만 늘렸다** (`D51` 과 같은 이유).
정수 직렬화라 중간에 끼우면 **기존 프리팹의 `kind` 가 조용히 밀린다.**

🔑 **투사체 배율을 `WeaponBase` 한 곳에 모았다** (`ScaledProjectileCount`).
근접의 *"연타"* 와 원거리의 *"투사체"* 는 이름이 달라서, 각자 곱하면 **한쪽만 고쳐지는 날이 온다.**

### 판정 6/6

```
[특전 전]     projMult=1  buffMult=1.0  goldGain=1.00  moveSpeed=3.70
[특전 후]     projMult=2  buffMult=2.0  goldGain=2.00
[중복 대조군] goldGain=2.00                      ← 두 번 받아도 3배가 안 된다
활성 투사체   2            ← 활 Lv1(기본 1발)로 실제 발사 수를 셌다
이속          3.70 → 5.70 · 겹쳐 먹은 뒤 5.70   ← 더해지지 않는다
이속 오라     연두 (0.550, 1.000, 0.450)
```

🔑 **투사체는 배율 숫자가 아니라 화면의 발사 수로 셌다.** 검(근접)을 빼고 활만 남겨
*"화면의 투사체 = 활이 쏜 것"* 이 되게 만든 뒤 세는 게 이 판정의 전부다.

### 🔴 `D67` 을 정정한다 — 커밋에 적은 굵기 9 가 실제로는 14 였다

`D67` 에서 *"실제 플레이 배율에서 골라 14 → 9"* 라고 적고 코드 기본값을 9 로 바꿨다.
**그런데 이번 판정에서 읽으니 14 였다.**

`D67` 을 커밋할 때 이미 `auraWidth = 14f` 상태로 한 번 컴파일돼서
**씬이 14 를 직렬화해 들고 있었다.** 코드 기본값은 **새로 붙이는 컴포넌트에만** 먹는다.

⇒ 씬 값을 **9 로 바로잡았다**(`Player` 오브젝트 · 프리팹 인스턴스 아님).

🔑 **정확히 `B11` 이다** — 그 교훈 때문에 색·위치는 코드가 강제하게 만들어 놓고,
**새로 추가한 `[SerializeField]` 하나에서 같은 함정을 밟았다.**
`[SerializeField]` 로 내놓는 순간 정본은 씬이 된다 — 기본값을 고치는 것으로는 안 바뀐다.

---

## 2-93. ✅ 버프 오라 — **타이머를 가르지 않으면 만들 수 없는 기능이었다** (D67, 2026-09-05 89차)

**한 줄:** 사용자 요구 `8`(무적·공속을 먹었을 때 겉모습 변화가 없다).
표시를 붙이려니 **먼저 `PlayerStats` 의 무적 타이머를 갈라야 했다.**

> 사용자: *"무적, 공속증가 등 아이템먹었을때 겉모습 이펙트변화가 없어 알기 어려움.
> 오라색 + 제한시간 되면 점등되는 식으로 알 수 있게"*

### 🔴 왜 타이머부터 갈랐나 — 피해 판정으로는 맞았는데 표시로는 틀렸다

`_invincibleTimer` 하나를 **피격 i-frame 과 픽업 무적이 공유**하고 있었다.
피해 판정만 보면 그래도 맞다 — `TryTakeHit` 은 *"맞을 수 있나"* 하나만 묻기 때문이다.

**그런데 화면에 표시를 붙이는 순간 틀린 설계가 된다.**
합쳐 두면 **적에게 스칠 때마다 0.x초짜리 오라가 번쩍인다** — 그건 *"무적 아이템을 먹었다"* 가 아니라
*"방금 맞았다"* 는 신호다. 정반대다.

| 무엇 | 전 | 후 |
|---|---|---|
| 피격 i-frame | `_invincibleTimer` | `_invincibleTimer` (그대로) |
| 픽업 무적 | 〃 (공유) | **`_buffInvincibleTimer`** (신설) |
| 맞을 수 있나 | `IsInvincible` | `IsInvincible` = 둘 중 하나라도 켜짐 |
| 오라가 보는 것 | — | **`IsBuffInvincible`** (피격은 안 본다) |

🟢 가르고 나니 예전 주석의 *"짧은 피격 무적이 긴 버프를 잘라내지 않게 긴 쪽을 남긴다"* 는
**조심이 통째로 필요 없어졌다** — 애초에 서로 못 건드린다.

### 만든 것 — 새 애셋 0 · 새 오브젝트 0

`VS_LIKE/SpriteOutline` 셰이더에 **외곽선이 이미 있다** (엘리트/보스 표시용).
그걸 빌린다 — 자식 오브젝트를 붙이면 정렬·풀링·바운스를 다 따로 맞춰야 한다.

🔴 **오라를 켜는 순간 `_SpriteRect` 가 필수가 됐다.** 걷기 시트가 4×4 라
그 사각형이 없으면 외곽선이 **옆 걷기 프레임의 알파를 빨아들인다** — 그게 `B1` 이었다.
플레이어는 지금까지 외곽선을 안 써서 없어도 됐던 것이다. `EnemyVisual` 의 계산을 그대로 옮겼다.

🔑 **점등은 굵기가 아니라 알파를 흔든다.** 굵기를 흔들면 실루엣이 커졌다 작아져
**캐릭터가 물리적으로 변한 것처럼** 보인다.

### 판정 6/6

| 무엇 | 결과 |
|---|---|
| 평상시 | 오라굵기 **0** |
| `_SpriteRect` | **(0, 0.75, 0.25, 1.00)** · 폭·높이 **0.25** (4×4 한 칸) · texSize **1024** |
| 픽업 무적 | 굵기 9 · 색 **(0.62, 0.88, 1.00)** 하늘색 |
| 🔴 **대조군 — 피격 무적만** | `IsInvincible=True` · `IsBuffInvincible=False` · **오라굵기 0** |
| 점등 (남은 0.50) | 지금 알파 **0.760** · 0.6초 구간 **0.36~1.00** |
| 공속 | 굵기 9 · 색 **(1.00, 0.68, 0.22)** 주황 |

🔑 **대조군이 이번 작업의 전부다.** 저 줄이 `False`·`0` 이 아니면 타이머를 가른 의미가 없다.

### 🔴 숫자는 통과했는데 그림이 틀렸다 — 굵기 14 → 9

픽셀로는 완벽해 보였다.

```
밝은 픽셀  6805 → 11681 (+71.7 %)
주황 픽셀    90 →  4966 (55.2배)
늘어난 밝은 픽셀 4876 == 늘어난 주황 픽셀 4876   ← 원화는 안 건드렸다는 뜻
```

**그런데 그림을 보니 검이 주황으로 메워져 있었다.** 칼날이 몇 px 뿐이라
양쪽 테두리가 맞닿아 원래 색이 안 보인다 — `B1` 과 같은 종류다.

🔑 **실제 플레이 배율(ortho 6)에서 다시 골랐다.** 1.6 으로 확대해 고르면
게임에서는 안 보이는 굵기를 고르게 된다. `9` 에서 칼날이 은색으로 남고 테두리도 읽힌다.

🔴 `D56`(도감 실루엣)에 이어 **두 번째로, 숫자가 통과한 것을 그림이 잡았다.**

---

## 2-92. ✅ 보스 화살표 · 클리어 화면 — **애셋 0개로 만들었다** (D66, 2026-09-05 88차)

**한 줄:** B 그룹 2건(`13` 보스 오프스크린 화살표 · `15` 클리어 화면 정보).
둘 다 **새 그림도 새 폰트도 안 썼다.**

### 13. 보스가 화면 밖일 때 테두리 화살표

| 파일 | 무엇 |
|---|---|
| `BossArrowGraphic.cs` (신규) | 삼각형 하나를 그리는 `Graphic`. 정점 4개 · 삼각형 2개 |
| `BossOffscreenArrowUI.cs` (신규) | 위치·회전·표시 판정. 화살표 오브젝트를 **런타임에 스스로 만든다** |
| `SampleScene.unity` | `UI Canvas` 에 컴포넌트 1개 |

🔑 **스프라이트도 폰트 글리프도 안 썼다.** 그림을 새로 만들면 애셋이 늘고,
폰트 기호(`▲`)는 **Static 115자 아틀라스**에 없으면 빈칸으로 나온다(`I-60`·`B4`).
삼각형은 정점 4개면 되는 모양이라 `Graphic.OnPopulateMesh` 로 직접 그렸다 — 어느 해상도에서도 안 뭉갠다.

🔑 **보스를 찾아다니지 않는다.** `WaveManager.OnBossSpawned` 를 구독한다 —
`BossHealthBarUI` 와 **같은 이벤트**라 둘이 어긋날 수가 없고, 그래서 **같은 오브젝트에 얹었다.**

🔑 **씬 배선이 0 이다.** 화살표 오브젝트를 코드가 만든다 — 캔버스에 칸을 하나 더 두면
그 칸이 언젠가 비거나(`I-24`) 위치가 어긋난다.

🔴 **카메라 뒤(z<0)를 따로 처리했다.** 2D 직교라 거의 안 나지만, 한 번이라도 나면
화면 좌표가 뒤집혀 **화살표가 정반대를 가리킨다** — 그러면 안 넣느니만 못하다.

#### 판정 3/3 — 두 경계축을 다 밟았다

| 보스 위치 | 화살표 | 기대 |
|---|---|---|
| 우상단 밖 | `pos (791.80, 475.08)` · `30.96°` | **위 테두리**(halfH 475) · 기대각 30.96 ✅ |
| 좌하단 밖 | `pos (-920.13, -207.03)` · `192.68°` | **왼 테두리**(halfW 920) · 기대각 −167.32 ≡ 192.68 ✅ |
| 화면 안 (vp 0.59, 0.58) | `active=False` | 숨어야 한다 ✅ |

🔑 **두 경우가 서로 다른 축에서 잘렸다** — 하나만 맞으면 `min(tx, ty)` 가 틀려도 통과한다.

### 15. 클리어 화면 — 새로 잰 값은 하나뿐이다

```
Layer  1        Level  1
Kills  0        Time  00:10
Biggest hit  771        Earned  0 G

Warrior

Sword Lv2  ·  Bow Lv1  ·  Gun Lv1
```

🔑 **새 계측은 `WaveManager.BiggestHit` 하나다.** 나머지(층·레벨·직업 사슬·보유 아이템)는
이미 굴러다니던 값이다. 성취감은 새로 재는 게 아니라 **이미 있는 숫자를 안 버리는 것**에서 나온다.

🔴 `raw` 가 아니라 **방어를 뺀 `dmg`** 를 넘긴다 — 보여 줄 것은 *"얼마를 쐈나"* 가 아니라
*"얼마가 들어갔나"* 다. 판정에서 `777 → 771`(보스 방어 6)로 확인했다.

🔴 `EnemyBase` 가 `WaveManager` 를 **필드에 캐시하지 않는다** (`I-8`/`I-38`) —
이 스크립트의 `Awake` 는 `GameManager.Start` 보다 먼저 돈다.

#### 🔴 넘침을 실제로 재서 잡았다

상자가 `600 × 180` 이었다. **세 줄짜리 요약에 맞춘 크기**라 아이템 줄이 들어갈 자리가 없다.

```
[최악] 아이템 16종 + 직업 사슬 3단 = 391자
       원본 391 · 렌더 391 · 10줄 · 글자크기 19.3
       렌더높이 229.6  ≤  칸높이 230.0     → 잘림 없음
```

카드(760×500)에서 **제목 아래(+110) ~ 버튼 위(−124) 사이 234px** 가 비어 있다.
상자를 `680 × 230`(y −5)로 넓히고, 자동 크기 15~28pt 를 **코드가 켠다**(`B11`).

🟡 **`preferredHeight` 는 넘침 판정에 못 쓴다** — 자동 크기가 켜져 있으면 `fontSizeMax` 기준으로
나와서 상자를 넓혀도 `233.8` 그대로였다. **렌더된 글자 수와 실제 렌더 높이**로 봐야 맞다.

🟢 `→`(U+2192)·`·`(U+00B7) 둘 다 문자표에 있다 — 391자 전부 렌더되고 **경고 0건**.

---

## 2-91. ✅ 플레이테스트 피드백 1차 — **셋은 고칠 게 없었고, 하나는 주석이 반대였다** (D65, 2026-09-05 87차)

**한 줄:** 사용자 24건을 `C(조사) → A(값이 크고 싼 것)` 순서로 처리했다.
조사 4건 중 **3건은 이미 맞게 되어 있었고**, 1건은 진짜 버그였다.

> `PLAYTEST.md` 의 답 전체 · 사용자 지시 *"푸시하고 진행해"*

### 🔴 16. 캐릭터가 이동 방향과 **반대로** 기운다 — 버그 확정

`PlayerVisual.PushShader` 에 이 줄이 있었다.

```csharp
// flipX 는 메시 x 를 뒤집으므로 기울기 부호도 같이 뒤집어야  ← 🔴 주석이 반대다
target = leanAmount * dir * (_sr.flipX ? -1f : 1f);
```

왼쪽 이동은 `dir = -1` 이고 `flipX = true` 라 **부호가 두 번 뒤집혀 `+leanAmount`** 가 된다.
셰이더는 `pos.x += pos.y * _LeanAmt` 로 **오브젝트 공간**에서 미는데,
`flipX` 는 정점을 뒤집을 뿐 **축은 안 뒤집는다** — 그래서 항상 화면 오른쪽으로 기울었다.

#### 실측으로 갈랐다 — 추론으로 끝내지 않았다

카메라 `cullingMask` 를 **플레이어 레이어 하나로** 줄여 검은 바탕에 실루엣만 남기고,
`위 25 % 줄`과 `아래 25 % 줄`의 가로 중심 차이를 쟀다.

| `_LeanAmt` | flipX | top−bot | 그림 비대칭(±7.52) 뺀 값 |
|---:|---|---:|---:|
| 0 | false | −7.52 | — (대조군) |
| **+0.4** | false | +31.58 | **+39.10** |
| **+0.4** | **true** | +46.10 | **+38.58** |
| **−0.4** | false | −46.09 | **−38.57** |
| **−0.4** | **true** | −31.58 | **−39.10** |

🔑 **네 식이 서로 검산된다.** 기울기 성분의 크기가 38.6~39.1 로 일치하고 부호가 `_LeanAmt` 를 그대로 따른다
⇒ **`flipX` 는 shear 에 관여하지 않는다.** 보정을 제거했다.

🟡 **여기서 캡처를 두 번 헛짚었다.** `Unity_Camera_Capture` 는 씬 뷰를 찍고,
`cam.Render()` 와 `ScreenCapture` 는 **에디터에 포커스가 없어 프레임이 안 돌아** 같은 그림만 세 장 나왔다
(파일 크기가 셋 다 같아서 알았다). `Camera.SubmitRenderRequest` 로 바꿔서 뚫었다.

### ✅ 17·18. 폭파광 / 진화용 캐릭터 — **고칠 게 없다**

```
class=Demolitionist tier=1 promoOnly=False | weapons(1)= Bomb | classesLen=6
```

- 폭파광은 **T1 · Bomb Lv1 한 자루**다 (`ProjectileCount 1|1|1|1|1`). 진화 전용이 아니다.
- `GameManager,classes` 에는 **T1 여섯뿐** — Sentinel·Doomlord·Warden·Aegis 는 없고
  `ClassSelectUI` 가 `IsPromotionOnly`(Tier>1) 로 한 번 더 막는다.

🟡 *"무기가 여러 개로 보였다"* 는 남는다 — 폭파광은 `BonusProjectileSize +0.35` 라 폭발이 크다.
**무엇이 여러 개로 보였는지**는 사람만 답할 수 있다.

### ✅ C-1. Ogre 를 못 봤다 — 버그가 아니라 설계다

`Waves.csv` 전체에서 Ogre 가 나오는 곳은 **한 군데**다.

```
Elite2 ... EliteOverride=Ogre, EliteCount=2, EliteTime=50
```

일반 소환 목록에는 **0회**. 엘리트 노드를 밟고 → 엘리트 2종 중 `Elite2` 가 뽑히고 → 50초를 버텨야 한다.
🔴 **`D51` 의 `Blocker` 행동을 하필 이 적에게 줬다** — 행동 분화가 화면에 거의 안 보인다.

### A 그룹 — 넣은 것

| # | 무엇 | 판정 |
|---|---|---|
| **14** | 보스 처치 = 즉시 클리어 | ✅ 아래 |
| **7** | 레벨업 카드 `1/2/3` 키 | ✅ `Sword(1)` → `Sword(1) Bow(1)` — **딱 한 장** |
| **10** | 경험치 곡선 | ✅ 아래 |
| **4** | 상점 `KILLS/TIME/LEVEL` 제거 | ✅ `Awake` 에서 끈다 |
| **1** | 리롤 가시성 | ✅ 상점 `REROLL 0 G` 노랑 / 레벨업 `REROLL 10 G (x1)` 회색 |
| **C-3** | 안내를 화면 외곽으로 | ✅ 둘 다 `(26, 26)` / `(26, 66)` · `BottomLeft` |
| **B-2** | 후반 적 수 ↑ | 🟢 **CONTENT `C43` 이 이미 했다** (`layerSpawnGrowth 0.17` · `maxAliveCeiling 360`) |

🔑 **색과 위치를 전부 코드에 두었다.** 씬이 정본이면 다음에 캔버스를 만질 때 조용히 돌아온다 —
`B11` 에서 실제로 그랬다.

🔴 `7` 은 `wasPressedThisFrame`(에지)로 받는다. `isPressed`(레벨)면 **다음 레벨업 패널이 같은 눌림에 또 먹힌다** —
44레벨이 쌓이는 판에서는 카드가 통째로 넘어간다. 판정에서 **정확히 한 장만** 들어온 것을 확인했다.

### 10. 경험치통 — 🔴 진짜 원인은 표가 아니라 **Lv11 의 벽**이었다

```
XpToNext = 표마지막(260) + CurrentLevel * 50      ← 예전
```

Lv10 이 `260` 인데 **Lv11 이 `810`** 으로 튄다. 이어지는 곡선이 아니라 **벽**이다.

| XP 예산 | 예전 | 새로 |
|---:|---:|---:|
| 2,600 | **Lv13** | **Lv24** |
| 3,500 | **Lv13** | Lv28 |
| 5,000 | Lv15 | Lv32 |

🔑 **예산을 40 % 늘려도 예전 곡선은 Lv13 에서 안 움직인다.**
그리고 `D61` 이 기록한 판2(9노드 완주)가 정확히 **13레벨**이었다 — **산수와 기록이 맞는다.**

- `xpThresholds` → `5|9|14|20|27|35|44|54|65|77`
- 코드에 박혀 있던 `50` 을 **`xpTailStep` 필드로 빼고 `12`**, 식도 `표마지막 + (레벨 − 표길이) × step`

씬 실측(Import 후): `Lv1=5 · Lv5=27 · Lv10=77 · **Lv11=89** · Lv12=101 · Lv20=197` — 예측과 일치.

### 🔴 D-1(승급 7분) — 지시와 실제가 어긋나 **조건은 안 건드렸다**

> 사용자: *"승급은 무기 만렙 + 건물 lv3부터 진화 가능하게"*

`ClassEvolutions.csv` 는 **이미 그렇다** (`Gun|Turret, 5|3` — 무기 Lv5 = 만렙, 건물 Lv3).
7분이 걸린 건 조건이 아니라 **거기 도달할 레벨업이 안 나와서**다.
⇒ **10번(경험치 곡선)이 D-1 의 실제 처방**이다. 조건 숫자를 같이 내리면
**두 변수를 함께 움직여 무엇이 들었는지 못 가른다** — 한 판 더 해 보고 정한다.

### 14. 보스 처치 = 클리어 — 판정

임시 프로브(`_TempBossClearProbe`)로 0층부터 9층 보스 노드까지 걸어가서 쟀다.

```
보스 노드 도착 · IsBossWave=True · 진행중=True
죽이기 전   진행중=True · 남은=240s · 적=2
[WaveManager] 보스 처치 — 웨이브 즉시 클리어
[GameManager] 런 정산 — 메타 골드 +148
죽인 직후   진행중=False · 상태=Victory
```

🔑 **240초가 남고 적이 2마리 살아 있는데 끝났다** — 그게 요구였다.
그리고 *"웨이브가 끝났나"* 가 아니라 **`Victory` 까지** 갔는지를 봤다.

🟢 프로브는 판정 후 **스크립트·씬 오브젝트 모두 삭제**했다 (`CLAUDE.md` §4).
⚠️ 프로브가 완주해서 `save.json` 에 메타 골드 `+148` 이 들어갔다 —
**정확히 그만큼 되돌렸다** (`1531 → 1383`, 백업 `save.json.d65bak`).

---

## 2-90. ✅ 보스 소환 · 남은 시간 조절 — **`Clear Node` 와 엮지 않았다** (D64, 2026-09-05 86차)

**한 줄:** `D63` 에서 내가 *"`Clear Node` 가 보스를 건너뛴다"* 고 했는데 **그것도 틀렸다.**
사용자가 보스 라운드에서 **보스를 봤다.** 건너뛴 건 사용자가 그 판에서 버튼을 눌러서다.

> 사용자: *"B인데 보스 라운드에서 보스 볼 수 있엇어 내가 clear node를 눌러 너가 그렇게 판단한 거 같아.
> 그냥 보스 소환 기능만 추가해서 따로 확인만 할 수 있게 / 라운드 남은 시간도 조절할 수 있게"*

### 🔴 또 로그로 의도를 추측했다

`D63` 은 5판째 로그에 `Bonecaller` 가 없는 것을 보고 *"강제 클리어가 보스 소환보다 먼저 온다"* 고 진단했다.
**로그는 맞았고 원인이 틀렸다** — 그 판에서 보스가 안 나온 이유는 코드 순서가 아니라
**사용자가 보스 시간(`BossTime`) 전에 버튼을 눌렀기** 때문이다.

🔑 `D62`(플레이 의도를 몰라서) · `D63`(같은 데이터를 두 번) 에 이어 **세 번째다.**
셋의 공통점은 하나다 — **사람이 무엇을 눌렀는지가 로그에 안 남는다.**

🟢 그래서 이번 설계는 그 자리에서 갈랐다 — **넘어가는 일(`Clear Node`)과 보는 일(`Spawn Boss`)을 섞지 않는다.**
사용자 지시도 정확히 그것이었다.

### 만든 것

| 파일 | 무엇 |
|---|---|
| `WaveManager.cs` | `DevSpawnBoss()` · `DevSetRemainingTime(float)` · `HasWaveTimer` — 전부 `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 안 |
| `WaveManager.cs` | `TimerRoutine` 의 지역 변수 `remaining` → **필드 `_timerRemaining`** |
| `DevPanel.cs` | `Spawn Boss` 버튼 + `Wave timer` 줄(`-30s` `-10s` `+30s` `5s left`) |

🔑 **보스 소환은 정규 경로와 같은 세 줄을 쓴다** — `SpawnEnemy(isBoss:true)` → 등장음 → `AttachBossBrain`.
하나라도 빼면 **패턴 없는 큰 잡몹**이 나와서 *"확인"* 이라는 목적 자체가 무너진다.

🔑 **보스 노드가 아니어도 부를 수 있다.** `_currentWaveData.BossOverride` 가 비면 `bossWave` 것으로 대신한다 —
1층에서도 보스를 볼 수 있어야 *"따로 확인만"* 이 성립한다.

🔴 **지역 변수를 필드로 바꾼 게 핵심이다.** `WaveRemainingTime` 만 고쳐도 **코루틴 안의 `remaining` 이 다음 프레임에 덮어쓴다** —
표시만 잠깐 바뀌고 아무 일도 안 일어난다.

🔴 **킬 목표 웨이브에서는 거절한다.** 그쪽은 `TimerRoutine` 이 아예 안 돌아
`WaveRemainingTime` 이 `-1` 이다. 값을 넣으면 **HUD 에 없던 타이머가 생기고 클리어 조건과 어긋난다.**
`DevPanel` 이 버튼을 막고, `WaveManager` 가 한 번 더 거절한다.

🔴 **슬라이더를 안 썼다.** 드래그하는 동안 매 프레임 값을 밀어 넣어서 **손을 뗄 때까지 시간이 안 흐른다.**

### 판정 — 5/5

```
timer  47.0 → 17.0            ← 조절이 먹었다
그 뒤  17.0 → 1.5             ← 🔑 얼지 않고 계속 흐른다
alive  19 → 20                ← 보스가 상한과 무관하게 들어왔다
boss   sprite=Bonecaller · scale 3.20 · isBoss=True · BossBrain=True
경고   0건                     ← BossPattern 이 붙었다 (없으면 LogWarning 이 뜬다)
```

🔑 **"조절됐다"로 끝내지 않고 "그 뒤에도 흐르나"까지 봤다.**
필드로 바꾸는 수정이라 **얼어붙는 게 가장 그럴듯한 실패 방식**이었다.

ℹ️ 오브젝트 이름이 `Enemy_Goblin(Clone)` 인 건 **풀 프리팹을 공유해서**다 — 정규 보스 경로도 같다.
그림·크기는 `EnemyData` 가 덮으므로 `sprite=Bonecaller · scale 3.20` 으로 확인했다.

🟡 **화면은 또 못 봤다** (`D63` 과 같다). 백틱 주입으로 Dev 패널이 안 열려서
**동작은 검증됐고 렌더링은 안 됐다.** 사용자가 백틱을 눌러 확인해야 한다.

---

## 2-89. ✅ Dev 패널에 노드 클리어 — **그리고 "구매 0" 이 상점 탓이 아니었다** (D63, 2026-09-05 85차)

**한 줄:** 사용자가 *"9라운드로 빠르게 가려고 스킵한 것"* 이라고 알려 줬다.
⇒ `D62` 의 진단이 또 틀렸다. 그리고 그 스킵을 **버튼 하나로** 만들었다.

> 사용자: *"개발자용 콘솔에 해당 노드 클리어 추가해서 빠르게 보스 라운드까지 갈 수 있게 해줘"*

### 🔴 `D62` 를 정정한다 — "만나도 안 산다"가 아니었다

`D62` 는 3판 동안 상점 3회 열고 **구매 0** 인 것을 보고
*"못 만나는 게 아니라 만나도 안 산다"* 로 진단을 옮겼다. **그것도 틀렸다.**

**사용자는 9층까지 빨리 가려고 상점을 의도적으로 건너뛴 것이다.**
⇒ **"구매 0" 은 상점 설계의 증거가 아니라 플레이 방식의 부산물이다.**

🔑 **같은 데이터를 두 번 잘못 읽었다.** 처음엔 판 하나로(`D61`), 다음엔
**플레이 의도를 모르고**(`D62`). 로그는 *무엇을 했는지*는 말해 주지만
***왜 했는지***는 말해 주지 않는다 — 그 자리를 내가 추측으로 메웠다.

🟢 **다만 `B12` 는 그대로 유효하다.** 그건 코드를 읽어서 확정한 것이지
플레이 기록에서 추론한 게 아니다. 근거가 다르면 정정도 따로 간다.

### 만든 것 — `Clear Node` 버튼

병목은 난이도가 아니라 **웨이브 타이머(60~100초)** 다. 그것만 건너뛴다.

| 파일 | 무엇 |
|---|---|
| `WaveManager.cs` | `DevForceClearWave()` — `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 안 |
| `DevPanel.cs` | `DrawStageRow()` — 층/배율 표시 + `Clear Node` 버튼 |

🔑 **`ClearWave()` 를 그대로 부른다. 흉내 내지 않는다.**
보상 지급·이벤트 효과 해제·스테이지 진행이 전부 그 안에 있어서,
따로 구현하면 **정상 클리어와 다른 상태**가 된다. 특히 `GameManager.OnWaveCleared` 를
건너뛰면 맵이 안 넘어가 버튼이 무의미해진다.

🔴 **릴리즈 빌드에 치트 입구를 안 남긴다** — `DevPanel` 이 파일 전체를 `#if` 로 감싸는 것과
같은 조건을 진입점에도 걸었다.

🔴 **웨이브일 때만 눌린다.** 상점·이벤트 노드는 각자 닫는 경로가 따로 있어서
여기서 흉내 내면 그쪽 상태가 어긋난다 — 버튼을 막고 지금 상태를 옆에 적는다.

### 판정 — 동작은 끝까지 봤다

```
누르기 전  상태 Wave · 진행중 True · 남은 51초 · 적 5
누른 직후  진행중 False · 적 0
그 뒤      상태 StageMap · 메타 골드 대기 8   ← 클리어 보상이 들어왔다
```

🔑 **"클리어됐다"로 끝내지 않고 "맵이 넘어갔나"까지 봤다.**
적만 사라지고 맵이 안 넘어가면 버튼이 있으나 마나다.

🟡 **화면은 못 봤다.** 백틱을 주입해도 Dev 패널이 안 열려서(입력 주입의 한계)
버튼이 *그려지는지*는 확인하지 못했다. **동작은 검증됐고 렌더링은 안 됐다** —
사용자가 백틱을 눌러 확인해야 한다. IMGUI 레이아웃이 어긋나면 콘솔에 요란하게 뜬다.

### ⚠️ 시험이 로그 감시를 오염시켰다

내 시험 판이 `런 정산 — 메타 +8 · 남은 17` 을 찍어서 **감시가 그걸 사용자 판으로 잡았다.**
메타 골드가 방금 내가 만든 클리어 보상(8)과 같아서 구분됐다.
⇒ 앞으로 감시가 걸린 동안에는 **내가 런을 시작하지 않는다.**

---

## 2-88. 🔴 판 3개가 모이니 그림이 바뀌었다 — **상점을 3번 열고 0건 샀다** (D62, 2026-09-05 84차)

**한 줄:** 세 번째 판(`M=465`)이 들어오면서 원인이 *"상점을 못 만난다"* 에서
*"만나도 안 산다"* 로 옮겨졌다. 그리고 그 자리에서 **버그를 하나 찾았다** (`B12`).

> `PLAYTEST.md` `A-0` · `B12` → [`Parallel/BUGS.md`](Parallel/BUGS.md)

### 세 판

| | 판 1 | 판 2 | 판 3 |
|---|---:|---:|---:|
| 직업 | Warrior | Warrior | **Demolitionist** |
| **종료** | 사망 | 🔴 **완주 (Victory)** | 사망 |
| 전투 노드 | 3 | 9 | 9 |
| **상점 열림** | 1회 | 0회 | **2회** |
| 🔴 **구매 / 리롤** | **0 / 0** | — | **0 / 0** |
| 레벨업 | 7 | 13 | 10 |
| `M` | 89 | **1168** | **465** |

### 🔴 앞 보고를 두 군데 고친다

1. **판 2 는 사망이 아니라 완주(`Victory`)였다.** 정산 로그가 상태 전이보다 **먼저** 찍혀서
   슬라이스 첫 줄을 앞 판의 종료 상태로 읽었다. 정산 줄 **다음** 전이를 봐야 맞다.
   ⇒ `M=1168` 은 *"운 좋게 오래 산 판"* 이 아니라 **정식 완주 기록**이다. 결론이 더 세졌다.
2. **원인 진단이 좁았다.** 판 2 만 보고 *"상점 노드를 못 만나서"* 라고 했는데,
   판 3 은 **두 번 열고도 0건**이다. 노드 확률만 올려서는 안 풀린다.

### 🔴 그 자리에서 버그가 나왔다 — `B12`

*"만나도 안 산다"* 를 파다가 상점 구매 경로를 읽었다. **`CanAcquire` 가 상점 어디에도 없다.**

| 곳 | 지금 |
|---|---|
| `RollShopSlots` | 못 받는 아이템도 진열한다 |
| `ShopCardUI.cs:80` | 버튼이 **골드만** 본다 |
| `ShopManager.Purchase` | 골드를 **먼저** 빼고 → `ApplyItem` 이 조용히 거절 → **성공 로그를 찍는다** |

⇒ 칸이 꽉 찬 카테고리의 새 아이템을 사면 **골드만 사라지고 로그는 정상**이다.

🔑 **레벨업 경로는 안 걸린다** — 카드 풀이 `CanAcquire` 로 미리 거르기 때문이다(`LevelUpManager:263`).
**상점만 그 방어 없이 같은 `return` 을 공유한다.**

⚠️ 이번 3판에서는 **안 터졌다** — 구매가 0건이라 경로를 아무도 안 밟았다.
코드로 확정했고 **고치지 않고 등재만 했다** (`CLAUDE.md` §1).

### 🟡 아직 사람만 답할 수 있는 것

상점을 세 번 열고 왜 한 번도 안 샀는지는 **로그가 말해 주지 않는다.**
`Purchase` 의 거절 경로에 로그가 없어서 *"안 눌렀다"* 와 *"눌렀는데 거절됐다"* 도 구분이 안 된다.
`PLAYTEST.md` `A-1` 이 그 질문이다.

---

## 2-87. 🔴 `M = 1168` — **판 하나로 뒤집은 게 성급했다** (D61, 2026-09-05 83차)

**한 줄:** 사용자가 두 판을 돌렸고 `Editor.log` 감시로 종료 정산을 그대로 잡았다.
`C40` 의 `1-a` 가 **실패**로 닫혔다 — 그리고 그 사이에 **내가 중간 보고를 한 번 틀렸다.**

> `PLAYTEST.md` `A-0` · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-44**

### 감시 방법 — MCP 없이도 됐다

Unity 는 `Debug.Log` 를 `%LOCALAPPDATA%/Unity/Editor/Editor.log` 에 그대로 쓴다.
`tail -F | grep -m1 "런 정산"` 을 백그라운드로 걸어 두면 **판이 끝나는 순간 깨어난다.**
콘솔 MCP 가 끊겨 있어도 동작한다.

### 두 판 — 같은 직업인데 13배 갈렸다

| | 판 1 | 판 2 |
|---|---:|---:|
| 직업 | Warrior · Sword Lv1 | Warrior · Sword Lv1 |
| 전투 노드 | 3 | **9** |
| **상점 노드** | 1회 (**구매 0** · 리롤 0) | 🔴 **0회** |
| 이벤트 노드 | 0 | 2 |
| 레벨업 | 7 | 13 |
| 승급 | — | Sentinel |
| 종료 | 사망 | 사망 |
| 🔴 **남은 런 골드 `M`** | **89** | **1168** |

### 판정 — 🔴 실패. 통과선 300 의 **3.9배**

짧게 죽은 판(89)이 통과한 건 **수입이 안 쌓인 것**이지 배수구가 도는 게 아니다.
**게임이 정상적으로 굴러간 판이 1168 이다.**

### 🔑 원인이 로그에 그대로 있다 — 배수구를 **지나지도 못했다**

판 2 는 **9노드를 도는 동안 상점이 한 번도 안 나왔다.**
`weightShop 0.15` 면 9노드 기대값이 **1.35회**인데 **0회**다.
판 1 은 만났지만 **아무것도 안 샀다.**

⇒ `C40` 이 미리 적어 둔 가지가 정확히 맞는 자리다 —
`weightShop 0.15 → 0.28` · `shopSlotCount 3 → 4`.

### 🔴 내가 중간에 틀린 말을 했다 — **표본 하나로 일반화**

판 1(3노드 사망)만 보고 *"배수구가 아니라 **수입이 적다**"* 고 보고했다. **틀렸다.**
판 2 의 노드당 수입은 **1168 / 9 ≈ 130 G** 로,
`D58` 에서 CSV 로 계산한 **Normal2 = 134 G** 와 거의 그대로 맞는다.

🔑 **산수는 맞았고 표본이 하나였다.** 죽어서 끝난 판은 *"경제가 어떻게 도는가"* 의
표본이 아니라 *"얼마나 못 살아남았는가"* 의 표본이다. 같은 이름의 숫자가 다른 것을 재고 있었다.

### 🔴 그리고 도구에서 같은 사고를 두 번째로 냈다

문서를 고치는 파이썬이 `open(path,'wb').write(t.encode('utf-8'))` 였다.
파이썬은 **`open` 을 먼저 평가**하므로 파일이 잘린 뒤에 인코딩이 실패한다 —
**`SETUP_STATUS.md` 663 KB 가 0바이트가 됐다.** `git checkout` 으로 복구했다(마지막 커밋 이후
변경이 없어 손실 0). `D54` 에서 같은 함정을 겪고 *"encode 를 먼저"* 로 바꿨는데,
**한 줄짜리 관용구에서 다시 새어 나왔다.**

⇒ 이제 도우미 함수 안에 주석으로 못박았다. 🔑 **규칙을 아는 것과 매번 지키는 것은 다르다** —
지켜지게 하려면 규칙이 *지나가는 자리*가 아니라 *거쳐야 하는 자리*에 있어야 한다.

### 🟡 곁일 — 승급 로그가 스스로를 가리킨다

```
[EvolutionManager] 직업 승급 — Sentinel → Sentinel
```

`EvolutionManager.cs:327` 이 `{evo.EvolutionName} → {evo.ResultClass.ClassName}` 를 찍는데
레시피 이름이 결과 직업과 같아 **같은 말이 두 번** 나온다. *"어디서 올라왔는지"* 가 안 보여
로그로 승급 사슬을 못 따라간다. **동작 결함이 아니라 고치지 않고 알린다** (`CLAUDE.md` §1).

---

## 2-86. ✅ 실플레이 체크리스트 + 완성률 — **문서가 3주째 낡아 있었다** (D59, 2026-09-04 82차)

**한 줄:** 사용자가 채울 [`PLAYTEST.md`](PLAYTEST.md) 를 만들고, 완성률을 **축을 갈라** 냈다.
그 과정에서 `ROADMAP` §0 현황표가 **2026-08-28 값 그대로** 방치돼 있던 걸 찾아 다시 셌다.

### 🔴 문서가 실제와 어긋나 있었다

`ROADMAP` §0 *"확인된 현황 수치"* 표를 애셋 폴더로 검산했더니 **거의 모든 줄이 틀렸다.**

| 항목 | 문서 | 실제 |
|---|---:|---:|
| 적 종류 | 6 | **7** |
| 적 행동 | 3 | **6** |
| 무기 | 5 | **12** |
| 아이템 / 패시브 | 23 / 10 | **28 / 11** |
| 이벤트 | 5 | **8** |
| 오디오 | 18 | **20** |
| 픽업 | 4 | **7** |

🔑 **표가 갱신 대상이라는 걸 아무도 안 적어 뒀다.** 각 작업이 §7(볼륨 표)은 고쳤는데
§0(요약 표)은 안 고쳤다 — 같은 사실이 두 곳에 적혀 있었던 탓이다.
이번에 §0 을 **실측 표**로 바꾸고 "재실측" 날짜를 박았다.

### 완성률 — 한 숫자보다 축이 중요하다

| 축 | 상태 | 가중치 | 근거 |
|---|---:|---:|---|
| 기능 / 시스템 | **95 %** | 45 % | `ROADMAP` §8 의 1~4단계 완료, 5단계는 게임패드만 남았고 그건 **사용자가 안 하기로** 함 |
| 콘텐츠 물량 | **70 %** | 35 % | §7 아홉 항목 중 다섯이 목표 도달·초과. 모자란 넷 = 적 7/12 · 보스 1/3 · 상위 직업 4/8 · 스테이지 테마 |
| 체감 검증 | **3 %** | 20 % | `TUNING.md` 115개 중 확인 3개 |

⇒ **68 %.** 🔑 **못 만든 게 아니라 안 만져 본 것이 남았다.**

⚠️ 포트폴리오(시각화 PDF · 마감 9/7) 기준으로는 체감 검증 축의 무게가 훨씬 작아
**85 % 대**로 읽어야 한다. 문서는 오히려 과잉이다 — **85절 · 141커밋 · 코드 13,512줄.**

### `PLAYTEST.md` — 세션이 묻고 사용자가 답한다

`C40` 요청-32 중 **사람이 해야만 판정되는 것**만 남겨 체크박스로 뽑았다.
숫자로 닫힌 6개(`D58`)는 뺐다 — 이미 답이 있는 걸 또 묻지 않는다.

🔑 **A-0 하나만 채워도 값이 있다** — 종료 로그의 `남은 런 골드 M` 한 줄이면
`C27` 이 10배로 올린 상점 물가가 맞는지 아닌지가 결정된다.

🔴 **소유를 문서 첫 줄에 박았다** — *"이 문서는 사용자가 채우는 곳"*.
세션이 답을 대신 적으면 검증이 아니라 창작이 된다.

### ✅ 곁일 — `C41` 이 되물은 "Ultra 냐 Medium 이냐"에 답했다

`D58` 이 *"품질이 Medium 이다"* 라고 했고 `C41` 이 애셋을 열어 *"둘 다 Ultra(5) 다"* 라고 되물었다.
**둘 다 맞았다.** 런타임에서 확인했다:

```
품질 목록  0:Very Low 1:Low 2:Medium 3:High 4:Very High 5:Ultra
지금 레벨  2 (Medium)
PlayerPrefs "gfx_quality" = 2
```

⇒ **인덱스 오독이 아니라 런타임 변경이다.** `D47` 이 넣은 `DisplaySettings.ApplySaved()` 를
`GameManager.Awake` 가 부르면서 **저장된 옵션이 애셋 기본값을 덮는다.**
애셋 기본은 `Ultra`, 이 기기의 저장값은 `Medium` 이다.

🔴 **그래서 `2-a` 의 111 fps 는 Medium 에서 잰 값이다.**
갓 설치한 사람은 `Ultra` 로 시작하므로 그 수치가 아니다 — `TUNING.md` §4 에 조건으로 달아야 한다.

---

## 2-85. 🟡 C40 실플레이 검증 — **숫자로 닫히는 것부터 닫았다** (D58, 2026-09-04 81차)

**한 줄:** 17개 중 **6개를 숫자로 닫았고**, 경제(§1)는 **자동화가 불가능한 이유**를 찾아
예측치를 붙여 사람에게 넘긴다. 코드·애셋 변경 0.

> `REQ/DEV.md` **요청-32** (C40) 처리 · 회신 `REQ/CONTENT.md` **요청-42**

### ✅ 숫자로 닫은 것

| # | 결과 |
|---|---|
| **0-a** | 🔴 **vSync 가 켜져 있었다** (`vSyncCount 1` · 144 Hz). CONTENT 예측대로였다. 측정 동안 껐다. ⚠️ 다만 품질은 `Ultra` 가 아니라 **Medium** 이다 |
| **2-a** | ✅ **294마리에서 111.3 fps (8.99 ms)** — 통과선 60 fps. 측정 창 9.61초 동안 적이 294 로 유지됐다(부하가 안 빠졌다) |
| **2-h** | ✅ HP 바 켜짐 · 이름 **`BONECALLER`** · ★★ · `BossBrain` 붙음 · HP **2926**(9층 = 1540 × 1.90) |
| **3-a** | ✅ 내 탄 vs 적 탄 — **색상각 170도**(청록 H197 vs 적색 H7) + **밝기 차 0.383**. 두 축이 동시에 갈린다 |
| 2-c | ✅ 이미 `D53` 에서 **dL 0.0727** (통과선 0.04) |
| 2-g | 🟡 이미 `D53` 에서 **층9 얼룩 0.0211** 측정 → 요청-41 로 넘겨 둠 |

### §0-b 는 숫자 대신 논리로 닫았다

`EditorLoop` 비중은 `ProfilerDriver` 가 이 실행 샌드박스에서 안 잡혀 못 쟀다.
**대신 그게 필요 없다** — 2-a 의 111 fps 는 **에디터 오버헤드가 포함된** 값이다.
`EditorLoop` 는 빌드에 없으므로 빌드는 그보다 **빠를 수만 있다.** 통과 판정은 뒤집히지 않는다.

### 🔴 §1(경제)이 자동화가 안 되는 이유를 찾았다

한 판을 자동으로 돌려 종료 로그의 `남은 런 골드 M` 을 읽으려 했는데 **20초 동안 한 칸도 안 움직였다.**
버그가 아니었다 — **레벨업 패널이 떠서 `timeScale 0`** 이었다.

⇒ 런은 레벨업마다 **카드 선택에서 멈춘다.** 그리고 그 선택이 빌드를 정하고,
빌드가 처치 속도를 정하고, 처치 속도가 골드를 정한다.
**자동으로 고르면 `M` 이 아니라 "아무 M" 이 나온다.** 그래서 사람이 해야 한다.

### 🟡 대신 예측을 붙였다 — **`M ≈ 600` 으로 본다** (통과선 300 의 2배)

전제부터 확인했다: 골드는 `EnemyBase.Die → GameManager.GrantGold(CurrencyDrop)` 로
**처치마다** 들어온다(픽업 전용이 아니다).

| | 값 |
|---|---|
| 웨이브별 골드(모두 처치 시) | Normal1 **66** · Normal2 **134** · Normal3 **234** · Elite1 **204** · Elite2 **240** |
| 전투 8노드 + 보스, 층 배율 포함 이론 최대 | **1757 G** |
| 실측 처치당 골드 (0층, 정지 상태) | **1.65 G** |
| 배수구 — 상점 1.2회 × 3칸 × 평균가 67G | **≈ 241 G** (+리롤 25·40) |

⇒ 절반만 잡아도 수입이 배수구의 **3배**를 넘는다. `weightShop 0.15 → 0.28` ·
`shopSlotCount 3 → 4` 가지가 열릴 가능성이 높다. **한 판의 로그 한 줄이면 확정된다.**

### 🔴 사람이 해야 하는 것 — 9개

1-b·1-c·1-d·1-e(경제 체감) · 2-b(내려찍기 회피) · 2-d(뱀서 느낌) · 2-e(겹침) ·
2-f(승급 연출) · 3-b·3-c(행동·등장 구분) · 3-d(난전 프롬프트).
**전부 "느낌이 맞나"** 라 로그로 못 만든다.

---

## 2-84. ✅ 도감 닫기를 우측 상단 X + ESC 로 — **ESC 가 안 먹는 줄 알았는데 시험이 먹고 있었다** (D57, 2026-09-04 80차)

**한 줄:** 하단 `CLOSE` 를 빼고 우측 상단 `X` + `ESC` 로 옮겼다. 격자·상세가 **60px 씩 넓어졌다**.

> 사용자: *"하단 close 빼고 우측 위 X 나 ESC로 끌수 있게 해서 공간 넓혀"*

### ESC 를 그냥 붙여도 되는지부터 확인했다

`PauseMenuUI` 도 ESC 를 읽는다. 부딪히면 도감을 닫으면서 일시정지가 같이 열린다.
읽어 보니 `CanPause(s) => s == GameState.Wave` 라 **메인 메뉴에서는 아무 일도 안 한다.**
도감은 메인 메뉴에서만 열리므로 겹치지 않는다.

🔴 **다만 그 전제를 코드 주석에 못박았다** — *"도감을 전투 중에 열 수 있게 만들면 이 전제가 깨진다"*.
그때는 같은 프레임에 도감이 닫히고 일시정지가 열린다.

### 🔴 ESC 가 안 닫힌다고 나왔다 — 코드가 아니라 시험이 문제였다

첫 판정에서 `ESC 뒤 열림 = True` 가 나왔다. X 는 되는데 ESC 만 안 됐다.

원인은 내 시험 방식이다. `wasPressedThisFrame` 은 **엣지**인데,
키를 큐에 넣고 **`InputSystem.Update()` 를 직접 불렀다.**
그 수동 업데이트가 `MonoBehaviour.Update` 가 돌기 전에 **엣지를 소모**해 버린다.

⇒ 큐에만 넣고 플레이어 루프가 처리하게 두니 **닫혔다.**

🔑 `D51` 에서는 같은 방식이 통했는데, 그건 `isPressed`(레벨)를 읽는 이동 입력이라
엣지가 소모돼도 상태가 남아 있었기 때문이다. **엣지 입력에는 그 방식을 쓰면 안 된다.**

### 판정 4/4

```
하단 CLOSE 제거      True
우측 상단 X 로 닫힘   ✅
ESC 로 닫힘          ✅ (큐에만 넣고 플레이어 루프가 처리)
대조군: ESC 로 일시정지가 안 열림   상태 MainMenu 유지 ✅
격자 높이 664 -> 724 (+60)
콘솔 0
```

---

## 2-83. ✅ 도감 가시성 — **밝게 칠한 실루엣은 실루엣이 아니었다** (D56, 2026-09-04 79차)

**한 줄:** 사용자 스크린샷에서 도감이 통째로 어두웠다. 고치다가 **정답을 새게 하는 실수**를 하고
다시 뒤집었다. 실루엣 대비 **0.147 → 0.453 (3.1배)**.

> 사용자: *"가시성이 너무 낮아 개선해"* (스크린샷 첨부)

### 원인은 둘이었다

| | 실측 |
|---|---|
| 어두운 칸 위의 **검은** 실루엣 | 칸 배경 `0.151` · 실루엣 `0.004` — 둘 다 바닥에 붙어 형체가 안 잡힌다 |
| 🔴 **글자가 11px 로 그려진다** | `CanvasScaler` 가 1920 기준인데 사용자 Game 뷰가 **856×498** ⇒ `scaleFactor 0.453`. 본문 25pt → **11.3px** |

두 번째는 짐작이 아니라 `canvas.scaleFactor` 를 읽어 확인했다.

### 🔴 여기서 크게 틀렸다 — 밝게 칠하면 실루엣이 안 된다

*"검은 실루엣이 안 보이니 실루엣을 밝게 하자"* 로 갔다. 그런데
<c>Image.color</c> 는 **곱셈**이다. 밝은 회색을 곱하면 형체만 남는 게 아니라
**원색이 조금 흐려질 뿐**이다.

⇒ 0/12 발견 상태에서 **무기 12종이 전부 알아볼 수 있게 떴다. 정답이 새어 나갔다.**
`???` 로 이름을 가리는 의미가 통째로 사라진 것이다.

**캡처를 안 봤으면 "가시성 개선"이라고 보고했을 것이다.** 숫자(대비)는 오히려 좋아졌기 때문이다.

### ✅ 그래서 반대로 뒤집었다 — 실루엣은 검정, **칸을 밝게**

곱셈으로 형체만 남기려면 색이 `0` 이어야 하고, 그러면 **바탕이 밝아야** 보인다.

| | 칸 배경 | 아이콘 |
|---|---|---|
| **미발견** | **밝은 회색 `0.62`** | 검정 (실루엣) |
| 발견 | 어두움 `0.24` | 원색 |
| 고른 칸 | 금색 | 그대로 |

명암이 통째로 뒤집히므로 **발견/미발견이 한눈에 갈린다** — 색을 못 봐도 구분된다.
상세창의 아이콘 판(`DetailIconBG`)도 같은 규칙으로 뒤집었다.
안 그러면 **칸에서는 보이는데 상세에서는 사라지는** 꼴이 된다.

### 같이 키운 것

칸 `108 → 132px` · 본문 `25 → 32pt` · 제목 `46 → 52` · 조건 줄 `34 → 40` ·
탭 라벨 `26 → 30` · 카드 명도 `0.13 → 0.20`.

### 🔴 글자를 키웠더니 상자를 뚫었다

본문 길이가 항목마다 다르다 — 패시브는 수치가 12줄, 적은 5줄이다.
32pt 로 고정하니 **직업 본문이 상자를 넘쳐 `CLOSE` 버튼을 덮었다.**
⇒ 자동 축소(20~32pt)로 맡기고 **네 종류를 실측**했다:

```
무기        27.9pt · 필요 388 / 칸 388 · 상자 안  ✅
패시브      32.0pt · 필요 242 / 칸 388 · 상자 안  ✅
직업        20.0pt · 필요 380 / 칸 388 · 상자 안  ✅   <- 가장 길다
적(미발견)  32.0pt · 필요 242 / 칸 388 · 상자 안  ✅
```

🔑 판정을 *"넘쳤나"* 가 아니라 **"필요한 높이 ≤ 칸 높이 이면서 상자 바닥을 안 뚫었나"** 로 잡았다.
눈으로만 보면 한 항목만 확인하고 넘어가게 된다.

### 판정 5/5

```
실루엣 대비   0.1468 -> 0.4533  (3.1배) · 밝기 비 11.3 -> 46.3배
정답 누설     0/12 에서 무기 종류를 못 알아본다 (캡처로 확인)
발견/미발견   명암이 뒤집혀 한눈에 갈린다
본문 넘침     4종 전부 상자 안 ✅
콘솔          0
```

---

## 2-82. ✅ 도감을 아이콘 격자로 다시 짰다 — **이름은 감추고 윤곽은 준다** (D55, 2026-09-04 78차)

**한 줄:** `D54` 는 글자 목록이었다. 사용자 스케치대로 **실루엣을 누르는 격자**로 바꿨다.
코드는 `CodexPanel` 하나만, 씬은 왼쪽 영역만 다시 지었다.

> 사용자 요구: *"이런 느낌으로 각 아이콘의 실루엣을 누르는 방식으로 원해"* (스케치 첨부)

### 무엇이 달라졌나

| | `D54` (목록) | `D55` (격자) |
|---|---|---|
| 고르는 것 | 글자 한 줄 | **아이콘 실루엣 한 칸** |
| 미발견 표시 | 이름 `???` + 작은 실루엣 | 이름 `???` + **칸을 채우는 실루엣** |
| 진화 조건 | 본문 안 한 줄 | **아이콘 아래 전용 줄**(금색, 34pt) |
| 왼쪽 폭 | 620 (세로 목록) | 880 (7열 격자) |

### 🔑 가리는 방향이 두 개다 — 이름은 감추고 윤곽은 준다

`???` 만 보여 주면 *"뭔가 있다"* 밖에 모른다.
칸을 채우는 실루엣은 **모양**을 주므로 검이랑 활이 구분된다.
`??? + ???` 로 조합의 개수를 알려 주는 것과 같은 성격이다 — **정답은 감추고 단서는 준다.**

⇒ 미발견도 `Icon` 을 **넣고** 검게(알파 0.85) 칠한다. `D54` 는 아이콘이 40px 라
   실루엣이 사실상 안 보였는데, 격자에서는 칸이 108px 라 그게 본체가 됐다.

### 그림이 없는 칸을 대비했는데 — 쓸 일이 없었다

직업은 `Icon` 이 아니라 `Portrait` 라 빠질 수 있어서, 그림이 없으면
이름 몇 글자를 대신 띄우는 예비 경로를 넣었다. 실측 결과:

```
WEAPONS   칸 12 · 그림 12 · 글자로 떨어진 칸 0
ITEMS     칸 16 · 그림 16 · 0
EVOLUTION 칸  7 · 그림  7 · 0
CLASSES   칸 10 · 그림 10 · 0      <- 초상화가 10개 다 있다
ENEMIES   칸  7 · 그림  7 · 0
```

**52칸 전부 그림이 있다.** 예비 경로는 안 쓰이지만 지우지 않았다 —
새 직업/적을 넣고 그림을 아직 안 만든 동안 칸이 통째로 비어 보이는 게 더 나쁘다.

### 판정 6/6

```
격자         5탭 전부 칸 수가 목록과 일치 (12·16·7·10·7)
실루엣       미발견은 검게, 발견은 원색 — 한 화면에 섞여 보이는 것을 캡처로 확인
선택 표시    고른 칸만 금색
진화 조건 줄 "Sword Lv.5  +  ???" 가 아이콘 아래 전용 줄에 뜬다
배선         null 0 / 8 · 탭 5 · MainMenuUI.codexPanel 재연결
콘솔         0
```

🔵 **패널을 통째로 다시 지었으므로 `MainMenuUI.codexPanel` 참조가 끊긴다.**
빌더가 그 자리에서 다시 꽂고 **되읽어서 확인**한다 — 안 그러면 버튼이 아무것도 안 여는데
에러도 안 난다.

### 🟡 `D54` 를 고쳐 쓰지 않고 새 커밋으로 남겼다

`CLAUDE.md` §5 가 `--amend` 를 금지한다. 그래서 `D54`(목록)와 `D55`(격자)가
둘 다 히스토리에 남는다 — *"왜 격자가 됐나"* 를 커밋 두 개로 읽을 수 있다.

---

## 2-81. ✅ 도감 — **없던 것은 "봤는지"를 적어 두는 자리 하나뿐이었다** (D54, 2026-09-04 77차)

**한 줄:** `ROADMAP` §3-3 의 마지막 화면. 탭 5개(무기·아이템·진화·직업·적)를 한 화면에 뒀다.
데이터는 다 있었고, **없던 것은 발견 기록 하나**였다.

> 사용자 요구: *"모든 아이템 적의 수치 확인가능하고 / 아이템의 경우 먹기 전에 ??? + ??? 이런식으로
> 플레이어가 유추할 수 있게 / 확인되면 gun lv.5 + turret lv.3 이런식으로 진화 조건도 보여줘 /
> 상단의 탭에서 무기, 아이템, 진화, 직업, 적으로 한 화면에서 간편하게"*

### 먼저 뭘 안 만들어도 되는지부터 셌다

| 필요한 것 | 있었나 |
|---|---|
| 진화 조건 `gun Lv.5 + turret Lv.3` | ✅ `EvolutionData.Ingredients` + `RequiredLevels` 가 이미 그 모양이다 |
| 수치 | ✅ 아이템 28 · 무기 12 · 적 7 · 직업 10 · 진화 3 + 승급 4 |
| 저장소 | ✅ `save.json`(`JsonUtility`) — **필드를 늘려도 옛 세이브가 안 깨진다** |
| 획득 길목 | ✅ `LevelUpManager.ApplyItem` 하나로 모여 있다 (D18 이 장부를 하나로 만든 덕이다) |
| 🔴 **발견 기록** | **없었다** — 이것만 새로 만들었다 |

### 🔑 도감이 자기 목록을 따로 배선하지 않는다

목록을 `SceneWiring.csv` 에 또 적을 수도 있었지만 그러지 않았다.
**이미 배선된 매니저에서 읽는다** — `LevelUpManager.AllItems` · `EvolutionManager.AllEvolutions` ·
`WaveManager.CollectEnemies()` · `GameManager.Classes`.

⇒ *"게임에 나오는 것 = 도감에 있는 것"* 이 **구조적으로 보장된다.**
애셋 폴더를 훑었다면 배선 안 된 시험용 애셋까지 떴을 것이고,
CSV 에 목록을 또 적었다면 게임과 도감이 따로 놀 수 있었다.

🔵 그 덕에 **빠진 것도 자동으로 드러났다** — `allItems` 는 25개인데 아이템 애셋은 28개다.
차이 3개는 **진화 결과**로, 레벨업 카드에 그냥 뜨면 진화가 의미를 잃기 때문에 일부러 빠져 있다.
도감에서는 합쳐서 28개를 보여준다. 직업도 같다(시작 6 + 승급 4 = 10).

### 사용자 요구의 핵심 — 재료를 **따로** 가린다

```
아무것도 안 먹었을 때   ???  +  ???
Gun 만 먹었을 때        Gun Lv.5  +  ???          <- 여기가 "유추"다
둘 다 먹었을 때         Gun Lv.5  +  Turret Lv.3
```

🔑 **통째로 가리지 않는다.** 하나만 열려도 "뭐랑 합치는지"를 좁혀 갈 수 있다.
통째로 가리면 조합이 몇 개짜리인지도 모른다.
가릴 때는 **레벨도 같이 가린다** — 이름만 가리면 유추가 아니라 정답 공개에 가깝다.

### 판정 8/8

```
탭 5개          WEAPONS 12 · ITEMS 16 · EVOLUTION 7 · CLASSES 10 · ENEMIES 7
미획득 표시     ??? + 검은 실루엣 아이콘
진화 조건       ??? + ???  →  Gun Lv.5 + ???  →  Gun Lv.5 + Turret Lv.3
수치            Ogre HP 220 · Speed 1.2 · Armor 6 + D51 행동 설명까지
옛 세이브       Currency 1231 · TotalRuns 27 그대로 · 발견 0 으로 시작
저장/복원       플레이 종료 시 flush → 재시작 후 유지
갈고리 3종      빈 세이브 0 → 직업 Ranger O · 아이템 Bow O · 적 Zombie O
                🔵 안 나온 적(Wolf·Demon·Bonecaller)은 X 로 남는다  <- 대조군
콘솔            0
```

대조군을 넣은 이유가 있다. **적이 전부 O 가 되면** 그건 갈고리가 도는 게 아니라
목록을 통째로 채운 것이다. 안 나온 적이 X 로 남아야 "본 것만 적힌다"가 증명된다.

### 🔴 검증이 결함을 하나 잡았다 — 직업이 영원히 `???` 였다

`ApplyClass` 갈고리가 **빠져 있었다.** 런 시작에 고른 직업이 도감에 영영 안 뜨는 상태였다.

원인은 내 쪽 사고다. 갈고리 4개를 한 번에 넣던 파이썬 스크립트가
**인코딩 에러로 죽으면서 `io.open(..., 'w')` 가 자기 자신을 0바이트로 날렸다.**
(`'w'` 는 열 때 자르고, 쓰기는 그다음이다.) 다시 쓸 때 넷 중 하나를 빠뜨렸다.

⇒ 이후 모든 스크립트를 **`t.encode('utf-8')` 을 먼저 하고 나서 파일을 여는** 방식으로 바꿨다.
⇒ 그리고 이건 **런타임 판정이 아니었으면 못 잡았다** — 컴파일도 통과했고 화면도 멀쩡했다.
   `Ranger` 로 런을 시작해 놓고 `IsDiscovered("Ranger") == False` 를 본 순간에야 드러났다.

### 🟡 성능 — 적 스폰마다 파일을 쓰지 않는다

`Discover` 는 목록에만 넣고 **더럽다고 표시만** 한다.
파일은 런이 끝날 때 · 도감을 열 때 · 게임을 끌 때 몰아서 쓴다.
안 그러면 적 하나 나올 때마다 디스크 I/O 가 웨이브 중에 끼어든다
(`C38` 이 9층 최대를 294 로 올려 둔 참이다).

### ⚠️ 시험이 사용자 세이브를 건드려서 되돌렸다

판정 중에 발견 기록이 사용자의 실제 `save.json` 에 섞였다.
백업 후 **원본으로 복구**했다 — `Currency 1231` 등은 그대로, 도감은 빈 상태로 시작한다.

### 바꾼 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/UI/CodexPanel.cs` | 신규 — 화면 전부 |
| `Assets/Scripts/Meta/MetaProgressionManager.cs` | `SaveData` +3 목록 · `Discover`/`IsDiscovered`/`FlushIfDirty` · `CodexKind` |
| `Assets/Scripts/{LevelUp/LevelUpManager,Player/PlayerStats,Enemy/EnemyBase}.cs` | 발견 갈고리 4곳 |
| `Assets/Scripts/{Evolution/EvolutionManager,Wave/WaveManager}.cs` | 도감이 읽을 읽기 전용 접근자 · `CollectEnemies()` |
| `Assets/Scripts/UI/MainMenuUI.cs` | `CODEX` 버튼 · 메뉴를 떠날 때 닫기(I-50) |
| `Assets/Scenes/SampleScene.unity` | `CodexPanel` + `CodexButton` |

---

## 2-80. ✅ 층별 바닥이 드디어 보인다 — **1.4 % 에서 10.5배** (D53, 2026-09-04 76차)

**한 줄:** `C37` 이 타일 `m_Color` 10줄을 바꾼 것을 **내 방식(화면 캡처)으로 다시 쟀다.**
`D49` 가 🟡 절반으로 남겨 뒀던 것이 닫혔다. **DEV 쪽 파일 변경 0.**

> `REQ/DEV.md` **요청-29** 처리 · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-41**

### 🔑 판정을 D49 캡처와 **같은 잣대**로 했다

`D49` 의 캡처 두 장이 아직 남아 있어서, 같은 크롭·같은 공식으로 전후를 나란히 쟀다.
*"예전에 0.006 이었는데 지금 0.07"* 이 아니라 **같은 자로 잰 0.0069 → 0.0727** 이다.

| | 층0 중앙값 | 층9 중앙값 | **dL** | 겹침 |
|---|---:|---:|---:|---:|
| `D49` (재틴트 전) | 0.1510 | 0.1441 | **0.0069** | 1.2 % |
| **`D53` (재틴트 후)** | **0.1530** | **0.0803** | **0.0727** | **0.0 %** |

**10.5배.** 통과선(0.04)을 훨씬 넘는다.
🔴 **겹침 0 %** 가 더 강한 말이다 — **얕은 층의 블록 중 깊은 층 중앙값보다 어두운 것이 하나도 없다.**

### ① 대조군 — 얕은 층이 안 어두워졌다

`0.1510 → 0.1530`. CONTENT 가 *"배율은 1을 못 넘어 밝히는 쪽으로는 조정이 안 되므로
얕은 쪽은 지금을 지키고 깊은 쪽만 내렸다"* 고 한 그대로다. **초반이 어두워지지 않았다.**

### ② 🟡 CONTENT 예측 0.0443 vs 내 실측 0.0727 — **공식 차이다**

CONTENT 가 *"숫자가 크게 어긋나면 조명·카메라 쪽에 내가 못 본 게 있다는 뜻"* 이라고 했으므로 파 봤다.
**조명 문제가 아니라 밝기 공식이 달랐다:**

| 공식 | 층0 → 층9 | dL |
|---|---|---:|
| Rec.709 (내가 쓰던 것) | 0.1502 → 0.0883 | **0.0619** |
| Rec.601 | 0.1449 → 0.0873 | 0.0576 |
| **단순 평균 (R+G+B)/3** | 0.1302 → 0.0895 | **0.0406** ← CONTENT 의 0.0443 과 같은 자리 |

⇒ **둘 다 맞다.** 어느 공식으로 재도 통과선 0.04 를 넘는다.

### ③ 퀼트 — 두 방식이 거의 같은 숫자를 냈다

CONTENT 는 타일 색 공간에서 **층0 편차 0.0138** 을 예측했다.
나는 화면 32px 블록의 밝기 표준편차로 **0.0134** 를 얻었다. **완전히 다른 방식인데 숫자가 겹친다.**

🟡 **다만 CONTENT 가 안 갖고 있던 숫자를 하나 준다 — 층9 편차는 0.0211 이다.**
CONTENT 가 γ2.2 를 버린 이유가 *"층9 편차가 0.0231 로 오른다"* 였는데,
채택한 γ1.6 도 화면에서는 **0.0211** 이다. 층0(0.0134)의 1.6배다.
깊은 층이 얕은 층보다 얼룩덜룩하다 — 판단은 CONTENT 몫이라 넘겼다.

### ④ 가독성 — 넷 다 읽힌다

깊은 층(9) 바닥 위에 넷을 놓고 **화면 좌표를 코드로 받아** 그 픽셀을 쟀다.

```
바닥 중앙값 0.0673 · 바닥 자체 편차 폭(p95−중앙값) 0.0818   ← 이걸 넘어야 "읽힌다"
Bonecaller  0.8488  (+0.7815)   ✅
Demon       0.3359  (+0.2686)   ✅
적탄        0.2704  (+0.2032)   ✅
플레이어 탄 0.9656  (+0.8983)   ✅
```

🔑 **통과선을 "바닥보다 밝은가"가 아니라 "바닥 자체의 얼룩보다 뚜렷한가"로 잡았다.**
그냥 밝기 비교면 얼룩진 바닥 위에서 통과해도 실제로는 안 보인다.

### 🔴 여기서 한 번 오진할 뻔했다 — "안 읽힌다"가 아니라 **"없었다"**

첫 측정에서 적탄이 바닥 대비 **1.13배**로 나왔다. 바닥 자체 편차(1.37배)보다도 낮아
*"깊은 층에서 적탄이 안 보인다"* 로 CONTENT 에 되돌려 보낼 뻔했다.

크롭해서 **눈으로 보니 그 자리에 아무것도 없었다.**
`EnemyProjectile.Initialize` 를 안 불러 `_life = 0` 이었고, 첫 `Update` 에서 스스로 사라진 것이다.
⇒ 제대로 살려 다시 재니 **+0.2032 로 통과**했다.

**숫자가 나빴을 때 그림을 안 봤으면 남의 작업을 잘못 반려했다.**

### 판정 5/5

① 층0 유지 0.1530 · ② dL **0.0727** ≥ 0.04 · ③ 퀼트 0.0134 ≈ 예측 0.0138 ·
④ 가독성 4/4 · ⑤ 콘솔 0. **DEV 파일 변경 0** (검증만).

---

## 2-79. ✅ CONTENT 가 정한 값이 게임에 들어갔다 — **씬만 봐서는 Import 됐는지 알 수 없다** (D52, 2026-09-04 75차)

**한 줄:** `C38` 의 요청-30(Import 1회)을 처리했다. 코드 0줄 · 씬 2줄.
곁에서 **"CSV 가 적용됐는지 파일로는 확인이 안 되는"** 구조를 하나 확인했다.

> 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-40**

### 바뀐 것은 둘뿐이다 — 나머지 셋은 대조군이다

CONTENT 가 다섯 값 중 **둘만 바꿨다**(*"이길 근거가 없어 유지"*). 그대로 확인했다:

```
[D52-1] layerSpawnGrowth 0.0800 → 0.1400   ✅
[D52-1] maxAliveCeiling  300    → 320      ✅
[D52-1] layerHpGrowth    0.1000 (유지)     ✅ 대조군
[D52-1] layerDamageGrowth 0.0600 (유지)    ✅ 대조군
[D52-1] slowTimeScale    0.250  (유지)     ✅ 대조군
```

씬 diff 도 **정확히 2줄**이다 — 유지된 셋은 오버라이드가 안 생겼다.

### 🔴 곁일 — `WaveManager.prefab` 이 필드보다 오래됐다

Import 전에 씬 값을 먼저 읽어 뒀는데 `layerSpawnGrowth` 가 **0.08**, `maxAliveCeiling` 이 **300** 이었다.
`Economy.csv` 는 이미 `0.14 / 320` 이었는데도 그렇다.

원인: `Assets/GameObjects/WaveManager.prefab` 은 **최초 커밋 이후 한 번도 다시 저장되지 않았다.**
`D45`·`D50` 이 넣은 필드가 프리팹에 없으므로, 오버라이드가 없으면 값이 **C# 기본값으로 떨어진다.**

⇒ **`Economy.csv` 값이 C# 기본값과 같으면 씬에 아무 흔적도 안 남는다.**
그래서 *"Import 를 했나 안 했나"* 를 **파일을 읽어서는 판정할 수 없다** — 런타임에 읽어야 한다.
이번에 `D50` 의 계수 7개(`ringBatch 8` 등)가 전부 기본값과 같아 흔적이 없었던 것도 같은 이유다
(그 값들은 `D50` 에서 **런타임으로** 동작을 확인했으므로 결과는 맞다).

🟡 고치려면 프리팹을 한 번 다시 저장하면 되는데, **지금 하지 않았다** —
프리팹을 건드리면 씬 오버라이드 전체가 재계산돼 diff 가 커진다.
`TODO.md` 에 남긴다.

### 🔵 CONTENT 의 예측을 코드로 다시 냈다 — 293 이 아니라 294 다

```
9층 소환 배율 2.260 · Elite2 MaxAlive 130 → 294   (CONTENT 예측 293)
천장 320 아래 ✅ · 위험선 400 아래 ✅
```

`130 × 2.26 = 293.8` 이고 `LayerScaling.ScaleCount` 가 **`RoundToInt`** 라 **294** 다.
CONTENT 는 내림으로 냈다. **1 차이라 결론은 그대로다** — 320 아래, 400 아래.

### 판정 7/7

값 5개(바뀐 둘 + 유지된 셋 대조군) · 9층 최댓값 294 가 천장 320 아래 · 콘솔 0.

---

## 2-78. ✅ 적 다섯이 똑같이 걸어오던 걸 갈랐다 — **대조군이 아니었으면 셋 다 오진했다** (D51, 2026-09-04 74차)

**한 줄:** `D50` 이 *등장*을 갈랐다면 이건 *움직임*이다. 적 7종 중 **5종이 `Chaser` 하나**였다.
행동 3종을 데이터로 추가했다 — 새 프리팹 0 · 새 물리 질의 0.

> 사용자 요구의 나머지 반쪽: *"**행동 분화** + 적들이 출현할때…"* · `ROADMAP` §7 *"종류보다 행동 분화가 먼저"*

### 무엇이 문제였나

| 적 | 속도 | AI |
|---|---:|---|
| Slime | 1.4 | `Chaser` |
| Goblin | 2.2 | `Chaser` |
| Zombie | 1.6 | `Chaser` |
| Wolf | 4.2 | `Charger` |
| Demon | 2.8 | `Ranged` |
| Ogre | **1.2** | `Chaser` |
| Bonecaller | 1.2 | `Chaser` |

**5/7 이 같은 코드로 움직였다.** 종류를 늘려도 화면에서는 색만 다른 같은 적이다.

### 🔑 컨셉을 지어내지 않고 각 적의 수치에서 끌어냈다

| 적 | 데이터가 말하는 것 | 새 행동 |
|---|---|---|
| Slime 45hp · 1.4 · 다수 | 한 마리는 아무 일도 아니고, 서른 마리도 *"한 마리 × 30"* 이다 | **`Swarmer`** — 이웃이 많을수록 빨라진다. 뭉치는 것 자체가 위협이 된다 |
| Goblin 30hp · 2.2 · 최다 | 전원이 같은 직선으로 와서 **무기 앞에 줄을 선다** | **`Flanker`** — 옆으로 돌아 들어오고 가까워지면 직진으로 수렴 |
| Ogre 220hp · **1.2** | 🔴 **플레이어(3.5~4.6)를 영원히 못 따라잡는다** — 화면에 있어도 없는 것과 같다 | **`Blocker`** — 쫓지 않고 플레이어가 **가려는 곳** 앞을 막는다 |
| Zombie 70hp · 1.6 | — | `Chaser` 유지 = **대조군** |

Ogre 는 `D48` 에서 보스 실루엣을 정한 것과 같은 근거다 — **못 쫓아오면 버티고 서야 한다.**

### 새 물리 질의를 하나도 안 늘렸다

`Swarmer` 의 이웃 수는 **무리 분리가 이미 세고 있는 값**이다.
`GetSeparation()` 이 4스텝마다 도는 `OverlapCircle` 결과를 세기만 했다 —
그 질의가 적 800 기준 프레임의 **19 %** 였으므로(`PERF.md` §7) 하나 더 놓을 자리가 없었다.

### 🔴 enum 에 값을 끼워 넣지 않았다

`EnemyAI` 는 `.asset` 에 **정수**로 직렬화된다. 중간에 끼우면 기존 적의 행동이 조용히 바뀐다.
그래서 뒤에만 붙이고 **정수값 보존을 판정 항목으로 넣었다** — `Chaser 0 · Ranged 1 · Charger 2` 그대로.

### 판정 8/8 — 매번 대조군을 옆에 뒀다

```
[D51-V]  Chaser 0 · Ranged 1 · Charger 2 · Flanker 3 · Swarmer 4 · Blocker 5   ✅ 기존 3종 보존
[D51-F4] Flanker 거리 19.8 → 진행각 40.4도 (40.4~40.5)   기대 atan(0.85)=40.4
         Chaser  거리 19.6 → 진행각  0.0도 (0.0~0.0)      대조군
[D51-F8] 이웃 0 인 4마리 · 예측 대비 최대 오차 0.65도       ✅ 식대로 움직인다
         이웃 있는 4마리만 어긋난다 (최대 168도)            ← 무리 분리의 몫이다
[D51-S3] Swarmer 8마리 전부 오차 0.0000
         혼자 0.840 (1.4 x 0.6) vs 무리 1.715 → 2.04배
         Chaser 는 이웃 2·3 에서 모두 1.600                대조군
[D51-B5] Blocker 예측 지점과 0.12도 · 플레이어와 23.7도
         Chaser  플레이어와 0.09도 · 예측 지점과 15.7도     ← 두 줄이 정확히 뒤집혔다
[D51-B7] 거리 1.47 (HoldRange 안) → 플레이어와 0.28도       ✅ 코앞에서는 예측을 끈다
콘솔 0 · Zombie.asset 의 AI 는 한 글자도 안 바뀌었다
```

### 🔴 시험에서 세 번 헛짚었다 — 세 번 다 대조군이 잡아냈다

1. **첫 각도 측정이 통째로 무효였다.** 고블린이 이미 플레이어에 도착해(거리 **0.0**)
   측정한 건 측면 접근이 아니라 **분리 조향**이었다. 좀비 대조군이 0 이 아니라 40.4 로
   나온 게 신호였다 — 대조군이 없었으면 *"측면 접근이 된다"* 로 통과시켰을 것이다.
   ⇒ `Reposition` 으로 거리를 고정하고 다시 쟀다.
2. **`timeScale = 0.0002` 로는 못 잰다.** `D50` 에서 쓰던 값인데, 그때는 소환이
   **프레임 기준**이라 통했고 이번엔 **`FixedUpdate` 가 사실상 안 돈다**(누적이 `fixedDeltaTime` 에 못 미친다).
   `0.02` 로 올렸다 — 실시간 1초에 물리 스텝 약 1회, 위치는 0.04 유닛만 움직인다.
3. **`Blocker` 시험에서 오우거와 좀비를 같은 좌표에 겹쳐 놓았다.**
   완전히 겹치면 분리 조향이 **난수 방향**을 주므로 대조군까지 망가진다(좀비 28.4도).
   6 유닛씩 떼어 놓고 다시 쟀다.

그리고 **기대값을 한 번 낡은 채로 썼다** — 5.25 에 세워 놓고 4.84 에서 재면서
5.25 기준 기대값(23.0도)과 비교해 실패로 찍혔다. 각 개체의 **지금 거리**로 예측을 다시 내니 오차 0.68도였다.

### 수치

전부 `Enemies.csv` 의 새 열 7개다. **로그로는 판정할 수 없다** → **요청-39**.
`FlankArcWeight 0.85` · `FlankCloseRange 3.5` ·
`SwarmSoloMult 0.6` · `SwarmPackMult 1.35` · `SwarmFullCount 6` ·
`BlockLeadTime 1.8` · `BlockHoldRange 2.5`

---

## 2-77. ✅ 적이 나오는 방식이 넷이 됐다 — **예고와 그 뒤의 일이 다른 시계를 타고 있었다** (D50, 2026-09-04 73차)

**한 줄:** 적이 전부 *"멀리서 걸어오는 것"* 하나였던 걸 **흩뿌리기 / 원으로 조이기 / 부대 / 땅굴** 넷으로 갈랐다.
검증 중에 **기능 사이를 가로지르는 결함**을 하나 잡았다.

> 사용자 요구: *"적들이 출현할때 단순히 멀리서 오는게 아니라 땅굴을 파고 나오거나 부대 느낌으로 무리 지어 오거나 플레이어 기준으로 큰원으로 조여오거나"*
> — 같은 요청의 나머지 반쪽(**행동 분화**)은 `D51` 로 넘긴다.

### 무엇을 만들었나

`WaveData` 에 `SpawnPattern` 을 넣고, 소환 루틴을 넷으로 갈랐다.

| 패턴 | 모양 | 쓰는 곳 |
|---|---|---|
| `Scatter` | 예전대로 소환 반경 아무 데나 | 기본값 — **기존 행은 한 글자도 안 바뀐다** |
| `Ring` | 한 번에 8마리가 **고른 각도로 둘러싼다** | `Normal2` 늑대 · `Elite1` |
| `Squad` | **한 방향에서 4열 대형으로** 몰려온다 | `Normal1` 고블린 · `Elite2` 데몬 |
| `Burrow` | 발밑(3~6)에 **예고가 뜨고 0.85초 뒤 솟는다** | `Normal3` 고블린 |

`Boss1` 은 안 건드렸다 — 보스는 한 마리라 대형이라는 개념이 없다.

### 패턴을 CSV 에 어떻게 넣었나

`Waves.csv` 의 소환 항목은 이미 `적*수@간격:시작-끝` 꼴이다.
여기에 **맨 뒤 `~패턴` 접미사**를 붙였다:

```
Goblin*36@1.4:12-60~Squad
```

임포터는 `~` 를 **시간 창(`:시작-끝`) 보다 먼저** 떼어낸다(맨 뒤에 있으므로).
내보낼 때는 `Scatter` 가 아닐 때만 쓴다 — 그래서 **기존 행은 바이트 단위로 똑같다.**

### 판정 — 소환 직후에 재야 한다

첫 측정은 `Ring` 의 반경을 **0.3** 으로 돌려줬다. 늑대가 이미 플레이어에 도착해 있었던 것이다.
`StartWave` 직후에 `timeScale = 0.0002` 를 걸어 얼렸다 —
**소환 자리 대기는 프레임 기준**이라 시간을 늦춰도 소환은 계속 돌아간다.

```
[D50-R2] Ring  — 적 8마리 · 기대 간격 45.0도
         각도 간격 45.0 ~ 45.0도                     ✅ 고르게 둘러쌌다
         반경 20.0 ~ 20.0 · SpawnRadius 20           ✅ 원 위에 있다
[D50-S]  Squad — 적 12마리
         각도 폭 9.4도                                ✅ 한 방향에서 온다
         이웃 간 평균 거리 1.10 (squadSpacing 1.1)     ✅ 뭉쳐 있다
         반경 20.0 ~ 22.3                            (대형에 깊이가 있다)
[D50-B3] 예고 4개 · 적 0마리                            ✅ 예고가 먼저다
         예고 거리 3.43 ~ 5.55 (기대 3~6)              ✅ 발밑에서 솟는다
[D50-B5] 예고 자리에서 가장 먼 적 1.43                    ✅ 예고한 자리에서 솟았다
[D50-B6] 예고 배선을 끊으니 거리 20.0 ~ 20.0             ✅ 기습이 안 된다
콘솔 0 · 씬 배선 살아 있음 · 시험용 WaveData 잔재 0
```

대조군을 세 군데 놓았다 — `Burrow` 의 예고 거리 3~6 옆에 **흩뿌리기였다면 20**,
`B5` 의 1.43 옆에 **흩뿌리기였다면 14 이상**, 그리고 `B6` 가 그 20 을 실제로 보였다.

### 🔴 기능 사이를 가로지르는 결함 — 예고와 그 뒤의 일이 시계가 달랐다

`D41` 에서 `PulseFx` 를 **`unscaledDeltaTime`** 으로 고쳤다 — 승급·레벨업 파동은
`timeScale = 0` 인 순간에 뜨므로 그게 맞았다.
그런데 `Burrow` 가 같은 연출을 **예고**로 쓴다. 예고 뒤의 일(`WaitForSeconds`)은 **scaled** 다.

| 시계 | 예고 사라지는 시각 | 적이 솟는 시각 |
|---|---|---|
| `timeScale = 1` | 0.85초 | 0.85초 — 맞는다 |
| **`timeScale = 0.25`** (TAB 스탯창) | **0.85초** | **3.4초** — 표시가 **2.5초 먼저 사라진다** |

⇒ `D46` 의 TAB 저배속 창을 열고 있으면 **경고 없는 기습**이 된다.
두 기능은 서로를 모른다 — **붙여 놓고 재야 보인다.**

고침: `PulseFx` 에 `useUnscaledTime` 을 놓고 **시계를 고를 수 있게** 했다(Version 3).

```
Fx_Burrow   useUnscaledTime = False   ← 예고니까 뒤에 올 일과 같은 시계
Fx_Promote  useUnscaledTime = True    ← timeScale 0 에서 떠야 한다 (D41)
Fx_LevelUp  useUnscaledTime = True
```

### 🔵 예고가 없으면 거리를 밀어낸다

`burrowTelegraphPrefab` 이 배선 안 돼 있으면 **3~6 이 아니라 소환 반경(20)** 에서 나오게 했다.
예고 없이 발밑에서 솟으면 **피할 수 없는 기습**이기 때문이다 — `BossSlam` 이 세운 규칙과 같다.
이 가지를 실제로 끊고 재보았고(`B6`), 거리가 20 으로 밀렸다.

### 수치

새로 들어간 값은 전부 `Economy.csv` 로 뽑는다(7줄).
`ringBatch 8` · `squadColumns 4` · `squadSpacing 1.1` ·
`burrowMinDistance 3` · `burrowMaxDistance 6` · `burrowWindup 0.85` · `burrowBatch 4`.

이 일곱은 **로그로 판정할 수 없다** — 둘러싸임이 답답한지, 예고 0.85초가 비킬 만한지는
사람이 해 봐야 안다. `TUNING.md` 는 CONTENT 소유라 **요청-38** 로 넘겼다.

### 바꾼 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Wave/WaveData.cs` | `SpawnPattern` enum + `WaveSpawnEntry.Pattern` |
| `Assets/Scripts/Wave/WaveManager.cs` | `Ring`/`Squad`/`Burrow` 루틴 · `SpawnEnemyAt` · 계수 7개 |
| `Assets/Scripts/Fx/PulseFx.cs` | `useUnscaledTime` (Version 3) |
| `Assets/Editor/BalanceImporter.cs` | `~패턴` 접미사 읽기/쓰기 |
| `Assets/Prefabs/Fx_Burrow.prefab` | 신규 — 땅굴 예고(scaled 시계) |
| `Assets/Game/Balance/{Economy,SceneWiring,Waves}.csv` | 계수 7 · 배선 1 · 소환 5행 |
| `Assets/Scenes/SampleScene.unity` | `WaveManager.burrowTelegraphPrefab` 배선 |

---

## 2-76. 🟡 층마다 바닥이 바뀐다 — **바뀌는데 안 보인다** (D49, 2026-09-04 72차)

**한 줄:** 층별 바닥 테마를 **새 애셋 없이** 배선했다. 배치는 완전히 뒤집히는데
**화면은 1.4 % 밖에 안 달라진다** — 그림 쪽 문제라 CONTENT 에 넘겼다.

> ✅ **닫혔다 → [2-80](#2-80-✅-층별-바닥이-드디어-보인다--14--에서-105배-d53-2026-09-04-76차) (D53).** `C37` 이 타일 `m_Color` 를 고쳐 **dL 0.0069 → 0.0727 (10.5배)**. 아래 기록은 **그때 무엇을 몰랐는지**를 남긴 것이라 고치지 않는다 — 손잡이가 PNG 가 아니라 `m_Color` 였다는 걸 여기서는 몰랐다.

> `ROADMAP` §7 *"스테이지/맵 1씬 → 3~4 테마"* · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-37**

### 왜 지금인가

`D45` 로 층마다 **난이도**는 달라졌는데 **보이는 것은 그대로**였다.
CONTENT 가 `C32` 에서 *"기능 없이 배경만 갈면 뭐가 달라졌는지 안 보인다"* 고 했던 것의 반대편이다 —
이제 기능이 있으니 **보이게 할 차례**다.

### 🔑 새 데이터를 만들지 않았다

`GroundTiler.tiles` 가 이미 **얕은 것 → 깊은 것** 순서로 정렬돼 있었다:

```
Grass 40 · GrassPebble 18 · Weeds 13 · Dirt 10 · Gravel 7
· Roots 5 · CrackedEarth 3 · MossyCobble 2 · StoneSlab 1 · Flagstone 1
```

⇒ 깊어질수록 **이 가중치 배열을 제 역순 쪽으로 보간**하면 풀밭이 돌바닥이 된다.
타일 10종을 그대로 쓰고, 테마 목록도 새 CSV 도 안 만들었다.

🔴 **순서 가정을 코드에 못박았다** — `tiles` 를 다시 정렬하면 이 기능이 조용히 이상해진다.
`Tooltip` 과 XML 주석에 *"얕은 것 → 깊은 것 순서여야 한다"* 를 적었다.

### 판정 — 대조군이 먼저다

```
0층  Grass 38.4%  GrassPebble 21.0%  Weeds 14.8%  …  StoneSlab 0.9%   ← 예전과 같아야 한다
9층  Flagstone 38.6%  StoneSlab 20.7%  MossyCobble 10.3%  …  GrassPebble 0.5%
```

**0층에서 `Grass 38.4 %`** 는 원래 가중치 `40/100` 과 맞는다 — `t=0` 이면 보간이
원래 값을 그대로 돌려주므로 **이 변경은 얕은 층을 건드리지 않는다.**
9층은 정확히 그 역순이다.

### 🔴 그런데 화면이 안 바뀐다 — 그걸 숫자로 잡았다

같은 자리에서 두 층을 캡처해 바닥 영역(68,600px)을 비교했다:

| | 값 |
|---|---|
| 평균 채널 차이 | **3.67 / 255 = 1.4 %** |
| 평균 밝기 | `0.1510` → `0.1447` (차이 **0.006**) |

**배치는 완전히 뒤집혔는데 색은 그대로다.** 무늬만 조금 움직인다.
⇒ **타일 10종이 서로 너무 비슷하다.** `Grass` 든 `Flagstone` 이든 화면에선 같은 어두운 올리브다.

이건 CONTENT 가 `TODO.md` 에 적어 둔 *"바닥이 너무 균일할 수 있다(I-28 여파)"* 와 같은 문제인데,
이번에 **숫자가 붙었다.** 통과선까지 같이 줬다 — **밝기 차 ≥ 0.04** 또는 **채널 차 ≥ 15/255**.

### 🟡 시험에서 두 번 헛짚었다

1. **첫 캡처 두 장이 파일 해시까지 같았다.** MCP 명령 두 개가 **한 에디터 프레임에 몰려 처리**돼
   두 `CaptureScreenshot` 이 같은 프레임을 기록한 것이다.
   *"차이 0.00"* 을 그대로 믿었으면 **"기능이 안 먹는다"** 로 오진했을 것이다.
   ⇒ 사이에 **타일맵 내용을 읽는 명령을 끼워** 프레임을 벌리고 다시 찍었다
   (`카메라 칸의 타일 = Tile_CrackedEarth` → `Tile_Grass` 로 실제 전환 확인).
2. 그 앞에서는 **메인 메뉴가 떠 있는 화면**을 찍고 바닥을 비교할 뻔했다.

---

## 2-75. ✅ 보스가 더 이상 큰 오우거가 아니다 — **축소가 PPU 를 같이 낮춘다** (D48, 2026-09-04 71차)

**한 줄:** 보스에게 전용 그림이 생겼다. `ROADMAP` §2-5 의 마지막 줄이었다.

> 사용자 지시 2026-09-04 *"보스 그림 재개해"* · `D38` 에서 보류로 내려 둔 `요청-27` 을 재개
> 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-36**

### 재개 조건이 둘 다 풀려 있었다

CONTENT 가 `C32` 에서 보류 이유로 적은 *"층별 난이도 기능이 먼저다"* 는 **`D45`** 로 해소됐고,
다른 조건인 `DESIGN_ART.md` §6 의 1~6번은 **`D42`** 로 끝났다.

### 🔴 컨셉을 내가 정했다 — 지어내지 않고 끌어냈다

원래 요청은 **CONTENT 가 컨셉·이름·아트 디렉션을 정하는** 것이었는데 **그쪽이 안 돌고 있었다.**
그래서 정하되, 근거를 전부 **이미 코드·데이터에 있는 사실**에서 가져왔다:

| 사실 | 그래서 |
|---|---|
| 보스 이동 최대 **2.38** vs 플레이어 **3.5~4.6** → **절대 못 쫓아온다**(D34) | 달려드는 게 아니라 **버티고 서서 내려찍는 것** ⇒ 넓적하고 무거운 실루엣 |
| 기술이 **내려찍기 + 고블린 소환**(`Bosses.csv`) | **강령술사형** — 뼈 전리품, 소환하는 자 |
| 예고 원(`BossSlamRing`)·등급 외곽선이 **붉은색** | 보스가 붉으면 **둘 다에 섞인다** ⇒ 붉은 대역 금지 |
| 타일이 어둡다 | 밝아야 읽힌다 |
| 직업 10종이 주황·청록·검정·은/빨강, 상점 주인이 보라 | **뼈색 + 병든 초록**이 비어 있다 |

### 🔑 잡몹 Ogre 는 안 건드렸다

보스가 쓰던 `EnemyData` 를 고치면 **잡몹 Ogre 도 같이 바뀐다.**
그래서 `Enemies.csv` 에 **보스 전용 행**을 새로 만들고 `Boss1.BossOverride` 만 갈아 끼웠다.
🔴 **수치는 Ogre 를 한 칸도 안 바꾸고 복사**했다 — 밸런스 변화 0 이고, 실제로 HP 가 **1540 으로 전과 같다.**

| 바뀐 것 | |
|---|---|
| `Enemies.csv` | `Bonecaller` 행 추가 (Id · EnemyName · Sprite 만 다르다. WalkSheet 는 비웠다) |
| `Waves.csv` | `Boss1.BossOverride` : `Ogre` → `Bonecaller` |
| `Bosses.csv` | `BossOgre.EnemyId` : `Ogre` → `Bonecaller` |

### 🔴 `maxTextureSize` 가 PPU 를 같이 낮춘다 — 보스가 2배가 될 뻔했다

적 스프라이트 규약은 **512² · PPU 1024 = 0.5 유닛**이다. 생성물이 1024² 라
`maxTextureSize 512` 로 줄여 맞추려 했더니:

```
[D48-I]  Bonecaller 512x512 · PPU 512 · 월드크기 1.00 유닛
[D48-I]  (대조) Ogre 월드크기 0.50 유닛
```

🔑 **Unity 는 축소할 때 월드 크기를 지키려고 PPU 도 같이 낮춘다.**
원본(1024, PPU 1024 = 1.0유닛)의 크기를 보존한 것이라 **의도한 0.5 가 아니라 1.0** 이 됐다.
보스는 여기에 다시 ×2 가 걸리므로 **잡몹의 4배**가 될 뻔했다.

⇒ 축소를 포기하고(1024 유지) **PPU 를 2048** 로 줘서 `1024/2048 = 0.5` 로 맞췄다.
**Ogre 와 나란히 재지 않았으면 못 봤다** — 숫자 하나만 보면 512/512 도 그럴듯하다.

### 판정 7/7

| # | 통과값 | 실측 |
|---|---|---|
| ① | 파일·크기 | 1024² ✅ |
| ② | 🔴 알파 최솟값 0 | **0** · 모서리 `0,0,0,0` ✅ |
| ③ | 색 수 1개 아님 | 픽셀아트 다색 ✅ |
| ④ | 🔴 어두운 타일 대비 | 평균 밝기 **0.371** ✅ |
| ⑤ | 🔴 예고 원과 안 섞임 | 붉은 치우침 **2** ✅ |
| ⑥ | 실루엣 구분 | 가로 **85.8 %** > 세로 **78.9 %** · 캡처 ✅ |
| ⑦ | 콘솔 | 0 ✅ |

런타임 — `DisplayName "Bonecaller"` · 스프라이트 `Bonecaller` · `BossBrain` 붙음 ·
HP **1540**(= 220 × 7, 전과 동일) · scale 3.20 · HP 바에 `BONECALLER`.
잡몹 Ogre 는 `Sprite Ogre` · **걷기 16프레임 그대로.**

> 🟡 `I-41` 가짜 투명이 **또** 났다(세 번째). 프롬프트에 명시해도 안 통한다 —
> `RemoveImageBackground` 를 돌려 `min 0` 으로 만들었다. 이제 이건 절차로 굳었다.

### CONTENT 확인 대기 3건

**이름 `Bonecaller`**(HP 바에 뜨는 플레이어 대면 값) · **보스를 3체로 늘릴지**(`ROADMAP` §7) ·
**걷기 시트**(`B2` 에서 AI 걷기 시트가 16칸 중 12칸 깨진 적이 있어 안 뽑았다).

---

## 2-74. ✅ 화면 설정과 크레딧 — **패널은 알파 1 로도 안 가려진다** (D47, 2026-09-04 70차)

**한 줄:** `ROADMAP` §3-3 의 *"없는 화면"* 을 정리했다. 키 바인딩은 **사용자가 안 하기로** 했다.

> 사용자 지시 2026-09-04 — *"키바인딩 필요없고 나머지 작업 푸시후 진행해"*

### 만든 것

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Core/DisplaySettings.cs` **(신규)** | 해상도 목록·전체화면·품질을 `PlayerPrefs` 로 저장하고 적용하는 정적 클래스 |
| `Assets/Scripts/UI/OptionPanel.cs` | 볼륨 2종 + **화면 3종**. `< 값 >` 화살표 방식 |
| `Assets/Scripts/UI/CreditsPanel.cs` **(신규)** | 메인 메뉴 `Credits` 버튼으로 연다 |
| `GameManager.Awake` | `DisplaySettings.ApplySaved()` 한 줄 |

### 🔑 값을 고르는 곳과 적용하는 곳을 갈랐다

옵션 패널은 **열려야** `Start()` 가 돈다. 그런데 저장된 해상도·품질은 **게임이 켜지자마자**
먹어야 한다. 그래서 적용은 `DisplaySettings` 가 맡고 `GameManager.Awake` 가 한 번 부른다 —
`AudioManager` 가 볼륨을 다루는 방식과 같다.

### 🔑 드롭다운을 안 썼다

`TMP_Dropdown` 은 템플릿·뷰포트·아이템까지 부품이 여럿이라 코드로 짓기 나쁘다.
`< 값 >` 화살표는 **기존 9-slice 버튼을 그대로 쓰고**(D40 의 `pixelsPerUnitMultiplier` 규칙까지 재사용)
나중에 게임패드로 옮기기도 쉽다. 끝에서 순환시켜 버튼이 죽지 않게 했다.

### 🔴 비침 결함 — 알파 1 로도 안 가려진다

크레딧을 처음 띄웠더니 **뒤 메뉴의 `VS_LIKE` 가 카드 위로 읽혔다.**
`Image.color` 알파를 1.0 으로 올려도 그대로였다. 스프라이트를 실측하니:

```
UI_Panel 한가운데 픽셀 알파 = 0.92   🔴 스프라이트가 반투명이다
```

⇒ **패널 스프라이트 자체가 8 % 비친다.** `Image.color` 로는 못 막는다 —
**가리는 일은 `DimBG` 몫**이다. 크레딧 `1.00`, 옵션 `0.65 → 1.00` 으로 올려 해결했다.

> ℹ️ 다른 패널(상점·일시정지 …)도 같은 8 % 를 갖고 있다. 거기는 뒤가 이미 어두워 티가 안 난다.

### 🟡 배치를 두 번 재봤다

처음 카드를 `520×620` 으로 잡았더니 `Quality` 와 `CLOSE` 사이가 **200px 비어** 미완성으로 보였다.
`520×500` 으로 줄이고 줄 간격을 다시 잡았다 — 이번엔 눈이 아니라 **숫자로** 확인했다
(제목 아랫변 182 vs 첫 줄 윗변 146 · 닫기 아랫변 -223 vs 카드 바닥 -250).

### 검증 5/5

| # | 무엇 | 결과 |
|---|---|---|
| ① | 품질 | ✅ `>` 클릭 → `QualitySettings 5 → 0`(순환) · 라벨 `Ultra → Very Low` · 저장 `0` |
| ② | 해상도 | ✅ `720x480 → 720x576` 저장 · `<` 로 **정확히 되돌아옴** |
| ③ | 전체화면 | ✅ `Off → On` · 저장 `1` |
| ④ | 크레딧 | ✅ 메뉴 버튼으로 열림 · 본문 297자 · `Close` 로 닫힘 |
| ⑤ | 콘솔 | ✅ 에러·경고 0 |

> 🟡 **첫 시도는 아무 값도 안 바뀌었다** — `SetActive(true)` 와 **같은 프레임**에 `onClick` 을 불러서
> `Start()` 가 아직 안 돌았고 리스너가 안 붙어 있었다. 라벨은 `OnEnable` 이 즉시 도니까 제대로
> 나왔는데 그게 오히려 헷갈렸다. 프레임을 넘겨 다시 눌렀다.
>
> ⚠️ **에디터에서는 해상도·전체화면이 눈에 안 보인다.** `Screen.SetResolution` 은 게임 뷰를
> 안 바꾼다 — 빌드 전용이다. 그래서 이 둘의 판정은 **"저장·조회가 맞나"** 로 했다. 품질은 실제로 바뀐다.

### 🔴 곁일 — 볼륨이 사실상 0 이다

슬라이더가 비어 보여서 값을 읽었더니 **`bgm_vol 0.0588` · `sfx_vol 0.0427`** 이었다.
**소리가 거의 안 들리는 상태다.** `TODO.md` §1 의 *"오디오 22종 청취"* 가 계속 미뤄진 게
이것 때문일 수 있다. **사용자 기기의 `PlayerPrefs` 값이라 안 바꾸고 `TODO.md` 에 적었다.**

---

## 2-73. ✅ 내 빌드가 뭔지 보인다 — **멈추지 않고 느려진다** (D46, 2026-09-04 69차)

**한 줄:** 전투 중 `TAB` 으로 스탯·보유 아이템을 본다. `ROADMAP` §3-1 의 마지막 칸이었다.

> 사용자 요구 2026-09-04 — *"플레이중 TAB 을 눌러 현재 스탯과 보유한 아이템 표시.
> 보는 도중에도 게임은 플레이 되게 하고 싶어서 저배속으로 느리게 진행되게"*
> 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-35**

### 무엇이 없었나

뱀서라이크에서 **"내 빌드가 뭔지 모른다"** 는 건 기능 공백 중 제일 크다.
레벨업 카드는 고르는 순간만 보이고, 상점은 **파는 것**만 보여 준다.
스탯(피해 배율·치명타·획득 배율 …)은 **어디에도 안 나왔다.**

### 🔑 멈추지 않고 느려진다 — 그게 설계를 바꿨다

사용자가 *"보는 도중에도 게임은 플레이 되게"* 라고 했다. 그래서 일시정지가 아니라 **저배속**이다.
그 한 줄이 레이아웃을 정했다:

**창을 보는 동안에도 적은 다가오고 무기는 나간다.** 전면 패널로 덮으면 아무것도 안 보인 채
맞는다. ⇒ 창은 화면 **왼쪽 560px 만** 덮고 오른쪽에서 전투가 계속 보인다.
HUD 체력바도 안 가리게 위 여백을 140 으로 내렸다.

### 🔴 `timeScale` 은 공유 자원이다 — 남의 계약을 따랐다

`GameManager.cs` 에 이미 경고가 적혀 있었다:

> *"`Time.timeScale` 은 이 프로젝트에서 여러 곳이 공유한다 — `WaveManager.PauseWave()` 가 0,
> 일시정지·레벨업·스테이지 결과창이 각각 0/1 을 쓴다. 아무 때나 1f 로 되돌리면
> 일시정지가 저절로 풀린다."*

그래서 `DoHitstop` 과 **똑같은 3단 계약**을 썼다:

1. **웨이브 중일 때만** 연다
2. **이미 `timeScale` 이 1 이 아니면 아예 안 연다** (누가 멈춰 놨다)
3. **닫을 때도 여전히 웨이브인지 다시 확인**하고, 아니면 손대지 않는다

🔑 **이 계약이 시험 중에 실제로 걸렸다.** 웨이브를 진짜로 돌려 놓고 TAB 을 눌렀더니
그 사이 XP 가 쌓여 레벨업 패널이 떴고, `GameState LevelUp · timeScale 0` 에서
창이 **정확히 안 열렸다.** 처음엔 버그로 보였지만 **코드가 맞게 거절한 것**이었다 —
흔들린 건 시험 쪽이었다.

> ⚠️ **알려진 부작용** — 창이 열려 있는 동안엔 히트스톱이 안 걸린다.
> `DoHitstop` 이 `timeScale != 1` 이면 스스로 물러나기 때문이다. 저배속에 히트스톱까지
> 겹치면 어차피 안 보인다 — 그대로 둔다.

### 🟢 부품이 이미 있었다

`Prefab_ItemChip` 은 **`LayoutElement` + `Icon` + `Label` 까지 갖춰져 있는데
어느 스크립트도 참조하지 않아 한 번도 안 쓰였다.** 새로 만들지 않고 `ItemChipUI` 만 붙였다.

> 🟡 처음엔 *"missing script 상태다"* 라고 봤는데 **틀렸다.** `grep` 이 guid 를 못 찾은 건
> 그 스크립트(`LayoutElement`)가 **패키지 안에 있어서**였고,
> `GetMonoBehavioursWithMissingScriptCount` 는 **0** 을 돌려줬다. 엔진에 물어보고 정정했다.

### 🟡 칩 정렬을 두 번 고쳤다 — 피벗을 안 보고 오프셋을 줬다

`Icon` 은 **위쪽 피벗**(0.5, 1), `Label` 은 **아래쪽 피벗**(0.5, 0)이었다.
거기에 "아이콘은 위로 +12, 라벨은 아래로 -48" 을 주니 **둘 다 칩 밖으로 나갔다** —
실측하니 칩 `130~188` 인데 아이콘 `152~193`, 라벨 `108~123` 이었다.
안쪽으로 넣어(`-4` / `+4`) 고쳤다. `D42` 에서 배운 것과 같은 함정이다(교훈 215).

### 검증 4/4

| # | 무엇 | 결과 |
|---|---|---|
| ① | TAB 으로 열린다 | ✅ 패널 열림 · `timeScale 0.25` · 칩 7 · 스탯 13줄 (캡처) |
| ② | 🔴 **거절 경로** | ✅ 레벨업이 끼어든 상태(`timeScale 0`)에서 **안 열렸다** |
| ③ | 격자 넘침 | ✅ 12개 → 6칸×2줄, 칩 최하단 **84** vs 패널 바닥 **18**. 계산상 **최대 18개** |
| ④ | 콘솔 | 에러·경고 **0** |

> 🟡 **키 시뮬레이션은 불안정했다.** `InputSystem.QueueStateEvent` 로 넣은 TAB 이
> 실제 키보드 장치 상태에 덮여 몇 번은 안 걸렸다. ①은 걸렸을 때 확인한 것이고,
> ③은 **격자만 따로** 채워 재는 쪽으로 갈랐다 — 여는 경로와 그리는 경로는 다른 문제다.
>
> 🔴 그 과정에서 **내 판정 코드에 구멍이 있었다** — 칩이 0개일 때 `float.MaxValue` 가
> "안 넘친다" 로 통과했다. **"판정 불가"와 "통과"를 구분**하도록 고쳤다.

### 남은 것 — 숫자로 답이 안 나오는 것들

`TODO.md` 와 `TUNING.md` 에 넘겼다: 저배속 0.25 가 적당한지 · hold 가 맞는지(토글로 바꿀지) ·
왼쪽 560px 이 충분한지 · **아이템 19개 이상이면 넘친다**(격자 6×3 = 18).

---

## 2-72. ✅ 층이 깊어지면 실제로 세진다 — **소환 수만 올리면 아무 일도 안 일어난다** (D45, 2026-09-04 68차)

**한 줄:** 층별 난이도 전역 배율(결정 4 · B안)을 넣었다. **`ROADMAP` 4단계의 마지막 구조적 공백이었다.**

> `ROADMAP` §3 결정 4 · `TODO.md` §3 · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-34**

### 무엇이 문제였나 — 곡선이 아예 없었다

`WaveManager.StartWave` 가 노드 타입만 보고 풀에서 **무작위로** 뽑았다:

```csharp
StageType.Elite => eliteWaves[Random.Range(0, eliteWaves.Length)],
_               => normalWaves[Random.Range(0, normalWaves.Length)]
```

⇒ **1층에서 Normal3(108마리)이 나오고 9층에서 Normal1(64마리)이 나올 수 있었다.**
10층짜리 맵인데 1층과 10층이 같았다.

### 왜 (B) 를 먼저 했나

결정 4가 (A)(웨이브에 `MinLayer`/`MaxLayer` 열) 와 (B)(전역 배율) 중
**(B) 를 먼저**로 정해 뒀고, 그 판단이 지금도 맞다 — **웨이브가 6개뿐**이라
(A) 를 지금 넣으면 층당 선택지가 거의 없다. (B) 는 데이터가 거의 안 들고 층이 몇 개든 자동이다.

### 🔑 짜면서 밟은 함정 — 소환 수만 올리면 화면이 그대로다

처음 설계는 `entry.Count` 에만 배율을 걸려던 것이었다. 그런데 넘치는 소환은
`WaitForSpawnSlot()` 이 **`MaxAlive` 상한에서 붙잡는다** — 대기줄만 길어지고
**화면에 보이는 적 수는 하나도 안 는다.**

⇒ `layerSpawnGrowth` 를 **셋에 같이** 걸었다:

| 대상 | 왜 |
|---|---|
| `entry.Count` | 총 소환량 |
| `MaxAlive` | 🔴 **이걸 안 올리면 위가 무의미하다** |
| `KillTarget` | 소환량이 늘었는데 목표가 그대로면 **층이 깊을수록 웨이브가 오히려 빨리 끝난다** |

### 🔴 `WaveData` 는 ScriptableObject 다 — 원본을 고치면 안 된다

`MaxAlive`/`KillTarget`/`Count` 는 전부 SO 필드다. 런타임에 거기다 배율을 곱해 쓰면
**애셋 파일이 더러워지고 다음 런까지 남는다**(`ItemData.CurrentLevel` 과 같은 함정).
⇒ 원본은 안 건드리고 **읽을 때마다 곱하는** `ScaledMaxAlive()` / `ScaledKillTarget()` 로 갔고,
시험에서 원본이 그대로인 것을 확인했다.

### 설계 결정 둘

- 🔴 **이동 속도는 안 올렸다.** 적이 플레이어(3.5~4.6)보다 빨라지면 피하는 게 아니라 맞는 게 된다 —
  그건 난이도가 아니라 **조작 불능**이다
- **`LayerScaling` 은 정적 클래스다.** `EnemyBase.Initialize` 가 읽는데 거기서
  `GameManager.Instance.XxxMgr` 를 타면 I-8/I-38 의 `Awake` 순서 함정에 걸린다.
  **쓰는 쪽(`WaveManager`)이 웨이브 시작 때 밀어 넣고 읽는 쪽은 조회만** 한다

### 안전망 — `maxAliveCeiling 300`

`EnemyBase.cs` 가 *"`MaxAlive` 를 **400 이상**으로 올리면 무리 분리 버퍼(64칸)를 다시
판단해야 한다"* 고 못박아 뒀다(`B8`). 그 선 아래로 묶었다.

현재 수치로는 **9층 최대 224**(Elite2 130 → 224)라 천장에 안 닿는다 — **지금은 순수한 안전망**이다.
CONTENT 가 `layerSpawnGrowth` 를 크게 올리면 그때 걸린다.

### 검증 6/6

| # | 무엇 | 결과 |
|---|---|---|
| ① | 🔴 **1층 대조군** | HP **x1.00** · 피해 **x1.00** — 안 걸려야 하는 자리에서 안 걸린다 |
| ② | 층별 배율 | 5층 x1.50/x1.30 · 9층 **x1.90**/**x1.54** |
| ③ | 🔴 **진짜 경로** | `StartWave(Layer 7)` → `Layer 7 · 1.70 · 1.42 · 1.56` — **`Economy.csv` 계수가 그대로 실린다** |
| ④ | 소환 스케일 | `Normal1` Count 30 → **52** · MaxAlive 60 → **103** (9층) |
| ⑤ | 🔴 **SO 무오염** | 시험 후 `Normal1` 원본 `MaxAlive 60` · `Count 30` 그대로 |
| ⑥ | 콘솔 | 에러·경고 **0** |

🔑 **①이 없으면 ②③은 아무 값이나 통과시킨다** — 배율이 늘 걸려 있어도 숫자는 커진다.

### 🟢 이걸로 보스 그림 보류의 재개 조건이 다 풀렸다

CONTENT 가 `C32` 에서 보스 그림·스테이지 배경을 7순위 보류로 두며 적은 이유가
*"층별 난이도 기능이 먼저다"* 였다. **그 기능이 들어갔고**, 다른 조건(`DESIGN_ART.md` §6 의 1~6번)은
`D42` 로 끝났다. ⇒ **다시 열 수 있다.** 여는 것은 사용자 판단이라 먼저 열지 않았다.

---

## 2-71. ✅ 안 쓰는 탄환 그림을 지웠다 — **첫 스캔은 통째로 틀렸다** (D44, 2026-09-04 67차)

**한 줄:** `ICON/Bullet.png` 을 지웠다. 작은 일인데 **재는 방법에서 한 번 크게 틀렸다.**

> CONTENT `C36`([`REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-28) 이 *"지워도 되지만 내가 지울 수 없다 —
> 네 판단에 맡긴다"* 고 넘긴 것. 회신은 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-33**.

### 🔴 첫 스캔 결과를 버렸다

guid 를 뽑는 데 `grep -oP` 를 썼는데 이 환경에서 이렇게 났다:

```
grep: -P supports only unibyte and UTF-8 locales
```

그래서 `GUID` 가 **빈 문자열**이 됐고, `grep -rl ""` 이 되어 **`Assets` 의 거의 모든 파일**이
"참조"로 잡혔다. 화면만 보면 *"엄청나게 많이 쓰인다"* 로 읽힌다 — **정반대 결론이다.**

⇒ `sed` 로 다시 뽑고, **대조군을 같이 넣었다:**

```
Bullet.png(구)   guid = 1f4e1489ffaa4e94690bcd7ca34c7b1f  →  참조 0곳
PlayerBullet(신) guid = 7bbb40b59fafbfb44bee1ea155205d91  →  Proj_Bullet.prefab   ← 대조군
```

🔑 **쓰는 쪽 guid 가 정확히 1곳으로 잡히는 걸 확인한 뒤에야 "0곳"을 믿었다.**
`D43` 의 한글 스캐너와 같은 이유다 — **0 은 깨끗하다는 뜻일 수도, 못 봤다는 뜻일 수도 있다.**

### 삭제 후 확인

```
Bullet.png 로드 = null (지워짐)
Proj_Bullet.prefab    의 스프라이트 = ✅ PlayerBullet
Proj_EnemyBolt.prefab 의 스프라이트 = ✅ EnemyBolt
```

콘솔 Error·Warning **0**.

### 남은 둘은 안 지웠다

| 파일 | 왜 |
|---|---|
| `ICON/goblin.png` | `Enemy_Goblin.prefab` 이 들고 있다. 화면엔 안 보이지만(`EnemyBase.cs:124` 가 덮어쓴다) 지우면 **missing 참조**가 남는다 |
| `ICON/Exp_Orb.gif` | 🔴 **경험치 구슬의 유일한 그림.** 지우면 한 판에 수백 개가 통째로 안 보인다 |

---

## 2-70. ✅ 파동이 제자리를 찾고 대사가 영문이 됐다 — **씬만 고쳤으면 부활했다** (D43, 2026-09-04 66차)

**한 줄:** `D41` 이 남긴 보류 하나와 `D40` 이 잡은 버그 하나를 사용자 결정대로 닫았다.

> 사용자 지시 2026-09-04 · `Parallel/BUGS.md` `B11` · `TODO.md` 레벨업 파동 3안 중 **A**

### ① 레벨업 파동 — 시점을 옮겼다 (A안)

`ScreenSpaceOverlay` 캔버스를 월드 스프라이트로 이길 방법은 없다(`sortingOrder` 로도 안 된다).
그래서 **덮이지 않는 시점으로 옮겼다.**

| 전 | 후 |
|---|---|
| `HUDManager.OnLevelUp` — 레벨업 패널이 **열리는 그 프레임** | `LevelUpManager.HidePanel` — 패널을 **실제로 내릴 때** |

🔑 **그 시점이 마침 플레이어가 다시 월드를 보는 순간**이라, 옮긴 게 타협이 아니라 개선이 됐다.

세부 두 가지를 같이 처리했다:

- **레벨업이 여러 번 밀려 있어도 파동은 마지막에 한 번**이다(`B10` 대기열). 세 번 겹쳐 뜨면 지저분하다
- **진화 제안(`ShowForcedChoices`)으로 열린 패널을 닫는 것으로는 안 뜬다.**
  같은 패널을 두 용도로 쓰기 때문에 `_levelUpConsumed` 로 따로 센다 —
  안 그러면 **진화 카드를 골랐을 뿐인데 레벨업 파동이 뜬다**

`HUDManager` 는 `levelUpEffect` 필드를 그대로 들고 `PlayLevelUpEffect()` 만 공개했다 —
`SceneWiring.csv` 배선을 안 건드리려는 것이다.

### ② `B11` — 씬만 고쳤으면 부활했다

씬의 `ShopUI.npcDialogues` 5칸을 코드 기본값과 같은 영문으로 덮었다. **코드는 손대지 않았다.**

```
[B11] 한글 줄 5 -> 0
[B11-V] 화면에 실제로 들어간 대사 = "Stronger foes are waiting. / Come prepared, friend."
```

🔴 **여기서 멈췄으면 다시 살아났다.** 고친 뒤 씬 전체를 훑으니 0건인데,
**프리팹까지 훑으니 `Assets/GameObjects/UI Canvas.prefab` 원본에 한글 5줄이 그대로 있었다.**
씬의 `UI Canvas` 는 그 프리팹의 인스턴스라 내가 고친 건 **오버라이드**였다 —
오버라이드를 되돌리거나 다시 인스턴스화하면 **한글이 부활한다.** 프리팹 원본도 같이 덮었다.

### 🔑 스캐너에 대조군을 넣었다 — 이게 `B4` 가 놓친 이유다

`SerializedProperty` 순회가 `string[]` **원소를 안 들여다보면**
*"한글 0건"* 은 **깨끗하다는 뜻이 아니라 못 봤다는 뜻**이다.
그래서 방금 넣은 영문(`"For a price"`)을 **같이 찾게** 했다:

```
[D43-S2] (대조군) 씬:UI Canvas · ShopUI.npcDialogues.Array.data[2]
[D43-S2] 씬 — 문자열 263개 · 한글 0건 · 대조군 1건   ✅ 배열 안까지 본다
[D43-S2] 🔴 프리팹:UI Canvas · ShopUI.npcDialogues.Array.data[0] = 좋은 물건만 골라왔지 …
[D43-S2] 프리팹 61개 — 문자열 69개 · 한글 5건
```

**대조군이 안 잡혔으면 0건을 믿으면 안 되는 것이었고, 실제로 그 순회가 프리팹의 5건을 잡았다.**
`B4` 는 *"씬 6곳 다 고쳤다"* 로 닫혔는데 그때 훑은 것은 `TextMeshProUGUI.text` 뿐이라
`string[]` 필드를 못 봤다 — 그래서 `B11` 이 남았다.

### 검증 3/3

| # | 무엇 | 결과 |
|---|---|---|
| ① | 파동이 패널 열릴 때 **안** 뜬다 | `PulseFx = 0` ✅ (**D41 에선 여기서 1** — 대조군) |
| ② | 파동이 패널 닫힐 때 뜬다 | `PulseFx = 1` · `timeScale = 1` ✅ |
| ③ | 대사가 영문이다 | 화면 문자열 확인 · **tofu 경고 0건** · 캡처 ✅ |

콘솔 Error·Warning **0**. 전수 조사 최종: 씬 문자열 263개 · 프리팹 69개 → **한글 0건**.

---

## 2-69. ✅ 메뉴에 배경이, 상점에 사람이 생겼다 — **"투명하게 그려 줘"는 안 통한다** (D42, 2026-09-04 65차)

**한 줄:** Unity AI 로 그림 2장을 뽑아 배선했다. `DESIGN_ART.md` §6 의 **1~6번이 이걸로 끝난다.**

> 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) **요청-27** · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-31**
> **코드 0줄** — CONTENT 가 요청 전에 꽂을 자리를 먼저 봐 두었다.

### 왜 DEV 가 했나

`DESIGN_ART.md` §6 의 1~5번은 CONTENT 가 `Tools/Art/*.py` 로 절차적으로 만들었다.
**인물·배경만 그게 안 된다** — 그리고 **Unity AI 생성은 DEV 만 할 수 있다**(`SESSION_PROMPT.md` §3).

| 파일 | 규격 | 모델 |
|---|---|---|
| `Sprites/UI/ShopKeeper.png` | 1024² · PPU 1024 · Point | `gemini-3.1-flash` · **`Mage.png` 을 스타일 참조로 넣었다** |
| `Sprites/UI/MainMenuBG.png` | **1344×768 (16:9)** · PPU 100 · Bilinear | `flux-2-dev` — 해상도 지정이 되는 몇 안 되는 모델 |

### 🔴 `I-41` 가짜 투명이 또 났다 — 말로 요청해도 안 통한다

프롬프트에 이렇게 **명시**했다:
*"Background must be fully transparent — no checkerboard pattern, no grey fill, no ground shadow plane."*

그런데 실측이 이랬다:

```
[D42-A] ShopKeeper 1024x1024 | 알파 min 255 max 255 | 색 100342종   🔴 가짜 투명
[D42-A]   모서리 알파 = 255, 255, 255, 255
```

`RemoveImageBackground`(`photoroom-bg-removal`) 를 돌린 뒤:

```
[D42-A2] ShopKeeper | 알파 min 0 max 255   ✅ 진짜 투명
[D42-A2]   모서리 알파 = 0, 0, 0, 0
```

🔑 **눈으로는 구분이 안 된다.** 체커 무늬가 RGB 에 그려져 있으면 미리보기가 똑같아 보인다.
CONTENT 가 `C28` 에서 이 함정을 표로 남겨 두지 않았으면 그대로 넘겼을 것이다.

### 🔴 CONTENT 의 배치 지시를 하나 안 따랐다 — 따랐으면 안 보였다

요청-27 은 *"메인 메뉴 배경은 `MainMenuPanel` 하위 **맨 뒤(형제 순서 0)**"* 라고 했다.
그 자리에 있는 게 **`DimBG` 이고 알파가 `1.00`** 이다 — **완전 불투명**이다.

⇒ 0번에 넣으면 **배경이 통째로 가린다.** `DimBG` **바로 뒤(순서 1)** 에 넣었다.
`raycastTarget` 은 꺼서 버튼 클릭을 안 막는다.

### 🟡 배치에서 한 번 틀렸고 실측이 잡았다 — `pivot` 이 위쪽이었다

상점 주인을 `y = -740` 에 놓았더니 패널 밖으로 나갔다:

```
주인 화면 y  -19 ~ 118        패널 화면 y  45 ~ 453     🔴 삐져나감
```

원인은 형제(`NpcDialogueText`)에서 복사한 `pivot` 이 **`(0.5, 1.0)` 위쪽 피벗**이라
`anchoredPosition.y` 가 **중심이 아니라 윗변**이었다는 것이다.
통계 마지막 줄이 `-620` 에서 끝나고 패널 바닥이 `-900` 이므로 그 사이 **280px** 에
`270×270` 을 넣었다 → 주인 `47 ~ 170` · 패널 `45 ~ 453` **안에 들어간다.**

### 검증 7/7

| # | 통과값 | 실측 |
|---|---|---|
| ① 알파 | 최솟값 0 · 모서리 4곳 0 | **0** · **0,0,0,0** ✅ |
| ② 임포트 | 주인 `PPU 1024`/Point · 배경 `PPU 100`/Bilinear | 그대로 ✅ |
| ③ 배경 밝기 | 중앙 세로 띠 평균 ≤ 0.25 | **0.150** ✅ |
| ④ 세로 점유 | 75~88 % | **86.1 %** ✅ |
| ⑤ 화면 | 상점에 주인 · 메뉴에 배경 | 캡처 ✅ |
| ⑥ 글자 가독성 | 타이틀 + 버튼 4개 전부 | 캡처 ✅ |
| ⑦ 콘솔 | 0 | 0 ✅ |

밝기는 `0.2126R + 0.7152G + 0.0722B` 로 **가로 중앙 1/3 띠 전체**를 평균 냈다.
CONTENT 가 다시 잴 때 같은 정의를 쓰도록 회신에 적어 뒀다.

### 🟡 남은 것

- **주인 발밑에 자갈 바닥이 붙어 나왔다.** 프롬프트에 *"no ground shadow plane"* 을 넣었는데도다.
  실측한 세로 점유 86.1 % 에 그게 포함돼 있다. 어두운 패널 위에선 안 어색해서 그냥 뒀고
  지울지는 CONTENT 판단으로 넘겼다
- **상점 대사는 여전히 빈칸**이다 — `B11`. 이건 그림과 무관하고 사용자 판단 대기 중이다

---

## 2-68. ✅ 승급이 조용하지 않다 — **정지한 화면에서 연출이 얼어붙었다** (D41, 2026-09-04 64차)

**한 줄:** 메타 아이콘 7장을 물리고, 승급·레벨업에 파동 연출을 넣었다.

> 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) **요청-26** · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-30**

### 무엇이 문제였나 — 비중이 거꾸로였다

CONTENT 가 짚은 그대로다:

| 사건 | 연출 |
|---|---|
| 보스 **등장** | 화면 흔들림 `0.55 / 0.7` |
| 엘리트 **처치** | 히트스톱 |
| 🔴 **직업 승급** | **아무것도 없음** |

`I-57` 이 겉모습(초상·걷기)은 바꿔 놨지만 **바뀌는 순간**은 여전히 조용했다.
메타 강화 카드도 `Icon` 이 비어 **글자만** 나왔다.

### 만든 것

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Fx/PulseFx.cs` **(신규)** | 링 여러 겹이 시차를 두고 퍼지며 사라지고 가운데 별이 터진다. **승급·레벨업이 같은 스크립트를 쓴다** — 다른 건 색·크기·겹수뿐 |
| `Assets/Prefabs/Fx_Promote.prefab` **(신규)** | 금색 `(1, 0.82, 0.35)` · 링 3겹 · 반경 2.6 · 흔들림 `0.35/0.30` |
| `Assets/Prefabs/Fx_LevelUp.prefab` **(신규)** | 청록 `(0.45, 0.92, 1)` · 링 2겹 · 반경 1.5 · **흔들림 0** |
| `EvolutionManager.cs` | `promoteEffect` 필드 + `PlayPromoteFx()` (🔴 `?.` 안 씀 — I-24) |
| `SceneWiring.csv` | 2줄 — `HUDManager,levelUpEffect` · `EvolutionManager,promoteEffect` |

🔑 **레벨업은 안 흔든다.** 레벨업은 자주 나서 매번 흔들면 피로하고,
그러면 **승급의 흔들림이 특별하지 않게 된다.**

### 🔴 검증 중에 내 결함을 잡았다 — `timeScale = 0`

처음엔 `Time.deltaTime` 을 썼다. 판정 ④ 를 재다가 이게 나왔다:

```
[D41-T] Time.timeScale = 0 · GameState = LevelUp
[D41-T]   Fx_Promote(Clone) Ring0 scale 0.30 alpha 1.00     <- 얼어붙어 있다
```

🔑 **이 연출이 뜨는 두 순간이 둘 다 게임이 멈춰 있는 때다.**
레벨업은 `GameState.LevelUp` 이 `timeScale = 0` 으로 만들고, 승급도 레벨업 카드 경로면 같다.
⇒ 연출이 도는 게 아니라 **정지한 금색 링이 화면에 그대로 붙어 있는다.**

`Time.unscaledDeltaTime` 으로 고쳤다. 고친 뒤 같은 조건에서 `PulseFx` 가 **3 → 0** 으로
끝까지 돈다 — **고치기 전엔 `1` 에서 멈춰 있었으니 대조군이 성립한다.**

> ⚠️ **흔들림은 정지 중엔 여전히 안 난다.** `CameraController.cs:84` 가
> `Time.deltaTime <= 0` 이면 `LateUpdate` 를 통째로 조기 반환한다.
> 승급 흔들림이 보이는 건 **제단(`E`) 승급**처럼 게임이 도는 중의 경로뿐이다.
> 카메라 쪽 설계라 여기서 안 건드렸다.

### 검증 5/6 PASS · 1 보류

| # | 무엇 | 결과 |
|---|---|---|
| ① | Import | ✅ `Upgrades : 7` · `!` 줄 0건 |
| ② | 🔴 아이콘이 실제로 물렸나 | ✅ **7/7.** `LoadRef` 는 경로를 못 찾아도 조용히 넘어가므로 **애셋의 `Icon` 필드를 하나씩 읽었다** |
| ③ | 메타 화면 | ✅ 카드 7장에 그림 — 캡처 |
| ④ | 🔴 승급 순간 | ✅ `EvolveClass(Sentinel) = True` · `PulseFx` **0 → 1** (조건을 실제로 채워 돌렸다 — Gun Lv5 + Turret Lv3) |
| ⑤ | 레벨업 | 🟡 **아래** |
| ⑥ | 링 모양 | ✅ 가운데가 비고 테두리가 얇다 — 캡처 |
| ⑦ | 콘솔 | ✅ 에러·경고 0 |

### 🟡 ⑤ 는 통과라고 못 쓴다 — 뜨는데 **안 보인다**

캡처 두 장이 갈랐다.

| | 결과 |
|---|---|
| 레벨업 패널이 떠 있을 때 | 🔴 **파동이 하나도 안 보인다** |
| 같은 순간 **패널만 끄고** 찍으면 | ✅ 플레이어 자리에 청록 링 2겹 (`scale 2.27 · alpha 0.52`) |

⇒ 배선도 스프라이트도 맞다. **순서가 문제다** — `HUDManager.OnLevelUp` 이 레벨업 패널이
열리는 **그 프레임**에 월드 공간 파동을 띄우는데, Canvas 가 `ScreenSpaceOverlay` 라
**월드 스프라이트는 무조건 그 아래**다. `sortingOrder` 로는 못 이긴다.

고칠 방법 3안은 전부 설계 결정이라 **고르지 않고** [`TODO.md`](TODO.md) 에 올렸다.

🔑 **승급 쪽은 이 문제가 없다** — 제단(`E`) 승급은 패널 없이 게임 중에 일어나 그대로 보인다.
즉 `C34` 가 하려던 것은 이미 이뤄졌다.

### 🟡 곁일 — 고아 애셋

`UpgradeDefinition` 애셋이 **8개**인데 `Upgrades.csv` 는 7행, 씬 배열도 7칸이다.
`UpGoldGain.asset` 이 어디에도 없는 옛 잔재다. **깨진 건 아니라** 버그로 안 올렸고,
`요청-28` §3 의 교훈대로 **참조를 재 보고** 지우지 않은 채 CONTENT 에 알렸다.

---

## 2-67. ✅ 탄환이 갈리고 UI 에 테두리가 생겼다 — **테두리가 버튼보다 컸다** (D40, 2026-09-04 63차)

**한 줄:** CONTENT `C33` 의 그림 5장을 임포트·배선했다. **코드 변경 0.**

> 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) **요청-25** · 회신 [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-29**

### 무엇이 문제였나

1. **`Proj_Bullet` 과 `Proj_EnemyBolt` 가 같은 스프라이트**(`ICON/Bullet.png`)를 썼다.
   색만 달랐는데 이 게임은 색 신호를 이미 셋 쓴다(엘리트 외곽선 · 피격 플래시 · 적탄 틴트).
2. 씬의 `Image` **72개 중 68개가 스프라이트 없는 단색**이었다.
3. 🔴 `spriteBorder` 는 **0 이어도 에러가 안 난다.** 9-slice 만 조용히 죽고
   "프레임이 이상하다"로 보인다 — CONTENT 가 *"이 요청에서 유일하게 조용히 실패하는 자리"* 라고 짚었다.

### 🔑 요청서에 없던 값을 하나 정해야 했다

`spriteBorder` 는 표대로 넣으면 되는데, **그것만으로는 작은 버튼이 뭉개진다.**

```
Prefab_ShopRemoveRow/RemoveButton  =  38 x 38 px
UI_Button 테두리                    =  위 20 + 아래 20 = 40 px    <- 버튼보다 크다
```

⇒ `Image.pixelsPerUnitMultiplier` 로 화면상 테두리를 줄였다. 규칙 하나로 통일했다 —
**테두리 총합이 짧은 변의 60 % 를 넘으면 그 비율만큼 배율을 올린다.**

| 대상 | 크기 | 배율 | 화면 테두리 |
|---|---|---|---|
| `ItemCard/SelectButton` | 130×30 | 2.2 | 20 → **9px** |
| `ShopRemoveRow/RemoveButton` | 38×38 | 1.8 | 20 → **11px** |
| `UpgradeCard/BuyButton` | 210×48 | 1.4 | 20 → **14px** |
| `PausePanel/*Button` | 300×60 | 1.1 | 20 → **18px** |
| 340×78 이상 | — | 1.0 | 20px 그대로 |

### 🔴 68개를 전부 바꾸면 안 된다 — 성격으로 갈랐다

| 씌운 것 | 개수 |
|---|---|
| 큰 판 → `UI_Panel` | 10 |
| 카드 → `UI_Card` | 5 (씬) + 5 (프리팹) |
| 버튼·노드 → `UI_Button` | 23 (씬) + 6 (프리팹) |
| **합계** | **49** |

| 🔴 손대지 않은 것 | 개수 | 왜 |
|---|---|---|
| `DimBG` | 9 | 전체화면 암전. 테두리가 생기면 안 된다 |
| 슬라이더 `Background`/`Fill`/`Handle` | 11 | **체력바에 테두리가 생긴다** |
| `CategoryBar` | 3 | 높이 6~16px 짜리 띠 |
| `Icon` · 초상 | 6 | 런타임에 스프라이트가 꽂힌다 |
| `StatusDot` · `Viewport` | 3 | 점 / 마스크 |

### 검증 6/6

| # | 무엇 | 결과 |
|---|---|---|
| ① | 임포트 | ✅ 5장 `alphaIsTransparency 1` · 탄환 `PPU 512`/Point · UI `PPU 100`/Bilinear |
| ② | 🔴 `spriteBorder` | ✅ 40/28/20. **임포터가 아니라 `Sprite.border` 에서 읽었다** |
| ③ | 탄환 크기 | 🟡 아래 참고 |
| ④ | 🔴 모양 구분 | ✅ 청록 예광탄(방향 있음) vs 붉은 방사 가시구슬(방향 없음) — 캡처 |
| ⑤ | 9-slice | ✅ 메인 메뉴 · 레벨업 · 상점 3화면 캡처. 모서리 안 뭉개짐 |
| ⑥ | 콘솔 | 에러 0 (아래 두 건은 이 작업과 무관) |

### 🟡 ③ — CONTENT 의 전제가 틀렸고, 결론은 맞았다

CONTENT 는 *"`50px ÷ 100 = 0.5 유닛` 이라 기존과 같다"* 고 적었는데 **실측은 41px** 이었다
(원본 이름이 `icons8-총알-50` 이라 50 으로 본 듯하다. 실제 rect 는 여백이 트리밍돼 41px).

```
ICON/Bullet.png   41px / 100 = 0.41 유닛
PlayerBullet.png 256px / 512 = 0.50 유닛   ->  +22 %
```

🔑 **그래도 게임플레이 변화는 0 이다. 다만 이유가 다르다** — 크기가 같아서가 아니라
**명중 판정이 스프라이트와 무관**하기 때문이다:

- 플레이어 탄 — `CircleCollider2D` **radius 0.5** (프리팹 고정)
- 적 탄 — 콜라이더가 **아예 없다.** `EnemyProjectile.cs:13` 이 거리 검사 `HitRadius = 0.45f` 로 하고,
  주석에 *"스프라이트 크기와 무관한 고정값"* 이라고 적혀 있다

⇒ 오히려 **덜 어긋난다.** 예전엔 0.41 유닛 그림이 반경 0.5 판정을 갖고 있었다.

### 콘솔에 나온 두 가지 — 둘 다 이 작업 것이 아니다

| 무엇 | 판정 |
|---|---|
| `ProjectileBase.Despawn` NRE 4건 | 🟢 **내 시험 탓이다.** `Initialize` 없이 프리팹만 놓아 `Pool` 이 null 이었다. 실제 경로는 항상 `Initialize` 를 거친다. (다만 `EnemyProjectile.Despawn` 은 null 가드가 있고 `ProjectileBase.Despawn` 은 없다 — 지금은 안 터지는 비대칭) |
| 한글 tofu 경고 25건 | 🔴 **진짜 버그.** 상점 NPC 대사 → [`Parallel/BUGS.md`](Parallel/BUGS.md) `B11` 등재. 고치지 않았다 |

### 🔴 곁일 — `B11` 을 잡았다. `B4` 가 놓친 자리다

상점 캡처에서 `MERCHANT` 대사가 **전부 빈칸**이었다.
`ShopUI.cs:47` 의 **코드 기본값은 영문**인데 `[SerializeField]` 라서
**씬에 저장된 옛 한글 값이 코드를 덮고 있었다**(실측 5줄 전부 한글).

🔑 **`B4` 가 같은 병을 고치면서 이 자리를 놓쳤다.** `B4`/`D5` 는 씬 **6곳**을 영문화해 닫았는데
그 6곳은 전부 `TextMeshProUGUI.text` 였다. 이건 **`string[]` 배열 필드**라
텍스트 컴포넌트를 훑는 방식으로 안 잡힌다.

---

## 2-66. ✅ `B3` 를 닫았다 — **"맨 앞에 넣는다"가 답이 아니었다** (D39, 2026-09-04 62차)

**한 줄:** 건물 설치 대기열을 **신규/증설 두 구간**으로 나눠, 새로 얻은 건물이 먼저 나오게 했다.

> `Parallel/BUGS.md` `B3` · 사용자 지시 2026-09-04 (`D2` 의 B안 위에 **C안**을 얹는다)

### 원인 — 버그가 아니라 설계였다

`BuildingManager.UnlockBuilding` 은 이렇게 돌고 있었다:

```csharp
int allowed = data.GetMaxCount(level);
for (int owned = PlacedCount(data) + PendingCountOf(data); owned < allowed; owned++)
    _pendingQueue.Add(data);          // ← 언제나 맨 뒤
```

`GetMaxCount` 는 **레벨에 따라 커진다.** Turret 을 레벨업 카드로 찍으면 그 순간
대기열에 Turret 이 **말없이 더 쌓인다.** 그 뒤 Village 를 얻으면 맨 뒤에 붙고,
`PlaceNext` 는 `_pendingQueue[0]` 부터 꺼내므로 밀린 Turret 이 먼저 나온다.

⇒ 예외도 null 도 아니다. **레벨업 카드가 "Village 획득"이라고 한 약속을 조작이 안 지킨** 것이다.

### 🔑 원안(C)을 그대로 쓰지 않았다

`BUGS.md` 의 C안은 *"새로 해금된 건물은 큐 맨 앞에 넣는다"* 였다. 그대로 넣으면 구멍이 생긴다 —
Village 를 얻고 아직 못 세운 상태에서 Farm 을 얻으면 **Farm 이 Village 를 추월한다.**
약속을 하나 지키려다 다른 약속을 깬다.

그래서 **"맨 앞"이 아니라 "신규 구간의 끝"** 에 넣는다:

| 구간 | 무엇 | 코드 |
|---|---|---|
| 앞 `_freshCount` 개 | **처음 해금된 건물** | `_pendingQueue.Insert(_freshCount++, data)` |
| 그 뒤 | **레벨업 증설분** | `_pendingQueue.Add(data)` |

구간 **안에서는 FIFO 가 유지**되므로 신규끼리도 얻은 순서를 지킨다.
규칙 한 줄로 설명된다 — **"처음 얻은 건물이 먼저. 레벨업으로 늘어난 몫은 그 뒤."**

### 🔴 `_freshCount` 는 장부다 — 큐를 만지는 모든 경로가 같이 맞춰야 한다

이게 이 작업에서 제일 깨지기 쉬운 부분이라 경로를 전부 세었다.

| 경로 | 무엇을 하나 | 대응 |
|---|---|---|
| `PlaceNext` | 맨 앞 제거 (**3곳**: 정상 설치 · 망가진 항목 · `BuildingBase` 없음) | `RemoveFront()` 헬퍼로 **통일**. `RemoveAt(0)` 직접 호출 금지 |
| `LockBuilding` | 상점에서 건물 제거 — **임의 위치** `RemoveAll` | 신규 구간(`0.._freshCount`) 안에서 몇 개가 빠지는지 **먼저 세고** 뺀다 |
| `ResetRunState` | 런 시작 | `_freshCount = 0` |

### 바꾼 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Building/BuildingManager.cs` | `_freshCount` 필드 · `UnlockBuilding` 분기 · `RemoveFront()` 헬퍼 · `LockBuilding` 보정 · `ResetRunState` · 클래스 주석 · `public const int Version = 2` |
| `Assets/Scripts/UI/HUDManager.cs` | `RefreshBuildPrompt` 주석이 **옛 동작을 현재형으로 설명**하고 있었다. B안/C안 관계로 다시 씀 (표시는 그대로 남긴다 — 대기가 여럿일 때 다음 것은 여전히 안 보인다) |

### 검증 — 리포트의 재현 절차 그대로 (플레이 모드)

```
1. Turret 해금       → NextPending=Turret      → Z 설치됨
2. Restaurant 해금   → NextPending=Restaurant  → Z 설치됨
3. Turret Lv2 · Restaurant Lv2 (증설) → 대기 1개, 맨 앞=Turret
4. Village 해금      → NextPending=Village     ← 🔑 여기서 뒤집힌다
5. Farm 도 해금      → NextPending=Village     ← 추월하지 않는다
6. Z 순서 = Village > Farm > Turret
```

🔑 **3번이 대조군이다.** Village 를 얻기 **직전** 맨 앞이 `Turret` 이었고 4번에서 `Village` 로
바뀌었다 — 구버전이면 4번이 `Turret` 으로 찍힌다. 즉 이 시험은 **아무 값이나 통과시키지 않는다**
(`D34` 에서 배운 것: 통과만 하는 시험은 시험이 아니다).

`LockBuilding` 장부는 따로 시험했다:

```
신규3(Village,Farm,Bombard) + 증설1(Turret) → 맨 앞=Village (대기 4)
상점에서 Village 제거                        → 맨 앞=Farm    (대기 3)
Village 재해금 후 Z 순서 = Farm > Bombard > Village > Turret   (기대값 일치)
```

- 컴파일: `error CS` **0건**, `BuildingManager.Version = 2` 조회로 **새 코드 로드 확인**(D27 절차)
- 런타임: 콘솔 Error·Warning **0건**
- 임시 오브젝트·스크립트 없음 (`RunCommand` 로만 검증)

---

## 2-65. ✅ CONTENT 의 아트 순서를 받았다 — **내가 요청한 것이 7순위였다** (D38, 2026-09-04 61차)

**한 줄:** 보스 그림 요청을 **보류로 내리고** CONTENT 의 순서를 받았다.
그리고 그쪽 정리 후보 3개를 실측해 **하나가 지우면 안 되는 것**임을 잡았다.

> 사용자 결정 2026-09-04 · CONTENT [`DESIGN_ART.md`](DESIGN_ART.md)(`C32`) 에 대한 회신
> **코드·씬은 한 줄도 안 건드렸다.** 문서와 요청서만 바뀐다.

### 원인 — "무엇이 없나"를 세지 않고 "내가 만들 수 있는 것"을 떠올렸다

`D37` 끝에 나는 `요청-27` 로 **보스 전용 도트 그림**을 요청했다. 근거는 *"보스가 잡몹과 같은
스프라이트를 쓴다"* 였고, 그건 사실이다. 문제는 **그게 지금 제일 급한 구멍이냐**였다.

CONTENT 가 씬을 실측해 답을 냈다:

| CONTENT 가 센 것 | 값 |
|---|---|
| 씬의 `Image` 컴포넌트 | **60개** |
| 그중 **스프라이트 없는 단색** | **59개** |
| 적 스프라이트 | 6종 ✅ |
| 직업 초상 | 10종 ✅ |
| 아이콘 | 34장 ✅ |
| **UI 그래픽** | **0장** 🔴 |

🔑 **이 표가 내 전제를 뒤집었다.** 그리고 진단이 한 번 더 아팠다 —
*"Unity AI 로 뽑기 쉬운 것(인물·몬스터)만 채워졌다"* 에서 **그 채운 사람이 나다**
(`D26` 직업 6장 · `D31` 보스 패턴 · `D32` 직업 3종).
나는 **이미 제일 잘 채워진 칸을 한 번 더 채우자고 요청한 것**이었다.

보스 그림에 대한 CONTENT 의 판정은 **7순위(보류)** 였고 이유는 한 줄이었다 —
*"층별 난이도 기능이 먼저다. 기능 없이 배경만 갈면 뭐가 달라졌는지 안 보인다."*

### 바꾼 것

| 파일 | 무엇 |
|---|---|
| `Docs/Parallel/REQ/CONTENT.md` | `요청-27` 을 **🅿️ 보류**로 낮추고 원문은 `<details>` 로 접어 보존 (재개 조건 명시) |
| `Docs/Parallel/REQ/CONTENT.md` | **`요청-28` 추가** — 순서 수용 · 1~5번은 CONTENT 가 만들고 DEV 는 배선만 · §7 정정 |
| `Docs/ROADMAP.md` §2-5 | 보류 표시 + 재개 조건 |
| `Docs/TODO.md` | 보스 그림 절을 보류로 접고, **"아트 — DEV 는 배선만"** 절을 새로 넣었다 (순서 6줄 + 정리 후보 정정표) |
| `Docs/Parallel/BOARD.md` | `D38` 선점·다음 번호 `D39` |

### 🔴 곁일에서 진짜를 하나 잡았다 — 정리 후보 3개 실측

`DESIGN_ART.md` §7 이 애셋 3개를 *"아무 데도 안 쓰인다"* 며 정리 후보로 올렸는데,
**본인이 *"지우기 전에 guid 로 참조를 확인할 것"* 이라는 단서를 같이 달아 뒀다.**
그 확인은 DEV 몫이라 실행했다.

```
goblin.png   guid=588cf3bf  →  Assets/Prefabs/Enemy_Goblin.prefab            (1곳)
Exp_Orb.gif  guid=985386b2  →  Assets/Prefabs/ExpDrop_Small.prefab           (1곳)
Bullet.png   guid=1f4e1489  →  Assets/Prefabs/Proj_Bullet.prefab
                               Assets/Prefabs/Proj_EnemyBolt.prefab          (2곳)
```

**셋 다 참조가 있었다.** 다만 참조가 있다고 다 같은 뜻은 아니라, 코드까지 봤다:

| 파일 | 런타임에 덮어써지나 | 판정 |
|---|---|---|
| `goblin.png` | ✅ `EnemyBase.cs:124` — `if (Data.Sprite != null) sr.sprite = Data.Sprite;` **매 소환마다** | 🟡 화면엔 안 보인다. 다만 지우면 **프리팹에 missing 참조**가 남는다 |
| `Exp_Orb.gif` | 🔴 **아니다** — `ExpDrop.cs` 에 `sprite` 대입이 **0건** | 🔴 **지우면 경험치 구슬이 통째로 안 보인다.** 한 판에 수백 개가 뜨는 물체의 **유일한 그림**이다 |
| `Bullet.png` | 아니다 | 🟡 §1(탄환 2종)이 끝난 **뒤에만** |

### 검증

- 문서 편집만 했다. **`Assets/` 아래는 아무것도 안 바뀌었다** (`git status` 로 확인)
- guid 검색 대상은 `Assets/**` 의 `*.unity` · `*.prefab` · `*.asset` · `*.csv` 전체

---

## 2-64. ✅ 이벤트가 처음으로 **눈에 보인다** — E7·E9·E11 (D37, 2026-09-03 60차)

**한 줄:** 배관만 있던 이벤트에 **화면과 선택지**가 생겼고, 이벤트 3종이 들어갔다.

> 설계 [`DESIGN_EVENTS.md`](DESIGN_EVENTS.md) §3 · `ROADMAP.md` §7 · 사용자 선택 2026-09-03

### 원인 / 배경 — 이벤트는 "공짜 XP 또는 노말 웨이브 한 번" 이었다

`EventManager` 는 **58줄짜리 스텁**이었다. 할 수 있는 게 `XpBonus`·`CurrencyBonus`·
`TriggerRandomWave` 셋뿐이고, 제목·설명은 **`Debug.Log` 로만** 나갔다.

| 없던 것 | 그래서 |
|---|---|
| **이벤트 UI** | **플레이어가 무슨 일이 났는지 못 봤다** → 선택형 이벤트가 성립을 못 한다 |
| **`StageType.Event` 웨이브 분기** | `WaveManager` `switch` 에 없어 노말로 떨어진다 → **전투형 이벤트가 존재할 수 없다** |

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Meta/EventManager.cs` | 스텁 → 실물. `EventKind` 4갈래 · `Accept`/`Decline` |
| `Assets/Scripts/UI/EventUI.cs` | 신규 — 제목·설명·**선택지 2버튼** |
| `Assets/Scripts/Wave/WaveManager.cs` | `StageType.Event` 분기 · `eventWaves` · `BeginMinefield` |
| `Assets/Scripts/Evolution/EvolutionManager.cs` | E9 — 제단 조건 우회 |
| `Assets/Game/Balance/Events.csv` | 열 8개 신설 · 이벤트 **5 → 8종** |
| `Assets/Editor/BalanceImporter.cs` | 새 열 + **값 실수 2종 경고** |
| `Assets/Scenes/SampleScene.unity` | `EventPanel` + `EventUI` 배선 |

### 넣은 이벤트 3종 — **이 게임에만 있는 축**을 하나씩 쓴다

| | 무엇이 다른가 | 무엇을 쓰나 |
|---|---|---|
| **E7 `Exchange`** | 런 골드를 **메타 골드로** 환전 (3:1 · 상한 40) | **두 지갑**(D25). 런 골드는 어차피 소멸하므로 "얼마를 들고 나갈까"가 진짜 선택이 된다 |
| **E9 `Field Promotion`** | **다음 전투 동안 제단 없이** 승급 | **제단 승급**. `ROADMAP` 이 *"유일한 설계 차별점"* 이라 부른 기능을 **모르는 사람에게 알려 준다** |
| **E11 `Minefield`** | 바닥에 **예고 원이 계속 깔린다** | **`BossSlam` 재활용**(D31·D34) |

🔑 **E11 에 새 시스템을 안 만들었다.** 예고 → 폭발 → 풀 반환이 이미 검증된 코드다(D34 판정 ③).
**이벤트가 다른 건 규칙이지 부품이 아니다.** 대신 보스보다 약하게 고정했다 —
반경 2.0(보스 2.4) · 피해 12(18) · 예고 **1.3초(보스 1.15초보다 관대하다)**.

### 🔴 E9 에서 진짜 결함을 하나 잡았다 — **말이 애매하면 코드도 애매해진다**

설계에 *"이번 층 동안"* 이라고 써 놓고 해제를 `StageMapManager.AdvanceToNext` 에 넣었다.
그런데 `EventManager.FinishEvent` 가 **바로 그 함수를 부른다** — 즉 수락하는 순간
**켜지자마자 스스로 꺼졌다.** 검증 로그가 `수락 후 = False` 로 잡아냈다.

근본 원인은 코드가 아니라 **정의**였다. **이벤트 노드 자체가 그 층의 내용물**이라
"이번 층"에는 효과가 쓰일 시간이 남지 않는다.
⇒ **"다음 전투 동안"** 으로 다시 정의하고 해제를 `WaveManager.ClearWave` 로 옮겼다.
UI 문구도 `Promote anywhere in your next battle` 로 고쳤다.

### 검증 로그 — 3종 전부 PASS

```
[E7]  런 골드 137 → 17   ·  메타 골드 1231 → 1271 (+40)   ·  state=StageMap
      본문: "120 run gold -> 40 meta gold (3:1)"   ← 상한 40 이 정확히 걸렸다(137 전부가 아니다)
[E9]  수락 전 False → 수락 후 True  ·  웨이브 시작 후에도 True
[E11] 지뢰 3개(MineCount) · scale 4.0(=반경 2.0) · 플레이어 반경 7 안에 분산 · 캡처 확인
[UI]  패널 켜짐 · 거절 버튼이 Exchange 에서만 보인다(Reward 는 숨김)
콘솔 에러·경고 0
```

> 🔴 검증이 실제 `save.json` 을 쓴다(E7 → `MetaProgression.Save`). 백업 후 `1271 → 1231` 복구했다.

### 임포터가 값 실수 2종을 잡는다

- **`Kind` 오타** — 모르는 문자열은 **조용히 `Reward` 로** 떨어진다. 게임이 못 잡으므로 Import 가 잡는다
- **선택형인데 `DeclineLabel` 이 빈 경우** — 거절 버튼이 안 떠서 **선택이 아니게 된다**

### 🟡 되살린 것 — `Cursed Offering`

`Events.csv` 를 통째로 다시 쓰면서 CONTENT 가 쓴 행 하나를 **실수로 지웠다가** 되살렸다.
지금은 기존 5종 + 신설 3종 = **8종**이다.
⇒ 남의 소유 CSV 를 다시 쓸 때는 **행 수를 먼저 세고 나중에 대조한다.**

### 안 한 것

- **수치 확정** — 환율 3:1 · 상한 40 · 지뢰 3개/2초는 **임시**다. 근거가 "메타 수입 132G" 하나뿐이다
- **`E1` 함정방** — 설계는 끝났지만 §7 **결정 6건**이 먼저다
- **E8 · E10 · E12** — 카탈로그에 이름만 있다
- **`eventWaves` 배열이 비어 있다** — 지금은 노말로 떨어진다. 전용 웨이브는 CSV 몫

---

## 2-63. ✅ 안내 문구 가독성 + 통계 표시 (D36, 2026-09-03 59차)

**한 줄:** `ROADMAP` 5단계 중 **사용자가 고른 둘**만 했다. 게임패드는 지시로 스킵.

> 근거 [`ROADMAP.md`](ROADMAP.md) §3-3 · §8 12번 · 사용자 결정 2026-09-03

### 원인 / 배경

`ROADMAP` 5단계는 셋이었다 — 게임패드 실배선 · 없는 화면들 · 콘텐츠 볼륨.
사용자가 이렇게 정했다:

| | 결정 |
|---|---|
| 조작 안내 | 🔴 **새 화면을 만들지 않는다.** *"조건이 갖춰졌을 때 뜨는 글자 가독성만 높여도 괜찮다"* |
| 통계 | 작업 |
| 이벤트 | **제안만** 받고 착수는 뒤로 |
| 게임패드 | **스킵** |

### 1. 안내 문구 가독성 — 조건부 표시는 이미 돼 있었다

읽어 보니 `[Z] Build` 와 `[E] PROMOTE` 는 **조건이 충족될 때만 뜨게 이미 잘 만들어져 있었다**
(`HUDManager.RefreshBuildPrompt` · `EvolutionPromptUI`). 문제는 **보이느냐**였다.

| | 이전 | 이후 |
|---|---|---|
| `outlineWidth` | **0.00** | **0.22** (검정) |
| underlay(그림자) | **0.00** | 소프트니스 0.25 · 알파 0.75 |
| fontSize | 26 / 30 | **32 / 36** |

🔴 **공유 머티리얼을 고치면 안 됐다.** 둘 다 `Pretendard SDF Material` 을 쓰는데,
거기에 외곽선을 켜면 **그 폰트를 쓰는 텍스트 75개가 전부** 바뀐다.
전용 파생본 `Assets/Fonts/Pretendard SDF - Prompt.mat` 을 만들어 **두 개에만** 물렸다.

#### 🔴 그리고 한 번 망가뜨렸다 — TMP 는 `_ScaleRatio` 를 다시 계산해야 한다

머티리얼을 복제하고 `OUTLINE_ON`/`UNDERLAY_ON` 키워드만 켰더니
**글자가 통짜 청록색 덩어리로 나왔다.** TMP 는 외곽선·그림자를 켜면
`_ScaleRatioA/B/C` 를 다시 계산해야 하는데 그걸 안 했기 때문이다.

```csharp
ShaderUtilities.GetShaderPropertyIDs();
ShaderUtilities.UpdateShaderRatios(mat);   // ← 이게 빠져 있었다
```

⇒ `A=0.900 B=1.000 C=0.731` 로 채워지고 정상 렌더됐다. **캡처를 안 했으면 못 잡았다.**

### 2. 통계 — 새 화면을 만들지 않고 메타 화면에 붙였다

`SaveData.TotalRuns`·`TotalKills` 는 **예전부터 쌓이고 있었는데 볼 곳이 없었다**(`ROADMAP` §3-3).
새 화면 대신 `D30` 이 만든 메타 화면 하단에 한 줄로 넣었다 —
**화면을 더 만들면 그만큼 더 안 보게 된다.**

```
RUNS 25   KILLS 8,885   AVG 355/run   TIME 0m
```

➕ **`SaveData.TotalPlaySeconds` 를 새로 넣었다.** `WaveManager.TotalElapsedTime` 이
이미 쌓이고 있었는데 저장이 안 됐다. `RegisterRunResult(kills, seconds)` 로 호출부 2곳을 고쳤다.

> ℹ️ **기존 세이브는 안 깨진다** — `JsonUtility` 는 없는 필드를 기본값(0)으로 읽는다.
> 그래서 예전 25판은 `TIME 0m` 으로 시작하고 **다음 판부터 쌓인다.** 화면의 `0m` 은 정상이다.

### 3. 배치를 한 번 틀렸다

`StatsText` 를 **top 앵커**(`0.5, 1`)로 만들었더니 카드 위에 겹쳤다.
같은 패널의 다른 요소들은 **center 앵커**(`0.5, 0.5`)라 기준이 달랐다.
center 로 맞추고 `SetAsLastSibling()` 으로 그리기 순서도 정리했다.
🔑 **패널에 요소를 더할 때는 그 패널이 이미 쓰는 앵커를 따라간다.**

### 검증

- 안내 문구 — 캡처로 확인. 어두운 타일 위에서 또렷하고 `[E]` 노란 강조가 살아 있다.
  **전용 머티리얼 사용 = 2개 · 나머지 텍스트 75개는 그대로**
- 통계 — 실제 세이브값(`TotalRuns 25` · `TotalKills 8885`)이 그대로 표시됐다. 캡처 확인
- 콘솔 에러·경고 **0**

### 안 한 것

- **게임패드 실배선** — 사용자 지시로 스킵. `PlayerInput` + 액션 콜백 전환은 입력 계층 교체라
  범위가 크고, 포트폴리오 화면에는 안 드러난다
- **이벤트** — 제안 12건을 냈고 **착수는 사용자 선택 대기**다.
  🔴 어떤 이벤트든 **이벤트 UI** 와 **`StageType.Event` 웨이브 분기** 둘이 선행이다
- **해상도/그래픽 설정 · 키 바인딩 · 도감 · 크레딧** — §3-3 의 나머지

---

## 2-62. ✅ 열린 버그 2건 + 성능 재측정 — **절반만 답이 나왔다** (D35, 2026-09-03 58차)

**한 줄:** `B5`·`B7` 을 닫았다. 성능은 **시나리오 A 만 재현됐고 B 는 못 했다.**

> [`Parallel/BUGS.md`](Parallel/BUGS.md) `B5`·`B7` · [`PERF.md`](PERF.md) §8-8

### 1. `B5` — 웨이브 밖 사망 NRE (광역기가 첫 사망자에서 통째로 멈춘다)

`WaveManager.OnEnemyKilled` 이 `_currentWaveData.UseKillClear` 를 그냥 참조했다.
웨이브가 안 도는 중에 적이 죽으면 NRE 가 나고, 그게 `EnemyBase.Die()` 를 통째로 걷어차
**사망 연출·소리·시체 정리가 전부 건너뛰어진다.**

🔴 **광역기에서는 더 나쁘다** — `OverlapCircleAll` → `foreach` → `TakeDamage` 루프 밖으로
예외가 나가 **같은 반경 안의 나머지 적이 피해를 아예 안 받는다** (D20 이 실제로 봤다).

**고친 것:** `_currentWaveData != null` 한 줄. ➕ **`SpawnMinion`(D31 에 내가 만든 공개 API)에도
같은 가드를 넣었다** — 같은 모양이었다.

**검증은 광역 사례로 했다:**

```
[B5] state=MainMenu (웨이브 미시작 = _currentWaveData null)
  배치: Demon(HP 160) · Ogre(HP 220)
  루프가 때린 수 = 2      ← 고치기 전이면 1 에서 끊긴다
  Demon: dead=True  · 활성 시체 0구  ← 사망 연출까지 정상
  Ogre : HP 26 (=220-200+방어6)     ← 안 고쳤으면 220
  콘솔 NullReference 0건
```

### 2. `B7` — 승급 4종 임포트 설정

`D26` 명령을 경로만 바꿔 돌렸다. **7종이 전부 같은 설정으로 수렴했다** —
초상 `ppu=1024 maxTS=512 Point 무압축`, 걷기 `ppu=256 maxTS=1024 Point 무압축`.

🔑 **`filterMode` 만 보면 안 된다** — `maxTextureSize` 와 압축도 같이 틀어져 있었고
그 둘은 `.meta` 최상단이 아니라 `platformSettings[DefaultTexturePlatform]` 안에 있다.
`I-61` 이 PPU 만 고치고 지나간 것도 같은 이유로 보인다.

### 3. 🔴 성능 재측정 — **시나리오 A 는 건강하고, B 는 재현 못 했다**

`D27` 이 남긴 *"무기 렉의 원인 미확정(BehaviourUpdate 의 71~81%)"* 을 다시 보려 했다.
하네스는 `D27` 이 지웠으므로 **Unity 프로파일러 + `ProfilerDriver`** 로 하네스 없이 쟀다.

| 조건 | 프레임 중앙값 | 최대 | 판정 |
|---|---:|---:|---|
| 적 **415** + 무기 1자루 | **5.19 ms** | 7.37 ms | 전 프레임 60fps 예산 안 |
| 적 **815** + 무기 1자루 | **5.08 ms** | 7.30 ms | 전 프레임 60fps 예산 안 |

🔴 **`vSync` 가 실제로 켜져 있었다** (`vSyncCount=1`, 품질 5). 측정 직전에 껐다 —
안 껐으면 전부 144Hz 에 붙어 아무것도 못 봤다. `TODO.md` §1-B 에 등재한 그대로였다.

#### 🔴 이 수치로 말할 수 있는 것과 없는 것

**말할 수 있는 것** — 적만 800마리인 조건(`D27` 의 시나리오 A)은 **여유가 크다.**
`D27` 의 121 fps 보다 낫지만, `D27` 이 *"세션이 다르면 2배 흔들린다"* 고 적었으므로
**"좋아졌다"고 말하지 않는다.**

**말할 수 없는 것** — `D27` 의 문제는 **시나리오 B**(무기 켜고 카이팅하며 대량 처치)다.
이번 조건은 플레이어가 무적으로 서 있고 무기가 1자루라 **적이 거의 안 죽었고**,
그래서 `ExpDrop` 이 **0개**였다. ⚠️ **이걸 "D29 의 회수가 고쳤다"로 읽으면 안 된다** —
애초에 구슬이 생길 조건이 아니었다.

#### 🔑 곁가지로 나온 것 — `EditorLoop` 가 프레임의 **45 %**

```
Frame 2206  EditorLoop 2.178ms (45.0%) · Profiler.FlushMemoryCounters 0.494ms (10.2%) · Idle 0.476ms (9.8%)
```

⇒ **에디터 플레이 모드 수치로 "게임이 느리다"를 판정하면 안 된다.**
게임 코드가 쓰는 시간보다 에디터 오버헤드가 크다. `D27` 이 "세션 편차 2배"를 겪은 것도
이 성분이 흔들린 것일 수 있다.

### 안 한 것

- **`B3`** — 사용자가 이미 **B안**(HUD 표시)을 골랐고 *"실플레이에서 헷갈리면 A·C·D 를 다시 본다"* 로
  둔 것이다. A·C 는 큐 순서를 바꿔 **건물 채수 밸런스**에 영향이 가므로 임의로 못 바꾼다
- **시나리오 B 재측정** — 하네스가 필요하다. `D27` 이 지운 `PerfHarness` 를 다시 만들거나
  실제로 한 판을 끝까지 플레이해야 한다 → `TODO.md` §1-B

---

## 2-61. 🔴 보스 수치 확정 — **내 검증이 틀렸다는 것을 계산이 먼저 잡았다** (D34, 2026-09-03 57차)

**한 줄:** `C30` 이 `radius / windup` 로 *"아무도 못 피한다"* 를 책상에서 증명했다. 판정 **6/6**.

> 전문 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-24 처리 결과

### 🔴 원인 / 배경 — `D31` 의 판정 ⑦ 이 불충분했다

`D31` 에서 나는 *"예고 회피가 이 작업의 핵심"* 이라고 쓰고, 그것을
**`transform.position` 으로 8유닛 순간이동**시켜 확인했다. 그건 회피 **가능**을 증명한 게 아니라
**판정이 터지는 순간의 위치로 이뤄진다**만 증명한 것이다 —
순간이동은 *"이미 최적 방향으로 무한 속도로 이동 중"* 이라는 **가장 유리한 가정**이다.

`C30` 은 코드를 읽고 계산했다. `BossSlam` 은 원을 깔고 **따라오지 않으며** 터지는 순간의
거리로만 판정한다 ⇒ 필요한 건 하나뿐이다: **필요 속도 = `radius / windup`.**

**내 값은 `3.2 / 0.9 = 3.56 u/s` 였다.**

| 직업 | 이속 | 반응 0초 | 반응 0.25초 |
|---|---:|---|---|
| **Demolitionist** | 3.5 | 🔴 **3.15 < 3.2** | 🔴 2.27 |
| Ranger *(최속)* | 4.6 | ✅ 4.14 | 🔴 **2.99 < 3.2** |

**폭발광은 반응 0초·완벽한 방향으로도 못 피하고, 반응 0.25초면 레인저조차 못 피한다.**
게다가 보스에 딜을 넣으려면 무기 사거리(3.5~6) 안에 있어야 하니 **피해야 할 순간에 대개 접근 중**이다.

### 변경한 값

| 열 | 내 임시값 | **`C30` 값** |
|---|---|---|
| `SlamWindup` | 0.9 | **1.15** |
| `SlamRadius` | 3.2 | **2.4** |
| `SlamDamage` | 22 | **18** (어쎄신 HP 70 의 1/4) |
| `PhaseSpeedMult` | `1\|1.15\|1.35` | **`1\|1.5\|2.2`** (보스 실속도가 1.08 이라 1.35배는 체감이 없다) |

새 값의 필요 속도는 `2.4 / 1.15 = 2.09 u/s` 로 **전 직업이 여유롭게 벗어난다.**

### 🔑 이번엔 **걸어서** 쟀다 — 그리고 대조군을 먼저 놓았다

`PlayerController` 를 끄고 `Rigidbody2D.linearVelocity` 에 직업 실제 이동속도를 넣어
진짜로 걸어나가게 했다. 반응 0.25초는 **"예고를 0.25초 줄이고 t=0 부터 걷는다"** 로 모형화했다.

🔴 **대조군을 먼저 돌렸다** — 내 옛 값으로 **맞는 것**을 확인한 뒤에 새 값을 쟀다.
그게 없으면 "안 맞았다"가 **테스트 고장과 구별되지 않는다.** `D31` 에 빠졌던 게 정확히 이것이다.

| 조건 | 필요 속도 | 폭발광 3.50 | 결과 |
|---|---:|---|---|
| 대조군 — 옛 값 (r 3.2 · w 0.9 · 반응 0.25) | 4.92 u/s | 🔴 못 미침 | **HP 110 → 93** |
| 본 시험 — 새 값 (r 2.4 · w 1.15 · 반응 0.25) | 2.67 u/s | ✅ 1.31배 | **HP 93 유지** |

### 검증 로그 — 판정 6/6 PASS

| # | 판정 | 결과 |
|---|---|---|
| ① | `Bosses.csv` `!` 줄 0건 | ✅ |
| ② | 애셋에 새 값 반영 | ✅ `1.15 / 2.4 / 18` · `PhaseSpeedMult 1\|1.5\|2.2` |
| ③ | 🔴 폭발광으로 피할 수 있나 | ✅ 위 표 |
| ④ | 예고 원 = 판정 원 | ✅ `scale 4.8` = 2.4 ÷ 0.5 · 캡처 확인 |
| ⑤ | 3페이즈 속도 | ✅ 보스 **2.38** vs 플레이어 **3.50** — 빨라졌지만 못 쫓아온다 |
| ⑥ | 콘솔 | ✅ **0** |

### 예고 원 스프라이트 — `spriteRadiusAtScaleOne` 을 안 고쳤다

`C30` 이 만든 `BossSlamRing.png` 를 받자마자 실측했다: **512² · 알파 `0–255` · RGB 전부 흰색**
(`BossSlam` 이 `telegraph.color` 를 덮으므로 텍스처는 알파로 모양만 준다).
임포트는 `ppu=512 · Single · Bilinear · 무압축 · maxTS 512`.

🔑 **`Sprite.bounds.extents.x` 가 정확히 `0.5`** 라 프리팹 값을 **한 글자도 안 고쳤다** —
`512px ÷ PPU 512 = 1유닛 폭 = 반경 0.5` 라는 `C30` 의 계산이 맞았다.

### 안 한 것

- **`warnColor.a` 0.30 → 0.45** — 권고받았지만 보류했다. 새 스프라이트는 테두리 알파가 1.0 이라
  캡처에서 이미 또렷하다. **지금 올리면 두 변경이 겹쳐 무엇 덕인지 못 가른다.** 실플레이 후 판단
- **보스 전용 그림** — 여전히 `Ogre` 다

---

## 2-60. ✅ 메타 강화 수치 확정 — **씬 배열을 CSV 손에 넘겼다** (D33, 2026-09-03 56차)

**한 줄:** `C29` 가 잡은 값 7항목을 넣고, `upgrades` 배열을 `SceneWiring.csv` 로 옮겼다. 판정 **6/6**.

> 전문 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-23 처리 결과 · [`BALANCE.md`](BALANCE.md) §3-5

### 원인 / 배경 — 값만 넣어서는 반영이 안 됐다

`D30` 이 만든 메타 화면의 수치는 임시였고, `C29` 가 근거를 갖고 다시 잡았다.
그런데 **항목 구성이 바뀌었다** — `UpGoldGain`(Greed) 빼고 `UpCritMultiplier`(Precision) 신설.

🔴 **`MetaProgressionManager.upgrades` 는 내가 `D30` 에서 씬에 손으로 꽂은 배열이었다.**
임포터는 `.asset` 만 만들고 그 배열은 안 건드린다. 그대로 Import 하면
Greed 애셋이 배열에 남아 **화면에 계속 뜨고**, Precision 은 만들어져도 **배열에 없어 안 뜬다.**

⇒ `SceneWiring.csv` 에 줄을 하나 열어 **목록 자체를 CONTENT 손에 넘겼다.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Game/Balance/SceneWiring.csv` | **`MetaProgressionManager,upgrades` 1줄 신설** (12→13행) |
| `Assets/Game/Balance/Upgrades.csv` | `C29` 가 값 확정 (Greed 제거 · Precision 신설) |
| `Assets/Game/UpgradeData/*.asset` | `UpCritMultiplier` 신설 + 값 반영 |
| `Assets/Scenes/SampleScene.unity` | `upgrades` 배열 7칸 재배선 |

🔴 **C# 변경 0.** 임포터가 이미 범용이라 코드를 안 건드렸다.

### 🔑 임포터가 특별 취급 없이 받았다

`ImportComponentFields`(`BalanceImporter.cs:697`)는 컴포넌트를 **타입 이름**으로,
필드를 **`SerializedProperty` 이름**으로 찾고, `WriteProperty` 가 **ObjectReference 배열을 `|` 로 나눠 채운다.**
`GameManager,classes` 와 **완전히 같은 경로**다.

⇒ **앞으로 씬의 어떤 배열이든 CSV 한 줄로 CONTENT 손에 넘길 수 있다.**
순서도 안전하다 — `ImportUpgrades` 는 1차(애셋 생성), `SceneWiring` 은 4차라
`UpCritMultiplier.asset` 이 만들어진 뒤에 배선된다.

### 검증 로그 — 판정 6/6 PASS

```
Upgrades  : 7                    · ! 줄 0건
SceneWiring.csv : 13/13 적용     ← 12 → 13
씬 배열: UpPickupRadius · UpMaxHp · UpDamage · UpXpGain · UpCritMultiplier · UpArmor · UpMoveSpeed
가장 싼 칸 = Magnetism 60G
Precision Lv1 구매 → 다음 런 Final.CritMultiplier 1.50 → 1.65
```

**⑤ 가 이번의 핵심이다.** `CritMultiplier` 는 처음 쓰인 `StatKey` 라
`ApplyStatKey` 에 있는지가 관건이었다 — **오타였으면 조용히 0 이었을 자리**인데 정확히 1.65 였다.

> 🔴 검증이 실제 `save.json` 을 쓴다(`PurchaseUpgrade` → `Save`). 백업 후 되돌렸다 — `D30` 과 같다.

### 🔑 `C29` 의 정정 4건을 전부 받아들였다 — 내 근거가 얇았다

| 내가 넣은 값 | `C29` 값 | 내가 못 본 것 |
|---|---|---|
| Greed(`GoldGain`) 포함 | **제외** | `GameManager.cs:380` — **`GrantMetaGold` 도 `GoldGain` 을 곱한다.** 메타 골드로 사서 메타 골드를 늘리는 되먹임이라 "안 사면 손해"가 된다 |
| `Armor` 최대 +4 | **+2** | `PlayerStats.cs:316` 이 `Mathf.Max(1, raw - Armor)` — **정률이 아니라 정액 + 바닥 1.** +4 면 좀비까지 바닥값이라 비엘리트 접촉이 무의미해진다 |
| `MoveSpeed` +0.7 | **+0.2** | `DESIGN_CLASSES.md` §7-B 가 레인저 기동을 `+0.6` 으로 못박았다. **한 직업이 돈 주고 사는 것을 모두가 공짜로 받으면 그 직업이 사라진다** |
| 총 8,600G (65판) | **3,910G (30판)** | 마감이 2026-09-07 이고 보는 사람은 1~3판 한다 — **65판 곡선에서는 이 화면이 아예 안 보인다** |

**내 근거는 "메타 수입 132G" 하나였고, `C29` 는 코드·기존 규칙·마감을 같이 봤다.**
값을 정하는 일에서 근거의 **개수**가 아니라 **종류**가 갈랐다.

### 안 한 것

- **`UpGoldGain.asset` 삭제** — 배열에서만 빠지면 화면에 안 뜬다.
  `RunCommand` 에서 `DeleteAsset` 은 금지돼 있기도 하다
- **`PickupRadius` 가 구슬에 못 닿는 것** — 천장이 7 인데 `magnetActivationRange` 가 8 이다.
  건드리면 자석 픽업 가치가 같이 움직이므로 `TUNING.md` §I-3 에 짝으로 둔 그대로 남긴다.
  `C29` 가 **값을 안 고치고 설명 문구만 정직하게 바꾼 것**이 옳은 처리였다

---

## 2-59. ✅ 직업 3종 배선 판정 — **이미 돌아 있던 것을 기준으로 다시 쟀다** (D32, 2026-09-03 55차)

**한 줄:** 요청-22 판정 **7/7 PASS**. `C6` 6단계가 닫혔다.

> 전문 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-22 처리 결과 · [`DESIGN_CLASSES.md`](DESIGN_CLASSES.md) §7-B

### 원인 / 배경 — 요청이 열린 채 남아 있었다

`C28` 이 그림 6장 통과를 확인하고 `Classes.csv` 3줄 + `SceneWiring.csv` 1줄을 쓴 뒤
**Import 1회**를 요청했다(요청-22). 그런데 그 Import 는 **`D30`·`D31` 작업 중에 이미 돌아 있었다** —
같은 메뉴를 여러 번 실행했기 때문이다. **다만 CONTENT 의 판정 기준으로 확인한 적이 없었다.**

오늘 다시 Import 를 돌렸더니 **`git status` 가 깨끗했다.** 바뀔 게 없었다는 뜻이고,
동시에 **임포터가 멱등하다**는 증거이기도 하다.

### 검증 로그 — 판정 7/7 PASS

| # | 판정 | 결과 |
|---|---|---|
| ① | `Classes.csv` 실패 줄 0 · `SceneWiring` 12/12 | ✅ `Classes : 10` |
| ② | 새 애셋 3개 | ✅ |
| ③ | `m_Script` 가 `0` 이 아닐 것 (`I-19`) | ✅ 셋 다 `74ae27f0…` = `CharacterClassData.cs` |
| ④ | 🔴 `Portrait`·`BodySprite`·`WalkSheet` 셋 다 채워짐 | ✅ **3종 모두** · `WalkFrames` 16장 |
| ⑤ | 선택 화면 6개 · 승급 4종 제외 | ✅ 전부 `Tier=1` · 캡처 확인 |
| ⑥ | 시작 무기가 실제로 나온다 | ✅ 아래 |
| ⑦ | 콘솔 에러·경고 | ✅ 0 |

```
Demolitionist | Bomb         → AoeWeapon(Weapon_Aoe)          | HP 110 이속 3.5 공속 1.15 치명 0.05 방어  1
Assassin      | Shuriken     → ProjectileWeapon(Weapon_Sword) | HP  70 이속 4.3 공속 0.75 치명 0.20 방어 -1
Summoner      | SummonDragon → SummonWeapon(Weapon_SummonDragon)| HP  90 이속 3.8 공속 1.00 치명 0.05 방어  0
```

**스탯이 `DESIGN_CLASSES.md` §7-B 확정본과 한 자리도 안 틀린다.**

### 🔴 ④ 는 값이 아니라 **YAML 을 직접 읽어** 판정했다

`BalanceImporter.cs:848·861` 의 `LoadRef`·`LoadSpriteSheet` 는 경로를 못 찾아도
**로그를 안 남기고 기본값으로 넘어간다.** ①(Import 로그 깨끗)이 통과해도 ④가 빌 수 있고,
그러면 새 직업이 **기사 모습으로 나온다**(`I-57`). 그래서 애셋 YAML 에서 `{fileID: 0}` 여부로 봤다.
CONTENT 가 *"CSV 를 그림보다 먼저 쓰지 않겠다"* 고 버틴 이유가 정확히 이 자리다.

### 🟡 내가 한 번 잘못 봤다 — 무기 누적

한 플레이 세션에서 `StartRun()` 을 세 번 연달아 불러 3종을 한꺼번에 보려 했더니
무기가 **1 → 2 → 3개로 쌓였다.** 버그로 의심했는데 아니었다 —
런이 끝나는 모든 경로(`RunEndUI` 의 Retry·Main Menu)가 `GameManager.ReloadScene` 으로
**씬을 다시 로드**하므로 실제 게임에서 `StartRun` 은 **씬 로드당 한 번**뿐이다.
깨끗한 단일 런으로 다시 재니 무기가 **정확히 1개**였다. **버그로 등재하지 않았다.**

> ℹ️ 곁가지 둘 — **프리팹 이름이 헷갈린다**(`Weapon_Sword` 가 Shuriken 용, Sword 는 `Weapon_Melee`).
> 배선은 맞고 이름만 옛것이 남았다. 그리고 카드의 `Attack Speed` 부호 반전은
> **의도된 것**이다(`ClassSelectUI.cs:180` 주석 — 쿨다운 배율이라 음수가 빠름).

### 안 한 것

- **요청-22 §3 두 건** — `Gun`·`Toxin`·`SummonOctopus` 에 시작 직업이 없는 것(알고 남긴 구멍)과
  어쎄신이 `minAttackSpeed` 하한을 물리는 것. 둘 다 CONTENT 가 *"알아만 둘 것"* 으로 넘겼다

---

## 2-58. ✅ 보스 패턴 — **큰 잡몹에서 벗어났다** (D31, 2026-09-03 54차)

**한 줄:** 페이즈·예고 광역기·소환·HP 바. **보스전이 보스전처럼 보인다.**

> 전문 [`Parallel/DONE/D31.md`](Parallel/DONE/D31.md) · 근거 [`ROADMAP.md`](ROADMAP.md) §2-5 · 값 요청 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-23

### 원인 / 배경

`Boss1` = `Ogre` + `BossHpMult 7` 이 전부였다. AI 가 `Chaser` 라 **잡몹과 같은 코드로 걸어왔고**,
HP 1540 을 깎는 동안 화면에 아무 표시도 없었다. 보스전인 걸 알 수 있는 건 BGM 뿐이었다.

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Enemy/BossPatternData.cs` | 신규 SO (**단독 파일** — I-19) |
| `Assets/Scripts/Enemy/BossBrain.cs` | 신규 — 페이즈·기술·소환 |
| `Assets/Scripts/Enemy/BossSlam.cs` | 신규 — 예고 원 → 폭발 (**플레이어**를 친다) |
| `Assets/Scripts/UI/BossHealthBarUI.cs` | 신규 — 상단 HP 바 |
| `Assets/Scripts/Enemy/EnemyBase.cs` | 읽기 전용 접근자 5개 + `SetSpeedMultiplier` |
| `Assets/Scripts/Enemy/EnemyData.cs` | `BossPattern` 참조 |
| `Assets/Scripts/Wave/WaveManager.cs` | `AttachBossBrain` · `SpawnMinion` · `OnBossSpawned` · `bossSlamPrefab` |
| `Assets/Editor/BalanceImporter.cs` | `ImportBosses` + Export |
| `Assets/Game/Balance/Bosses.csv` | 신규 (**값은 임시**) |
| `Assets/Prefabs/Fx_BossSlam.prefab` | 신규 (`Proj_ToxinField` 파생) |

### 🔴 설계에서 지킨 것 둘

**① 보스 로직을 `EnemyBase.FixedUpdate` 에 안 넣었다.** 거기는 적 800마리가 매 물리 프레임
도는 자리다(`PERF.md`). `if (IsBoss)` 하나가 **보스 없는 웨이브에서도 800번** 돈다.
보스는 한 판에 한 마리뿐이니 그 인스턴스에만 붙는 컴포넌트가 맞다.

**② `EnemyBase` 는 읽기 전용으로만 열었다.** `HpNow`·`HpMax`·`HpFraction`·`Dead`·`DisplayName`.
HP 를 **쓰는** 통로는 안 만들었다 — 밖에서 쓰면 방어 계산과 사망 처리를 건너뛴다.
속도 배수는 슬로우와 **따로** 곱한다(성격이 다르다 — 교훈 181). 🔴 풀 재사용 대비로
`Initialize` 에서 `1f` 로 되돌린다. 안 지우면 **이전 보스의 배수가 잡몹에 남는다.**

### 값은 새 CSV 로 — `Enemies.csv` 는 안 건드렸다

그 표는 이미 33열이고 보스 패턴이 붙는 적은 **6종 중 하나뿐**이라 열을 늘리면 대부분이 빈칸이 된다.
`Bosses.csv` 의 `EnemyId` 가 `EnemyData.BossPattern` 을 되꽂는다.
⚠️ 그래서 Export 에 이 값이 안 나온다 — 복원은 `Bosses.csv` 가 한다.

임포터가 **값으로 만들 수 있는 실수 둘**을 잡는다: `SlamWindup <= 0`(피할 수 없는 공격이 된다) ·
`PhaseThresholds` 가 내림차순이 아님(`PhaseOf` 가 엉뚱한 답을 한다).

### 🔴 검증 중에 내 오독을 잡았고, 그게 실제 버그였다

⑤(소환)를 볼 때 **잡몹이 4→15로 늘어서** 소환이 도는 줄 알았다. **틀렸다** —
그건 웨이브가 스스로 소환한 `Zombie`·`Demon` 이었다. 내가 심어 둔 경고가 잡아냈다:

```
[WaveManager] 보스 소환 대상 'Goblin' 가 이번 웨이브 소환 목록에 없다
```

내가 쓴 `ResolveSummon` 이 **이번 웨이브 소환 목록에서만** 대상을 찾게 했는데
`Boss1` 에는 `Goblin` 이 없어 **소환이 통째로 죽어 있었다.** 그 제약에는 근거가 없었다 —
`ObjectPool.Get` 은 큐가 없으면 만든다(`ObjectPool.cs:38`). ⇒ `BossPatternData.SummonEnemy`
참조를 두고 **임포터가 꽂게** 고쳤다.

🔑 **재검증은 오독할 수 없게 다시 설계했다** — "잡몹 수"가 아니라 **"Goblin 수"** 를 센다.
`Boss1` 에는 Goblin 이 없으니 **0 이 아니면 보스가 부른 것뿐이다.** 결과 **0 → 4마리**.

### 검증 로그 — 판정 8/8 PASS

```
① Ogre HP 1540/1540 · BossBrain=True · 페이즈 1/3
② HP 바: 'OGRE' · '★★' · slider 1.00
③ HP 0.67 → 페이즈 2/3 · '★' · fill 주황
④ HP 0.37 → 페이즈 3/3 · 'FINAL' · fill 노랑
⑤ Goblin 0 → 4마리 (2페이즈 SummonCount 와 일치) · 그 외 6마리
⑥ Fx_BossSlam scale 6.40 (= 반경 3.2 × 2)
⑦-① 발밑: HP 130 → 112 (20 − 방어 2)   ⑦-② 6유닛 밖: 112 그대로
⑦-③ 예고 중 8유닛 밖으로: 112 그대로 · 남은 BossSlam 0개
⑧ 처치: HP 바 꺼짐 · 살아 있는 보스 0
```

**⑦ 이 핵심이다.** 예고 없이 터지면 패턴이 아니라 그냥 체력 깎기다.
맞는 경우와 피하는 경우를 **둘 다** 재서, 판정이 "예고를 본 시점"이 아니라
**"터지는 순간의 위치"** 로 이뤄진다는 걸 확인했다. 콘솔 에러·경고 **0**.

### 검증 방법으로 두 번 틀렸다

- **페이즈가 안 바뀐다고 봤다** — 한 `RunCommand` 안에서 피해를 세 번 넣었는데
  판정이 `Update` 에 있어 **프레임이 안 넘어갔다.** ⇒ `Update` 기반 상태는 프레임을 넘겨야 본다
- **`GrantInvincibility(0f)` 로 무적이 안 풀렸다** — 구현이 `Mathf.Max` 라 **줄일 수가 없다**
  (`PlayerStats.cs:274`). 900초를 걸어 둔 탓에 슬램 피해가 0 으로 나왔다.
  ⇒ **검증용으로 건 상태가 검증 대상을 가릴 수 있다**

### 안 한 것

- **수치 확정** — 근거가 하나뿐이다(웨이브 240초·HP 1540) → 요청-23
- **예고 원 전용 스프라이트** — 지금은 `ToxinField` 를 붉게 틴트해 쓴다. 모양이 블롭이다
- **보스 전용 그림** · **컷인·줌 등장 연출** — 새 애셋/새 UI 라 범위 밖

---

## 2-57. ✅ 메타 강화 화면 — **재화 고리를 닫았다** (D30, 2026-09-03 53차)

**한 줄:** 메타 골드를 **쓸 데가 생겼다.** 죽어도 뭔가 남는다.

> 전문 [`Parallel/DONE/D30.md`](Parallel/DONE/D30.md) · 근거 [`ROADMAP.md`](ROADMAP.md) §8 9번 · 값 요청 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-22

### 원인 / 배경 — 없던 것은 백엔드가 아니라 화면이었다

`ROADMAP.md` §5 가 *"자료구조만 있고 게임에 안 붙어 있다"* 로 남겨 둔 자리다.
읽어 보니 `MetaProgressionManager` 는 **이미 완성돼 있었다** — `PurchaseUpgrade` ·
`GetStatBonus` · JSON 저장/로드까지. `PlayerStats.cs:228` 도 `GetStatBonus()` 를 **이미 부르고 있었다.**

없던 건 셋이다: **애셋 0개**(`I-19` 함정에 막혀 있었다) · **화면**(`GameState.MetaScreen` 은
enum 에만 있고 전환 코드 0개) · **진입점**(메인 메뉴에 버튼 없음).
⇒ 메타 골드는 **벌기만 하고 쓸 데가 없었다.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Meta/UpgradeDefinition.cs` | 신규 — `MetaProgressionManager.cs:51` 에서 **분리**(`I-19`) |
| `Assets/Scripts/UI/MetaScreenUI.cs` | 신규 — `GameStatePanel` 파생 |
| `Assets/Scripts/UI/UpgradeCardUI.cs` | 신규 |
| `Assets/Scripts/Meta/MetaProgressionManager.cs` | `Upgrades` 프로퍼티 + `GetStatBonus` null 안전 |
| `Assets/Scripts/UI/MainMenuUI.cs` | `metaButton` + `OnMetaClicked` |
| `Assets/Editor/BalanceImporter.cs` | `ImportUpgrades` · Export · 🔴 **`EnsureFolder` 버그 수정** |
| `Assets/Game/Balance/Upgrades.csv` | 신규 · 7항목 (**값은 임시**) |
| `Assets/Game/UpgradeData/*.asset` | 신규 7개 |
| `Assets/Prefabs/Prefab_UpgradeCard.prefab` | 신규 |
| `Assets/Scenes/SampleScene.unity` | `MetaScreenPanel` · `MetaButton` · `MetaScreenUI` 배선 |

### 🔴 순서가 전부였다 — `I-19` 를 먼저 풀지 않으면 되돌릴 수 없다

`UpgradeDefinition` 은 `MetaProgressionManager.cs` 안에 얹혀 있었다.
그 상태로 `.asset` 을 만들면 `m_Script` 가 **`0` 으로 기록되고 재임포트로도 복구되지 않는다.**
애셋이 0개였던 덕에 아직 아무도 안 다쳤을 뿐, **만드는 순간 터질 자리**였다.

그래서 1단계가 파일 분리였다. 결과 — 7개 전부:

```
m_Script: {fileID: 11500000, guid: 03c86aaa1fa60f94f8d23dbdd777603d, type: 3}
                                    ↑ UpgradeDefinition.cs 의 guid
```

**`0` 이 하나도 없다. 먼저 했기 때문이다.**

### 🔴 새 폴더를 만드는 임포터에서만 터지는 버그를 밟았다

첫 Import 뒤 `Assets/Game/` 에 **`UpgradeData 1` ~ `UpgradeData 6`** 이 생겼다.
`EnsureFolder` 가 `AssetDatabase.IsValidFolder` 만 보는데,
**`StartAssetEditing()` 구간에서는 AssetDatabase 가 갱신되지 않아 방금 만든 폴더를
"없다"고 답한다.** 그래서 행마다 `CreateFolder` 가 다시 돌고 Unity 가 이름을 비켜 준다.

🔑 **기존 폴더들은 전부 이미 디스크에 있어서 여태 안 드러났다** — 새 폴더를 처음 만드는
임포터가 `Upgrades` 가 처음이었다. 디스크를 같이 보는 것으로 막았다:

```csharp
if (AssetDatabase.IsValidFolder(folder))  return;
if (System.IO.Directory.Exists(folder))   return;   // 추가 (CreateFolder 는 즉시 반영한다)
```

### 값은 CSV 로 갔다 — 그리고 임포터가 오타를 잡는다

`Costs`·`Bonus` 가 배열이라 인스펙터 예외를 둘까 했지만 `CsvTable.ArraySeparator`(`|`)와
`CsvRow.Ints`/`Floats` 가 이미 있어 예외가 필요 없었다(사용자 판단).
`MetaProgressionManager.ApplyStatKey` 는 모르는 `StatKey` 를 **조용히 무시**하므로
(사도 효과가 없고 예외도 경고도 없다) 임포터에 `IsKnownStatKey` 검사를 넣어 `!` 를 찍게 했다.

### 검증 로그 — 판정 6/6 PASS

```
[D30-RT] ① 화면: state=MetaScreen · panel.active=True · 카드 수=7 · timeScale=1
[D30-RT] ② 구매: 성공=True · Lv 0→1 · Gold 3231→3111 (비용 120) · GetStatBonus().MaxHp 0→10
[D30-RT] ③ 잔액 0 에서 구매 시도 = False (정상)
[D30-RT] ④ StartRun 후 PlayerStats.Final.MaxHp = 140  (meta +10 포함)
[D30-RT] ④ Lv2 구매 후 MaxHp 140 → 150
[D30-RT] ⑤ MetaScreen → MainMenu 왕복 후 state=MainMenu · timeScale=1
```

**④ 가 이 작업의 전부다.** 강화를 사자 다음 런의 스탯이 실제로 올랐다 —
클리어 보상 → `SettleRun` → `Currency` → **이 화면** → `GetStatBonus()` → `PlayerStats`.
콘솔 에러·경고 **0**. 화면은 스크린샷으로 눈으로도 확인했다.

> 🔴 **검증이 실제 세이브 파일을 쓴다.** `PurchaseUpgrade` 가 `Save()` 를 부르기 때문이다.
> 시작 전에 `save.json` 을 백업하고 끝나고 되돌렸다(`Currency 1231` · 강화 레벨 0 복구 확인).
> **검증이 사용자 데이터를 바꾸면 그것도 정리 대상이다** — 임시 스크립트와 같다.

### 화면은 복제로 만들었다 (그리고 한 번 틀렸다)

`Prefab_ShopCard` → `Prefab_UpgradeCard`, `MainMenuPanel` → `MetaScreenPanel`,
`StartButton` → `MetaButton`. 폰트·색·앵커·`CanvasScaler` 를 그대로 물려받는 게 안전하다.

🔴 **복제 버튼의 `onClick` 을 반드시 비운다** — `StartButton` 복제본은 `OnStartClicked` 를
그대로 갖고 있어서, 안 지우면 **Upgrades 를 눌렀는데 런이 시작된다.**

첫 캡처에서 카드 내용이 셀 밖으로 흘렀다. 셀을 250×220 으로 잡았는데 카드 원본이
250×**440** 이었다 — `GridLayoutGroup` 은 카드 `RectTransform` 만 줄이고 자식은 위쪽 기준
고정 오프셋이라 아래가 잘린다. 아이콘이 없어 위 공간이 비므로 **카드를 250×320 으로 압축**했다.

> ⚠️ **UI 는 카메라 캡처로 못 본다.** Canvas 가 `ScreenSpaceOverlay` 라
> `Unity_Camera_Capture` 는 씬 뷰만 찍는다. `ScreenCapture.CaptureScreenshot` 을 쓸 것.

### 곁다리 — 직업 3종이 게임에 들어갔다 (요청-19 닫힘)

Import 로그에 `Classes : 10` 이 나왔다. CONTENT 가 `8ab5551`(C28)로 `Classes.csv` 를
이미 써 뒀고 **내 Import 가 그걸 애셋으로 만든 것**이다.
`Demolitionist`·`Assassin`·`Summoner` 셋 다 `WalkFrames` 17줄(=16프레임+헤더) ·
`Portrait` guid 각각 다름 — **`Warrior` 기준선과 동일**.
`GameManager.classes` 도 **6개**가 됐다. **`C6` 6단계가 닫혔다.**

### 안 한 것

- **강화 수치 확정** — `Upgrades.csv` 값은 **임시**다(메타 수입 132G 만 근거). → 요청-22
- **강화 아이콘 7장** — `Icon` 열이 비어 있다. `UpgradeCardUI` 가 null 이면 `Image` 를 꺼서 안 깨진다
- **직업 해금 흐름** — `ClassSelectUI.IsUnlocked()` 가 아직 `UnlockedByDefault` 만 본다. 별건

---

## 2-56. ✅ 구슬 원거리 회수 — 요청이 **예고된 자리에서 막혔고, 그 자리가 버그였다** (D29, 2026-09-03 52차)

**한 줄:** 38유닛 밖 구슬을 그 자리에서 거둔다. 그리고 그걸 넣기 전에 **`B10` 을 먼저 닫아야 했다.**

> 전문 [`Parallel/DONE/D29.md`](Parallel/DONE/D29.md) · 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-21 · [`Parallel/BUGS.md`](Parallel/BUGS.md) `B10`

### 원인 / 배경 — 적에게는 있는 규칙이 구슬에는 없었다

`D27` 이 *"`ExpDrop` 은 수명이 없어 주울 때까지 영원히 남는다. 수명 상한을 줄지는 설계 결정"* 을 남겼다.
`C28` 의 답은 **"수명은 두지 않는다"** 였다 — 얻는 게 0.5 ms(60fps 예산의 3 %)인데
잃는 게 **자석 픽업의 존재 이유**라 거래가 성립을 안 한다.

대신 이미 있는 원칙을 옮기자고 했다. `WaveManager.RecycleFarEnemies` 의 주석이 답을 적어 놨다 —
*"너무 멀어진 적을 **죽이지 않고** 옮긴다. **지우면 경험치·골드가 증발하니** 옮겨서 다시 쓴다."*
**적에게는 이 규칙이 있고 구슬에는 없었다.** 그게 전부다.

### 🔴 그런데 요청을 받자마자 막혔다 — CONTENT 가 예고한 바로 그 자리에서

요청-21 §3 에 *"레벨업이 한 번에 여러 번 오를 수 있다. `LevelUpManager` 가 큐를 처리하는지
확인해 줘 — 못 하면 이 요청은 거기서 막힌다"* 고 적혀 있었다. **확인했더니 큐가 없었다.**

`ExperienceManager.CollectXp:145` 의 `while` 이 레벨마다 `TriggerLevelUp()` 을 부르는데,
`LevelUpManager.ShowLevelUpPanel` 은 `_currentChoices` 를 **그냥 덮어쓴다.**
3레벨이 오르면 패널이 3번 다시 만들어지고 **마지막 것만 남는다** — 카드 2장이 조용히 사라진다.
얼어붙지는 않는다(`ChangeState:117` 의 가드). **정지가 아니라 조용한 손실**이라 더 안 보였다.

지금은 구슬이 **개체마다 따로** `CollectXp` 를 불러 잘 안 드러난다.
🔴 **그런데 요청-21 은 수백 개를 합산해 한 번만 부르라고 한다** — 비용상 옳은 요구지만,
그러면 다중 레벨업이 **기본 동작**이 된다. ⇒ `B10` 등재 후 **사용자 판단으로 먼저 고쳤다.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | **`B10`** — `_pendingLevelUps` 대기열 + `_panelIsForced` 구분 |
| `Assets/Scripts/Experience/ExpDrop.cs` | `Active` 정적 목록(`OnEnable`/`OnDisable`) · `Harvest()` · `PullAllToPlayer` 를 목록 기반으로 |
| `Assets/Scripts/Wave/WaveManager.cs` | `RecycleFarExpDrops()` + `MaintainRoutine` 에서 호출 |

🔴 **씬·프리팹·CSV·SO 변경 0.** 검증도 전부 `RunCommand` 로 했고 임시 오브젝트를 안 만들었다.

### 🔑 설계 도중에 상호작용 하나를 더 잡았다

처음 안은 `HidePanel` 에서 **무조건 하나 깎는** 것이었다. 그런데 진화 제안 패널
(`ShowForcedChoices`)이 떠 있는 동안 레벨업이 들어오면, 진화 카드를 고른 것이
**레벨업 빚을 갚은 것으로 세어져 대기 중인 레벨업이 그대로 사라진다.**
`_panelIsForced` 로 패널 종류를 구분해 **진화는 빚을 만들지도 갚지도 않게** 했다.

| 경우 | 결과 |
|---|---|
| 3레벨 동시 | 카드 3장 |
| 진화 제안만 | 카드 1장, 그대로 닫힘 |
| 진화 중 레벨업 1회 | 진화 카드 → **이어서 레벨업 카드** |

### 검증 로그 — 판정 5/5 PASS

```
[D29-DROP] 무적 600s · 구슬 8개 배치 (5·15·25·35·37·39·45·80) · Active=8 · waveActive=True
[WaveManager] 원거리 구슬 회수 3개 · XP +3
[D29-DROP] 남은 구슬 4개 — 거리: 15.0  25.0  35.0  37.0
[D29-DROP] Lv=1 Xp=4 state=Wave waveActive=True
```

| # | 판정 기준 (요청-21) | 결과 |
|---|---|---|
| ① | 웨이브를 넘어 단조 증가하지 않을 것 | ✅ 조작 없이 **8 → 4개** |
| ② | 회수된 경험치가 버려지지 않을 것 | ✅ `CurrentXp 4` = 회수 3 + 자동흡수 1 |
| ③ | 회수량 로그 | ✅ `원거리 구슬 회수 3개 · XP +3` |
| ④ | 자석이 죽지 않을 것 | ✅ 38 **안**은 그대로 |
| ⑤ | 콘솔 에러·경고 | ✅ **0** |

🔑 **거리 사다리를 놓아 임계값을 경계 양쪽으로 증명했다** — 걷힌 것 `39·45·80`,
남은 것 `15·25·35·37`. 경계가 37 과 39 사이 = **정확히 38**(`SpawnRadius 20 × 1.9`).
`D25` 의 교훈 180(*경계가 0 인 판정은 아무것도 증명하지 못한다*)을 이번엔 **설계에 먼저** 넣었다.

`B10` 검증(별도 시행):

```
[B10] CollectXp(40) 후  Lv=1 → 4  state=LevelUp timeScale=0
[B10] HidePanel 1회  state=LevelUp
[B10] HidePanel 2회  state=LevelUp
[B10] HidePanel 3회  state=Wave  timeScale=1
```

**`HidePanel` 을 세 번 다 써야** `Wave` 로 돌아온다. 수정 전이라면 **1회에 끝났다.**

### 씬 전체 순회를 새로 만들지 않았다

`RecycleFarEnemies` 는 `_alive` 목록을 쓰는데 구슬에는 목록이 없었다.
0.25초마다 `FindObjectsByType<ExpDrop>` 을 도는 건 **`D27` 이 방금 걷어낸 그 패턴**이라
`ExpDrop.Active`(`OnEnable`/`OnDisable` 로 유지되는 정적 목록)를 뒀다.
풀 반환도 `SetActive(false)` 라 같은 경로를 탄다(`ObjectPool.Return:61` 확인).
➕ `PullAllToPlayer()` 의 `FindObjectsByType` 도 같은 목록으로 바꿨다.

### ⚠️ 밸런스가 바뀌었다 — 숫자는 안 정했다

지금까지 뒤에 흘린 구슬은 **조용히 소실**되고 있었다. 이제 회수되므로 **경험치 수입이 는다.**
얼마나인지는 **추측하지 않는다** — ③ 로그의 `XP +M` 을 실플레이에서 보고 정한다.
`xpThresholds` 는 자리표시 값이라 지금 미리 손대지 않았다 → `TUNING.md` §I-3(CONTENT).

---

## 2-55. ✅ 가격 재산정 Import — **"안 바뀌는 게 정상"인 판정이 배선을 증명했다** (D28, 2026-09-03 51차)

**한 줄:** `C27` 이 저장해 둔 CSV 를 한 번 밀어 넣었다. **판정 5/5 PASS.**

> 전문 [`Parallel/DONE/D28.md`](Parallel/DONE/D28.md) · 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-20 · 답신 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-20

### 원인 / 배경

`D25` 가 지갑을 둘로 가른 뒤 `C27` 이 상점·해금 가격을 두 예산에 맞춰 다시 잡아 CSV 에 저장했다.
CSV 는 원본일 뿐이고 **Import 를 돌려야 게임이 본다** — 그리고 Import 는 에디터 조작이라 DEV 몫이다.
같이, `D27` 이 새로 확인한 **Unity MCP 함정 둘이 `CLAUDE.md` 에 안 들어가 있었다.**
`PERF.md` 에는 있지만 매 세션이 읽는 건 `CLAUDE.md` 다.

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Game/ItemData/*.asset` **25개** | `ShopPrice` `5~12` → `40~110` |
| `Assets/Scenes/SampleScene.unity` | 프리팹 오버라이드 5개 — `baseRerollCost 25` · `rerollCostIncrease 15` · `rerollCost 10` · `characterUnlockCost 150` · `skinUnlockCost 75` |
| `ProjectSettings/QualitySettings.asset` | `D27` 이 올린 품질 `0→5` **유지**(사용자 판단) + Unity 6.3 포맷 업그레이드 |
| `CLAUDE.md` §4 | 함정 **2행 신설** + 기존 `C# 수정 반영` 행 정정 |

🔴 **C# 변경 0 · CSV 변경 0 · 그림 변경 0.** 요청-20 §3 의 코드 2건은 *"알아만 둘 것"* 이라 안 건드렸다.

### 검증 로그

```
[BalanceImporter] Import 완료
  Weapons: 12 · Buildings: 5 · Passives: 11 · Enemies: 6 · Items: 28
  Waves: 6 · Classes: 7 · Evolutions: 3 · ClassEvos: 4 · Events: 5
  Economy.csv     : 49/49 적용
  SceneWiring.csv : 12/12 적용
```

| # | 판정 기준 (요청-20) | 결과 |
|---|---|---|
| ① | `Items.csv` 실패 줄(`!`) 없을 것 | ✅ 0건 |
| ② | `! 씬에 StageMapManager 없음` 이 뜨나 | ✅ 안 떴다 · `49/49` |
| ③ | `ItemData/Sword.asset` 가격 | ✅ `ShopPrice: 8 → 70` |
| ④ | 씬 `ShopManager` | ✅ `3→25` · `1→15` |
| ⑤ | 씬 `StageMapManager` 가 **하나도 안 바뀔 것** | ✅ 오버라이드 **0개** |

### 🔑 ⑤ 는 낭비가 아니라 **판정 하나를 공짜로 더 만든 설계였다**

`C27` 이 ⑤ 를 일부러 no-op 으로 넣었다 — 값은 코드 기본값 그대로다.
그런데 씬 diff 에 오버라이드가 0개 생긴 것은 두 가지를 **동시에** 뜻한다.

1. 값이 같다 (의도한 no-op)
2. **임포터가 그 컴포넌트를 실제로 찾았다** — 못 찾았으면 `49/49` 가 아니라 `43/49` + `!` 였다

⇒ 이제 `weightShop`(상점 노드 빈도)을 CSV 에서 튜닝할 수 있다는 것이 **부작용 없이** 증명됐다.

### 알게 된 것 둘

- **필드 이름이 `shopPrice` 가 아니라 `ShopPrice` 다** (대문자 S).
  `FindProperty("shopPrice")` 는 `null` 을 돌려주고 `.intValue` 에서 **NRE 가 난다.** 실제로 밟았다
- 🔴 **Import 값은 프리팹이 아니라 씬 인스턴스에 오버라이드로 들어간다.**
  `ShopManager.prefab` 은 Import 뒤에도 `3/1` 이다 — 인스펙터 확인은 **씬 오브젝트**로 해야 한다

### 🔴 `D27` 인계 반영 — `CLAUDE.md` §4 에 함정 2행

| 새 행 | 내용 |
|---|---|
| **에러 0 인데 새 필드가 없다** | `Assets/Refresh` 가 컴파일을 건너뛴 것. `CompilationPipeline.RequestScriptCompilation()` 을 직접 부르고 **로드된 어셈블리에서 새 심볼을 조회해** 확인한다 |
| **컴파일 에러가 `Error` 로 안 잡힌다** | Unity 컴파일 에러는 `Log` 타입으로 온다. `Types:["Error"]` **0건** ↔ `["All"]`+`FilterText:"error CS"` **11건** |

➕ 이번에 하나 더 밟았다 — **`Unity_RunCommand` 는 톱레벨 문을 안 받는다**
(`CS8805`). `internal class CommandScript : IRunCommand` + `Execute(ExecutionResult result)` 형식이 강제다.
**툴 설명에 이미 적혀 있던 것**이라 함정이 아니라 안 읽은 것이라, `CLAUDE.md` 에는 안 넣었다.

### 🔴 품질 레벨 `5`(Ultra) 유지 — 대가를 같이 적어 둔다

`D27` 이 `ProjectSettings/QualitySettings.asset` 의 품질을 `0`(Very Low) → **`5`(Ultra)** 로 올려 뒀다.
근거는 `PERF.md` §7 에 있다 — *"`Standalone` 기본값이 5 다. Very Low 로 재면 렌더 비용이 실제보다 낮게 나온다."*
**사용자 판단으로 유지**했다 (2026-09-03).

🔴 **Ultra 는 `vSyncCount: 1` 이고, `vSyncCount` 는 품질 레벨마다 따로 저장된다.**
강제로 꺼 주던 `PerfHarness` 는 `D27` 이 지웠다. ⇒ **이제 에디터 플레이가 144 Hz 에 고정된다.**
프레임 타임이 정확히 6.94 ms 에 붙어 있으면 **성능이 좋아진 게 아니라 vSync 를 보고 있는 것**이다.
`TODO.md` §1-B 에 등재했고, `TUNING.md` §G 는 CONTENT 소유라 요청-20 으로 넘겼다.

---

## 2-54. ✅ 성능을 처음으로 **쟀다** — 가설 11개를 죽이고 버그 2개를 찾았다 (D27, 2026-09-02 50차)

> 전체 측정 기록은 [`PERF.md`](PERF.md)(신설). 그림은 [`Figures/`](Figures), 촬영 계획은 [`SHOTLIST.md`](SHOTLIST.md).

### 무엇이 문제였나

이 프로젝트는 **성능을 한 번도 재 본 적이 없었다.** [`ROADMAP.md`](ROADMAP.md) §6 에
*"`OverlapCircleAll` 이 할당한다"* 같은 **코드를 읽고 쓴 추측**만 있고 숫자가 없었다.

포트폴리오(넥토리얼, 마감 2026-09-07) 쪽에서도 같은 구멍이 있었다 — *"만들었다"* 는 사례는
많은데 **"재고 고치고 다시 쟀다"** 가 없었다.

### 🔑 이 작업의 성격 — **측정 전에 판정 기준을 못박았다**

[`PERF.md`](PERF.md) §1~§5 는 **첫 측정 전에** 썼다. 환경 고정값 · 표본 프로토콜 ·
지표 정의 · **"어떤 숫자가 나오면 어느 쪽으로 간다"** 까지.

> 재고 나서 기준을 정하면 유리한 숫자를 고르게 된다. 그건 측정이 아니라 홍보다.

실제로 그 기준 중 **2개가 실패로 나왔고, 낮추지 않고 실패로 남겼다**(§8-3).

### 죽인 가설 11개

| # | 가설 | 무엇이 죽였나 |
|---|---|---|
| 1 | 적↔적 트리거 충돌이 물리를 터뜨린다 | `m_LayerCollisionMatrix` 를 디코드하니 **layer 6 마스크가 `0x380`** — bit 6 이 없다. **이미 꺼져 있었다** |
| 2 | GC 정지가 스파이크를 만든다 | **`GC.Collect = 0.000`.** 프레임당 17~39 KB 를 할당하지만 정지를 안 만든다 |
| 3 | "적이 많으면 느리다" | 적만 **800마리에서 121 fps** |
| 4 | 풀이 비어 `Instantiate` 폭풍 | 느린 10프레임 / 나머지 배수가 **4회 중 1회만** 기준(5배) 초과 |
| 5 | 적 사망 처리 폭주 | 4회 중 1회만 |
| 6 | 데미지 팝업 생성 | 4회 중 1회만 |
| 7 | **`OverlapCircleAll` 할당 8곳** | 호출부 전부를 계측 → **프레임의 1.3 %** |
| 8 | 투사체 `Update()` | 인스턴스를 세어 보니 **1~3개**뿐 |
| 9 | `ExpDrop.Update` 가 비싸다 | 한 실행 안 A/B → **개당 0.9 µs**, 최적화 효과 −11 %(노이즈 안) |
| 10 | `DamagePopup`(TMP)이 비싸다 | **개당 1.6 µs · 프레임당 0.12~0.22 ms** |
| 11 | `Update()` 호출 고정 비용 | 빈 `Update()` 더미 0/500/1000/2000 → 기울기 **0.139 µs**. 예측(1~11 µs)과 **10배 이상** 차이 |

### 찾은 것 2개

**🔴 `B8` — 이웃 12칸 무언 절단** ([`Parallel/BUGS.md`](Parallel/BUGS.md))

`EnemyBase.GetSeparation()` 의 이웃 버퍼가 12칸 고정인데
`Physics2D.OverlapCircle(…, Collider2D[])` 는 넘치는 이웃을 **경고 없이 버린다.**
순서 보장도 없어 **`1/d²` 로 가장 세게 밀어내야 할 가까운 이웃이 빠질 수 있다.**

`n` 히스토그램이 분포가 아니라 **벽**을 보여 준다 (적 800):

```
n=  1    2    3    4    5    6    7    8    9   10   11      12
  831 1564 1577 1903 2124 2546 2609 2678 2467 2320 2229   26617   ← 벽
```

절단 비율은 적 **400에서 12~19 %**, **800에서 58~71 %** 다.
🔑 **성능 우회책 안에 정합성 상한이 숨어 있었다** — `12` 는 "이웃을 몇 마리까지 볼지"가 아니라
**"배열을 몇 칸으로 할지"** 를 정한 값이 우연히 판정 규칙이 된 것이다.

**🔴 적 겹침의 원인은 밀도다**

적 800에서 **85 %가 0.5유닛 안에 이웃을 둔다** (분리 반경은 0.85, 최소 거리 0.005~0.03).
버퍼를 64로 키워도 이 비율은 **84.6 % → 85.1 %** 로 그대로였다.
⇒ **절단은 증상이지 원인이 아니었다.**

### 고친 것

| 파일 | 변경 | 근거 |
|---|---|---|
| `Enemy/EnemyBase.cs` | `NeighborBufSize` **12 → 64** | 적 ≤400 에서 절단 **0 %**, 비용 차이 없음 |
| `Experience/ExpDrop.cs` | 스탯 캐시 · `sqrMagnitude` · `SharedPool` | 씬 전체 순회와 매 프레임 `GetComponent` 제거 |
| `Pickup/WorldPickup.cs` | 위와 같음 | 〃 |
| `Dev/DevPanel.cs` | **`Enemy Spawn` 절 신설** | 웨이브를 거치지 않고 적 N마리를 세우는 수단 |

🔴 **`ExpDrop`·`WorldPickup` 수정은 "성능 개선"이라고 주장하지 않는다.**
한 실행 안 A/B 로 잰 단가 개선이 **−11 %** 로 같은 경로 내 편차(44 %)보다 작았다.
**씬 전체 순회를 없앤 것은 그 자체로 옳으니 유지**할 뿐이다.

🔑 `SharedPool` 은 새 패턴을 만들지 않고 `EnemyBase.cs:598` 의 관용구를 그대로 옮겼다 —
**같은 실수를 이미 한 번 고치고 주석까지 남겨 놓고 나머지에 안 옮겼던 것**이다.

### 🔴 사전 등록 기준 대조 — 2개 실패

| # | 조건 (측정 전에 못박음) | 결과 |
|---|---|---|
| 1 | 적 800에서 포화 도수 **0** | 71 % → **1.8 %** ❌ |
| 2 | 적 간 평균 최근접 거리 **증가** | 0.3144 → **0.3400** ✅ |
| 3 | `p50`·`p99` **+5 % 이내** | 분리 비용 28 % → **41 %** ❌ |
| 4·5 | 적을 때 손해 없음 · 경고 0 | ✅ |

**그래도 64 를 유지한 것은 판단이지 기준 통과가 아니다** — 적 800 은 `Waves.csv` 어디에도
없는 조건이고, 실제로 나오는 구간(130·400)에서는 비용 차이가 측정 노이즈 안이다.
⚠️ **`MaxAlive` 를 400 이상으로 올리면 다시 판단해야 한다** (코드 주석에도 적었다).

### 스스로 무너뜨린 결론 3개

| 무엇 | 어떻게 |
|---|---|
| *"이 프로젝트에는 렉이 없다"* | 시나리오 A(적만)에서만 맞았다. 무기를 켜면 프레임이 **2.5배** |
| **`B9`** — "게임이 `timeScale=0` 으로 영구 정지하는 버그" | 🔴 **오진.** 사용자 지적으로 `StageClearUI` 가 Continue 를 기다리는 **정상 상태**임이 드러났다. `Show()` 가 `timeScale=0` 을 만들면서 `ChangeState` 를 안 해 state 가 `Wave` 로 남는다 |
| *"무기 렉의 범인은 사망 부산물"* | 근거였던 `BehaviourUpdate` 16 ms 가 **열화된 세션 한 번**의 값이었다(프레임 51~54 ms, 정상은 13 ms). 정상 세션에서는 0.56~1.46 ms |

🔑 **셋 다 같은 패턴이었다 — 그럴듯한 설명을 찾자마자 검증을 건너뛰었다.**

### 밟은 함정 11가지 (전부 실제로 겪은 것)

| # | 함정 | 어떻게 드러났나 |
|---|---|---|
| ① | **품질 레벨을 올리면 vSync 가 따라 켜진다** (레벨별 저장이다) | `p50 = 6.804 ms` = 정확히 144 Hz |
| ② | 편집 모드 `Screen` 은 **실제 렌더 타겟이 아니다** | 640×480 인 줄 알았는데 플레이 모드는 1920×1080 |
| ③ | 🔴 **`Assets/Refresh` 는 재컴파일을 보장하지 않는다** | 에러 0 인데 새 필드가 어셈블리에 없었다 |
| ④ | 🔴 **Unity 컴파일 에러가 `Error` 가 아니라 `Log` 타입으로 온다** | `Types:["Error"]` 는 0건, `["All"]` 로 보니 11건 |
| ⑤ | **레벨업이 게임을 멈춘 채 표본에 들어간다** | 적 200이 살아 있는데 분리 질의 **0회** |
| ⑥ | **웨이브 제한시간 만료** | 표본 전체가 걸러져 **0으로 가득 찬 행** |
| ⑦ | **`0.000 ms` 는 "비용 없음"이 아니다** | URP 는 `Camera.Render` 를 안 쓴다 — 측정을 안 한 것 |
| ⑧ | 🔴 **계측기를 바꿔 놓고 before/after 를 비교했다** | 렌더러를 껐는데 3 ms 느려졌다 |
| ⑨ | 🔴 **세션이 다르면 같은 조건이 2배 흔들린다** | 같은 조건이 13.03 ↔ 7.25 ms |
| ⑩ | **사람이 측정 중 Game 뷰를 클릭했다** | 콘솔 스택에 `EventSystem:Update` → `ItemCardUI` |
| ⑪ | **길게 돌리면 씬이 무거워진다** | 같은 세션 시행 1 = 47 fps → 시행 2 = **3 fps** (풀 5,814개) |

🔑 **①·②·⑧·⑨ 는 뿌리가 같다 — "재는 값을 재는 상태에서 읽지 않았다."**
그래서 하네스가 매 실행마다 환경값을 직접 읽어 로그로 남기고,
**`PerfHarness.Version` 상수로 "내가 방금 고친 코드가 실제로 로드됐는가"를 조회**하게 만들었다.
이 가드가 실제로 **v8 코드로 v9 결과를 적을 뻔한 것을 막았다.**

### 검증 규모

시나리오 A/B · 적 130/200/400/800 · **총 40회 이상의 시행**.
프레임 타임은 평균이 아니라 **`p50`/`p95`/`p99`/`max`** 로 기록했다 —
사람이 "렉"이라고 부르는 것은 평균이 아니라 **꼬리**이기 때문이다.

### 남긴 것 / 지운 것

- ✅ **계측 코드 전량 제거** — `PerfHarness`·`PerfCounters`·`PerfDummyUpdater` 삭제,
  게임 코드의 `🔬` 블록 전부 제거, `OverlapCircleAll` 래퍼 8곳 원복.
  로드된 어셈블리에서 심볼이 사라진 것까지 조회로 확인
- ✅ **`SampleScene.unity` 는 이번 작업에서 한 줄도 안 바뀌었다** — 하네스를 씬에 저장하지 않고
  메모리에만 두었기 때문이다
- 🟢 **남긴 것**: `DevPanel` 의 `Enemy Spawn`(앞으로도 쓸 도구) · `NeighborBufSize = 64` ·
  `ExpDrop`/`WorldPickup` 최적화

### 곁가지 — 닫힌 미검증 항목

**건물 앞 `E` 승급**이 촬영(S1) 과정에서 **처음으로 실제 조작 경로로 검증됐다.**
`E` 로 실제 승급되고 **겉모습도 바뀐다**(`walkFrames` 배선이 로그가 아니라 화면에서 확인).
반 년간 [`TODO.md`](TODO.md) §1 에 열려 있던 항목이다 — 이 게임의 **유일한 설계 차별점**이다.

---

## 2-53. ✅ 직업 3종 그림 6장 — 그리고 "실패했다"는 보고를 믿었다면 버릴 뻔했다 (D26, 2026-08-31 49차)

**한 줄:** Unity AI 로 폭발광·어쎄신·소환사 초상 3장 + 걷기 시트 3장을 뽑았다. **`C6` 6단계의 유일한 막힘이 풀렸다.**

> 전문 [`Parallel/DONE/D26.md`](Parallel/DONE/D26.md) · 요청 [`Parallel/REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-19 · 답신 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-19

### 원인 / 배경 — 값은 다 정해졌는데 그림이 없었다

CONTENT 가 `DESIGN_CLASSES.md` §7-B 에 직업 3종의 값을 **전부 확정**해 뒀다
(무기 · HP · 이동 · 공속 · 치명 · 방어 · 슬롯 9칸까지). 그런데 `Classes.csv` 를 쓸 수 없었다.
**Unity AI 생성은 에디터 조작이라 CONTENT 세션이 못 하기 때문**이다.

순서를 뒤집으면 안 되는 이유도 분명했다 — `BalanceImporter.cs:848`(`LoadRef`) 와
`:861`(`LoadSpriteSheet`) 은 그림을 못 찾으면 **로그 한 줄 없이 fallback** 한다.
CSV 를 먼저 쓰면 기본 그림이 조용히 박히고 아무도 모른다. **그림이 먼저다.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Game/Sprites/Classes/Demolitionist.png` (+`.meta`) | 신규 · 초상 · 알파 `0–255` · 투명 60.9 % |
| `Assets/Game/Sprites/Classes/Assassin.png` (+`.meta`) | 신규 · 초상 · 알파 `0–255` · 투명 79.9 % |
| `Assets/Game/Sprites/Classes/Summoner.png` (+`.meta`) | 신규 · 초상 · 알파 `0–255` · 투명 75.8 % |
| `Assets/Game/Sprites/Classes/Walk/Demolitionist_Walk.png` (+`.meta`) | 신규 · 16프레임 · 색 81,800 |
| `Assets/Game/Sprites/Classes/Walk/Assassin_Walk.png` (+`.meta`) | 신규 · 16프레임 · 색 11,265 |
| `Assets/Game/Sprites/Classes/Walk/Summoner_Walk.png` (+`.meta`) | 신규 · 16프레임 · 색 135,134 |
| `Docs/Parallel/BUGS.md` | `B7` 등재 (승급 4종 임포트 설정) |

🔴 **C# 변경 0 · 씬 변경 0 · 프리팹 변경 0 · CSV 변경 0.** 애셋 6장이 전부다.
`Warrior.png.meta` 도 `git status` 에 안 뜬다 — 기준선에 맞추는 명령을 같이 돌렸는데
**이미 그 값이었다**는 뜻이다(대조군이 제 역할을 했다).

### 검증 로그

초상 4종(`Warrior` 는 기준선):

```
Warrior / Demolitionist / Assassin / Summoner
  → ppu=1024 maxTS=512 filter=Point comp=Uncompressed mode=Single loaded=512x512
```

걷기 시트 4종:

```
[SLICE] Warrior_Walk         ppu=256 maxTS=1024 filter=Point comp=Uncompressed mode=Multiple sprites=16
[SLICE] Demolitionist_Walk   ppu=256 maxTS=1024 filter=Point comp=Uncompressed mode=Multiple sprites=16
[SLICE] Assassin_Walk        ppu=256 maxTS=1024 filter=Point comp=Uncompressed mode=Multiple sprites=16
[SLICE] Summoner_Walk        ppu=256 maxTS=1024 filter=Point comp=Uncompressed mode=Multiple sprites=16
```

| # | 판정 기준 (요청-19) | 결과 |
|---|---|---|
| ① | 6장이 정확한 경로·이름에 | ✅ |
| ② | 전부 1024×1024 | ✅ |
| ③ | `.meta` 가 `Warrior`/`Warrior_Walk` 와 같은가 | ✅ 필드 단위 대조 |
| ④ | 시트마다 16 슬라이스 | ✅ |
| ⑤ | 알파 최솟값 0 | ✅ |
| ⑥ | 칸마다 캐릭터 하나 · 여백 30px | 🟡 아래 |

**⑥ 은 기준선 자체가 못 지키는 기준이었다.** 48칸을 알파 `getbbox` 로 재 보니
`Warrior_Walk` 의 최소 여백이 **2px** 다. 새 3장은 14 / 25 / 31px 로 **전부 기준선보다 낫고**,
48칸 어디에도 칸 넘침이 없다. 중요한 건 30 이라는 숫자가 아니라 **0 이 아니라는 것**이다.

### 🔴 이번 작업의 진짜 소득 — 두 번의 "낡은 정보를 믿었다"

| | 무엇을 믿었나 | 결과 |
|---|---|---|
| **DEV(나)** | Unity AI 툴의 `success: false` 반환값 | **성공한 생성을 버릴 뻔했다** |
| **CONTENT** | 1~2분 전에 읽은 파일 상태 | **이미 고쳐진 것을 반려**했다 (`f09558e`) |

Kling 호출이 실패를 반환했는데 붙어 온 job GUID 가 **직전 seedance 실패 때와 똑같았다** —
세션이 **낡은 job 결과를 재생**한 것이다. 반환값만 봤으면 여기서 접었다.
파일을 열어 보니 1,166.6 KB · 색 102,531 개짜리 **진짜 16프레임 시트**였다.

CONTENT 쪽도 같은 종류다. 그쪽이 "빈 파일이니 재생성하라"고 지목한 `Summoner_Walk` 는
seedance 가 3번 실패하며 남긴 **자리표시 파일**이었고, 그 시점엔 이미 다시 뽑아 둔 뒤였다
(`mtime` 14:53:29 vs 반려 커밋 14:54:53).

> **믿을 수 있는 신호는 디스크의 파일뿐이다** — 크기 · 색 수 · 알파 extrema.
> 그리고 **애셋 판정에는 `mtime` 을 같이 적는다.** 답신에 양쪽 다 그렇게 하자고 제안했다.

### 🟡 함정 — 유효 `maxTextureSize` 는 `.meta` 최상단에 없다

`.meta` 최상단의 `maxTextureSize: 2048` 을 읽고 한 번 틀렸다. 적용되는 값은
`platformSettings[buildTarget: DefaultTexturePlatform]` 안에 있다(초상은 **512**).

```csharp
var ps = ti.GetDefaultPlatformTextureSettings();
ps.maxTextureSize     = 512;
ps.textureCompression = TextureImporterCompression.Uncompressed;
ti.SetPlatformTextureSettings(ps);
```

### 안 한 것

- **`Classes.csv` · `SceneWiring.csv`** — CONTENT 소유다. 답신을 보냈으니 그쪽이 쓴다
- **Assassin 시트 자주 악센트** — CONTENT 권고였고 필수 아님. 3번 실패 끝에 얻은 시트라
  재생성 위험이 크고, 어두운 타일 위 시인성은 **외곽선(`D15`)으로 푸는 게 싸다**
- **승급 4종(`Sentinel`·`Doomlord`·`Warden`·`Aegis`) 임포트 설정** — `Bilinear` + 압축 + `maxTS 2048` 로
  T1 3종과 다르다. `I-61` 이 PPU 만 고치고 놓쳤다. 별건이라 [`Parallel/BUGS.md`](Parallel/BUGS.md) `B7` 로
  **등재만** 했다 (사용자 판단 2026-08-31)

---

## 2-52. ✅ 재화 분리 — 런 골드 ↔ 메타 골드 (D25, 2026-08-31 48차)

**한 줄:** 지갑을 둘로 갈랐다. **이제 상점 가격과 메타 강화 가격을 따로 잡을 수 있다.**

> 전문 [`Parallel/DONE/D25.md`](Parallel/DONE/D25.md) · 근거 [`TODO.md`](TODO.md) §2-B(결정 3, 2026-08-29 확정)

### 원인 / 배경

런 골드와 메타 골드가 **`MetaProgression.Currency` 하나**였다.
[`BALANCE.md`](BALANCE.md) §3-1 기준 10층 런의 예상 수입 **약 1,240G** 중
**처치 보상이 ≈1,084G** 인데 상점 아이템은 **5~12G** 다. 상점이 사실상 무제한이었다.

문제는 "상점이 싸다"가 아니라 **값을 잡을 수가 없다**는 것이었다 —
상점 가격을 올리면 메타 강화가 같이 비싸지고, 메타를 싸게 하면 상점이 공짜가 된다.
**한 지갑에 성격이 다른 두 예산이 들어 있었다.** 이래서 §3-1 의 가격이 계속 미정으로 남아 있었다.

### 지갑 둘

| | 런 골드 `GameManager.RunGold` | 메타 골드 `MetaProgression.Currency` |
|---|---|---|
| **번다** | 적 처치 · 골드 픽업 · 농장 건물 · 런 중 이벤트 | **스테이지 클리어 보상만** (8 / 20 / 60) |
| **쓴다** | 상점 구매 · 상점 리롤 · 레벨업 리롤 | 영구 강화 · 캐릭터/스킨 해금 |
| **끝나면** | 🔴 **소멸** | 저장된다 |
| **보인다** | HUD · 스테이지 맵 · 상점 | 메인 메뉴 · 런 종료 화면(`Earned`) |
| **10층 예산** | **≈1,084G** | **≈156G** |

**수치는 하나도 안 바꿨다.** 원래 있던 두 흐름을 각자의 지갑에 꽂았을 뿐이다.

🔑 **클리어 보상은 곧바로 메타에 안 들어간다.** `GrantMetaGold` 는 `_pendingMetaGold` 에
**적립만** 하고, `SettleRun()`(사망 / 보스 처치)이 한 번에 `MetaProgression` 으로 넘긴다.

```
GrantMetaGold(8)  ──> _pendingMetaGold += 8      (Currency 그대로)
      …
OnPlayerDied() ──> SettleRun() ──> Currency += pending · RunGold = 0 · Save()
```

`TODO.md` §2-B 의 **"`MetaProgression.Currency` 는 런 종료 정산에서만 늘어나게 한다"** 를
글자 그대로 구현한 것이다. 부수 효과로 **런을 끝내야 메타 골드를 번다** 는 규칙이 생겼다 —
일시정지 메뉴의 `Quit` 은 `SettleRun` 을 거치지 않으므로 적립분이 사라진다. (의도한 동작)

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Core/GameManager.cs` | 🔴 **핵심.** `RunGold` · `OnRunGoldChanged` · `AddRunGold` · `SpendRunGold` · `GrantMetaGold` · `SettleRun` · `LastSettledMetaGold` 신설. `GrantGold` 가 런 골드로 방향 전환. `StartRun` 이 지갑 초기화, `OnWaveCleared`/`OnPlayerDied` 가 정산 |
| `Assets/Scripts/Shop/ShopManager.cs` | 구매·리롤·환급이 전부 런 골드 |
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | 레벨업 리롤 비용이 런 골드 (`SpendRunGold` 반환값으로 잔액 검사까지 한 번에) |
| `Assets/Scripts/Building/FarmBuilding.cs` | `meta.AddCurrency` → `GrantGold`. 중복이던 `GoldGain` 곱셈도 제거 |
| `Assets/Scripts/Meta/EventManager.cs` | 이벤트 `CurrencyBonus` → 런 골드 |
| `Assets/Scripts/UI/HUDManager.cs` · `StageMapUI.cs` · `ShopUI.cs` · `StageClearUI.cs` | 표시·구독 대상을 런 골드로 |
| `Assets/Scripts/UI/RunEndUI.cs` | `Gold {누적 보유액}` → **`Earned {이번 런이 번 메타 골드}`** |
| `Assets/Scripts/Dev/DevPanel.cs` | `Run n G` / `Meta n G (+적립)` 둘 다 표시 · 버튼도 2개 |

**씬·프리팹·CSV·SO 변경 0** — 새 직렬화 필드가 없어 `SampleScene.unity` 는 손대지 않았다
(`D24` 의 교훈대로 `git diff` 로 확인).

### 검증 로그 — 8건 전부 PASS

플레이 모드에서 `GameManager` API 를 직접 호출해 판정했다. **에디터 정지 · 콘솔 에러 0 · 잔류 오브젝트 0.**

| # | 판정 | 결과 |
|---|---|---|
| ① | `GrantGold(100)` 이 **런 골드**로 간다 | ✅ `Run 0→100` · `Meta 1231` **그대로** |
| ② | `GrantMetaGold(50)` 은 **적립만** 한다 | ✅ `Pending 0→50` · `Meta 1231` **그대로** |
| ③ | `SpendRunGold` 경계 | ✅ `30`→`True`(Run 70) · `999999`→`False` (잔액 불변) |
| ④ | `StartRun()` 이 런 지갑을 비운다 | ✅ `Run=0` · `Pending=0` |
| ⑤ | **실제 사망 경로**로 처치 골드가 런에 붙는다 | ✅ 고블린 1마리(`CurrencyDrop=1`) → `Run 0→1` · `Meta` 불변 |
| ⑥ | 🔴 **상점이 보는 지갑이 런 골드다** | ✅ 10G 슬롯 기준 `Run 0`→False · `9`→False · `10`→**True** (`Meta 1231` 은 무관) |
| ⑦ | 런 종료 정산 | ✅ `OnPlayerDied()` → `Meta 1231→1319`(+88) · `Run 500→0` · `Pending 0` · `LastSettled 88` |
| ⑧ | 화면 3곳이 각자 맞는 지갑을 본다 | ✅ 아래 |

**⑧ 내역** — `Run=777` · `Meta=1231` 인 상태에서 씬의 `CurrencyText` 를 전부 읽었다.

| 경로 | 표시 | 봐야 하는 것 |
|---|---|---|
| `UI Canvas/HUD/CurrencyText` | **`777 G`** | 런 골드 ✅ |
| `UI Canvas/MainMenuPanel/CurrencyText` | **`1231 G`** | 메타 골드 ✅ |
| `UI Canvas/StageMapPanel/CurrencyText` | `0 G` | 런 골드 ✅ (그리는 시점이 `StartRun` 직후라 0) |

> ⚠️ **⑥ 은 한 번 헛짚었다.** 처음엔 `CanReroll()` 로 판정했는데 `True/True` 가 나왔다.
> 버그가 아니라 **상점을 연 적이 없어 `CurrentRerollCost` 가 `0G`** 였던 것이다 (`0 >= 0`).
> 가격 10G 짜리 슬롯으로 다시 쟀다 → **교훈 180**.

검증으로 늘어난 메타 골드 **88G 는 회수**했다 (`AddCurrency(-88)` + `Save`) — 시작값 `1231` 로 복귀 확인.
다만 `OnPlayerDied()` 가 `RegisterRunResult` 도 부르므로 **개발용 세이브의 `TotalRuns` 가 1 늘었다.**

### 남은 것 — 🔴 이제 값을 잡을 수 있다

분리 자체가 목적이 아니라 **가격을 잡기 위한 전제**였다.
[`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-18** 로 넘겼다 —
상점 가격(런 골드 ≈1,084G 예산)과 메타 강화 가격(메타 골드 ≈156G 예산)을 **따로** 산정.
`BALANCE.md` §3-1 은 CONTENT 소유다.

---

## 2-51. ✅ 픽업 드랍표 CSV Import — 그리고 행운이 카드에 안 뜨던 것을 잡았다 (D24 / C25, 2026-08-31 47차)

**한 줄:** Import 한 번이 전부인 작업이었는데, **그 한 번이 46차가 놓친 것을 드러냈다.**

### 원인 / 배경

46차(`D20`)가 코드·프리팹·스프라이트·SFX 를 전부 넣었지만
`ExperienceManager` 의 드랍표는 **DEV 가 손으로 꽂아 둔 임시 배선**이었다.
`SceneWiring.csv` 는 CONTENT 소유 경로라 [요청-16](Parallel/REQ/CONTENT.md) 으로 넘겼고,
CONTENT 가 `C25` 에서 3줄(`pickupPrefabs`·`pickupChances`·`healPickupAmount`)을 채워
[요청-18](Parallel/REQ/DEV.md) 로 돌려줬다. **DEV 가 할 일은 Import 1회.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | `allItems` **24 → 25** (`Luck` 추가). **픽업 배열은 diff 0** |
| `Docs/Parallel/DONE/D24.md` | 신설 |
| `Docs/Parallel/REQ/DEV.md` | 요청-17 `닫힘(D20)` · 요청-18 `닫힘(D24)` |
| `Docs/Parallel/BOARD.md` · `TODO.md` | 현황 반영 |

🔴 **코드·애셋·CSV 변경 0.**

### 검증 로그 — 판정 6건 전부 PASS

| # | 판정 | 결과 |
|---|---|---|
| ① | Import 경고 `! ... 필드 없음` 이 없다 | ✅ `SceneWiring.csv : 12/12 적용` · 경고 0 · 에러 0 |
| ② | `pickupPrefabs` 6칸 · 순서 Gold/Heal/Haste/Bomb/Magnet/Invincible | ✅ 순서까지 일치 |
| ③ | `pickupChances` 6칸 · `0.025/0.015/0.01/0.007/0.006/0.004` | ✅ **합 0.067 (6.7 %)** |
| ④ | `healPickupAmount` = 30 | ✅ |
| ⑤ | 🔴 두 배열 길이가 같다 (어긋나면 조용히 틀린다) | ✅ **6 = 6** |
| ⑥ | 실제 사망 경로에서 가끔 떨어진다 | ✅ **300 처치 → 21개 (7.0 %)** |

⑥ 내역 — Gold 9 · Heal 5 · Haste 3 · Magnet 2 · Bomb 1 · Invincible 1. **6종 전부 등장.**
특히 `HealPickup` 은 이름 규칙에서 벗어난 유일한 항목이라 경로가 틀렸으면 **여기서만 0** 이 나왔을 것이다.
죽은 필드 `magnetPrefab`·`magnetDropChance` 는 `FindProperty` 가 둘 다 `null` — 완전히 사라졌다.

### 🔴 발견 — `D20` 커밋은 행운이 **레벨업 3택에 안 뜨는** 상태였다

Import 뒤 씬 diff 가 **딱 한 곳**이었다: `allItems.Array.size` **24 → 25**(`Luck`).
픽업 배열은 diff 가 **0** — `D20` 의 임시 배선이 CONTENT CSV 와 값이 완전히 같았다는 뜻이라
그건 좋은 소식이다. 문제는 `allItems` 다.

| 커밋 | 씬의 `allItems.Array.size` |
|---|---:|
| `dc9c03b` (45차) | 24 |
| **`a3e9e5c` (46차 D20)** | **24** ← 🔴 Luck 없음 |
| 지금 (47차) | **25** |

`D20` 은 **런타임에서 25개를 읽고 판정 ③ PASS** 로 적었다. 그런데 디스크에는 안 들어갔다.
런타임 `Length == 25` 는 *"메모리에 있다"* 까지만 증명한다 — 둘 사이에 `File/Save` 가 있다.
그 상태로 빌드했으면 **드랍 확률을 올리는 유일한 수단이 게임에서 사라지고,
`BonusLuck` 배관은 전부 살아 있으니 예외도 로그도 안 난다.**

🔑 **교훈 — 씬·프리팹을 바꾸는 판정은 `git diff` 로 한 번 더 본다.**
Import·인스펙터 편집처럼 **직렬화 파일을 건드리는 작업**은 커밋 직전에
`git diff <파일>` 에 그 변화가 실제로 보이는지 확인해야 한다.
이번엔 `D24` 가 우연히 같은 Import 를 다시 돌려서 드러났을 뿐이다 → [`TODO.md`](TODO.md) §1 에 등재.

### 부수 — `B5` 가 정량화됐다

300 처치에서 **300회 전부 예외**가 났다(웨이브 밖 사망 = `_currentWaveData` null).
다만 예외 지점이 `RollPickupDrop` **뒤**라 드랍 자체는 살아 있었다 —
`Die()` 순서가 `SpawnExpDrop` → `RollPickupDrop` → `GrantGold` → **`WaveManager.OnEnemyKilled`** 다.
→ [`Parallel/BUGS.md`](Parallel/BUGS.md) `B5`

---

## 2-50. ✅ 잡몹이 물건을 떨군다 — 픽업 6종 + 행운 (D20 / C23, 2026-08-31 46차)

### 왜 했나

적을 죽여도 나오는 게 **경험치 구슬 하나뿐**이었다. 자석·상자는 있었지만 엘리트 전용이라
**잡몹은 사실상 아무것도 안 줬다.** 뱀서라이크에서 "잡몹을 밀어붙일 이유"의 절반은
가끔 튀어나오는 물건인데 그 자리가 비어 있었다.

CONTENT 가 [`REQ/DEV.md`](Parallel/REQ/DEV.md) 요청-17(`C23`)로 **그림 5장 · 소리 3개 ·
드랍표 확률 6개 · 행운 곡선**을 한 번에 넘겼다. DEV 는 그것을 **게임 안에서 굴러가게** 만들었다.

### 무엇을 했나

| 갈래 | 내용 |
|---|---|
| **행운 스탯 신설** | `StatBlock.Luck` → `PassiveData.BonusLuck` → `PassiveEffect` 합산 → `BalanceImporter` 임포트·**익스포트** |
| **시한 버프** | `GrantInvincibility(sec)` · `GrantHaste(sec, delta)` · `IsHasted` |
| **픽업 4종** | `PickupKind` 에 `Bomb`·`Invincible`·`Haste`·`Gold` 추가 |
| **드랍표** | `ExperienceManager.RollPickupDrop` — 누적 확률을 **한 번만** 통과 |
| **애셋 8개** | 그림 5 · SFX 3 임포트 + `AudioLibrary` 등재(24→**27**) + 프리팹 4종 |

🔴 **`StatBlock.Zero()` 에도 `Luck` 을 넣었다** (I-21). 보너스용 블록이 기본 생성자로 만들어지면
기본 스탯이 그대로 더해져 값이 2배가 된다 — 새 필드를 넣을 때마다 같이 봐야 한다.

🔴 **익스포트도 같이 고쳤다.** 임포트만 고치면 다음 Export 때 `BonusLuck` 열이 통째로 사라져
CSV 가 조용히 망가진다. 왕복이 맞아야 CSV 가 원본 노릇을 한다.

🔴 **`minAttackSpeed = 0.1f` 하한 신설.** `WeaponBase.cs:54` 가 쿨다운에 `Final.AttackSpeed` 를
**곱하므로** 이 값이 0 이하가 되면 쿨다운이 0 이 된다. 오늘 최악 조합이
`Aegis −0.05 + AttackSpeed Lv5 −0.30 + 공속 −0.50 = 0.15` — 여유가 **0.05** 뿐이라
값이 아니라 **바닥**을 깔았다.

### 🔑 설계 판단 셋

**① 드랍은 한 번만 굴린다.** 종류마다 따로 굴리면 한 마리가 폭탄과 무적을 같이 떨구고,
표의 "합계 6.7%" 가 실제 드랍률과 어긋난다. 한 번만 굴리면 **합계가 곧 드랍률**이다.
행운은 합계가 아니라 **각 항목에** 곱한다 — 값은 같지만 **종류별 비율이 행운과 무관하게 유지**된다.

**② 힐 픽업은 새로 안 만들었다.** 기존 `HealPickup.prefab` 을 드랍표에 그대로 넣었다.
다만 드랍으로 나올 땐 건물 레벨이 없어 회복량을 줄 사람이 없으므로 `healPickupAmount` 를
`ExperienceManager` 가 들고 주입한다. 안 주면 **0 을 회복하고 조용히 사라진다.**

**③ 드랍표는 나란한 배열이다.** `SceneWiring.csv` 는 **구조체 배열을 못 쓴다** —
`BalanceImporter.WriteProperty`(605행)가 원소마다 `WriteScalar` 를 부르는데 구조체 원소는
`SerializedPropertyType.Generic` 이라 `! 타입 미지원` 으로 튕긴다.
`GameObject[]` + `float[]` 를 **같은 순서·같은 길이**로 두는 것이 유일한 길이고,
어긋나면 조용히 틀리므로 `Mathf.Min` 으로 잘라 쓴다.

### 변경한 파일

| 파일 | 무엇을 |
|---|---|
| `Scripts/Player/StatBlock.cs` | `Luck` 필드 + `Zero()` |
| `Scripts/Passive/PassiveData.cs` · `PassiveEffect.cs` | `BonusLuck` · `GetLuck` · 합산 |
| `Scripts/Player/PlayerStats.cs` | 무적/공속 타이머 · `minAttackSpeed` 하한 |
| `Scripts/Audio/AudioId.cs` | `BombPickup`·`BuffPickup`·`GoldPickup` (25/26/27) |
| `Scripts/Pickup/WorldPickup.cs` | `PickupKind` 4종 + 분기 + `DetonateBomb` |
| `Scripts/Experience/ExperienceManager.cs` | 드랍표 3필드 + `RollPickupDrop` |
| `Scripts/Enemy/EnemyBase.cs` | `Die()` — 엘리트·보스=상자 / 잡몹=드랍표 |
| `Editor/BalanceImporter.cs` | `BonusLuck` 임포트 + 익스포트 |
| `Game/Sprites/Pickups/{Bomb,Invincible,Haste,Gold}.png` · `Passives/Luck.png` | **신규** (+meta) |
| `Game/Audio/SFX_{Bomb,Buff,Gold}Pickup.wav` · `AudioLibrary.asset` | **신규 3** + 등재 |
| `Prefabs/Pickup_{Bomb,Invincible,Haste,Gold}.prefab` | **신규** |
| `Game/PassiveData/Luck.asset` · `Game/ItemData/Luck.asset` | **신규** (Import 산출물) |
| `Scenes/SampleScene.unity` | `ExperienceManager` 드랍표 **임시 배선** |
| `Docs/Parallel/BUGS.md` · `REQ/CONTENT.md` · `DONE/D20.md` | B5 증상 추가 · 요청-16 · 상세 |

`git diff --stat` = **32 files · 455 insertions(+) · 21 deletions(-)** (신규 파일 제외).

⚠️ **씬 배선은 임시다.** `SceneWiring.csv` 는 CONTENT 소유라 못 고친다. 지금은 씬에 직접 써 뒀고
그 행이 CSV 에 아직 없어 **Import 가 덮어쓰지 않는다.** 요청-16 이 처리되면 CSV 가 원본이 된다.

### 검증 로그

컴파일 에러 **0**. CSV Import — Passives 10→**11** · Items 28 · `SceneWiring.csv : 9/11 적용`
(못 적용한 2줄은 **예상된 것** — `magnetPrefab`/`magnetDropChance` 가 코드에서 사라졌다).

| # | 기준 | 결과 |
|---|---|---|
| ① | `Luck` SO `BonusLuck` = 0.15/0.3/0.5/0.7/1 · 기존 10개는 전부 0 | ✅ 기존 non-zero **0건** |
| ② | 픽업 5종이 Magnet 과 같은 크기 | ✅ 실체 높이 **480** vs Magnet **482** · 월드 0.50 동일 |
| ③ | `Luck` 이 3택 카드에 뜬다 | ✅ `allItems` **25**개, Luck 포함 |
| ④ | 행운 Lv5 에서 드랍이 정확히 2배 | ✅ **2.001배** (6.688% → 13.368%, 5만 회 × 2) |
| ⑤ | 폭탄이 Demon 은 지우고 Ogre 는 안 지운다 | ✅ **실물리 확인** |
| ⑥ | 소리 3종이 각자 다르게 들린다 | ❌ **불가** — 귀로만 판정 → 사용자 몫 |

**⑤ 는 한 마리씩 두 번 놓아 확인했다** (아래 B5 때문에 같이 놓으면 루프가 끊긴다):
Demon 3유닛 → 사망(`ExpDrop` 0→1) · Ogre 5유닛 → `DamagePopup` **오우거 자리 1건뿐** ·
`ExpDrop` 변화 **0**(생존) · 11유닛 밖 Demon **무피해**(반경 10 정확) · 폭탄 `active=False`(정상 despawn).
산술과도 맞는다 — Demon `200−4=196 ≥ 160` · Ogre `200−6=194 < 220`(**26 남음**).

### 🔑 통계 이상을 보면 로직보다 **세는 방법**을 먼저 의심한다

④ 의 첫 시행에서 −3.1σ 가 나왔다. 로직을 고치기 전에 `ObjectPool.cs` 를 읽어
**켜져 있는 오브젝트를 재사용하지 않는다**(= 개수 세기가 정확하다)를 확인해 계수 오류를
배제한 뒤 시드를 바꿔 다시 돌렸다. 편차는 사라졌다 — **시드 탓이었다.**

### 하다가 발견한 것 (고치지 않음)

- 🔴 **B5 에 새 증상** — `Die():449` 의 NRE 가 `DetonateBomb` 의 `foreach` **밖으로 전파**된다.
  뒤쪽 적은 피해를 아예 안 받고, `Collect()` 가 `Despawn()` 전에 끊겨 **폭탄이 바닥에 남는다.**
  같은 모양이 `AoeProjectile`·`MeleeWeapon`·`SummonWeapon`·`ToxinField` 에도 있다 →
  [`BUGS.md` B5](Parallel/BUGS.md) 에 등재, 우선순위 `낮음`→`재검토 필요`
- ℹ️ **스프라이트 bbox 기준이 서로 달랐다** — CONTENT 는 `alpha>127`, `PIL.getbbox()` 는 `alpha>0`.
  `Magnet`·`GoldGain` 에 거의 안 보이는 AI 잔여 픽셀이 **1.4~1.5%** 흩어져 있어 `a>0` bbox 가
  최대 311px 부풀었다. **애셋 결함이 아니라 측정 기준 차이**다(새 5장은 부분 알파 **0픽셀**)
- ℹ️ `.meta` 를 복사해 쓸 때 **GUID 말고 하나 더 있다** — 원본의 스프라이트 서브애셋 이름이
  `internalIDToNameTable`·`spriteSheet.sprites[].name`·`nameFileIdTable` **세 곳**에 박혀 있다
- ℹ️ `ExecutionResult.Log` 는 **정렬 지정자(`{1,-24}`)를 못 쓴다** — 그대로 문자로 찍힌다
- ℹ️ **대량 스폰 실험은 플레이 세션을 오염시킨다** — 5만 회 실험이 남긴 픽업 하나가 플레이어에게
  끌려가 먹히며 `LevelUp`(timeScale 0)로 들어갔고, `WorldPickup.Update` 가 그 상태에서 조기
  반환하므로 **다음 검증이 통째로 막혔다.** 실험 뒤에는 플레이를 껐다 켤 것

---

## 2-49. ✅ 문서 36개를 옵시디언에서 연다 — 정션 하나 (D23, 2026-08-31 45차)

### 왜 했나

문서가 **36개 · 약 1.1MB** 로 불었다. `SETUP_STATUS.md` 혼자 **388KB** 다.
"그때 그거 어디 적었더라"에 매번 `grep` 를 돌려야 했다.
**문제는 내용이 아니라 열람 수단**이었다.

옮기거나 복사하지 않았다. 복사하면 **두 벌이 되고 두 벌은 반드시 갈라진다** —
`CLAUDE.md` 가 규정한 경로(`Docs/...`)와 git 이력도 같이 깨진다.
그래서 **디렉터리 정션** 하나만 걸었다. 실체는 repo 에 그대로 있고 옵시디언은 들여다보기만 한다.

```
D:\obsidian_claude\UNITY_GAME\VS_LIKE   →   C:\Unity\VS_LIKE\Docs
```

### 변경한 파일

| 파일 | 무엇을 |
|---|---|
| `Docs/Parallel/DONE/D23.md` | 신설 — 상세 전문 |
| `Docs/Parallel/REQ/DEV.md` | 요청-16 `열림`→`닫힘(D21)` · **요청-17 원장 줄 신설**(본문 §요청-17 은 있는데 표에 줄이 없었다) |
| `Docs/Parallel/REQ/CONTENT.md` | 요청-15 신설 — `BOARD §0` 에 `Tools/Art/`·`Tools/Audio/` 누락 |
| `Docs/Parallel/BOARD.md` | §1 `D23` 등록→삭제 · §3 `D24` · §0 CONTENT 칸에 `DESIGN_CLASSES.md` |
| vault `00_INDEX.md` · `지도\개발 지도.md` | 신설 (git 밖) |

🔴 **코드·애셋 변경 0. Unity 에디터 미접촉** — `BOARD §2` 는 내내 `IDLE`.
🔴 repo `.md` 의 **본문은 위 5개 말고 한 글자도 안 고쳤다.** frontmatter 도 안 넣었다.

### 검증 로그

- 정션 mode `d----l` · repo 에 만든 임시 파일이 vault 에 즉시 보임(확인 후 삭제) ✅
- vault 노트 6개의 링크 **57개 전부 도달**, 깨짐 0 ✅
- `00_INDEX` → `지도\개발 지도` → 문서 = **2클릭**으로 36개 어디든 ✅
- vault 에 repo 문서 **사본 0** ✅

### 🔑 교훈 — 검사 도구가 "이상 없음"을 반환하면 그 도구부터 의심한다

1차 링크 검사는 `grep -oP '\]\(\K...'` 를 썼고 **"전 노트 0 링크"** 를 돌려줬다.
노트에 링크가 눈에 보이는데 0 이면 통과가 아니라 **고장**이다.
`sed` 로 다시 돌렸더니 `지도\개발 지도.md` 의 **36개가 전부 깨져 있었다** —
하위 폴더에서 `VS_LIKE/...` 를 썼는데 `../VS_LIKE/...` 여야 했다.
**거짓 통과를 그대로 믿었으면 지도 전체가 죽은 채로 커밋될 뻔했다.**

### 고치지 않고 보고만 한 것

- **문서↔실제 불일치 3건** — `DONE/D13.md` 머리말이 *"판정 12개 전부 PASS"* 라는데 **⑪ 는 실패**했다 ·
  `DONE/C22.md` §4 의 72° 근거는 `C23` 실측으로 **뒤집혔는데** 폐기 표시가 없다 ·
  `DONE/C1.md` §2 소제목이 자기 결론과 반대다
- **가리키는 대상이 없는 참조 4건** — `SfxId.Crit`(예약만) · `offsetAngle` 72°(**그런 코드 없음**) ·
  `Assets/Scripts/{Weapons,Visual}/`(**없는 폴더**) · `TUNING.md §3` "문어가 서는 자리"(`C23` 이 지움)
- 전부 vault `지도\개발 지도.md` §2 표에 등재. **고치지 않았다** (`CLAUDE.md` §1)

### 병렬 세션 사고

CONTENT 가 `C24` 를 커밋(`4c282e2`)하면서 **내가 스테이지도 안 한 `BOARD.md` 의 `D23` 줄까지
같이 가져갔다.** 내용상 문제는 없었지만, `git add` 와 `git commit` 을
**한 명령으로 붙여야** 하는 이유가 다시 확인됐다.

---

## 2-48. ✅ 촉수가 기사 뒤로 간다 — 한 줄 (D21 / C22, 2026-08-31 44차)

### 왜 했나

문어 소환수의 촉수 링(`Fx_TentacleLash`)이 `m_SortingOrder: 20` 이라 **플레이어보다 앞**에
그려졌다. D16 에서 눈으로 확인해 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-10 ③ 으로
넘긴 문제다. DEV 가 낸 두 안은 CONTENT 가 **둘 다 반려**했다 —
A(`Range` 축소)는 Lv1 넓이의 44% 가 되고, B(`offsetDistance` 확대)는 플레이어에게
달라붙은 적을 링이 못 잡는다.

🔑 **CONTENT 가 낸 대조군이 문제의 성격을 바꿨다.**

> 검 아크(`Fx_SwingArc`)는 **똑같이 order 20 이고 플레이어 위에서 바로 터지는데
> 기사를 0.00% 덮는다. 중앙이 비어 있어서다.**

⇒ 기하 문제가 아니라 **합성 문제**다. A·B 는 둘 다 헛다리였다.

### 변경한 파일

| 경로 | 무엇 |
|---|---|
| `Assets/Prefabs/Fx_TentacleLash.prefab:83` | `m_SortingOrder` **`20` → `-5`**. `git diff --stat` = **1 file · 1 insertion · 1 deletion** |

`-5` 는 새 층이 아니다 — `Proj_ToxinField`(독 장판)가 이미 쓰는 자리다.

```
바닥 타일 -100  <  촉수·독장판 -5  <  플레이어·적·투사체 0  <  픽업 1 …  검 아크 20
```

🔴 **일부러 D19 와 분리했다.** CONTENT 경고 *"둘을 같이 바꾸면 어느 쪽이 들었는지 모른다"* —
D19 도 문어가 서는 자리를 바꾸므로 정확히 그 상황이었다.
`Fx_SwingArc.prefab:83` 은 **`20` 그대로** 두었다.

### 검증 로그

플레이어를 `(3.71, 1.08)` 에 고정(`PlayerController` off), 문어 **Lv3**(`Range` 2.2),
촉수를 `SwingArcFx` 꺼서 **손으로 세워** 프레임을 멈춘 뒤,
같은 프레임을 order `20`/`-5` 로 **두 번 렌더**해 픽셀을 뺐다.
기사 실루엣은 플레이어 `SpriteRenderer` 를 껐다 켜서 떠냈다 — **336px**.

| 프레임 | 잉크 `20` | 잉크 `-5` | **기사 가림 `20`** | **기사 가림 `-5`** |
|---|---:|---:|---:|---:|
| TentacleLash_0 | 795 | 792 | 0 (0.0%) | **0** |
| TentacleLash_1 | 1352 | 1335 | 14 (4.2%) | **0** |
| TentacleLash_2 | 1691 | 1681 | 9 (2.7%) | **0** |
| TentacleLash_3 | 1702 | 1700 | 0 (0.0%) | **0** |
| TentacleLash_4 | 1412 | 1383 | 29 (8.6%) | **0** |
| TentacleLash_5 | 877 | 831 | **41 (12.2%)** | **0** |
| **합** | **7829** | **7722** | **93** | **0** |

최악 조건 — **적 8마리를 링 둘레(반지름 2.2)에 일부러 배치**:
잉크 7826 → 7706, 손실 **120px (1.5%)**. 폴백 `-1` 은 **필요 없다**.

눈으로도 확인: 같은 프레임(`_5`)을 order 만 바꿔 게임 카메라로 두 장 찍었더니
기사의 허리를 가로지르던 촉수 가닥이 **뒤로 넘어갔다**.

| # | CONTENT 기준 | 결과 |
|---|---|---|
| ① | 프리팹 `-5` · 검 아크 `20` 유지 | ✅ Unity 보고 — `Fx_TentacleLash -5` / `Fx_SwingArc 20` / `Proj_ToxinField -5` |
| ② | 🔴 Lv3 이상에서 기사 얼굴·상반신이 보이나 | ✅ **93px → 0px** |
| ③ | 바닥(`-100`)에 파묻히지 않나 | ✅ 잉크 손실 **1.37%** |
| ④ | 적(`0`) 위에서 읽히나 (안 되면 `-1`) | ✅ 최악 조건 손실 **1.5%**, `-1` 불필요 |
| ⑤ | 컴파일 에러 0 · 콘솔 경고/에러 0 | ✅ 플레이 종료 후 **Log 7건뿐** |

**정리 확인** — 임시 오브젝트(`__D21Lash`·`__D21Cap`) 삭제 · `PlayerController` 원복 ·
링에 세운 적 8마리 원위치 · `RenderTexture`/`Texture2D` 해제 · 임시 `.cs` **0개**(`RunCommand` 만) ·
씬 미변경(`git status` = 프리팹 1개뿐).

### 알게 된 것

🔑 **CONTENT 예상 대가 3.7~4.2% 가 실측 1.37% 였는데, 틀린 게 아니라 전제가 바뀐 것이다.**
D19 로 소환수가 공전을 그만두고 플레이어 **1.20 뒤**로 물러나면서 링 중심과 기사 중심이
어긋났다. CONTENT 의 예상은 **공전 시절(중심 거의 일치) 기준**이었다.
⇒ 예측이 어긋나면 "누가 틀렸나"보다 **"전제가 언제 바뀌었나"**를 먼저 본다.

🔑 **대조군이 문제의 성격을 바꿀 수 있다.** 고칠 값을 찾기 전에
**같은 조건에서 안 아픈 사례**가 있는지 먼저 찾는다.

**`protected` 필드는 `RunCommand` 에서 안 보인다.** `WeaponBase.Data`/`Level` 이 `protected` 라
`CS0122`, `[SerializeField]` 도 아니라 `SerializedObject.FindProperty` 는 `null` → NRE.
`System.Reflection` 은 금지(`CLAUDE.md` §4). 해법은 **public 우회로**였다 —
`AssetDatabase.LoadAssetAtPath<ItemData>(…)` → `.WeaponRef` · `.CurrentLevel` · `.GetRange(lv)`.
private `SwingArcFx.frames` 대신 **스프라이트 시트에서 6장을 직접 로드**했다.

상세: [`Parallel/DONE/D21.md`](Parallel/DONE/D21.md) · 회신 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-14

---

## 2-47. ✅ 이벤트 설계 문서 신설 — 첫 사례로 함정방 (D22, 2026-08-31 43차)

### 왜 했나

사용자 지시: *"EVENT로서 발생할 수 있는 전투 중 함정방 느낌으로 / 무한적 + 시간제한으로
살아남기 같은 컨셉 / 여러 이벤트를 추가할거라 그중 하나로 **일단은 문서화만**"*.

🔴 **코드 변경 0.** 지시가 문서화만이었다.

### 변경한 파일

| 경로 | 무엇 |
|---|---|
| `Docs/DESIGN_EVENTS.md` | **신설** (307줄). DEV 소유로 `Parallel/BOARD.md` §0 에 등재 |

### 🔑 코드를 읽었더니 설계가 바뀌었다

문서를 쓰기 전에 관련 파일 13개를 먼저 읽었다. **이벤트 시스템은 이미 절반 지어져 있었다.**

| 이미 있는 것 | 어디 |
|---|---|
| `StageType.Event` (맵 가중치 15%) · `GameState.Event` · `EventManager` 뼈대 | `Stage/` · `Meta/EventManager.cs` |
| `Events.csv` 5행 + `TriggerRandomWave` | `Assets/Game/Balance/Events.csv` |
| `UseTimerClear` / `SurvivalTime` — **시간제한 생존이 이미 된다** | `Wave/WaveData.cs` |
| `MaxAlive` — 차 있으면 소환이 **대기**한다(**취소가 아니다**) | `WaveManager.cs:173` |
| `RecycleFarEnemies` — 멀어진 적을 죽이지 않고 **앞으로 옮긴다** | `WaveManager.cs:253` |

⇒ **"무한적 + 시간제한"에 새 전투 시스템이 필요 없다.**
`MaxAlive` 가 대기이므로 **죽인 만큼 즉시 채워지고**, `RecycleFarEnemies` 때문에
**도망은 이미 불가능**하다. 무한 소환은 `WaveManager.cs:156` 의 `Count <= 0` 재해석
**한 줄**이면 된다(§4 B안 권장).

**진짜 벽을 세워 가두는 안(A)은 권장하지 않는다** — 뱀서라이크에서 몰리는 것은
회피 실력의 결과여야지 운이 되면 안 된다. §4-5 C안(가두지 않고 연출만) 권장.

### 남은 것

🔴 **결정 6건이 사용자 대기다** — [`DESIGN_EVENTS.md`](DESIGN_EVENTS.md) §7 · [`TODO.md`](TODO.md) §2.
사용자가 *"이벤트 관련은 나중에"* 로 보류했다 (2026-08-31).
⚠️ `E2 Elite Ambush` 는 `WaveManager.cs:193,206` 이 엘리트/보스 관문을
`StageType` 으로 하드코딩하고 있어 그 두 줄을 건드려야 한다.

---

## 2-46. ✅ 펫이 공전을 그만두고 꼬리처럼 따라온다 (D19, 2026-08-31 42차)

### 왜 했나

사용자 지시: *"펫류 무기가 꼬리처럼 따라오게. 많아지면 기차처럼 늘어지게."*

소환수는 **플레이어 둘레를 공전**하고 있었다. `SummonWeapon.FollowTarget()` 이
프리팹에 박힌 `offsetAngle` 로 방향을, `offsetDistance` 로 거리를 잡아
**플레이어 기준 고정 좌표**를 목표로 삼았다. 문제가 둘이다.

| # | 무엇 | 왜 |
|---|---|---|
| ① | 플레이어가 꺾으면 소환수가 **옆으로 미끄러진다** | 목표점이 플레이어와 통째로 회전한다. "따라온다"가 아니라 "매달려 있다" |
| ② | 각도를 **손으로 배분**해야 안 겹친다 | 드래곤 90° · 문어 210°. 소환사는 무기 칸이 5개라 셋째부터 답이 없다 |

🔴 **곁가지로 문서 오류를 찾았다.** [`TUNING.md`](TUNING.md) §3 이 *"3마리 이상이면 72° 간격으로
재배치된다"* 고 적고 있었고, CONTENT 도 그걸 근거로 각도 조정을 보류하고 있었다.
**그런 코드는 없다** — `offsetAngle` 은 전 프로젝트에서 `SummonWeapon.cs:104` **한 곳**에서만 읽혔다.
두 세션이 아무도 짜지 않은 기능을 근거로 판단을 미루고 있었다.

### 무엇을 했나 — "플레이어 기준 오프셋"을 버리고 **"지나온 길"**을 따라간다

| 경로 | 무엇 |
|---|---|
| `Assets/Scripts/Player/PlayerTrail.cs` | **신설.** 플레이어가 지나온 길을 짧게 기억하고, 길 위 임의 거리의 점을 돌려준다 |
| `Assets/Scripts/Weapon/SummonWeapon.cs` | 공전 → **기차.** 정적 줄(`Train`)에 등록하고 길 위 자기 칸 자리를 따라간다 |
| `Docs/Parallel/DONE/D19.md` | 신규 |
| `Docs/Parallel/REQ/CONTENT.md` | 요청-13 — 72° 정정 + 체감 3값 등재 요청 |

- **`SampleBack(d)`** = 지금 자리에서 **길을 따라** `d` 유닛 거슬러 올라간 점. 직선 거리가
  아니라 **길 위의 거리**라 꺾인 자리를 지나간다
- **얻은 순서가 곧 칸 순서.** 뒤처짐 거리 = **앞칸들의 `offsetDistance` 누적 합**
  (곱이 아니라 합이라 칸마다 간격을 다르게 줄 수 있다) → `1.2 · 2.4 · 3.6 …`

굳이 이렇게 한 이유 넷:

| 결정 | 이유 |
|---|---|
| **씬에 배선하지 않는다** (`PlayerTrail.Of` 가 없으면 붙인다) | 소환수는 무기 획득 시점에 생긴다. 그때 배선이 돼 있으리란 보장이 없고, 요구하면 `SceneWiring.csv`(**CONTENT 소유**)까지 건드려야 한다 |
| **`SampleBack` 이 스스로 기록한다** (프레임 번호로 잠금) | `PlayerTrail.LateUpdate` 와 `SummonWeapon.LateUpdate` 의 **실행 순서가 보장되지 않는다.** 어느 쪽이 먼저 와도 한 프레임에 딱 한 번 |
| **런 시작에 길을 미리 깔아 둔다**(`Seed`) | 빈 채로 두면 런이 시작하는 순간 **소환수가 전부 발밑에 겹쳐 있다가** 움직여야 풀린다 |
| **3유닛 넘게 순간이동하면 길을 지운다** | 런 재시작·스테이지 이동에서 플레이어가 통째로 옮겨진다. 옛 길을 두면 **화면 밖 옛 자리까지 줄을 서서** 끌려간다 |

`offsetAngle` 은 **필드만 남겼다** — `OwnerStats` 가 없을 때의 물러날 길. 평소엔 안 쓰인다(툴팁에 명시).

### 어떻게 확인했나

임시 프로브로 **플레이어를 ㄱ자로 몰았다**(+X 3초 → 직각으로 꺾어 +Y). 드래곤·문어를
실제 지급 경로(`LevelUpManager`)로 주고, 🔑 **"옛 방식이었다면 어디 있었을까"를 같은 줄에** 찍었다.

```
① 시작 직후  플레이어=(3.71,1.08)  Dragon 상대=(-1.20,+0.00)  Octopus 상대=(-2.40,+0.00)
② 직선 주행                        Dragon=(-1.87,+0.00)      Octopus=(-3.07,+0.00)
③ 꺾는 순간 t=0.50  Dragon 꺾인점까지 0.39  ·  Octopus 1.11   <- 앞칸이 먼저 돈다
   t=1.00           둘 다 x 편차 0.00
```

주행 중 1.87 은 `followLerp` 감쇠의 정상상태 지연과 맞는다 — `4 ÷ 6 = 0.667`, `1.20 + 0.667 = 1.867`.

| # | 확인할 것 | 결과 |
|---|---|---|
| ① | 시작부터 늘어서 있다 (발밑에 안 뭉친다) | ✅ 1.20 / 2.40 |
| ② | 칸 간격이 일정하다 | ✅ 항상 **1.20** (정지·주행·회전 전부) |
| ③ | 🔑 **길을 되짚는다** — 꺾을 때 옆으로 안 미끄러진다 | ✅ 꺾은 뒤 `x` 편차 **0.00** |
| ④ | 🔑 **앞칸이 먼저 돈다** (기차/뱀 거동) | ✅ t=0.50 에 드래곤 0.39 · 문어 1.11 |
| ⑤ | 옛 방식과 실제로 다르다 (대조군) | ✅ 옛자리와의 차 **1.13 ~ 3.08** — 한 번도 안 겹친다 |
| ⑥ | 컴파일 에러 0 · 콘솔 경고/에러 0 | ✅ 0건 |

임시 스크립트 삭제 완료 · 프로브 오브젝트는 **런타임에만** 세워 씬(`.unity`)을 안 건드렸다.

### 알게 된 것

**문서가 코드 동작을 주장하면 의심한다.** `TUNING.md` 의 "72° 재배치"는 아무도 짜지 않은
기능인데 두 세션이 그걸 근거로 판단을 미루고 있었다. 확인은 `grep` 한 번이었다.

### 남은 것

- 🔴 **요청-16(`Fx_TentacleLash.prefab` `m_SortingOrder` `20`→`-5`)은 여전히 필요하다.**
  문어가 2번칸이면 2.40~3.07 뒤로 물러나 `Range` 2.6 을 거의 벗어나지만,
  **문어만 들면 1번칸(1.20)** 이라 그대로다. CONTENT 가 *"둘을 같이 바꾸면 어느 쪽이 들었는지
  모른다"* 고 경고했으므로 **`D21` 로 따로 친다**
- **체감 미확인** — `followLerp` 6 · `minStep` 0.08 · `maxLength` 14 → 요청-13 으로 등재 요청
- **3칸 이상은 계산으로만 확인했다** (소환수 무기가 2종뿐)

전체 기록: [`Parallel/DONE/D19.md`](Parallel/DONE/D19.md)

---

## 2-45. ✅ 무기 장부를 하나로 합쳤다 — 증상은 넷이었다 (D18 / B6, 2026-08-31 41차)

### 왜 했나

무기 개수를 세는 장부가 **둘**인데 서로 안 맞았다. `GameManager.cs:228` 이 시작 무기를
`WeaponManager` 에 **직접** 넣어 `LevelUpManager._inventory` 를 건너뛰었기 때문에
매 판 시작부터 `_weapons.Count == CountOwned(Weapon) + 1` 이었다.

🔴 **개수만 문제가 아니었다.** B6 은 "무기 칸이 하나 모자란다"로만 등재돼 있었는데,
장부에 없다는 사실에서 증상이 넷으로 갈라진다:

| # | 증상 | 왜 |
|---|---|---|
| ① | 무기 칸이 하나 모자란다 | `CanAcquire` 가 통과시킨 걸 `AddOrUpgradeWeapon` 이 거절 |
| ② | 시작 무기가 레벨업 카드에 **"신규"로 다시 뜬다** | 미보유 취급. 고르면 슬롯만 먹고 무기는 안 는다 |
| ③ | `HasItem`·`GetItemLevel` 이 거짓말한다 | 시작 무기에 `false`/`0` |
| ④ | **진화 재료 판정에서 빠진다** | ③ 때문에 시작 무기를 쓰는 레시피가 성립 안 함 |

### 무엇을 했나 — A안 (사용자 결정)

| 경로 | 무엇 |
|---|---|
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | `GrantStartingWeapon(WeaponData,int)` + `FindWeaponItem()` 신설 |
| `Assets/Scripts/Core/GameManager.cs` | `ApplySelectedClass()` 가 `WeaponManager` 대신 이쪽을 부른다 + 경고 주석 |
| `Docs/Parallel/DONE/D18.md` | 신규 |

**`ApplyItem` 을 재활용하지 않았다** — 그쪽은 "한 레벨 올린다"(`_inventory[item]++`)라
`StartingWeaponLevel` 이 2 이상인 직업이 생기면 맞지 않는다. 여기는 레벨을 그대로 박는다.

**대응 `ItemData` 가 없으면 경고를 남기고 우회 지급한다** — 조용히 넘어가면 B6 이
되살아난 채 아무도 모른다. 지금은 Warrior/Ranger/Mage 셋 다 `Sword`·`Bow`·`Fireball` 이 있다.

⚠️ **호출 순서에 묶여 있다.** `StartRun()` 이 `ResetRunState()` → `ApplySelectedClass()`
순으로 부른다. 뒤집으면 `_inventory.Clear()` 가 시작 무기를 지운다.

### 어떻게 확인했나

```
① 런 시작 직후    CountOwned=1  _weapons=1  차이=0 (예전 1)  상한=3
                  장부: Sword Lv1 HasItem=True
② 상한까지 채우기  Bow 1→2 OK · Gun 2→3 OK · CountOwned=3 _weapons=3
                  꽉 찬 뒤에도 CanAcquire=true 인 신규 무기 = 0
```

| # | 확인할 것 | 결과 |
|---|---|---|
| ① | 두 장부의 수가 같다 | ✅ 차이 0 |
| ② | 상한까지 실제로 채워진다 | ✅ 3/3, 거절 0 |
| ③ | 🔴 `무기 슬롯 꽉 참` 경고 0 | ✅ 콘솔 6건 전부 `Log` |
| ④ | 시작 무기가 보유로 잡힌다 | ✅ `HasItem=True` · `Lv1` |
| ⑤ | 꽉 찬 뒤 과잉 제안 없음 | ✅ 0건 |

### 알게 된 것

**두 장부를 각각 찍지 말고 뺄셈 한 줄을 찍는다.** 0 이 아니면 그 자체가 버그다 —
다음에 어긋나도 같은 프로브로 바로 드러난다.

전체 기록: [`Parallel/DONE/D18.md`](Parallel/DONE/D18.md)

---

## 2-44. ✅ 엘리트·보스 외곽선을 ×0.75 로 — 자 두 개 사이에 끼운 한 줄 (D17 / C21, 2026-08-31 40차)

### 왜 했나

D15 가 `_OutlineTexSize` 하드코딩을 고친 뒤로 `_OutlineWidth` 는 **진짜 텍셀 수**가 됐다.
그런데 `14/12` 는 그 전에 눈으로 맞춘 값이라, 고친 뒤에는 **낱장 시절의 약 2배**로 두꺼웠다.
진짜 원인은 시트 전환이 아니라 **PPU 2048 → 512** 다 — 텍셀은 ½ 인데 PPU 가 ¼ 이라 순증 2배.

### 무엇을 했나

`EnemyBase.ApplyRankOutline()` **한 줄.** 비율 7:6 은 유지하고 배율만 ×0.75.

```cs
_mpb.SetFloat(OutlineWidthId, IsBoss ? 9f : IsElite ? 10.5f : 0f);   // 전: 12f / 14f
```

| 경로 | 무엇 |
|---|---|
| `Assets/Scripts/Enemy/EnemyBase.cs` | 폭 1줄 + 자 두 개를 적은 주석 |
| `Docs/Parallel/DONE/D17.md` | 신규 |
| `Docs/Parallel/REQ/DEV.md` | 요청-15 → `닫힘(D17)` |
| `Docs/Parallel/REQ/CONTENT.md` | 요청-11 신설 (실측 회신) |
| `Docs/Parallel/BOARD.md` · `TODO.md` · `SETUP_STATUS.md` | 현황 |

### 🔴 왜 `7` 이 아닌가 — 자가 두 개고 서로 반대다

`ortho 6` · 1080p → 1 유닛 = 90 화면 px. 화면 px = `폭 ÷ 512 × SizeScale × 등급배율 × 90`.

| 자 | 값 | 근거 |
|---|---|---|
| 하한 | **2 화면 px** | 시트가 `filterMode 0`(Point) — 그 밑은 선이 끊겨 **점선**이 된다 |
| 상한 | **키의 ~9 %** | 슬라임 엘리트는 화면에서 **23px** 뿐이라 더 굵으면 실루엣을 먹는다 |

`7` 이면 Wolf **1.36px** · Slime 1.44 · Goblin 1.60 으로 흔한 잡몹 셋이 점선이 된다.
`10.5` 는 하한 Wolf **2.04px** · 상한 Slime **9.2 %** 로 **양쪽에 동시에 딱 걸친다.**

**종별 보정은 하지 않았다** — 두 자가 정반대를 가리킨다. Goblin 을 10.5 로 두고 맞추면
Ogre 가 화면 px 균일화 **6.6** vs 키 대비 균일화 **16.1** 로 갈린다.
한 숫자로 두면 두 오차를 반씩 나눠 갖는다(화면 px 2.0~3.8 · 키 대비 5.0~9.2 %).
이 판단 근거를 코드 주석에 박아 뒀다.

### 어떻게 확인했나

**① 속성 프로브** — `EnemyData` 6종 × 등급 3 을 실제로 `Initialize()` 태워 MPB 값을 읽었다.
CONTENT 예측표와 **소수점까지 일치**(Wolf 엘리트 2.04px · Ogre 보스 5.06px …).

**② 렌더 픽셀 실측** — 계산이 아니라 **나온 픽셀을 셌다.** `timeScale 0` 으로 세워 놓고
`Camera.main` → `RenderTexture(1920×1080)` → `ReadPixels`, 외곽선 색 ±0.10 카운트.

```
일반   보라 0    빨강 0     bbox 없음
엘리트 보라 261  빨강 0     bbox 34x31
보스   보라 0    빨강 555   bbox 50x50
```

**③ 하한 자 검증(점선 여부)** — 행마다 색 픽셀 연속 덩어리를 셌다.
최악 사례 Wolf 엘리트(`SizeScale 0.85` → 2.04px)에서 보라 216px · 24행 · 끊긴 행 2
(= 머리·발 끝 캡). **가장 작은 적에서도 선이 이어진다.**

**④ 실플레이 회귀** — Warrior 68.8초, 살아 있는 적 전수 감사 2회(19마리·24마리)
**전부 일반 · `_OutlineWidth` 불일치 0.**

| # | 확인할 것 | 결과 |
|---|---|---|
| ① | `EnemyBase.cs` 가 `9f` / `10.5f` | ✅ |
| ② | 컴파일 0 · 엘리트/보스가 보라/빨강 | ✅ 보라 261px · 빨강 555px 실측 |
| ③ | 일반 몹 외곽선 0 (회귀) | ✅ 24마리 불일치 0 |
| ④ | 🟡 난전에서 0.2초 안에 골라지는가 | 🟡 **미판정** — 68.8초에 엘리트가 한 번도 안 나왔다 |

정리 확인 — 세워 둔 오브젝트 4개 파괴 · `timeScale` 1 복구 · 플레이 종료 ·
콘솔 `Types:["All"]` **10건 전부 `Log`**(Warning 0 / Error 0).

### 알게 된 것

- **자동 플레이로는 엘리트를 못 본다.** 68.8초를 돌려도 등급 몹이 안 나온다.
  등급 표현을 볼 때는 `Initialize(data, isElite:true)` 로 **세워 놓고 찍는 쪽**이 유일하게 확실하다
- **`LevelUpManager.allItems` 는 private 직렬화 필드다** — `RunCommand` 에서 직접 못 읽는다.
  `new SerializedObject(lum).FindProperty("allItems")` 로 우회. 레벨업이 뜨면 `timeScale 0` 이라
  치워 주지 않으면 게임 시간이 안 흐른다
- **외곽선 색이 등급마다 고정(보라/빨강)이라 색 카운트만으로** 있는지·몇 px 인지·끊겼는지를
  한 번에 잰다. 셰이더 값을 믿을 필요가 없다

전체 기록: [`Parallel/DONE/D17.md`](Parallel/DONE/D17.md)

---

## 2-43. ✅ 소환수 2종이 게임에 등장하고, 제 소리로 운다 (D16 / C19·C20, 2026-08-30 39차)

### 왜 했나

요청-13(C19)과 요청-14(C20)가 **같은 무기 2종을 각각 반쪽씩** 다루고 있어 하나로 묶었다.

| | 요청-13 | 요청-14 |
|---|---|---|
| 무엇 | 소환수 2종을 CSV 로 올린다 + 촉수 그림 교체 | 소환수 2종에 제 효과음을 준다 |
| 없으면 | 프리팹·코드는 다 있는데 **레벨업 카드에 안 뜬다** | 화염구=`WeaponCast`(마법음) · 촉수=`WeaponFire`(**총소리**) |

D14 에서 검·독장판이 남의 소리를 쓰던 걸 고쳤는데 **소환수 2종만 남아 있었다.**
촉수 그림은 원판이 **가운데가 꽉 찬 원반**이라 소환수 몸통을 가렸다 —
"문어가 후린다"가 아니라 "문어가 사라졌다"로 보였다.

### 변경한 파일

| 경로 | 무엇 |
|---|---|
| `Assets/Game/WeaponData/Summon{Dragon,Octopus}.asset`(+`.meta`) | 신규 — CSV Import 산출물 |
| `Assets/Game/ItemData/Summon{Dragon,Octopus}.asset`(+`.meta`) | 신규 — 〃 |
| `Assets/Scenes/SampleScene.unity` | `LevelUpManager.allItems` 22 → **24** |
| `Assets/Game/Sprites/Effects/TentacleLash.png` | 덮어쓰기 (**`.meta` 보존**) |
| `Assets/Game/Audio/SFX_{TentacleLash,DragonSpit}.wav`(+`.meta`) | 신규 — `_Incoming/` 에서 이동 |
| `Assets/Game/Audio/AudioLibrary.asset` | `sfx` 22 → **24** + 기존 2개 볼륨 조정 |
| `Assets/Scripts/Audio/AudioId.cs` | `SfxId.TentacleLash=6` · `DragonSpit=7` |
| `Assets/Scripts/Weapon/SummonWeapon.cs` | 호출부 **2줄** + 경고 주석 |
| `Docs/Parallel/DONE/D16.md` | 신규 |
| `Docs/Parallel/REQ/CONTENT.md` | 요청-10 신설 (청취 판정 넘김) |
| `Docs/Parallel/REQ/DEV.md` | 요청-13·14 → `닫힘(D16)` |

### 검증 로그

임시 프로브를 `AudioManager.PlaySfx(SfxId)` 에 넣고 Warrior 로 한 판 돌렸다.
D14 와 같은 이유로 **옛 소리를 대조군으로 같이 셌다.**

```
[SFXPROBE] 집계 46건 — DragonSpit=17  TentacleLash=15  WeaponSwing=14
                        대조 WeaponFire=0  WeaponCast=0
드래곤 간격 = 1.4s 정확 (Lv1 쿨다운, 한 번도 안 거름)
촉수 간격 = 1.2s, 중간 2.4s 공백 = FireRing 이 적 없어 조용히 리턴
```

촉수 그림 6프레임 실측 (`timeScale=0` 으로 세워 놓고 캡처):

```
        바깥지름   중앙 구멍   잉크
f0      45.49px    12.65px    2.3%
f3      99.85px    15.52px    4.6%   <- 최대
f5      80.65px    16.97px    2.3%
중심 r<=12px 불투명 픽셀 = 0  (6프레임 전부)
계약 spriteRadiusAtScaleOne 0.9951 = 99.51px  →  실측 99.85px, 차 0.34px (코드 무수정)
```

| 판정 | 결과 |
|---|---|
| `WeaponData`·`ItemData` 4개 생성, `m_Script` 유효 | ✅ |
| `allItems` 22 → 24, null 0 · 레벨업 카드 출현 (4장×20회에서 Dragon 6 / Octopus 5) | ✅ |
| 🔴 촉수 링이 몸통을 가리지 않는다 | ✅ 6프레임 전부 중앙 불투명 0 |
| 🔴 옛 소리가 같이 나지 않는다 | ✅ `WeaponFire=0` / `WeaponCast=0` |
| 컴파일 에러 0 · 콘솔 0 · 프로브 잔재 0 | ✅ |
| 청취 4건(촉수/드래곤 음색 · 타격 싱크 · 마법사 `WeaponCast`) | 🟡 → `REQ/CONTENT.md` 요청-10 |

전체 기록: [`Parallel/DONE/D16.md`](Parallel/DONE/D16.md)

---

## 2-42. ✅ 엘리트 외곽선이 실루엣으로 돌아왔다 — 원인은 둘이었다 (D15 / B1, 2026-08-30 38차)

### 왜 했나

사용자가 실플레이에서 **"엘리트 외곽선이 실루엣과 어긋난다"** 고 보고했다 (B1).
`BUGS.md` 에 이미 원인이 *"외곽선이 시트의 옆 프레임 알파를 빨아들인다"* 로 적혀 있었다.

🔴 **고치기 전에 재 보니 그 진단은 절반이었다.** 문서를 믿고 바로 고쳤으면
**주범을 놓친 채 곁가지만 고치고 닫았을 것이다.**

### 원인 ① (주범) — `_OutlineTexSize` 가 `512` 로 굳어 있었다

선 굵기는 `d = _OutlineWidth / _OutlineTexSize` 라는 **uv 거리**다.
`_MainTex_TexelSize` 를 쓰면 2D SRP Batcher 가 머티리얼을 배칭에서 통째로 빼기 때문에
텍스처 크기를 **직접 넘기는** 구조인데, 그 값이 셰이더 기본값에서 멈춰 있었다.

I-58 이 적을 **1024 시트**로 바꾼 순간 이 값이 틀린 값이 됐다:

| | 낱장 png 시절 (512) | 시트 (1024) |
|---|---|---|
| `_OutlineWidth 14` 의 실제 굵기 | **14 텍셀** | **28 텍셀** |

그런데 시트의 **한 프레임 자체가 130~230텍셀**뿐이다:

| 적 | 프레임 높이 | 28텍셀 선이 차지하는 비율 |
|---|---|---|
| Slime | 118 | **23.8 %** |
| Wolf | 133 | **21.1 %** |
| Goblin | 135 | **20.8 %** |
| Demon | 190 | 14.7 % |
| Zombie | 194 | 14.4 % |
| Ogre | 206 | 13.6 % |

낱장 시절엔 같은 설정이 **키의 2.7 %** 였다. 사용자가 본 것은 선이 아니라
**캐릭터를 덮은 보라 덩어리**다. 체감의 대부분이 이것이었다.

### 원인 ② — 옆 프레임 번짐. 있지만 예측보다 훨씬 드물다

`BUGS.md` 의 여백 표는 프레임이 **256 격자 칸에 꽉 찬다**고 가정했는데,
실제 `Sprite.textureRect` 는 **타이트 슬라이싱**이라 칸이 아니다:

```
Wolf frame_5 textureRect = (274.08, 580.08, 223.90, 117.85)   ← 소수점. 격자가 아니다
```

내 여백 + 이웃 여백이 합쳐지므로 28텍셀을 넘는 경우가 드물다. 96프레임 전수 조사:

| 적 | 번지는 픽셀 | 판정 |
|---|---|---|
| Demon f14 | **2616 px** | 🔴 실제로 번진다 |
| Wolf | 0~수 px | 사실상 없음 |
| 그 외 4종 | 0 px | 없음 |

그래도 고쳤다 — **드물다 ≠ 안 고쳐도 된다.** 앞으로 시트를 빽빽하게 채우면 바로 재발하고,
고쳐 두면 CONTENT 에 "여백 30px 이상" 같은 제약을 요구하지 않아도 된다.

### 변경한 파일

| 경로 | 무엇 |
|---|---|
| `Assets/Game/Shaders/SpriteOutline.shader` | `_SpriteRect` 프로퍼티 신설(기본 `(0,0,1,1)`). `SampleAlpha` 의 `0~1` 검사를 **이 사각형 검사**로 교체. `_OutlineTexSize` 주석에 512 의 내력 기록 |
| `Assets/Scripts/Enemy/EnemyVisual.cs` | `ApplySpriteRect(Sprite)` 신설 — `Setup` 끝 + `StepFrames` 2곳에서 호출 |

> ⚠️ **`Assets/Game/Shaders/` 는 CONTENT 소유인데 이 파일만 DEV 가 고쳤다** (사용자 결정).
> MPB 로 값을 넘기는 쪽(`EnemyVisual`)과 받는 쪽(셰이더)이 한 몸이라
> 쪼개면 **어느 쪽이 원인인지 검증할 수 없다.**

```cs
r       = new Vector4(tr.xMin/tw, tr.yMin/th, tr.xMax/tw, tr.yMax/th);
texSize = Mathf.Max(tw, th);   // uv 거리는 가로세로 공통이라 기준도 하나여야 한다
```

**스프라이트가 바뀔 때만** 넘긴다 (`_rectSprite` 캐시). `SetPropertyBlock` 은 매 프레임
부를 만큼 싸지 않고, 프레임은 초당 10장 남짓만 바뀐다.

⚠️ **풀 재사용 함정을 같이 막았다.** `Setup` 에서 `_rectSprite = null` 로 캐시를 깨고 **항상**
다시 넘긴다 — 안 그러면 늑대였던 개체가 슬라임으로 재활용될 때 **남의 칸**이 남는다.

### 검증 로그

🔴 **A/B 렌더가 처음에 `diff px = 0` 을 냈다. 여기서 "고쳐졌다"고 넘어가면 안 됐다** —
값이 셰이더에 도달조차 못 한 경우와 구분이 안 되기 때문이다.
`_SpriteRect` 를 **일부러 아주 작은 상자**로 강제해 대조군을 세웠다:

```
보라 픽셀  32821 → 4710      ← MPB 가 CBUFFER 안의 프로퍼티도 덮어쓴다는 증명
```

그 다음에야 `diff = 0` 이 "이 표본엔 번질 이웃이 없다"는 뜻임을 말할 수 있었다.
표본을 Demon f14 로 바꾸니 **2616px → 0px**.

**실플레이 검증 (살아있는 적 14마리):**

```
살아있는 적 14   _SpriteRect 불일치 0   _OutlineTexSize 틀림 0   외곽선 씌운 수 4
Enemy_Goblin(Clone) frame_10 tex=1024 want=(0.5147,0.3165,0.7363,0.4511) got=(동일) texSize=1024
```

서로 **다른 프레임 3종**(`frame_2`·`frame_10`·`frame_14`)이 각자 자기 사각형과 일치 =
스폰 때 한 번이 아니라 **프레임마다 따라간다.**

| # | 확인할 것 | 결과 |
|---|---|---|
| ① | 셰이더가 `_SpriteRect` 를 받는다 | ✅ 대조군 32821 → 4710 |
| ② | 프레임마다 갱신된다 | ✅ 14마리 불일치 0 · 다른 프레임 3종 |
| ③ | `_OutlineTexSize` 가 실제 크기다 | ✅ 14마리 전부 `1024` |
| ④ | 옆 칸 알파를 안 빨아들인다 | ✅ Demon f14 2616px → 0px |
| ⑤ | 낱장 png 적이 망가지지 않는다 | ✅ 기본값 `(0,0,1,1)` = 종전과 동일 |
| ⑥ | 게임 화면에서 선이 실루엣에 붙는다 | ✅ 게임 카메라 캡처 |
| ⑦ | 컴파일 에러 0 | ✅ 콘솔 8건 전부 `Log`, Warning·Error **0** |
| ⑧ | 최종 굵기가 보기 좋은가 | 🟡 사람이 봐야 한다 → CONTENT 이관 |

### 검증 장치 — 씬을 안 더럽히는 오프라인 A/B 렌더

`HideFlags.HideAndDontSave` 오브젝트를 레이어 31 에 놓고 `cullingMask = 1<<31` 인
직교 카메라로 `RenderTexture` 에 그린 뒤 `ReadPixels` → `EncodeToPNG` → `Temp/`.
**에디트 모드에서 돌고, 프로브 MonoBehaviour 도 씬 오브젝트도 만들지 않는다** —
이번 작업은 지울 임시 파일이 하나도 없다.

> ℹ️ 스프라이트 원본 텍스처는 `isReadable=false` 라 `GetPixels32` 가 안 된다.
> png 를 `File.ReadAllBytes` + `ImageConversion.LoadImage` 로 일회용 `Texture2D` 에 올려 읽었다.

### 남긴 것

- 🟡 **최종 굵기는 사람이 정한다.** 지금 `_OutlineWidth` 는 엘리트 **14** · 보스 **12** 이고
  이제 *실제* 텍스처 크기로 걸리므로 낱장 시절 체감의 **2배**다. 같게 하려면 **7 / 6**.
  → [`Parallel/REQ/CONTENT.md` 요청-9](Parallel/REQ/CONTENT.md) · 값은 `EnemyBase.cs:134` 하드코딩
- `Assets/Game/Materials/SpriteOutline.mat` 은 `_OutlineTexSize` 를 **직렬화하지 않는다**
  (셰이더 기본값 512 를 씀). 지금은 `EnemyVisual` 이 매번 덮어써서 문제없지만
  **적 이외의 것**이 이 머티리얼을 쓰면 512 를 다시 밟는다. 현재 사용처는 적뿐
- **`B6` 신규 등재** — Dev 패널로 무기 레벨을 내리면 무기가 사라질 수 있다.
  `LevelUpManager._inventory` 와 `WeaponManager._weapons` 라는 **장부가 둘**인데
  `GameManager.cs:228` 이 시작 무기를 뒤쪽에만 넣는다. **고치지 않고 등재만** 했다

### 이번에 배운 것

🔴 **`IN.uv` 는 스프라이트 기준이 아니라 텍스처 기준이다.**
`if (uv.x < 0 || uv.x > 1)` 같은 검사는 **낱장 png 일 때만 우연히 맞는다.**
아틀라스·시트를 쓰는 순간 한 번도 걸리지 않는다.
앞으로 스프라이트 셰이더에서 **이웃 샘플링**을 하면 사각형을 반드시 같이 넘길 것.

🔴 **`Sprite.textureRect` 는 격자 칸이 아니라 타이트 bbox 다.**
슬라이서가 알파에 딱 붙여 자르므로 소수점 좌표가 나온다.
"칸이 256 이니 여백이 몇 px" 같은 계산은 **틀린 전제**다. 실제 rect 를 읽어야 한다.

🔴 **문서에 적힌 원인 진단도 "실제 상태"가 아니다.** B1 원문은 두께 문제를
*"TUNING 감"* 이라며 미뤄 뒀는데 그게 주범이었다. `CLAUDE.md` §4 의
"문서를 믿고 바로 수정하지 않는다"는 **애셋뿐 아니라 진단에도 적용된다.**

ℹ️ `MaterialPropertyBlock` 은 `CBUFFER_START(UnityPerMaterial)` 안의 프로퍼티도 덮어쓴다.
(대신 MPB 를 쓰는 렌더러는 SRP Batcher 에서 빠진다 — 적은 `_AnimPhase` 때문에 이미 빠져 있었다.)

---

## 2-41. ✅ 검과 독 장판이 남의 소리를 그만 빌린다 (D14 / C17·C18, 2026-08-30 37차)

### 왜 했나

무기 두 개가 **자기 소리가 없어서 남의 것을 쓰고 있었다.**

| 무기 | 나던 소리 | 실제로 하는 일 |
|---|---|---|
| 검 (D10 에서 근접으로 재해석) | `WeaponFire` = **총소리** | 칼을 휘두른다 |
| 독 장판 (D11 신설) | `WeaponCast` = **마법 시전음** | 독을 바닥에 쏟는다 |

🔴 **이건 소리 문제가 아니라 판정 문제였다.** `TUNING.md` §3 의 근접 7값·장판 값들은
"안 시원하다"를 재는 항목인데, 그 원인이 `halfAngle` 이 아니라 **총소리**일 수 있다.
소리를 안 고치고 숫자를 흔들면 **엉뚱한 값이 굳는다.** 그래서 수치 판정보다 먼저 했다.

클립은 CONTENT 가 `Tools/Audio/gen_sword_swish.py` · `gen_toxin_splash.py` 로 **절차 생성**해
`_Incoming/Audio/` 에 두고 요청했다 ([`REQ/DEV.md` 요청-12](Parallel/REQ/DEV.md)).

### 무엇을 했나

| # | 무엇 |
|---|---|
| ① | wav 2장을 `_Incoming/Audio/` → `Assets/Game/Audio/` 로 이동 |
| ② | 임포트 규격을 기존 SFX 와 동일하게 강제 |
| ③ | `SfxId` 무기 블록에 `WeaponSwing = 4` · `ToxinSpill = 5` |
| ④ | `AudioLibrary.asset` 항목 2개 (20 → **22**) |
| ⑤ | `MeleeWeapon.cs:52` `WeaponFire` → `WeaponSwing` |
| ⑥ | `FieldWeapon.cs:32` `WeaponCast` → `ToxinSpill` |

**변경한 파일**

| 경로 | 무엇 |
|---|---|
| `Assets/Game/Audio/SFX_WeaponSwing.wav`(+`.meta`) | 신규 — 0.280s · mono · 44100 · ADPCM |
| `Assets/Game/Audio/SFX_ToxinSpill.wav`(+`.meta`) | 신규 — 0.701s · 〃 |
| `Assets/Game/Audio/AudioLibrary.asset` | `sfx` 항목 2개 추가 |
| `Assets/Scripts/Audio/AudioId.cs` | `SfxId` +2 (기존 값 무변경) |
| `Assets/Scripts/Weapon/MeleeWeapon.cs` | 1줄 |
| `Assets/Scripts/Weapon/FieldWeapon.cs` | 1줄 |
| `Docs/Parallel/DONE/D14.md` | 신규 |
| `Docs/Parallel/REQ/CONTENT.md` | 요청-8 신설 — 실측 회신 + 소환수 SFX 요청 |
| `Docs/Parallel/REQ/DEV.md` | 요청-12 → `닫힘(D14)` |

### 어떻게 만들었나 — 위험했던 지점 3개

**가. Unity 기본 임포트 설정은 프로젝트 규격이 아니다**

Refresh 직후 두 wav 는 `compressionFormat: 1`(Vorbis) · `forceToMono: 0` 으로 들어왔다.
기존 SFX 는 전부 `2`(ADPCM) · `1` 이다. **그냥 두면 두 소리만 다른 압축으로 굴러간다.**
`AudioImporter` 를 직접 세팅하고 `SaveAndReimport()` 했다 — D8 에서 밟은 함정과 같다.

CONTENT 는 스테레오로 구워 왔지만 `forceToMono: 1` 이 모노로 접는다. CONTENT 가 미리 재 둔
**모노 합산 피크 0.344 / 0.383** 이 실제로 들리는 값이고, 좌우 상쇄가 없다는 예측이 맞았다.

**나. `SfxId` 값은 정수로 직렬화된다 — 중간에 끼워 넣는 건 안전하다**

`AudioLibrary` 는 배열 인덱스가 아니라 `Id` 값으로 매핑한다(`_sfxMap[e.Id] = e`).
그래서 **분류 안의 빈 번호(4·5)에 끼워 넣어도 기존 항목이 하나도 밀리지 않는다.**
"끝에 추가하라"는 옛 설명은 D8 에서 이미 정정했다. 기존 값은 하나도 안 건드렸다.

**다. `.asset` YAML 을 손으로 쓰지 않았다**

에디터가 들고 있는 메모리 사본이 이기면 디스크 편집이 다음 저장에 덮어써진다.
`SerializedObject` → `ApplyModifiedPropertiesWithoutUndo` → `SaveAssets` 로 넣고
**디스크를 다시 읽어** 확인했다. Id 4·5 가 이미 있으면 중단하도록 짜서 재실행도 안전하다.

### 검증 로그

임시 프로브 `Assets/Scripts/Dev/D14SfxProbe.cs` 로 `AudioManager` 보이스 풀(16개)을
매 프레임 훑어 **새로 시작한 클립**을 셌다. 🔴 **빌려 쓰던 소리도 대조군으로 같이 셌다** —
"새 소리가 난다"만으로는 **옛 소리가 같이 나는지**를 알 수 없기 때문이다.
🔴 보이스는 재사용되므로 상승 엣지는 `isPlaying` 만으로 못 잡는다. `(!wasPlaying || clip != lastClip)` 둘 다 봤다.

```
[D14] 프로브 시작 — 보이스 17개, SfxVolume=0.31
[D14] SWING #5 vol=0.094 pitch=0.971 t=48.496
[D14] SWING #6 vol=0.094 pitch=0.968 t=48.745
[D14] SWING #7 vol=0.094 pitch=0.968 t=48.999
[D14] TOXIN #1 vol=0.119 pitch=1.072 t=47.634
[D14] 프로브 집계 — Swing=7 Toxin=1 | 대조 Fire=0 Cast=0
```

| # | 확인할 것 | 결과 |
|---|---|---|
| ① | 클립 2개가 `Assets/Game/Audio/` 에 있고 `AudioLibrary` 에 물렸다 | ✅ `GetSfx(4)`→`SFX_WeaponSwing` · `GetSfx(5)`→`SFX_ToxinSpill` |
| ② | Swing **0.30/0.12** · Toxin **0.38/0.10** | ✅ |
| ③ | `WeaponSwing=4` · `ToxinSpill=5` · 기존 번호 무변경 | ✅ |
| ④ | `MeleeWeapon` 에 `WeaponFire` 가, `FieldWeapon` 에 `WeaponCast` 가 없다 | ✅ + **실플레이 대조 Fire=0 / Cast=0** |
| ⑤ | 컴파일 에러 0 | ✅ |
| ⑥ | 검 = 총소리가 아닌 "쉭" | 🟡 지정 클립 재생까지 확인. **청취 필요** |
| ⑦ | 장판 = 마법음이 아닌 "철퍽 + 치익" | 🟡 〃 |
| ⑧ | Lv5 3연타가 뭉개지지 않는다 | 🟡 간격 실측 **0.249 · 0.254s** · 겹침 **31ms**. 뭉개짐은 청취 |
| ⑨ | 둘을 같이 들었을 때 구분된다 | 🟡 청취 |
| ⑩ | 원거리·폭탄 소리는 그대로 | ✅ `ProjectileWeapon:26` · `AoeWeapon:28` · `SummonWeapon:159,174` 무변경 |

**볼륨 실측이 계산과 맞다** — Swing `0.30 × 0.31(sfx_vol) = 0.094` · Toxin `0.38 × 0.31 = 0.119`.
🟡 4건은 **못 들어서가 아니라 기계가 판정할 수 없어서** 남는다 (이 기계의 `sfx_vol` 은
D8 때 `0.00` 이었는데 지금은 `0.31` 이라 사람은 들을 수 있다).

프로브 스크립트와 씬 오브젝트는 **삭제했다.** 삭제 후 Refresh → 콘솔 **0건**,
`git status` 에 씬 변경 없음.

### 🔴 알게 된 것 — 검 클립을 **늘릴 수 없다**

| 값 | 실측 |
|---|---|
| `SFX_WeaponSwing` 길이 | **0.280s** |
| Lv5 연타 간격 (`hitDelay 0.067` + `comboInterval 0.18`) | **0.247s** (프로브 실측 0.249·0.254) |
| 겹침 | **0.031s** |

CONTENT 가 알고 만들었다 — "3연타 합성 피크 0.400 = 1타와 동일"이라 미리 쟀고
겹치는 31ms 는 페이드아웃 구간이다. **지금은 문제가 아니다.**

문제는 **선택지 쪽**이다. 검이 "탁"으로 들려 `DUR` 을 0.28 → 0.34 로 늘리면
겹침이 **31ms → 93ms** 로 커져 본체끼리 겹친다. 그때는 클립이 아니라
`MeleeWeapon.comboInterval`(0.18)을 같이 올려야 하는데 **연타의 손맛이 바뀐다.**
→ [`REQ/CONTENT.md` 요청-8](Parallel/REQ/CONTENT.md) 에 이 조건을 적어 보냈다.

같이 계산해 보낸 것: `EnemyHit` 실제 출력 **0.087** vs 검 **0.094** — 차이가 **1.08배**뿐인데
휘두름 1회에 부채꼴 안의 적이 전부 맞아 타격음이 여러 번 겹치고 Lv5 는 3연타다.
**검이 묻힐 가능성이 높다** → 예비값 0.35 가 맞을 공산이 크다(판정은 청취).

### 정리

빌려 쓰던 소리 **2개가 줄었다.** 남은 건 소환수 2종 —
`SummonWeapon:159` 화염구=`WeaponCast` · `:174` 촉수=`WeaponFire`.
D13 이 끝나 이제 그 파일을 건드릴 수 있어 클립 요청을 요청-8 에 같이 넣었다.

---

## 2-40. ✅ 소환수 2종(드래곤·문어) — 판정 기준점이 처음으로 플레이어가 아니다 (D13 / C16, 2026-08-30 36차)

### 왜 했나

지금까지의 무기는 **전부 플레이어를 기준점**으로 적을 찾고 때렸다. 소환수는 처음으로
**저 혼자 떨어져 서서, 저 자리에서 찾고, 저 자리에서 때리는** 무기다.
`DESIGN_CLASSES.md` §6 **5단계** — 그림 5장은 CONTENT(C13·C14·C16)가 이미 다 그려 놨고
**코드·프리팹만 비어 있었다.**

🔴 **CSV 는 이번에 안 넣었다.** 요청-11 이 못 박은 대로 — 프리팹 경로가 없으면 CONTENT 가
`Weapons.csv` 를 쓸 수 없다. **이 작업의 산출물이 곧 그 경로**이고, CSV 는 CONTENT 가 채운다.

**기존 파일은 한 줄도 안 고쳤다.** 전부 신규다.

### 무엇을 했나

| 순 | 한 일 |
|---|---|
| ① | 스프라이트 **5장** 임포트 (`git mv` + 임포터 코드 실행) |
| ② | `SummonWeapon.cs` · `SummonVisual.cs` **신설** |
| ③ | 프리팹 **5개** 신설 |

**변경한 파일**

| 파일 | 무엇 |
|---|---|
| `Assets/Game/Sprites/Summons/Dragon_Fly.png` 🆕 | PPU **512** · Multiple **4×4** → `Dragon_Fly_0`~`_15` |
| `Assets/Game/Sprites/Summons/Octopus_Idle.png` 🆕 | PPU **512** · Multiple **4×4** → 16칸 |
| `Assets/Game/Sprites/Effects/TentacleLash.png` 🆕 | PPU 100 · Multiple **6×1** · pivot Center |
| `Assets/Game/Sprites/Weapons/{Dragon,Octopus}.png` 🆕 | 아이콘 · PPU **512** · Single |
| `Assets/Scripts/Weapon/SummonWeapon.cs` 🆕 | `WeaponBase` 상속. 몸통을 낳고·따라다니게 하고·**몸통 자리에서** 싸운다 |
| `Assets/Scripts/Weapon/SummonVisual.cs` 🆕 | 16프레임 루프 재생기 + `flipX` |
| `Assets/Prefabs/Summon_{Dragon,Octopus}.prefab` 🆕 | 몸통. `frameRate 12` · **Collider·Rigidbody 없음** |
| `Assets/Prefabs/Weapon_SummonDragon.prefab` 🆕 | `mode Ranged` · `offsetAngle 90` · `offsetDistance 1.2` · `followLerp 6` |
| `Assets/Prefabs/Weapon_SummonOctopus.prefab` 🆕 | `mode Ring` · `offsetAngle **210**` · `lashHitDelay 0.1` |
| `Assets/Prefabs/Fx_TentacleLash.prefab` 🆕 | `SwingArcFx` 재사용 · `frameRate 30` · `spriteRadiusAtScaleOne` **0.9951** |

> ℹ️ 요청서 경로 `Assets/Scripts/Weapons/` · `Assets/Scripts/Visual/` 는 **존재하지 않는다.**
> 이 저장소는 `Assets/Scripts/Weapon/`(단수)이고 `*Visual.cs` 는 담당 폴더에 흩어져 있다. 둘 다 `Weapon/` 에 넣었다.

### 어떻게 만들었나 — 위험했던 지점 3개

**(가) 🔴 파생 클래스에 `Update()` 를 선언하면 무기가 조용히 죽는다.**
`WeaponBase` 의 `private void Update()` 가 쿨다운을 돌린다. 같은 이름을 선언하면 그걸 **가려
모든 소환수가 영영 공격하지 않는다.** 위치 보정은 `LateUpdate()` 에 뒀다 —
플레이어가 움직인 **뒤에** 따라가야 맞기도 하다.

**(나) 🔑 철거는 `OnDisable()` 하나로 끝난다.**
`WeaponManager.RemoveWeapon` → `ObjectPool.Return` → `SetActive(false)` 가 곧 `OnDisable` 이다.
**`WeaponBase` 에 새 훅을 팔 필요가 없었다.** 이게 없었으면 상점에서 환불한 뒤
**소환수만 필드에 영원히 남는다.** 씬을 내릴 때도 불리므로 `if (Pool != null)` 로 감쌌다.

**(다) 몸통에 Collider2D·Rigidbody2D 를 안 넣었다.**
적이 아니라 맞지도 막지도 않고, **강체가 없어야 넉백에 휩쓸려 날아가지 않는다.**
따라오기는 프레임률에 안 흔들리는 감쇠 `k = 1 - exp(-followLerp·dt)` 다 —
`Lerp(t = lerp·dt)` 로 쓰면 프레임이 튈 때 따라오는 속도가 같이 변한다.

**`SummonVisual` 이 일부러 안 한 것** — 위아래 흔들림(그림에 이미 ±1.6px 그려져 있다 · 코드가 또
흔들면 두 번 흔들린다) · ping-pong(16프레임이 `sin(2π·i/16)` 이라 15→0 이 이음매 없이 붙는다) ·
`EnemyVisual` 재사용(그쪽은 `_rb.linearVelocity` 로 프레임을 넘기는데 소환수엔 강체가 없다).
대신 `OnEnable()` 에서 **시작 프레임을 무작위로 흩뜨린다** — 여러 마리가 같은 박자로
날갯짓하면 복제처럼 보인다.

### 검증 로그 — 판정 12개 전부 PASS

| # | 기준 | 결과 |
|---|---|---|
| ① | 시트 2장 16칸 · 촉수 6칸 | ✅ `16 / 16 / 6` |
| ② | PPU 512·512·100·512·512 | ✅ |
| ③ | 🔴 오우거의 **약 65%** | ✅ 드래곤 **64.7%** · 문어 **66.7%** (오우거 204px / 132·136px) |
| ④ | 따라오고 멈추면 곁에 선다 | ✅ 8초 원운동 중 거리 **0.42~1.99** · 정지 시 정확히 **1.20** · `flipX` 전환 5회 |
| ⑤ | 🔴 적이 통과한다 | ✅ `col=False` |
| ⑥ | 🔴 넉백에 안 날아간다 | ✅ `rb=False` |
| ⑦ | 드래곤이 **자기 위치에서** 쏜다 | ✅ 화염구 생성점 — 몸통 **0.03** / 플레이어 **0.39** |
| ⑧ | 문어가 **등 뒤 적도** 때린다 | ✅ 후리기 14회 · 피격 44건 · 각도 최대 **171°** (90 초과 9건) |
| ⑨ | 촉수 바깥 끝 = 판정 반경 | ✅ `scale 2.512 × 0.9951 = 2.50` = 사거리 2.50 |
| ⑩ | 두 마리가 안 겹친다 | ✅ 간격 **2.08** (계산값 `2 × 1.2 × sin60° = 2.078`) |
| ⑪ | 🔴 환불하면 몸통도 사라진다 | ✅ 드래곤 몸통 **0** · 문어는 **1** 로 남음 |
| ⑫ | 재획득이 레벨업이지 두 마리가 아니다 | ✅ 몸통 **1** 유지 · 콘솔 **0건** |

캡처 2장 — 촉수가 반경 2.5 를 한 바퀴 훑으며 **사방에** 피해 팝업(23/24/25)이 뜬 장면,
그리고 FX 를 끄고 찍은 문어 몸통(`Octopus_Idle_10` · `order=0`).

### 🔴 프로브를 세 번 고쳐야 했다 — 세 번 다 "검증 장치가 틀린 것"

| 증상 | 원인 | 고친 방법 |
|---|---|---|
| ④ 가 항상 `dist=1.20` 으로만 찍힘 | **플레이어가 안 움직인다.** 정지한 대상으로는 "따라온다"를 증명할 수 없다 | `rb.MovePosition` 으로 반지름 3 원운동을 직접 먹였다 |
| ⑧ 이 **첫 후리기 한 번만** 잡힘 | 🔴 **풀에서 나온 오브젝트는 `GetInstanceID()` 가 돌아온다.** "새 것"을 ID 로 가리면 두 번째부터 영영 안 잡힌다 | "지금 후리는 중인가"의 **상승 에지**로 바꿨다 |
| 시퀀스가 ⑩ 에서 통째로 멈춤 | `Time.timeScale = 0` — 레벨업 패널(교훈 158)과 **ESC 일시정지**가 코루틴을 얼린다 | 프로브가 `HidePanel()` · `PauseMenuUI.Close()` 로 스스로 풀게 했다 |

판정 ③ 은 `sprite.bounds` 로 잴 수 없었다 — 소환수도 오우거도 똑같이 `0.500` 이다.
**`bounds` 는 셀 사각형 기준**이라 그림이 셀 안 어디에 얼마나 그려졌는지를 모른다(교훈 138 재발).
알파 bbox 를 PIL 로 직접 재서 비교했다.

피해 팝업을 셀 때 **플레이어가 맞아서 뜬 팝업을 걸러냈다** — D12 에서 이미 당한 함정이다
(교훈 161). 문어는 플레이어에게서 1.2 유닛이라 **링 안에 플레이어가 들어온다.**

### 정리

임시 프로브 `Assets/Scripts/D13Probe.cs` **삭제** · 씬의 `D13Probe` 오브젝트 **삭제 후 씬 저장** ·
Refresh 후 콘솔 **0건**. 상세 → [`Parallel/DONE/D13.md`](Parallel/DONE/D13.md)

CONTENT 에게 [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-7** 로 넘겼다 —
`Weapons.csv` 에 쓸 **프리팹 경로 5쌍**(열 추가 없음, 12열 그대로) · 크기 판정 통과 통보 ·
**소환수 전용 SFX 부재**(지금은 `WeaponCast`/`WeaponFire` 를 빌려 쓴다) · 체감 미판정 4건.

---

## 2-39. ✅ CSV Import 1회로 수리검·검근접·독장판·바닥폭탄이 게임에 나왔다 (D12 / C12·C15, 2026-08-30 35차)

### 왜 했나

D10(관통·근접)과 D11(신관·장판)은 **코드·프리팹·그림만** 만들어 놓은 상태였다.
`Assets/Game/Balance/*.csv` 에 줄이 없으면 **게임에 존재하지 않는다** — 레벨업 3택에도 상점에도 안 나온다.
CONTENT 가 요청-9(C12)·요청-10(C15)으로 그 줄을 채웠는데, **둘 다 같은 CSV 3장**
(`Weapons` · `Items` · `SceneWiring`)을 건드렸다.

🔴 **그래서 Import 는 1회다.** 따로 처리할 수 없다 — 먼저 도는 Import 가 나중 요청의 줄까지
이미 반영해 버린다. 판정만 두 벌로 나눠 돌렸다.

**C# 수정은 0줄이다.** 이번 작업은 전부 Import · 배선 · 검증이다.

### 변경한 것

| 경로 | 무엇 |
|---|---|
| `Assets/Game/WeaponData/Shuriken.asset` (+meta) | **신규** — 관통 3 |
| `Assets/Game/ItemData/Shuriken.asset` (+meta) | **신규** |
| `Assets/Game/WeaponData/Toxin.asset` (+meta) | **신규** — 독 장판 |
| `Assets/Game/ItemData/Toxin.asset` (+meta) | **신규** |
| `Assets/Game/WeaponData/Sword.asset` | 수정 — **근접 재해석** (`Weapon_Melee` · speed 0 · `Range[0]`=2) |
| `Assets/Game/ItemData/Sword.asset` | 수정 — `Description` |
| `Assets/Game/WeaponData/Bomb.asset` | 수정 — `TravelPrefab` = `Proj_BombGround` (**신관 켜짐**) |
| `Assets/Scenes/SampleScene.unity` | `SceneWiring` 11/11 배선 저장 |

🔴 **CSV 는 DEV 가 안 건드렸다** (요청서 규칙). CONTENT 가 자기 커밋(`24b61ce` · `b457dcf`)으로
이미 올려 놨으므로 이 커밋에는 CSV 가 없다 — **산출물(SO)과 씬과 문서뿐**이다.

### 검증 로그

```
Weapons: 10 · Items: 25 · SceneWiring.csv : 11/11 적용        (에러 0)
```

**요청-9 (C12) 판정 ①~⑧ 전부 PASS**

| # | 기준 | 결과 |
|---|---|---|
| ① | Import 완료 · 에러 0 | ✅ 위 로그 |
| ② | `WeaponData/Shuriken` · `ItemData/Shuriken` 생성 | ✅ |
| ③ | Shuriken `ProjectilePrefab` = `Proj_Shuriken` (guid ≠ 0) | ✅ guid `2b839dd3…` |
| ④ | Sword `WeaponPrefab`=`Weapon_Melee` · `ProjectilePrefab`=`Fx_SwingArc` · speed 0 · `Range[0]`=2 | ✅ 4개 전부 |
| ⑤ | 레벨업 3택 / 상점에 `Shuriken` | ✅ **60회 추첨(180장)** 에서 4·6·8·11장 (4회 반복, 매번) |
| ⑥ | 검이 **총알 없이** 호로 벤다 | ✅ `SWORD proj=0 arc=13 hits=19 kills=14 hitsPerSec=1.4 minDist=0.61` |
| ⑦ | 수리검이 **여러 마리를 같이** 닳게 한다 | ✅ `hitsPerProj` 흩어짐 **0.91** vs 뭉침 **3.10** (프리팹 `pierceCount: 3` 과 일치, 3.4배) |
| ⑧ | `Weapons.csv` 12열 · `Items.csv` 8열 | ✅ |

**요청-10 (C15) 판정 ①~⑧ 전부 PASS** — ①~⑤⑧ 은 같은 Import 로 닫혔고, ⑥⑦ 은 게임 카메라 캡처다.

| # | 기준 | 결과 |
|---|---|---|
| ⑥ | 폭탄이 날아가 떨어지고 **달아오른 뒤** 터진다 | ✅ 캡처 2장 — 비행 중(불붙은 심지) / 착탄 후 **벌겋게** |
| ⑦ | **초록 웅덩이**가 깔리고 안의 적이 느려진다 | ✅ 캡처 1장 — 장판 **2개 동시** · 안쪽 적에 9/11/9/6 누적 |

장판 2개 동시는 CONTENT 예고 그대로다 — Lv5 쿨 1.8 < 지속 4.0.

### 🔴 `hitsPerProj` 를 처음엔 5.00 으로 잘못 쟀다

프리팹 상한이 관통 3인데 5가 나올 수 없다. **숫자가 아니라 계측이 틀린 것**이었고,
원인은 `PlayerStats.TakeDamage` 도 **피해 팝업을 띄운다**는 것이었다(`PlayerStats.cs:258`).
적 18마리가 붙어 있으면 **플레이어가 맞는 팝업**이 "적 명중"으로 섞인다.
플레이어로부터 2.5유닛 밖의 팝업만 세도록 고쳐 **3.10** 이 나왔다 → 교훈 161.

### 실플레이에서 나온 답 (CONTENT 회신)

- **검은 약하지 않다 — 다만 자주 논다.** Lv5 로 14초에 14킬(수리검 16킬과 동급).
  그런데 가만히 선 플레이어에겐 가장 가까운 적이 **4.67유닛**에 머물러 **6초 동안 0회** 휘둘렀다.
  카이팅 플레이에서 **사거리 2.50 이 충분한지**가 진짜 질문 → `TUNING.md`
- **수리검은 떼거리에서 확실히 세다.** Lv5 상한 ≈ **276 DPS** (4발/s × 관통 3 × 23딜)
- **Lv5 장판은 화면을 안 덮는다.** 하나가 화면 세로 1/4 — `ProjectileSize` 아직 안 줄여도 된다
- 🔴 **적을 한 번도 안 만나는 장판이 실제로 있었다.** 캡처의 웅덩이 2개 중 하나는 안이 **0마리**.
  `FieldWeapon` 은 조준하지 않으므로 **구조적 헛방**이다 → `TUNING.md`
- **검이 아직 총소리를 쓴다** (`MeleeWeapon.cs:52` `SfxId.WeaponFire`) → [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-6

### 정리

임시 프로브 `Assets/Scripts/D12Probe.cs`(3벌) **삭제** · 씬의 `D12Probe` 오브젝트 **삭제 후 씬 저장** ·
Refresh 후 콘솔 **0건**. 상세 → [`Parallel/DONE/D12.md`](Parallel/DONE/D12.md)

---

## 2-38. ✅ 바닥 폭탄(신관) + 독 장판(슬로우) (D11 / C9, 2026-08-30 34차)

### 왜 했나

`DESIGN_CLASSES.md` §6 **3·4단계** — D10 이 *공격 방식*을 갈랐다면 이번엔 **시간축**을 연다.

- **바닥 폭탄** — 모든 폭발이 *닿는 즉시* 터졌다. **기다렸다 터지는 것**이 하나도 없어서
  "저기 폭탄이 떨어졌으니 피해야겠다"는 판단이 게임에 존재하지 않았다
- **독 장판** — 이 게임에 **지속 피해도 슬로우도 개념 자체가 없었다.**
  적을 느리게 만들 수단이 전무해서, 몰려오면 도망만 답이었다

### 무엇을 했나

| 순 | 한 일 |
|---|---|
| ① | `BombProjectile` 에 **신관**(`fuseTime` + `fuseFrames`) 추가 |
| ② | `EnemyBase` 에 **슬로우**(`ApplySlow` / `CurrentSpeed`) 추가 |
| ③ | `ToxinField.cs` · `FieldWeapon.cs` **신설** |
| ④ | 스프라이트 3장 임포트 + 프리팹 3개 신설 |

**변경한 파일**

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Building/BombProjectile.cs` | 착탄 후 `fuseTime` 대기 + 신관 그림을 **시간 게이지**로 훑음 |
| `Assets/Scripts/Enemy/EnemyBase.cs` | `_slowMult`/`_slowUntil` + `CurrentSpeed`. 이동 3곳 교체 |
| `Assets/Scripts/Weapon/ToxinField.cs` 🆕 | 장판 본체. 콜라이더 없이 매 틱 `OverlapCircleAll` |
| `Assets/Scripts/Weapon/FieldWeapon.cs` 🆕 | **조준하지 않는** 무기. 주변 무작위 좌표에 깐다 |
| `Assets/Game/Sprites/Effects/BombGround.png` 🆕 | 1024×256 · PPU **256** · Multiple **4칸** |
| `Assets/Game/Sprites/Effects/ToxinField.png` 🆕 | 1536×256 · PPU **100** · Multiple **6칸** · **Custom pivot** |
| `Assets/Game/Sprites/Weapons/Toxin.png` 🆕 | 1024² 아이콘 · PPU **512** |
| `Assets/Prefabs/Proj_BombGround.prefab` 🆕 | `Proj_Bomb` 복제 · scale 0.5 · `fuseTime 1.1` |
| `Assets/Prefabs/Proj_ToxinField.prefab` 🆕 | SpriteRenderer(order **−5**) + `ToxinField`. **콜라이더 없음** |
| `Assets/Prefabs/Weapon_Field.prefab` 🆕 | `FieldWeapon` (`fieldRadius 1.2`) |

### 어떻게 고쳤나 — 위험했던 지점 3개

**(가) 슬로우는 `MoveSpeed` 를 덮어쓰지 않는다.** 원본은 그대로 두고 **읽는 쪽에서 곱한다**
(`CurrentSpeed => MoveSpeed * ...`). 덮어썼다면 "원래 값"을 어딘가 보관해야 하고,
그 보관값이 풀 재사용을 건너뛰는 순간 적이 **영구히 느려진다.**

**(나) Trigger Enter/Exit 을 쓰지 않는다.** 장판이 **매 틱 짧게 다시 걸고**, 안 걸어 주면
`_slowUntil` 이 지나 저절로 풀린다. Trigger 였다면 적이 장판 안에서 **죽거나 풀로 반납될 때
Exit 가 안 와서** 슬로우가 붙은 채 재사용된다 — 이 게임은 적이 풀에서 도는 게 정상 경로다.

**(다) 슬로우 겹침은 곱하지 않는다.** `if (Time.time > _slowUntil || mult < _slowMult)` —
"가장 센 것 하나만". 곱했다면 장판 2개가 겹칠 때 `0.6 × 0.6 = 0.36` 으로 **적이 멈춘다.**

> 🔴 **곡사포는 안 건드렸다.** `fuseTime = 0f` 는 C# 초기값이라 기존 `Proj_Bomb.prefab` 에
> 직렬화되어 있지 않다. 미직렬화 필드는 초기값이 먹으므로 프리팹 파일이 그대로여도 즉폭이다.
> D10 의 `pierceCount = 1` 과 같은 수법 — 그래도 대조군으로 확인했다(판정 ②).

> 🔴 **풀 재사용 리셋은 `Setup()` 이 아니라 `Initialize()` 다.** 요청서가 메서드를 잘못 짚었다.
> 실제 재사용 리셋 블록(`_knockbackTimer` · 사망 팝 코루틴 · 콜라이더)이 `Initialize()` 안에 있다.

### 검증 로그

| # | 기준 | 결과 |
|---|---|---|
| ① | 4장/6장 분할, `ToxinField bounds ≈ (2.56, 2.56)` | ✅ `(2.56, 2.56)` · `BombGround (1.00, 1.00)` · pivot `(0.5234, 0.4844)` |
| ② | 🔴 **곡사포 회귀** — 예전처럼 즉폭 | ✅ `BOMB-PLAIN flight=0.53s fuseTotal=0.00s` |
| ③ | 🔴 **적 속도 회귀 + 풀 재사용 초기화** | ✅ 아래 |
| ④ | 폭탄이 멈추고 · 안 돌고 · 달아오른다 | ✅ 아래 |
| ⑤ | 장판 안에서 느려지고 나오면 돌아온다 | ✅ `FIELDx1 in(avg=0.840) out(avg=1.400) ratio=0.600` |
| ⑥ | 🔴 **겹친 장판 2개가 적을 멈추지 않는다** | ✅ 아래 |
| ⑦ | 도트로 적이 밀려나지 않는다 | ✅ 장판 안 최대 \|v\| 가 기본 속도(2.200)를 **한 번도 안 넘겼다** |
| ⑧ | 🔴 **그림 반지름 = 맞는 반지름** | ✅ **봤고, 쟀다.** 아래 |
| ⑨ | 레벨업 3택·상점에 `Toxin` | ⏸ **CSV 대기** ([`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-5) |
| ⑩ | 콘솔 에러 0 | ✅ `Error`·`Warning` **0건** |

**③ — 슬로우가 남의 속도를 갉아먹지 않았나**

```
[D11] REUSE   normal=2.200  slowed=0.660(0.300x)  reinit=2.200(1.000x)
[D11] SPEED-BEFORE  avg=1.493  max=6.000
[D11] SPEED-AFTER   avg=1.885  max=6.000
```

`0.300x` 는 건 배율 그대로다. `reinit=1.000x` — 30초짜리 슬로우가 걸린 채
`Initialize()` 를 다시 부르자 **즉시 원속**. 이게 곧 풀에서 다시 꺼내는 경로다.
장판을 다 쓰고 난 뒤 **`max` 가 6.000 으로 동일**한 것이 회귀 없음의 증거다.

**④ — "달아오른다"를 픽셀로 쟀다**

`flight=0.53s` 는 `PLAIN` 과 **완전히 같고**, 착탄 뒤 `fuseTotal=1.10s` 가 붙었다.
프레임은 fuse `0.00 / 0.28 / 0.55 / 0.83` 에 정확히 4등분. 착탄 후 `rotZ` 는 계속 **`0.0`**.
캡처를 PIL 로 읽은 프레임별 평균 R: **51 → 96 → 162 → 227**, 붉은 픽셀 수 **528 → 720 → 16208 → 20544**.

> ⚠️ 첫 줄만 `rotZ=284.2` 로 찍혔는데 **프로브 아티팩트**다. 착탄 판정 허용치가
> `0.02유닛` 이라 **실제 착탄 한 프레임 전**에 먼저 걸린다. 증상을 코드 버그로 오인하기 쉽다.

**⑥ — 0.36 이 나왔는지 어떻게 갈랐나**

장판 2개를 같은 자리에 겹쳐 깔았다. 관측된 감속 속도는 **오직 두 값**뿐이다 —
`0.840 (=1.400×0.6)` 과 `1.320 (=2.200×0.6)`. 곱셈 누적이었다면 `0.504`·`0.792` 가
섞여 나왔어야 하는데 **한 번도 안 나왔다.**

**⑧ — 봤고, 쟀다**

`Unity_Camera_Capture(cameraInstanceID: -1602)` 로 **게임 카메라**를 찍었다 (ortho 6 → **90 px/유닛**).
눈으로: 초록 장판이 플레이어·적 **아래**에 깔리고 그 위로 피해 숫자(4·10·5·10)가 **틱마다** 뜬다.

```
scale 1.237 (=1.2/0.97) · 렌더러 bounds (3.17, 3.17)
기대 중심 (935.7, 520.6)  ↔  실측 중심 (935.0, 520.0)   →  🔴 오차 0.7px
기대 반지름 108.0px  ↔  코어 rx 104.5 / 알파 끝 rx 109.1   → 사이에 정확히 들어간다
```

**🔴 요청서의 장판 규격 2개가 그림과 달라 실측으로 고쳤다 (사용자 승인).**
요청서는 *"Pivot Center"* 와 *"`spriteRadiusAtScaleOne = 1.0` 고치지 말 것"* 을 못 박았는데,
셀 알파를 재 보니 원의 중심이 셀 중앙(128,128)이 아니라 **(134, 132)px**, 반지름이 128 이 아니라
**≈97px** 이었다. → pivot `(0.5234375, 0.484375)` · `spriteRadiusAtScaleOne = 0.97`.
보정이 먹은 증거가 위의 **0.7px** 다 — 보정이 없었다면 x 로만 약 **6.7px** 어긋났어야 한다(10배).

### 남긴 것

🔴 **CSV 가 안 들어오면 게임에 아무것도 안 나온다** — `Toxin` 줄 + `Bomb` 줄의
`TravelPrefab`·`ProjectileSpeed` + `Items` + `SceneWiring`.
→ [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-5**. 판정 ⑨ 는 그때 닫힌다.
체감값 7개(`fuseTime` 1.1 · `fieldDuration` 4.0 · `tickInterval` 0.5 · `slowMult` 0.6 등)도 거기에.
상세는 [`Parallel/DONE/D11.md`](Parallel/DONE/D11.md).

임시 프로브 `D11Probe` 스크립트·씬 오브젝트 **삭제 후 씬 저장** · Refresh 후 **콘솔 0건**.

---

## 2-37. ✅ 수리검(관통) + 검 근접화 + 휘두름 호 (D10 / C8, 2026-08-30 33차)

### 왜 했나

`DESIGN_CLASSES.md` §6 **2단계** — 무기의 성격을 나누는 첫 걸음이다.
지금까지 모든 무기가 *"제일 가까운 적에게 발사체 하나"* 뿐이라 검이든 활이든 손맛이 같았다.

- **수리검** — 이 게임에 **관통이라는 개념 자체가 없었다.** 적이 뭉칠수록 무기가 약해졌다
- **검** — 이름이 검인데 **`Proj_Bullet.prefab` 총알을 쏘고 있었다.**
  D9 이 활에서 총알을 걷어냈으니 이번엔 검인데, 검은 그림 교체로 안 되고
  **부채꼴을 직접 베는 새 코드**가 필요하다

### 무엇을 했나

| 순 | 한 일 |
|---|---|
| ① | `ProjectileBase` 에 **관통**(`pierceCount`) + **자전**(`spinSpeed`) 추가 — 기존 파일 유일한 수정 |
| ② | `MeleeWeapon.cs` · `SwingArcFx.cs` **신설** |
| ③ | 스프라이트 3장 임포트 + 프리팹 3개 신설 |

**변경한 파일**

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Weapon/ProjectileBase.cs` | 관통 + 자전. 이동을 **로컬 → 월드**로 |
| `Assets/Scripts/Weapon/MeleeWeapon.cs` 🆕 | 발사체 없이 부채꼴을 벤다 |
| `Assets/Scripts/Weapon/SwingArcFx.cs` 🆕 | 호 6프레임 재생 후 풀 반납. **피해 없음** |
| `Assets/Game/Sprites/Projectiles/Shuriken.png` 🆕 | 256² · PPU **512** · guid `5dd881a3…` |
| `Assets/Game/Sprites/Weapons/Shuriken.png` 🆕 | 1024² 아이콘 · guid `e9a415f7…` |
| `Assets/Game/Sprites/Effects/SwingArc.png` 🆕 | 1536×256 · PPU **100** · Multiple **6칸** · guid `c1f8508d…` |
| `Assets/Prefabs/Proj_Shuriken.prefab` 🆕 | `Proj_Bullet` 복제 + 3줄 · guid `2b839dd3…` |
| `Assets/Prefabs/Fx_SwingArc.prefab` 🆕 | guid `a977bc5a…` |
| `Assets/Prefabs/Weapon_Melee.prefab` 🆕 | guid `422b92f4…` |

### 🔴 자전을 넣으면 발사체가 나선을 그린다

원래 이동은 `transform.Translate(Vector2.right * …)` — **`Space.Self`** 다.
"진행 방향 = 현재 회전"이라는 뜻이라, 매 프레임 z 를 돌리면 진행 방향도 같이 돌아 **원을 그린다.**
방향을 회전에서 떼어냈다:

```csharp
transform.position += (Vector3)(_direction * (Speed * Time.deltaTime));   // 월드
```

**기존 발사체에게는 수식이 완전히 같다** — `Initialize` 가 회전을 `dir` 로 한 번 굳히고
그 뒤 아무도 안 건드리므로 `TransformDirection(right) == _direction` 이 항상 성립한다.
(`Translate(Space.Self)` 는 **월드 회전**을 쓰고 **스케일을 무시**한다.)
그래도 믿지 않고 **대조군 회귀 검사**를 돌렸다 — 아래.

### 🔴 관통은 카운터만으로는 관통이 아니다

`_pierceLeft--` 만 두면 **한 마리의 콜라이더를 세 번 스치는 것**으로 관통 3이 소모된다.
맞힌 적을 `HashSet<EnemyBase>` 에 기억하고 `Initialize()`(= 풀에서 꺼내는 지점)에서 비운다.

### 🔴 호의 실제 반지름은 128px 도 100px 도 아닌 **107.4px** 이었다

코드를 쓰기 **전에** 프레임마다 셀 중심에서의 최대 알파 거리를 쟀다.
`셀 256px · 바깥 반지름 107.4px · PPU 100` → **scale 1 일 때 1.074 유닛.**
`localScale = Range` 로 뒀으면 **호가 사거리를 7% 부풀려 보여 준다.**
`AoeProjectile` 의 나눗셈 패턴을 그대로 썼다 — `localScale = radius / 1.074`.

### `WeaponData` 에 새 열을 만들지 않았다

`MeleeWeapon` 은 기존 열을 **재해석**한다:

| 열 | 원거리 | 근접 |
|---|---|---|
| `Range` | 비행 거리 (10~15) | **호의 바깥 반지름 (≈2)** |
| `ProjectileCount` | 동시 발사 수 | **연타 횟수** |
| `ProjectilePrefab` | 발사체 | **휘두름 이펙트** |
| `ProjectileSpeed` | 속도 | 안 씀 (0) |

피해는 **휘두름 1회에 정확히 1번**이다. `SwingArcFx` 는 그림만 그리고,
판정은 `MeleeWeapon` 이 `hitDelay` 뒤에 한 번만 굴린다. 양쪽에서 때리면 연타와 곱해진다.

### 검증 로그

| # | 기준 | 결과 |
|---|---|---|
| ① | `SwingArc` 6장, 프레임 `bounds.size ≈ (2.56, 2.56)` | ✅ 6장, `(2.56, 2.56)` |
| ② | `Shuriken` `bounds.size ≈ (0.5, 0.5)` | ✅ `(0.50, 0.50)` · PPU 512 |
| ③ | 🔴 **관통 회귀** — 기존 무기는 첫 적에서 사라진다 | ✅ 아래 |
| ④ | 관통 동작 — 3마리를 뚫는다 (1마리 3번 아님) | ✅ 아래 |
| ⑤ | 레벨업 3택·상점에 `Shuriken` | ⏸ **CSV 대기** ([`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-4) |
| ⑥ | 검 — 화면 반대편 적이 안 맞는다 | ✅ `D10_Back`(−1.8, 사거리 안) 속도 `(0, 0)` |
| ⑦ | 🔴 호의 중심 = 플레이어, 바깥 끝 = 실제 사거리 | ✅ **눈으로 봤다.** 아래 |
| ⑧ | 맞은 적이 플레이어 반대로 밀린다 | ✅ `D10_Front` 속도 `(6.00, 0.00)` = +X |
| ⑨ | 콘솔 에러 0 | ✅ Log 3건, Error·Warning **0** |

**③④ — 일렬 3마리에 대조군을 같이 쐈다**

| 발사체 | 소멸 지점 | 어느 적 앞 |
|---|---|---|
| `Proj_Bullet` (대조군) | `(7.37, −1.92)` | **1번째** 적 0.63 앞 |
| `Proj_Shuriken` | `(11.37, 1.08)` | **3번째** 적 0.63 앞 |

HP 는 `protected` 라 못 읽는다. 대신 **소멸 위치**로 갈랐다 — 한 마리를 3번 때렸다면
**첫 적 앞**에서 멈췄어야 한다. 소멸 시 `rotZ = 139.4°`, 즉 **자전 중인데도 직선**이었다.
`Proj_Bullet`/`Proj_Arrow` 프리팹 필드도 `pierce=1 spin=0` 이고 **`.prefab` 파일은 미변경** —
새 필드의 C# 기본값이 그대로 먹었다.

> 🔑 `0.63` 은 D9 의 총알·화살에서도 나온 숫자다. CircleCollider2D r=0.5 + 고블린 캡슐의 상수.

**⑦ — 0.2초짜리 애니메이션을 한 장으로 찍었다**

6프레임 전부를 플레이어 중심에 **겹쳐 세워** 부채꼴 전체를 한 장으로 만들고,
반지름 **정확히 2.0** 위에 빨간 점 13개(+90°~−90°), 플레이어 자리에 초록 점을 찍은 뒤
`Unity_Camera_Capture(cameraInstanceID: -1602)` 로 **게임 카메라**를 찍었다.

화면에서: 오목한 안쪽이 초록 점을 감쌌고, 초록 점을 지나는 수평선에 대칭이며,
바깥 흰 테두리가 빨간 점들 위에 정확히 얹혔다. 부채꼴은 오른쪽 반쪽에만 있다.
산술로도 `1.074 × 1.862 = 2.000`.

### 남긴 것

🔴 **CSV 가 안 들어오면 게임에 아무것도 안 나온다** — 수리검 3줄 + 검 재해석.
→ [`REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) **요청-4**. 판정 ⑤ 는 그때 닫힌다.
상세는 [`Parallel/DONE/D10.md`](Parallel/DONE/D10.md).

`D10_*` 임시 오브젝트 22개 전부 삭제 · 플레이 종료 · 컴파일 에러 0.

---

## 2-36. ✅ 활이 총알 대신 화살을 쏜다 (D9 / C7, 2026-08-30 32차)

### 왜 했나

`BowData.ProjectilePrefab` 이 `Proj_Bullet.prefab` 이었다. 그게 그리는 그림은
`Assets/Game/ICON/Bullet.png` — **icons8 벡터 아이콘**이다. 픽셀아트인 이 게임과 결이
다른 것도 문제지만, 무엇보다 **활에서 총알이 나갔다.**

`DESIGN_CLASSES.md` §6 항목 중 **코드가 0줄**인 유일한 건이라 먼저 잡았다.
그림은 CONTENT(C7)가 `Bow.png` 팔레트를 직접 뽑아 그려 `_Incoming/` 에 넣어 두었다.

### 무엇을 했나 — 3단계, 순서가 중요하다

| 순 | 한 일 | 산출 |
|---|---|---|
| ① | `_Incoming/Projectiles/Arrow.png` → `Assets/Game/Sprites/Projectiles/Arrow.png` (폴더 신설) | guid `fa36863d…` · 스프라이트 fileID `21300000` |
| ② | `Proj_Bullet.prefab` 을 **복제**해 `Proj_Arrow.prefab` 신설, `m_Sprite` 만 교체 | guid `d8a10298…` |
| ③ | `Game/Balance/Import CSV -> ScriptableObjects` 1회 | `BowData.ProjectilePrefab` 이 화살을 가리킴 |

🔴 **②를 ③보다 먼저 해야 한다.** 프리팹이 없는 상태로 Import 하면
`ProjectilePrefab` 이 `0` 으로 덮이고 **활이 아무것도 안 쏘게 된다.**

### 임포트 설정 — Unity 기본값은 전부 틀렸다

넣기만 하면 `ppu=100 · Bilinear · Compressed · Multiple` 로 들어온다. 명시적으로 덮어썼다:

| 항목 | 값 | 안 맞추면 |
|---|---|---|
| **PPU** | **512** | 1024 면 화면상 절반 크기 (I-58 과 같은 함정) |
| Filter | **Point** | Bilinear 은 픽셀아트를 뭉갠다 |
| Compression | **Uncompressed** | 화살대가 4px 이라 압축에 뭉개진다 |
| Sprite Mode | **Single** · Pivot **Center** | 회전축이 화살대 중간이어야 한다 |
| Alpha Is Transparency | ✅ | |

알파는 `min=0 max=255` 로 **I-41 가짜 투명이 아니었다.** bbox `200×72px`, 좌우 여백 28px.

### 프리팹 — 다른 줄이 정확히 하나다

```diff
-  m_Sprite: {fileID: 430330343462687638, guid: 1f4e1489ffaa4e94690bcd7ca34c7b1f, type: 3}
+  m_Sprite: {fileID: 21300000, guid: fa36863d29706ab498581ebf57104088, type: 3}
```

Layer `8` · Tag `Projectile` · `Rigidbody2D`(Kinematic · gravity 0 · Constraints 4) ·
`CircleCollider2D`(isTrigger · radius 0.5) · `ProjectileBase` · `DrawMode 0` 을 전부 승계.

> ℹ️ 루트 GameObject 의 fileID 가 `691123369852945806` 으로 원본과 **같다.**
> `AssetDatabase.CopyAsset` 은 로컬 fileID 를 보존한다 — 정상이다. 가르는 건 **guid** 다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Game/Sprites/Projectiles/Arrow.png` (+`.meta`) | 신설 (CONTENT 산출물 이관) |
| `Assets/Prefabs/Proj_Arrow.prefab` (+`.meta`) | 신설 (`Proj_Bullet` 복제 + 스프라이트 1줄) |
| `Assets/Game/WeaponData/Bow.asset` | `ProjectilePrefab` guid `b302c4b4…` → `d8a10298…` **(1줄)** |
| `_Incoming/Projectiles/Arrow.png` | 삭제 (규칙대로) |

**C# 은 한 줄도 안 고쳤다.**

### 검증 로그

| # | 기준 | 결과 |
|---|---|---|
| ① | `bounds.size` | ⚠️ `(0.500, 0.500)` — 기대값 `(0.391, 0.141)` 과 다르지만 **둘 다 맞다**(아래) |
| ② | `Bow.asset` guid | ✅ `d8a10298…`, `0` 아님 |
| ③ | 나머지 수치 불변 | ✅ `ProjectileSpeed 14` · `Damage 8\|12\|17\|24\|33` · `Cooldown` 그대로 |
| ④ | 🔴 **실플레이 방향** | ✅ **눈으로 봤다** — 아래 |
| ⑤ | 명중 시 소멸 + 피해 | ✅ 대조군 비교 + 스택 트레이스 |
| ⑥ | Import 로그 | ✅ Weapons **8** · Economy **43/43** · SceneWiring **11/11** · 에러 0 |

**① 이 왜 달랐나 — 서로 다른 것을 재고 있었다**

| 무엇 | 계산 | 값 |
|---|---|---|
| `sprite.bounds.size` (Single) | **텍스처 전체 rect** 256 ÷ 512 | `(0.500, 0.500)` |
| 그림의 실제 크기 | **알파 bbox** 200 ÷ 512 · 72 ÷ 512 | `(0.391, 0.141)` |

Single 스프라이트는 알파를 잘라내지 않는다. **PPU 는 정확히 512** 이므로 ①이 잡으려던
"PPU 1024 로 들어감"(그랬다면 `0.25`)은 아니다. 실제 폭 0.391 유닛은 기존 총알 0.41 과 같다.

**④ — 화살 4개를 세우고 게임 카메라로 찍었다**

```
[D9] +X(오른쪽) → z회전 = 0.0    sprite = Arrow  flipX = False
[D9] +Y(위)     → z회전 = 90.0   sprite = Arrow  flipX = False
[D9] -X(왼쪽)   → z회전 = 180.0  sprite = Arrow  flipX = False
[D9] -Y(아래)   → z회전 = 270.0  sprite = Arrow  flipX = False
```

`Unity_Camera_Capture(Camera.main)` 결과 **네 방향 모두 촉이 진행 방향을 향한다.** 뒤집힘 0.
`ProjectileBase` 에 `flipX` 는 없다 — 방향은 `Mathf.Atan2(dir.y, dir.x)` 회전과
`Translate(Vector2.right …)` 로만 만들어지므로 **그림이 +X 를 향해야만 맞다.** 실제로 그렇다.

**⑤ — 대조군을 같이 쐈다**

| 발사체 | 소멸 지점 | 이동 거리 | 표적과의 거리 |
|---|---|---:|---:|
| `Proj_Bullet` (대조군) | `(7.07, 1.08)` | **2.37** | 0.63 |
| `Proj_Arrow` | `(7.70, 0.45)` | **2.37** | 0.63 |

숫자가 완전히 같다. 스택 트레이스가 경로 전체를 보여 준다 —
`ProjectileBase.OnTriggerEnter2D` → `EnemyBase.TakeDamage` → `EnemyBase.Die`.

> ℹ️ 첫 시도는 화살이 표적을 지나쳐 사거리 끝까지 날아갔다. 표적을 **스폰한 그 프레임에**
> 8유닛/초로 쐈던 케이스다 — 에디터가 포커스를 잃으면 프레임 간격이 커져 한 프레임
> 이동량이 콜라이더 반지름을 넘는다. **화살이 아니라 테스트 조건 문제**였고,
> 그걸 가른 게 대조군이다.

### 같이 나온 것

[`Parallel/BUGS.md` B5](Parallel/BUGS.md) 신설 — **고치지 않았다.**
웨이브 밖에서 적이 죽으면 `WaveManager.OnEnemyKilled` 가 NRE 를 던진다(`_currentWaveData` null).
`Die()` 중간에서 터져 **사망 연출·사망음이 통째로 건너뛰어지고 시체가 남는다.**
지금 게임 경로로는 안 나므로 우선순위 낮음.

임시 오브젝트 `D9_*` **7개 전부 삭제** 확인(`남은 D9_* = 0`), 플레이 종료 후 **컴파일 에러 0**.

상세: [`Parallel/DONE/D9.md`](Parallel/DONE/D9.md)

---

## 2-35. ✅ 소리 없던 트리거 6곳 배선 + 엘리트 사망음 (D8 / C3, 2026-08-30 31차)

### 왜 했나

효과음 키가 있는 자리는 I-49 이후 다 울고 있었는데, **아예 키가 없어서 조용한 지점이 6곳** 있었다.
터렛이 쏴도 · 원거리 적이 쏴도 · 식당이 회복시켜도 · 보스가 나와도 · 리롤을 눌러도 소리가 없다.

CONTENT(C3)가 스펙을 냈지만 **통째로 DEV 요청으로 왔다** — 클립 생성은 Unity AI(에디터)이고
`AudioLibrary.asset` 등록도 `.asset` 이라 둘 다 DEV 소유다. CONTENT 는 숫자만 낼 수 있었다.

같이 들어온 사용자 결정: **잡몹은 전부 같은 사망음, 엘리트·보스만 다르게.**

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Audio/AudioId.cs` | `SfxId` **7개 추가** (`EnemyDieElite`=14 · `EnemyShoot`=16 · `Crit`=17 · `Heal`=24 · `BuildingFire`=32 · `BossAppear`=33 · `UiCancel`=41) · **`:8` 주석 수정** |
| `Assets/Scripts/Enemy/EnemyBase.cs` | 사망 시 `Play(IsElite \|\| IsBoss ? EnemyDieElite : EnemyDie)` · `FireProjectile()` 끝에 `EnemyShoot` |
| `Assets/Scripts/Building/HealPickup.cs` | 회복 직후 `Heal` — 🔴 **요청서와 다른 위치** (아래) |
| `Assets/Scripts/Building/TurretBuilding.cs` | 발사 직후 `BuildingFire` |
| `Assets/Scripts/Building/BombardBuilding.cs` | 발사 직후 `BuildingFire` (중괄호 없던 if/else 에 중괄호 추가) |
| `Assets/Scripts/Wave/WaveManager.cs` | 보스 소환 직후 `BossAppear` 1회 |
| `Assets/Scripts/Shop/ShopManager.cs` | `Reroll()` 에 `UiCancel` |
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | `OnRerollClicked()` 에 `UiCancel` |
| `Assets/Game/Audio/SFX_*.wav` **6개** | 신규 생성 (`elevenlabs-sound-effects-v2`) + ADPCM·mono 재임포트 |
| `Assets/Game/Audio/AudioLibrary.asset` | `sfx` 목록 **14 → 20** |

### 🔴 `SfxId` 를 "끝에 추가"하지 않은 이유 — 기존 주석이 틀렸다

`AudioId.cs:8` 에 `"새 항목은 끝에 추가한다"` 라고 적혀 있었는데 **구현과 다르다.**
`AudioLibrary` 는 배열 인덱스가 아니라 **`Id` 값**으로 매핑한다
(`AudioLibrary.cs:70` `_sfxMap[e.Id] = e`). 그래서 **분류별 빈 번호 삽입은 안전**하고,
금지되는 것은 *기존 항목의 **값** 변경*뿐이다.
이 주석 때문에 CONTENT 도 한 번 잘못 판단했다 → 문장을 고쳤다.

삽입 후 `WeaponFire=1` … `UiSelect=40` 이 **전부 그대로**인 것을 확인했다.
`15` 는 나중에 보스만 갈라야 할 때를 위해 `EnemyDieBoss` 자리로 **비워 뒀다.**

### 🔴 요청서와 다르게 한 것 — `Heal` 위치 (사용자 승인)

요청서는 `RestaurantBuilding.OnCooldownElapsed()` 를 지목했는데, **그 메서드는 회복을 안 한다.**
힐 픽업을 **떨어뜨릴** 뿐이고 실제 회복은 `HealPickup.cs:34` `player.Heal(_amount)` 다.

요청서 자신의 규칙(*"주기적인 건물 산출에 소리를 달면 잔소리가 된다 — Farm·Village 에 안 단 것과 같은 이유"*)을
그대로 적용하면 **떨굴 때가 아니라 주울 때** 울어야 한다. 주울 때는 플레이어가 밟아서 일어난 일이라
소리가 **정보**가 된다. → 사용자에게 알리고 승인받았다.

### 🔴 터렛이 `WeaponFire` 를 재사용하지 않은 이유

`AudioManager` 의 중복 컷이 **0.04초**다(`AudioManager.cs:44`, `AudioClip` 기준).
터렛과 플레이어가 같은 키를 쓰면 **터렛 소리가 플레이어 발사음을 잡아먹는다.**
그래서 `BuildingFire` 라는 별도 키를 뒀다.

### 클립 6개 — 프로젝트 규격에 맞춰 재임포트해야 했다

🔴 **Unity AI 의 `GenerateSound` 기본값은 Vorbis + 스테레오**인데
기존 SFX 14개는 전부 **ADPCM + `forceToMono`** 다. 그대로 두면 6개만 규격이 다르다.
`compressionFormat = ADPCM` · `forceToMono = true` 로 바꾸고 `SaveAndReimport()` 했다.

| 파일 | 길이 | Volume | PitchJitter |
|---|---:|---:|---:|
| `SFX_BuildingFire.wav` | 0.52s | 0.22 | 0.14 |
| `SFX_EnemyShoot.wav` | 0.63s | 0.30 | 0.12 |
| `SFX_UiCancel.wav` | 0.52s | 0.45 | 0.03 |
| `SFX_Heal.wav` | 0.91s | 0.50 | 0.05 |
| `SFX_EnemyDieElite.wav` | 1.23s | 0.75 | 0.04 |
| `SFX_BossAppear.wav` | 2.51s | 0.95 | 0.00 |

### 검증 로그

```
[D8] EnemyDieElite -> SFX_EnemyDieElite playing=True vol=0.750 pitch=0.998
[D8] EnemyShoot    -> SFX_EnemyShoot    playing=True vol=0.300 pitch=1.082
[D8] Heal          -> SFX_Heal          playing=True vol=0.500 pitch=1.041
[D8] BuildingFire  -> SFX_BuildingFire  playing=True vol=0.220 pitch=0.934
[D8] BossAppear    -> SFX_BossAppear    playing=True vol=0.950 pitch=1.000
[D8] UiCancel      -> SFX_UiCancel      playing=True vol=0.450 pitch=0.993
```

볼륨 6개가 스펙 표와 정확히 일치하고, **`BossAppear` 만 `pitch=1.000`** 인 것이
`PitchJitter=0` 스펙까지 같이 증명한다 (나머지 5개는 전부 1.000 이 아니다).

**엘리트 분기 — 요청서는 "노말 웨이브에서는 검증할 수 없다"고 했다.**
D6 우회로를 그대로 썼다. Goblin 2마리 중 한 마리는 그대로, 한 마리는
`Initialize(data, isElite:true)` 로 재초기화한 뒤 죽였다.

```
[D8] 재초기화 후 elite=True boss=False
[D8] voice: SFX_EnemyDie
[D8] voice: SFX_EnemyDieElite
```

같은 프레임에 두 클립이 동시에 물렸다. **한쪽만 봤으면 분기가 죽어 있어도 통과했을 것이다.**

`AudioLibrary.DescribeMissing()` 은 정확히 **`Crit` 하나**를 반환한다 — 2단계로 미룬 것이 그대로 드러난다.
플레이 종료 후 콘솔 Log 8건, **Warning·Error 0**.

### 🔴 이 머신은 게임이 무음이다 — 판정 2개를 못 했다

검증 중에 발견했다. **버그가 아니라 저장된 설정이다.**

```
[D8] 저장된 설정 bgm_vol=0.00 sfx_vol=0.00
```

`AudioManager.PlaySfx()`(`:158`)는 `SfxVolume <= 0f` 이면 **보이스에 클립을 물리기도 전에 return** 한다.
그래서 첫 검증에서 6개가 전부 `(못 찾음)` 으로 나왔고 **배선이 통째로 실패한 것처럼 보였다.**

검증만 통과시키려고 런타임에서 `am.SetSFXVolume(1f)` 를 호출했다 —
`SetSFXVolume` 은 `PlayerPrefs` 를 건드리지 않으므로(`:96~105`) **사용자 설정을 바꾸지 않는다.**

요청서 판정 ③(터렛·적탄·식당·리롤 청취)와 ⑤(플레이어 발사음이 터렛에 묻히는가)는
*"소리로 판정한다. 로그로는 안 된다"* 인데 **지금 설정으로는 사용자도 아무것도 들을 수 없다.**
→ [`TODO.md`](TODO.md) §1 · [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-2

### 남긴 것

**`Crit`(값 `17`)은 값만 예약하고 클립·호출을 안 넣었다.** 요청서 지시다 —
`WeaponBase.CalculateDamage()` 가 치명타를 내부에서 굴리고 **결과를 버려서**
`ProjectileBase`/`AoeProjectile` 까지 플래그를 배관해야 한다.
한 커밋에 섞으면 **"소리가 안 나는 게 배선 탓인지 배관 탓인지" 구분이 안 된다.**

### 같이 닫은 문서

- `TODO.md` §3 **"적 6종이 같은 사망음을 쓴다 → `EnemyData` 에 `SfxId` 열"** —
  사용자 결정으로 **방향 자체가 바뀌었다.** 열을 추가하지 않는다 (근거는 위 요청서 인용)
- `TODO.md` §3 **"아직 소리가 없는 트리거"** — 6곳이 채워지고 `Crit` 만 남았다
- `TODO.md` §0 오디오 행 — SFX 14 → **20종**, 호출부 18 → **24곳**

---

## 2-34. ✅ 진화 무기 3종 아이콘 배선 — 그림은 이미 있었다 (D7 / C6, 2026-08-30 30차)

### 왜 했나

Excalibur / Windforce / Devastator 가 **재료 무기 아이콘을 그대로 쓰고 있었다.**
진화했는데 그림이 안 바뀌면 "뭐가 달라졌지?"가 된다.

🔴 **그림이 없던 게 아니라 배선이 빠져 있었다.** 전용 png 3장은 **I-57(20차, `a85cc9f`)에
이미 들어와 있었고**, 빠진 것은 `Weapons.csv` 의 `Icon` 열 하나뿐이었다.
그래서 `TODO.md` §4 에는 20차 시점부터 "진화 무기 아이콘도 교체했다"고 적혀 있었다 — **거짓이었다.**

CONTENT 가 `4f1a92c`(C6)로 CSV 를 고쳐 두었으므로 DEV 는 **Import 1회**만 하면 됐다.
CSV 는 CONTENT 소유, `WeaponData/*.asset` 은 DEV 소유라 작업이 이렇게 갈린다.

### 한 것 — `Game/Balance/Import CSV -> ScriptableObjects` 1회

Import **전에** guid 를 먼저 기록했다. 안 그러면 "원래 그랬던 것"과 "바뀐 것"을 구분할 수 없다.

| 무기 | 전 — 재료 무기 아이콘 | 후 — 전용 png |
|---|---|---|
| Excalibur | `009d725fe54e4a1499ff3903c4bf378e` (Sword) | `c35a002a740ab5c4aa5a673ce063905a` |
| Windforce | `7cb2b59ea0ed3e44e91c54ebd05f2221` (Bow) | `077dea53464b16546941e766e8074cd3` |
| Devastator | `0cfefbeb330f45c42918c8beb48a1d2d` (Gun) | `e515f1078e686b444a21c688bbaafe9f` |

### 검증

| # | 기준 | 결과 |
|---|---|---|
| ① | Excalibur 의 `Icon` 이 Sword guid 가 아니다 | ✅ `c35a002a…` |
| ② | Windforce · Devastator 도 각자 전용 png | ✅ `077dea53…` / `e515f107…` |
| ③ | 🔴 **나머지 필드가 안 바뀐다** | ✅ `git diff` 가 **파일당 `Icon` 한 줄뿐.** `Damage` 70/55/34 그대로 |
| ④ | Import 에러 0 | ✅ `[BalanceImporter] Import 완료` 1건 · Warning·Error 0 |

③ 이 핵심이다. Import 는 CSV 전체를 다시 쓰므로 **의도한 열 하나만 바뀌었는지**를 diff 로
확인하지 않으면 다른 무기 수치가 조용히 되돌아가도 알 수 없다.
`Economy.csv : 43/43 적용` 이 그대로라 **29차의 `CombatFeel` 12줄도 무사하다.**

**실플레이 검증은 면제받았다**(요청-5). 진화 아이템은 보물상자에서만 나와 화면에 띄우는
비용이 크고, 아이콘은 `WeaponData.Icon` 을 UI 가 그대로 그리는 구조라 guid 3개면 충분하다.

> 상세: [`Docs/Parallel/DONE/D7.md`](Parallel/DONE/D7.md)

---

## 2-33. ✅ 적 타격감 상수 12개를 CSV 로 — `CombatFeel` 씬 컴포넌트 신설 (D6 / C2, 2026-08-30 29차)

### 왜 했나

적 타격감 12개가 `EnemyBase.cs` 에 `const` 로 박혀 있었다. `TUNING.md` 방침상
**플레이하며 조절해야 하는 값**인데 고칠 때마다 코드를 건드리고 재컴파일해야 했다.
CONTENT 가 `Economy.csv` 에 12줄을 이미 넣었고(C2), 코드와 씬 배선만 남아 있었다.

**왜 씬 컴포넌트인가** — 다른 두 길이 막혀 있다.
`Economy.csv` 에 `EnemyBase,…` 로 적으면 `BalanceImporter.FindSceneComponent`(`:892`)가
`FindObjectsByType` 로 **씬만 훑는데** 적은 프리팹 1개를 공유해 씬에 없다.
`Enemies.csv` 에 열을 추가하면 흔들림·히트스톱이 **엘리트/보스 여부**로 갈리는데
등급은 `EnemyData` 가 아니라 **소환 시점 인자**(`Initialize(data, isElite, isBoss)`)라 표현이 안 된다.
→ 씬 컴포넌트 하나로 모으면 **임포터를 고치지 않아도** 기존 3열 규칙에 그대로 걸린다.

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scripts/Enemy/CombatFeel.cs` | **신설.** `[SerializeField]` 12개 + `Default*` `const` 12개 + static 조회 프로퍼티 12개 |
| `Assets/Scripts/Enemy/EnemyBase.cs` | `const` 3개 제거, 7군데를 `CombatFeel` 읽기로 |
| `Assets/Scenes/SampleScene.unity` | `GameManager` 오브젝트에 `CombatFeel` 1개 |

### 설계 결정 두 가지

**① `Default*` `const` 하나가 필드 기본값이자 폴백이다.**
숫자가 두 군데로 갈라지면 *컴포넌트가 없을 때만 다른 감각으로 조용히 도는* 상황이 생긴다.
이번 판정 기준이 "화면이 하나도 안 바뀐다"라 특히 위험했다.

**② `Awake()` 는 자기 등록만 하고, 읽기는 매번 `== null` 로 확인한다.**
`Awake` 캐시 금지(I-8/I-38)와 "가짜 null"(I-24)을 둘 다 피한다.
12개 static 프로퍼티가 `c != null ? c.field : Default*` 로 답하므로
**씬에 컴포넌트가 없어도 예외 없이 기본값으로 굴러간다.**

`DeathPopRoutine` 만 루프 **전에** 지역변수로 한 번 읽는다 — 연출 도중 값이 바뀌면 보간이 튄다.
**잡몹의 넉백 저항 `1f` 는 빼지 않았다** — "저항 없음"이라는 기준값이지 튜닝 대상이 아니다.

### 검증 로그

```
[BalanceImporter] Import 완료   Economy.csv : 43/43 적용     ← 31 → 43, 정확히 +12
                                ! 경고 0줄

[D6] Enemy_Goblin(Clone) elite=False boss=False 넉백속도=20.000  ← CSV 를 20 으로 올렸을 때
[D6] 엘리트 넉백속도=8.000  (기대 8.000 = 20 × 0.4)
[D6] 보스   넉백속도=0.000  (기대 0.000 = 저항 0)

[D6] ① 잡몹 넉백=6.000 (기대 6.000)                          ← 6 으로 되돌린 뒤
[D6] ② 보스 사망 → timeScale=0 (기대 0 · BossHitstop=0.09)
```

플레이 종료 후 콘솔 **7건 전부 `Log`** · Warning 0 · Error 0.

🔴 **④(화면이 안 바뀐다)만으로는 부족하다.** 12줄이 전부 원래 값이라
배선이 통째로 죽어 있어도 화면은 똑같다. `6`→`20`→`6` 왕복이 이 작업의 실제 판정이었다.
엘리트 `8.000` 은 저항까지 곱해진 값이라 **저항 2줄도 같이 증명한다.**

🔴 **`GameManager` 는 프리팹 인스턴스라 `CombatFeel` 이 `m_AddedComponents` 오버라이드로 붙었다.**
동작에는 문제가 없지만 **프리팹 오버라이드를 Revert 하면 컴포넌트가 사라지고**
CSV 12줄이 `! 씬에 CombatFeel 없음` 으로 조용히 건너뛰어진다 — 에러 없이 CSV 만 먹통이 된다.

자세한 기록: [`Parallel/DONE/D6.md`](Parallel/DONE/D6.md)

---

## 2-32. ✅ 일시정지·옵션창 한글 6곳 영문화 — 빈칸이 사라졌다 (D5 / B4, 2026-08-30 28차)

### 왜 했나

B4. 일시정지·옵션 패널의 글자 **6곳이 빈칸**으로 나왔다. 버튼은 눌렸다 — **글자만** 안 그려졌다.

원인은 23차(I-60)가 남긴 알려진 제약이다. 폰트 `Pretendard SDF` 는 그때부터
**Static · 115자**(ASCII 32~126 + 기호 20)라 **한글 글리프가 없다.**
그런데 씬 6곳에 한글이 남아 있었다 — `CLAUDE.md` §3 *"UI에 표시되는 문자열은 영문"* 위반이기도 하다.

**폰트를 다시 굽지 않고 영문화한다**(사용자 결정). 6개 단어를 위해 한글을 넣으면
I-60 이 55MB → 4.8MB 로 줄인 아틀라스가 도로 커진다.

### 고치기 전에 확인한 3가지

| # | 확인 | 결과 |
|---|---|---|
| ① | `BUGS.md` 의 하이어라키 경로 6개가 실제로 있나 | 6/6 존재 · 중복 없음 |
| ② | **코드가 한글을 `.text` 에 넣는 곳은 없나** | `Assets/Scripts/**` 전수 — **0건** |
| ③ | 넣을 6단어가 폰트 문자표에 있나 | `HasCharacters` → 전부 `missing=[]` |

②가 핵심이다. 코드가 한글을 만들고 있었다면 씬만 고쳐도 다시 빈칸이 됐다.

> 🔴 **`BUGS.md` 표의 행 번호는 이미 밀려 있었다.**
> 문서 12562·13296·3141·5364·4186·4444 → 실제 **3278·4323·4581·5501·12699·13433.**
> 그 표에 붙어 있던 "행 번호로 찾지 말 것" 경고가 그대로 맞았다.

### 변경한 파일

| 파일 | 무엇 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | TMP `m_text` **6곳**만 한글 → 영문 (`6 insertions(+) / 6 deletions(-)`) |

| 하이어라키 경로 | 전 | 후 |
|---|---|---|
| `UI Canvas/PausePanel/Card/TitleText` | 일시정지 | `PAUSED` |
| `UI Canvas/PausePanel/Card/ResumeButton/Label` | 계속하기 | `Continue` |
| `UI Canvas/PausePanel/Card/OptionButton/Label` | 옵션 | `Options` |
| `UI Canvas/PausePanel/Card/QuitButton/Label` | 종료 | `Quit` |
| `UI Canvas/OptionSubPanel/Card/TitleText` | 옵션 | `OPTIONS` |
| `UI Canvas/OptionSubPanel/Card/CloseButton/Label` | 닫기 | `CLOSE` |

> 제목 2개만 대문자다. 씬의 다른 패널 제목(`SELECT CLASS` · `LEVEL UP` · `MERCHANT` · `SHOP`)과 맞췄다.
> 버튼 라벨은 기존 버튼(`Retry` · `Start` · `Back` · `Reroll`)과 같은 규칙으로 첫 글자만 대문자.

### 🔴 걸려 넘어진 것 — 플레이 모드에서는 씬을 저장할 수 없다

치환은 `changed = 6 / 6` 으로 성공했는데 저장에서 터졌다.

```
System.InvalidOperationException: This cannot be used during play mode
  at UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty
```

**D4 끝에 내가 껐던 플레이 모드를 그 사이에 사용자가 다시 켜 놓았다.**
플레이 모드에서 한 씬 변경은 플레이를 끄면 **전부 되돌아간다** — 디스크에는 안 남는다
(`git status --short Assets/Scenes/` 가 빈 출력인 것으로 확인). 임의로 끄지 않고 물어서
승인을 받은 뒤 `Stop` → 재실행했고, 그 뒤로 스크립트 맨 앞에 가드를 넣었다:

```csharp
if (EditorApplication.isPlaying) { result.LogError("[D5] 아직 플레이 모드다. 중단한다."); return; }
```

### 검증 로그 — 판정 기준 5개

| # | 기준 | 결과 |
|---|---|---|
| ① | 6곳이 영문 | 전부 `ascii=True` ✅ |
| ② | 폰트 문자표에 전부 있음 | `HasCharacters=True · missing=[]` ✅ |
| ③ | 실제로 글리프가 그려짐 | `ForceMeshUpdate` 후 `characterCount == visible` ✅ |
| ④ | 🔴 실플레이 육안 | 캡처 2장 ✅ |
| ⑤ | 🔴 폰트 경고 0건 | 플레이 전체 로그 **7건 전부 `Log`** · Warning 0 · Error 0 ✅ |

```
[D5] [PAUSED]   font=Pretendard SDF  hasAll=True  missing=[]  chars=6 visible=6
[D5] [Continue] font=Pretendard SDF  hasAll=True  missing=[]  chars=8 visible=8
[D5] [Options]  font=Pretendard SDF  hasAll=True  missing=[]  chars=7 visible=7
[D5] [Quit]     font=Pretendard SDF  hasAll=True  missing=[]  chars=4 visible=4
[D5] [OPTIONS]  font=Pretendard SDF  hasAll=True  missing=[]  chars=7 visible=7
[D5] [CLOSE]    font=Pretendard SDF  hasAll=True  missing=[]  chars=5 visible=5
```

③이 ②보다 강하다. 문자표에 있어도 렌더링에서 빠질 수 있는데, 6곳 모두
`characterCount` 와 `isVisible` 개수가 같았다 = **빈칸이 하나도 없다.**

#### ④ — 오버레이 캔버스는 카메라 캡처에 안 잡힌다

`UI Canvas` 는 `ScreenSpaceOverlay` 다. `Unity_Camera_Capture` 는 카메라를 렌더 타깃에
그리는 방식이라 **오버레이 UI 가 한 픽셀도 안 나온다** — 첫 캡처는 타일과 적만 찍혔다.
플레이 모드 한정으로 `renderMode` 를 `ScreenSpaceCamera` + `worldCamera = Camera.main` 로
바꿔 찍었다. 플레이를 끄면 되돌아가므로 디스크에는 안 남는다 —
끝난 뒤 `git diff --stat` 이 여전히 `6 insertions(+), 6 deletions(-)` 인 것으로 확인했다.

두 장 다 또렷하게 읽혔다 — 일시정지 `PAUSED / Continue / Options / Quit`,
옵션 `OPTIONS / BGM / SFX / CLOSE`.

웨이브까지는 D4 와 같은 경로로 들어갔다 — `PauseMenuUI.Open()` 은
`CanPause(s) => s == GameState.Wave` 라 메인 메뉴에서는 그냥 return 한다.
`StartRun()` → `StageMapManager.SelectNode(Layers[0][0])`.

### 남은 것

폰트는 여전히 Static 115자다. **새 UI 문자열에 한글을 넣으면 같은 버그가 재발한다.**
`CLAUDE.md` §3 이 그 방어선이다.

> ℹ️ 같이 훑다 확인 — `UI Canvas/StageMapPanel/HeaderText` 의
> `Choose your path — Layer 1/10` 에 있는 `—`(U+2014)는 **문자표에 있다.** 7번째 사례가 아니다.

---

## 2-31. ✅ 걷기 시트 4종 교체 — 축소판 격자가 사라졌다 (D4 / B2, 2026-08-30 27차)

### 왜 했나

B2. 적이 **걸을 때만** 한 프레임이 아니라 작은 캐릭터가 격자로 뭉친 그림으로 보였다.
원인은 25차에 확정돼 있었다 — **코드가 아니라 애셋이 깨져 있었다.**
Goblin·Slime 은 16칸 중 12칸이 프레임이 아니라 "축소판 9~16마리"였다.

수정본 제작은 CONTENT(C1), **임포트와 실플레이 판정은 에디터가 필요해 DEV(D4)** 로 넘어왔다.

### 🔴 이 작업의 전부는 "png 만 덮어쓴다"

`.meta` 를 지우면 두 가지가 **동시에** 깨진다.

| 지웠을 때 | 결과 |
|---|---|
| PPU 가 기본값 **1024** 로 재생성 | 적이 **걷는 순간 크기가 절반**이 된다 — I-58 이 잡았던 버그의 재발 |
| 스프라이트 서브애셋 GUID 재발급 | `EnemyData.WalkFrames` **16칸이 통째로 `None`** |

그래서 png 4장만 바이트 교체했다. `.meta` 6개는 **타임스탬프까지 그대로**임을 확인했다.
Ogre·Zombie 는 원래 정상이라 손대지 않았다.

### 넘겨받기 전 다시 센 것

CONTENT 보고를 그대로 믿지 않고 `_Incoming/` 의 png 를 직접 열었다.

- 4장 전부 `alpha min=0 max=255` → **I-41 "가짜 투명" 아님**
- 16칸 × 4장 = **64칸 전수** 알파 덩어리 정확히 **1개**
- bbox — Goblin `149×132` · Slime `175×114` · Demon `256×211` · Wolf `230×133` (보고와 일치)

### 검증 로그 (판정 기준 5개)

| # | 기준 | 결과 |
|---|---|---|
| ① | PPU 512 | 6종 전부 `ppu=512` · `mode=2`(Multiple) |
| ② | 슬라이스 16개 | 6종 전부 `sprites=16` |
| ③ | `WalkFrames` 16장 · `None` 0 | 6종 전부 `count=16 None=0` |
| ④ | 🔴 실플레이 격자 0회 | 아래 |
| ⑤ | `sprite.bounds.size` | `[0]`/`[last]` 둘 다 `(0.500, 0.500, 0.200)` — 크기 안 변함 |

`StageMapManager.RollStageType` 은 `layer == 0` 이면 항상 `Normal` 이라
`map.Layers[0][0]` 을 `SelectNode` 해 확실히 노말 웨이브를 띄웠다. 그 위에서
살아 있는 적 14마리를 `Initialize` 로 **Goblin/Slime 으로 강제 교체**해 가장 심하게
깨져 있던 2종을 확실히 화면에 올렸다.

```
[D4] 지금 화면에 쓰인 칸 12종
[D4]   Goblin_Walk/frame_3  256x256      [D4]   Slime_Walk/frame_1  256x256
[D4]   Goblin_Walk/frame_11 256x256      [D4]   Slime_Walk/frame_10 256x256
[D4]   Goblin_Walk/frame_5  256x256      [D4]   Slime_Walk/frame_15 256x256
[D4]   Goblin_Walk/frame_4  256x256      [D4]   Slime_Walk/frame_8  256x256
[D4]   Goblin_Walk/frame_9  256x256      [D4]   Slime_Walk/frame_0  256x256
[D4]   Goblin_Walk/frame_0  256x256      [D4]   Goblin_Walk/frame_12 256x256
```

🔑 **f8~f15 구간이 포함돼 있다.** 예전에 깨져 있던 게 정확히 그 구간인데
지금은 전부 **단일 256×256 칸**이다. 카메라 캡처 3장에서도 격자는 한 번도 안 나왔다.

플레이 종료 후 **컴파일 에러 0.**

### 같이 닫은 것 — 사용자 스크린샷 2장

- ✅ `[E] PROMOTE — Sentinel` 이 **뜬다**
- ✅ `PROMOTION READY  Sentinel — stand by your Turret and press E` (진화 안내 4단 중 ④ 승급 쪽)
- ✅ 난전에서도 안내 문구가 **읽힌다**
- ✅ **`DevPanel` 이 화면에 그려진다** — 26차(2-30)의 마지막 미확인 항목

### 남은 것

- **이 작업은 B1 을 고치지 않는다.** Demon(최대 bbox `256` = 여백 **0px**) ·
  Wolf(`230` = 여백 **13px**)는 **일부러 안 줄였다** — 줄이면 외곽선 번짐이 가려지지만
  **적의 화면상 크기가 바뀐다.** 그건 버그 수정이 아니라 연출 변경이다
- **`[E] EVOLVE`(무기 진화)는 여전히 안 뜬다 — 버그가 아니라 콘텐츠 공백이다.**
  진화 SO 3종의 재료가 셋 다 무기+패시브라
  `EvolutionData.IsFinalEvolution => AltarBuilding != null`(`:54`)이 성립하지 않는다 →
  [`TODO.md`](TODO.md) §3

상세: [`Parallel/DONE/D4.md`](Parallel/DONE/D4.md)

---

## 2-30. ✅ DEV 치트 패널 — 진화·승급을 즉시 시험한다 (D3, 2026-08-30 26차)

### 왜 했나

사용자 지시 — *"진화나 기능테스트를 위해 dev 모드 만들어서 여러 아이템들 레벨 쉽게 조정할 수 있게 해"*

**정상 플레이로는 검증이 사실상 불가능했다.** 무기 진화는 Lv5 무기 + Lv3 건물,
직업 승급은 그 위에 조건이 더 붙는다. 한 번 확인하려고 매번 처음부터 몇 분씩 굴려야 했고,
그래서 [`TODO.md`](TODO.md) §1 의 미검증 항목이 쌓이기만 했다.

### 설계 결정 3가지

**① 캔버스 UI 가 아니라 IMGUI(`OnGUI`)** — 셋 다 실익이다.

| 이유 | 내용 |
|---|---|
| 배선량 | 아이템 20종 + 진화 목록을 스크롤뷰 + 행 프리팹으로 만들면 **배선이 본 게임보다 커진다** |
| 폰트 | TMP 는 I-60 이후 **Static 115자** — 문자표에 없는 글자가 빈칸이 된다(B4). IMGUI 는 내장 폰트라 무관 |
| 정렬 순서 | 캔버스 `sortingOrder` 싸움(I-50)에 끼어들지 않는다 |

**② 씬에 넣지 않는다** — `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 로 스스로 붙는다.
씬에 오브젝트로 두면 이 클래스가 `#if` 로 잘려 나가는 **릴리즈 빌드에서 "Missing script" 로 남는다.**
런타임 생성이면 `.unity` 에 흔적이 0 이라 병렬 세션의 씬 충돌도 안 만든다.
`DontDestroyOnLoad` 인 이유 — 이 콜백은 **게임 시작 때 한 번만** 돈다. Retry 는 씬을 다시 로드한다(I-17).

**③ 🔑 치트가 게임 로직을 우회하지 않는다** — 우회하면 **거기서 나온 상태를 신뢰할 수 없다.**

- 슬롯 상한(`CanAcquire`)을 그대로 지킨다. 뚫으면 `LevelUpManager.ApplyItem` 주석이 경고하는
  *"인벤토리에는 있는데 무기는 없는"* 유령 상태가 생긴다 — **치트로 만든 유령은 버그처럼 보인다.**
- "레벨 내리기" 경로를 새로 만들지 않는다(게임에 그 개념이 없다).
  검증된 공개 API 만 조합한다 — `RemoveItemFull()` 후 `ApplyItemFromShop()` × N.

### 변경한 파일

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Dev/DevPanel.cs` | **신규.** 파일 전체가 `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 안 — 릴리즈에 한 줄도 안 들어간다 |
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | `public IReadOnlyList<ItemData> AllItems => allItems;` 한 줄 |

**씬 파일은 안 건드렸다** (위 ②).

### 쓰는 법

**백틱(`` ` `` — 숫자 1 왼쪽)** 으로 열고 닫는다. 열려 있는 동안 `Time.timeScale = 0`.

> 🔴 **`F1` 은 못 쓴다** — 유니티 에디터가 "Unity 매뉴얼 열기"로 물고 있어 브라우저가 뜬다.

| 줄 | 있는 것 |
|---|---|
| 플레이어 | `State` · `Class` · `Lv (xp/next)` · `G` + `Level +1` `Gold +100` `Full Heal` |
| 아이템 | Weapon / Passive / Building 별 `(보유/슬롯)` 헤더 + 행마다 `-` `+` `Max` `X` |
| 진화 | `Ready Evolutions` — 조건이 찬 무기 진화 · 직업 승급 버튼 |

닫을 때 `timeScale` 은 **열 때 값으로** 되돌린다. 무조건 1 로 되돌리면
ESC 일시정지 위에서 열었다 닫았을 때 **일시정지 화면 뒤에서 게임만 다시 흐른다.**
`OnDisable` 에서도 되돌린다 — 안 하면 씬 전환 시 `timeScale` 0 이 남아 게임이 멈춘 것처럼 보인다.

### 검증 로그 (플레이 모드)

```
[D3] [DevPanel] object = FOUND   component = OK   scene = DontDestroyOnLoad
[D3] GameManager = OK  state = MainMenu       AllItems = 20
[D3] StartRun -> state=StageMap   pick weapon=Sword building=Turret
[D3] after 5x '+'   lv=5  CurrentLevel=5  equipped=1
[D3] after '-'      lv=4  equipped=1        ← remove-then-reapply 후에도 무기가 남아 있다
[D3] building Max   lv=5  pending=3  next=Turret
[D3] after 'X'      lv=0  equipped=0
[D3] 전부 Max 시도 -> 보유 13종 (슬롯 상한 때문에 전부는 안 된다)   ← ③ 이 지켜진다
[D3] ready weapon=1 class=2
[D3]   Evolve  -> Excalibur  altar=False
[D3]   Promote -> Sentinel     [D3]   Promote -> Warden
[D3] EvolveClass(Sentinel) = True -> class=Sentinel  chain=2
```

플레이 종료 후 **컴파일 에러 0.**

### 남은 것

- ✅ **`OnGUI` 렌더링은 27차에 닫혔다** — 사용자가 실플레이 스크린샷을 줬고,
  게임 뷰 좌측에 패널이 아이템 행 · `Ready Evolutions` · `Promote -> Sentinel [altar]` 까지
  정상으로 그려져 있었다. 26차에 남긴 방식(사람이 백틱을 눌러 판정) 그대로 판정됐다.
  > MCP 로 백틱을 주입하려던 시도는 끝내 실패했다 — **게임 뷰에 포커스가 없어 입력이 도달하지 않는다.**
  > **도구의 한계이지 코드 문제가 아니다.** 입력이 필요한 검증은 앞으로도 로직을 직접 부르고
  > 화면은 사람 눈에 맡긴다.
- ⚠️ **진화 버튼은 제단 근접 판정을 건너뛴다** (`EvolutionManager.Evolve` 는 `IsSatisfied` 만 본다).
  **"건물 앞에서 `E`" 경로는 이걸로 검증되지 않는다** — [`TODO.md`](TODO.md) §1 에 그대로 남는다.

상세: [`Parallel/DONE/D3.md`](Parallel/DONE/D3.md)

---

## 2-29. ✅ 설치 대기 건물을 HUD 에 표시 (D2 / B3, 2026-08-30 25차)

### 왜 했나

사용자 실플레이 보고 — **"Turret·Restaurant 가 설치된 상태에서 Village 를 얻고 `Z` 를 눌렀더니
Village 가 아니라 Turret·Restaurant 가 한 채씩 더 설치됐다."** → [`Parallel/BUGS.md`](Parallel/BUGS.md) B3

### 원인 — 예외도 null 도 아니었다. 설계대로 돌고 있었다

`BuildingManager.UnlockBuilding` (38~51행):

```csharp
int allowed = data.GetMaxCount(level);
for (int owned = PlacedCount(data) + PendingCountOf(data); owned < allowed; owned++)
    _pendingQueue.Add(data);
```

`BuildingData.MaxCount` 는 `{ 1, 1, 2, 2, 3 }` 이라 **레벨이 오르면 동시 배치 가능 수가 늘어난다.**
그래서 Turret 을 레벨업하는 것만으로 `_pendingQueue` 에 Turret 이 **말없이 더 쌓인다.**
뒤에 얻은 Village 는 큐 맨 뒤에 붙고, `PlaceNext` 는 `_pendingQueue[0]` 부터 꺼내므로(FIFO)
밀려 있던 Turret 이 먼저 나온다.

🔑 **진짜 문제는 "잘못 설치된다"가 아니라 "무엇이 설치될지 모른다" 였다.**
레벨업 카드는 "Village 획득"이라고 말해 놓고 `Z` 는 다른 걸 준다 — **UI 가 한 약속을 조작이 안 지킨다.**
게다가 `BuildingManager.NextPending` 은 **이미 public 이었는데 HUD 가 그냥 안 쓰고 있었다.**

### 왜 큐 순서를 안 바꿨나 (사용자 결정: B안)

| 안 | 내용 | 대가 |
|---|---|---|
| A | `Z` 가 가장 최근에 얻은 것부터 (LIFO) | 밀린 Turret 2채가 영영 안 나올 수 있다 |
| **B** ✅ | HUD 에 `NextPending` 을 **표시만** 한다 | 동작은 그대로. **가장 작고 되돌리기 쉽다** |
| C | 신규 해금은 큐 앞, 레벨업 증설분은 뒤 | 규칙이 둘로 갈려 설명이 길어진다 |
| D | 설치할 건물을 플레이어가 고른다 (새 UI) | 범위가 크다 → `TODO.md` 감 |

A·C 는 **큐 순서를 바꾸는 일이라 건물 채수 밸런스에 영향이 간다.**
지금은 수치를 잡는 단계가 아니므로(`CLAUDE.md` §1 방침) 표시부터 넣고 실플레이로 다시 본다.

### 변경한 파일

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/UI/HUDManager.cs` | `buildPromptText` 직렬화 필드 + `RefreshBuildPrompt(gm)` 신설. `RefreshWaveInfo` 끝에서 호출 |
| `Assets/Scenes/SampleScene.unity` | `UI Canvas/HUD/BuildPromptText` (TMP) 생성 · `HUDManager.buildPromptText` 에 배선 |

표시 문구 (UI 문자열은 영문 — `CLAUDE.md` §3):

- 대기 1개 : `[Z] Build: Village`
- 대기 여러 개 : `[Z] Build: Turret  (+3 queued)`
- 대기열이 비면 **오브젝트를 끈다**

배치는 `anchoredPosition (0, 90)`, 폭 900 — **진화 안내(`EvolutionPromptText`, y=140) 바로 아래**다.

> ℹ️ 폰트는 `EvolutionPromptText` 의 SDF 를 그대로 물렸다. I-60 이후 **Static 115자**라
> 문자표에 없는 글자는 빈칸이 되는데, 위 문구는 전부 ASCII 라 안전하다.

> ℹ️ `RefreshBuildPrompt` 는 `GameManager` 를 **폴링**한다 (I-8 · I-38 회피).
> 문자열은 `pending` 또는 `count` 가 **바뀔 때만** 만든다 — 이 메서드는 매 프레임 돈다.

### 검증 로그 (플레이 모드, 실제 버그 상황 재현)

```
[D2] Turret Lv1 해금          → pending=1 next=Turret
[D2] Restaurant Lv1 해금      → pending=2 next=Turret
[D2] 둘 다 Lv3 (MaxCount 1→2) → pending=3                    ← 조용히 늘어난 지점
[D2] Village 획득             → pending=4 next=Turret         ← 사용자가 본 상황 재현됨
[D2] HUD active=True  text='[Z] Build: Turret  (+3 queued)'
[D2] PlaceNext=True → pending=3 next=Restaurant
[D2] 설치 1채 후    → text='[Z] Build: Restaurant  (+2 queued)'  ← 따라온다
[D2] pending=0 → active=False                                 ← 큐가 비면 숨는다
```

① 버그 상황이 재현되고 ② 그 상황에서 `Turret` 이 뜨고 ③ 설치하면 **갱신되고** ④ 비면 **숨는다.**
플레이 종료 후 컴파일 에러 0.

### 같이 확정한 것 — B1 · B2 (고치지는 않았다)

같은 보고에 있던 다른 두 건은 **원인만 확정하고 손대지 않았다** (`CLAUDE.md` §1 "임의로 고치지 말 것").

- **B2 — 걷기 시트가 깨졌다 (담당 CONTENT).** 코드·슬라이스(4×4·256×256)·SO 배선(6종 전부 16장)이
  모두 정상임을 확인한 뒤 칸별 **알파 덩어리 수**로 애셋 문제임을 입증했다.
  Goblin·Slime 은 16칸 중 **12칸이 작은 몹 뭉치**(각 16마리·9마리), Demon 3칸·Wolf 1칸도 오염.
  Ogre·Zombie 는 16칸 전부 정상. 정지 중엔 `_idleFrame`(f0) 고정이라 **움직일 때만** 보인다
  → [`Parallel/REQ/CONTENT.md`](Parallel/REQ/CONTENT.md) 요청-1
- **B1 — 엘리트 외곽선이 어긋난다 (담당 결정 대기).** `SpriteOutline.shader` 의 `SampleAlpha` 가
  `uv 0~1` 로 아틀라스 밖을 막는데, **시트에서 잘라 온 스프라이트의 `uv` 는 텍스처 전체 기준**이라
  그 검사가 절대 안 걸린다 → **옆 칸 알파를 빨아들인다.** 반경은 `14/512 = 28텍셀`이고
  Wolf 여백 14px · Demon 0px 이라 수치도 맞아떨어진다. I-58 에서 낱장 png → 시트로 바꾼 순간 전제가 깨졌다.
  **원인은 로직인데 파일은 CONTENT 소유**라 담당을 정해야 한다

### 남은 것

**B3 은 닫지 않았다.** 표시가 붙었을 뿐 큐 순서는 그대로다 — 다만 이제 **일어나기 전에 보인다.**
실플레이에서 여전히 헷갈리면 A·C·D 를 다시 본다.

---

## 2-28. ✅ 승급 배타 + 승급 전용 직업 차단 — `Tier` 신설 (I-61, 2026-08-29 24차)

### 왜 했나

결정 5·6 의 구현이다.

**(1) 배타가 없었다.** 조건만 채우면 T2 세 갈래(Sentinel · Doomlord · Warden)를 **다 먹을 수 있었다.**
직업 사슬은 보너스가 **누적**되므로(`PlayerStats.SlotLimit` · `ApplyBonus` 가 사슬 전체를 합산),
다 모으면 갈래가 사라질 뿐 아니라 **후반이 일방적으로 세진다.**

> 왜 배타인가: 이래야 직업이 **"빌드가 도달하는 지점"** 이 된다.

**(2) 승급 전용 직업을 데이터가 아니라 배선으로만 막고 있었다.**
`SceneWiring.csv` 의 `GameManager,classes` 에 안 적는 것이 유일한 방어선이었다.
나중에 직업 해금 흐름을 붙일 때 **실수로 넣으면 T2 로 런이 시작된다.**

### 🔑 `Tier` 를 어디에 둘 것인가 — TODO 의 전제가 틀렸다

`TODO.md` §2-C 는 `Tier` 열을 **`ClassEvolutions.csv`(레시피)** 에 두는 것을 전제했다. 그런데
배타 판정이 실제로 묻는 것은 **"내 사슬에 이미 같은 티어가 있나"** 이고,
`PlayerStats.ClassChain` 이 들고 있는 건 레시피가 아니라 **`CharacterClassData`** 다.

레시피에 두면 사슬의 각 직업을 "그걸 만들어 낸 레시피"로 **역추적**해야 하는데,
**T1(Warrior·Ranger·Mage)은 자기를 만든 레시피가 아예 없다.** 역추적이 성립하지 않는다.

→ **`Classes.csv` 에 두는 것이 맞다.**

### 🔑 `IsPromotionOnly` 는 열이 아니라 파생 프로퍼티로

결정 6 은 `IsPromotionOnly` **열** 추가였지만, 그 값은 정확히 `Tier > 1` 이다.
CSV 열 두 개가 같은 사실을 말하면 **어긋날 길이 생기고, 어긋나면 T2 로 런을 시작하는 사고**가 난다.
이름과 용도는 그대로 두되 **단일 출처(`Tier`)에서 파생**시켰다.

```csharp
public bool IsPromotionOnly => Tier > 1;
```

### 한 일

| 파일 | 변경 |
|---|---|
| `Player/CharacterClassData.cs` | `Tier`(기본 1) 필드 + `IsPromotionOnly` 파생 프로퍼티 |
| `Player/PlayerStats.cs` | `HasTier(int)` — 사슬에 그 티어가 있는지. LINQ 없이(매 프레임 호출된다) |
| `Evolution/EvolutionManager.cs` | `IsClassSatisfied` 에 `if (ps.HasTier(evo.ResultClass.Tier)) return false;` |
| `UI/ClassSelectUI.cs` | `BuildCards` 가 `IsPromotionOnly` 를 걸러내고 경고 · `Select` 도 같이 막음 |
| `Editor/BalanceImporter.cs` | `Tier` 임포트(`Mathf.Max(1, …)`) + Export 헤더에 추가 |
| `Game/Balance/Classes.csv` | `Tier` 열 신설 — T1×3 / T2×3 / T3×1 + 주석 |

`ClassSelectUI.Select` 도 같이 막은 이유: `OnShown` 이 **저장된 `SelectedClassIndex` 를 그대로** 넘기므로,
그 인덱스가 승급 전용을 가리키면 **카드는 없는데 Start 버튼만 살아나** 필터가 무의미해진다.

### 검증 로그 (Play 모드)

Warrior 로 시작해 Gun5·Turret5·Bomb5·Bombard3·Armor3 을 지급하고 실행:

```
[TIER] BEFORE  chainCount=1  ready=Sentinel Doomlord      ← T2 두 갈래 다 열려 있다
[TIER] AFTER   evolved=True  chain=Warrior(T1) Sentinel(T2)  ready=(none)
[TIER] Doomlord satisfied=False   HasTier2=True HasTier3=False

[TIER] Turret=5  ready=Aegis                              ← T3 은 막히면 안 된다
[TIER] evolved=True  chain=Warrior(T1) Sentinel(T2) Aegis(T3)  HasTier3=True

[SEL] wired=4  cards=3   ← Sentinel 을 목록에 끼워 넣자 카드에서 빠졌다
[Warning] [ClassSelectUI] 'Sentinel' 은 Tier 2 승급 전용이라 선택 화면에서 제외했다.
[SEL] forced selectedIdx=3 -> Sentinel
[SEL] after reopen selectedIdx=0 -> Warrior                ← Select 가드
```

> 🔑 **"막힌다"만 확인하면 반쪽이다.** 티어 규칙이 *모든* 승급을 막고 있어도 같은 로그가 나온다.
> **T3(Aegis)이 열리는 것까지** 확인해야 "같은 티어만 막는다"가 증명된다.

> `[SEL]` 검증은 승급 직업을 **플레이 모드에서** `classes` 배열에 끼워 넣어 만든 상황이다.
> 플레이 종료 후 씬이 `Warrior Ranger Mage` 로 되돌아온 것을 확인했다.

> ✅ 곁들여 확인된 것 — **위 플레이 세션 뒤에도 `Pretendard SDF.asset` 이 안 바뀌었다.**
> I-60 의 Static 화가 실제로 먹혔다는 증거다.

---

## 2-27. ✅ 폰트 정리 — 미사용 18장 삭제 · SDF Static 고정 (I-60, 2026-08-29 23차)

### 왜 했나

두 가지 문제가 한 파일에 겹쳐 있었다.

**(1) `Pretendard SDF.asset` 이 Dynamic 이라 매 커밋마다 diff 가 25만 줄이었다.**
Dynamic 모드에서는 런타임에 처음 만나는 글리프를 그 자리에서 아틀라스에 굽고 애셋을 dirty 로 만든다.
즉 **플레이만 해도 폰트 애셋이 바뀐다.** 그래서 지금까지 이 파일을 계속 staging 에서 빼 왔고,
"언젠가 정리한다"가 결정 1로 남아 있었다.

**(2) 폰트 원본 19장 중 18장이 아무 데서도 안 쓰였다.** `Assets/Fonts` 가 55MB 였다.

### 삭제해도 되는지 먼저 확인한 것

| 확인 | 방법 | 결과 |
|---|---|---|
| SDF 가 참조하는 원본이 무엇인가 | `.asset` 의 `sourceFontFileGUID` → `.meta` 대조 | `16f72bceebca3684b818b190e859a996` = `alternative/Pretendard-Regular.ttf` |
| 나머지 18장을 쓰는 데가 있나 | 18개 GUID 를 `.unity`/`.prefab`/`.asset`/`.mat` 전체에서 검색 | **전부 0건** |

### Static 으로 굳혀도 되는지 먼저 확인한 것

Static 은 **문자표에 없는 글자를 빈칸으로 낸다.** 그런데 폰트에는 한글이 **66자 구워져 있었다**
(`닫기 옵션 일시정지 종료 리롤 오늘 특가…` — 한국어 UI 를 쓰던 시절의 잔재).
그대로 한글을 버리면 남아 있는 한글 UI 가 통째로 빈칸이 된다. 그래서 **네 경로를 전수 확인**했다.

| 경로 | 방법 | 한글 |
|---|---|---|
| 씬 `m_text` | `SampleScene.unity` 파싱 | **0건** |
| 프리팹 `m_text` | `Assets/**/*.prefab` | **0건** |
| C# 문자열 리터럴 | 임시 스캔 스크립트 (주석·`Tooltip`·`Header`·`Debug.Log` 제외) | 8건이나 **전부 툴팁 이어붙이기·후행 주석** → TMP 로 안 간다 |
| CSV 표시 열 | Items/Events/Classes/Buildings/Weapons/Passives/Evolutions/ClassEvolutions | **전부 영문** |

→ **한글 66자는 버려도 안전하다**는 결론. (CLAUDE.md §3 "UI 문자열은 영문" 규칙 덕에 이미 정리돼 있었다.)

### 한 일

| 대상 | 변경 |
|---|---|
| `Assets/Fonts/public/static/*.otf` 9장 | 삭제 (+`.meta`) |
| `Assets/Fonts/public/static/alternative/*.ttf` 8장 | 삭제 (+`.meta`). `Pretendard-Regular.ttf` 만 남김 |
| `Assets/Fonts/public/variable/` | 삭제 |
| `Assets/_Recovery/0.unity` | 삭제 — 옛 복구 사본, 참조 0건 |
| `Assets/Fonts/Pretendard SDF.asset` | `ClearFontAssetData` → 목표 문자만 재굽기 → **`atlasPopulationMode = Static`** |

굽기는 **기존 애셋을 그대로 두고 문자표만 다시 채우는** 방식으로 했다.
⚠️ `TMP_FontAsset.CreateFontAsset` 으로 새로 만들면 **GUID 가 바뀌어 씬 참조가 전부 끊긴다.**

목표 문자 집합 = **ASCII 32~126 전부(95자)** + 실제 사용이 확인된 기호 6개
(`…`U+2026 `—`U+2014 `□`U+25A1 `·`U+00B7 `▲`U+25B2 `→`U+2192) + 여유 기호 14개
(`← ▼ – × • ★ ☆ © ® ° ± ≤ ≥ ∞`). 여유분은 폰트에 없으면 그냥 빠지므로 무해하다.

> 굽기 전 폰트에는 **ASCII 가 72자밖에 없었다** — `" # $ % & ' * ; < = > @ J Z \ ^ \` j q { | } ~` 23자가 빠져 있었다.
> Dynamic 이라 "쓰는 순간 구워지니까" 문제가 안 보였던 것뿐이고, Static 으로 굳히면 그대로 빈칸이 됐다.

### 검증 로그

```
[굽기]   ok=True  missing=''  chars=115  glyphs=115  atlases=1  mode=Static
[검증]   mode=Static  chars=115  atlas=1024x1024  texCount=1
         asciiMissing='(none)'   symMissing='(none)'
[콘솔]   Error 0 / Warning 0  (18장 삭제 후 Assets/Refresh)
```

| 지표 | 전 | 후 |
|---|---|---|
| `Assets/Fonts` 용량 | 55 MB | **4.8 MB** |
| `Pretendard SDF.asset` 줄 수 | 250,961 | **2,274** (−248,687) |
| 아틀라스 텍스처 | 2장 | **1장** |
| 문자표 | 144 (ascii 72 / 한글 66 / 기호 6) | **115 (ascii 95 / 기호 20)** |
| 커밋 취급 | 계속 staging 제외 | **정상 커밋** |

> 🔑 **이제 이 파일은 플레이해도 안 바뀐다.** 결정 1 종료.
> 단, **앞으로 새 기호를 UI 에 쓰려면 폰트를 다시 구워야 한다** — 안 그러면 빈칸으로 나온다.
> 재굽기 절차는 `BALANCE.md` 를 볼 것.

---

## 2-26. ✅ 체감 튜닝 문서 신설 — `TUNING.md` (I-59, 2026-08-29 22차)

### 왜 했나

사용자가 방침을 정했다 — **"수치는 완성 먼저 하고 진행하면서 조절한다."**

그때까지 미뤄 둔 체감 항목이 **문서 세 곳에 흩어져 있었다.**
`TODO.md` §0 의 "⚠️ 실플레이 체감 미확인" 꼬리표, §1 의 체크박스 더미, §3 의 자리표시 수치 표.
그래서 **"플레이하면서 뭘 보면 되는지"를 한 번에 볼 수 없었다.**

더 나쁜 건 `TODO.md` §1 이 성격이 다른 두 가지를 섞고 있었다는 점이다:

| | 질문 | 답 | 판정 방법 |
|---|---|---|---|
| 동작 검증 | "동작하나?" | 예/아니오 | 한 번 확인하면 **끝난다** |
| 체감 판정 | "느낌이 맞나?" | **숫자** | 고치고 다시 플레이하는 **반복 작업** |

섞여 있으면 §1 이 영원히 안 줄어든다. 체감 항목은 판정해도 **다음 수치로 넘어갈** 뿐이기 때문이다.

### 한 일

| 파일 | 변경 |
|---|---|
| `Docs/TUNING.md` | **신설.** §1 수치 위치표 · §2 체감 체크리스트(A~G) · §3 자리표시 수치표 · §4 세션 기록 |
| `Docs/TODO.md` | §1 에서 **체감 항목 전부 제거** → 동작 검증만 남김. §0·§6 에 방침·포인터 |
| `CLAUDE.md` | §1 에 `TUNING.md` 의 성격과 **3문서 경계**를 등록 |

`TUNING.md` §2 의 각 항목은 **"무엇을 보나 → 이상하면 어느 파일의 무엇을 얼마나"** 형식이다.
"체감 미확인" 이라고만 적어 두면 몇 달 뒤에 **무엇을 보려던 건지 알 수 없어서** 그렇다.

### 🔴 곁들여 발견 — 적 타격 반응이 CSV 에 없다

수치 위치표(§1)를 만들다 발견했다. I-44 로 넣은 타격 반응 중 **플레이어 쪽**은
`Economy.csv` 에 있는데 (`knockbackForce` 9 · `shakeMagnitude` 0.18 …)
**적 쪽 5개는 `EnemyBase.cs` 에 `const` 로 박혀 있다.**

| 값 | 현재 | 위치 |
|---|---:|---|
| `KnockbackForce` | 6.0 | `EnemyBase.cs:354` |
| `KnockbackTime` | 0.10 | `EnemyBase.cs:355` |
| `DeathPopTime` | 0.14 | `EnemyBase.cs:435` |
| 처치 흔들림 | 일반 `0.20/0.25s` · 보스 `0.45/0.5s` | `EnemyBase.cs:469` |
| 처치 히트스톱 | 일반 `0.05s` · 보스 `0.09s` | `EnemyBase.cs:471` |

**하필 이 5개가 체감 튜닝에서 가장 많이 만지게 될 값이다.** 한 번 고칠 때마다
도메인 리로드를 기다려야 해서 튜닝 루프가 몹시 느려진다.
`CLAUDE.md` §4 대로 **고치지 않고** `TUNING.md` §1 에 적고 사용자에게 알렸다.

> ⚠️ 옮길 때 걸림돌이 하나 있다 — **적은 프리팹 하나를 공유하므로 씬 컴포넌트가 아니다.**
> `Economy.csv` 임포터가 **프리팹 경로**를 다룰 수 있는지 먼저 확인해야 하고,
> 안 되면 `Enemies.csv` 의 열로 빼는 쪽이 맞다 (종별로 다르게 줄 수 있다는 장점도 있다).

### 검증

문서 작업이라 런타임 검증 대상이 없다. 대신 인용한 수치를 **전부 원본에서 다시 읽어** 맞췄다 —
`Economy.csv` · `SceneWiring.csv` · `EnemyBase.cs` · `PlayerController.cs`.
(`TODO.md` 에 적혀 있던 값을 그대로 옮기지 않았다. 그게 20차의 PPU 오판을 만든 방식이다)

---

## 2-25. ✅ 적 걷기 애니메이션 배선 (I-58, 2026-08-29 21차)

### 왜 했나

20차(I-57)에 적 걷기 시트 6장을 뽑아 놓고 **소비할 코드가 없어 그대로 뒀다**
(체험 기간이 끝나면 못 뽑는 자원이라 생성만 먼저 하기로 사용자와 합의). 그 배선이 이번 작업이다.

적은 항상 화면에 수십 마리가 있다. 그동안은 정지 그림 한 장이 셰이더 바운스로만 흔들려
**미끄러지듯 다가왔다.** 화면이 가장 크게 바뀌는 항목이었다.

### 🔴 시작하자마자 내 20차 커밋에서 버그를 찾았다 — PPU

`TODO.md` 에 "적 걷기 시트는 PPU 1024" 라고 적어 두고 그대로 커밋했는데, **틀렸다.**

| | rect | PPU | 월드 크기 |
|---|---:|---:|---:|
| `Slime.png` (정지) | 512 | 1024 | **0.500유닛** |
| `Slime_Walk.png` (걷기, 20차 커밋) | 256 | 1024 | **0.250유닛** ← 절반 |
| `Slime_Walk.png` (21차 교정) | 256 | **512** | **0.500유닛** |

그대로 배선했으면 **애니메이션이 켜지는 순간 적 6종이 전부 절반으로 줄어들었을 것이다.**

원인은 검증 방식이었다. 20차에는 `.meta` 의 `spritePixelsToUnits`(2048)와 플랫폼
`maxTextureSize`(512)를 눈으로 조합해 "0.25유닛" 이라고 **계산**했다. 실제로는 Unity 가
rect 를 축소된 텍스처 기준으로 다시 잡아 512/1024 = 0.5유닛이었다.
이번엔 계산하지 않고 `sprite.bounds.size.x` 를 **직접 물었다.** 애초에 그랬어야 했다.

> 🔑 **스프라이트 크기는 메타 파일로 추론하지 말고 `bounds.size` 로 물어볼 것.**
> `rect` · `pixelsPerUnit` · `maxTextureSize` 가 서로 물려 있어 손계산이 자주 틀린다.

### 무엇을 했나

`PlayerVisual` 이 `CharacterClassData.WalkFrames` 를 쓰는 구조를 적 쪽에 그대로 옮겼다.
**`EnemyVisual` 은 이미 있었다** (I-47 의 바운스·좌우 반전·피격 플래시 담당) — 새로 만들 필요가 없었고,
`Setup` 에 프레임 배열 인자 하나를 늘리는 것으로 끝났다.

| 파일 | 무엇을 |
|---|---|
| `Assets/Game/Sprites/Enemies/Walk/*.png` (6) | **PPU 1024 → 512** 교정 |
| `Scripts/Enemy/EnemyData.cs` | `public Sprite[] WalkFrames` 추가 |
| `Scripts/Enemy/EnemyVisual.cs` | `Setup(sr, moveSpeed, frames)` · `StepFrames()` 추가 |
| `Scripts/Enemy/EnemyBase.cs` | `Visual.Setup` 에 `Data.WalkFrames` 전달 |
| `Editor/BalanceImporter.cs` | `WalkSheet` 열 Import(`LoadSpriteSheet`) + Export |
| `Game/Balance/Enemies.csv` | `WalkSheet` 열 신설 + 6행 경로 + 머리말 주석 |
| `Game/EnemyData/*.asset` (6) | Import 산출물 |

설계에서 신경 쓴 것 세 가지:

- **풀 재사용 시 프레임 배열을 반드시 지운다.** 안 지우면 시트가 있는 늑대가 죽고 그 자리에
  재사용된 슬라임이 **늑대 프레임으로 걷는다.** 적은 프리팹 하나를 6종이 공유하므로
  이 실수의 대가가 크다 (I-47 이 쿨다운·조향 상태에 대해 같은 처리를 해 둔 것과 같은 이유)
- **시작 프레임을 개체마다 무작위로 어긋나게** 준다. `AnimPhase` 를 어긋나게 준 것과 같은 이유 —
  같은 프레임에 스폰된 무리가 발까지 맞추면 "한 몸"으로 보인다
- **걸음 속도는 자기 `MoveSpeed` 를 1배로 본다.** 절대 속도로 하면 오우거(1.2)가 제자리걸음처럼
  보이고 늑대(4.2)는 발이 헛돈다. 돌진 중에는 자연히 빨라진다(상한 2.5배)

### 검증 로그

```
[PPU] Slime/Goblin/Zombie/Wolf/Demon/Ogre  정지 0.500 | 걷기 0.500 (16장) ppu 512   ← 6/6
[BalanceImporter] Import 완료  Enemies : 6   (경고 없음)
[E] 6종 전부 frames=16  frame_0..frame_15  걷기 0.500 / 정지 0.500유닛
[P] Enemy_Goblin.prefab EnemyVisual = 있음
```

런타임(플레이 모드, Warrior 로 1층 전투 진입):

```
[R-A] 활성 4  | frame_0(w256)@1.6 frame_4@1.6 frame_8@1.6 frame_12@1.6
[R-B] 활성 10 | frame_0@1.6 frame_13@4.2 frame_5@1.6 frame_9@1.6 frame_13@1.6 frame_1@1.6
```

- `w256` — 정지 그림(512)이 아니라 **걷기 프레임**이 렌더러에 올라가 있다
- A→B 에서 인덱스가 **바뀐다** = 프레임이 실제로 넘어간다
- 같은 시각에 개체마다 인덱스가 **다르다** = 무리가 발을 맞추지 않는다
- `@4.2` 는 늑대(`MoveSpeed` 4.2) — 종별 속도가 그대로 반영된다

게임 카메라 캡처로 늑대가 **플레이어의 절반 크기**로 정상 표시되는 것까지 확인했다.
플레이 종료 후 콘솔 Error/Warning **0건**.

### 남은 것

적의 **공격·사망 모션은 여전히 없다.** 걷기 시트만 있다. 사망은 스케일 팝(I-44)으로 대신한다.

---

## 2-24. ✅ 승급 직업·진화 무기·적 걷기 애셋 17장 (I-57, 2026-08-29 20차)

### 왜 했나

19차(I-56)로 직업 승급이 **동작하게** 됐지만, 승급해도 **화면에서는 아무 일도 일어나지 않았다.**
`Classes.csv` 의 `Portrait`/`BodySprite`/`WalkSheet` 세 열이 승급 4행에서 전부 비어 있었고,
`PlayerStats.ApplyClassVisual` 은 그 경우 조기 반환한다 — 승급 전 모습이 그대로 유지된다.
게임에서 가장 큰 성취인 승급이 **로그로만 존재**했다.

진화 무기 3종도 같은 상태였다. `Items.csv` 의 `Icon` 이 재료 무기의 아이콘을 그대로 재사용해서
레벨업 카드에서 `Sword` 와 `Excalibur` 가 **같은 그림**으로 떴다.

그리고 애셋 생성을 **먼저** 한 이유가 하나 더 있다 — Unity AI 체험 기간이 끝나면
포인트 할당이 끊기고, 그건 이 백로그에서 **되돌릴 방법이 없는 유일한 항목**이다
(코드·수치는 언제든 고칠 수 있다). [`TODO.md`](TODO.md) §6 이 애셋 생성을 0순위로 둔 근거다.

### 만든 것 — 17장

| 분류 | 파일 | 참조 원화 | 비고 |
|---|---|---|---|
| 승급 원화 4 | `Sprites/Classes/{Sentinel,Doomlord,Warden,Aegis}.png` | Ranger / Mage / Warrior / **Sentinel** | 1024², T1 을 참조로 넘겨 "같은 인물의 강화형" |
| 승급 걷기 4 | `Sprites/Classes/Walk/{Id}_Walk.png` | 위 4장 | 1024² · 4×4 16프레임 · PPU **256** |
| 진화 아이콘 3 | `Sprites/Weapons/{Excalibur,Windforce,Devastator}.png` | Sword / Bow / Gun | 512² · Single · PPU **512** |
| 적 걷기 6 | `Sprites/Enemies/Walk/{Slime,Goblin,Zombie,Wolf,Demon,Ogre}_Walk.png` | 각 정지 스프라이트 | 1024² · 4×4 16프레임 · PPU **1024** |

**Aegis 만 T1 이 아니라 `Sentinel` 결과물을 참조로 썼다.** `ClassEvolutions.csv` 에서
`Aegis.FromClass = Sentinel` 이므로, 데이터상의 사슬을 그림에서도 그대로 이었다 —
Aegis 가 Sentinel 의 실루엣·색계열을 유지한 채 두꺼워진다.

### 바꾼 파일

| 파일 | 변경 |
|---|---|
| `Assets/Game/Balance/Classes.csv` | 승급 4행의 `Portrait` / `BodySprite` / `WalkSheet` 채움 |
| `Assets/Game/Balance/Items.csv` | 진화 3행의 `Icon` 을 재료 무기 → 전용 아이콘으로 교체 |
| `Assets/Game/ClassData/{Sentinel,Doomlord,Warden,Aegis}.asset` | Import 산출물 |
| `Assets/Game/ItemData/{Excalibur,Windforce,Devastator}.asset` | Import 산출물 |
| `Assets/Game/Sprites/**` (17 png + meta) | 신규 |

### 🔴 I-41 이 그대로 재현됐다 — 그리고 해법을 찾았다

CLAUDE.md 가 경고한 **"AI 스프라이트의 투명 배경은 가짜"** 가 이번에도 **17장 전부**에서 나왔다.
알파가 전 픽셀 `255` 이고 흰 배경이 RGB 에 그려져 있다. 그대로 넣으면 캐릭터마다
**흰 사각형이 따라다닌다.**

지금까지의 대응은 "재생성"이었지만(그래도 알파는 가짜였다), 이번에 실제로 통하는 경로를 찾았다:

```
Unity_AssetGeneration_GenerateAsset(command: "RemoveImageBackground", targetAssetPath: <png>)
```

- `savePath` 는 **무시된다.** 원본을 제자리에서 고치므로 **GUID 가 보존**된다 (배선이 안 끊긴다)
- `referenceImageInstanceId` 로는 안 된다 — `'targetAssetPath' is required` 로 거절당한다
- **스프라이트시트의 4×4 슬라이싱도 보존된다** (16프레임 · `frame_0` 그대로)

배경 제거 전후 (`Warrior` = 정상 T1 기준):

```
[A] Warrior   alpha 0~255    transparent 62.5%  OK        ← 기준
[A] Sentinel  alpha 255~255  transparent  0.0%  FAKE-ALPHA  →  alpha 0~255  72.2%  OK
[A] Doomlord  alpha 255~255  transparent  0.0%  FAKE-ALPHA  →  alpha 0~255  71.2%  OK
[A] Warden    alpha 255~255  transparent  0.0%  FAKE-ALPHA  →  alpha 0~255  54.5%  OK
[A] Aegis     alpha 255~255  transparent  0.0%  FAKE-ALPHA  →  alpha 0~255  58.4%  OK
```

### 함정 — 생성물의 임포터 설정이 규약을 안 따른다

생성 직후 PPU 가 **텍스처 크기와 같게** 박힌다(1024² → PPU 1024). 그대로 두면 크기가 어긋난다.
전부 기존 규약에 맞춰 다시 임포트했다:

| 종류 | 생성 직후 | 교정 후 | 근거 |
|---|---|---|---|
| 승급 걷기 시트 | PPU 1024 | **256** | `Warrior_Walk` 와 동일 (프레임 256px = 1유닛) |
| 진화 아이콘 | PPU 1024 | **512** | `Sword.png` 와 동일 |
| 적 걷기 시트 | PPU 1024 | **1024** | `Slime.png` 가 512px/PPU 2048 = **0.25유닛**. 프레임 256px 이므로 1024 가 같은 크기 |

> 요청 해상도도 무시된다 — `width/height: 512` 로 요청해도 산출물은 1024² 다.
> 아이콘은 `maxTextureSize 512` 로 눌러 기존 무기 아이콘과 맞췄다.

### 검증

CSV Import → `File/Save` 후 SO 를 직접 읽었다.

```
[V] Warrior   portrait=Warrior   body=frame_0  walkFrames=16  first=frame_0
[V] Ranger    portrait=Ranger    body=frame_0  walkFrames=16  first=frame_0
[V] Mage      portrait=Mage      body=frame_0  walkFrames=16  first=frame_0
[V] Sentinel  portrait=Sentinel  body=frame_0  walkFrames=16  first=frame_0
[V] Doomlord  portrait=Doomlord  body=frame_0  walkFrames=16  first=frame_0
[V] Warden    portrait=Warden    body=frame_0  walkFrames=16  first=frame_0
[V] Aegis     portrait=Aegis     body=frame_0  walkFrames=16  first=frame_0

[C] Sword=Sword  Bow=Bow  Gun=Gun
[C] Excalibur=Excalibur  Windforce=Windforce  Devastator=Devastator

[E] Slime/Goblin/Zombie/Wolf/Demon/Ogre  1024x1024  alpha 0~255  sprites 16  ppu 1024  OK
```

**승급 4종이 T1 3종과 완전히 같은 형태**(포트레이트 + 16프레임 걷기)가 됐다.

> ⚠️ **`BodySprite` 만 채워서는 소용없다.** `PlayerVisual` 이 `sr.sprite` 를 걷기 프레임 0 으로
> 덮어쓰므로 `WalkSheet` 이 있어야 겉모습이 바뀐다. 그래서 T1 과 똑같이 **두 열 모두** 시트 경로를 적었다.

### ⚠️ 적 걷기 시트 6장은 **아직 아무 데도 안 붙어 있다** (사용자 승인)

걷기 프레임을 소비하는 코드는 `PlayerVisual` **하나뿐**이다. `EnemyData` 에 `WalkFrames` 필드가,
`Enemies.csv` 에 `WalkSheet` 열이 없어서 시트를 넣을 자리가 없다.

사용자에게 물었고 **"지금 뽑고 배선은 다음에"** 로 결정됐다 — 애셋 생성만이 되돌릴 수 없는 자원이기 때문.
배선(= `EnemyVisual` 컴포넌트 + `EnemyData.WalkFrames` + CSV 열 + 임포터 대응)은
[`TODO.md`](TODO.md) §1 에 **I-58** 로 남겼다.

> ⚠️ 여기서도 로그가 증명한 것은 **"배선됐다"까지다.** 승급했을 때 실제로 그림이 바뀌는지,
> 새 아이콘이 카드에서 구분되는지는 **눈으로만** 확인할 수 있다 → [`TODO.md`](TODO.md) §1

---

## 2-22. ✅ 직업별 소지 상한 (I-55, 2026-08-29 18차)

### 왜 했나

사용자 요구: **"각 캐릭터당 소지가능한 무기, 패시브, 건물 개수도 다르게"**.

배경은 이렇다. 지금까지 3직업을 가르는 건 `Bonus*` 스탯뿐이었는데 그 차이가 몇 퍼센트라
**플레이 중에 체감되지 않았다.** 시작 무기만 다르고 나머지는 같은 게임이 세 번 도는 셈이다.
소지 상한은 성격이 다르다 — 무기를 3종밖에 못 드는 것과 5종을 다 드는 것은
**런 전체의 모양**이 달라진다. 스탯이 "얼마나 센가"라면 상한은 "무엇을 할 수 있는가"다.

### 상한을 어디에 둘 것인가

`WeaponManager` 에 이미 `maxWeaponSlots = 6` 이 있었지만 **무기 전용**이었고,
건물·패시브에는 상한이라는 개념 자체가 없었다. 세 곳에 각각 숫자를 두면 직업마다 다르게 만들 수 없다.

```
CharacterClassData.Max{Weapon,Passive,Building}Slots   ← 원본 (Classes.csv)
        ↓
PlayerStats.SlotLimit(ItemCategory)                    ← 조회 단일 창구
        ↓
LevelUpManager.CanAcquire(ItemData)                    ← 판정 단일 창구
        ↓          ↘
   PickCandidates   ApplyItem
   (카드 · 상점)     (지급)
```

**`PickCandidates` 하나가 레벨업 카드와 상점 진열 양쪽을 먹인다**
(`GetShopCandidates(count) => PickCandidates(count)`). 그래서 거기 한 줄만 막으면 두 화면이 같이 잡힌다.

`WeaponManager.maxWeaponSlots` 는 **삭제**했다. `Economy.csv` 에 그 행이 없는 것을 먼저 확인했다
(§3 규칙 — C# 필드를 지우면 CSV 행도 같이 지워야 한다).
`BuildingManager` 에는 별도 상한을 넣지 않았다 — `UnlockBuilding` 의 유일한 호출자가 `ApplyItem` 이라
그 앞의 가드가 이미 덮는다. 두 곳에 검사를 두면 서로 어긋날 때를 걱정해야 한다.

### ⚠️ 부수 발견 — "보유 중인데 무기는 없는" 유령 아이템

작업 중 드러난 기존 버그다. 상한이 6이라 잠복해 있었지만 **상한 3을 넣는 순간 즉시 터진다.**

```
[예전] 무기 슬롯이 찬 상태로 새 무기 카드를 고른다
   ApplyItem      → _inventory[item]++ · item.CurrentLevel = 1     (기록됨)
   AddOrUpgrade   → Debug.LogWarning 후 조용히 return              (무기는 안 생김)
```

결과는 단순한 "무기 하나 손해"가 아니다. 그 아이템은 이후 **보유로 취급**되어

- 카드 가중치가 `OwnedWeight = 2` 배가 된다 → **없는 무기의 레벨업 카드가 계속 뜬다**
- `GetItemLevel` 이 레벨을 돌려준다 → **진화 재료 판정을 통과한다**

`ApplyItem` 맨 앞의 `if (!CanAcquire(item)) return;` 로 막았다.
`WeaponManager` 쪽 검사는 **지우지 않고 경고 로그로 남겼다** — 조용히 return 하던 그 자리가
정확히 이 사고의 원인이었으므로, 같은 경로가 다시 생기면 로그로 드러나야 한다.

### 변경한 파일

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Player/CharacterClassData.cs` | `MaxWeaponSlots` / `MaxPassiveSlots` / `MaxBuildingSlots` 3필드 + 헤더 |
| `Assets/Scripts/Player/PlayerStats.cs` | `SlotLimit(ItemCategory)` 신설. **직업이 없으면 `int.MaxValue`** |
| `Assets/Scripts/Weapon/WeaponManager.cs` | `maxWeaponSlots` 필드 **삭제** → 직업 값 조회. 검사는 경고 로그로 존치 |
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | `CanAcquire` / `CountOwned` 신설 · `PickCandidates` 필터 · `ApplyItem` 가드 |
| `Assets/Game/Balance/Classes.csv` | 3열 추가 + 3직업 차등 + 머리말 설명 |
| `Assets/Editor/BalanceImporter.cs` | `ImportClasses` 읽기 3줄 · `ExportRows` 헤더/값 3열 |
| `Assets/Game/ClassData/*.asset` | Import 로 재생성 (3종) |

### 수치와 근거

| Id | 무기 | 패시브 | 건물 | 의도 |
|---|---:|---:|---:|---|
| Warrior | **3** | 5 | **5** | 무기는 적게, **건물은 전부**. 진지 구축형 |
| Ranger | **5** | 4 | **2** | **무기 전종**, 건물은 거의 못 세운다. 순수 화력형 |
| Mage | 3 | **8** | 3 | 무기·건물이 좁은 대신 패시브로 큰다 |

콘텐츠 총량이 **무기 5 · 건물 5 · 패시브 10** 이므로 Warrior 의 건물 5 와 Ranger 의 무기 5 는
**의도적으로 상한 없음**이다 — "이 축은 자유롭다"를 표현한 것이다.

`Bonus*` 스탯은 **일부러 건드리지 않았다.** 이미 차등화되어 있고,
[`TODO.md`](TODO.md) §3 에서 실플레이 후 재조정 대상으로 이미 잡혀 있다.
한 번에 두 축을 같이 흔들면 어느 쪽이 효과를 냈는지 알 수 없다.

### 검증 로그

`CLSVERIFY` (에디트 모드 — CSV Import 결과가 SO 에 실제로 들어갔는가)

```
[CLS] Warrior  weapon=3 passive=5 building=5 start=Sword    hp=+30 armor=+2
[CLS] Ranger   weapon=5 passive=4 building=2 start=Bow      hp=-15 speed=+0.6
[CLS] Mage     weapon=3 passive=8 building=3 start=Fireball hp=-25 dmg=+0.2
[CLS] 콘텐츠 총량 : 무기 10(진화 포함) · 건물 5 · 패시브 10
```

`SLOTTEST1` (플레이 모드 — 상한이 실제로 걸리는가 · 유령 기록이 남는가)

```
[SLOT] 직업=Warrior 상한 무기=3 패시브=5 건물=5
[SLOT] Sword     CanAcquire=True  → 보유무기종류=1 실제장착=1 HasItem=True
[SLOT] Bow       CanAcquire=True  → 보유무기종류=2 실제장착=2 HasItem=True
[SLOT] Gun       CanAcquire=True  → 보유무기종류=3 실제장착=3 HasItem=True
[SLOT] Fireball  CanAcquire=False → 보유무기종류=3 실제장착=3 HasItem=False
```

마지막 줄이 핵심이다 — **거절되었고 `_inventory` 에도 안 남았다.** 예전이면 `HasItem=True` 인데
`실제장착=3` 인 어긋난 상태가 됐다.

`SLOTTEST2` (플레이 모드 — 후보 풀 · 다른 카테고리 · 진화 상호작용)

```
[SLOT] 후보 60장 : 신규무기=0 (0 이어야 함) · 보유무기업글=16 · 그 외=44
[SLOT] 건물 보유=5 / 상한 5
[SLOT] 패시브 Damage=O Speed=O AttackSpeed=O MaxHp=O Armor=O PickupRadius=X XpGain=X → 보유=5 / 상한 5
[SLOT] 진화 전 : Sword Lv5 · Damage Lv3 · 무기 3/3
[SLOT] 준비된 레시피 1개 : Excalibur
[SLOT] 진화 후 : Excalibur Lv1 · Sword Lv0 · 무기 3/3 · 실제장착=3
```

- **신규무기 0 · 보유무기업글 16** — 상한이 찼어도 **육성 카드는 계속 뜬다.** 상한은 칸만 막는다
- **건물·패시브도 동일하게 동작** — 무기 전용 로직이 아님이 확인됐다
- **꽉 찬 상태에서도 진화가 성사된다** — `Evolve()` 가 재료를 **먼저** 소모하기 때문이다.
  순서가 반대였다면 여기서 조용히 실패했을 것이다 (17차에 이미 그 순서로 짜 둔 게 여기서 값을 했다)

컴파일: `Assets/Refresh` 후 콘솔 **0건**.
검증은 전부 `Unity_RunCommand` 로만 했다 — **임시 MonoBehaviour 나 씬 오브젝트를 만들지 않았으므로 정리할 잔여물이 없다.**

---

## 2-21. ✅ 상점 가시성 + 무기 진화 3조합 (I-53~I-54, 2026-08-29 17차)

> 사용자 지시 두 건. **① 상점 UI 가 너무 작다** (스크린샷 첨부), **② 무기 진화를 넣되
> "패시브+무기 / 무기+무기" 뿐 아니라 `패시브·무기·건물` 세 종류의 조합이 전부 가능한 구조로** 설계할 것.

### I-53 — 상점 UI 가시성

**원인.** 상점 카드는 `160×220`, 아이콘 `55×55`, 설명 글자 `10pt` 였다.
1920×1080 에서 한 카드가 화면 세로의 **1/5** 을 차지하지 못하고, 10pt 는
게임을 플레이하는 거리(모니터 앞 60~70cm)에서 **읽으려면 몸을 기울여야 하는 크기**다.
레벨업 카드는 11차에 이미 한 번 키웠는데(사용자: "뱀파이어 서바이벌처럼 한눈에 딱")
**상점만 옛날 치수로 남아 있었다.**

덤으로 두 가지가 같이 걸렸다 —
① 카드 라벨이 `무기`/`건물`/`패시브`/`신규` 로 **한글**이었다 (CLAUDE.md §3: UI 문자열은 영문).
② 리롤 버튼이 `"🔀 리롤 ({cost}G)"` 였는데 **U+1F500 이 Pretendard SDF 에도 폴백에도 없다.**
16차에서 💰(U+1F4B0)로 똑같이 데였던 자리다 — 콘솔 경고와 함께 `␡` 가 그려진다.

| 파일 | 변경 |
|---|---|
| `Assets/Prefabs/Prefab_ShopCard.prefab` | 카드 **160×220 → 250×440** · 아이콘 **55×55 → 110×110** · 글자 `13→24` `14→24` `20→38` `11→17` `10→18` |
| `Assets/Prefabs/Prefab_ShopRemoveRow.prefab` | **200×50 → 240×70** |
| `Assets/Scenes/SampleScene.unity` | 상점 패널 4종 `700×420→820×460` `400×700→440×900` `760×700→900×900` `360×560→400×760` · 간격 `16→22` `6→10` · 글자 `20→26` `22→30` `26→34` `18→24` |
| `Assets/Scripts/UI/ShopCardUI.cs` | `무기`/`건물`/`패시브` → `WEAPON`/`BUILDING`/`PASSIVE`, `신규` → `NEW` |
| `Assets/Scripts/UI/ShopRemoveRowUI.cs` | 위와 동일 |
| `Assets/Scripts/UI/ShopUI.cs` | NPC 대사 5종 영문화 · 리롤 `🔀 리롤 (nG)` → **`Cost n G`** · KILLS/LEVEL/TIME 라벨에 `<color=#8A8F98>` |

> ⚠️ **Screen Space Overlay 캔버스는 `Unity_Camera_Capture` 에 안 찍힌다.**
> 그래서 검증은 그림이 아니라 **프리팹/씬 YAML 의 수치를 직접 읽어** 했다.
> **눈으로 보는 확인은 아직 안 했다** → [`TODO.md`](TODO.md) §1

### I-54 — 무기 진화 (패시브 · 무기 · 건물 3조합)

**원인.** 진화 자체가 없었다. 그런데 [`ROADMAP.md`](ROADMAP.md) §2-3 에 적혀 있던 **원래 설계안이
사용자 요구를 구조적으로 표현할 수 없었다.**

```
(폐기된 안)  ItemData.EvolvesInto  +  ItemData.RequiredPassive
```

이 모양은 "무기 1 + 패시브 1" 만 쓸 수 있다. **건물 + 무기**도, **무기 + 무기**도 못 적는다.
그래서 아이템에 필드를 붙이는 대신 **레시피를 별도 SO 로 분리**했다.

```
EvolutionData :  ItemData[] Ingredients  +  int[] RequiredLevels  →  ItemData ResultItem
```

`ItemData` 는 원래부터 `WeaponRef`/`BuildingRef`/`PassiveRef` + `ItemCategory` 를 함께 든
**단일 타입**이다. 재료를 `ItemData[]` 로 두는 순간 레시피는 **카테고리를 신경 쓰지 않게** 되고,
3조합이 특별 취급 없이 그냥 표현된다. 재료 개수도 가변이 된다.

**전달 경로는 열이 아니라 재료가 정한다.** (사용자 결정: "보물상자 1안으로 가되 최종 진화는
해당 건물 앞에서 상호작용 키")

```csharp
public BuildingData AltarBuilding   // 재료 중 첫 Building 의 BuildingRef, 없으면 null
public bool IsFinalEvolution => AltarBuilding != null;
```

플래그 열(`DeliveryType`)을 두지 않은 이유는 **데이터만으로 깨진 상태가 만들어지기 때문**이다 —
"건물이 재료인데 상자에서 나온다" 같은 조합을 CSV 가 허용해 버린다. 파생값이면 모순이 불가능하다.
건물은 이미 `BuildingManager._placedBuildings` 로 맵에 실재하므로 **제단 프리팹도 필요 없다.**

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Evolution/EvolutionData.cs` | **신규** — 레시피 SO. `AltarBuilding` / `IsFinalEvolution` 은 재료에서 파생 |
| `Assets/Scripts/Evolution/EvolutionManager.cs` | **신규** — 싱글턴. `IsSatisfied` / `GetReadyEvolutions` / `Evolve` / `TryOfferChestEvolution` / `FindAltarEvolution` / `TryEvolveAtAltar` / `ResetRunState` |
| `Assets/Scripts/UI/EvolutionPromptUI.cs` | **신규** — HUD 하단 3단계 안내 (① 지금 E ② 상자에서 ③ 제단으로) |
| `Assets/Game/Balance/Evolutions.csv` | **신규** — 레시피 5종 |
| `Assets/Game/Balance/Items.csv` · `Weapons.csv` | 진화 결과 5종 추가 (아이템 20 → **25**, 무기 5 → **10**) |
| `Assets/Game/Balance/SceneWiring.csv` | `EvolutionManager,allEvolutions` 행 추가 |
| `Assets/Editor/BalanceImporter.cs` | `Evolutions` 단계 추가. **Items 단계의 `SaveAssets`+`Refresh` 뒤에 따로** 돌려야 한다 (아래 ⚠️) |
| `Assets/Scenes/SampleScene.unity` | `ExperienceManager` 에 `EvolutionManager` 부착 · `HUD` 에 `EvolutionPromptUI` + `EvolutionPromptText` 신설 |
| `Assets/Scripts/LevelUp/LevelUpManager.cs` | **`ShowForcedChoices(choices, onSelected)`** — 후보를 강제 지정해 같은 패널을 띄운다. 진화는 결과를 주기 **전에 재료를 소모**해야 해서 `ApplyItem` 을 그냥 태울 수 없다. 리롤은 막는다(후보 1장을 다시 뽑는다는 개념이 없다) |
| `Assets/Scripts/Experience/ExperienceManager.cs` | `GrantChestReward()` 가 **진화를 먼저** 시도. 무기를 다 키워 놓고도 상자에서 평범한 카드만 나오면 "모아 봐야 도착점이 없다"가 된다 |
| `Assets/Scripts/Building/BuildingManager.cs` | `FindNearestPlaced(origin, radius)` — 제단 판정용 최근접 설치 건물 조회 |
| `Assets/Scripts/Building/BuildingBase.cs` | `DataRef` 노출 — 인스턴스가 어떤 건물인지 알아야 재료와 대조할 수 있다 |
| `Assets/Scripts/Core/GameManager.cs` | `EvolutionMgr` 프로퍼티 + `StartRun()` 에서 `ResetRunState()` 호출 |
| `Assets/Scripts/Player/PlayerController.cs` | `E` 키 → `TryEvolveAtAltar(transform.position)`. 조건이 안 맞으면 **아무 일도 안 한다** — 실패음도 안 낸다 |

**레시피 5종 — 3조합이 전부 들어 있다.**

| 결과 | 재료 | 조합 | 전달 |
|---|---|---|---|
| Excalibur | Sword Lv5 + Damage Lv3 | 무기 + **패시브** | 보물상자 |
| Windforce | Bow Lv5 + CritChance Lv3 | 무기 + **패시브** | 보물상자 |
| Devastator | Gun Lv5 + Fireball Lv5 | **무기 + 무기** | 보물상자 |
| Sentinel | Gun Lv5 + Turret Lv3 | **무기 + 건물** | **제단(E)** |
| Doomsday | Bomb Lv5 + Bombard Lv3 | **무기 + 건물** | **제단(E)** |

**소모 규칙 — 무기 재료만 사라진다.** (사용자 미응답 → 기본값 선택 후 명시)
① 제단 앞에서 눌렀는데 **제단이 증발**하면 납득이 안 된다.
② `WeaponManager` 슬롯이 **6칸 상한**이라 재료 무기를 먼저 비우지 않으면
`AddOrUpgradeWeapon` 이 경고만 남기고 **조용히 거절**한다.
③ 패시브는 스탯 누적이라 회수하면 진화가 오히려 손해가 된다.

> ⚠️ **`BalanceImporter` 단계 순서.** `LoadById` 는 `AssetDatabase.LoadAssetAtPath` 를 쓰는데
> **같은 `StartAssetEditing` 블록 안에서 만든 애셋은 보이지 않는다.** 진화는 아이템을 참조하므로
> Items 단계가 `SaveAssets()` + `Refresh()` 로 끝난 **뒤에** Evolutions 단계가 시작돼야 한다.

> ⚠️ **진화 결과 5종을 `LevelUpManager,allItems` 에 넣지 말 것.** 레벨업 카드에 그냥 떠 버린다.
> 대신 `allItems` 밖에 있으므로 `LevelUpManager.ResetRunState()` 가 이들의 `CurrentLevel` 을
> 못 지운다 — `EvolutionManager.ResetRunState()` 가 **직접** 0 으로 되돌린다 (I-17 과 같은 함정).

**검증 중 고친 것 — 안내 문구가 한 번도 안 떴다.**
`EvolutionPromptUI` 를 `StageMap` 에서도 뜨게 짰는데 `[EVOTEST]` 로그가 `HUD active=False` 를
찍었다. **HUD 자체가 `StateVisibilityBinder` 로 Wave/LevelUp/Paused 에서만 켜진다** —
`StageMap` 분기는 **도달 불가**였다. 반대로 레벨업 카드 위에 안내가 겹칠 여지가 있어
`GameState.Wave` **하나만** 남겼다.

### 검증 로그

CSV Import — `!` 줄 없음:

```
Weapons: 10 · Buildings: 5 · Passives: 10 · Enemies: 6 · Items: 25
Waves: 6 · Classes: 3 · Evolutions: 5 · Events: 5
Economy 31/31 · SceneWiring 10/10
[EVOWIRE] ExperienceManager 에 붙음 · allEvolutions = 5개 · altarRadius = 2.2
          allItems = 20개 · 진화 결과 유출 = 0건
[EVOUI]   생성 완료 · font=Pretendard SDF · promptText=OK
```

Play 모드 — 한 세션에서 두 경로 전부:

| 검증 | 로그 |
|---|---|
| 상자 경로 조건 | `Sword Lv5 · Damage Lv3` → `준비된 레시피 1개 : Excalibur(최종=False)` → `상자 제안 = True · state = LevelUp` |
| 상자 경로 소모 | `Excalibur Lv1 · Sword Lv0 · Damage Lv3` · `무기=[Excalibur]` |
| 제단 경로 조건 | `Gun Lv5 · Turret Lv3` → `근처건물=Turret` → `제단 판정 = Sentinel` → `E 진화 = True` |
| 제단 경로 소모 | `Sentinel Lv1 · Gun Lv0 · Turret Lv3` · **`건물 여전히 존재=True`** |
| 프롬프트 ② | `<color=#8A8F98>EVOLUTION READY</color>  Windforce  —  open a treasure chest` |
| 프롬프트 ① | `<color=#F0C040>[E]</color>  EVOLVE  —  Doomsday` |

`Assets/Refresh` 후 **콘솔 0건**(`Types:["All"]`), `File/Save` 완료.
검증은 `Unity_RunCommand` 로만 했으므로 **씬에 남긴 임시 오브젝트·스크립트가 없다.**

> ⚠️ 프롬프트 **③**("stand by your \<Building\> and press E")은 화면에 직접 띄우지 못했다.
> 준비된 레시피가 2개이고 그중 하나가 비-최종이면 ②가 항상 먼저 이긴다.
> 같은 코드 경로의 문자열 하나라 위험은 낮다고 보고 넘겼다 → [`TODO.md`](TODO.md) §1

---

## 2-1. 이슈 목록 (I-1 ~ I-61 · 25차부터 `D`/`C`/`B`)

> 미해결 항목은 [`TODO.md`](TODO.md), **깨진 것**은 [`Parallel/BUGS.md`](Parallel/BUGS.md) 참조.

| # | 이슈 | 상태 |
|---|---|---|
| I-1 | Player `Rigidbody2D.gravityScale = 1` (탑다운인데 아래로 떨어짐) | ✅ 해결 |
| I-2 | Player에 `Collider2D` 없음 | ✅ 해결 |
| I-3 | 한글 TMP 폰트 없음 (모든 한글이 `□`) | ✅ 해결 — Pretendard |
| I-4 | CanvasScaler = Constant Pixel Size @ 800x600 | ✅ 해결 |
| **I-5** | **Player `tag = Untagged`** — `ExpDrop`/`EnemyBase`가 `"Player"` 태그로 찾으므로 **경험치 획득·접촉 데미지가 전부 무효**였음 | ✅ 해결 (I-1 작업 중 발견) |
| **I-6** | **Enemy/Projectile 프리팹도 `gravityScale = 1`** — 적과 총알이 아래로 떨어짐 | ✅ 해결 |
| **I-7** | **`Proj_Bullet` Rigidbody2D가 Dynamic** — `ProjectileBase`는 `transform.Translate`로 이동시켜 물리와 충돌 | ✅ 해결 → Kinematic |
| **I-8** | **스크립트 실행 순서 경쟁** — `GameManager.Start()`가 매니저 참조를 채우는데 `ShopUI.Start()`가 먼저 돌아 `NullReferenceException` | ✅ 해결 (Play 검증에서 발견) |
| **I-9** | **`MetaProgressionManager.upgrades[0] = NULL`** — `GetStatBonus()`의 `foreach`에서 `NullReferenceException` | ✅ 해결 (Play 검증에서 발견) |
| **I-10** | **런 시작 경로가 없음** — `GameManager.StartRun()` 호출자가 **0개**. MainMenu UI / StageMap UI **스크립트 자체가 미작성** | ✅ 해결 (2026-08-26, 스크립트 5종 신규 작성 → 2-2 참조) |
| **I-11** | `AudioManager` / `StageClearUI` 스크립트가 씬 어디에도 없음 | ✅ 해결 (씬 배치 + 호출부 연결) |
| **I-12** | **`WaveData` 3종(`Normal1`/`Elite1`/`Boss1`)의 `Spawns`가 전부 빈 배열** — 웨이브가 시작돼도 **적이 한 마리도 스폰되지 않음** | ✅ 해결 (2026-08-26) |
| **I-13** | **`CameraController` NaN 오염** — `UpdateLookAhead()`가 `delta / Time.deltaTime`을 하는데 `timeScale == 0`(일시정지/레벨업/클리어)에서 `0/0 = NaN`. 한 번 발생하면 `_lookAheadPos`와 카메라 위치가 영구 오염되어 매 프레임 에러 스팸 | ✅ 해결 (2026-08-26) |
| **I-14** | **Shop 노드를 지나도 맵이 진행되지 않음** — `CloseShop()`이 `AdvanceToNext(node)`를 부르지 않아 같은 층을 무한 반복. **상점을 고르면 런이 사실상 끝남** | ✅ 해결 (2026-08-26 2차) |
| **I-15** | **Event가 웨이브를 띄우면 HUD가 사라짐** — `TriggerRandomWave` 분기에 `ChangeState(Wave)` 누락. 상태가 `Event`로 남아 어떤 패널도 안 뜬 채 전투 진행 | ✅ 해결 |
| **I-16** | **ESC 일시정지가 아무 상태에서나 열리고, 닫으면 무조건 `Wave`가 됨** — 메뉴/맵/상점에서 ESC→닫기 시 게임이 `Wave` 상태로 착각 | ✅ 해결 |
| **I-17** | **Retry 시 아이템 레벨이 이전 런에서 이월** — `ItemData.CurrentLevel`은 SO 애셋에 쓰는 런타임 값이고 `[NonSerialized]`는 **도메인 리로드에서만** 초기화된다. `ReloadScene()`은 씬만 다시 로드하므로 값이 남음 | ✅ 해결 |
| **I-18** | `SaveData.TotalRuns` / `TotalKills` 가 아무 데서도 증가하지 않음 (죽은 필드) | ✅ 해결 |
| **I-19** | **`WaveData` 가 `WaveManager.cs` 안에 정의되어 있어** 새로 만든 `.asset` 의 `m_Script` 가 `0`으로 기록됨 (`No script asset for WaveData` 경고). 기존 `Normal1`/`Elite1`/`Boss1` 도 이미 깨져 있었음 | ✅ 해결 (2026-08-26 2차) |
| **I-20** | **적에 닿으면 체력이 순식간에 증발** — `EnemyBase.OnTriggerStay2D` 가 `ContactDamage × fixedDeltaTime`(≈0.16)을 매 물리 프레임 넣는 지속 피해였는데, `PlayerStats.TakeDamage` 의 `Mathf.Max(1, raw - Armor)` **바닥값이 그 0.16을 1로 올려** 적 하나당 **초당 50 피해**가 됐다. 무적 시간도 없어 여러 마리가 겹치면 즉사 | ✅ 해결 (2026-08-26 3차) |
| **I-21** | **플레이어 전 스탯이 2배** — `MetaProgressionManager.GetStatBonus()` 가 `new StatBlock()` 을 **보너스 블록**으로 쓰는데 `StatBlock` 의 필드 초기값이 기본 스탯(`MaxHp 100` / `MoveSpeed 4` / `Damage 1` …)이라 `base + meta` 합산이 전부 2배가 됨. `AttackSpeed` 는 쿨다운 배율이라 **공격 속도가 절반**이었다 | ✅ 해결 (2026-08-26 3차) |
| **I-22** | **시작 무기가 없음** — `WeaponManager.AddOrUpgradeWeapon()` 의 호출자가 레벨업 카드 하나뿐이라 `StartRun()` 직후 플레이어가 **맨손**이었다. 첫 레벨업 전까지 적을 공격할 수단이 전무 | ✅ 해결 (2026-08-26 4차 — 직업 시스템 → 2-6) |
| **I-23** | **직업을 고를 수 없음** — I-22 로 직업 3종이 생겼지만 선택 화면이 없어 `defaultClassIndex` 로 고정. Ranger/Mage 를 보려면 인스펙터를 고쳐야 했다 | ✅ 해결 (2026-08-26 5차 — 직업 선택 UI → 2-7) |
| **I-24** | **레벨업해도 카드 3장이 안 뜸** — `HUDManager.OnLevelUp` 의 `levelUpAnimator?.SetTrigger()` 가 **미할당 직렬화 필드**에서 `UnassignedReferenceException` 을 던졌다. `?.` 는 Unity 의 "가짜 null" 을 못 막는다. 예외가 `CollectXp` 밖으로 전파되어 `ShowLevelUpPanel()` 에 도달하지 못함. **레벨업 성장이 통째로 막혀 있었다** | ✅ 해결 (2026-08-26 6차 → 2-8) |
| **I-25** | **애셋 공백 — 적 6종이 스프라이트 1장을 공유하고 아이콘 17종이 5장을 돌려 씀, 직업 일러스트 0.** 종을 `Tint`(색 곱셈)로만 구분해 전부 같은 실루엣이었다. Unity AI 구독이 없어 생성이 `NoSubscription` 으로 반려되던 것이 전제 조건 | ✅ 해결 (2026-08-27 7차 → 2-9) |
| **I-26** | **적 연출 — (1) 엘리트/보스가 `sr.color` 곱셈이라 고유 스프라이트를 탁하게 만듦(녹색 좀비 × 보라 ≈ 검정), (2) 26장이 전부 1프레임이라 적이 미끄러짐.** 둘 다 I-25 직후에 드러난 문제 | ✅ 해결 (2026-08-27 8차 → 2-10) |
| **I-27** | **플레이어만 화면에서 작다** — `maxTextureSize`(512)가 원본(1024)보다 작으면 Unity 가 텍스처만 줄이고 `spritePixelsPerUnit`(1024)은 그대로 둔다. `bounds = rect/ppu` 라 직업 그림이 적과 같은 **0.5 유닛**이 되어 주인공이 안 보였다 | ✅ 해결 (2026-08-27 9차 → 2-11) — ppu 512 → 1.0 유닛 |
| **I-28** | **바닥이 없다** — 카메라가 `Skybox` 클리어로 URP 기본 파랑(#314D79)을 그대로 비추고 있었다. 바닥 애셋·타일맵 0개, 게다가 밝은 파랑이라 눈이 아프다 | ✅ 해결 (2026-08-27 9차 → 2-11) — 타일 10종 + `GroundTiler` |
| **I-29** | **플레이어가 미끄러진다** — I-26 은 **적만** 고쳤다. 플레이어는 `PlayerController` 의 `sr.flipX` 한 줄이 전부라 좌우로 갈 땐 뒤집히기만 하고 상하로 갈 땐 아무 일도 없었다 (사용자 표현: "현재는 머리만 움직임") | ✅ 해결 (2026-08-27 9차 → 2-11) — 16프레임 걷기 시트 + `PlayerVisual` |
| **I-30** | **씬 `Camera` 가 `Untagged`** — `Camera.main == null` 이라 **카메라 흔들기가 한 번도 발동하지 않았고**, 건물 배치의 `GetMouseWorldPos()` 는 `NullReferenceException` 이 날 자리였다 | ✅ 해결 (2026-08-27 9차, I-28 작업 중 발견) — `MainCamera` |
| **I-31** | **적 `CapsuleCollider2D` 가 그림보다 2배 김** — 0.5×1.0 인데 스프라이트는 0.5×0.5. 그림에 닿기도 전에 접촉 피해가 들어왔다 | ✅ 해결 (2026-08-27 9차, I-27 작업 중 발견) — 0.34×0.36 |
| **I-32** | **패시브가 레벨업할 때마다 누적됨** — `ApplyPassive` 가 매번 `new PassiveEffect` 를 리스트에 추가하고 `RecalculateStats` 가 전부 합산했다. `Passives.csv` 값은 "그 레벨의 총 보너스"라 Lv5 면 5개 레벨 값이 다 더해진다. `Damage` +0.55 → **+1.57**, `BuildingCooldown` 은 배수라 **-0.30 으로 음수 반전** → 하한 0.1 에 걸려 **전 건물이 10배 빨라짐** | ✅ 해결 (2026-08-27 10차, 건물 쿨다운 패시브 검증 중 발견 → 2-12) |
| **I-33** | **경험치 오브가 플레이어만 함** — `Exp_Orb.gif` 의 `spritePixelsToUnits` 가 작아 화면이 오브로 뒤덮였다 | ✅ 해결 (2026-08-27 10차) — ppu 460 |
| **I-34** | **리롤을 안 돌리면 계속 같은 아이템만 나옴** — `PickCandidates` 가 보유 업그레이드로 슬롯을 먼저 다 채워, 최대레벨이 아닌 아이템 3개만 있으면 카드 3장이 **영구히 고정**됐다 | ✅ 해결 (2026-08-27 10차) — 가중치 비복원 추첨(보유 ×2) |
| **I-35** | 클리어 화면에 **성장 정보가 없음** — 킬 수/시간만 있고 무엇을 얼마나 키웠는지 안 보였다 | ✅ 해결 (2026-08-27 10차) — 보유 아이템 아이콘 + `Lv.n` 나열 |
| **I-36** | **건물을 설치할 방법이 없음** — 마우스 배치 모드인데 `SelectBuildingForPlacement()` 호출자가 **0개**라 `_selectedBuildingData` 가 늘 null. **건물 아이템을 먹어도 아무 일도 안 일어났다** | ✅ 해결 (2026-08-27 10차) — `Z` 즉시 설치 + FIFO 대기열 + 밀리는 건물 |
| **I-37** | **건물이 전부 "때리는 것"뿐** — 3종 중 `House` 는 스크립트가 없어 세워도 아무 동작을 안 했다 | ✅ 해결 (2026-08-27 10차) — 5종(전투 2 + 경험치/골드/회복 3) + `BuildingCooldown` 패시브 |
| **I-38** | **`Z` 를 눌러도 건물이 안 세워짐** — `PlayerController.Awake()` 가 `GameManager.Instance?.BuildingMgr` 를 캐시하는데, 그 값은 `GameManager.Start()` 에서 채워진다. **모든 `Awake` 는 모든 `Start` 보다 먼저** 돌므로 `_buildingManager` 가 늘 null 이었고 Z 분기가 통째로 건너뛰어졌다 (I-8 과 같은 실행 순서 경쟁) | ✅ 해결 (2026-08-27 10차, 사용자 제보) — 첫 사용 시점 지연 조회 |
| **I-39** | **폭발 그림이 맵에 영구히 쌓임 (잔상)** — `BombardBuilding` 이 연출 프리팹을 풀 없이 `Instantiate` 만 하고 `AoeProjectile.Initialize` 를 안 불렀다. 풀 반환은 그 `Initialize` 가 시작하는 코루틴 안에 있어서 **영영 회수되지 않았다.** 곡사포가 3초마다 쏘므로 웨이브 하나에 수십 장이 겹쳤다 | ✅ 해결 (2026-08-27 11차, 사용자 제보) — 폭탄 투사체 경유 + 풀 반환 |
| **I-40** | **폭발 스프라이트가 회색 네모** — `ICON/폭발 애니메이션 시퀀스.png` 는 배경이 불투명하게 구워진 30컷 몽타주였고, 슬라이스도 4장만 엉뚱한 좌표에 잡혀 있었다 | ✅ 해결 (2026-08-27 11차) — Unity AI 로 16프레임 시트 재생성 |
| **I-41** | **AI 가 만든 스프라이트시트의 "투명 배경"이 가짜** — 알파 채널이 전부 255 이고 모델이 **체커보드 무늬를 RGB 에 그려 넣었다.** 그대로 넣으면 폭발 뒤에 체커 사각형이 보인다 | ✅ 해결 (2026-08-27 11차) — 체커 회색 씨앗 flood fill 로 알파 재생성 |
| **I-42** | **폭발 그림과 피해 범위가 따로 놀았다** — 폭발이 반경과 무관하게 항상 scale 1(0.5유닛)로 떴다. I-39 에서 그림을 반경에 맞추자 `Weapon_Aoe.explosionRadius = 3`(**지름 6유닛 = 화면의 1/3**)이 드러났다 | ✅ 해결 (2026-08-27 11차 후속) — 반경 3 → **2** (곡사포와 동일). 사용자 결정 |
| **I-43** | **HUD 에 타이머·킬 수·골드가 없다** — 플레이어가 "언제 끝나는지 / 몇 마리 잡았는지 / 돈이 얼마인지"를 알 방법이 없었다. `WaveManager` 는 남은 시간을 `TimerRoutine` 의 **지역 변수**로만 들고 있어 밖에서 읽을 수조차 없었고, `HUD/CurrencyText` 는 **아무 스크립트도 굴리지 않는 죽은 UI** 였다 | ✅ 해결 (2026-08-28 13차 → 2-17) |
| **I-44** | **때려도 맞은 티가 안 난다** — 적이 **밀리지 않고**, 죽을 때 한 프레임에 사라졌다(`OnDeath()` 가 빈 함수). `Shake()` 호출자는 플레이어 **피격** 하나뿐이라 **적을 죽일 때 화면이 무반응** | ✅ 해결 (2026-08-28 13차 → 2-17) |
| **I-45** | **SFX 슬라이더를 0 으로 내리면 BGM 도 꺼진다** — `SetSFXVolume()` 이 `AudioListener.volume`(**전역 마스터**)을 건드려 최종 음량이 `bgmSource.volume × SfxVolume` 이 됐다. 애초에 **효과음 재생 함수 자체가 없었다**(`PlaySfx` 0건) | ✅ 해결 (2026-08-28 13차 → 2-17) |
| **I-46** | **웨이브 뒤쪽 적이 아예 등장하지 않는다** — `SpawnRoutine()` 이 소환 항목을 **순차** 처리해 앞 항목을 다 뿌려야 다음이 시작됐다. `(마리수 × 간격)` 합이 `SurvivalTime` 을 넘으면 뒤쪽 적은 등장 자체를 못 한다 (`Normal3` = 86.4초 소요 / 90초 생존 → Goblin 40마리 유실). 사용자가 말한 "몬스터가 전 종류 안 나온다"의 실제 원인. 동시에 **적 수 상한이 없어** 병렬화하면 즉시 프레임이 무너질 상태였다 | ✅ 해결 (2026-08-28 14차 → 2-18) |
| **I-47** | **적 6종이 전부 같은 행동을 한다** — `MoveTowardsPlayer()` 직선 추격 하나가 전부라 Ogre 는 큰 고블린, Wolf 는 빠른 고블린이었다. 게다가 적 콜라이더가 전부 **트리거**라 물리 반발이 없어 **다 겹쳐 한 덩어리로 뭉쳤다** | ✅ 해결 (2026-08-28 14차 → 2-18) |
| **I-48** | **필드 픽업이 2종뿐** — `ExpDrop_Small` / `HealPickup`. 엘리트를 잡아도 보상이 경험치뿐이라 **처치의 무게가 없었고**, 흩어진 경험치를 회수할 방법이 걸어가는 것밖에 없었다 | ✅ 해결 (2026-08-28 14차 → 2-18) |
| **I-49** | **게임이 완전히 무음이다** — I-45 로 배관(`PlaySfx`/`PlayBgm`/보이스 풀/중복 컷)은 깔렸지만 `Assets/` 안에 **오디오 파일 0개**, `PlaySfx` **호출부 0곳**. 막고 있던 건 파일이 아니라 "**클립 참조를 어디에 둘 것인가**" 라는 미결정 하나였다 | ✅ 해결 (2026-08-28 15차 → 2-19) — (B)안 `AudioLibrary` SO + `SfxId`/`BgmId` enum |
| **I-50** | **전투를 시작하자마자 옵션창이 화면을 덮었다** — 원인이 **둘**이다. ① `UI Canvas` 자식 10개가 전부 전체화면(1920×1080)인데 `Canvas` 정렬 오버라이드가 없어 **형제 순서 = 그리기 순서**였고, `OptionSubPanel` 이 index 3(메뉴 패널들보다 뒤)이라 메인 메뉴에서 옵션을 눌러도 **아무 변화가 없어 보였다.** ② `MainMenuUI` 에 옵션창을 닫는 코드가 아예 없어 켜진 채로 남았다. 전투 진입에서 앞의 메뉴 패널이 전부 꺼지는 순간 그것만 남아 튀어나왔다 | ✅ 해결 (2026-08-29 16차 → 2-20) — 코드(`OnHidden`/`Close`)와 구조(오버레이를 맨 위로) **양쪽** 수정 |
| **I-51** | **ESC 로 멈춰도 BGM 이 계속 흘렀다** — 15차에서 `Paused` 를 `LevelUp` 과 묶어 "곡을 안 바꾸는 상태"로 정했는데(아래 2-19 ⚠️), 실제로 플레이해 보니 **손에서 게임을 놓은 시간에 음악만 도는 게 어색**했다. `StopBgm` 은 곡을 버려서 재개 때 도입부가 다시 나오므로 쓸 수 없었다 | ✅ 해결 (2026-08-29 16차 → 2-20) — `PauseBgm`/`ResumeBgm` + `_bgmPaused` 플래그(`AudioSource` 에 `isPaused` 가 없다) |
| **I-52** | **파이어볼이 날아오지 않고 적 위에서 그냥 터졌다** — `AoeWeapon` 은 목표 지점에 폭발을 바로 생성했다. 그런데 파이어볼과 폭탄이 **`Weapon_Aoe.prefab` 하나를 공유**해서, 프리팹에 몸체 필드를 달면 폭탄까지 날아가 버린다 | ✅ 해결 (2026-08-29 16차 → 2-20) — `WeaponData.TravelPrefab` + CSV 열로 **데이터 쪽에서** 갈랐다 |
| **I-53** | **상점 UI 가 너무 작다** — 카드가 `160×220`, 설명 글자가 `10pt` 였다. 레벨업 카드는 11차에 이미 키웠는데 **상점만 옛 치수로 남아** 있었다. 곁들여 카드 라벨이 한글이었고(§3 규칙 위반), 리롤 버튼의 `🔀`(U+1F500)가 Pretendard SDF 에 없어 **`␡` 로 그려졌다**(16차 💰와 같은 함정) | ✅ 해결 (2026-08-29 17차 → 2-21) — 카드 **250×440** · 글자 최대 `38pt` · 영문화 · 이모지 제거 |
| **I-54** | **무기 진화가 아예 없다.** 게다가 ROADMAP §2-3 의 원래 설계(`ItemData.EvolvesInto` + `RequiredPassive`)는 **"무기+패시브" 하나만 표현할 수 있어** 사용자가 요구한 `패시브·무기·건물` 3조합을 구조적으로 못 담았다 | ✅ 해결 (2026-08-29 17차 → 2-21) — 레시피를 **별도 `EvolutionData` SO** 로 분리(`ItemData[]` 재료). 전달 경로(상자 / 건물 앞 E)는 **열이 아니라 재료에서 파생** |
| **I-55** | **직업이 시작 무기 말고는 다를 게 없다** — 3직업을 가르는 건 `Bonus*` 스탯뿐인데 차이가 몇 퍼센트라 플레이 중에 체감되지 않았다. 소지 상한은 무기에만(`WeaponManager.maxWeaponSlots = 6`) 있었고 **직업과 무관한 전역 상수**였으며, 건물·패시브에는 상한 개념 자체가 없었다. 곁들여 **`PickCandidates` 에 상한 검사가 없어**, 슬롯이 찬 상태로 새 무기를 고르면 `_inventory` 에는 기록되고 `WeaponManager` 는 경고만 남긴 채 거절해 **"보유 중인데 무기는 없는"** 유령 아이템이 만들어졌다(카드 가중치 2배 · 진화 재료 판정 통과). 상한 6 이라 잠복해 있었을 뿐 **상한 3 을 넣는 순간 즉시 터지는** 상태였다 | ✅ 해결 (2026-08-29 18차 → 2-22) — 상한을 `CharacterClassData` 로 옮기고 `PlayerStats.SlotLimit` → `LevelUpManager.CanAcquire` **단일 창구**로 통일 |
| **I-56** | **진화 3조합이 전부 무기를 뱉었다** — 17차에 `패시브·무기·건물` 조합을 열어 뒀지만 결과가 다 무기라, 건물이 재료로 들어가는 이유가 "제단이 필요해서"뿐이고 **조합마다 결과가 달라지지 않았다**. 게다가 직업은 런 시작에 한 번 정해지면 끝이라 **성장 축이 없었다** | ✅ 해결 (2026-08-29 19차 → 2-23) — **무기+건물을 직업 승급으로 분리**(`ClassEvolutions.csv` + `ClassEvolutionData`). 직업을 사슬(`PlayerStats.ClassChain`)로 바꿔 **보너스·소지 칸을 누적**시켰다 (교체하면 승급이 손해가 된다) |
| **I-57** | **승급해도 화면에서는 아무 일도 일어나지 않았다** — `Classes.csv` 의 `Portrait`/`BodySprite`/`WalkSheet` 가 승급 4행에서 전부 비어 있었고, `PlayerStats.ApplyClassVisual` 은 그 경우 **조기 반환**한다. 게임에서 가장 큰 성취인 승급이 **로그로만 존재**했다. 진화 무기 3종도 재료 무기의 아이콘을 재사용해 레벨업 카드에서 `Sword` 와 `Excalibur` 가 **같은 그림**으로 떴다 | ✅ 해결 (2026-08-29 20차 → 2-24) — 애셋 **17장** 생성·배선. 곁들여 **I-41(가짜 투명 배경)의 실제 해법**을 찾았다 — `RemoveImageBackground` + `targetAssetPath` 는 원본을 제자리에서 고쳐 **GUID·슬라이싱을 보존**한다 |
| **I-58** | **적 걷기 시트 6장이 놀고 있었다** — 20차에 뽑아 놓고 소비할 코드가 없어 뒀다(사용자 승인). 적은 항상 화면에 수십 마리가 있는데 정지 그림 한 장이 셰이더 바운스로만 흔들려 **미끄러지듯** 다가왔다. 더 나쁜 건 20차가 커밋한 시트 PPU 가 **1024(=0.25유닛)** 라, 그대로 배선했으면 **애니메이션이 켜지는 순간 적 6종이 전부 절반으로 줄어들** 상태였다는 점이다 | ✅ 해결 (2026-08-29 21차 → 2-25) — PPU **512** 로 교정 후 `EnemyData.WalkFrames` + `Enemies.csv` 의 `WalkSheet` 열 + `EnemyVisual.StepFrames` 로 배선. `EnemyVisual` 은 **이미 있던** 컴포넌트라 인자 하나를 늘리는 것으로 끝났다 |
| **I-59** | **"플레이해 보고 정할 것"이 문서 세 곳에 흩어져 있었다** — `TODO.md` §0 의 꼬리표 · §1 의 체크박스 · §3 의 자리표시 수치표. 게다가 §1 이 **"동작하나?"(한 번 확인하면 끝)** 와 **"느낌이 맞나?"(고치고 다시 플레이하는 반복)** 를 섞고 있어 §1 이 영원히 줄어들지 않는 구조였다. 사용자가 **"수치는 완성 먼저 하고 진행하면서 조절한다"** 로 방침을 정하면서 그 목록을 한곳에 모을 필요가 생겼다 | ✅ 해결 (2026-08-29 22차 → 2-26) — `Docs/TUNING.md` 신설. 항목마다 **무엇을 보나 → 어느 파일의 무엇을 얼마나** 형식. 곁들여 **적 타격 반응 5개가 CSV 가 아니라 `EnemyBase.cs` 의 `const`** 인 것을 발견해 기록했다 (고치지는 않음) |
| **I-60** | **`Pretendard SDF.asset` 이 커밋마다 25만 줄씩 바뀌었다** — TMP 폰트가 **Dynamic** 이라 런타임에 처음 만난 글리프를 그 자리에서 굽고 애셋을 dirty 로 만든다. 즉 **플레이만 해도 파일이 바뀌어** 이 파일 하나만 계속 staging 에서 빼 왔다(결정 1). 겸사겸사 폰트 원본 19장 중 **18장이 아무 데서도 안 쓰이고** 있었다 (`Assets/Fonts` 55MB) | ✅ 해결 (2026-08-29 23차 → 2-27) — 18장 GUID 참조 0건 확인 후 삭제(55MB→4.8MB), SDF 는 **ASCII 32~126 전부 + 기호 20개 = 115자**로 다시 구워 `atlasPopulationMode = Static` 고정. 굽기 전 ASCII 가 **72자뿐**이었던 것(23자 누락)을 이때 발견했다 |
| **I-61** | **T2 세 갈래를 다 먹을 수 있었다** — 조건만 채우면 Sentinel·Doomlord·Warden 을 전부 가질 수 있었고, 직업 사슬은 보너스가 **누적**되므로 갈래가 사라질 뿐 아니라 후반이 일방적으로 세졌다. 게다가 승급 전용 직업을 데이터가 아니라 **SceneWiring 배선으로만** 막고 있어서, 직업 해금 흐름을 붙일 때 실수로 넣으면 **T2 로 런이 시작될** 상태였다 | ✅ 해결 (2026-08-29 24차 → 2-28) — `Classes.csv` 에 **`Tier` 열 신설**(레시피가 아니라 직업 쪽 — 사슬이 들고 있는 게 직업이고 T1 은 레시피가 없다). 같은 티어는 하나만(`PlayerStats.HasTier`), `Tier > 1` 이면 선택 화면에서 제외. `IsPromotionOnly` 는 **열이 아니라 파생 프로퍼티**로 둬 어긋날 길을 없앴다 |
| **D2**<br>(B3) | **"Village 를 얻었는데 Turret 이 설치된다"** — 예외가 아니라 **설계대로**였다. `BuildingData.MaxCount = {1,1,2,2,3}` 이라 건물을 레벨업하는 것만으로 `_pendingQueue` 에 같은 건물이 조용히 쌓이고, 큐가 FIFO 라 나중에 얻은 게 뒤로 밀린다. 실체는 "잘못 설치된다"가 아니라 **"무엇이 설치될지 모른다"** | 🟡 **완화** (2026-08-30 25차 → 2-29) — HUD 에 `[Z] Build: Turret  (+3 queued)` 를 띄운다(`NextPending` 은 이미 public 이었다). **B3 은 닫지 않았다** — 큐 순서는 그대로고 표시만 붙었다 |
| **B1** | **엘리트 외곽선이 스프라이트와 어긋난다** — 25차 진단은 *"`SampleAlpha` 의 `uv 0~1` 검사가 안 걸려 옆 칸을 빨아들인다"* 였다. 맞지만 **절반이었다** — 진짜 주범은 `_OutlineTexSize` 가 `512` 로 굳은 것(시트는 1024) | ✅ 해결 (2026-08-30 38차 → 2-42, `D15`) — 선이 **의도의 2배(28텍셀)** 로 그려졌고 프레임이 130~230텍셀뿐이라 실루엣을 덮었다(키의 13.6~23.8%, 낱장 시절 2.7%). 번짐은 실측 결과 **Demon f14 만**(2616px) — `textureRect` 가 격자가 아니라 **타이트 bbox** 라서다. 둘 다 고침 |
| **B2** | **움직일 때 "작은 몹 여러 마리 뭉치"가 뜬다** — 걷기 시트 16칸 중 Goblin·Slime 은 **12칸이 깨져** 있다(칸당 16마리·9마리). Demon 3칸 · Wolf 1칸도 오염. 코드·슬라이스·SO 배선은 전부 정상 | 🔴 **미해결** — 원인 확정(25차), **애셋 재생성 요청 전달**(`Parallel/REQ/CONTENT.md` 요청-1). 정지 중엔 f0 고정이라 **움직일 때만** 보였다 |
| **B4** | 일시정지·옵션창 글자 6곳이 **빈칸**으로 나온다 — 씬에 한글이 남아 있는데 폰트는 Static 115자(I-60)라 한글 글리프가 없다 | 🔴 **미해결** — CONTENT 가 보고, 담당 DEV |
| **D3** | **진화·승급을 시험할 방법이 없었다** — Lv5 무기 + Lv3 건물을 정상 플레이로 모으려면 매번 몇 분씩 걸려서 `TODO.md` §1 의 미검증 항목이 쌓이기만 했다 | ✅ 해결 (2026-08-30 26차 → 2-30) — `DevPanel` 신설. 백틱으로 여는 IMGUI 치트 패널(아이템 `-`/`+`/`Max`/`X` · 진화·승급 즉시 실행). 파일 전체가 `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`, 씬에는 흔적 0(런타임 자동 생성). **슬롯 상한과 공개 API 를 우회하지 않는다** — 우회하면 거기서 나온 상태를 신뢰할 수 없다 |
| **D4**<br>(B2) | **걷는 적이 축소판 격자로 보였다** — 코드가 아니라 애셋이 깨져 있었다. Goblin·Slime 은 16칸 중 12칸이 프레임이 아니라 "작은 캐릭터 9~16마리 뭉치"였고, `StepFrames` 가 16칸을 순서대로 도니 12/16 확률로 그게 떴다 | ✅ 해결 (2026-08-30 27차 → 2-31) — 수정본 제작은 CONTENT(C1), **임포트·실플레이 판정은 DEV(D4).** 🔴 **png 만 덮어썼다 — `.meta` 를 지우면 PPU 가 1024 로 돌아가고(I-58 재발) 스프라이트 GUID 가 바뀌어 `WalkFrames` 16칸이 통째로 끊긴다.** 판정 5개 전부 통과, 실플레이에서 f8~f15 를 포함한 12칸이 동시에 떴는데 전부 단일 256×256 |
| **D5**<br>(B4) | **일시정지·옵션창 글자 6곳이 빈칸으로 나왔다** — 씬에 한글이 남아 있는데 폰트가 I-60 이후 **Static 115자**(ASCII + 기호 20)라 한글 글리프가 없다. `CLAUDE.md` §3 "UI 문자열은 영문" 위반이기도 하다 | ✅ 해결 (2026-08-30 28차 → 2-32) — 폰트를 다시 굽지 않고 **영문화**했다(사용자 결정). 씬 `m_text` 6곳만 교체 = `6 insertions(+) / 6 deletions(-)`. 판정 5개 전부 통과 — `missing=[]` · `characterCount == visible` · 캡처 2장 · **폰트 경고 0건**. 🔴 **`BUGS.md` 의 행 번호는 이미 밀려 있었다** — 하이어라키 경로로 찾을 것 |
| **D6**<br>(C2) | **적 타격감 12개가 `EnemyBase.cs` 에 `const` 로 박혀 있었다** — 플레이하며 조절해야 하는 값인데 고칠 때마다 재컴파일이 필요했다. `Economy.csv` 로 뺄 수 없었던 이유는 임포터가 **씬만 훑는데** 적은 프리팹을 공유하기 때문 | ✅ 해결 (2026-08-30 29차 → 2-33) — **`CombatFeel` 씬 컴포넌트** 신설로 임포터를 안 고치고 기존 3열 규칙에 태웠다. `Default*` `const` 하나가 **필드 기본값이자 폴백**이라 컴포넌트가 없어도 같은 감각으로 돈다. `Economy.csv` **43/43 적용**(+12) · `!` 경고 0. 🔴 **"안 바뀐다"만으로는 배선을 판정할 수 없어** `6`→`20`→`6` 왕복으로 확인했다(엘리트 8.000 = 20×0.4 로 저항 2줄도 같이 증명) |
| **D7**<br>(C6) | **진화 무기 3종이 재료 무기 아이콘을 그대로 썼다** (Excalibur = Sword 아이콘). 🔴 **그림이 없던 게 아니라 배선이 빠져 있었다** — 전용 png 3장은 I-57(20차 `a85cc9f`)에 이미 들어와 있었고 `Weapons.csv` 의 `Icon` 열만 재료 무기를 가리킨 채였다 | ✅ 해결 (2026-08-30 30차 → 2-34) — CONTENT 가 `4f1a92c`(C6)로 CSV 를 고쳐 두어 DEV 는 **Import 1회**만 했다. guid 3쌍을 **Import 전에 기록**해 두고 대조. `git diff` 가 **파일당 `Icon` 한 줄뿐**이라 다른 수치가 안 되돌아갔음을 증명(`Damage` 70/55/34 유지) · `Economy.csv 43/43` 도 유지되어 29차 `CombatFeel` 도 무사. 실플레이는 요청-5 가 면제 |
| **D8**<br>(C3) | **소리가 아예 안 나는 지점이 6곳** 있었다 — 터렛·곡사포 발사, 원거리 적의 발사, 식당 회복, 보스 등장, 상점·레벨업 리롤. 여기에 사용자 지시 **"잡몹은 같은 소리, 엘리트·보스만 다르게"** 가 더해졌다. CONTENT 는 스펙만 낼 수 있었다 — 클립 생성(Unity AI)도 `.asset` 등록도 에디터라 DEV 소유다 | ✅ 해결 (2026-08-30 31차 → 2-35) — `SfxId` **7개 추가**(`Crit` 은 값만 예약) · 호출부 **6곳** · 클립 **6개** 생성 후 프로젝트 규격(ADPCM+mono)으로 재임포트 · `AudioLibrary` **14→20**. 🔴 **`AudioId.cs:8` 의 "끝에 추가한다" 주석이 틀렸다** — `Id` 값 매핑이라 빈 번호 삽입이 안전하다(같이 고침). 엘리트 분기는 요청서가 "노말 웨이브에서 불가"라 했으나 **재초기화 우회로로 `EnemyDie`/`EnemyDieElite` 를 같은 프레임에 동시 증명**. 🔴 **판정 2개는 못 했다 — 이 머신이 `sfx_vol=0.00` 이라 무음**(버그 아닌 저장된 설정) |
| **D9**<br>(C7) | **활이 총알을 쏘고 있었다.** `BowData.ProjectilePrefab` 이 `Proj_Bullet.prefab` 이라 icons8 벡터 아이콘(`ICON/Bullet.png`)이 날아갔다. `DESIGN_CLASSES.md` §6 중 **코드가 0줄인 유일한 항목** | ✅ 해결 (2026-08-30 32차 → 2-36) — `Arrow.png` 임포트(**PPU 512** · Point · Uncompressed) · `Proj_Arrow.prefab` **복제 신설**(다른 줄 = `m_Sprite` **1줄**) · `Weapons.csv` Import(**저장소 전체에서 바뀐 줄 1줄**). 🔴 **④(방향)를 게임 카메라 캡처로 직접 봤다** — 네 방향 모두 촉이 진행 방향. ⑤는 `Proj_Bullet` 을 **대조군으로 같이 쏴** 이동거리 2.37 일치 + 스택 트레이스로 증명. ⚠️ **판정 ①의 기대값은 `bounds`(0.5) 가 아니라 알파 bbox(0.391) 기준이었다** — PPU 는 정확히 512. 덤으로 **B5** 발견(고치지 않음) |
| **D10**<br>(C8) | 무기가 전부 *"가까운 적에게 발사체 하나"* 뿐이라 손맛이 같았다. **관통이라는 개념이 게임에 아예 없었고**(적이 뭉칠수록 무기가 약해진다), **검은 이름과 달리 총알을 쏘고 있었다.** `DESIGN_CLASSES.md` §6 **2단계** | ✅ 해결 (2026-08-30 33차 → 2-37) — `ProjectileBase` 에 관통+자전(기존 파일 **유일한 수정**) · `MeleeWeapon`·`SwingArcFx` 신설 · 스프라이트 3장 + 프리팹 3개. 🔴 **자전을 넣으면 나선을 그린다** — 이동이 `Translate(Space.Self)` 라 진행방향=회전이었다. **월드 `_direction`** 으로 분리(기존 발사체엔 수식 동일). 🔴 **관통은 hit-set 이 있어야 관통**(없으면 한 마리를 3번). 🔴 **호의 실제 반지름이 107.4px** 이라 `localScale=Range` 는 사거리를 **7% 부풀린다** → `÷1.074`. ③ 회귀는 `Proj_Bullet` **대조군**이 1번째 적 앞(0.63), 수리검은 **3번째** 적 앞에서 소멸로 증명. ⑦ 은 **6프레임을 겹쳐 세워 게임 카메라로 직접 봤다**. ⏸ **⑤(레벨업 3택)는 CSV 대기** → `REQ/CONTENT.md` 요청-4 |
| **D11**<br>(C9) | 게임에 **시간축이 없었다.** 모든 폭발이 닿는 즉시 터져 *"저기 폭탄이 떨어졌으니 피하자"* 는 판단이 존재하지 않았고, **지속 피해도 슬로우도 개념 자체가 없어** 적이 몰려오면 도망만 답이었다. `DESIGN_CLASSES.md` §6 **3·4단계** | ✅ 해결 (2026-08-30 34차 → 2-38) — `BombProjectile` 에 신관(`fuseTime`+`fuseFrames`) · `EnemyBase` 에 슬로우(`ApplySlow`/`CurrentSpeed`) · `ToxinField`·`FieldWeapon` 신설 · 스프라이트 3장 + 프리팹 3개. 🔴 **슬로우는 `MoveSpeed` 를 덮어쓰지 않는다** — 읽는 쪽에서 곱하고 `_slowUntil` 로 **저절로 만료**시킨다. Trigger Enter/Exit 이면 적이 장판 안에서 죽거나 풀로 반납될 때 Exit 가 안 와 **영구 슬로우**가 된다. 🔴 **겹침을 곱하면 `0.6×0.6=0.36` 으로 적이 멈춘다** → "가장 센 것 하나만". 🔴 **풀 재사용 리셋은 `Setup()` 이 아니라 `Initialize()`**(요청서가 메서드를 잘못 짚었다). 🔴 **요청서의 Pivot Center / `radius 1.0` 이 그림과 달랐다** — 실측 중심 `(134,132)px` · 반경 `≈97px` → Custom pivot + `0.97`(사용자 승인). 화면 픽셀 검증 **오차 0.7px**(보정 없었으면 ≈6.7px). ②는 `Proj_Bomb` **대조군**이 `fuseTotal=0.00s` 로 즉폭 유지, ⑥은 겹친 장판에서 `0.504`/`0.792` 가 **한 번도 안 나온 것**으로 증명. ⏸ **⑨(레벨업 3택)는 CSV 대기** → `REQ/CONTENT.md` 요청-5 |
| **D12**<br>(C12·C15) | D10·D11 이 만든 **코드·프리팹·그림이 게임에 안 나오고 있었다.** `Balance/*.csv` 에 줄이 없으면 레벨업 3택에도 상점에도 등장하지 않는다. CONTENT 의 두 요청(C12 수리검·검근접 / C15 독장판·바닥폭탄)이 **같은 CSV 3장**을 건드려 **Import 1회로 둘 다** 반영해야 했다 | ✅ 해결 (2026-08-30 35차 → 2-39) — **C# 수정 0줄.** Import 1회(`Weapons 10 · Items 25 · SceneWiring 11/11`, 에러 0)로 SO **4개 신설**(`Shuriken`·`Toxin` × Weapon/Item) + **3개 수정**(`Sword` 근접화 · `Bomb` 신관 켜짐). 요청-9·요청-10 **판정 16개 전부 PASS**. 🔴 **`SceneWiring` Import 는 씬을 더럽히기만 한다 — `SaveScene` 없으면 배선이 날아간다.** 🔴 **`hitsPerProj` 를 처음엔 5.00 으로 잘못 쟀다** — 프리팹 상한 3을 넘길 수 없으니 계측이 틀린 것이고, 원인은 `PlayerStats.cs:258` 도 **피해 팝업을 띄운다**는 것(접촉 피해가 "적 명중"에 섞였다). 거리로 걸러 **3.10**(= `pierceCount: 3`). 🔴 **가만히 선 프로브에겐 적이 근접 사거리까지 안 온다** — 최근접 4.67유닛이라 검이 6초간 **0회** 휘둘렀다. `EnemyBase.Reposition()` 으로 몰아 세워 `SWORD proj=0 arc=13 kills=14`. ⑤는 **60회 추첨(180장)** 으로 실물 확인. ⑥⑦(폭탄 신관·장판)은 **게임 카메라 캡처 3장**. 🔴 **CSV 는 DEV 가 안 건드렸다**(CONTENT 가 `24b61ce`·`b457dcf` 로 이미 커밋) |
| **D13**<br>(C16) | 모든 무기가 **플레이어를 기준점**으로 적을 찾고 때렸다. 소환수는 처음으로 **기준점이 플레이어가 아닌 무기** — 저 혼자 떨어져 서서 저 자리에서 싸운다. 그림 5장은 CONTENT 가 이미 다 그려 놨고 **코드·프리팹만 비어 있었다.** `DESIGN_CLASSES.md` §6 **5단계** | ✅ 해결 (2026-08-30 36차 → 2-40) — 스프라이트 5장 임포트 + `SummonWeapon`·`SummonVisual` 신설 + 프리팹 5개. **기존 파일 수정 0줄.** 판정 **12개 전부 PASS**. 🔴 **파생 클래스에 `Update()` 를 선언하면 무기가 조용히 죽는다** — `WeaponBase.Update()` 가 쿨다운을 돌리는데 그걸 가린다. `LateUpdate()` 로. 🔑 **철거는 `OnDisable()` 하나** — `RemoveWeapon`→`Pool.Return`→`SetActive(false)` 가 곧 그것이라 새 훅이 필요 없었다(없었으면 환불 후 **소환수만 영원히 남는다**). 몸통에 **Collider·Rigidbody 없음**(`col=False` `rb=False`) — 넉백에 안 날아간다. 🔴 **`sprite.bounds` 로는 크기를 못 잰다** — 소환수도 오우거도 `0.500`(셀 기준). 알파 bbox 실측 **64.7%/66.7%**. 🔴 **프로브를 세 번 고쳤고 세 번 다 검증 장치가 틀렸다** — 플레이어가 안 움직여 ④ 가 `1.20` 고정 / **풀 객체는 `GetInstanceID()` 가 돌아와** ⑧ 이 첫 후리기만 / ESC 일시정지의 `timeScale=0` 이 코루틴을 얼림. ⑧ 은 각도 최대 **171°** 로 등 뒤 피격 증명. ⏸ **CSV 는 일부러 안 넣었다** → `REQ/CONTENT.md` 요청-7 |
| **D14**<br>(C17·C18) | 무기 두 개가 **자기 소리가 없어 남의 것을 빌려 쓰고 있었다** — 검(D10 근접 재해석)이 `WeaponFire`=**총소리**, 독 장판(D11)이 `WeaponCast`=**마법 시전음**. 🔴 소리 문제가 아니라 **판정 문제**다: `TUNING.md` §3 의 "안 시원하다"가 `halfAngle` 탓인지 총소리 탓인지 구분이 안 되므로 **수치 판정보다 먼저** 해야 했다 | ✅ 해결 (2026-08-30 37차 → 2-41) — wav 2장 임포트 + `SfxId` **4·5** + `AudioLibrary` 2항목 + 호출부 **딱 2줄**. 🔴 **Unity 기본 임포트가 프로젝트 규격이 아니다** — Vorbis·스테레오로 들어와서 `AudioImporter` 를 직접 ADPCM·`forceToMono`로 맞추고 `SaveAndReimport()`(D8 과 같은 함정). 🔑 **`SfxId` 는 정수 직렬화라 분류 안 빈 번호에 끼워 넣는 건 안전**(`_sfxMap[e.Id]=e`) — 기존 값 0개 변경. `.asset` YAML 을 손으로 안 쓰고 **`SerializedObject`** 로 넣었다(메모리 사본이 이겨 덮어써진다). 🔴 **프로브가 옛 소리를 대조군으로 같이 셌다** — "새 소리가 난다"만으로는 옛 소리가 같이 나는지 모른다 → **Fire=0 / Cast=0**. 보이스 재사용 때문에 상승 엣지는 `isPlaying` + `clip` 둘 다 봐야 한다. 볼륨 실측이 계산과 일치(0.094 / 0.119). 🔴 **검 클립 0.280s 가 연타 간격 0.247s 를 이미 넘었다**(실측 0.249·0.254, 겹침 31ms — 페이드아웃 구간이라 무해). **늘리는 선택지는 봉쇄** → `comboInterval` 을 같이 손대야 함. 🟡 **청취 4건**(⑥⑦⑧⑨)은 기계가 판정 못 함 → `REQ/CONTENT.md` 요청-8 |
| **D15**<br>(B1) | 엘리트 외곽선이 실루엣과 어긋났다. 🔴 **`BUGS.md` 에 적힌 원인 진단이 절반만 맞았다** — 문서를 믿고 바로 고쳤으면 주범을 놓친 채 곁가지만 고치고 닫았을 것이다 | ✅ 해결 (2026-08-30 38차 → 2-42) — 셰이더 `_SpriteRect` 신설 + `EnemyVisual.ApplySpriteRect` 로 프레임마다 **사각형과 실제 텍스처 크기**를 MPB 로 전달. 🔴 **주범은 `_OutlineTexSize` 512 하드코딩**(I-58 이 1024 시트로 바꾸며 두께가 2배로) — 이걸 `BUGS.md` 는 *"TUNING 감"* 이라며 미뤄 뒀다. 🔴 **`Sprite.textureRect` 는 격자 칸이 아니라 타이트 bbox**(`274.08, 580.08, 223.90, 117.85` — 소수점)라 96프레임 전수 조사 결과 번짐은 **Demon f14 만** 2616px, 나머지는 0. 🔴 **A/B 렌더가 처음에 `diff=0` 을 냈다** — "값이 안 도달함"과 구분이 안 되므로 `_SpriteRect` 를 작은 상자로 강제한 **대조군**(32821→4710)으로 MPB 가 `UnityPerMaterial` 을 덮어씀을 먼저 증명했다. 실플레이 **적 14마리 불일치 0** · 서로 다른 프레임 3종 확인. 검증 장치는 `HideAndDontSave` + 레이어 31 카메라라 **씬을 안 더럽히고 지울 임시 파일도 0.** 🟡 **최종 굵기(14/12 → 7/6?)는 사람이 봐야 함** → `REQ/CONTENT.md` 요청-9. 덤으로 **B6** 발견(고치지 않음) |
| **D16**<br>(C19·C20) | 소환수 2종(드래곤·문어)이 **프리팹도 코드도 다 있는데 게임에 없었다** — CSV 에만 있고 SO 가 없어 레벨업 카드에 안 떴다. 게다가 촉수 그림은 **가운데가 꽉 찬 원반**이라 후릴 때마다 소환수 몸통을 덮었고, 소리는 D14 에서 안 고친 채 남아 화염구=`WeaponCast`(마법음) · 촉수=`WeaponFire`(**총소리**)를 빌려 쓰고 있었다 | ✅ 해결 (2026-08-30 39차 → 2-43) — CSV Import 로 `WeaponData`·`ItemData` **4개 생성**, `allItems` 22→**24**. 🔴 **`SceneWiring` Import 는 씬을 더럽히기만 한다** — `ManageScene Save` 를 빼면 그대로 22 로 돌아간다(D12 함정 재발 방지). 🔴 **png 를 덮어쓸 때 `.meta` 를 지우면 안 된다** — 6분할·PPU 100·스프라이트 GUID 가 전부 날아간다. 촉수 6프레임 실측 **중심 r≤12px 불투명 0** (몸통이 링 너머로 보인다) · 바깥 99.85px vs 계약 99.51px(**0.34px 차** → 코드 무수정). 소리는 `SfxId` **6·7** + 호출부 **2줄**, 프로브 46건에 **대조 `WeaponFire=0` / `WeaponCast=0`**. 드래곤 간격 1.4s 정확, 촉수 2.4s 공백 = 적 없을 때 조용히 리턴(허공 안 후림). 🔴 **`AudioImporter.preloadAudioData` 가 Unity 6.3 에서 obsolete**(`CS0619` = 컴파일 에러) — `defaultSampleSettings` 로 옮겨졌다. 🔴 **촉수 클립은 `lashHitDelay` 0.1s 에 묶여 있다** — 바꾸면 `gen_tentacle_lash.py` 의 `HIT_AT` 도 다시 구워야 한다(주석 2곳에 박음). 덤으로 **링 반경 1.8 > 오프셋 1.2 라 플레이어가 링 안에 들어오는 것**을 눈으로 확인 → `REQ/CONTENT.md` 요청-10. 🟡 청취 4건 |
| **D17**<br>(C21) | D15 가 `_OutlineTexSize` 를 고친 뒤 `_OutlineWidth` 가 **진짜 텍셀 수**가 됐는데, 값 `14/12` 는 그 전에 눈으로 맞춘 것이라 **낱장 시절의 2배**로 두꺼웠다. 진짜 원인은 시트 전환이 아니라 **PPU 2048 → 512**(텍셀 ½ · PPU ¼ → 순증 2배) | ✅ 해결 (2026-08-31 40차 → 2-44) — `EnemyBase.ApplyRankOutline()` **한 줄**, 비율 7:6 유지하고 배율만 **×0.75** (`14/12` → **`10.5/9`**). 🔴 **`7` 로 못 내린다** — 시트가 `filterMode 0`(Point)이라 **2 화면 px 밑은 선이 끊겨 점선**이 되는데 `7` 이면 Wolf 1.36px · Slime 1.44 · Goblin 1.60 으로 흔한 잡몹 셋이 깨진다. 상한은 **키의 ~9 %**(슬라임 엘리트는 화면에서 23px 뿐). `10.5` 가 하한 Wolf **2.04px** · 상한 Slime **9.2 %** 로 **양쪽에 동시에 걸친다**. 🔴 **종별 보정은 안 했다** — 두 자가 정반대라 Ogre 에서 6.6 vs 16.1 로 갈린다(코드 주석에 박음). 검증은 계산이 아니라 **렌더 픽셀을 셌다** — `RenderTexture(1920×1080)`+`ReadPixels` 색 카운트로 엘리트 보라 **261px** · 보스 빨강 **555px** · 일반 **0**. 점선 여부는 행별 연속 덩어리로, 최악 사례 Wolf 엘리트도 24행 중 끊긴 행 2(머리·발 캡)뿐. 실플레이 68.8초 전수 감사 2회(19·24마리) **일반 몹 불일치 0**. 🟡 **판정 ④(난전 0.2초 판별)는 미판정** — 68.8초에 엘리트가 한 번도 안 나왔다. 사람 눈 몫 |
| **D18**<br>(B6) | 무기 개수를 세는 장부가 **둘**인데 안 맞았다. `GameManager.cs:228` 이 시작 무기를 `WeaponManager` 에 직접 넣어 `LevelUpManager._inventory` 를 건너뛰어서, 매 판 `_weapons.Count == CountOwned(Weapon) + 1` 이었다 | ✅ 해결 (2026-08-31 41차 → 2-45) — **A안(장부를 하나로), 사용자 결정.** `LevelUpManager.GrantStartingWeapon()` 신설, `ApplySelectedClass()` 가 이쪽을 부른다. 🔴 **증상이 넷이었다** — 등재된 "무기 칸 하나 모자람" 외에 **시작 무기가 카드에 신규로 재출현** · **`HasItem`/`GetItemLevel` 이 거짓말** · **진화 재료 판정에서 누락**. 개수만 맞추는 B안이었으면 셋이 남았다. `ApplyItem` 재활용 안 함(그쪽은 `_inventory[item]++` 라 `StartingWeaponLevel` 2 이상이면 어긋난다). 대응 `ItemData` 없으면 **경고 남기고 우회 지급**(재발 감지). ⚠️ `StartRun()` 의 `ResetRunState()` → `ApplySelectedClass()` **순서에 묶여 있다**. 검증: 차이 **0**(예전 1) · 상한 3/3 채움 거절 0 · **`무기 슬롯 꽉 참` 경고 0** · 과잉 제안 0 |
| **D19** | 소환수가 프리팹에 박힌 `offsetAngle`/`offsetDistance` 로 **플레이어 기준 고정 좌표**를 쫓아 공전했다. 플레이어가 꺾으면 목표점이 통째로 회전해 **옆으로 미끄러지고**, 겹치지 않으려면 각도를 손으로 배분해야 했다(소환사 무기 칸 5개 → 셋째부터 답 없음) | ✅ 해결 (2026-08-31 42차 → 2-46) — 사용자 지시 *"꼬리처럼, 많아지면 기차처럼"*. `PlayerTrail.cs` **신설** — 플레이어가 지나온 길을 자국으로 기억하고 `SampleBack(d)` 로 **길 위 거리** `d` 만큼 거슬러 간 점을 돌려준다(직선 거리가 아니라 호 길이라 꺾인 자리를 지나간다). `SummonWeapon` 은 정적 `Train` 에 **얻은 순서대로** 등록되고 뒤처짐 거리 = **앞칸들 `offsetDistance` 누적 합**(곱이 아니라 합 → 칸마다 간격을 다르게 줄 수 있다). 🔴 **씬에 배선하지 않는다** — 소환수는 무기 획득 시점에 생겨 배선 보장이 없고, 요구하면 CONTENT 소유 `SceneWiring.csv` 를 건드려야 한다. 🔴 **`SampleBack` 이 스스로 기록한다**(프레임 번호 잠금) — 두 `LateUpdate` 의 실행 순서가 보장되지 않는다. `Seed` 로 런 시작에 길을 미리 깔고(안 그러면 발밑에 뭉친다), 3유닛 초과 순간이동이면 길을 지운다(스테이지 이동 시 화면 밖까지 끌려간다). `offsetAngle` 은 소유자 없을 때의 물러날 길로 **필드만** 남겼다. 🔑 검증은 **대조군을 같은 로그 줄에** — ㄱ자 주행에서 정지 **1.20/2.40** · 주행 **1.87/3.07**(칸 간격 항상 1.20, 1.87 = 1.20 + 속도4÷followLerp6) · 꺾은 뒤 `x` 편차 **0.00** · t=0.50 에 **드래곤이 먼저 돈다**(0.39 vs 문어 1.11) · 옛자리와의 차 **1.13~3.08** 로 한 번도 안 겹침. 🔴 **곁가지 — `TUNING.md` 의 "3마리 이상이면 72° 재배치"는 없는 기능이었다**(`offsetAngle` 사용처 1곳). 두 세션이 그걸 근거로 각도 조정을 보류 중이었다 → 요청-13 으로 정정 요청. 🟡 요청-16(촉수 `m_SortingOrder`)은 **문어 단독일 때 1번칸 1.20** 이라 여전히 필요 → `D21` 로 분리 |
| **D22** | 사용자 지시 *"EVENT로서 전투 중 함정방 느낌 · 무한적 + 시간제한 생존 · 여러 이벤트 중 하나로 **일단은 문서화만**"*. 이벤트가 하나도 설계돼 있지 않았다 | ✅ 해결 (2026-08-31 43차 → 2-47) — `Docs/DESIGN_EVENTS.md` **신설**(307줄, DEV 소유). 🔴 **코드 변경 0** — 지시가 문서화만이었다. 🔑 **코드를 먼저 읽었더니 설계가 바뀌었다** — 이벤트 시스템은 **이미 절반 지어져 있었다**: `StageType.Event`(맵 가중치 15%) · `GameState.Event` · `EventManager` 뼈대 · `Events.csv` 5행 · `UseTimerClear`/`SurvivalTime`(**시간제한 생존이 이미 된다**) · `MaxAlive`(차 있으면 소환이 **대기**한다, 취소가 아니다 → 죽인 만큼 즉시 채워진다) · `RecycleFarEnemies`(멀어진 적을 죽이지 않고 앞으로 옮긴다 → **도망은 이미 불가능**). ⇒ "무한적 + 시간제한"에 **새 전투 시스템이 필요 없다.** 무한 소환은 `WaveManager.cs:156` 의 `Count <= 0` 재해석 **한 줄**(§4 B안 권장). **진짜 벽으로 가두는 A안은 권장하지 않는다** — 뱀서라이크에서 몰리는 것은 회피 실력의 결과여야지 운이 되면 안 된다(§4-5 C안 권장). 🔴 **결정 6건 사용자 대기**(§7) — 사용자가 *"이벤트 관련은 나중에"* 로 보류. ⚠️ `E2 Elite Ambush` 는 `WaveManager.cs:193,206` 이 엘리트/보스 관문을 `StageType` 으로 하드코딩해 그 두 줄을 건드려야 한다 |
| **D21**<br>(C22) | 문어 촉수 링(`Fx_TentacleLash`)이 `m_SortingOrder: 20` 이라 **플레이어보다 앞**에 그려졌다(D16 육안 확인 → 요청-10 ③). DEV 가 낸 A(`Range` 축소)·B(`offsetDistance` 확대)를 CONTENT 가 **둘 다 반려** — A 는 Lv1 넓이의 44% 가 되고 B 는 플레이어에게 달라붙은 적을 링이 못 잡는다 | ✅ 해결 (2026-08-31 44차 → 2-48) — `Fx_TentacleLash.prefab:83` `m_SortingOrder` **`20` → `-5`** **한 줄**(`git diff --stat` = 1 file · 1 insertion · 1 deletion). 🔑 **CONTENT 가 낸 대조군이 문제의 성격을 바꿨다** — "검 아크는 **같은 order 20 인데 기사를 0.00% 덮는다. 중앙이 비어 있어서다**" ⇒ 기하 문제가 아니라 **합성 문제**이고 A·B 는 둘 다 헛다리. `-5` 는 새 층이 아니라 `Proj_ToxinField` 가 이미 쓰는 자리다(바닥 -100 < 촉수·독장판 -5 < 플레이어·적 0 < 픽업 1 … 검 아크 20). 🔴 **D19 와 일부러 분리**했다 — CONTENT 경고 "둘을 같이 바꾸면 어느 쪽이 들었는지 모른다". `Fx_SwingArc` 는 `20` 그대로. 검증은 **같은 프레임을 order 20/-5 로 두 번 렌더해서 뺐다**(플레이어 고정 · Lv3 · `Range` 2.2 · `SwingArcFx` 꺼서 프레임 정지): 기사 실루엣 336px 중 **가림 93px → 0px**(최악 프레임 `_5` 12.2% → 0) · 촉수 잉크 7829 → 7722(**손실 1.37%**, 바닥에 안 묻힌다) · **적 8마리를 링 둘레에 일부러 배치한 최악 조건**에서도 7826 → 7706(**1.5%**)이라 폴백 `-1` **불필요** · 육안으로도 기사 허리를 가로지르던 가닥이 뒤로 넘어감 · 콘솔 **Log 7건, Warning/Error 0**. 🔑 **CONTENT 예상 대가 3.7~4.2% 가 실측 1.37% 였는데 틀린 게 아니라 전제가 바뀐 것** — D19 로 문어가 1.20 뒤로 물러나 링 중심과 기사 중심이 어긋났다(예상은 공전 시절 기준). **`protected` 필드는 `RunCommand` 에서 안 보인다** — `WeaponBase.Data`/`Level` 이 `CS0122`, `[SerializeField]` 도 아니라 `SerializedObject.FindProperty` 는 null → NRE. `System.Reflection` 은 금지라 **public 우회로**(`AssetDatabase.LoadAssetAtPath<ItemData>` → `.WeaponRef`/`.CurrentLevel`/`.GetRange`)와 **스프라이트 시트 직접 로드**로 풀었다 |
| **D23** | 문서가 **36개 · 약 1.1MB** 로 불었다(`SETUP_STATUS.md` 혼자 **388KB**). 편집기로는 어디에 뭐가 있는지 못 찾아 매번 `grep` 를 돌렸다. **문제는 내용이 아니라 열람 수단**이었다 | ✅ 해결 (2026-08-31 45차 → 2-49) — **디렉터리 정션 하나**. `D:\obsidian_claude\UNITY_GAME\VS_LIKE` → `C:\Unity\VS_LIKE\Docs`. 🔴 **옮기지도 복사하지도 않았다** — 복사하면 두 벌이 되고 두 벌은 반드시 갈라진다. `CLAUDE.md` 의 `Docs/...` 경로 규정과 git 이력도 그대로 산다. vault 에 `00_INDEX.md`(36문서 전체 지도 + "어떤 문서에 적나" 라우팅표)와 `지도\개발 지도.md`(`SETUP_STATUS` 절 목차 · `DONE` 25건 한 줄 요약 · 문서↔실제 어긋난 곳 표 · `02_DEV_세션_프롬프트` 가 원본보다 뒤처진 7곳) 신설. 낡은 노트 2건은 **지우지 않고** `아카이브/` 로 옮기며 경고 배너(그중 🔴 *"스크립트 코드는 수정하지 마"* 는 2026-08-26 부로 **폐기된 규칙**). 🔴 **코드·애셋 변경 0 · Unity 미접촉**(`BOARD §2` 내내 `IDLE`) · repo `.md` **본문 무수정**(frontmatter 금지). 🔑 **교훈 — 검사 도구가 "이상 없음"을 반환하면 그 도구부터 의심한다**: 1차 링크 검사 `grep -oP '\]\(\K'` 가 **"전 노트 0 링크"** 를 돌려줬는데, 링크가 눈에 보이는데 0 이면 통과가 아니라 고장이다. `sed` 로 다시 도니 `지도\개발 지도.md` 의 **36개가 전부 깨져 있었다**(하위 폴더에서 `../` 누락). 고쳐서 최종 **57/57 도달, 깨짐 0**. **보고만 하고 안 고친 것** — 문서↔실제 불일치 3건(`D13` 머리말 *"12개 전부 PASS"* 인데 **⑪ 실패** · `C22` §4 의 72° 근거가 `C23` 실측으로 뒤집혔는데 표시 없음 · `C1` §2 소제목이 자기 결론과 반대) + 없는 대상 참조 4건(`SfxId.Crit` 예약만 · `offsetAngle` 72° **코드 자체가 없음** · `Assets/Scripts/{Weapons,Visual}/` **없는 폴더** · `TUNING.md §3` "문어가 서는 자리"는 `C23` 이 지움). `BOARD §0` 에 `Tools/Art/`·`Tools/Audio/` 누락은 CONTENT 소유라 요청-15 로 넘겼다. ⚠️ **병렬 사고** — CONTENT 의 `C24` 커밋(`4c282e2`)이 내가 스테이지도 안 한 `BOARD.md` 의 `D23` 줄을 같이 가져갔다(`add`+`commit` 을 한 명령으로 붙여야 하는 이유). ⚠️ `cmd //c mklink /J` 는 이 bash 에서 **안 된다**(MSYS 인자 파괴) — PowerShell `New-Item -ItemType Junction` |
| **D20**<br>(C23) | 적을 죽여도 나오는 게 **경험치 구슬 하나뿐**이었다. 자석·상자는 엘리트 전용이라 **잡몹은 사실상 아무것도 안 줬다** — 뱀서라이크에서 "잡몹을 밀어붙일 이유"의 절반이 비어 있었다 | ✅ 해결 (2026-08-31 46차 → 2-50) — CONTENT 요청-17 의 재료(그림 5 · SFX 3 · 확률 6 · 행운 곡선)를 게임에 넣었다. **행운 스탯 신설**(`StatBlock.Luck` → `PassiveData.BonusLuck` → `PassiveEffect` → `BalanceImporter` **임포트+익스포트**) · **시한 버프**(`GrantInvincibility`/`GrantHaste`) · **픽업 4종**(`Bomb`·`Invincible`·`Haste`·`Gold`) · **드랍표**(`RollPickupDrop`) · 애셋 8개 임포트 + `AudioLibrary` 24→**27** + 프리팹 4종. 🔑 **설계 판단 셋** — ① 드랍은 **한 번만** 굴린다(종류별로 굴리면 폭탄+무적이 같이 나오고 표의 "합계 6.7%"가 실제와 어긋난다. 행운은 합계가 아니라 **각 항목에** 곱해 종류별 비율을 행운과 무관하게 유지) · ② 힐 픽업은 새로 안 만들고 기존 `HealPickup.prefab` 을 드랍표에 넣되 **회복량을 `ExperienceManager` 가 주입**한다(건물 레벨이 없어 안 주면 **0 을 회복하고 조용히 사라진다**) · ③ 드랍표는 **나란한 배열**이다 — `SceneWiring.csv` 는 **구조체 배열을 못 쓴다**(`WriteProperty` 가 원소마다 `WriteScalar` 를 부르는데 구조체는 `Generic` 이라 `! 타입 미지원`). 🔴 `StatBlock.Zero()` 에도 `Luck` 추가(I-21) · 🔴 **익스포트도 같이** 고쳤다(안 그러면 다음 Export 때 `BonusLuck` 열이 사라져 CSV 가 조용히 망가진다) · 🔴 **`minAttackSpeed = 0.1f` 하한 신설**(`WeaponBase.cs:54` 가 쿨다운에 `AttackSpeed` 를 **곱한다**. 최악 조합 `Aegis −0.05 + AttackSpeed Lv5 −0.30 + 공속 −0.50 = 0.15` 로 여유가 **0.05** 뿐). **판정 6건 중 5건 PASS** — ① `BonusLuck` 기존 10개 전부 0 · ② 실체 높이 480 vs Magnet 482 · ③ `allItems` 25개 · ④ 행운 Lv5 에서 **2.001배**(5만 회 × 2) · ⑤ **실물리** Demon 사망 / Ogre 생존 + 11유닛 밖 무피해(반경 10 정확). ⑥ 소리 구분은 **귀로만 판정 가능 → 사용자 몫**. 🔑 **통계 이상은 로직보다 "세는 방법"을 먼저 의심한다** — ④ 첫 시행 −3.1σ 를 `ObjectPool` 이 활성 오브젝트를 재사용하지 않음을 확인해 계수 오류부터 배제한 뒤 시드를 바꿔 재실행, 편차 소멸(**시드 탓**). ⚠️ **씬 배선은 임시다** — `SceneWiring.csv` 가 CONTENT 소유라 요청-16 으로 넘겼다(그 행이 CSV 에 없어 Import 가 덮어쓰지 않는다). **고치지 않고 보고만 한 것** — 🔴 `B5` 새 증상(`Die():449` NRE 가 `DetonateBomb` 의 `foreach` **밖으로 전파**돼 뒤쪽 적이 피해를 안 받고 폭탄이 바닥에 남는다. 같은 모양이 `AoeProjectile`·`MeleeWeapon`·`SummonWeapon`·`ToxinField` 에도 → 우선순위 `낮음`→`재검토 필요`) · 스프라이트 bbox 기준 차이(CONTENT `a>127` vs `PIL` `a>0`, `Magnet`·`GoldGain` 의 잔여 픽셀 1.4~1.5% 가 bbox 를 최대 311px 부풀린다 — **측정 기준 차이지 애셋 결함 아님**) · `.meta` 복사 시 **GUID 말고 서브애셋 이름이 세 곳**에 박혀 있다 · `ExecutionResult.Log` 는 **정렬 지정자(`{1,-24}`)를 못 쓴다** · **대량 스폰 실험은 플레이 세션을 오염시킨다**(잔여 픽업이 먹히며 `LevelUp`(timeScale 0)에 들어가 `WorldPickup.Update` 가 조기 반환 → 다음 검증이 통째로 막힘) |
| **D24**<br>(C25) | `D20` 이 넣은 픽업 드랍표가 **DEV 가 손으로 꽂은 임시 배선**이었다. `SceneWiring.csv` 는 CONTENT 소유라 요청-16 으로 넘겼고 `C25` 가 3줄을 채워 돌려줬다 — **Import 1회**가 전부인 작업. | `Game/Balance/Import CSV -> ScriptableObjects` 1회 + `File/Save`. **코드·애셋·CSV 변경 0** | 판정 **6/6 PASS** — `SceneWiring.csv : 12/12 적용`(경고 0) · `pickupPrefabs`/`pickupChances` **6칸=6칸** · 확률 합 **6.7%** · `healPickupAmount` **30** · 죽은 필드 `magnetPrefab`/`magnetDropChance` `FindProperty` **둘 다 null** · 실제 사망 경로 **300 처치 → 21개(7.0%)**, 6종 전부 등장(Gold 9·Heal 5·Haste 3·Magnet 2·Bomb 1·Invincible 1) | 🔴 **`D20` 커밋(`a3e9e5c`)의 씬은 `allItems` `24` 였다 — 행운이 레벨업 3택에 안 뜨는 상태였다.** `D20` 은 **런타임 25개**를 읽고 판정 PASS 를 적었는데 `File/Save` 가 빠져 디스크에 안 들어갔다. 예외도 로그도 안 나는 종류다 ⇒ **씬·프리팹을 바꾸는 판정은 `git diff` 로 재확인**(`TODO.md` §1 등재) · `B5` 정량화 — 웨이브 밖 사망 **300/300 전부 예외**. 다만 예외가 `RollPickupDrop` **뒤**라 드랍은 살아 있다 |
| **D25** | 런 골드와 메타 골드가 **`MetaProgression.Currency` 하나**였다. 10층 런 수입 ≈1,240G 중 **처치 보상이 ≈1,084G** 인데 상점 아이템은 **5~12G** — 상점이 사실상 무제한. 문제는 "싸다"가 아니라 **값을 잡을 수가 없다**는 것이었다: 상점을 올리면 메타가 같이 비싸지고 메타를 내리면 상점이 공짜가 된다. **한 지갑에 성격이 다른 두 예산**이 들어 있었다 (`TODO.md` §2-B 결정 3) | ✅ 해결 (2026-08-31 48차 → 2-52) — 지갑을 둘로 갈랐다. `GameManager.RunGold`(처치·픽업·농장·이벤트 → 상점·리롤, **런 종료 시 소멸**) ↔ `MetaProgression.Currency`(**스테이지 클리어 보상만** → 영구 강화·해금). `GrantMetaGold` 는 `_pendingMetaGold` 에 적립만 하고 `SettleRun()` 이 런 종료에 한 번에 넘긴다. 코드 10개 파일. **씬·프리팹·CSV·SO 변경 0 · 수치 변경 0** | 판정 **8/8 PASS** — `GrantGold(100)`→`Run 0→100`(Meta 불변) · `GrantMetaGold(50)`→`Pending 0→50`(Meta 불변) · `SpendRunGold` 경계 `30`✅/`999999`❌ · `StartRun` 이 두 지갑 초기화 · **실제 사망 경로** 고블린 1마리→`Run 0→1` · **상점이 보는 지갑이 런 골드**(10G 슬롯: `0`❌/`9`❌/`10`✅) · `OnPlayerDied`→`Meta 1231→1319`(+88)·`Run 500→0` · UI 3곳(HUD `777 G` 런 / MainMenu `1231 G` 메타 / StageMap 런). 콘솔 에러 0 · 세이브 `1231` 복원 |
| **D26** | 직업 3종(폭발광·어쎄신·소환사) 값이 `DESIGN_CLASSES.md` §7-B 에 **전부 확정**돼 있는데 `Classes.csv` 를 쓸 수 없었다 — **그림이 없고, Unity AI 생성은 에디터 조작이라 CONTENT 세션이 못 한다.** 순서를 뒤집을 수도 없었다: `BalanceImporter.cs:848`(`LoadRef`)·`:861`(`LoadSpriteSheet`) 은 그림을 못 찾으면 **로그 없이 fallback** 해서 기본 그림이 조용히 박힌다 (`C6` 6단계의 유일한 막힘) | ✅ 해결 (2026-08-31 49차 → 2-53) — 초상 3장(`gpt-image-1-5`) + 걷기 시트 3장(`video-kling-v3-i2v-standard`) 생성, 임포트 설정을 `Warrior`/`Warrior_Walk` 와 **필드 단위로** 맞추고 4×4 16분할. **C#·씬·프리팹·CSV 변경 0** | 판정 **①~⑤ PASS** — 초상 4종 `ppu=1024 maxTS=512 filter=Point comp=Uncompressed mode=Single`, 시트 4종 `ppu=256 sprites=16`, 알파 전부 `0–255`, 색 81,800 / 11,265 / 135,134. ⑥(여백 30px)은 **기준선 `Warrior_Walk` 가 2px** 이라 기준 자체가 틀렸다 — 새 3장은 14/25/31px 로 전부 낫다. 🔴 **Kling 이 `success:false` 를 반환했는데 파일은 성공작이었다**(낡은 job GUID 재생) |
| **D27**<br>(B8·B9) | 이 프로젝트는 **성능을 한 번도 재 본 적이 없었다.** `ROADMAP.md` §6 에 *"`OverlapCircleAll` 이 할당한다"* 같은 **코드를 읽고 쓴 추측**만 있고 숫자가 없었다. 포트폴리오에도 *"만들었다"* 는 많은데 **"재고 고치고 다시 쟀다"** 가 없었다 | ✅ 해결 (2026-09-02 50차 → 2-54) — 부하 하네스를 만들어 **시나리오 A/B × 적 130/200/400/800 · 40회 이상** 측정. 🔑 **측정 전에 판정 기준을 `PERF.md` §1~§5 에 못박았다** — 재고 나서 기준을 정하면 유리한 숫자를 고르게 되기 때문이다. **가설 11개를 숫자로 죽였다**: 적↔적 충돌(매트릭스 `0x380` — **이미 꺼져 있었다**) · GC(**`GC.Collect = 0.000`**) · "적이 많으면 느리다"(**800마리 121 fps**) · 풀 `Instantiate`·사망 처리·데미지 팝업(각 4회 중 1회만 기준 초과) · **`OverlapCircleAll` 할당 8곳(프레임의 1.3 %)** · 투사체 `Update`(**1~3개뿐**) · `ExpDrop`(0.9 µs) · `DamagePopup`(1.6 µs) · **`Update()` 호출 단가(0.139 µs — 예측과 10배 차이)**. 🔴 **찾은 것 둘** — **`B8`**(이웃 12칸 무언 절단: 적 400에서 12~19 %, 800에서 58~71 %. `n` 히스토그램이 분포가 아니라 **벽**이다. **성능 우회책 안에 정합성 상한이 숨어 있었다**) · **적 겹침의 원인은 밀도**(800에서 85 %가 0.5유닛 안. 버퍼를 64로 키워도 84.6→85.1 % 로 그대로 ⇒ **절단은 증상이지 원인이 아니었다**). 고친 것: `NeighborBufSize` **12→64**(적 ≤400 절단 **0 %**) · `ExpDrop`/`WorldPickup` 의 씬 전체 순회·매 프레임 `GetComponent` 제거 · `DevPanel` 에 `Enemy Spawn` 신설. 🔴 **사전 등록 기준 5개 중 2개는 실패로 남겼다**(적 800에서 포화 0 미달 · 프레임 +5 % 초과) — 64 유지는 **판단이지 기준 통과가 아니다**. 🔴 **스스로 무너뜨린 결론 3개** — "렉이 없다"(A에서만 맞았다) · **`B9` 오진**(사용자 지적으로 `StageClearUI` 의 정상 정지임이 드러남) · "무기 렉의 범인은 사망 부산물"(근거였던 16 ms 가 **열화된 세션 한 번**의 값. 정상은 0.56~1.46 ms). **셋 다 그럴듯한 설명을 찾자마자 검증을 건너뛴 것이다.** 함정 11가지를 값과 함께 남겼다 — 특히 **품질 레벨을 올리면 vSync 가 따라 켜진다**(레벨별 저장) · **`Assets/Refresh` 는 재컴파일을 보장하지 않는다** · 🔴 **Unity 컴파일 에러가 `Error` 가 아니라 `Log` 타입으로 온다** · **세션이 다르면 같은 조건이 2배 흔들린다**. ⇒ 하네스가 매 실행 환경값을 직접 읽어 로그로 남기고 **`Version` 상수로 코드 반영을 조회**하게 했고, 그 가드가 **v8 코드로 v9 결과를 적을 뻔한 것을 막았다**. ✅ **계측 전량 제거**(심볼 소멸까지 조회 확인) · **`SampleScene.unity` 한 줄도 안 바뀜**(하네스를 저장하지 않고 메모리에만 뒀다). 곁가지로 **건물 앞 `E` 승급이 촬영(S1) 중 처음으로 실제 조작 경로 검증** — 외형 변화까지 확인, 반 년 열려 있던 `TODO §1` 항목이 닫혔다 |
| **D28** | `C27` 이 상점·해금 가격을 두 예산에 맞춰 다시 잡아 CSV 에 **저장만** 해 뒀다. CSV 는 원본일 뿐 **Import 를 돌려야 게임이 본다** — 그리고 Import 는 에디터 조작이라 CONTENT 가 못 한다. 같이, `D27` 이 새로 확인한 **Unity MCP 함정 둘이 `CLAUDE.md` 에 안 들어가 있었다**(`PERF.md` 에만 있었다 — 매 세션이 읽는 건 `CLAUDE.md` 다) | ✅ 해결 (2026-09-03 51차 → 2-55) — Import 1회. `ItemData` **25개** 가격 `5~12→40~110` · 씬 오버라이드 5개(리롤 `3/1→25/15` · 레벨업 리롤 `1→10` · 해금 `30/15→150/75`). `CLAUDE.md` §4 에 함정 2행 신설. **C#·CSV·그림 변경 0** — 요청-20 §3 의 코드 2건은 *"알아만 둘 것"* 이라 안 건드렸다 | 판정 **5/5 PASS** — `Economy.csv 49/49` · `SceneWiring.csv 12/12` · `Items 28` · 실패 줄 0 · `Sword.ShopPrice 8→70` · 씬 `ShopManager 3→25 / 1→15`. 🔑 **⑤(`StageMapManager` 무변화)가 판정 하나를 공짜로 더 만들었다** — 오버라이드 0개는 *값이 같다* 와 ***임포터가 컴포넌트를 찾았다*** 를 동시에 뜻한다(못 찾았으면 `43/49` + `!`). ⇒ `weightShop` 튜닝 경로가 **부작용 없이** 증명됐다. ⚠️ 알게 된 것 둘 — 필드가 `shopPrice` 가 아니라 **`ShopPrice`**(소문자로 조회하면 NRE) · **값은 프리팹이 아니라 씬 인스턴스 오버라이드로 들어간다**(프리팹은 여전히 `3/1`). 🔴 품질 레벨 `0→5`(Ultra) 는 **사용자 판단으로 유지** — 대신 **Ultra 는 `vSyncCount: 1` 이라 에디터 플레이가 144 Hz 에 고정**된다(`PerfHarness` 가 지워져 꺼 주는 것도 없다) |
| **D29**<br>(B10) | `D27` 이 남긴 결정 *"구슬에 수명 상한을 줄 것인가"* 에 `C28` 이 **"두지 않는다"** 로 답했다 — 얻는 게 0.5 ms(예산의 3 %)인데 잃는 게 **자석 픽업의 존재 이유**라 거래가 성립을 안 한다. 대신 `RecycleFarEnemies` 의 원칙(*지우면 경험치가 증발하니 옮겨서 다시 쓴다*)을 구슬에도 적용해 달라고 했다. **적에게는 그 규칙이 있고 구슬에는 없었다** | ✅ 해결 (2026-09-03 52차 → 2-56) — `WaveManager.RecycleFarExpDrops()` 를 `MaintainRoutine`(0.25초) 안 `RecycleFarEnemies()` 옆에 넣었다. 새 코루틴·새 상수 없이 `RecycleRadiusMult 1.9` 를 **적과 공유**한다. 🔴 **그 전에 `B10` 을 먼저 닫아야 했다** — `LevelUpManager` 에 **큐가 없어** 한 번의 `CollectXp` 로 3레벨이 오르면 패널이 덮어써져 **카드 2장이 조용히 사라진다**(정지가 아니라 손실이라 안 보였다). 합산 호출이 이걸 **기본 동작**으로 만들 참이었다. 씬 전체 순회를 새로 만들지 않으려 `ExpDrop.Active` 정적 목록을 뒀다(`PullAllToPlayer` 도 이걸 쓴다). **씬·프리팹·CSV·SO 변경 0** | 판정 **5/5 PASS** — 거리 사다리 `5·15·25·35·37·39·45·80` → 걷힌 것 `39·45·80` · 남은 것 `15·25·35·37` ⇒ **임계값이 정확히 38 임을 경계 양쪽으로 증명**(교훈 180 을 설계에 먼저 넣었다). `원거리 구슬 회수 3개 · XP +3` · `CurrentXp 4`(회수 3 + 자동흡수 1) · 콘솔 에러·경고 0. `B10` 은 `CollectXp(40)`→`Lv 1→4` 후 **`HidePanel` 3회를 다 써야** `Wave` 복귀(수정 전이면 1회). 🔑 **설계 도중 상호작용 하나를 더 잡았다** — 진화 제안 패널 중 레벨업이 들어오면 진화 카드 선택이 레벨업 빚으로 세어져 **대기 중인 레벨업이 사라진다**. `_panelIsForced` 로 갈랐다. ⚠️ **밸런스가 바뀐다** — 소실되던 구슬이 회수되므로 경험치 수입이 는다. 수치는 실플레이 로그를 보고 CONTENT 가 정한다 |
| **D30** | `ROADMAP.md` §5 가 *"자료구조만 있고 게임에 안 붙어 있다"* 로 남겨 둔 자리. 메타 골드는 **벌기만 하고 쓸 데가 없었다** — 죽으면 아무것도 안 남는다. 백엔드(`PurchaseUpgrade`·`GetStatBonus`·저장/로드)는 이미 완성돼 있었고 `PlayerStats.cs:228` 도 이미 부르고 있었는데, **애셋 0개**(`UpgradeDefinition` 이 `MetaProgressionManager.cs:51` 안에 있어 `I-19` 함정) · **화면 없음** · **진입점 없음** 셋이 비어 있었다 | ✅ 해결 (2026-09-03 53차 → 2-57) — `UpgradeDefinition` 파일 분리(`I-19`) → `Upgrades.csv` + `ImportUpgrades` 신설 → 애셋 7개 → `MetaScreenUI`/`UpgradeCardUI` + `MetaScreenPanel`/`Prefab_UpgradeCard`(기존 것 복제) → 메인 메뉴 `Upgrades` 버튼. 값은 CSV 가 원본이라 인스펙터 예외를 두지 않았다(사용자 판단) | 판정 **6/6 PASS** — 카드 7장 · 구매 `Lv0→1`·`-120G` · 잔액 0 거절 · **`StartRun` 후 `MaxHp 140→150`(다음 런 반영)** · `Back` 복귀 `timeScale 1` · 콘솔 0. 🔑 **애셋 7개의 `m_Script` 가 전부 `UpgradeDefinition.cs` 의 guid** — 분리를 먼저 해서 `I-19` 를 피했다. 🔴 **새 폴더를 만드는 임포터에서만 터지는 `EnsureFolder` 버그를 밟았다** — `StartAssetEditing()` 안에서 `IsValidFolder` 가 방금 만든 폴더를 못 봐서 `UpgradeData 1`~`6` 이 생겼다. `Directory.Exists` 를 같이 보게 고쳤다(기존 폴더는 이미 디스크에 있어 여태 안 드러났다). ⚠️ 검증이 **실제 세이브를 쓴다**(`PurchaseUpgrade`→`Save`) — 백업 후 복구했다. ➕ 곁다리로 Import 가 `Classes : 10` 을 만들어 **직업 3종이 게임에 들어갔다**(`C6` 6단계 닫힘, `GameManager.classes` 6개) |
| **D31** | `ROADMAP.md` §2-5 — **보스가 큰 잡몹이다.** `Boss1` = `Ogre` + `BossHpMult 7` 이 전부고 AI 가 `Chaser` 라 잡몹과 같은 코드로 걸어왔다. HP 1540 을 깎는 동안 화면에 **아무 표시도 없어서** 보스전인 걸 알 수 있는 건 BGM 뿐이었다 | ✅ 해결 (2026-09-03 54차 → 2-58) — `BossPatternData`(단독 파일, I-19) + `Bosses.csv` + `ImportBosses` → `BossBrain`(페이즈·소환) · `BossSlam`(예고 원 → 폭발, **플레이어**를 친다) · `BossHealthBarUI`. 🔴 **보스 로직을 `EnemyBase.FixedUpdate` 에 안 넣었다** — 거기는 적 800마리가 매 물리프레임 도는 자리라 분기 하나가 **보스 없는 웨이브에서도 800번** 돈다. `EnemyBase` 는 **읽기 전용 접근자만** 열었다 (HP 를 쓰면 방어·사망 처리를 건너뛴다). `Enemies.csv`(33열)는 안 건드리고 `Bosses.csv` 가 `EnemyData.BossPattern` 을 되꽂는다 | 판정 **8/8 PASS** — 페이즈 `0.67→2` · `0.37→FINAL`(색까지 전환) · 소환 **Goblin 0→4** · 슬램 scale 6.40(=반경 3.2×2) · **명중 `HP 130→112`(20−방어2) / 예고 중 회피 시 `112` 그대로** · 처치 시 바 꺼짐 · 콘솔 0. 🔑 **⑦(예고 회피)이 핵심이다** — 예고 없이 터지면 패턴이 아니라 체력 깎기다. 맞는 경우와 피하는 경우를 **둘 다** 쟀다. 🔴 **검증 중 내 오독을 잡았다** — 잡몹 4→15 를 소환으로 봤는데 웨이브 자체 소환이었고, 내가 쓴 `ResolveSummon` 이 웨이브 목록에서만 찾게 해 **소환이 통째로 죽어 있었다**(내가 심은 경고가 잡았다). 참조를 임포터가 꽂게 고치고, 재검증은 **"Goblin 수"** 로 바꿔 오독이 불가능하게 했다 |
| **D32** | `C28` 이 그림 통과 뒤 `Classes.csv` 3줄 + `SceneWiring.csv` 1줄을 쓰고 **Import 1회**를 요청했는데(요청-22) 그 요청이 **열린 채 남아 있었다.** Import 자체는 `D30`·`D31` 작업 중에 이미 돌아 있었지만 **CONTENT 의 판정 기준으로 확인한 적이 없었다** — 특히 ④(`Portrait`·`BodySprite`·`WalkSheet`)는 `LoadRef`·`LoadSpriteSheet` 가 **로그 없이 fallback** 하므로 Import 로그가 깨끗해도 빌 수 있다 | ✅ 해결 (2026-09-03 55차 → 2-59) — Import 재실행 후 판정 7개를 전부 다시 측정. **코드·애셋 변경 0** — 재실행이 `git status` 를 안 바꿨다(임포터가 멱등하다는 증거이기도 하다) | 판정 **7/7 PASS** — `Classes : 10` · `m_Script` 셋 다 `74ae27f0…`(=`CharacterClassData.cs`) · 🔴 **④ 그림 3필드 전부 채워짐**(YAML 에서 `{fileID: 0}` 여부로 직접 판정) · `WalkFrames` 16장 · 선택 화면 **6개**(전부 `Tier=1`, 승급 4종 제외, 캡처 확인) · 시작 무기 3종 정확 · **스탯이 §7-B 와 한 자리도 안 틀림** · 콘솔 0. 🟡 **내가 한 번 잘못 봤다** — 한 세션에서 `StartRun` 을 3회 불러 무기가 쌓이길래 버그로 의심했으나, 런 종료 경로가 전부 씬을 리로드해 `StartRun` 은 **씬 로드당 1회**뿐이다. 깨끗한 단일 런에서 무기 **정확히 1개**. **등재 안 했다** |
| **D33** | `D30` 이 만든 메타 화면의 수치가 임시였고 `C29` 가 근거를 갖고 다시 잡았다. 그런데 **항목 구성이 바뀌었다**(Greed 제거 · Precision 신설) — `MetaProgressionManager.upgrades` 는 `D30` 이 **씬에 손으로 꽂은 배열**이라 임포터가 안 건드린다. 그대로 Import 하면 Greed 가 화면에 계속 뜨고 Precision 은 만들어져도 안 뜬다 | ✅ 해결 (2026-09-03 56차 → 2-60) — `SceneWiring.csv` 에 **`MetaProgressionManager,upgrades` 줄 신설**(12→13행)로 목록 자체를 CONTENT 손에 넘겼다. **C# 변경 0** — `ImportComponentFields` 가 이미 범용이라 컴포넌트를 타입 이름·필드를 `SerializedProperty` 이름으로 찾고 ObjectReference 배열을 `|` 로 채운다(`GameManager,classes` 와 같은 경로) | 판정 **6/6 PASS** — `Upgrades : 7` · `!` 0건 · `SceneWiring 13/13` · 씬 배열이 **지정 순서 그대로 재배선**(Greed 빠짐·Precision 들어감, 캡처 확인) · 가장 싼 칸 `60G` · 🔑 **새 `StatKey` `CritMultiplier` 가 `1.50 → 1.65` 로 실제 반영**(오타였으면 조용히 0 이었을 자리) · 콘솔 0. 🔑 **`C29` 의 정정 4건을 전부 받아들였다** — `GrantMetaGold` 도 `GoldGain` 을 곱한다(되먹임) · `Mathf.Max(1, raw-Armor)` 는 정액+바닥 · 메타가 레인저 기동(+0.6)을 넘으면 안 된다 · 총량 8,600→3,910(마감 기준). **내 근거는 하나였고 그쪽은 코드·규칙·마감을 같이 봤다** |
| **D34** | 🔴 **`D31` 의 판정 ⑦ 이 불충분했다.** *"예고를 깔고 빠져나가면 안 맞는다"* 를 `transform.position` **순간이동**으로 확인했는데, 그건 회피 **가능**이 아니라 *판정이 터지는 순간 위치로 이뤄진다*만 증명한 것이다. `C30` 이 코드를 읽고 계산했다 — `BossSlam` 은 원을 깔고 안 따라오므로 **필요 속도 = `radius / windup`** 이고, 내 값은 **3.56 u/s** 라 **폭발광(3.5)은 반응 0초에도, 레인저(4.6)조차 반응 0.25초면 못 피한다** | ✅ 해결 (2026-09-03 57차 → 2-61) — `C30` 값으로 Import: `SlamWindup 0.9→1.15` · `SlamRadius 3.2→2.4` · `SlamDamage 22→18` · `PhaseSpeedMult 1|1.15|1.35 → 1|1.5|2.2`(보스 실속도가 1.08 이라 1.35배는 체감이 없다). 예고 원 전용 스프라이트(`BossSlamRing.png`)를 `ppu=512 Single Bilinear` 로 임포트해 프리팹에 꽂았다 — `bounds.extents.x` 가 정확히 `0.5` 라 **`spriteRadiusAtScaleOne` 을 한 글자도 안 고쳤다** | 판정 **6/6 PASS** — 🔑 **이번엔 걸어서 쟀다.** `PlayerController` 를 끄고 `linearVelocity` 에 실제 이동속도를 넣고, 반응 0.25초는 *예고를 0.25초 줄이고 t=0 부터 걷기* 로 모형화. 🔴 **대조군을 먼저 놓았다** — 옛 값(필요 4.92)에서 **HP 110→93 으로 맞는 것**을 확인한 뒤 새 값(필요 2.67)에서 **93 유지**를 확인했다. 그게 없으면 "안 맞았다"가 **테스트 고장과 구별되지 않는다.** ④ `scale 4.8` = 반경 2.4 (보이는 원 = 판정 원) · ⑤ 3페이즈 보스 **2.38** vs 플레이어 **3.50**(못 쫓아온다) · 콘솔 0 |
| **D35**<br>(B5·B7) | 열린 버그가 셋 남아 있었다 — `B5`(웨이브 밖 사망 NRE · **광역기가 첫 사망자에서 통째로 멈춘다**) · `B7`(승급 4종 임포트 설정이 T1 규약과 다르다 · 등재만 해 둠) · `B3`(건물 큐 순서 · 사용자가 B안 선택 후 보류). 그리고 `D27` 이 *"무기 렉의 원인 미확정"* 을 남겼다 | ✅ 해결 (2026-09-03 58차 → 2-62) — `B5` 는 `OnEnemyKilled` 에 `_currentWaveData != null` 한 줄 + **같은 모양이던 `SpawnMinion`(D31 산)에도 가드**. `B7` 은 `D26` 명령을 4종에 돌려 **7종이 같은 설정으로 수렴**. 🔴 **`B3` 은 안 건드렸다** — A·C 안은 큐 순서를 바꿔 건물 채수 밸런스에 영향이 간다 | `B5` 판정 — 광역 사례로 쟀다: **루프가 2마리를 다 때렸고**(안 고쳤으면 1) Ogre `HP 26`(=220−200+방어6) · Demon 사망 연출까지 정상 · **활성 시체 0구** · `NullReference` 0건. `B7` — 초상 7종 `ppu=1024 maxTS=512 Point 무압축` · 걷기 7종 `ppu=256 maxTS=1024 Point 무압축` 전부 일치. 🔴 **성능은 절반만 답했다** — 적 815마리에서 중앙값 **5.08 ms**(전 프레임 60fps 안)로 시나리오 A 는 건강하지만, **시나리오 B(무기 켜고 대량 처치)는 재현 못 했다**(`ExpDrop 0` 은 구슬이 생길 조건이 아니었기 때문이지 D29 의 효과가 아니다). 곁가지로 **`EditorLoop` 가 프레임의 45 %** 라는 게 나왔다 — 에디터 수치로 게임 성능을 판정하면 안 된다 |
| **D36** | `ROADMAP` 5단계(게임패드 · 없는 화면 · 콘텐츠 볼륨) 중 무엇을 할지 사용자가 정했다 — **조작 안내는 새 화면 대신 기존 문구 가독성만**, **통계는 작업**, 이벤트는 제안만, **게임패드는 스킵**. `[Z] Build`·`[E] PROMOTE` 는 조건부 표시가 이미 돼 있었지만 **외곽선·그림자가 0 이라 어두운 타일 위에서 안 보였고**, `TotalRuns`/`TotalKills` 는 쌓이는데 **볼 곳이 없었다** | ✅ 해결 (2026-09-03 59차 → 2-63) — 전용 머티리얼 `Pretendard SDF - Prompt.mat` 파생(외곽선 0.22 · 그림자 · 크기 26→32/30→36)을 **두 문구에만** 물렸다(공유 머티리얼을 고치면 텍스트 75개가 전부 바뀐다). 통계는 새 화면 대신 **메타 화면 하단 한 줄** — 화면을 더 만들면 그만큼 더 안 보게 된다. ➕ `SaveData.TotalPlaySeconds` 신설(`WaveManager.TotalElapsedTime` 이 쌓이는데 저장이 안 됐다) | 판정 — 문구는 캡처로 확인(**전용 머티리얼 2개 · 나머지 75개 그대로**), 통계는 실제 세이브값 `RUNS 25 KILLS 8,885 AVG 355/run` 표시, 콘솔 0. 🔴 **두 번 틀리고 캡처로 잡았다** — ① 머티리얼 복제 후 `ShaderUtilities.UpdateShaderRatios` 를 안 불러 **글자가 통짜 덩어리로 렌더**됐다 ② `StatsText` 를 top 앵커로 만들어 **카드 위에 겹쳤다**(그 패널은 center 앵커를 쓴다) |
| **D37** | 이벤트는 배관만 뚫려 있고 **안이 비어 있었다**(`ROADMAP` §7). `EventManager` 는 58줄 스텁이라 `XpBonus`·`CurrencyBonus`·`TriggerRandomWave` 셋뿐이었고 제목·설명이 **`Debug.Log` 로만** 나가 **플레이어가 무슨 일이 났는지 못 봤다.** `WaveManager` 에 `StageType.Event` 분기도 없어 **전투형이 존재할 수 없었다** | ✅ 해결 (2026-09-03 60차 → 2-64) — 선행 2건(**이벤트 UI** · **Event 웨이브 분기**)을 먼저 놓고 **E7 `Exchange`**(런 골드 → 메타 골드 3:1) · **E9 `Field Promotion`**(다음 전투 동안 제단 없이 승급) · **E11 `Minefield`**(바닥에 예고 원)를 넣었다. 🔑 **E11 은 새 시스템을 안 만들고 `BossSlam`(D31·D34)을 재활용** — 이벤트가 다른 건 규칙이지 부품이 아니다. `Events.csv` 5→8종, 임포터가 `Kind` 오타와 빈 `DeclineLabel` 을 경고한다 | 판정 **3/3 PASS** — E7 `런 137→17 · 메타 1231→1271(+40)`(상한 40 이 정확히 걸림) · E9 `False→True` · E11 지뢰 3개 `scale 4.0`(반경 2.0) 캡처 확인 · 거절 버튼이 선택형에서만 보임 · 콘솔 0. 🔴 **E9 에서 진짜 결함을 잡았다** — 해제를 `AdvanceToNext` 에 뒀는데 `FinishEvent` 가 그 함수를 불러 **켜지자마자 꺼졌다.** 원인은 코드가 아니라 *"이번 층"* 이라는 **애매한 정의**였다(이벤트 노드 자체가 그 층의 내용물이라 효과가 쓰일 시간이 없다) ⇒ **"다음 전투 동안"** 으로 다시 정의하고 `ClearWave` 로 옮겼다. 🟡 `Events.csv` 를 다시 쓰며 CONTENT 의 `Cursed Offering` 행을 실수로 지웠다가 되살렸다 |
| **D38** | 내가 `요청-27` 로 **보스 전용 그림**을 요청한 직후, CONTENT 의 [`DESIGN_ART.md`](DESIGN_ART.md)(`C32`)가 올라와 **정반대 순서**를 냈다 — 보스 그림은 **7순위(보류)**, 이유는 *"층별 난이도 기능이 먼저다"*. 그리고 §7 이 애셋 3개를 *"아무 데도 안 쓰인다"* 며 정리 후보로 올렸다 | ✅ 해결 (2026-09-04 61차 → 2-65) — 사용자가 CONTENT 순서를 택했다. `요청-27` 을 **보류로 내리고**(원문은 접어서 보존) `요청-28` 로 순서를 수용, 1~5번은 **CONTENT 가 만들고 DEV 는 배선만** 한다. 🔴 **§7 정리 후보 3개를 guid 로 실측한 결과 셋 다 참조가 있었다** — 특히 `Exp_Orb.gif` 는 `ExpDrop.cs` 에 `sprite` 대입이 **0건**이라 **지웠으면 경험치 구슬이 통째로 안 보일 뻔했다** | 실측 로그 — `goblin.png`(guid `588cf3bf`) → `Enemy_Goblin.prefab` 1곳 · `Exp_Orb.gif`(`985386b2`) → `ExpDrop_Small.prefab` 1곳 · `Bullet.png`(`1f4e1489`) → `Proj_Bullet` + `Proj_EnemyBolt` **2곳**. 코드 대조 — `EnemyBase.cs:124` 는 덮어쓰고 `ExpDrop.cs` 는 안 덮어쓴다. **문서만 바꿨고 코드·씬은 안 건드렸다** |
| **D39** | `B3` — Turret·Restaurant 가 설치된 상태에서 **Village 를 얻고 `Z` 를 눌러도 Turret 이 먼저 나왔다.** `D2` 에서 B안(HUD 표시)만 넣어 두고 **큐 순서는 그대로** 둔 채 `보류` 로 열려 있었다 | ✅ 해결 (2026-09-04 62차 → 2-66) — **C안**: 대기열을 **신규 구간 + 증설 구간**으로 나눈다. 처음 해금은 `Insert(_freshCount++)`, 레벨업 증설분은 `Add()`. 🔑 **원안의 "맨 앞(index 0)"은 안 썼다** — 그러면 나중에 해금한 게 먼저 해금한 것을 **추월**한다. `_freshCount` 는 장부라 `PlaceNext`(`RemoveFront()` 로 통일) · `LockBuilding`(임의 위치 제거) · `ResetRunState` 가 전부 같이 맞춘다 | 판정 **2/2 PASS** — 리포트 재현 절차 그대로: 3번에서 맨 앞이 `Turret`, 4번 Village 해금 직후 `Village` 로 **뒤집혔다**(구버전이면 `Turret`). 5번 Farm 추가에도 맨 앞은 `Village` — **추월 없음**. 최종 `Village > Farm > Turret`. `LockBuilding` 장부 별도 시험도 기대값과 일치. 콘솔 Error·Warning **0건** |
| **D40** | CONTENT `C33` 이 만든 그림 5장(탄환 2 · UI 9-slice 프레임 3)이 애셋 폴더에 들어와 있었는데 **임포트 설정이 안 돼 있었다.** 특히 `spriteBorder` 는 **0 이어도 에러가 안 나고 9-slice 만 조용히 죽는다.** 그리고 `Proj_Bullet` 과 `Proj_EnemyBolt` 가 **같은 `ICON/Bullet.png`** 를 써서 내 탄과 적 탄이 색으로만 갈렸다 | ✅ 해결 (2026-09-04 63차 → 2-67) — 5장 임포트 설정 + 탄환 프리팹 2개 교체 + **9-slice 배선 49곳**(씬 38 · 프리팹 11). 🔑 작은 버튼은 `spriteBorder` 만으로 안 된다 — 38×38 버튼에 20+20px 테두리라 **버튼보다 테두리가 크다.** `pixelsPerUnitMultiplier` 규칙(테두리 총합 ≤ 짧은 변의 60 %)을 만들어 5단계로 적용했다 | 판정 **6/6** — `spriteBorder` 40/28/20 을 **임포터가 아니라 `Sprite.border` 에서** 읽어 확인 · 탄환 모양 구분 캡처 확인 · 9-slice 3화면 캡처 확인 · 에러 0. 🟡 CONTENT 의 `50px` 전제가 틀렸다(**실측 41px**) — 크기가 +22 % 지만 명중 판정이 `CircleCollider2D 0.5` / `HitRadius 0.45` 고정이라 **게임플레이 변화 0**. 🔴 곁일로 `B11`(상점 대사 한글 tofu) 발견·등재 |
| **D41** | 메타 강화 카드가 **글자만** 나왔고(`Icon` 열이 비어 있었다), **승급에 연출이 하나도 없었다** — 보스 등장엔 화면 흔들림, 엘리트 처치엔 히트스톱이 있는데 **가장 큰 성취인 승급만 조용해서 비중이 거꾸로**였다(C34) | ✅ 해결 (2026-09-04 64차 → 2-68) — 그림 3장 임포트 + `Upgrades.csv` Import + `PulseFx` 신설 + 프리팹 `Fx_Promote`/`Fx_LevelUp` + `EvolutionManager` 호출 + `SceneWiring.csv` 2줄. 🔴 **검증 중에 내 결함을 잡았다** — `Time.deltaTime` 을 썼는데 이 연출이 뜨는 두 순간이 **둘 다 `timeScale = 0`** 이라 **얼어붙은 링이 화면에 그대로 붙어 있었다**(scale 0.30 · alpha 1.00 에서 정지). `unscaledDeltaTime` 으로 고쳤다 | 판정 **5/6 PASS · 1 보류** — ① `Upgrades : 7` · `!` 0건 · ② 아이콘 **7/7** (로그가 아니라 애셋 필드를 하나씩 읽었다) · ③ 메타 화면 캡처 · ④ `EvolveClass(Sentinel) = True` · `PulseFx` **0 → 1** · ⑥ 링이 비어 있다(캡처) · ⑦ 콘솔 0. 🟡 **⑤ 는 통과라고 못 쓴다** — 파동은 뜨지만 `ScreenSpaceOverlay` 패널이 덮어 **레벨업 순간엔 안 보인다**(패널만 끄고 찍으면 정확히 떠 있다) → `TODO.md` 결정 3안. 🟡 곁일로 고아 애셋 `UpGoldGain.asset` 발견 |
| **D42** | 메인 메뉴가 **전부 글자**였고(배경 0장) 상점의 `MERCHANT` 칸에 **인물이 없었다.** CONTENT 의 `DESIGN_ART.md` §6 에서 **인물·배경은 절차적 생성이 안 되는 유일한 항목**이라 DEV 가 Unity AI 로 뽑아야 했다 | ✅ 해결 (2026-09-04 65차 → 2-69) — `ShopKeeper.png`(1024² · PPU 1024 · Point) · `MainMenuBG.png`(**1344×768 16:9** · PPU 100 · Bilinear) 생성·임포트·배선. **코드 0줄.** 🔴 **`I-41` 가짜 투명이 또 났다** — 프롬프트에 *"fully transparent background"* 를 명시했는데도 **알파가 전부 255** 였다. `RemoveImageBackground` 로 처리 | 판정 **7/7** — ① 알파 min **0** · 모서리 **0,0,0,0** · ③ 배경 중앙 띠 밝기 **0.150** (≤ 0.25) · ④ 세로 점유 **86.1 %** (대역 75~88) · ⑤⑥ 캡처 확인(타이틀·버튼 4개 전부 읽힘) · ⑦ 콘솔 0. 🔴 CONTENT 의 배치 지시(*"형제 순서 0"*)를 **일부러 안 따랐다** — 그 자리의 `DimBG` 가 **알파 1.00 불투명**이라 배경이 통째로 가린다. 순서 1 로 넣었다. 🟡 상점 주인을 처음에 `y=-740` 에 놨다가 패널 밖으로 나갔다(`pivot` 이 위쪽이라 그 값이 **윗변**) |
| **D43** | 사용자 결정 2건 — ① `B11` 상점 대사 5줄이 **빈칸**(씬의 옛 한글이 코드를 덮는다) ② `D41` 이 남긴 **레벨업 파동이 패널에 가려 안 보이는** 문제 | ✅ 해결 (2026-09-04 66차 → 2-70) — ① 씬 **과 프리팹 원본** 둘 다 영문으로 덮었다 ② **A안**: `HUDManager.OnLevelUp` 이 아니라 **`LevelUpManager.HidePanel` 이 패널을 실제로 내릴 때** 띄운다. 큐가 여러 개여도 **마지막에 한 번**, 진화 제안 패널에는 **안 뜬다** | 판정 **3/3** — 파동: 패널 열림 직후 `PulseFx = 0`(D41 에선 **1**) · 닫힘 직후 `1` · `timeScale = 1`. 대사: 화면 문자열 `"Stronger foes are waiting..."` · tofu 경고 **0건**. 🔴 **씬만 고쳤으면 부활했을 것** — 프리팹 원본에 한글 5줄이 남아 있었다. 전수 조사(씬 문자열 263 · 프리팹 69)에서 **한글 0건**, 스캐너에 대조군을 넣어 배열까지 보는 걸 증명했다 |
| **D44** | `D40` 에서 두 투사체의 스프라이트를 새 그림으로 바꾼 뒤 `Assets/Game/ICON/Bullet.png` 이 **아무 데도 안 쓰이게 됐다.** CONTENT 가 *"지워도 되지만 내가 지울 수 없다"* 며 DEV 판단으로 넘겼다(`C36` 요청-28) | ✅ 해결 (2026-09-04 67차 → 2-71) — guid 로 참조를 다시 재고 **0곳**을 확인한 뒤 `.png` + `.meta` 삭제. 🔴 **첫 스캔은 버렸다** — `grep -P` 가 로케일 오류로 실패해 guid 가 **빈 문자열**이 됐고, 빈 패턴이라 **Assets 의 거의 모든 파일**이 참조로 잡혔다. 그대로 믿었으면 **정반대 결론**이 나온다 | 판정 — 구 guid `1f4e1489…` 참조 **0곳** · 대조군(신 `PlayerBullet` guid `7bbb40b5…`)이 `Proj_Bullet.prefab` **1곳**으로 잡혀 스캔 유효성 확인. 삭제 후 `Bullet.png` 로드 `null`, `Proj_Bullet` → `PlayerBullet` · `Proj_EnemyBolt` → `EnemyBolt` **둘 다 살아 있음**. 콘솔 0 |
| **D45** | 🔴 **난이도 곡선이 존재하지 않았다.** `WaveManager.StartWave` 가 노드 타입만 보고 웨이브 풀에서 **무작위로** 뽑아서 **1층에서 Normal3(108마리)이 나오고 9층에서 Normal1(64마리)이 나올 수 있었다.** `ROADMAP` §3 결정 4에서 *(B) 전역 배율 먼저* 로 방향만 정해 두고 2026-08-29 이후 안 건드린 항목이다 | ✅ 해결 (2026-09-04 68차 → 2-72) — `LayerScaling` 정적 클래스 신설. 층당 **체력 +10 % · 접촉 피해 +6 % · 소환량 +8 %**(계수는 `Economy.csv` 의 `WaveManager` 4행). 🔑 **소환 수만 올리면 아무 일도 안 일어난다** — `MaxAlive` 상한에서 대기줄만 길어진다. 소환 수 · 동시 생존 상한 · 처치 목표에 **같이** 걸었다. 🔴 이동 속도는 **일부러 뺐다** — 적이 플레이어보다 빨라지면 난이도가 아니라 조작 불능이다 | 판정 **6/6** — **1층 대조군 x1.00** · 9층 HP **x1.90**/피해 **x1.54** · 진짜 경로 `StartWave(Layer 7)` → `1.70/1.42/1.56`(CSV 계수 그대로) · 🔴 **`WaveData` SO 원본 무오염 확인**(런타임에 쓰면 애셋이 더러워진다) · `StartRun` 에서 `Reset()` · 콘솔 0. 🟢 **이걸로 보스 그림 보류의 재개 조건이 다 풀렸다** |
| **D46** | **뭘 들고 있는지 인게임에서 확인할 방법이 없었다**(`ROADMAP` §3-1). 레벨업 카드는 고르는 순간만 보이고 상점은 파는 것만 보여 준다. 스탯도 마찬가지로 **어디에도 안 나온다** | ✅ 해결 (2026-09-04 69차 → 2-73) — `StatsPanelUI` + `ItemChipUI` 신설. 전투 중 **TAB 을 누르고 있으면** 스탯 13종 + 직업 사슬 + 보유 아이템 칩이 뜬다. 🔑 사용자 요구로 **멈추지 않고 느려진다**(`timeScale 0.25`) — 그래서 창은 화면 **왼쪽만** 덮는다. 🔴 `timeScale` 은 공유 자원이라 `GameManager.DoHitstop` 과 **같은 계약**을 따른다 (웨이브 중에만 · 이미 1 이 아니면 안 연다 · 닫을 때 다시 확인). 🟢 `Prefab_ItemChip` 은 **이미 있었는데 아무도 안 쓰던 부품**이라 새로 안 만들고 살려 썼다 | 판정 **4/4** — TAB 으로 열림(`0.25` · 칩 7 · 스탯 13줄, 캡처) · 🔴 **거절 경로가 실제로 걸렸다**(시험 중 레벨업이 끼어들자 `timeScale 0` 에서 정확히 안 열렸다) · 격자 12개에서 6칸×2줄 안 넘침(최대 18) · 콘솔 0. 🟡 칩 정렬을 두 번 고쳤다 — `Icon` 은 **위쪽 피벗**, `Label` 은 **아래쪽 피벗**이라 내가 준 오프셋이 칩 밖으로 나갔다 |
| **D47** | 옵션에 **볼륨 슬라이더 2개뿐**이었고 크레딧 화면이 없었다(`ROADMAP` §3-3). 해상도·전체화면·그래픽 품질을 바꿀 방법이 아예 없었다 | ✅ 해결 (2026-09-04 70차 → 2-74) — `DisplaySettings` 신설 + `OptionPanel` 확장 + `CreditsPanel` 신설. 🔑 드롭다운 대신 **`< 값 >` 화살표** — `TMP_Dropdown` 은 부품이 여럿이라 코드로 짓기 나쁘고, 화살표는 기존 9-slice 버튼을 그대로 쓴다. 🔴 **값을 고르는 곳과 적용하는 곳을 갈랐다** — 옵션 패널은 **열려야** `Start` 가 도는데 저장된 설정은 게임이 켜지자마자 먹어야 한다 ⇒ 적용은 `GameManager.Awake` 가 부른다 | 판정 **5/5** — 품질 `5 → 0` 순환 + `QualitySettings` 실제 변경 + 저장 · 해상도 이동 후 `<` 로 복귀 · 전체화면 `Off → On` + 저장 · 크레딧 열기/닫기 · 콘솔 0. 🔴 **비침 결함을 잡았다** — `UI_Panel` 스프라이트의 **채움 알파가 0.92** 라 `Image.color` 알파를 1 로 올려도 뒤가 비친다. 가리는 일은 `DimBG` 몫이라 1.0 으로 올렸다. 🔴 곁일: 저장된 볼륨이 **BGM 0.059 · SFX 0.043** — 사실상 안 들린다(사용자 기기값이라 안 바꿈) |
| **D48** | 보스가 **잡몹 Ogre 와 같은 스프라이트**를 썼다. `Boss1.BossOverride` 가 잡몹과 **같은 `EnemyData`(Ogre)** 를 가리켜서 그림을 공유했고, 차별은 크기 2배 + 붉은 외곽선뿐이라 화면 한가운데 있는 게 **"조금 큰 오우거"** 였다. `D38` 에서 보류로 내려 뒀던 항목이다 | ✅ 해결 (2026-09-04 71차 → 2-75) — `Bonecaller.png` 생성 + `Enemies.csv` 에 보스 전용 행 + `BossOverride`/`EnemyId` 갈아 끼움. 🔑 **잡몹 Ogre 는 안 건드렸다** — 보스 전용 행을 새로 만들고 **수치는 Ogre 를 그대로 복사**해 밸런스 변화 0. 🔴 **컨셉을 지어내지 않고 보스가 하는 일에서 끌어냈다** — 못 쫓아오니(2.38 vs 3.5~4.6) 버티고 서서 내려찍는 넓적한 실루엣, 고블린을 소환하니 강령술사, 예고 원과 외곽선이 붉으니 붉은 대역 금지 | 판정 **7/7** — 알파 **0**·모서리 0 · 밝기 **0.371** · 붉은 치우침 **2** · 가로 **85.8 %** > 세로 78.9 % · 런타임 `DisplayName "Bonecaller"`·`BossBrain` 붙음·HP **1540**(전과 동일) · 콘솔 0. 🔴 **`maxTextureSize` 가 PPU 를 같이 낮춘다** — 512 로 줄였더니 PPU 도 512 가 되어 보스가 **2배가 될 뻔했다**(0.5 → 1.0 유닛). Ogre 와 대조해서 잡았다 |
| **D74** | 사용자 요구 `11` — 기본 체력 재생 + 재생 아이템. `D73` 이 보스 패턴을 둘 늘려 **압박만 커졌는데** 회복은 상점 휴식뿐이었다 | ✅ 해결 (2026-09-05 96차 → 2-100) — `StatBlock.HpRegen` 신설(기본 **0.4/s**) + 패시브 `Regeneration`(0.4~2.6/s). 🔴 죽은 뒤·만피에는 **아예 안 돈다** (`Heal` 은 `IsDead` 를 안 봐서 시체가 채워진다 · 만피에 매 프레임 부르면 회복 연출이 헛돈다) | 판정 **4/4** — 기본 재생 **오차 0.001** · 패시브 0.40→0.80→1.20 · 만피에서 정지 · Import 12/29/74. 🔴 **판정이 아니었으면 0 인 채로 넘어갔다** — `RecalculateStats` 가 `Final` 을 **필드별 나열**로 만들어서 새 필드가 통째로 빠졌다(첫 측정 `HpRegen=0.00`). 🔴 **그 자리에서 `Luck` 도 같은 이유로 빠진 것을 찾았다 → `B13` 등재**(아직 안 터진다 · 고치지 않음). 🟡 시험 오염 1건(적을 껐는데 웨이브가 계속 소환 → 무적으로 다시 쟀다). ⚠️ 아이콘을 `MaxHp.png` 로 임시 재사용 → 요청-51 |
| **D73** | 사용자 요구 `B-1` — 보스가 내려찍기·소환 둘뿐이라 *"패턴을 좀 더 추가해야 할 것 같다"* | ✅ 해결 (2026-09-05 95차 → 2-99) — **쫄 소환 + 돌진** · **예고 범위 일직선**. 🔑 **소환과 돌진을 하나로 묶은 게 설계의 전부다** — 따로 두면 각각 *"가끔 일어나는 일"* 인데, 묶으면 **쫄이 나타난 것 자체가 돌진의 예고**가 되고 쫄이 길을 막아 피할 자리를 좁힌다. 🔑 일직선은 **새 시스템이 아니다** — `BossSlam` 을 줄 세웠다(`D37` 지뢰밭과 같은 부품). 🔴 돌진 방향은 노려보기가 끝나는 순간 **한 번만** 정한다(매 프레임 겨누면 유도 미사일이다). `EnemyBase` 에 `AiSuspended`/`DriveVelocity` 신설 — 넉백과 같은 이유로 `return` 이 필요하고, 풀 재사용 때문에 `Initialize` 에서 반드시 끈다 | 판정 **7/7** — 일직선 **직선 이탈 0.000 · 간격 1.71 균일 · 양끝 10.29** · 노려보기/경직 `AiSuspended=True` 속도 0 · **돌진 6.16 유닛**(기대 6.05) · 복귀 `False` 속도 1.08. 🔴 **판정 중에 결함을 찾았고 그 결함으로 판정했다** — `EnterPhase` 가 두 쿨다운을 안 채워 **등장 첫 프레임에 둘 다 나갔다**. 덕분에 쿨다운 8~9초를 안 기다리고 쟀고, 재고 나서 고쳤다(고친 뒤 등장 직후 **예고 0 · AiSuspended=False**) |
| **D72** | 사용자가 스크린샷으로 3건을 짚었다 — 맵 배열이 이상하다 · 선이 난잡하다 · 휴식이 상시로 뜬다 | ✅ 해결 (2026-09-05 94차 → 2-98) — ① 세로 정렬을 `(i-(n-1)/2)` 로 **0 중심 대칭**(예전 식은 2칸/3칸 층이 반 칸씩 어긋났다) ② 연결을 **단조**로(자기 자리와 이웃에만) ③ 선을 **사각형 교점**까지만 긋는다(중심→중심이라 박스 밑을 지났다) ④ `D71` 의 휴식을 **살 게 없을 때만** — *"더이상 구매 못하는 상황에서"* 가 원래 의도였다 | 판정 **5/5** — 교차 판당 **1.0** · 먼 점프 **0** · 세로 `-150 0 150` / `-75 75` · 선 최소 길이 **90.0 = 200−110**(양쪽 테두리) · 대각 153.9 가 교점 계산과 일치 · 상점 `Item Item Item` → 꽉 차면 `Heal Exchange Exchange`. 🔴 **숫자가 통과한 것을 화면이 잡은 세 번째**(`D56`·`D67`). 앞의 둘은 내가 캡처해 잡았고 **이번엔 사용자가 잡았다** — `ScreenSpaceOverlay` 캔버스는 `SubmitRenderRequest` 로 못 찍어 **UI 를 내가 볼 수단이 없다** |
| **D71** | 사용자 요구 `6`(상점 휴식) + `B12`(칸이 꽉 차면 **골드만 사라진다**) — 사용자 답이 둘을 하나로 묶었다 | ✅ 해결 (2026-09-05 93차 → 2-97) · 🟢 **`B12` 닫힘** — `ShopSlot` 에 종류를 붙였다(`Item`/`Heal`/`Exchange`). 평상시 `Item·Item·Heal`, 꽉 차면 `Exchange·Exchange·Heal`. 🔑 앞서 적어 둔 A~C 는 전부 *"못 사게 막는다"* 라 **상점을 밟은 게 헛수고**가 되는데, 사용자 답은 **살 게 없으면 다른 걸 판다** 였다. 🔴 `CanAcquire` 만으로는 부족 — 이미 가진 것에 무조건 true 라 **만렙까지 통과**한다. 🔴 `Purchase` 의 순서를 뒤집었다(줄 수 있는지 **먼저** 묻는다) — 그게 `B12` 의 나머지 절반. 🔴 전환은 `GrantMetaGold` 를 안 쓴다(`GoldGain` 이 두 번 곱해진다) → `AddPendingMetaGold` 신설 | 판정 **6/6** — 평상시/꽉 참 진열 · 휴식 HP 54→106(+52) 골드 −60 · 전환 런 −100 → 메타 +40. 🔴 **대조군 둘** — 만렙 구매 시도에 **골드 2000→2000 그대로**(경고 로그·IsSold=False) · `GoldGain 2.00` 에서도 메타 **+40 그대로**. 🟡 시험 오염 1건(회복 재려다 플레이어를 죽여 정산이 돌았다 — 플레이 재시작 후 재측정). ⚠️ **아이템 칸이 3→2** 로 줄었다(휴석이 한 칸). `shopSlotCount` 4 로 올리려면 화면을 봐야 한다 |
| **D70** | 사용자 요구 `5` — *"shop 2연속으로 안되거나 타당성있게 노드들이 배치되게"* | ✅ 해결 (2026-09-05 92차 → 2-96) — 🔑 **굴림의 단위를 노드에서 층으로 올렸다.** *"이 층에 상점이 있나"* 는 층을 다 보고서야 답할 수 있다. 규칙 5개 — ①상점 연속 금지 ②자비(4층) ③엘리트는 2층부터 ④한 층이 전부 같은 특수 금지 ⑤보스 직전 선호. 막힌 종류는 가중치 0 으로 만들고 **남은 것끼리 다시 정규화**한다(안 하면 노말로 흘러 이벤트까지 줄어든다) | 판정 **400판** — 위반 4종 전부 **0** · 상점 판당 **2.65**(2~4) · **0회 판 0건**(`D61` 이 겪은 것) · 보스 직전 73 %. 🔴 **통계가 내 결함을 두 번 잡았다** — ① 1차에서 규칙 위반 0 인데 상점 평균 4.03 이 이상해 다시 재니 **0층이 300/300 판 상점**이었다(`RollStageType` 이 Normal 을 줘도 **그 뒤 강제 규칙이 갈아 끼운다**) ② 2차에서 **1층이 400/400** — 시작값을 `-shopPityLayers` 로 둬 1층이 바로 자비에 걸렸다. `_lastShopLayer = 0` 으로 바꿔 연속 금지가 1층을 막게 했다. **매판 같은 자리면 갈림길이 아니다** |
| **D69** | 사용자 요구 `2` — *"상점이 너무 비쌈 (아이템 레벨·스테이지 단계에 따라 비싸지게)"* | ✅ 해결 (2026-09-05 91차 → 2-95) — 🔑 **모순이 아니었다.** `D61` 의 두 판이 초반과 후반을 정반대로 가리킨다(3노드 사망 `M=89` = 못 산다 / 9노드 완주 `M=1168` = 남아돈다). 고정가 하나로는 둘 다 못 맞춘다 ⇒ **기준가를 내리고(×0.6) 층·레벨로 올린다.** `가격 = ShopPrice × 0.6 × (1+0.35×보유레벨) × (1+0.18×층)`. 환급도 **지금 가격**의 절반으로(고정가면 깊은 층에서 비싸게 산 걸 싸게 되판다) | 판정 **8/8** — Sword 70 → 0층 Lv0 **42** · 0층 Lv2 **71** · 8층 Lv4 **246** · 환급 **123** · 되돌려 42, 진열 3종이 기대와 일치. 🔴 **배선 순서를 읽다 결함을 찾았다** — `LayerScaling.Layer` 는 `WaveManager.StartWave` 에서만 갱신돼서 **상점 노드는 한 층 뒤처진다**(3층 상점이 2층 가격). `_currentNode.Layer` 를 직접 본다. 🔑 판정 대조군이 **`LayerScaling.Layer = 0` 인데 층 4 가 반영된 것**이다. 🟡 **`B12` 는 여전히 열려 있다** — 가격이 싸져 구매가 늘면 터질 확률도 는다 |
| **D68** | 사용자 요구 `12` — 진화가 **수치만 올린다.** 특전을 나눠 달라 (+ 이속 픽업 신설) | ✅ 해결 (2026-09-05 90차 → 2-94) — `EvolutionPerk` 신설. **Devastator=투사체 2배 · Windforce=필드 드랍 지속 2배 · Excalibur=골드 2배**(+@ 는 내가 골랐다 — 불만 `2` 와 맞물린다). `PickupKind.Swift` + `Pickup_Swift.prefab`(드랍 0.008) + `GrantSwift`, 오라에 **연두** 추가(우선순위 무적→공속→이속). 🔑 투사체 배율을 `WeaponBase.ScaledProjectileCount()` **한 곳**에 모았다 — 근접 '연타'와 원거리 '투사체'는 이름이 달라 각자 곱하면 한쪽만 고쳐진다. 🔴 enum 둘 다 **뒤에만** 늘렸다(정수 직렬화 · `D51`) | 판정 **6/6** — projMult 1→2 · buffMult 1→2 · goldGain 1→2 · **중복 대조군 2.00 그대로** · **활성 투사체 2**(활 Lv1 기본 1발 · 화면의 발사 수로 셌다) · 이속 3.70→5.70(겹쳐도 5.70) · 연두 오라. 🔴 **`D67` 정정** — 커밋에 *"굵기 9"* 라고 적었는데 **실제로는 14 가 돌고 있었다**. `D67` 커밋 시점에 이미 14 로 컴파일돼 **씬이 그 값을 직렬화**했고, 코드 기본값은 새 컴포넌트에만 먹는다. 씬 값을 9 로 바로잡았다. **`B11` 을 알고도 새 `[SerializeField]` 에서 같은 함정을 밟았다** |
| **D67** | 사용자 요구 `8` — 무적·공속을 먹어도 **겉모습이 안 바뀌어** 켜졌는지 모른다 | ✅ 해결 (2026-09-05 89차 → 2-93) — 🔴 **먼저 `PlayerStats` 의 무적 타이머를 갈랐다.** 피격 i-frame 과 픽업 무적이 `_invincibleTimer` 하나를 공유하고 있었는데, 피해 판정으로는 맞아도 **표시를 붙이는 순간 틀린 설계**가 된다 — 스칠 때마다 오라가 번쩍인다. 오라 자체는 **새 애셋 0 · 새 오브젝트 0** — `SpriteOutline` 셰이더의 외곽선을 빌렸다. 🔴 그 바람에 `_SpriteRect` 가 필수가 됐다(4×4 걷기 시트 · 없으면 옆 프레임 알파를 빤다 = `B1`) | 판정 **6/6** — 평상시 0 · `_SpriteRect (0,0.75,0.25,1.00)` texSize 1024 · 무적 하늘색 · 공속 주황 · 점등 알파 0.760(구간 0.36~1.00) · 🔴 **대조군: 피격 무적만 켜졌을 때 `IsInvincible=True` 인데 오라 0**. 🔴 **숫자는 통과했는데 그림이 틀렸다** — 밝은 픽셀 +71.7 %, 늘어난 픽셀이 전부 오라색(4876=4876)이었는데 **보니까 검이 주황으로 메워져 있었다**(칼날이 얇아 양쪽 테두리가 맞닿는다 · `B1` 계열). **실제 플레이 배율에서** 다시 골라 굵기 14 → **9**. `D56` 에 이어 두 번째로 그림이 숫자를 잡았다 |
| **D66** | 플레이테스트 **B 그룹** 착수 — `13`(보스 화면 밖 화살표) · `15`(클리어 화면이 단조롭다) | ✅ 해결 (2026-09-05 88차 → 2-92) — **둘 다 애셋 0개.** 화살표는 `Graphic.OnPopulateMesh` 로 삼각형을 직접 그렸다(폰트 `▲` 는 문자표에 없으면 빈칸이다 · `I-60`). 보스는 `OnBossSpawned` 구독 — `BossHealthBarUI` 와 **같은 이벤트라 어긋날 수 없다.** 화살표 오브젝트는 **런타임에 코드가 만든다**(씬 배선 0). 클리어 화면은 **새로 잰 값이 `BiggestHit` 하나**뿐이고 나머지(층·레벨·직업 사슬·아이템)는 있던 값이다 | 판정 **화살표 3/3** — 우상단 밖 `(791.80, 475.08) 30.96°`(위 테두리) · 좌하단 밖 `(-920.13, -207.03) 192.68°`(왼 테두리) · 화면 안 `active=False`. 🔑 **두 경우가 서로 다른 축에서 잘렸다** — 하나만 맞으면 `min(tx,ty)` 가 틀려도 통과한다. 🔴 **상자 넘침을 실제로 쟀다** — `600×180` 은 세 줄 요약용 크기다. 최악(391자·16종)에서 `680×230` 이면 **렌더높이 229.6 ≤ 230.0** 으로 안 잘린다. 🟡 `preferredHeight` 는 자동 크기가 켜지면 `fontSizeMax` 기준이라 **넘침 판정에 못 쓴다**. 🔴 피해는 `raw` 가 아니라 **방어를 뺀 값**을 보고한다(`777 → 771`) |
| **D65** | 사용자가 `PLAYTEST.md` 를 다 채웠다 — **24건.** 마감(9/7)까지 전부는 못 한다 | ✅ 1차 (2026-09-05 87차 → 2-91) — `C(조사 4건) → A(값이 크고 싼 8건)` 순서. 🔴 **조사 4건 중 3건은 고칠 게 없었다** — 폭파광은 T1·Bomb 한 자루(`weapons(1)= Bomb`), 승급 전용 직업은 `classes` 에 애초에 없다(`classesLen=6`), Ogre 는 `Elite2` 의 EliteOverride **한 군데뿐**이라 안 보이는 게 정상이다(🔴 `D51` 의 `Blocker` 를 하필 그 적에게 줬다). 넣은 것: 보스 처치=클리어 · 레벨업 `1/2/3` 키 · XP 곡선 · 상점 KILLS/TIME/LEVEL 제거 · 리롤 색 · 안내를 좌하단으로 | 🔴 **16번은 진짜 버그였다** — `PlayerVisual` 의 `* (flipX ? -1 : 1)` 보정이 틀렸고 **주석이 반대로 적혀 있었다**. 카메라를 플레이어 레이어만 남기고 찍어 실측: lean ±0.4 에서 기울기 성분 **+39.10 / +38.58 / −38.57 / −39.10** — **flipX 와 무관**하고 부호는 `_LeanAmt` 를 따른다(네 식이 서로 검산된다). 🔴 **10번의 진짜 원인은 표가 아니라 `Lv11 = 810` 이라는 벽**이었다 — 예산을 40 % 늘려도 예전 곡선은 Lv13 에서 안 움직인다(`D61` 의 9노드 완주 판이 정확히 13레벨). `xpTailStep` 신설 · 실측 `Lv11 = 89`. 🔴 **D-1 은 지시와 실제가 어긋나 안 건드렸다** — 승급 조건은 **이미** 무기 Lv5 + 건물 Lv3 다. 14번 판정: 보스를 죽이니 **남은 240s · 적 2 인데 Victory**. 🟡 캡처를 두 번 헛짚었다(씬 뷰 · 포커스 없어 프레임 정지) → `SubmitRenderRequest`. ⚠️ 프로브 완주로 들어간 메타 골드 **+148 을 되돌렸다** |
| **D64** | `D63` 이 *"`Clear Node` 가 보스를 건너뛴다"* 고 했는데 **사용자는 보스를 봤다** — 그 판에서 사용자가 `BossTime` 전에 버튼을 눌렀을 뿐이다 | ✅ 해결 (2026-09-05 86차 → 2-90) — **넘어가는 일과 보는 일을 갈랐다.** `DevSpawnBoss()` · `DevSetRemainingTime()` · `HasWaveTimer` 신설(전부 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`). 보스는 **정규 경로와 같은 세 줄**(`SpawnEnemy(isBoss:true)` → 등장음 → `AttachBossBrain`)을 쓰고, 보스 노드가 아니면 `bossWave` 것으로 대신한다. 🔴 `TimerRoutine` 의 **지역 변수를 필드로** 바꿔야 했다 — `WaveRemainingTime` 만 고치면 다음 프레임에 덮어써진다 | 판정 **5/5** — 타이머 **47.0 → 17.0** 조절 후 **17.0 → 1.5 로 계속 흐른다**(얼지 않는다) · 보스 `sprite=Bonecaller · scale 3.20 · isBoss=True · BossBrain=True` · 경고 0. 🔴 **로그로 의도를 추측한 게 세 번째다**(`D62` `D63` `D64`) — 공통 원인은 **사람이 무엇을 눌렀는지가 로그에 안 남는 것**. 🔴 킬 목표 웨이브는 **거절한다**(타이머가 없어 HUD·클리어 조건과 어긋난다) · 슬라이더는 **일부러 안 썼다**(드래그 중 시간이 멈춘다). 🟡 `D63` 과 같이 **렌더링은 미확인** |
| **D63** | 사용자가 9층까지 빨리 가려고 **상점·노드를 스킵**하고 있었다. 웨이브 타이머(60~100초)를 매번 기다려야 해서다 | ✅ 해결 (2026-09-05 85차 → 2-89) — Dev 패널에 **`Clear Node`** 버튼. `WaveManager.DevForceClearWave()` 를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에 두고 **`ClearWave()` 를 그대로 부른다** — 보상·이벤트 해제·맵 진행이 전부 그 안에 있어 흉내 내면 상태가 어긋난다. 웨이브일 때만 눌린다(상점·이벤트는 닫는 경로가 따로 있다) | 판정 — 누른 뒤 **진행중 False · 적 0 · 상태 StageMap · 클리어 보상 8** ✅ (*"클리어됐다"* 가 아니라 **"맵이 넘어갔나"** 까지 봤다). 🟡 백틱 주입으로 패널이 안 열려 **렌더링은 미확인** — 동작만 검증됐다. 🔴 **`D62` 진단을 정정한다** — 상점 *"구매 0"* 은 설계 문제가 아니라 **사용자의 스킵**이었다. 같은 데이터를 두 번 잘못 읽었다(판 하나로 · 의도를 모르고). **로그는 무엇을 했는지만 말한다.** 🟢 `B12` 는 코드로 확정한 것이라 그대로 유효하다 |
| **D62** | `D61` 이 판 2개로 `1-a` 를 닫았는데 **세 번째 판**(폭발광 · `M=465`)이 들어왔다 | ✅ 갱신 (2026-09-05 84차 → 2-88) — 진단이 *"상점을 못 만난다"* 에서 **"만나도 안 산다"** 로 옮겨졌다. 3판 동안 상점 **3회 열고 구매 0 · 리롤 0** | 🔴 **앞 보고를 두 군데 고쳤다** — ① 판 2 는 사망이 아니라 **완주(Victory)** 였다(정산 로그가 상태 전이보다 먼저 찍혀 슬라이스 첫 줄을 앞 판 종료로 읽었다) ② 원인 진단이 좁았다(노드 확률만으로는 안 풀린다). 🔴 **그 자리에서 `B12` 를 찾았다** — 상점 어디에도 `CanAcquire` 가 없어 칸이 꽉 차면 **골드만 사라지고 성공 로그가 찍힌다**. 레벨업은 카드 풀이 미리 걸러 안 걸린다. 이번 3판에서는 구매가 0건이라 **안 터졌다** · 고치지 않고 등재 |
| **D61** | `C40` 의 `1-a`(남은 런 골드 `M`)는 **사람이 한 판 해야만** 나오는 값이었다 (`D58` 이 자동화 불가 이유를 찾아 뒀다) | ✅ 닫혔다 (2026-09-05 83차 → 2-87) — `Editor.log` 를 백그라운드로 감시해 종료 정산을 그대로 잡았다. **MCP 없이도 된다** — Unity 가 `Debug.Log` 를 그 파일에 쓴다 | 🔴 **실패 — 통과선 300 의 3.9배.** 판1 `M=89`(3노드 사망 · 상점 1회 **구매 0**) · 판2 🔴 `M=1168`(**9노드 · 상점 0회**). 🔑 **원인이 로그에 그대로 있다** — 9노드를 도는 동안 상점이 한 번도 안 나왔다(`weightShop 0.15` 기대 1.35회). ⇒ `C40` 이 미리 적어 둔 `weightShop 0.28` · `shopSlotCount 4` 가 맞는 자리다. 🔴 **중간에 틀린 말을 했다** — 판1 하나로 *"수입이 적다"* 고 보고했는데, 판2 의 노드당 **130 G** 는 `D58` 의 CSV 계산 **134 G** 와 거의 같다. **산수는 맞았고 표본이 하나였다.** 🔴 도구 사고도 냈다 — `open('wb').write(t.encode())` 가 `SETUP_STATUS.md` **663KB 를 0바이트로** 날렸다(`git checkout` 복구 · 손실 0). `D54` 에 이어 두 번째다. 🟡 곁일: 승급 로그가 `Sentinel → Sentinel` 로 같은 말을 두 번 찍는다(`EvolutionManager.cs:327`) |
| **D59** | 사용자가 *"확인이 필요한 항목을 문서로 만들어 달라"* · *"완성률을 보여 달라"* | ✅ 해결 (2026-09-04 82차 → 2-86) — [`PLAYTEST.md`](PLAYTEST.md) 신설(사용자가 채우는 체크리스트) + 완성률을 **축을 갈라** 산출. 🔑 **못 만든 게 아니라 안 만져 본 것이 남았다** — 기능 95 % · 콘텐츠 70 % · **체감 검증 3 %** ⇒ 가중 합 **68 %**(포트폴리오 기준 85 %대) | 🔴 **곁일이 본일보다 컸다** — `ROADMAP` §0 현황표가 **2026-08-28 값 그대로** 방치돼 적 6→7 · 행동 3→6 · 무기 5→12 · 아이템 23→28 · 이벤트 5→8 로 **거의 모든 줄이 틀렸다**. 원인은 같은 사실이 §0 과 §7 **두 곳**에 적혀 있어 §7 만 갱신돼 온 것. §0 을 실측 표로 바꾸고 날짜를 박았다 |
| **D58** | `C40` 이 `TUNING.md` 미체크 111개 중 **2판으로 답이 나오는 17개**를 실플레이 검증으로 넘겼다(요청-32). 마감이 9/7 이다 | 🟡 절반 (2026-09-04 81차 → 2-85) — **코드·애셋 변경 0.** 숫자로 닫히는 것부터 닫았다 | ✅ **0-a** vSync 가 켜져 있었다(1 · 144Hz) · ✅ **2-a** 294마리 **111.3 fps** · ✅ **2-h** `BONECALLER`·HP 2926·BossBrain · ✅ **3-a** 색상각 **170도** + 밝기 차 0.383 · 2-c·2-g 는 `D53` 값 재사용. §0-b 는 `ProfilerDriver` 접근 불가라 **논리로 닫았다**(에디터 오버헤드 포함 111 fps ⇒ 빌드는 더 빠르다). 🔴 **§1 자동화가 안 되는 이유를 찾았다** — 런이 **레벨업 카드 선택에서 `timeScale 0`** 으로 멈춘다. 자동으로 고르면 `M` 이 아니라 아무 M 이 나온다. 🟡 대신 예측을 붙였다 — 이론 최대 **1757 G** vs 배수구 **241 G**, 실측 처치당 **1.65 G** ⇒ **M ≈ 600**(통과선 300). **사람이 해야 하는 것 9개**는 목록으로 넘겼다 |
| **D57** | 도감 하단의 `CLOSE` 버튼이 **세로 공간을 60px 먹고 있었다**. 사용자 요구: 우측 상단 X 나 ESC 로 닫고 그만큼 넓힐 것 | ✅ 해결 (2026-09-04 80차 → 2-84) — 하단 `CLOSE` 제거, 우측 상단 `X`(70px) + `ESC`. 격자·상세 바닥을 96 → 36 으로 내려 **+60px**. 🔵 `PauseMenuUI` 도 ESC 를 읽지만 `CanPause` 가 `Wave` 뿐이라 안 부딪힌다 — **그 전제를 주석에 못박았다**(도감을 전투 중에 열면 깨진다) | 판정 **4/4** — X ✅ · ESC ✅ · 대조군(ESC 로 일시정지 안 열림, 상태 MainMenu 유지) · 격자 664→724. 🔴 **처음엔 ESC 가 안 닫혔는데 코드가 아니라 시험이 문제였다** — `wasPressedThisFrame` 은 엣지인데 `InputSystem.Update()` 를 직접 불러 **엣지를 소모**했다. 큐에만 넣으니 닫혔다. `D51` 에서 같은 방식이 통한 건 그쪽이 `isPressed`(레벨)였기 때문이다 |
| **D56** | 사용자 스크린샷 — 도감이 **통째로 어두워** 실루엣이 안 보였다. 칸 배경 `0.151` 위에 검은 실루엣 `0.004` 로 둘 다 바닥에 붙어 있었고, `CanvasScaler` 가 1920 기준인데 Game 뷰가 856 이라 본문 25pt 가 **11.3px** 로 그려졌다 | ✅ 해결 (2026-09-04 79차 → 2-83) — **실루엣은 검정으로 두고 미발견 칸을 밝게 뒤집었다**. 칸 108→**132px** · 본문 25→**32pt**(자동 축소 20~32) · 카드 명도 0.13→0.20. 상세의 아이콘 판도 같은 규칙으로 뒤집었다(안 그러면 칸에서만 보이고 상세에서 사라진다) | 판정 **5/5** — 실루엣 대비 **0.1468 → 0.4533 (3.1배)**, 밝기 비 11.3→46.3배 · 본문 4종 전부 상자 안(직업이 20pt 로 가장 길다) · 콘솔 0. 🔴 **고치다가 정답을 새게 했다** — *"실루엣을 밝게"* 로 갔는데 `Image.color` 는 **곱셈**이라 실루엣이 되지 않고 **원색이 흐려질 뿐**이었다. 0/12 인데 무기 12종이 다 보였다. **숫자(대비)는 좋아졌으므로 캡처를 안 봤으면 개선이라고 보고했을 것이다** |
| **D55** | `D54` 의 도감이 **글자 목록**이었다. 사용자가 스케치로 *"각 아이콘의 실루엣을 누르는 방식"* 을 요구했다 — 아이콘이 40px 라 실루엣이 사실상 안 보였다 | ✅ 해결 (2026-09-04 78차 → 2-82) — 왼쪽을 **7열 아이콘 격자**(칸 108px)로 다시 짓고, 진화 조건을 아이콘 아래 **전용 줄**(금색 34pt)로 뺐다. 🔑 **가리는 방향이 두 개다** — 이름은 `???` 로 감추고 **윤곽은 준다**. 칸이 108px 라 실루엣이 본체가 됐고, 검과 활이 구분된다 | 판정 **6/6** — 5탭 칸 수 일치(12·16·7·10·7) · 실루엣/원색이 한 화면에 섞여 보임(캡처) · 선택 칸 금색 · 조건 줄 `Sword Lv.5 + ???` · 배선 **null 0/8** · 콘솔 0. 🔵 **52칸 전부 그림이 있다** — 그림 없는 칸용 예비 경로는 안 쓰였지만 남겼다(새 직업 그림을 아직 안 만든 동안 칸이 비어 보이는 게 더 나쁘다). 🟡 `--amend` 금지라 `D54` 를 고치지 않고 새 커밋으로 남겼다 |
| **D54** | `ROADMAP` §3-3 의 *"없는 화면"* 중 마지막 하나 — **도감이 없었다.** 아이템 28·적 7·직업 10 의 수치를 게임 안에서 볼 방법이 아예 없었고, 진화 조건은 더 그랬다 | ✅ 해결 (2026-09-04 77차 → 2-81) — `CodexPanel` 신설. 탭 5개 한 화면. 🔑 **도감이 목록을 따로 배선하지 않는다** — 이미 배선된 매니저에서 읽어 *"게임에 나오는 것 = 도감에 있는 것"* 이 구조적으로 보장된다. 🔑 **없던 것은 발견 기록 하나뿐이었다** — 나머지 데이터는 다 있었다. `SaveData` 에 목록 3개를 더했는데 `JsonUtility` 라 **옛 세이브가 안 깨진다** | 판정 **8/8** — 탭 5개 · `???` 마스킹 · **재료를 따로 가린다**(`Gun Lv.5 + ???` → 완전 공개) · 수치(적은 D51 행동 설명까지) · 옛 세이브 `Currency 1231` 보존 · 저장/복원 · **갈고리 3종이 빈 세이브에서 0→기록, 안 나온 적은 X 로 남는다**(대조군) · 콘솔 0. 🔴 **검증이 결함을 잡았다** — `ApplyClass` 갈고리가 빠져 **고른 직업이 영원히 `???`** 였다. 원인은 내 스크립트가 인코딩 에러로 죽으며 **`io.open('w')` 가 자기 자신을 0바이트로 날린 것**(이후 전부 encode-먼저 방식으로 바꿨다). 컴파일도 화면도 멀쩡해서 **런타임 판정에서만 드러났다** |
| **D53** | `D49` 가 층별 바닥 테마를 배선했지만 **화면은 1.4 % 밖에 안 달라졌다**(🟡 절반). `C37` 이 타일 `m_Color` 10줄을 고쳐 답을 냈고, 그게 실제로 통과선을 넘는지는 **DEV 가 캡처로 재야** 했다(요청-29) | ✅ 해결 (2026-09-04 76차 → 2-80) — **DEV 파일 변경 0.** 검증만 했다. 🔑 `D49` 의 캡처 두 장이 남아 있어 **같은 크롭·같은 공식**으로 전후를 쟀다 — *"예전엔 0.006"* 이 아니라 같은 자로 잰 비교다 | 판정 **5/5** — ① 층0 대조군 **0.1510 → 0.1530**(안 어두워졌다) · ② dL **0.0069 → 0.0727**(**10.5배**, 통과선 0.04) · **겹침 0 %**(얕은 층 블록 중 깊은 층 중앙값보다 어두운 것이 하나도 없다) · ③ 퀼트 **0.0134** vs CONTENT 예측 0.0138 (전혀 다른 방식인데 겹친다) · ④ 가독성 4/4 · ⑤ 콘솔 0. 🟡 CONTENT 예측 0.0443 vs 내 0.0727 은 **밝기 공식 차이**였다(단순 평균으로 재면 0.0406). 🔴 **적탄을 오진할 뻔했다** — 1.13배로 나와 반려 직전이었는데 크롭해 보니 **그 자리에 아무것도 없었다**(`Initialize` 를 안 불러 `_life=0`, 첫 `Update` 에 자멸). 제대로 살려 재니 **+0.2032 통과**. **숫자가 나빴을 때 그림을 안 봤으면 남의 작업을 잘못 반려했다** |
| **D52** | `C38` 이 `Economy.csv` 에서 `layerSpawnGrowth` 0.08→**0.14** · `maxAliveCeiling` 300→**320** 으로 올렸는데 **게임에는 안 들어가 있었다**(CSV 가 원본이므로 Import 를 해야 반영된다 · 요청-30) | ✅ 해결 (2026-09-04 75차 → 2-79) — Import 1회. **코드 0줄 · 씬 2줄.** 🔑 Import **전** 값을 먼저 읽어 대조군을 만들었다 — 안 그러면 *"원래 그랬는지 내가 바꿨는지"* 를 못 가린다 | 판정 **7/7** — 바뀐 둘 ✅ · **유지된 셋(HP·피해 성장률, `slowTimeScale`)이 안 움직였다** ✅ · 씬 diff 정확히 2줄 · 9층 Elite2 MaxAlive **294**(천장 320·위험선 400 아래) · 콘솔 0. 🔴 **곁일: `WaveManager.prefab` 이 필드보다 오래됐다** — 최초 커밋 이후 재저장이 없어 `D45`·`D50` 의 필드가 없고, 오버라이드가 없으면 값이 **C# 기본값으로 떨어진다**. ⇒ `Economy.csv` 값이 기본값과 같으면 씬에 흔적이 안 남아 **"Import 했는지"를 파일로는 판정할 수 없다**(런타임으로 읽어야 한다). 🟡 CONTENT 예측 293 vs 실제 **294** — `ScaleCount` 가 `RoundToInt` 다. 1 차이라 결론은 같다 |
| **D51** | 적 7종 중 **5종이 `Chaser` 하나**였다(Slime·Goblin·Zombie·Ogre·Bonecaller). 종류를 늘려도 화면에서는 **색만 다른 같은 적**이다. 특히 Ogre 는 이동 **1.2** 인데 플레이어는 3.5~4.6 이라 **영원히 못 따라잡는다** — 화면에 있어도 없는 것과 같았다 | ✅ 해결 (2026-09-04 74차 → 2-78) — `EnemyAI` 에 `Flanker`·`Swarmer`·`Blocker` 추가. 🔑 **컨셉을 각 적의 수치에서 끌어냈다** — 다수인 Slime 은 뭉치면 빨라지고(`Swarmer`), 최다인 Goblin 은 줄 서지 않게 옆으로 돌고(`Flanker`), 못 쫓아오는 Ogre 는 **가려는 곳을 막는다**(`Blocker`). Zombie 는 **대조군으로 `Chaser` 유지**. 🔑 **새 물리 질의 0** — 이웃 수는 무리 분리가 이미 세던 값을 썼다. 🔴 enum 은 `.asset` 에 정수로 직렬화되므로 **뒤에만 붙였다** | 판정 **8/8** — 정수값 보존(Chaser 0·Ranged 1·Charger 2) · Flanker 40.4도 vs 대조군 0.0도 · 이웃 0 개체 예측 오차 **0.65도** · Swarmer 8마리 오차 **0.0000**, 혼자 0.840 vs 무리 1.715(**2.04배**), 대조군 좀비는 이웃 2·3 에서 모두 1.600 · Blocker 예측 지점과 **0.12도**/플레이어와 23.7도, 대조군은 정확히 뒤집힘 · HoldRange 안에서 0.28도 · 콘솔 0. 🔴 **시험에서 세 번 헛짚었고 세 번 다 대조군이 잡았다** — 적이 이미 도착해 분리 조향을 쟀고, `timeScale 0.0002` 로는 `FixedUpdate` 가 안 돌았고(`D50` 에선 통했다 — 소환은 프레임 기준이라서), 두 무리를 같은 좌표에 겹쳐 놓아 난수 방향을 쟀다 |
| **D50** | 적이 나오는 방식이 **하나뿐**이었다 — 어떤 웨이브든 소환 반경 20 의 아무 데나 한 마리씩 흩뿌려져 **멀리서 걸어온다**. 사용자 요구: 땅굴 · 부대 · 큰 원으로 조이기 | ✅ 해결 (2026-09-04 73차 → 2-77) — `WaveData` 에 `SpawnPattern` 4종 + `WaveManager` 소환 루틴 분기. 🔑 **`Waves.csv` 에 열을 안 늘렸다** — 소환 항목 맨 뒤에 **`~패턴` 접미사**를 붙이고 `Scatter` 일 때는 안 쓴다 ⇒ **기존 행은 바이트 단위로 동일**. 🔵 예고가 배선 안 돼 있으면 땅굴 거리를 **소환 반경까지 밀어낸다** (예고 없는 발밑 소환 = 피할 수 없는 기습) | 판정 **7/7** — Ring 각도 간격 **45.0~45.0도**·반경 20.0 · Squad 각도 폭 **9.4도**·이웃 거리 1.10 · Burrow 예고 4/적 0(**순서**)·예고 거리 **3.43~5.55** · 예고한 자리에서 솟음(오차 1.43) · 배선 끊으면 거리 **20.0**(대조군) · 콘솔 0. 🔴 **기능 사이를 가로지르는 결함을 잡았다** — `PulseFx` 는 `D41` 에서 **unscaled** 가 됐는데 땅굴 대기(`WaitForSeconds`)는 **scaled** 다. `D46` 의 TAB 저배속(0.25)에서 **예고가 2.5초 먼저 사라지고 적이 나중에 솟는다** = 경고 없는 기습. `useUnscaledTime` 을 두어 시계를 고르게 했다(`Fx_Burrow` 만 `False`) |
| **D49** ✅→D53 | `ROADMAP` §7 — 스테이지가 **1씬뿐**이라 1층과 10층이 똑같이 생겼다. `D45` 로 난이도는 층마다 달라졌는데 **그게 눈에 안 보인다** | 🟡 절반 (2026-09-04 72차 → 2-76) — `GroundTiler` 가 층이 깊어질수록 타일 가중치를 **제 역순 쪽으로 보간**한다. 🔑 **새 데이터를 안 만들었다** — `tiles` 가 이미 얕은 것→깊은 것 순서라 그 순서를 뒤집는 것으로 충분했다. 계수는 `Economy.csv` 2줄 | 판정 — 🔴 **0층 대조군이 예전과 같다**(Grass 38.4 % ≈ 원래 가중치 40/100) · 9층은 `Flagstone 38.6 %` 로 **완전히 뒤집힌다** · 콘솔 0. 🔴 **그런데 화면은 1.4 % 밖에 안 달라진다** — 같은 자리 캡처 비교에서 평균 채널 차이 **3.67/255**, 밝기 **0.1510 → 0.1447**. **타일 10종이 서로 너무 비슷하다** ⇒ 그림 쪽 문제라 판정 기준(밝기 차 ≥ 0.04)과 함께 CONTENT 에 넘겼다(요청-37) |

### 해결 상세

**I-1 / I-2 / I-5 — Player**

| 항목 | 변경 |
|---|---|
| `tag` | `Untagged` → **`Player`** |
| `layer` | `Default(0)` → `Player(7)` |
| `Rigidbody2D.gravityScale` | `1` → `0` |
| `Rigidbody2D.freezeRotation` | → `true` (충돌 시 회전 방지) |
| `Rigidbody2D.collisionDetectionMode` | → `Continuous` |
| `Rigidbody2D.interpolation` | → `Interpolate` |
| `CircleCollider2D` | **신규 추가** — `radius 0.4`, `isTrigger = false` |

> `isTrigger = false`로 둔 이유: `Enemy_Goblin`의 `CapsuleCollider2D`가 이미
> `isTrigger = true`라 `EnemyBase.OnTriggerStay2D`는 정상 발동하고,
> 동시에 `Building_Turret`(non-trigger)과는 물리 충돌이 유지됨.

**I-6 / I-7 — 프리팹 물리**

| 프리팹 | 변경 |
|---|---|
| `Enemy_Goblin` | `gravityScale 1 → 0`, `freezeRotation` (이동은 `Rb.linearVelocity`라 Dynamic 유지) |
| `Proj_Bullet` | `gravityScale 1 → 0`, `bodyType Dynamic → Kinematic` |
| `Proj_Aoe(Boom)` / `ExpDrop_Small` / `Building_Turret` | Rigidbody2D 없음 — 변경 불필요 |

**I-3 — 한글 폰트 (Pretendard)**

사용자가 `Assets/Fonts/`에 Pretendard 배포판을 추가 → TMP Font Asset을 직접 생성.

| 항목 | 값 |
|---|---|
| 소스 | `Assets/Fonts/public/static/alternative/Pretendard-Regular.ttf` |
| 생성물 | **`Assets/Fonts/Pretendard SDF.asset`** (+ Atlas / Material 서브애셋) |
| 렌더 모드 | `SDFAA`, 샘플링 90pt, 패딩 9, 아틀라스 1024×1024 |
| **Atlas Population Mode** | **`Dynamic`** ← 한글은 완성형만 11,172자라 Static으로 구우면 아틀라스가 터짐. 런타임에 필요한 글리프만 추가됨 |
| Multi Atlas | 활성 |

적용 범위:
- `TMP Settings.defaultFontAsset` → Pretendard SDF (앞으로 만드는 텍스트의 기본값)
- `TMP Settings.fallbackFontAssets` → Pretendard SDF 등록 (**전역 안전망** — 어떤 텍스트가 다른 폰트를 쓰더라도 한글이 깨지지 않음)
- 씬 TMP 텍스트 **40개** 전부 교체
- 프리팹 TMP 텍스트 **18개** 교체 — `DamagePopup`, `Prefab_ItemCard`, `Prefab_ShopCard`, `Prefab_ShopRemoveRow`

> 글리프 커버리지 검증: `TryAddCharacters("가나다라마바사한글안녕게임상점레벨업▲▼")` → **missing 0**.
> `StageClearUI`가 쓰는 `▲` `▼`까지 포함됨.

**I-4 — CanvasScaler**

| 항목 | 값 |
|---|---|
| UI Scale Mode | `Constant Pixel Size` → **`Scale With Screen Size`** |
| Reference Resolution | **1920 × 1080** |
| Screen Match Mode | `Match Width Or Height`, **Match = 0.5** |

> Match 0.5(가로/세로 균등)로 둔 이유 — **옵션 메뉴에서 해상도를 바꿀 예정**이라서.
> 이 설정이면 `Screen.SetResolution()`만 호출해도 UI가 자동으로 비율에 맞춰 스케일되므로
> 해상도별 레이아웃을 따로 만들 필요가 없음. 울트라와이드/4:3에서도 잘림 없이 대응됨.

**I-12 — 빈 `WaveData.Spawns`**

Play 스모크 테스트에서 `3 enemies spawned (0 alive)` FAIL로 발견.
세 애셋 모두 `Spawns: []` 였음. 현재 존재하는 유일한 `EnemyData`인
`Assets/Game/EnemyData/Goblin.asset`(프리팹 `Enemy_Goblin`)로 채움.

| 애셋 | 클리어 조건 | Spawns | 오버라이드 |
|---|---|---|---|
| `Normal1` | Timer 60초 | Goblin ×18 @0.9s, Goblin ×22 @0.6s | — |
| `Elite1` | Timer 75초 | Goblin ×24 @0.7s | `EliteOverride`=Goblin, `EliteCount`=2 |
| `Boss1` | Kill 13 | Goblin ×12 @0.8s | `BossOverride`=Goblin |

`SpawnRadius`는 셋 다 `12`. 적 종류가 Goblin 하나뿐이라 **임시 밸런스**였음.

> ⚠️ **위 표는 2차(2026-08-26) 작업에서 대체됨.** 지금 Spawns 는 `Assets/Game/Balance/Waves.csv`
> 가 원본이고, 적 6종·웨이브 6종으로 다시 짜였다. 현재 수치는 [`BALANCE.md`](BALANCE.md) §2-6 참조.

**I-13 — CameraController NaN**

`LateUpdate` 진입부에 조기 반환을 추가. NaN은 한 번 섞이면 계속 전파되므로 아예 갱신을 건너뛴다.

```csharp
private void LateUpdate()
{
    if (target == null) return;
    if (Time.deltaTime <= 0f) return;   // timeScale = 0 → 0/0 = NaN 방지
    ...
}
```

---

## 3. 결정 사항

| # | 항목 | 결정 |
|---|---|---|
| A | MCP 연결 방식 | Unity 에디터를 열어 공식 릴레이로 연결 (YAML 직접 편집 안 함) |
| B | 씬 인스턴스 참조 | 프리팹 애셋 참조로 수정 |
| C | UI 범위 | HUD·PauseMenu·Option·Shop **전부** 구축 |
| D | `Normal1 1.asset` | `Normal1.asset`으로 이름 정리 |
| E | Input 처리 | (나) 스크립트를 Input System으로 이관 **＋** (가) Active Input Handling = `Both` 둘 다 적용 |

---

## 4. 다음 할 일

1. ✅ Unity 에디터 실행 / MCP 연결
2. ✅ `Normal1 1.asset` → `Normal1.asset` 리네임 (D)
3. ✅ Input System 이관 (E-나) — 컴파일 클린
4. ✅ 씬 인스턴스 참조 → 프리팹 애셋 참조 교체 + 방치 인스턴스 정리 (B)
5. ✅ `Weapon_Sword.prefab` 생성 및 WeaponData/EnemyData/BuildingData 프리팹 연결 (4단계)
6. ✅ HUD / PausePanel / OptionSubPanel 구축 + 필드 연결 (C)
7. ✅ ShopRoot 구축 + ShopUI 16개 필드 연결 (C)
8. ✅ Pretendard TMP 폰트 애셋 생성 + 전역 적용 (I-3)
9. ✅ CanvasScaler → Scale With Screen Size 1920×1080 (I-4)
10. ✅ Player 물리/태그/레이어 + 프리팹 중력 정리 (I-1, I-2, I-5, I-6, I-7)
11. ❌ Active Input Handling = `Both` (E-가) — **불필요로 판정, 미적용**
12. ✅ Play 모드 콘솔 검증 — 에러 0 / 경고 0 (5단계)
13. ✅ **MainMenuUI / StageMapUI / RunEndUI / GameStatePanel / StateVisibilityBinder 작성 (I-10)**
14. ✅ `AudioManager` / `StageClearUI` 씬 배치 + 호출부 연결 (I-11)
15. ✅ `WaveData` 3종 Spawns 채우기 (I-12)
16. ✅ `CameraController` NaN 가드 (I-13)
17. ✅ **Play 모드 전체 루프 검증 — 18 / 18 PASS** (6단계)

**2차 (2026-08-26)**

18. ✅ 진행 차단 버그 5건 수정 (I-14~I-18) — ⚠️ **런타임 재검증 미완** → [`TODO.md`](TODO.md) §1
19. ✅ `WaveData` 를 `WaveManager.cs` 에서 분리 + 깨진 애셋 6개 재생성 (I-19)
20. ✅ **CSV ↔ ScriptableObject 파이프라인 신설** — `CsvTable.cs` / `BalanceImporter.cs` / CSV 9장
21. ✅ 콘텐츠 투입 — 적 6 · 무기 5 · 건물 3 · 패시브 9 · 아이템 17 · 웨이브 6 · 이벤트 5
22. ✅ `MoveSpeed` 패시브 수치 정상화 (CSV 임포트로 해결)
23. ✅ 경제/경험치 임시 수치 확정 + [`BALANCE.md`](BALANCE.md) 작성
**3차 (2026-08-26)**

24. ✅ 접촉 피해를 지속 → 한 방 + 무적 0.6초로 교체 (I-20)
25. ✅ 피격 넉백 + 붉은 플래시 + 깜빡임 + 카메라 흔들기 (→ 2-4)
26. ✅ `StatBlock` 기본값이 보너스로 두 번 더해지던 문제 (I-21 → 2-5)

**4차 (2026-08-26)**

27. ✅ **직업 시스템 신설 — 런 시작 시 무기 지급** (I-22 → 2-6). 직업 3종 + `Classes.csv`

**5차 (2026-08-26)**

28. ✅ **직업 선택 UI** (I-23 → 2-7). 카드 그리드 + 일러스트 패널 + 설명 바 + `Prefab_ClassCard`
29. ✅ `CLAUDE.md` 신설 — 문서 3종을 매 작업마다 자동 갱신하도록 규칙화

**6차 (2026-08-26)**

30. ✅ **레벨업 카드가 안 뜨던 문제** (I-24 → 2-8). `?.` 가 Unity 가짜 null 을 못 막아 예외가 흐름을 끊고 있었다
31. ✅ **레벨업 리롤을 레벨업당 1회로 제한** (→ 2-8). `rerollCostIncrease` 는 무의미해져 제거
32. ✅ **레벨업 패널 확대** (→ 2-8 끝). 560×320 → 1560×900, 카드 150×200 → 440×560
33. ✅ **스프라이트 26종 생성 + 전면 배선** (I-25 → 2-9). Unity AI(`gpt-image-1-5`)로 픽셀아트를 뽑아
    적 6 · 무기 5 · 패시브 9 · 건물 3 · 직업 3 을 채우고 CSV 5종의 경로 열을 갈아끼웠다.
    스프라이트 필드 **37/37 연결, null 0**. `Tint` 는 흰색으로 되돌림(연출 전용으로 용도 변경)
34. ✅ **적 연출 — 등급 외곽선 + 걷기 바운스 + 피격 플래시** (I-26 → 2-10). `sr.color` 곱셈을
    **알파 팽창 외곽선**(URP Unlit 커스텀 셰이더)으로 대체해 원화 색을 되살리고,
    정지 이미지를 **버텍스 스쿼시/바운스**로 흔들었다. 스케일이 아니라 정점을 건드리므로
    `CapsuleCollider2D` 판정은 그대로다. 포인트 소모 **0**

**9차 (2026-08-27)**

35. ✅ **플레이어 크기 정상화** (I-27 → 2-11). PPU 함정 — `maxTextureSize` 축소가 `ppu` 는
    안 건드려 직업 그림이 적과 같은 0.5 유닛이었다. ppu 1024 → 512 로 **1.0 유닛**
36. ✅ **바닥 타일맵 신설** (I-28 → 2-11). 이음매 없는 타일 **10종** 생성 + `Tile.color` 로
    평균 휘도를 평준화(퀼트 제거) + 카메라 추종 무한 타일러 `GroundTiler`.
    배경 #314D79 → **#161815** (사용자 요청 "눈 안 아프게")
37. ✅ **플레이어 걷기 애니메이션 + 몸 연출** (I-29 → 2-11). 직업 3종 **16프레임** 걷기 시트
    생성(기존 원화를 레퍼런스로) + `PlayerVisual` + 셰이더 `_LeanAmt` 전단.
    `Classes.csv` 에 `WalkSheet` 열을 신설해 **CSV 파이프라인 안**에서 관리한다
38. ✅ **씬 `Camera` 태그 `Untagged` → `MainCamera`** (I-30). 카메라 흔들기가 죽어 있었다
39. ✅ **적 캡슐 콜라이더 0.5×1.0 → 0.34×0.36** (I-31). 그림보다 2배 길어 헛맞았다

**10차 (2026-08-27)** — 상세는 2-12

40. ✅ **건물 5종 + `Z` 즉시 설치 FIFO** (I-36·I-37). 전투 2 + 경험치/골드/회복 3
41. ✅ **패시브 레벨 누적 버그** (I-32). `BuildingCooldown` 이 음수 반전해 건물이 10배 빨랐다
42. ✅ **리롤 고정 버그**(I-34) · **경험치 오브 크기**(I-33) · **클리어 화면 성장 표시**(I-35)
43. ✅ **`Z` 가 안 먹던 원인** (I-38). `Awake` 에서 `GameManager.Instance.BuildingMgr` 캐시

**11차 (2026-08-27)** — 상세는 2-13 ~ 2-15

44. ✅ **곡사포 폭탄 투사체 + 16프레임 폭발** (I-39~I-41). 잔상 0 확인
45. ✅ **건물 크기 1.30 유닛** · **폭발 반경 3 → 2** (I-42)
46. ✅ **미사용 애셋 정리** — 스프라이트 5 · 스텁 스크립트 3 · 폰트 `web/` 24MB
47. ✅ **GitHub 연동** — `popzap/VS_LIKE` (Private) · 3.1G → **677파일 / 83MB**

**12차 (2026-08-28)** — 상세는 2-16

48. ✅ **장르 완성도 갭 분석** — [`ROADMAP.md`](ROADMAP.md) 신설.
    오디오 **0개** · 적 행동 **1종** · 무기 진화 **없음** · HUD 에 타이머/킬/골드 **없음** ·
    `MetaScreen` **화면 없음** 을 확인하고 5단계 진행 순서를 잡았다.
    회귀 의심 3건(CanvasScaler · HUD 배선 · 무기 경로)은 **조사해서 기각**
**13차 (2026-08-28)** — 상세는 2-17

49. ✅ **HUD 정보 3종** (I-43). 생존 타이머(`0:00`, 10초 이하 빨강) · `N Kills` · `N G`.
    `WaveManager` 에 `WaveRemainingTime`/`IsWaveActive` 노출. **구독이 아니라 폴링** — I-8·I-38 회피.
    덤으로 **죽은 UI 였던 `CurrencyText`** 를 살렸다
50. ✅ **타격 반응 4종** (I-44). 적 넉백(등급별 저항) · 사망 스케일 팝 · 히트스톱 · 처치 흔들기.
    히트스톱은 `Time.timeScale` 공유 사고를 막으려 **3중 가드**를 걸었다
51. ✅ **오디오 배관** (I-45). 볼륨 계통 분리 · `PlaySfx`/`PlayBgm`/`StopBgm` · 보이스 풀 16 ·
    같은 클립 0.04초 중복 컷. **AudioMixer 는 만들지 않았다** (이유는 2-17)

**14차 (2026-08-28)** — 상세는 2-18

52. ✅ **웨이브 병렬 소환 + 적 수 상한 + 화면 밖 적 재배치** (I-46). 소환 항목마다 코루틴을
    따로 띄우고 `Spawns` 에 **시간창**(`:시작-종료`)을 넣어 여러 종이 섞여 나오게 했다.
    `MaxAlive` 상한은 **취소가 아니라 대기**이고, 소환 반경 1.9배 밖으로 나간 적은
    **지우지 않고 옮긴다**(경험치 증발 방지). 살아 있는 수는 `int` 가 아니라 **목록 + 실물 확인**
53. ✅ **적 행동 분화 3종** (I-47). Demon **원거리**(거리 유지 + 옆걸음 + 적탄) ·
    Wolf **돌진**(예고→대시→경직) · **전 종류 무리 분리**(조향, 4스텝마다).
    ROADMAP 의 상속 대신 **`EnemyAI` enum(데이터 주도)** — 이유는 2-18
54. ✅ **보물상자 + 자석 픽업** (I-48). 엘리트/보스가 상자를 **확정** 드랍(레벨업 패널 재사용,
    레벨은 안 오름), 잡몹이 `0.6%` 로 자석을 떨군다(웨이브당 1~2개). 자석은 **static 상태 없이**
    그 순간 살아 있는 구슬에만 표시를 남긴다

**15차 (2026-08-28)** — 상세는 2-19

55. ✅ **게임에 소리가 붙었다** (I-49). BGM **4곡** + SFX **14종** 생성 · 호출부 **18곳** 배선.
    미결정이던 "클립을 어디에 둘 것인가"는 **(B) `AudioLibrary` SO 한 장**으로 확정(사용자 결정).
    (B) 의 약점이던 이름 오타는 키를 `string` 이 아니라 **`SfxId`/`BgmId` enum** 으로 만들어 막았다
56. ✅ **BGM 전환을 `GameManager.ChangeState` 한 곳에 몰았다.** 모든 화면 전환이 지나는 길목이라
    화면마다 흩어 놓으면 반드시 빠뜨린다. `LevelUp`·`Paused` 는 **곡을 안 바꾼다** — 웨이브 위에
    끼어드는 상태라, 레벨업마다 음악이 끊기면 전투의 흐름이 매번 잘린다
    (⚠️ `Paused` 는 **16차 I-51 에서 뒤집혔다** — 아래 60번)
57. ✅ **"소리 뭉개짐"을 코드 위치로 막았다** — 죽는 타격은 피격음 생략(사망음과 겹침) ·
    다발 발사는 볼리당 1회 · 레벨업 팡파레는 `while` 안(보물상자와 구분) · 건물 설치음은 성공 경로만.
    **중복 컷이 실제로 도는 것도 확인**(20회 연타 → 보이스 1개)

**16차 (2026-08-29)** — 상세는 2-20

58. ✅ **전투 시작하자마자 옵션창이 튀어나오던 버그** (I-50). 원인이 둘 겹쳤다 —
    `UI Canvas` 자식 10개가 전부 전체화면이라 **형제 순서 = 그리는 순서**인데 `OptionSubPanel` 이
    메뉴 패널들보다 **뒤**에 있었고(안 보임), 상태가 바뀔 때 **아무도 안 꺼줬다**.
    메뉴에서 옵션을 누르면 보이지도 않은 채 켜져 있다가, 전투 진입에서 앞 패널이 전부 꺼지는
    순간 화면에 남았다. **사용자가 직접 플레이해서 잡아낸 첫 버그**
59. ✅ **오버레이 2종을 캔버스 맨 뒤(= 맨 위)로 옮겼다** — `PausePanel` 8, `OptionSubPanel` 9.
    코드로만 막으면 새 패널을 추가할 때마다 같은 함정을 다시 밟는다
60. ✅ **ESC 일시정지가 BGM 을 멈춘다** (I-51). `StopBgm` 이 아니라 **`PauseBgm`** 이다 —
    `StopBgm` 은 곡을 버려서 재개할 때 도입부만 반복해 듣게 된다. `AudioSource` 에는
    "일시정지 중인가"를 묻는 프로퍼티가 없어 `_bgmPaused` 를 직접 들고 있고,
    같은 곡을 다시 요청받으면 `PlayBgm` 이 처음부터 틀지 않고 **이어서 재생**한다
61. ✅ **파이어볼이 날아간다** (I-52). Fireball 과 Bomb 이 **같은 `Weapon_Aoe.prefab`** 을 쓰므로
    프리팹이 아니라 **데이터로 갈랐다** — `WeaponData.TravelPrefab` 신설(CSV 열 추가).
    Fireball 은 `Proj_Fireball`(직선·무회전) 을 속도 11 로 쏘고, Bomb 은 비워 둬 **즉발 유지**
62. ⏳ **다음**: [`ROADMAP.md`](ROADMAP.md) §8 — 남은 가장 큰 구멍은 **무기 진화 0**
    (→ **17차에서 해결**, 아래 63~66번)

**17차 (2026-08-29)** — 상세는 2-21

63. ✅ **상점 UI 를 읽을 수 있게 키웠다** (I-53). 카드 `160×220` → **`250×440`**,
    설명 글자 `10pt` → `18pt`, 이름 `20` → `38`. 레벨업 카드는 11차에 이미 키웠는데
    **상점만 옛 치수로 남아 있던 것**이다. 라벨 영문화 + 리롤 버튼의 `🔀` 제거
    (U+1F500 이 Pretendard SDF 에 없어 `␡` 로 그려졌다 — 16차 💰와 같은 함정)
64. ✅ **무기 진화가 붙었다** (I-54). ROADMAP 의 원래 안(`ItemData.EvolvesInto` +
    `RequiredPassive`)은 **"무기+패시브" 하나밖에 표현 못 한다** — 사용자가 요구한
    `패시브·무기·건물` 3조합을 담으려면 아이템에 필드를 붙이는 방식 자체를 버려야 했다.
    레시피를 **별도 `EvolutionData` SO** 로 빼고 재료를 `ItemData[]` 로 두자
    카테고리가 무의미해져 3조합이 특별 취급 없이 표현된다. 재료 개수도 가변
65. ✅ **전달 경로를 플래그가 아니라 재료에서 파생시켰다.** 재료에 건물이 있으면
    **그 건물이 제단**(앞에서 `E`), 없으면 **보물상자**. `DeliveryType` 열을 뒀다면
    "건물이 재료인데 상자에서 나온다" 같은 **데이터만으로 깨진 상태**를 CSV 가 허용한다.
    건물은 이미 맵에 실재하므로 제단 프리팹도 필요 없었다.
    소모는 **무기 재료만** — 제단이 증발하면 납득이 안 되고, 무기 슬롯 6칸 상한 때문에
    재료 무기를 먼저 비우지 않으면 결과가 **조용히 거절**된다
66. ✅ **`EvolutionPromptUI` 3단계 안내** — ① 지금 `E` ② 상자에서 나온다 ③ 제단으로 가라.
    진화는 **조건이 조용히 충족돼서** 표시가 없으면 플레이어가 영영 모른다.
    검증 중 `HUD` 가 **`StateVisibilityBinder` 로 Wave/LevelUp/Paused 에서만 켜지는** 걸
    발견해 `StageMap` 분기(도달 불가)를 지우고 **`Wave` 하나만** 남겼다
67. ✅ **17차 마무리** — `109280f` 로 커밋·푸시 완료

**18차 (2026-08-29)** — 상세는 2-22

68. ✅ **직업마다 소지 상한이 다르다** (I-55). 3직업을 가르는 게 `Bonus*` 스탯뿐이었는데
    차이가 몇 퍼센트라 **플레이 중에 체감되지 않았다.** 상한은 성격이 다르다 —
    무기 3종과 5종은 **런 전체의 모양**이 달라진다. 스탯이 "얼마나 센가"라면 상한은
    "무엇을 할 수 있는가"다. Warrior 3/5/5(진지형) · Ranger 5/4/2(화력형) · Mage 3/8/3(패시브형)
69. ✅ **상한 원본을 `CharacterClassData` 로 옮겼다.** `WeaponManager.maxWeaponSlots`(전역 상수 6)는
    **삭제** — `Economy.csv` 에 그 행이 없음을 먼저 확인했다(§3 규칙).
    조회는 `PlayerStats.SlotLimit`, 판정은 `LevelUpManager.CanAcquire` **한 곳씩**이다.
    `BuildingManager` 에는 상한을 따로 안 넣었다 — `UnlockBuilding` 의 유일한 호출자가
    `ApplyItem` 이라 그 앞 가드가 이미 덮는다. 두 곳에 두면 서로 어긋날 때를 걱정해야 한다
70. ✅ **레벨업 카드와 상점이 한 번에 잡혔다** — 둘 다 `PickCandidates` 를 지난다
    (`GetShopCandidates => PickCandidates`). 필터 한 줄이 두 화면을 먹인다
71. ⚠️ **부수 발견: "보유 중인데 무기는 없는" 유령 아이템.** `PickCandidates` 에 상한 검사가
    없어서, 슬롯이 찬 채로 새 무기를 고르면 `_inventory` 에는 기록되고 `WeaponManager` 는
    **경고만 남긴 채 조용히 거절**했다. 손해가 무기 하나로 끝나지 않는다 — 그 아이템이
    이후 **보유로 취급**되어 카드 가중치가 2배가 되고(없는 무기의 레벨업 카드가 계속 뜬다)
    **진화 재료 판정까지 통과**한다. 상한 6 이라 잠복했을 뿐 **상한 3 을 넣는 순간 터진다.**
    `ApplyItem` 맨 앞 가드로 막고, `WeaponManager` 의 검사는 **지우지 않고 경고 로그로 남겼다**
    — 조용히 return 하던 그 자리가 정확히 사고의 원인이었으므로 재발 시 드러나야 한다
72. ✅ **꽉 찬 상태에서도 진화가 성사된다** — `Evolve()` 가 재료를 **먼저** 소모하기 때문이다.
    17차에 그 순서로 짜 둔 것이 여기서 값을 했다. 순서가 반대였다면 상한 3 에서 조용히 실패했을 것
73. ✅ **다음 작업으로 직업 진화를 예고** → 19차에서 처리됨 (I-56)

**19차 (2026-08-29)** — 상세는 2-23

74. ✅ **무기 + 건물 → 직업 승급으로 분리** (I-56). 17차에 3조합을 열어 뒀지만 **결과가 전부 무기**라
    건물이 재료인 이유가 "제단이 필요해서"뿐이었다. 이제 조합마다 결과가 다르다 —
    무기+무기·무기+패시브는 **보물상자에서 무기**, 무기+건물은 **그 건물 앞 `E` 로 직업**.
    `Sentinel`·`Doomsday` 두 레시피가 정확히 그 조합이었으므로 **무기에서 직업으로 옮겼다**
75. ✅ **직업을 하나가 아니라 사슬로 들게 했다** (`PlayerStats._classChain`). 교체 방식이면
    T2 로 올라가는 순간 T1 의 보너스와 칸이 사라져 **승급이 손해가 된다** —
    Warrior(건물 5) → Sentinel(건물 1) 로 줄어드는 식. 스탯·소지 상한 모두 **사슬 전체를 합산**한다
76. ✅ **`Max*Slots` → `Bonus*Slots` 개명.** 합산 구조에서 "총량"으로 두면 승급 행에 "총 4"를
    적었을 때 `3 + 4 = 7` 이 되는 함정이 생긴다. **데이터만 봐서는 눈치챌 수 없는 종류**라
    이름부터 `Bonus*` 스탯과 맞췄다
77. ✅ **승급은 재료를 소모하지 않는다.** 무기 진화는 무기를 먹고 더 센 무기를 돌려주니 교환이지만,
    승급은 돌려주는 게 직업이라 재료까지 가져가면 **순수한 손해**다.
    체력도 **가득 채우지 않는다** — 완전 회복이 붙으면 "위험할 때 승급을 아껴 두는" 운용이 생긴다.
    대신 늘어난 최대치만큼은 얹는다(130 → 150). 안 그러면 승급이 눈에 안 띈다
78. ✅ **`FromClass` 는 "현재 직업"이 아니라 "거쳐 왔는가"를 본다.** 끝만 보면 같은 T2 에서
    T3 두 갈래가 갈릴 때 한쪽을 타는 순간 다른 쪽이 영영 막힌다
79. ✅ **프롬프트 판정 순서를 실행 순서와 일치**시켰다. `TryEvolveAtAltar` 가 승급을 먼저 보므로
    `EvolutionPromptUI` 도 승급을 먼저 본다 — 안 그러면 `[E] EVOLVE` 라 써 놓고 승급이 일어난다.
    실행 순서가 승급 우선인 이유는, 무기 진화가 재료를 먹어 치우면
    **그 무기를 재료로 쓰던 승급이 조용히 불가능해지기 때문**이다
80. ✅ ~~승급 직업 4종의 캐릭터 그림이 없다~~ → **20차에서 해결** (I-57, 아래 81).
    남은 것: ① **건물 앞 `E` 실조작 미검증** (로직은 검증된 무기 제단과 동일하나 실제로 눌러 본 적 없음) ·
    ② 승급 수치는 전부 **자리표시값** (→ [`TODO.md`](TODO.md) §1·§3)

**20차 (I-57) — 애셋 17장**

81. ✅ **승급해도 겉모습이 안 바뀌던 문제를 그림으로 메꿨다** (I-57). `Classes.csv` 의
    `Portrait`/`BodySprite`/`WalkSheet` 가 승급 4행에서 비어 있으면 `ApplyClassVisual` 이
    **조기 반환**한다 — 승급이라는 가장 큰 성취가 **로그로만 존재**했다
82. ✅ **`BodySprite` 만 채우면 소용없다.** `PlayerVisual` 이 `sr.sprite` 를 걷기 프레임 0 으로
    덮어쓰므로 **`WalkSheet` 이 있어야** 겉모습이 바뀐다. T1 과 똑같이 두 열 모두 시트 경로를 적었다
83. 🔴 **I-41(가짜 투명 배경)이 17장 전부에서 재현됐고, 이번에 해법을 찾았다** —
    `GenerateAsset(command: "RemoveImageBackground", targetAssetPath: <png>)`.
    `savePath` 를 무시하고 **원본을 제자리에서** 고쳐서 **GUID 와 4×4 슬라이싱이 보존**된다.
    `referenceImageInstanceId` 로는 안 되고 `targetAssetPath` 여야 한다
84. ⚠️ **생성물의 임포터 설정은 규약을 안 따른다.** PPU 가 텍스처 크기와 같게(1024) 박히고
    요청 해상도(`width: 512`)도 무시된다. 클래스 걷기 시트 **256** / 아이콘 **512** 로 다시 임포트했다.
    🔴 **적 시트를 1024 로 둔 것은 오판이었다** — 21차(I-58)에 **512** 로 고쳤다 (아래 88)
85. ✅ **Aegis 만 T1 이 아니라 Sentinel 결과물을 참조**로 썼다. `Aegis.FromClass = Sentinel` 이라는
    데이터상의 사슬을 그림에서도 이어, Sentinel 의 실루엣을 유지한 채 두꺼워지게 했다
86. ✅ ~~적 걷기 시트 6장이 아직 안 붙어 있다~~ → **21차에서 해결** (I-58, 아래 87).

**21차 (I-58) — 적 걷기 애니메이션**

87. ✅ **`EnemyVisual` 은 이미 있었다.** I-47 때 바운스·좌우 반전·피격 플래시용으로 만들어 둔
    컴포넌트라, `Setup` 에 프레임 배열 인자 하나를 늘리는 것으로 배선이 끝났다.
    20차에 "`EnemyVisual` 컴포넌트를 새로 만들어야 한다"고 적었던 건 **읽어 보지 않고 쓴 추정**이었다
88. 🔴 **내 20차 커밋의 PPU 가 틀렸다.** 적 정지 그림은 0.5유닛인데 걷기 시트를 PPU 1024 로
    커밋해 0.25유닛이었다 — 그대로 배선했으면 **애니메이션이 켜지는 순간 적이 절반으로 줄었다.**
    원인은 `.meta` 의 `spritePixelsToUnits`(2048)와 `maxTextureSize`(512)를 눈으로 조합해
    **손계산**한 것. Unity 는 rect 를 축소된 텍스처 기준으로 다시 잡는다.
    🔑 **스프라이트 크기는 메타로 추론하지 말고 `sprite.bounds.size` 로 직접 물을 것**
89. ⚠️ **풀 재사용 시 프레임 배열을 반드시 지운다.** 적 6종이 프리팹 하나를 공유하므로,
    안 지우면 늑대가 죽은 자리에 재사용된 슬라임이 **늑대 프레임으로 걷는다**
90. ✅ **시작 프레임을 개체마다 어긋나게** 준다 — `AnimPhase` 를 어긋나게 준 것과 같은 이유다.
    걸음 속도는 자기 `MoveSpeed` 를 1배로 본다 (오우거 1.2 는 느리게, 돌진 중인 늑대는 빠르게)

**22차 (I-59) — 체감 튜닝 문서**

91. 🔑 **"동작하나?"와 "느낌이 맞나?"는 다른 문서에 둔다.** 전자는 한 번 확인하면 끝나지만
    후자는 **고치고 다시 플레이하는 반복 작업**이라, 한 목록에 섞이면 그 목록이 영원히 줄지 않는다.
    `TODO.md` §1 이 그 상태였다 → `TUNING.md` 로 분리
92. ✅ **"체감 미확인" 이라고만 적으면 몇 달 뒤에 무엇을 보려던 건지 알 수 없다.**
    그래서 `TUNING.md` 의 모든 항목을 **무엇을 보나 → 어느 파일의 무엇을 얼마나** 형식으로 썼다
93. 🔴 **적 타격 반응 5개가 `EnemyBase.cs` 의 `const` 다** — 플레이어 쪽은 `Economy.csv` 에 있는데
    적 쪽만 빠졌다. **하필 체감 튜닝에서 가장 많이 만질 값들**이라 고칠 때마다 도메인 리로드를
    기다려야 한다. 튜닝에 들어가기 직전에 CSV 로 뺄 것.
    ⚠️ 적은 **프리팹 하나를 공유**하므로 씬 컴포넌트가 아니다 — `Economy.csv` 임포터가
    프리팹 경로를 다룰 수 있는지 먼저 확인해야 한다
94. ✅ **인용한 수치를 전부 원본에서 다시 읽었다.** `TODO.md` 에 적힌 값을 그대로 옮기지 않았다 —
    그게 20차의 PPU 오판을 만든 방식이다 (위 88)

**23차 (I-60) — 폰트 정리**

95. 🔑 **Dynamic TMP 폰트는 "플레이만 해도 바뀌는 애셋"이다.** 런타임에 처음 만난 글리프를
    그 자리에서 아틀라스에 굽고 애셋을 dirty 로 만든다. 그래서 매 커밋 25만 줄 diff 가 났고
    이 파일 하나만 계속 staging 에서 빼야 했다. **Static 으로 굳히면 멈춘다**
96. 🔴 **Dynamic 은 빠진 글리프를 숨긴다.** 굽기 전 폰트에 ASCII 가 72자뿐이었는데
    (`" # $ % & ' * ; < = > @ J Z \ ^ \` j q { | } ~` 23자 누락) **쓰는 순간 구워져서 문제가 안 보였다.**
    Static 화의 진짜 위험은 "지금 안 보이는 누락이 그때 빈칸으로 드러나는 것"이라,
    **ASCII 32~126 을 전부 넣는 것**이 대책이다
97. ⚠️ **`TMP_FontAsset.CreateFontAsset` 으로 새로 만들지 말 것.** GUID 가 바뀌어 씬 참조가 전부 끊긴다.
    기존 애셋을 그대로 두고 `ClearFontAssetData` → `TryAddCharacters` → `atlasPopulationMode = Static`
98. ✅ **폰트에 한글이 66자 구워져 있었다** — 한국어 UI 시절의 잔재.
    버리기 전에 씬 `m_text` · 프리팹 · C# 리터럴 · CSV 표시 열 **네 경로를 전수 확인**했다.
    C# 리터럴은 8건이 걸렸지만 전부 `Tooltip`·주석이라 TMP 로 안 간다
99. ✅ **삭제 전 GUID 로 참조를 확인한다.** 18장의 GUID 를 `.unity`/`.prefab`/`.asset`/`.mat`
    전체에서 검색해 0건인 것을 확인하고 지웠다. `Assets/Fonts` 55MB → 4.8MB

**24차 (I-61) — 승급 배타**

100. 🔑 **판정식이 무엇을 들고 있는지가 데이터 위치를 정한다.** `TODO.md` 는 `Tier` 를
     레시피(`ClassEvolutions.csv`)에 두려 했지만, 배타 판정은 "**내 사슬**에 같은 티어가 있나"를
     묻고 사슬이 들고 있는 건 `CharacterClassData` 다. 게다가 **T1 은 자기를 만든 레시피가 없어**
     역추적이 아예 성립하지 않는다 → `Classes.csv` 가 맞다
101. 🔑 **같은 사실을 말하는 열을 두 개 두지 않는다.** `IsPromotionOnly` 는 정확히 `Tier > 1` 이라
     별도 열로 두면 어긋날 수 있고, **어긋나면 T2 로 런이 시작된다.** 파생 프로퍼티로 뒀다
102. 🔴 **"막힌다"만 확인하면 반쪽짜리 검증이다.** 티어 규칙이 *모든* 승급을 막고 있어도
     "Doomlord 가 잠겼다"는 같은 로그가 나온다. **T3(Aegis)이 열리는 것까지** 확인해야
     "같은 티어만 막는다"가 증명된다
103. ⚠️ **UI 필터는 표시만 막고 선택은 안 막는다.** `ClassSelectUI.BuildCards` 에서 걸러도
     `OnShown` 이 **저장된 `SelectedClassIndex` 를 그대로** `Select` 에 넘기므로,
     그 인덱스가 승급 전용을 가리키면 **카드는 없는데 Start 버튼만 살아난다.** 두 곳을 같이 막았다

**25차 (D2) — 설치 대기 표시 / 실플레이 버그 3건 분류**

104. 🔑 **"버그"의 셋 중 하나는 버그가 아니었다.** B3 은 예외도 null 도 아니고 **설계대로** 돌고 있었다.
     `MaxCount` 가 레벨과 함께 늘어 큐에 조용히 쌓인 것뿐이다. 그러니 고칠 대상은 로직이 아니라
     **"UI 가 한 약속을 조작이 안 지킨다"** 였다. 원인을 읽기 전에 코드부터 고쳤으면
     큐 순서를 바꿔 **건물 채수 밸런스를 건드렸을** 것이다
105. 🔑 **`NextPending` 은 이미 public 이었다.** 필요한 API 가 다 있는데 HUD 가 안 쓰고 있었다 —
     "없어서 못 보여줬다"가 아니라 **아무도 물어본 적이 없었다.** 기능을 붙이기 전에
     기존 공개 멤버부터 볼 것
106. 🔴 **스프라이트 시트에서 `uv 0~1` 검사는 아무것도 막지 못한다** (B1). 시트에서 잘라 온
     스프라이트의 `IN.uv` 는 **텍스처 전체 기준**이라 `[0.25, 0.5]` 같은 부분 구간이다.
     낱장 png 시절엔 **우연히** 맞았고 I-58 에서 시트로 바꾼 순간 전제가 깨졌다.
     **애셋 형태를 바꾸면 그 애셋을 읽는 셰이더의 전제도 같이 확인할 것**
107. ✅ **애셋이 깨졌다는 판정은 "칸당 알파 덩어리 수"로 한다** (B2). bbox 크기만으로는
     "원래 큰 스프라이트"와 구분이 안 된다. 연결 성분이 1개가 아니면 그 칸엔 캐릭터가 여러 마리다
108. ⚠️ **첫 가설이 틀렸다.** B2 를 "시트가 4방향×4프레임인데 코드가 일렬로 순환한다"로 추정했는데,
     정상인 `Ogre_Walk.png` 를 **눈으로 열어 보니** 16칸이 전부 같은 방향이었다.
     사용자가 "스프라이트 열어서 확인하고"라고 하지 않았으면 담당을 DEV 로 잘못 잡을 뻔했다
109. ⚠️ **병렬 세션이라 문서 쓰기가 충돌한다.** `BUGS.md` 를 고치려는데 CONTENT 가 그 사이 B4 를
     추가해 `Edit` 가 거부됐다. 규칙의 **"쓰기 직전에 다시 읽는다"** 는 예방책이 아니라 필수 절차다

**26차 (D3) — DEV 치트 패널**

110. 🔑 **치트가 게임 로직을 우회하면 거기서 나온 상태를 신뢰할 수 없다.** `DevPanel` 은 슬롯 상한
     (`CanAcquire`)을 그대로 지키고, "레벨 내리기"도 새 경로를 만드는 대신 이미 검증된 공개 API
     (`RemoveItemFull` + `ApplyItemFromShop` × N)만 조합한다. 뚫었으면 *"인벤토리엔 있는데 무기는 없는"*
     유령 상태가 만들어지고 — **치트로 만든 유령은 버그처럼 보인다**
111. ✅ **개발 도구는 씬에 넣지 않는다.** `[RuntimeInitializeOnLoadMethod]` 로 스스로 붙게 하면
     ① `#if` 로 잘려 나가는 릴리즈 빌드에서 **"Missing script" 로 안 남고** ② `.unity` 에 흔적이 0 이라
     **병렬 세션의 씬 충돌을 안 만든다.** 대신 이 콜백은 게임 시작 때 **한 번만** 도니
     `DontDestroyOnLoad` 가 필수다 — Retry 는 씬을 다시 로드한다(I-17)
112. ✅ **개발 도구엔 IMGUI 가 낫다.** 캔버스로 만들면 행 20개 배선이 본 게임보다 커지고, TMP 는
     Static 115자라 **문자표에 없는 글자가 빈칸이 되며**(I-60·B4), 캔버스 정렬 순서(I-50)와도 싸운다.
     `OnGUI` 는 셋 다 통째로 피한다. 못생긴 건 감수한다
113. 🔴 **`Key.F1` 을 개발용 단축키로 쓰지 말 것.** 유니티 에디터가 "매뉴얼 열기"로 물고 있어
     누르면 브라우저가 뜬다. 백틱(`Key.Backquote`)으로 갔다
114. 🔴 **MCP 로 주입한 키 입력은 게임에 도달하지 않는다** (게임 뷰 포커스 없음).
     `InputSystem.QueueStateEvent` 뒤에 `InputSystem.Update()` 를 직접 부르면 전이가 그 자리에서
     소비돼 다음 `MonoBehaviour.Update` 에는 `wasPressedThisFrame` 이 안 남고, 큐만 넣어도 안 먹는다.
     **입력이 필요한 검증은 로직을 직접 호출해서 하고, 화면은 사용자 눈에 맡긴다**

**27차 (D4) — 걷기 시트 교체**

115. 🔴 **애셋을 교체할 때는 png 만 덮어쓴다. `.meta` 를 지우면 안 된다.** 지우면 PPU 가 기본값
     1024 로 재생성돼 **적이 걷는 순간 크기가 절반**이 되고(I-58 재발), 스프라이트 서브애셋 GUID 가
     새로 발급돼 `EnemyData.WalkFrames` **16칸이 통째로 `None`** 이 된다. 반대로 png 만 갈면
     슬라이스·PPU·GUID 가 전부 살아 있어 **배선이 한 칸도 안 끊긴다**
116. ✅ **깨진 애셋을 고친 뒤에는 "그 깨진 칸이 실제로 화면에 떴는지"를 봐야 한다.** `WalkFrames` 가
     16장이라는 것만으로는 부족하다 — 예전에 깨져 있던 건 f8~f15 인데 짧게 보면 그 구간이 안 뜬다.
     살아 있는 적을 `Initialize` 로 문제 종류로 **강제 교체**하고 화면에 실제로 쓰인 칸을 전수로 찍어
     f8~f15 가 포함된 것을 확인했다
117. ⚠️ **`EnemyData` 의 필드는 대문자 `WalkFrames` 다.** `so.FindProperty("walkFrames")` 는 6종
     전부 `null` 을 돌려준다 — "배선이 끊겼다"로 오해하기 딱 좋다. 같은 맥락으로 `EnemyBase.Data` 는
     public 이 아니고(`CS0122`), 스프라이트 서브애셋 이름은 `<Name>_Walk_frame_0` 이 아니라 그냥
     **`frame_0`~`frame_15`** 다. **이름은 추측하지 말고 한 번 찍어 볼 것**

**28차 (D5) — 일시정지·옵션창 영문화**

118. 🔴 **플레이 모드에서는 씬을 저장할 수 없다.** `EditorSceneManager.MarkSceneDirty` / `SaveScene`
     이 `InvalidOperationException: This cannot be used during play mode` 를 던진다. 더 무서운 건
     **플레이 모드에서 한 씬 변경은 플레이를 끄면 전부 되돌아간다**는 것이다 — 치환은 `6/6` 성공했는데
     디스크에는 아무것도 안 남았다. 씬을 고치는 `RunCommand` 는 맨 앞에
     `if (EditorApplication.isPlaying) return;` 가드를 넣을 것
119. 🔴 **에디터 상태는 매번 다시 확인한다.** D4 끝에 내가 껐던 플레이 모드를 그 사이에 사용자가
     다시 켜 놓았다. `BOARD.md` §2 는 **내 기록일 뿐 실제 상태가 아니다** — `ManageEditor(GetState)`
     로 물어야 한다. (그리고 임의로 끄지 말고 **물어보고** 끈다)
120. 🔴 **`ScreenSpaceOverlay` 캔버스는 `Unity_Camera_Capture` 에 한 픽셀도 안 잡힌다.** 카메라를
     렌더 타깃에 그리는 방식이라 오버레이가 합성되지 않는다. **플레이 모드 한정으로** `renderMode` 를
     `ScreenSpaceCamera` + `worldCamera = Camera.main` 로 바꿔 찍으면 된다 —
     플레이를 끄면 되돌아가므로 디스크에는 안 남는다(`git diff --stat` 으로 확인할 것)
121. ✅ **TMP 글자가 실제로 그려지는지는 `HasCharacters` 보다 `ForceMeshUpdate` 가 강하다.**
     문자표에 있어도 렌더링 단계에서 빠질 수 있다. `ForceMeshUpdate` 뒤
     `textInfo.characterCount` 와 `characterInfo[i].isVisible` 개수가 같으면 **빈칸이 하나도 없다**는 뜻이다
122. ⚠️ **MCP 호출 사이에는 프레임이 간다.** `PauseMenuUI.Open()` 을 부르고 다음 호출에서 봤더니
     이미 `LevelUp` 으로 넘어가 패널이 닫혀 있었다. 상태를 고정하려면 `ManageEditor(Pause)` 로
     에디터를 멈출 것 — 멈춰도 `Camera_Capture` 는 찍힌다
123. ⚠️ **`foreach (Transform child in parent)` 안에서 `SetAsLastSibling()` 을 부르지 말 것.**
     순회 중 순서가 바뀌어 일부가 건너뛰어진다 — 끄려던 패널이 안 꺼졌다. 순회와 재정렬을 분리한다
124. ⚠️ **씬 YAML 을 bash `grep 'm_text:.*\\u'` 로 세지 말 것.** 이스케이프 해석 때문에 오탐한다
     (고친 뒤인데 8을 반환했다). `grep -n 'm_text:' … | grep 'u[0-9A-F]\{4\}'` 처럼 리터럴로 다시
     거르거나, Unity 쪽에서 문자 코드로 직접 검사할 것

**29차 (D6) — 적 타격감 상수를 CSV 로**

125. 🔴 **"값이 안 바뀐다"는 배선 검증이 될 수 없다.** CSV 12줄이 전부 원래 값이라
     배선이 통째로 죽어 있어도 화면은 똑같다. **한 값을 일부러 크게 바꿔 왕복**시켜야
     "안 바뀐 것"과 "안 읽힌 것"이 구분된다 (`6`→`20`→`6`). 되돌리기까지가 한 세트다
126. 🔴 **씬 컴포넌트를 프리팹 인스턴스에 붙이면 `m_AddedComponents` 오버라이드가 된다.**
     동작은 정상이지만 **프리팹 Revert 한 번에 사라지고**, 그러면 CSV 가
     `! 씬에 xxx 없음` 으로 조용히 건너뛰어진다 — **에러 없이 파이프라인만 먹통이 된다**
127. ⚠️ **`RunCommand` 의 `result.Log("{0}…{4}", …)` 는 인자를 3개까지만 채운다.**
     `{3}` 부터는 **포맷 문자열이 그대로 출력된다** (숫자를 읽는 줄 알고 한 번 속았다).
     4개 이상은 `string.Format` 으로 미리 조립해서 넘길 것
128. ⚠️ **`timeScale == 0` 을 히트스톱의 증거로 쓰지 말 것.** `LevelUp`·`GameOver` 도 0 이다.
     측정 전에 `Time.timeScale = 1f` 로 맞춰 놓고 봐야 한다. 그리고 `DoHitstop` 은
     `CurrentState != Wave` 면 return, `_hitstopRoutine != null` 이면 무시한다(겹침 방지) —
     **연달아 두 번 부르면 두 번째는 안 걸린다**
129. ⚠️ **플레이를 켜 두면 원하는 상태가 유지되지 않는다.** 방치하면 `Wave` → `LevelUp` →
     `GameOver` 로 알아서 넘어간다. 측정 사이에 **상태를 매번 다시 확인**할 것
130. ℹ️ **적 등급은 런타임에 바꿔 끼울 수 있다.** `Initialize(data, isElite, isBoss)` 가
     소환 인자라 기존 잡몹을 엘리트/보스로 재초기화하면 **Elite/Boss 노드에 가지 않고도**
     저항·임팩트를 검증할 수 있다. `EnemyData` 는 `AssetDatabase.FindAssets("t:EnemyData")` 로
     가져온다 (`EnemyBase.Data` 와 `IsDead` 는 `protected` 라 밖에서 못 읽는다)

**30차 (D7) — 진화 무기 아이콘 배선**

131. 🔴 **"애셋이 없다"와 "애셋 배선이 없다"는 다른 문제다.** 진화 아이콘 3장은 I-57 에
     이미 들어와 있었고 빠진 건 `Weapons.csv` 의 `Icon` 열 하나였다. 그런데
     `TODO.md` 는 20차부터 "교체 완료"라고 적고 있었다 — **png 를 만든 것을 배선까지
     끝낸 것으로 적으면 문서가 조용히 거짓말을 한다.** 만든 것과 꽂은 것을 따로 적을 것
132. ⚠️ **CSV Import 검증은 "바뀐 줄"이 아니라 "안 바뀐 줄"을 봐야 한다.** Import 는
     대상 SO 를 통째로 다시 쓰므로 의도한 열 하나만 움직였는지는 `git diff` 로만 안다.
     이번엔 **파일당 `Icon` 한 줄**이라 다른 수치가 안 되돌아갔음이 증명됐다.
     ℹ️ 다른 CSV 도 같이 돌아가므로 **직전 작업의 산출물**(`Economy.csv 43/43`)도 함께 확인할 것

**31차 (D8) — 소리 없던 트리거 배선**

133. 🔴 **"안 들린다"는 배선 실패가 아니라 볼륨 0 일 수 있다.** `AudioManager.PlaySfx()` 는
     `SfxVolume <= 0f` 이면 **보이스에 클립을 물리기도 전에 return** 한다(`:158`). 이 머신은
     저장된 `sfx_vol` 이 `0.00` 이라 첫 검증에서 6개가 **전부 `(못 찾음)`** 으로 나왔고
     배선이 통째로 실패한 것처럼 보였다. **소리를 검증하기 전에 볼륨부터 확인할 것.**
     `SetSFXVolume` 은 `PlayerPrefs` 를 안 쓰므로 런타임에서 `1f` 로 올려도 설정이 안 바뀐다.
134. ⚠️ **"끝에 추가한다" 같은 주석은 구현을 읽어 확인할 것.** `AudioId.cs:8` 이 그렇게 적혀
     있었지만 `AudioLibrary` 는 **배열 인덱스가 아니라 `Id` 값**으로 매핑한다(`:70`).
     실제 규칙은 *"기존 항목의 **값**을 바꾸지 말 것"* 이고 **빈 번호 삽입은 안전**하다.
     **틀린 주석은 다음 사람도 똑같이 걸리게 한다 — 발견하면 그 자리에서 고친다.**
135. 🔴 **분기를 검증할 때는 양쪽을 다 봐야 한다.** 엘리트 사망음은 한 프레임에
     **잡몹 하나 + 재초기화한 엘리트 하나**를 같이 죽여 `SFX_EnemyDie` 와 `SFX_EnemyDieElite` 가
     동시에 물리는 것을 봤다. 엘리트만 봤으면 **분기가 죽어 있어도**(항상 Elite) 통과했을 것이다.
     ℹ️ 요청서는 "Elite/Boss 노드로 가야 한다, 노말 웨이브에서는 검증할 수 없다"고 봤지만
     **D6 의 `Initialize(data, isElite:true)` 재초기화 우회로**로 노말 웨이브에서 증명된다.
136. ⚠️ **생성 도구의 기본 임포트 설정은 프로젝트 규격과 다르다.** Unity AI 의 `GenerateSound` 는
     **Vorbis + 스테레오**로 들어오는데 기존 SFX 14개는 **ADPCM + `forceToMono`** 였다.
     그대로 뒀으면 6개만 규격이 다른 채 묻혔을 것이다. **기존 애셋의 임포트 설정을 먼저 읽고 맞출 것.**
137. ℹ️ **요청서가 지목한 위치가 요청서 자신의 의도와 어긋날 수 있다.** `Heal` 은
     `RestaurantBuilding.OnCooldownElapsed()` 로 왔지만 그 메서드는 **회복을 안 한다** — 픽업을 떨굴 뿐이다.
     요청서 자신의 규칙("주기적인 건물 산출에 소리를 달면 잔소리")을 적용하면 `HealPickup` 이 맞다.
     **스펙과 코드가 어긋나면 임의로 맞추지 말고 사용자에게 근거와 함께 물을 것.**

**32차 (D9) — 활이 화살을 쏜다**

138. 🔴 **Single 스프라이트의 `bounds.size` 는 알파 크기가 아니라 텍스처 전체 rect 다.**
     256×256 캔버스에 200×72 짜리 화살을 그리고 PPU 512 로 넣으면 `bounds` 는
     `(0.391, 0.141)` 이 아니라 **`(0.500, 0.500)`** 이다. 둘 다 맞는 숫자다 —
     **판정 기준에 크기를 적을 때 `bounds` 기준인지 알파 bbox 기준인지 같이 적을 것.**
     (Multiple 모드는 슬라이스 rect 를 쓰므로 또 다르다.)
139. 🔴 **"안 맞았다"를 애셋 탓으로 돌리기 전에 대조군을 같이 쏠 것.** 화살이 적을 통과해
     사거리 끝까지 날아갔는데, 같은 조건으로 **기존 `Proj_Bullet` 을 같이 쏘니 이동 거리가
     2.37 로 완전히 같았다** — 새 프리팹이 아니라 **테스트 조건**(스폰 직후 발사 + 낮은 프레임률)이
     문제였다. 원인이 새것에 있는지 판을 가르는 가장 싼 방법은 **옛것을 나란히 돌리는 것**이다.
140. ⚠️ **에디터가 포커스를 잃으면 프레임 간격이 커져 트리거가 뚫린다.** `transform.Translate`
     로 움직이는 발사체는 한 프레임 이동량이 콜라이더 반지름을 넘으면 그냥 통과한다.
     원격(MCP) 검증에서 속도가 빠른 것을 볼 때는 **속도를 낮추거나 표적을 크게** 할 것.
     (게임 자체의 터널링이 아니라 **검증 환경**의 함정이다.)
141. ⚠️ **`AssetDatabase.CopyAsset` 은 로컬 fileID 를 보존한다.** 복제한 프리팹의 루트
     GameObject fileID 가 원본과 똑같이 나와도 **버그가 아니다.** 둘을 가르는 건 **guid** 다.
     "fileID 가 같은데 괜찮나"로 시간을 쓰지 말 것.
142. 🔴 **새 png 를 `Assets/` 에 넣기만 하면 임포트 설정이 프로젝트 규격과 다르다.**
     Unity 기본값은 `ppu=100 · Bilinear · Compressed · Multiple` 이다.
     `TextureImporter` 로 명시적으로 덮고 **`SaveAndReimport()` 까지** 부를 것 (교훈 136 의 그림판).
143. 🔴 **`transform.Translate` 의 기본은 `Space.Self` — "진행 방향 = 현재 회전"이다.**
     그래서 **자전과 직진은 양립하지 않는다.** 매 프레임 z 를 돌리면 발사체가 나선을 그린다.
     방향을 `Initialize` 때 굳힌 **월드 벡터**로 따로 들고 가면 회전이 순수 연출이 된다.
     회전을 한 번만 설정하는 기존 발사체에게는 **수식이 완전히 같다**
     (`Translate(Space.Self)` 는 월드 회전을 쓰고 스케일을 무시한다).
144. 🔴 **그림의 실제 반지름을 재고 나서 스케일 식을 쓸 것.** `SwingArc` 는 셀이 256px 이라
     반지름 128px 로 보이지만 실측은 **107.4px** 이다. `localScale = Range` 로 뒀으면
     **호가 사거리를 7% 부풀려 보여 준다** — 그림이 사거리를 속이는 건 판정 버그보다 나쁘다.
     `AoeProjectile` 의 `localScale = radius / spriteRadiusAtScaleOne` 패턴을 쓸 것.
145. 🔴 **관통은 카운터가 아니라 hit-set 이 있어야 관통이다.** `_pierceLeft--` 만 두면
     **한 마리의 콜라이더를 세 번 스치는 것**으로 관통 3이 소모된다. 그건 관통이 아니라 연타다.
     맞힌 적을 `HashSet` 에 기억하고 **풀에서 꺼내는 지점(`Initialize`)에서 비운다.**
146. 🔑 **관통이 "3마리를 뚫었다"인지 "1마리를 3번"인지는 소멸 *위치*로 가른다.**
     HP 는 `protected` 라 밖에서 못 읽는다. 대신 적을 일렬로 띄워 세우면,
     한 마리를 3번 때렸을 때는 **첫 적 앞**에서 멈춘다. 세 번째 적 앞에서 멈췄다면 관통이다.
147. 🔑 **0.2초짜리 애니메이션은 프레임을 전부 겹쳐 세워 한 장으로 찍는다.**
     그냥 캡처하면 한 프레임밖에 안 잡힌다. 6프레임을 같은 중심에 겹치면 부채꼴 전체가 나오고,
     **알고 있는 반지름 위에 점을 찍어 두면 중심과 크기를 동시에 증명**할 수 있다.
148. ⚠️ **임시 오브젝트 이름은 정리 접두어가 서로 먹지 않게 지을 것.**
     `StartsWith("D10_B")` 가 레인 B 의 적들과 `D10_Bullet` 을 **둘 다** 지웠다.
     파괴된 GameObject 가 풀 큐에 남아 다음 `Get()` 에서 `MissingReferenceException` 이 난다.
149. 🔴 **일시적 상태 변경은 원본을 덮어쓰지 말고 "읽는 쪽에서 곱하고, 시간으로 만료"시킬 것.**
     슬로우가 `MoveSpeed` 를 덮어썼다면 원래 값을 어딘가 보관해야 하고, 그 보관값이
     풀 재사용을 한 번만 건너뛰어도 **적이 영구히 느려진다.**
     `CurrentSpeed => MoveSpeed * (Time.time <= _slowUntil ? _slowMult : 1f)` 는
     아무도 해제해 주지 않아도 저절로 풀린다.
150. 🔴 **오브젝트 풀이 도는 게임에서 Trigger Enter/Exit 짝맞추기는 반드시 깨진다.**
     적이 장판 **안에서 죽거나 풀로 반납되면 Exit 가 안 온다.** 지속 효과는
     콜라이더 대신 **매 틱 `OverlapCircleAll` + 짧게 다시 걸기**로 만든다.
     다시 거는 지속시간은 틱 간격보다 **길어야** 한다(`tick × 1.6`) — 같으면 사이가 끊긴다.
151. 🔴 **배율 효과의 겹침은 곱하지 말고 "가장 센 것 하나만" 적용할 것.**
     `0.6 × 0.6 = 0.36` 이면 적이 사실상 멈춘다. 같은 장판을 여러 개 까는 게
     그 무기의 성장 방향이라면 **반드시 터진다.**
     검증은 값으로 한다 — 곱셈이었다면 나왔어야 할 `0.504`/`0.792` 가 **한 번도 안 나온 것**이 증거다.
152. ⚠️ **요청서가 지목한 메서드 이름을 믿지 말고 실제 리셋 블록을 찾을 것.**
     "`Setup()` 에 초기화를 넣어라"였지만 풀 재사용 리셋(`_knockbackTimer`·사망 팝·콜라이더)은
     `Initialize()` 안에 있었다. **엉뚱한 곳에 넣으면 조용히 안 먹는다.**
153. 🔴 **"C# 기본값은 기존 프리팹을 보호한다"를 두 번째로 써먹었다.**
     `fuseTime = 0f` 는 기존 `Proj_Bomb.prefab` 에 **직렬화되어 있지 않아** 초기값이 먹는다.
     `.prefab` 파일이 미변경이어도 동작이 그대로다 (D10 `pierceCount = 1` 과 동일).
     **그래도 대조군으로 확인할 것** — 이번에도 `PLAIN` 을 같이 쏴서 `fuseTotal=0.00s` 를 봤다.
154. ⚠️ **프로브의 "도착 판정" 허용치가 실제보다 한 프레임 일찍 걸린다.**
     `sqrMagnitude < 0.0004`(0.02유닛)가 프레임당 이동거리보다 작지 않으면 먼저 찍힌다.
     착탄 직후 `rotZ=284.2` 가 나와 **회전 리셋 버그로 오인**했는데, 다음 프레임부터는 전부 `0.0` 이었다.
155. ⚠️ **바닥은 `SpriteRenderer` 가 아니라 Tilemap 이다.** 픽셀 측정용으로 화면을 비우려고
     모든 `SpriteRenderer` 를 꺼도 **배경이 그대로 남는다.** 밝기 임계값이 화면 전체
     (2,073,600px)를 잡았다. 색 마스크로 우회할 것.
156. ⚠️ **`result.Log` 는 서식 지정자를 안 먹는다.** `{0}` 만 치환하고 `{6:F1}` 은
     **글자 그대로** 찍힌다. `.ToString("F1")` 로 문자열을 이어 붙일 것.
157. ⚠️ **`RunCommand` 는 `System.Reflection` 을 통째로 거부한다.** `BindingFlags` 한 단어만
     있어도 **스크립트 전체가 반려**된다. 타입 존재 확인 같은 건 다른 방법을 찾을 것.
158. 🔑 **레벨업 패널은 `timeScale = 0` 이라 프로브 코루틴을 통째로 멈춘다.**
     긴 런타임 측정을 하려면 `WaitForSecondsRealtime` 으로 도는 감시 코루틴을 붙여
     `LevelUpManager.HidePanel()` 로 닫아 줘야 한다. 안 그러면 **측정이 조용히 중단된다.**
159. 🔴 **요청서의 피벗·반지름 규격도 그림을 재고 나서 믿을 것** (144 의 재발).
     `ToxinField` 는 요청서가 *"Pivot Center · 반경 1.0"* 이라 했지만 실측은
     중심 `(134, 132)px` · 반경 `≈97px` 이었다. Center 를 쓰면 **맞는 원과 보이는 원이 어긋난다.**
     PIL 은 y 를 **위에서** 재고 Unity 는 **아래에서** 잰다 — `y = (256-132)/256` 로 뒤집을 것.
160. 🔴 **`SceneWiring` Import 는 씬을 더럽히기만 한다 — `SaveScene` 을 안 하면 배선이 날아간다.**
     `Weapons`/`Items` 는 `.asset` 을 직접 쓰지만 `SceneWiring` 은 **씬의 컴포넌트 참조**를 고친다.
     Import 로그가 `11/11 적용` 이라고 해도 저장 전이면 아직 디스크에 없다.
161. 🔴 **피해 팝업은 플레이어가 맞을 때도 뜬다.** `PlayerStats.cs:258` 과 `EnemyBase.cs:392` 가
     같은 `DamagePopupManager.Instance.Show` 를 부른다. 팝업 수로 **"적 명중"을 세면
     접촉 피해가 섞여** 값이 부풀려진다 (관통 3짜리 무기에서 `hitsPerProj = 5.00` 이 나왔다).
     **플레이어로부터의 거리로 걸러야** 실값(3.10)이 나온다.
162. 🔑 **가만히 선 프로브에게 적은 근접 사거리까지 오지 않는다.** 가장 가까운 적이
     **4.67유닛**에 머물러 사거리 2.50 인 검이 **6초 동안 0회** 휘둘렀다 —
     무기가 고장 난 게 아니라 **측정 배치가 틀린 것**이다.
     근접·관통을 재려면 `EnemyBase.Reposition()`(public)으로 **직접 몰아 세울 것.**
163. 🔑 **풀링 객체는 "인스턴스 ID 상승엣지"로 셀 수 있다** — 단, 수명이 한 프레임보다 길 때만.
     발사체 0.75s · 팝업 0.5s · 호 0.2s 는 전부 안전하다. 반납 직후 재획득이
     60fps 폴링에서 "계속 있었음"으로 보이지 않기 때문이다.
164. ⚠️ **프리팹 이름이 거짓말을 한다.** `Shuriken` 은 `Weapon_Sword.prefab`(=`ProjectileWeapon`)을,
     `Sword` 는 `Weapon_Melee.prefab`(=`MeleeWeapon`)을 쓴다. 이름은 유물이니 **`m_Script` 를 볼 것.**
165. 🔴 **`WeaponBase` 를 상속하면서 `Update()` 를 선언하면 그 무기는 영영 발사되지 않는다.**
     쿨다운이 베이스의 `private void Update()` 안에 있어서 파생 선언이 그걸 **가린다.**
     경고도 예외도 안 난다. 파생 무기의 매 프레임 처리는 **`LateUpdate()`** 에 둘 것 —
     위치 보정이라면 플레이어가 움직인 뒤에 도는 게 맞기도 하다.
166. 🔑 **풀에 들어가는 무기의 "부산물" 철거는 `OnDisable()` 하나로 끝난다.**
     `WeaponManager.RemoveWeapon` → `ObjectPool.Return` → `SetActive(false)` 가 곧 `OnDisable` 이다.
     베이스 클래스에 새 훅을 팔 필요가 없다. 단 **씬을 내릴 때도 불리므로** `if (Pool != null)` 로 감쌀 것.
     이게 없으면 상점에서 환불한 뒤 **소환수만 필드에 영원히 남는다.**
167. 🔴 **풀에서 나온 오브젝트는 `GetInstanceID()` 가 재사용된다 — "새 것"을 ID 로 가리면 안 된다.**
     `_seen.Add(id)` 로 중복을 막으면 **첫 인스턴스만** 잡히고 두 번째부터 영영 안 잡힌다
     (문어 후리기 14회 중 1회만 계측됐다). 교훈 163 의 상승엣지는 **ID 가 아니라 상태**에 걸 것 —
     `bool _lashActive` 처럼 "지금 그것이 살아 있는가"의 `false → true` 전이를 본다.
168. 🔴 **프로브를 얼리는 `timeScale = 0` 은 두 군데다.** 레벨업 패널(교훈 158)뿐 아니라
     **ESC 일시정지**(`PauseMenuUI.Open()`)도 얼린다. 긴 시퀀스에는 둘 다 푸는 감시를 넣을 것 —
     `LevelUpManager.HidePanel()` · `if (state == Paused) PauseMenuUI.Instance.Close()`.
     증상이 **"로그가 그냥 끊김"** 이라 무기 버그로 오인하기 쉽다.
     진단은 `isPlaying / paused / timeScale / CurrentState` 를 한 번에 찍어 보는 것.
169. 🔑 **가만히 선 플레이어로는 "따라온다"를 증명할 수 없다.** 거리가 계속 `1.20` 으로만 찍히는데,
     그건 잘 따라온다는 뜻이 아니라 **아무 일도 안 일어났다는 뜻**이다. 콘솔이 같은 줄을
     접기까지 해서 더 헷갈린다. `FixedUpdate` 에서 `rb.MovePosition` 으로 **원운동을 직접 먹이고**
     거리의 **min/max**(0.42~1.99)와 `flipX` 전환 횟수를 볼 것.
170. 🔴 **"새 소리가 난다"는 "옛 소리가 안 난다"의 증거가 아니다.** 배선을 바꿨으면
     **빌려 쓰던 옛 키도 대조군으로 같이 세라.** `Swing=7` 만 보면 통과 같지만,
     같은 프레임에 `WeaponFire` 가 같이 울고 있어도 그 로그는 똑같이 나온다.
     D14 의 판정은 `Swing=7 Toxin=1` 이 아니라 **`Fire=0 Cast=0`** 이었다.
171. 🔴 **오디오 임포트는 Unity 기본값이 프로젝트 규격과 다르다.** 새 wav 는
     `compressionFormat: 1`(Vorbis) · `forceToMono: 0` 으로 들어오는데 기존 SFX 는 전부
     `2`(ADPCM) · `1` 이다. 눈에 안 보이고 **소리도 비슷해서** 그냥 지나간다.
     `AssetImporter.GetAtPath` → `defaultSampleSettings` 세팅 → `SaveAndReimport()` 로 맞출 것
     (D8 에서 이미 밟았고 D14 에서 또 밟았다 — **매번 확인해야 한다**).
172. 🔑 **`.asset` YAML 을 손으로 쓰지 말고 `SerializedObject` 로 고쳐라.**
     에디터가 애셋을 메모리에 들고 있으면 **그 사본이 이긴다** — 디스크 편집이 다음 저장에
     조용히 덮어써진다. `ApplyModifiedPropertiesWithoutUndo` → `SaveAssets` 후
     **디스크를 다시 읽어** 확인할 것. 재실행 안전장치(이미 있으면 중단)도 같이 넣는다.
173. 🔑 **효과음 길이는 그 무기의 연사 주기와 같이 봐야 한다.** 클립만 보면 0.28초는 짧지만,
     `MeleeWeapon` Lv5 의 연타 간격은 `hitDelay + comboInterval` = **0.247초**다.
     길이를 늘리는 순간 **본체끼리 겹친다.** 소리를 만드는 쪽에 "상한이 얼마"인지
     **코드에서 나온 숫자로** 알려 줄 것 — 안 그러면 다시 굽고 나서야 안다.
174. 🔑 **고칠 값을 찾기 전에 "같은 조건인데 안 아픈 사례"를 먼저 찾을 것** (D21).
     촉수가 플레이어를 덮는 문제에 DEV 는 `Range` 축소·`offsetDistance` 확대를 냈는데
     **둘 다 기하 가설**이었다. CONTENT 가 *"검 아크는 **같은 order 20 인데 플레이어 위에서
     터지면서도 기사를 0.00% 덮는다.** 중앙이 비어 있어서다"* 를 들이대자
     가설이 무너지고 **합성 문제**로 재분류됐다 — 그리고 `m_SortingOrder` **한 줄**로 끝났다.
     대조군은 답을 검증하는 도구만이 아니라 **문제의 성격을 바꾸는 도구**다.
175. 🔑 **예측이 실측과 어긋나면 "누가 틀렸나"보다 "전제가 언제 바뀌었나"를 먼저 볼 것** (D21).
     CONTENT 의 "잉크 3.7~4.2% 손실" 예상이 실측 **1.37%** 로 나왔다. 계산이 틀린 게 아니라
     **하루 전 커밋(D19)이 전제를 바꿨다** — 소환수가 공전을 그만두고 1.20 뒤로 물러나
     링 중심과 기사 중심이 어긋났다. 예상은 **공전 시절 기준**으로 맞는 값이었다.
176. 🔑 **정렬 순서 변경은 "같은 프레임을 두 번 렌더해서 빼면" 픽셀로 판정된다** (D21).
     `SwingArcFx` 를 꺼서 애니메이션을 멈추고 `SpriteRenderer.sortingOrder` 만 `20`/`-5` 로
     번갈아 `Camera.Render()` → `ReadPixels`. **기사 실루엣 마스크**는 플레이어
     `SpriteRenderer` 를 껐다 켜서 뜬다. "좋아 보인다"가 아니라 **93px → 0px** 가 나온다.
     최악 조건도 손으로 만들 것 — 적 8마리를 링 둘레에 세워 봤고 손실은 1.5% 였다.
177. 🔴 **`protected` 필드는 `RunCommand` 에서 안 보이고, `SerializedObject` 도 못 뚫는다** (D21).
     `WeaponBase.Data`/`Level` 접근은 `CS0122` 로 컴파일 실패고, `[SerializeField]` 가 아니라
     `FindProperty("Data")` 가 **`null` 을 돌려줘 NRE** 가 난다(부분 실행 경고까지 뜬다).
     `System.Reflection` 은 금지(교훈 157). **public 우회로를 찾을 것** —
     `AssetDatabase.LoadAssetAtPath<ItemData>(…)` → `.WeaponRef` · `.CurrentLevel` · `.GetRange(lv)`.
     private `[SerializeField] Sprite[] frames` 도 **스프라이트 시트에서 직접 로드**하면 같은 그림을 얻는다.
178. 🔑 **문서를 쓰기 전에 코드를 읽으면 설계가 바뀐다** (D22).
     "무한적 + 시간제한 이벤트"를 새로 짜려다 읽어 보니 **절반이 이미 있었다** —
     `UseTimerClear`/`SurvivalTime` 로 시간제한 생존이 이미 되고, `MaxAlive` 는 상한에 닿으면
     소환을 **취소가 아니라 대기**시켜서 **죽인 만큼 즉시 채워지며**, `RecycleFarEnemies` 는
     멀어진 적을 죽이지 않고 앞으로 옮겨 **도망이 이미 불가능**하다.
     남은 건 `WaveManager.cs:156` 의 `Count <= 0` 재해석 **한 줄**이었다.

179. 🔴 **런타임 로그는 "메모리에 있다"까지만 증명한다. 디스크는 `git diff` 로 봐라** (D24).
     `D20` 은 플레이 중에 `allItems.Length == 25` 를 읽고 **판정 PASS** 를 적었다.
     그런데 커밋된 씬은 **`allItems.Array.size: 24`** 였다 — `File/Save` 가 빠진 것이다.
     그대로 나갔으면 **행운 패시브가 레벨업 3택에 영원히 안 뜬다.**
     `BonusLuck` 배관은 전부 살아 있으니 **예외도 경고도 안 난다** — 조용히 없는 기능이 된다.
     ⇒ Import·인스펙터 편집처럼 **직렬화 파일을 바꾸는 작업**은 커밋 직전에
     `git diff <파일>` 에 그 변화가 **실제로 보이는지** 확인한다.
     이번엔 `D24` 가 우연히 같은 Import 를 다시 돌려서 드러났을 뿐이다.

180. 🔴 **경계가 0 인 판정은 아무것도 증명하지 못한다** (D25).
     "상점이 런 골드를 보는가"를 `CanReroll()` 로 쟀더니 `RunGold = 0` 인데도 `True` 가 나왔다.
     버그가 아니었다 — 상점을 연 적이 없어 `_currentRerollCost` 가 아직 `0` 이었고,
     검사식이 `0 >= 0` 이라 **무엇을 넣어도 통과**하는 상태였던 것이다.
     ⇒ `a >= b` 를 검증할 때는 **`b` 가 0 이 아닌지부터 확인**한다.
     경계값이 0 이면 그 테스트는 참/거짓을 가르지 않는다.
     다시 잴 때는 `Price = 10` 짜리 슬롯을 손으로 만들어 `0`/`9`/`10` 세 점을 찍었다 —
     **거짓이 나와야 하는 점**이 실제로 거짓이 되는지를 봐야 판정이 성립한다.

181. 🔑 **한 변수에 성격이 다른 두 예산이 들어 있으면 수치는 영원히 안 잡힌다** (D25).
     상점 가격이 반 년 가까이 "미정"이었던 이유는 게임 디자인이 어려워서가 아니라
     `MetaProgression.Currency` 하나가 **≈1,084G 짜리 소비 예산**과
     **≈156G 짜리 저축 예산**을 동시에 담고 있었기 때문이다.
     한쪽을 만지면 반드시 다른 쪽이 망가지니 어떤 값도 "맞다"가 될 수 없었다.
     ⇒ 밸런싱이 계속 제자리면 **값을 의심하기 전에 그릇이 하나인지 둘인지**를 본다.
     이번 수정은 **수치를 단 하나도 바꾸지 않았는데** 튜닝이 가능해졌다.

182. 🔑 **도구가 "실패했다"고 말해도, 판정은 디스크의 파일로 한다** (D26).
     Unity AI 생성 호출이 `success: false` 를 반환했는데 **결과물은 멀쩡했다** —
     세션이 직전 실패의 **낡은 job GUID 를 재생**하고 있었다. 반환값만 믿었으면
     1,166 KB · 색 102,531 개짜리 완성된 16프레임 시트를 버리고 작업을 접었을 것이다.
     같은 날 CONTENT 는 **1~2분 전에 읽은 상태**로 이미 고쳐진 애셋을 반려했다(`f09558e`).
     ⇒ 도구의 반환값도, 남이 적어 준 관측도 **둘 다 과거형이다.**
     애셋은 **크기·색 수·알파 extrema** 로 지금 재고, 판정을 적을 땐 **`mtime` 을 같이 적는다.**

183. 🔑 **판정 기준을 새로 쓸 때는 기존 것부터 그 기준으로 재 본다** (D26).
     요청-19 의 판정 ⑥ 은 "칸마다 여백 30px 이상" 이었다. 그런데 기준선인
     `Warrior_Walk` 를 재 보니 **2px** 였다 — **기존 7장 중 어느 것도 통과 못 하는 기준**이었다.
     통과시켜야 할 진짜 조건은 30 이라는 숫자가 아니라 **여백이 0 이 아닐 것**(칸 넘침 없음)이다.
     ⇒ 새 산출물에만 적용되는 기준은 **기존 산출물을 불합격시키는지 먼저 확인**한다.
     아니면 멀쩡한 물건을 다시 만들게 된다.

184. 🔑 **"안 바뀌는 게 정상"인 판정 줄을 하나 끼워 두면 배선을 공짜로 증명한다** (D28).
     `C27` 이 `Economy.csv` 의 `StageMapManager` 6줄을 **일부러 no-op 으로**(코드 기본값 그대로) 넣고
     판정 ⑤ 에 *"하나도 안 바뀌어야 정상"* 이라고 적었다. Import 뒤 씬에 오버라이드가 0개 생겼는데,
     이건 *값이 같다* 만 뜻하는 게 아니라 **임포터가 그 컴포넌트를 실제로 찾았다**는 뜻이기도 하다 —
     못 찾았으면 `49/49` 가 아니라 `43/49` + `!` 였을 것이다.
     ⇒ **새 배선을 열 때는 값을 바꾸는 줄과 안 바꾸는 줄을 같이 넣는다.**
     전자는 기능을, 후자는 **경로가 살아 있다는 것을 부작용 없이** 증명한다.

185. 🔴 **측정을 위해 바꾼 환경 설정은 측정이 끝나도 남는다** (D27 → D28).
     `D27` 이 품질 레벨을 `0`(Very Low) → `5`(Ultra) 로 올린 건 옳았다(빌드 기본값과 맞추려고).
     문제는 **Ultra 의 `vSyncCount` 가 `1`** 이고 `vSyncCount` 는 **품질 레벨마다 따로 저장**된다는 것이다.
     강제로 꺼 주던 `PerfHarness` 는 같은 세션이 지웠으니, 이제 **에디터 플레이가 144 Hz 에 고정된다.**
     `D27` 은 이걸 `PERF.md` 에 "함정 ①" 로 적어 놓고도 **그 상태 그대로 두고 나갔다.**
     ⇒ 계측 코드를 지울 때는 **계측을 위해 바꾼 설정도 같은 목록에 넣는다.**
     지울 수 없으면(=유지가 맞으면) **대가를 `TODO` 에 등재**한다. 이번엔 후자를 택했다.

186. 🔑 **요청을 보낸 쪽이 "여기서 막힐 것"이라고 짚어 준 자리는 실제로 버그다** (D29 / C28).
     `C28` 이 요청-21 에 *"`LevelUpManager` 가 큐를 처리하는지 확인해 줘 — 못 하면 이 요청은
     거기서 막힌다"* 고 미리 적어 뒀다. 확인했더니 **큐가 없었고**, 한 번의 `CollectXp` 로
     레벨이 여러 번 오르면 **카드가 조용히 사라지고 있었다**(`B10`).
     요청을 넣은 쪽은 자기 요구가 **어떤 가정 위에 서 있는지**를 안다 — 코드를 못 봐도 안다.
     ⇒ 요청서의 "⚠️ 확인해 줘" 항목은 **예의상 붙인 말이 아니라 가장 먼저 볼 곳**이다.
     이번엔 그 한 줄이 없었으면 합산 호출을 그대로 넣고 **플레이어 손해를 늘렸을 것이다.**

187. 🔑 **경계를 증명하려면 경계 양쪽에 하나씩 놓는다** (D29).
     "38유닛 밖을 회수한다"를 검증할 때 멀리 하나·가까이 하나만 놓으면 *동작한다*까지만 보인다.
     `5·15·25·35·37·39·45·80` 로 **사다리**를 놓았더니 걷힌 것이 `39·45·80`,
     남은 것이 `15·25·35·37` 이라 **경계가 37 과 39 사이 = 정확히 38** 임이 그 자리에서 나왔다.
     ⇒ 상수를 쓰는 판정은 **그 상수를 되읽어 낼 수 있게** 설계한다.
     교훈 180(*경계가 0 인 판정은 아무것도 증명하지 못한다*)의 실행판이다.

188. 🔑 **"기능이 없다"의 절반은 배관이 아니라 화면이 없는 것이다** (D30).
     메타 진행은 반 년간 "자료구조만 있고 안 붙어 있다"로 남아 있었다. 열어 보니
     `PurchaseUpgrade`·`GetStatBonus`·저장/로드가 **전부 완성돼 있었고**
     `PlayerStats` 도 **이미 그 보너스를 더하고 있었다.** 없던 건 애셋 7개와 패널 하나였다.
     ⇒ 큰 항목을 착수하기 전에 **"정말 없는 게 뭔지" 먼저 읽는다.**
     이번엔 그 덕에 코드를 거의 안 쓰고 끝났다.

189. 🔴 **새 폴더를 처음 만드는 코드는 `AssetDatabase` 를 믿으면 안 된다** (D30).
     `EnsureFolder` 가 `AssetDatabase.IsValidFolder` 만 봤는데,
     `StartAssetEditing()` 구간에서는 AssetDatabase 가 갱신되지 않아
     **방금 만든 폴더를 아직 "없다"고 답한다.** 행마다 `CreateFolder` 가 다시 돌아
     `UpgradeData 1`~`UpgradeData 6` 이 줄줄이 생겼다.
     🔑 **기존 임포터들은 폴더가 이미 있어서 이 버그를 여태 안 밟았다** —
     같은 코드가 몇 달을 멀쩡히 돌았다는 게 그 코드가 옳다는 뜻은 아니다.
     ⇒ 캐시 계층(AssetDatabase)과 실체(디스크)가 갈릴 수 있는 자리에서는 **둘 다 본다.**

190. 🔴 **검증이 사용자 데이터를 바꾸면 그것도 정리 대상이다** (D30).
     메타 강화 구매를 검증하려면 `PurchaseUpgrade` 를 불러야 하는데
     그 안에 `Save()` 가 들어 있어 **실제 `save.json` 이 바뀐다.**
     골드를 2,000 넣고 강화를 두 번 사면 그게 사용자의 진짜 저장에 남는다.
     시작 전에 백업하고 끝나고 되돌렸다.
     ⇒ "임시 스크립트와 씬 오브젝트를 반드시 지운다"(`CLAUDE.md` §4)에는
     **검증이 건드린 영구 데이터**도 포함된다.

191. 🔑 **판정을 오독할 수 없게 설계한다 — "무엇이 늘었나"가 아니라 "무엇만 늘 수 있나"** (D31).
     보스 소환을 검증할 때 "잡몹 수"를 셌더니 4 → 15 로 늘어서 통과로 봤다. **틀렸다** —
     그건 웨이브가 스스로 소환한 것이었고 **보스 소환은 한 번도 안 돌았다.**
     다시 잴 때는 **"Goblin 수"** 를 셌다. `Boss1` 웨이브에는 Goblin 이 없으므로
     **0 이 아니면 그건 보스가 부른 것뿐이다** — 다른 해석이 성립하지 않는다.
     ⇒ 숫자가 늘어난 것을 보고 원인을 **귀속**하지 말고,
     **그 원인만이 만들 수 있는 숫자**를 고른다. `D27` 이 세 번 철회한 것도 같은 종류였다.

192. 🔑 **내가 심은 경고가 내 오독을 잡았다** (D31).
     `ResolveSummon` 을 쓰면서 *"대상이 웨이브 목록에 없으면 소환이 조용히 안 돈다"* 는 게
     걱정돼 `Debug.LogWarning` 을 넣어 뒀다. 그리고 **정확히 그 일이 일어났다** —
     `Boss1` 에 `Goblin` 이 없어서 소환이 통째로 죽어 있었는데, 나는 잡몹 수가 늘어난 걸 보고
     통과로 적은 뒤였다. 콘솔을 확인할 때 그 경고가 나를 멈춰 세웠다.
     ⇒ **"조용히 실패할 수 있다"고 느낀 자리에는 그 순간 경고를 남긴다.**
     그 경고의 첫 독자는 대개 **자기 자신**이다.

193. 🔴 **검증용으로 건 상태가 검증 대상을 가릴 수 있다** (D31).
     슬램 피해를 재려고 `GrantInvincibility(0f)` 로 무적을 풀려 했는데,
     구현이 `Mathf.Max(_invincibleTimer, seconds)` 라 **줄일 수가 없다**(`PlayerStats.cs:274`).
     앞서 편의로 걸어 둔 900초 무적이 그대로 남아 피해가 0 으로 나왔고,
     하마터면 "슬램이 안 맞는다"는 버그를 만들어 낼 뻔했다.
     ⇒ 검증을 위해 켠 것(무적·치트·상한 해제)은 **판정 직전에 실제로 꺼졌는지 확인**한다.
     끄는 API 가 없으면 **환경을 새로 만든다** — 이번엔 플레이 모드를 다시 켰다.

194. 🔑 **값을 정하는 일에서는 근거의 개수가 아니라 종류가 갈랐다** (D33 / C29).
     내가 `Upgrades.csv` 임시값을 잡을 때 쓴 근거는 **"메타 수입이 한 판 132G"** 하나였다.
     `C29` 는 같은 표를 **코드**(`GrantMetaGold` 도 `GoldGain` 을 곱한다 ·
     `Mathf.Max(1, raw-Armor)` 는 정액+바닥) · **기존 규칙**(`DESIGN_CLASSES` §7-B 의 레인저 기동) ·
     **마감**(포트폴리오라 보는 사람이 1~3판 한다) 셋으로 다시 잡았고, 내 값 4개를 정정했다.
     ⇒ 수치를 제안할 때 **"내가 본 축이 몇 개인가"** 를 먼저 센다.
     축이 하나면 그건 값이 아니라 **자리표시**다 — 그렇게 표시해서 넘긴다.

195. 🔑 **손으로 꽂은 배열은 CSV 손에 넘길 수 있다 — 특별 취급이 필요 없었다** (D33).
     `D30` 에서 `MetaProgressionManager.upgrades` 7칸을 씬에 직접 배선했다.
     그러자 CONTENT 가 항목 구성을 바꿀 때 **임포터가 그 배열을 못 건드려** 값만 바뀌고
     화면은 옛 목록 그대로가 될 참이었다.
     확인해 보니 `ImportComponentFields` 는 컴포넌트를 **타입 이름**으로, 필드를
     **`SerializedProperty` 이름**으로 찾고 ObjectReference 배열을 `|` 로 채운다 —
     `GameManager,classes` 와 **완전히 같은 경로**라 코드를 한 줄도 안 고치고 CSV 한 줄로 끝났다.
     ⇒ 씬에 손으로 꽂는 배열이 생기면 **그 자리에서 `SceneWiring.csv` 줄로 만들 수 있는지 본다.**
     나중에 넘기면 "값은 CSV, 목록은 씬"이라는 **반쪽 상태**가 오래 남는다.

196. 🔴 **"핵심 판정"이라고 쓴 것일수록 그 판정 방법이 옳은지 먼저 의심한다** (D31 → D34).
     `D31` 에서 나는 *"예고 회피가 이 작업의 전부"* 라고 써 놓고 그것을
     **순간이동으로** 확인했다. 순간이동은 *이미 최적 방향으로 무한 속도로 이동 중*이라는
     **가장 유리한 가정**이라, 실제로는 **폭발광이 반응 0초에도 못 피하는 값**이었는데
     내 시험은 통과로 나왔다. `C30` 이 `radius / windup` 한 줄로 그걸 책상에서 잡았다.
     ⇒ 판정을 설계할 때 **"이 방법이 통과시킬 수 있는 가장 나쁜 구현은 무엇인가"** 를 묻는다.
     내 방법은 **어떤 값을 넣어도 통과**시킬 수 있었다 — 그건 판정이 아니다.

197. 🔑 **"안 맞았다"를 주장하려면 "맞는다"를 먼저 보여야 한다** (D34).
     새 값으로 회피를 재기 전에 **옛 값으로 대조군을 먼저 돌려 HP 110 → 93 으로 맞는 것**을 확인했다.
     그게 없으면 "안 맞았다"는 **회피 성공**과 **테스트 고장**을 구별하지 못한다 —
     `D31` 에서 빠진 게 정확히 이 한 칸이었고, `D31` 의 무기 누적 오독도 같은 결의 실수였다.
     ⇒ 음성 결과(안 일어남)를 판정에 쓸 때는 **양성 대조군을 같은 시행에 붙인다.**

198. 🔴 **재현하지 못한 조건에서 나온 "정상"은 정상이 아니다** (D35).
     `D27` 이 남긴 무기 렉을 다시 재려고 적 815마리를 넣고 쟀더니 프레임이 **5.08 ms** 로 여유로웠고
     `ExpDrop` 도 **0개**였다. 여기서 *"D29 의 회수가 구슬 누적을 고쳤다"* 로 읽고 싶어진다.
     **틀렸다** — 플레이어가 무적으로 서 있고 무기가 1자루라 **적이 거의 안 죽었고,
     애초에 구슬이 생길 조건이 아니었다.** 나는 시나리오 A 를 재고 시나리오 B 의 답으로 쓸 뻔했다.
     ⇒ 측정 결과를 읽기 전에 **"내가 만든 조건이 문제의 조건과 같은가"** 를 먼저 확인한다.
     조건이 다르면 그 숫자는 **다른 질문의 답**이다.

199. 🔑 **에디터 플레이 모드의 프레임에는 게임이 아닌 것이 절반 들어 있다** (D35).
     프로파일러로 보니 `EditorLoop` 가 프레임의 **45 %**,
     `Profiler.FlushMemoryCounters` 가 10 % 였다. 게임 코드가 쓰는 시간보다 에디터 오버헤드가 크다.
     `D27` 이 겪은 *"세션이 다르면 같은 조건이 2배 흔들린다"* 도 이 성분이 흔들린 것일 수 있다.
     ⇒ 에디터에서 잰 절대 시간으로 **"게임이 느리다"를 판정하지 않는다.**
     비교는 같은 실행 안의 **비율**로 하고, 절대값이 필요하면 **빌드로 재야 한다.**

200. 🔴 **공유 애셋을 고치기 전에 "이걸 몇 개가 쓰나"를 센다** (D36).
     안내 문구 두 개에 외곽선을 주려고 `fontSharedMaterial` 을 고칠 뻔했다.
     그 머티리얼은 **텍스트 75개가 같이 쓴다** — HUD·상점·레벨업 카드가 전부 바뀌었을 것이다.
     파생본을 만들어 두 개에만 물리고, **나머지 75개가 그대로인지 숫자로 확인**했다.
     ⇒ 머티리얼·SO·프리팹처럼 **참조로 공유되는 것**을 고칠 때는
     고치기 전에 사용처를 세고, 고친 뒤에 **안 바뀐 쪽도 센다.**

201. 🔑 **UI 는 로그로 통과해도 캡처로 한 번 봐야 한다** (D36).
     TMP 머티리얼을 파생하고 `outlineWidth=0.22`, `underlaySoftness=0.25` 가 제대로 들어간 것을
     로그로 확인했다. **그런데 화면에서는 글자가 통짜 청록색 덩어리였다** —
     `ShaderUtilities.UpdateShaderRatios` 를 안 불러 SDF 계산이 무너진 것이다.
     같은 날 `StatsText` 도 좌표값은 맞는데 **카드 위에 겹쳐** 있었다.
     ⇒ **값이 맞는 것과 보이는 것이 맞는 것은 다른 판정이다.**
     UI 작업은 값 확인으로 끝내지 말고 반드시 `ScreenCapture` 로 눈으로 본다.

202. 🔴 **애매한 말로 쓴 설계는 애매한 코드가 된다** (D37 · E9).
     *"이번 층 동안 제단 없이 승급"* 이라고 써 놓고 해제를 `StageMapManager.AdvanceToNext` 에 넣었다.
     그런데 `EventManager.FinishEvent` 가 바로 그 함수를 부른다 — **수락하는 순간 켜지자마자 꺼졌다.**
     코드를 고치기 전에 보니 문제는 정의였다: **이벤트 노드 자체가 그 층의 내용물**이라
     "이번 층"에는 효과가 쓰일 시간이 애초에 없다.
     ⇒ 효과의 수명을 적을 때는 **"언제 켜지고 언제 꺼지는지"를 코드의 함수 이름으로** 적는다.
     "이번 층" 대신 **"다음 `ClearWave` 까지"** 라고 썼으면 설계 단계에서 걸렸다.

203. 🔑 **남의 소유 파일을 통째로 다시 쓸 때는 행 수를 세고 대조한다** (D37).
     `Events.csv` 에 열을 추가하려고 파일을 새로 쓰면서 CONTENT 가 쓴
     `Cursed Offering` 행을 **말없이 빠뜨렸다.** 커밋 전에 세어 보고 되살렸다.
     ⇒ 열 추가는 DEV 몫이지만 **행은 CONTENT 의 것**이다.
     전체를 다시 쓸 수밖에 없으면 **쓰기 전 행 수 · 쓴 뒤 행 수**를 둘 다 찍는다.

204. 🔴 **"참조가 없다"와 "화면에 안 보인다"는 다른 판정이다** (D38).
     CONTENT 가 애셋 3개를 *"아무 데도 안 쓰인다"* 며 정리 후보로 올렸는데
     **셋 다 guid 참조가 있었다.** 그런데 그중 `goblin.png` 는 참조가 있는데도 안 보이고
     (`EnemyBase.cs:124` 가 매 소환마다 덮어쓴다), `Exp_Orb.gif` 는 **참조도 있고 보인다.**
     ⇒ **앞쪽은 guid 로, 뒤쪽은 코드로** 봐야 한다. 한쪽만 보고 지우면
     **경험치 구슬이 통째로 사라질 뻔했다.**

205. 🔑 **"무엇을 만들 수 있나"가 아니라 "무엇이 없나"를 먼저 센다** (D38).
     내가 요청한 보스 그림은 CONTENT 순위에서 **7순위**였다. 이유가 정확했다 —
     적 6종·직업 10종·아이콘 34장은 이미 있는데 **UI 그래픽이 0장**이고,
     `Image` 60개 중 **59개가 단색**이다. 그리고 그 인물·몬스터를 채운 게 **나 자신**이라
     (`D26`·`D31`·`D32`) 나는 **제일 잘 채워진 칸을 한 번 더 채우자고** 한 셈이었다.
     ⇒ 다음 할 일을 고를 때는 **내가 잘하는 것**이 아니라 **세어 본 공백**에서 고른다.

206. 🔴 **적어 둔 선택지를 그대로 구현하면 안 된다 — 다시 검사한다** (D39 · B3).
     `BUGS.md` 에 C안을 *"새로 해금된 건물은 큐 맨 앞에 넣는다"* 라고 내가 적어 뒀는데,
     막상 짜려고 보니 **Village 를 못 세운 채 Farm 을 얻으면 Farm 이 Village 를 추월**했다.
     약속 하나를 지키려다 다른 약속을 깬다. **"맨 앞"이 아니라 "신규 구간의 끝"** 이 답이었다.
     ⇒ 며칠 전의 내가 한 줄로 적어 둔 안은 **요약이지 설계가 아니다.**

207. 🔴 **인덱스를 세는 필드를 두면 그 배열을 만지는 경로를 전부 센다** (D39).
     `_freshCount` 는 `_pendingQueue` 앞쪽 몇 개가 "신규"인지를 세는 장부다.
     큐를 건드리는 곳이 `PlaceNext`(**3곳**) · `LockBuilding`(임의 위치) · `ResetRunState`
     로 흩어져 있어서, 하나만 빠뜨려도 **조용히 어긋난다**(예외가 안 난다).
     ⇒ 맨 앞 제거는 `RemoveFront()` 헬퍼로 **통일**하고, 임의 위치 제거는
     **빠지는 개수를 먼저 세고** 뺐다. 그리고 `LockBuilding` 경로를 **따로 시험했다.**

208. 🔴 **"같은 크기"를 파일 이름으로 믿지 않는다 — rect 를 잰다** (D40).
     CONTENT 가 기존 탄환을 `50px` 로 보고 새 그림의 PPU 를 맞췄는데, **실제 rect 는 41px** 이었다.
     원본 파일명이 `icons8-총알-50` 이라 그렇게 읽힌 것이고, 임포트 때 여백이 트리밍돼 있었다.
     ⇒ **파일명·원본 해상도가 아니라 `Sprite.rect` 를 읽는다.** 그리고 크기가 달라졌을 때
     **"그래서 게임플레이가 바뀌나"는 따로 확인한다** — 이번엔 명중 판정이
     `CircleCollider2D 0.5` / `HitRadius 0.45` 고정이라 바뀌지 않았다.

209. 🔴 **9-slice 는 테두리 px 만으로 안 된다. 쓸 자리의 짧은 변을 같이 봐야 한다** (D40).
     `UI_Button` 테두리가 20px 인데 `RemoveButton` 은 **38×38** 이었다 —
     위아래 20+20 = 40px 라 **버튼보다 테두리가 크다.** 에러는 안 나고 모양만 뭉개진다.
     ⇒ `Image.pixelsPerUnitMultiplier` 로 **테두리 총합 ≤ 짧은 변의 60 %** 규칙을 세워 적용했다.
     애셋 규격을 받을 때는 **"어디에 쓸 것인가"** 를 같이 받아야 한다.

210. 🔴 **`[SerializeField]` 는 코드 기본값을 이긴다 — 코드를 고쳐도 씬이 옛것을 들고 있다** (D40 · B11).
     `ShopUI.npcDialogues` 의 코드 기본값은 **영문**인데 화면엔 한글 빈칸이 떴다.
     씬에 직렬화된 옛 값이 코드를 덮고 있었다.
     ⇒ 더 아픈 건 **`B4` 가 이미 이 병을 고쳤다고 닫혀 있었다**는 점이다.
     `D5` 는 씬의 `TextMeshProUGUI.text` **6곳**을 훑었는데, 이건 **`string[]` 필드**라 안 잡혔다.
     **"같은 병을 다 고쳤다"고 닫을 때는 훑은 방법이 무엇을 못 보는지도 같이 적는다.**

211. 🔴 **연출을 넣을 때는 "그때 게임이 돌고 있나"를 먼저 묻는다** (D41).
     승급·레벨업 파동을 `Time.deltaTime` 으로 짰는데, **그 둘이 뜨는 순간이 둘 다
     `timeScale = 0`** 이었다(`GameState.LevelUp`). 연출이 도는 게 아니라
     **얼어붙은 링이 화면에 그대로 붙어 있었다.** `unscaledDeltaTime` 으로 고쳤다.
     ⇒ 같은 이유로 **화면 흔들림은 정지 중엔 아예 안 난다**
     (`CameraController.cs:84` 가 `Time.deltaTime <= 0` 이면 조기 반환).
     연출을 붙이기 전에 **그 순간의 `timeScale`** 을 먼저 찍어 본다.

212. 🔴 **"떴다"와 "보인다"는 다른 판정이다** (D41).
     레벨업 파동은 코드도 배선도 스프라이트도 맞았고 `PulseFx` 도 실제로 생겼다.
     그런데 화면에는 **하나도 안 보였다** — `ScreenSpaceOverlay` 캔버스가
     그 프레임에 열리는 패널로 덮기 때문이다. **월드 스프라이트는 오버레이를 못 이긴다**
     (`sortingOrder` 로도 안 된다).
     ⇒ 캡처를 **패널 켠 것 / 끈 것 두 장**으로 찍어야 이게 갈린다. 한 장만 봤으면
     "안 뜬다"고 오진했을 것이고, 로그만 봤으면 "통과"라고 썼을 것이다.

213. 🔴 **생성 AI 에게 "투명 배경"을 말로 시키는 것은 안 통한다** (D42 · I-41 재발).
     프롬프트에 *"fully transparent background — no checkerboard, no grey fill"* 을
     **명시했는데도** 나온 알파가 **전부 255** 였다.
     ⇒ 생성 뒤에는 **항상 `RemoveImageBackground` 를 돌리고 알파 min 을 잰다.**
     프롬프트로 막았다고 생각하고 건너뛰면 안 된다. **미리보기로는 구분이 안 된다.**

214. 🔴 **"맨 뒤에 놓으라"는 지시는 그 자리에 뭐가 있는지 보고 따른다** (D42).
     CONTENT 가 배경을 `MainMenuPanel` 형제 순서 **0** 에 넣으라고 했는데,
     그 자리의 `DimBG` 가 **알파 1.00 불투명**이라 그대로 따랐으면 **배경이 통째로 가렸다.**
     순서 1(DimBG 바로 뒤)로 넣었다.
     ⇒ 배치 지시를 받으면 **이웃 오브젝트의 알파와 순서를 먼저 읽는다.**

215. 🔴 **`anchoredPosition.y` 는 중심이 아니다 — `pivot` 에 달렸다** (D42).
     형제에서 앵커를 복사했더니 `pivot` 이 `(0.5, 1.0)` 위쪽이라
     `y = -740` 이 **중심이 아니라 윗변**이 됐고, 300px 짜리가 패널 밖으로 나갔다.
     ⇒ `GetWorldCorners()` 로 **부모와 자식의 화면 y 범위를 같이 찍어** 확인한다.
     눈으로도 보이지만, 숫자로 보면 **얼마나 넘쳤는지**까지 나온다.

216. 🔴 **"전부 훑었다"고 말하기 전에 스캐너에 대조군을 넣는다** (D43 · B11).
     씬의 문자열을 훑어 *"한글 0건"* 을 얻었는데, 그 순회가 **`string[]` 원소를 보는지**를
     증명하지 않으면 그 0 은 **깨끗하다는 뜻이 아니라 못 봤다는 뜻**이다.
     그래서 방금 넣은 영문(`"For a price"`)을 같이 찾게 해서
     `npcDialogues.Array.data[2]` 로 잡히는 걸 확인한 뒤에 0건을 믿었다.
     ⇒ 그 순회가 실제로 **프리팹에 남아 있던 5건**을 잡아냈다.
     `B4` 가 *"씬 6곳 다 고쳤다"* 로 닫히고도 `B11` 을 남긴 이유가 정확히 이것이다.

217. 🔴 **씬을 고쳤다고 고친 게 아니다 — 프리팹 원본이 따로 있다** (D43).
     씬의 `ShopUI` 값을 영문으로 덮었더니 씬 스캔은 0건이 됐는데,
     그 오브젝트는 `UI Canvas.prefab` 의 **인스턴스**였고 원본에는 한글이 그대로였다.
     내가 고친 건 **오버라이드**라, 되돌리거나 다시 인스턴스화하면 **부활한다.**
     ⇒ 씬 컴포넌트를 고칠 때는 **그게 프리팹 인스턴스인지 먼저 보고, 맞으면 원본도 같이 고친다.**

218. 🔑 **덮이는 연출은 시점을 옮기는 게 답일 수 있다** (D43 · A안).
     레벨업 파동이 `ScreenSpaceOverlay` 패널에 가렸는데, 이걸 이기는 방법은 없다
     (`sortingOrder` 로도 안 된다). UI 로 다시 만드는 대신
     **패널이 닫히는 시점으로 옮겼더니** 마침 **플레이어가 다시 월드를 보는 순간**이라
     타협이 아니라 개선이 됐다. **그림·프리팹·배선은 하나도 안 버렸다.**

219. 🔴 **셸 명령이 실패해도 파이프라인은 답을 내놓는다 — 빈 변수는 "전부 일치"가 된다** (D44).
     `grep -oP` 가 *"-P supports only unibyte and UTF-8 locales"* 로 실패해 guid 가
     **빈 문자열**이 됐고, `grep -rl ""` 이 되어 **`Assets` 의 거의 모든 파일**이 참조로 잡혔다.
     에러 한 줄은 출력 맨 위에 파묻혀 있었고, 결과만 보면 *"엄청나게 많이 쓰인다"* 로 읽힌다 —
     **지우면 안 된다는 정반대 결론이다.**
     ⇒ 추출한 값을 **먼저 찍어 본다**(`echo "[$GUID]"`). 그리고 스캔에는 늘
     **잡혀야 하는 대조군**을 같이 넣는다 — `D43` 과 같은 규칙이다.

220. 🔴 **숫자를 올릴 때는 그 숫자를 붙잡고 있는 상한도 같이 본다** (D45).
     층별 소환량을 `entry.Count` 에만 곱하려 했는데, 넘치는 소환은 `WaitForSpawnSlot()` 이
     **`MaxAlive` 상한에서 붙잡는다** — 대기줄만 길어지고 **화면의 적 수는 하나도 안 는다.**
     "고쳤는데 아무 일도 안 일어나는" 종류의 실패고, 로그로는 소환이 정상으로 보인다.
     ⇒ 소환 수 · **동시 생존 상한** · 처치 목표 셋에 같이 걸었다.
     **곱할 값을 정할 때 "이 값을 제한하는 다른 값이 있나"를 먼저 센다.**

221. 🔴 **ScriptableObject 필드에 런타임 배율을 곱해 저장하면 애셋이 더러워진다** (D45).
     `WaveData.MaxAlive`/`KillTarget`/`Count` 는 전부 SO 필드다. 거기에 층 배율을 써 넣으면
     **애셋 파일이 바뀌고 다음 런까지 남는다** (`ItemData.CurrentLevel` 과 같은 함정인데,
     그쪽은 의도된 것이고 이쪽은 아니다).
     ⇒ 원본은 안 건드리고 **읽을 때마다 곱하는** 헬퍼로 갔고,
     시험 마지막에 **원본 값이 그대로인지 다시 찍어** 확인했다.

222. 🔑 **"안 걸려야 하는 자리"를 시험에 넣는다** (D45).
     층 배율을 5층·9층에서 재면 숫자는 당연히 커진다 — 배율이 **늘 켜져 있어도** 커진다.
     그래서 **1층(Layer 0)에서 정확히 x1.00** 이 나오는지를 같이 봤다.
     이게 없으면 ②③은 아무 구현이나 통과시킨다. `D34`·`D39`·`D43`·`D44` 와 같은 규칙이다 —
     **대조군은 "통과해야 하는 것"이 아니라 "떨어져야 하는 것"으로도 만든다.**

223. 🔴 **"판정 불가"를 "통과"로 세지 않는다** (D46).
     격자가 패널을 넘쳤는지 재면서 최하단 y 를 `float.MaxValue` 로 시작했는데,
     **칩이 하나도 없으면 그 초기값이 그대로 남아 "안 넘친다"로 통과**했다.
     실제로 그 로그를 한 번 찍었다 — `칩 0개 · ✅ 안 넘친다`.
     ⇒ 표본이 0 이면 **통과가 아니라 판정 불가**다. 시험 코드에도 그 분기를 넣는다.

224. 🟡 **엔진에 물어볼 수 있는 것을 grep 으로 단정하지 않는다** (D46).
     `Prefab_ItemChip` 의 `m_Script` guid 가 `Assets` 안에 없길래
     *"missing script 상태"* 라고 사용자에게 말했는데 **틀렸다** —
     그 스크립트는 **패키지 안**에 있었고(`LayoutElement`),
     `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` 는 **0** 을 돌려줬다.
     ⇒ 프로젝트 밖(패키지·빌트인)을 볼 수 없는 도구로 **"없다"** 를 결론짓지 않는다.

225. 🔑 **공유 자원을 만질 때는 먼저 쓰던 쪽의 계약을 찾아 따른다** (D46).
     `Time.timeScale` 은 일시정지·레벨업·상점·결과창·히트스톱이 전부 만진다.
     `GameManager.DoHitstop` 이 이미 3단 계약(웨이브 중에만 · 1 이 아니면 물러남 ·
     복구 때 재확인)을 세워 두고 그 이유까지 주석에 적어 놨길래 **그대로 따랐다.**
     ⇒ 그 덕에 시험 중 레벨업이 끼어들었을 때 **새 코드가 알아서 물러났다.**
     새 규칙을 만들기 전에 **같은 자원을 쓰는 기존 코드**를 먼저 읽는다.

226. 🔴 **`Image.color` 알파 1 이 "불투명"을 뜻하지 않는다 — 스프라이트가 반투명일 수 있다** (D47).
     크레딧 카드 뒤로 메뉴 글자가 읽혀서 알파를 1.0 으로 올렸는데 **그대로였다.**
     실측하니 `UI_Panel` 스프라이트의 **채움 픽셀 알파가 0.92** 였다 — 최종 불투명도는
     `color.a x sprite.a` 라 8 % 가 계속 통과한다.
     ⇒ **가리는 일은 카드가 아니라 `DimBG` 가 한다.** 그리고 "안 가려진다" 를 만나면
     **스프라이트 픽셀을 직접 재 본다** — 색 설정만 보면 원인이 안 나온다.

227. 🔴 **`SetActive(true)` 와 같은 프레임에 버튼을 누르면 아무 일도 안 일어난다** (D47).
     옵션 패널을 켜고 곧바로 `onClick.Invoke()` 를 불렀더니 **모든 값이 그대로**였다.
     `Start()` 가 아직 안 돌아 리스너가 안 붙어 있었기 때문이다.
     🔑 **헷갈린 건 라벨은 제대로 나왔다는 점** — `OnEnable` 은 즉시 돌아서
     "화면은 멀쩡한데 버튼만 죽은" 모습이 됐다.
     ⇒ 활성화와 조작 사이에 **프레임을 한 번 넘긴다.**

228. 🔴 **`maxTextureSize` 로 줄이면 Unity 가 PPU 도 같이 낮춘다** (D48).
     적 규약(512² · PPU 1024 = 0.5유닛)에 맞추려고 1024² 생성물을 512 로 줄였더니
     **PPU 가 512 로 따라 내려가** 월드 크기가 **0.5 가 아니라 1.0** 이 됐다.
     Unity 는 축소해도 **원본의 월드 크기를 보존**하기 때문이다.
     보스는 거기에 ×2 가 더 걸리므로 **잡몹의 4배**가 될 뻔했다.
     ⇒ 축소하지 말고 **PPU 를 원본 해상도에 맞춰 올린다**(1024 → PPU 2048).
     그리고 크기는 **혼자 보지 말고 기준 애셋과 나란히 잰다** — `512/512` 도 숫자만 보면 그럴듯하다.

229. 🔑 **정할 사람이 없을 때는 지어내지 말고 "이미 있는 사실"에서 끌어낸다** (D48).
     보스 컨셉은 CONTENT 몫이었는데 그쪽이 안 돌고 있었다. 세계관을 상상하는 대신
     **보스가 실제로 하는 일**을 근거로 삼았다 — 못 쫓아오니(2.38 vs 3.5~4.6) 버티고 서서
     내려찍는 실루엣, 고블린을 소환하니 강령술사, 예고 원이 붉으니 붉은 대역 금지,
     쓰이지 않은 색이 뼈색·초록.
     ⇒ 이러면 **틀려도 왜 그렇게 정했는지가 검증 가능**하고, 상대가 뒤집기도 쉽다.
     그리고 **바꾸기 쉬운 형태로 넣는다** — 이름은 CSV 한 칸이다.

230. 🔴 **캡처 두 장이 같은 프레임일 수 있다 — 해시를 먼저 본다** (D49).
     층을 바꾸고 찍은 두 스크린샷이 **파일 해시까지 동일**했다. MCP 명령 두 개가
     한 에디터 프레임에 몰려 처리돼 `CaptureScreenshot` 이 같은 프레임을 두 번 기록한 것이다.
     그대로 믿었으면 *"차이 0.00 → 기능이 안 먹는다"* 로 **오진**했을 것이다.
     ⇒ 비교 캡처 사이에는 **상태를 읽는 명령을 끼워 프레임을 벌리고**,
     비교 전에 **`md5sum` 으로 두 파일이 다른지부터 본다.**

231. 🔴 **"데이터가 바뀐다"와 "화면이 바뀐다"는 다른 판정이다** (D49).
     층별 타일 배치는 `Grass 38.4 %` → `Flagstone 38.6 %` 로 **완전히 뒤집혔는데**
     같은 자리 캡처의 평균 채널 차이는 **3.67/255(1.4 %)** 였다 — 사람 눈에는 같은 바닥이다.
     히스토그램만 봤으면 "성공"이라고 적었을 것이다.
     ⇒ 보이는 것을 바꾸는 작업은 **픽셀로 재고 통과선을 숫자로 정한다**
     (여기서는 밝기 차 ≥ 0.04 · 채널 차 ≥ 15/255).

**16~17 과정에서 함께 처리한 것**

| 항목 | 내용 |
|---|---|
| `PlayerSettings.runInBackground` | `false` → **`true`**. 에디터가 포커스를 잃어도 돌아야 MCP 원격 검증이 가능 |
| 💰 이모지 제거 | `ShopUI` / `ShopCardUI`의 `$"💰 {n}"` → `$"{n} G"`. U+1F4B0가 Pretendard SDF와 폴백 어디에도 없어 TMP 경고 + `□` 발생. 이제 `MainMenuUI`/`StageMapUI`와 표기 통일 |
| CS0414 경고 2건 | `StageMapManager.weightEvent` 미사용 → `RollStageType`에서 실제로 사용(가중치 합 1.0이라 동작 동일), `WaveManager._elapsedTime` 미사용 필드 삭제(`TotalElapsedTime`이 실사용) |

### 6단계 — Play 모드 전체 루프 검증  ✅ 18/18 PASS

임시 `LoopSmokeTest` MonoBehaviour를 씬에 붙여 코루틴으로 루프를 자동 주행시키고
`[SMOKE]` 태그 로그를 콘솔에서 읽는 방식으로 검증함. **검증 후 스크립트·오브젝트 모두 삭제 완료.**

| 단계 | 검증 내용 | 결과 |
|---|---|---|
| 1 | 콜드 스타트 → `MainMenu`, `MainMenuPanel` 활성 | ✅ |
| 2 | Start 버튼 → `StageMap`, 맵 노드 생성 (Content 자식 64 = 노드 21 + 라인 43) | ✅ |
| 3 | 노드 선택 → `Wave`, HUD 활성 / StageMapPanel 숨김, **2초 후 적 3마리 생존** | ✅ |
| 4 | `OnWaveCleared` → `StageClearPanel` 활성, `timeScale = 0` | ✅ |
| 5 | Continue → `StageMap` 복귀, `timeScale = 1` | ✅ |
| 6 | `OnPlayerDied` → `GameOver`, `RunEndPanel` 활성 / HUD 숨김, `[Meta] Saved to …/save.json` | ✅ |

**아직 루프에서 미검증 (스모크 테스트가 의도적으로 건너뜀)**

Shop 노드 · Event 노드 · 실제 Boss/Victory 경로 ·
Retry 버튼의 `GameManager.ReloadScene(true)` 씬 리로드

> LevelUp 패널은 **6차에서 검증 완료** (I-24 → 2-8). 카드 3장 표시 + 리롤 1회 제한 확인.

### 검증 방법 메모 (다음 세션용 MCP 함정)

| 함정 | 대응 |
|---|---|
| `Unity_ReadConsole`의 기본 `Types`(`[2,1,0]`)가 대부분의 엔트리를 **조용히 걸러냄** | 항상 `Types: ["All"]` 를 명시 |
| 에디터가 **포커스를 잃으면 프레임이 진행되지 않음**. 코루틴 테스트가 멈춘 것처럼 보임 | `runInBackground = true` + `Play` 후 `GetState` / `ReadConsole` 호출로 에디터를 펌프 |
| `Unity_RunCommand` 컴파일이 **도메인 리로드를 유발**해 static이 리셋되고 Play 모드가 종료됨 | 플레이 모드 조사는 RunCommand 대화형 대신 **임시 씬 MonoBehaviour + 로그** 방식 사용 |
| `System.Reflection`은 RunCommand **금지 네임스페이스** | `Unity_ManageGameObject get_component` + `include_non_public_serialized: true` 로 대체 |
| RunCommand 내 `AssetDatabase.DeleteAsset` / `Refresh` → `User interactions are not supported` | 로드된 애셋을 제자리에서 수정 + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()` |
| `Unity not detected (no fresh discovery files found)` | 거의 항상 도메인 리로드 중. `sleep 15~20` 후 `ManageEditor(GetState)`로 `IsCompiling=false` 확인 후 재시도 |

> **UI 문자열은 영문 유지** (사용자 결정). 폰트는 한글을 지원하지만,
> C단계에서 만든 패널의 라벨은 영문(`SHOP`/`REROLL`/`CLOSE` 등) 그대로 둠.
> 스크립트에 하드코딩된 한글(`ShopUI.npcDialogues`, `StageClearUI`)은 이제 정상 렌더링됨.

### E-가를 적용하지 않은 이유

당초 결정(E)은 (나) 스크립트 이관 **＋** (가) `Active Input Handling = Both` 둘 다였으나,
검증 결과 (가)가 **불필요**해서 적용하지 않음. 에디터 재시작(= MCP 릴레이 끊김) 비용만 발생함.

| 확인 항목 | 결과 |
|---|---|
| 레거시 `Input.GetAxis/GetKey/GetMouse/mousePosition` 잔존 | **0건** (E-나에서 전량 이관 완료) |
| `ProjectSettings.activeInputHandler` | `1` (Input System 전용) |
| `EventSystem`의 입력 모듈 | **`InputSystemUIInputModule`** ✅ (레거시 `StandaloneInputModule`이면 UI 클릭이 전부 죽었을 것) |

> `Both`로 바꾸면 레거시 입력 백엔드가 추가로 올라가지만 이를 쓰는 코드가 없음.
> 나중에 레거시 `Input`을 쓰는 외부 에셋을 도입하면 그때 켜면 됨.

---

## 5. 남은 작업

**→ [`TODO.md`](TODO.md) 로 분리됨.** (2026-08-26 2차 기준)

문서 3종의 역할:

| 문서 | 담는 것 |
|---|---|
| `SETUP_STATUS.md` (이 문서) | **완료된 작업의 이력** |
| [`TODO.md`](TODO.md) | **아직 안 된 것** |
| [`BALANCE.md`](BALANCE.md) | **수치를 고치는 법** (CSV 파이프라인) |

`TODO.md` 요약:

| 구분 | 내용 |
|---|---|
| 🔴 재검증 필요 | I-14~I-18 은 **코드 수정만 끝났다.** 스모크 테스트가 건너뛴 경로라 실제 플레이 확인 필요 |
| 🔴 결정 필요 | **런 골드 vs 메타 골드** 구조. 지금 둘이 `MetaProgression.Currency` 하나를 공유해 10층 런 수입 ≈1,240G 대 상점가 5~12G |
| 🔴 구조적 공백 | **층별 난이도 스케일링 없음** — `StartWave()` 가 풀에서 무작위로 뽑아 1층에 Normal3 이 나올 수 있음 |
| 🔴 미조정 수치 | I-21 로 실효 스탯이 절반이 된 뒤 **아무도 재조정하지 않았다.** 직업 `Bonus*` 도 감으로 넣은 자리표시값 |
| ⚠️ 미연결 시스템 | `MetaScreen` 전환 코드 0 · `UpgradeDefinition` 애셋 0 · 캐릭터/스킨 해금 호출자 0 · **직업 해금 흐름 없음**(`UnlockedByDefault` 만 봄) |
| ⚠️ 콘텐츠 잔여 | ~~적 스프라이트 1장 공유 · 무기 아이콘 재사용 · 직업 일러스트 0~~ → **I-25 로 26종 생성·배선 완료.** ~~승급 직업 그림 0 · 진화 무기 아이콘 재사용~~ → **I-57 로 17장 생성·배선 완료.** ~~적 걷기 시트 6장 미배선~~ → **I-58 로 배선 완료.** 남은 것: 적 **공격·사망 모션** 없음 |
| ⚠️ 빈 슬롯 | 배치 커서 · HUD 표정/레벨업 연출 (전부 null 가드 — I-24 로 `?.` 가 아닌 `!= null` 로 교정) |
| 미검증 경로 | Shop · Event · Boss/Victory · Retry · 피격/사망 · 건물 배치 · **신규 무기 5종 실사격** |

---

## 6. 참고

- 검증 스크립트: `Temp/scan.py`(프리팹 인스턴스 오버라이드 덤프), `Temp/tree.py`(하이어라키 트리).
  `Temp/`는 Unity가 지우는 폴더이므로 임시용. (없으면 다시 만들면 됨)
- 이 문서는 `Assets/` 바깥에 두어 Unity가 임포트하지 않도록 함.
- **밸런스 수치를 인스펙터에서 직접 고치지 말 것.** `Assets/Game/Balance/*.csv` 가 원본이고,
  `Game/Balance/Import CSV -> ScriptableObjects` 를 실행하면 인스펙터 값이 **덮어써진다.**
  반대 방향(`Export ScriptableObjects -> CSV`)도 있으니 실수로 SO를 고쳤다면 Export로 회수할 것.
- **ScriptableObject 클래스는 반드시 클래스명과 같은 파일에 둘 것** (I-19).
  다른 파일에 있으면 새로 만든 `.asset` 의 `m_Script` 가 `0`으로 기록되고,
  이미 만들어진 애셋은 **재임포트로도 복구되지 않아 삭제 후 재생성**해야 한다.
