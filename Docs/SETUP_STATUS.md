# VS_LIKE — 프로젝트 현황

> Unity 6.3 LTS (6000.3.8f1) / 2D 뱀서라이크
>
> **2026-08-26 — 작업 전제 변경 (사용자 지시)**
> 기존의 「C# 스크립트는 완성 단계」라는 전제와 「요청 없이 코드 건드리지 말 것」 규칙이 **해제됨**.
> 이제 게임 완성을 위해 C# 스크립트 신규 작성·수정이 허용된다.
>
> **최종 갱신:** 2026-08-30 (37차 — 검과 독 장판이 남의 소리를 그만 빌린다, D14 / C17·C18)
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
> 원격 동기화: `popzap/VS_LIKE` `main` @ **`b68a39d`** (2026-08-30, 28차 D5 까지 푸시됨)
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
| **B1** | **엘리트 외곽선이 스프라이트와 어긋난다** — `SpriteOutline.shader` 의 `SampleAlpha` 가 `uv 0~1` 로 아틀라스 밖을 막지만, **시트에서 잘라 온 스프라이트의 `uv` 는 텍스처 전체 기준**이라 검사가 절대 안 걸리고 **옆 칸을 빨아들인다.** I-58 에서 낱장 png → 시트로 바꾼 순간 전제가 깨졌다 | 🔴 **미해결** — 원인 확정(25차). 반경 `14/512 = 28텍셀` vs Wolf 여백 14px · Demon 0px. **로직 버그인데 파일은 CONTENT 소유**다. 담당은 **DEV 로 확정**(사용자: *"코드 쓰는거면 너가해"*) → `Parallel/BUGS.md` |
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
