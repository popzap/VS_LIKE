# VS_LIKE — 작업 규칙

Unity 6.3 LTS (6000.3.8f1) · URP · 2D 뱀서라이크. 단일 씬 `Assets/Scenes/SampleScene.unity`.
Unity MCP 릴레이로 **켜져 있는 에디터**를 직접 조작한다.
원격 저장소: **`popzap/VS_LIKE` (Private)** · 기본 브랜치 `main`.

---

## 1. 문서 자동 정리 (매 작업마다, 요청 없이도 수행)

`Docs/` 문서는 **작업의 산출물이 아니라 작업의 일부**다.
아래 3종은 사용자가 시키지 않아도 **항상** 갱신한다.

| 문서 | 담는 것 |
|---|---|
| `Docs/SETUP_STATUS.md` | **완료된 작업의 이력.** 이슈 번호(I-n), 해결 상세, 검증 로그, 단계별 기록 |
| `Docs/TODO.md` | **아직 안 된 것만.** 미검증 경로, 결정 필요 항목, 콘텐츠 공백, 권장 진행 순서 |
| `Docs/BALANCE.md` | **수치를 고치는 법.** CSV 파이프라인 사용법 + 현재 수치의 근거 |

`Docs/ROADMAP.md` 는 성격이 다르다 — **장르 완성도 갭 분석**이다
("뱀서라이크로서 아직 없는 것": 오디오·적 행동·진화·메타·게임 필).
매번 고칠 필요는 없고, **큰 기능이 하나 끝났을 때** 해당 항목을 지우고 우선순위를 다시 본다.
버그·미검증처럼 **지금 굴러가는 일**은 `ROADMAP.md` 가 아니라 `TODO.md` 에 적는다.

`Docs/TUNING.md` 도 성격이 다르다 — **사람이 직접 플레이해야만 판정되는 것**을 모은다.
**방침(2026-08-29): 수치는 지금 잡지 않는다. 기능을 먼저 완성하고 플레이하며 조절한다.**
경계는 이렇다:

- **"동작하나?"** (답이 예/아니오) → `TODO.md` §1
- **"느낌이 맞나?"** (답이 숫자) → `TUNING.md`
- **"어떻게 고치나?"** (파이프라인·근거) → `BALANCE.md`

기능을 새로 넣었는데 **로그로는 판정할 수 없는 값**을 남겼다면 `TUNING.md` 에 적는다
(예: 볼륨·넉백 세기·소환 수·상한). 그냥 "체감 미확인"이라고만 쓰지 말고
**무엇을 보는지 · 이상하면 어느 파일의 무엇을 얼마나 고치는지**까지 같이 적을 것.

### 작업을 끝냈을 때

1. `SETUP_STATUS.md` — 새 `##` 절에 **원인 → 변경한 파일 표 → 검증 로그** 를 적고,
   `2-1. 이슈 목록` 표에 이슈 행 추가, 하단 번호 목록에 `n차` 항목 추가
2. `TODO.md` — 해결된 항목 **제거**, §0 현황표 갱신
3. `BALANCE.md` — 수치·CSV 열이 바뀌었으면 근거 표도 같이
4. **커밋한다** (§5). 문서 갱신까지 끝난 뒤 **코드·애셋·문서를 한 커밋에** 담는다

### 작업 중 새 문제를 발견했을 때

**임의로 고치지 말고** 먼저 적고 사용자에게 알린다.
(예외: 지금 하는 작업이 그 버그 때문에 검증 불가능한 경우 → 알리고 허락을 받는다)

어디에 적는지는 **성격**으로 가른다:

| 무엇 | 어디 |
|---|---|
| **깨진 것** — 예외·null·동작 안 함 (답이 예/아니오) | [`Docs/Parallel/BUGS.md`](Docs/Parallel/BUGS.md) |
| **아직 없는 것** — 미구현·결정 대기 | `TODO.md` |
| **느낌이 안 맞는 것** — 세다/약하다/답답하다 (답이 숫자) | `TUNING.md` |

### 무언가를 "나중에" 로 미뤘을 때

미룬 이유와 재개 조건을 `TODO.md` 에 남긴다. 특히:
- **새 UI가 필요해 미룬 것** → §2 표
- **애셋(스프라이트/사운드/일러스트)이 없어 비워 둔 슬롯** → §3·§4 표
- **감으로 넣은 자리표시 수치** → §3 스탯 재조정 절

### 이슈 번호

**세션 첫 글자 + 일련번호**를 쓴다 — `D1`(DEV) · `C1`(CONTENT), 버그는 공용 `B1`.
다음 번호는 [`Docs/Parallel/BOARD.md`](Docs/Parallel/BOARD.md) §3 에서 **선점하고 즉시 올린다.**

> 과거 이력의 `I-1`~`I-61` 은 그대로 둔다. 병렬 작업 규칙 전문은
> [`Docs/Parallel/SESSION_PROMPT.md`](Docs/Parallel/SESSION_PROMPT.md).

---

## 2. 밸런스 수치

**`Assets/Game/Balance/*.csv` 가 원본이다.** ScriptableObject 는 거기서 생성되는 산출물.

- 수치를 바꾸려면 CSV → `Game/Balance/Import CSV -> ScriptableObjects` 메뉴 실행
- **인스펙터에서 SO 를 직접 고치지 말 것.** 다음 Import 때 덮어써진다
- 실수로 고쳤으면 `Game/Balance/Export ScriptableObjects -> CSV` 로 회수
- 새 아이템/웨이브/직업은 `SceneWiring.csv` 에도 경로를 추가해야 게임에 등장한다
- **C# 필드를 지우면 `Economy.csv` 의 그 행도 같이 지울 것** — 임포터가 잡을 대상이 없어진다

---

## 3. 코드

- **ScriptableObject 클래스는 클래스명과 같은 파일에 단독으로 둘 것** (I-19).
  다른 파일에 있으면 새 `.asset` 의 `m_Script` 가 `0`으로 기록되고 **재임포트로 복구되지 않는다.**
- **직렬화된 Unity Object 필드에 `?.` 를 쓰지 말 것. `if (field != null)` 로 검사한다** (I-24).
  미할당 필드는 C# 기준 `null` 이 아니라 접근 시 `UnassignedReferenceException` 을 던지는
  **"가짜 null"** 이라 `?.` 가 그냥 통과시킨다. 그 예외가 이벤트 핸들러 밖으로 전파되면
  **호출 사슬 전체가 조용히 끊긴다** (실제로 레벨업 패널이 통째로 안 떴다).
- **보너스로 쓰는 `StatBlock` 은 반드시 `StatBlock.Zero()`** 로 만들 것 (I-21).
  기본 생성자의 필드 초기값은 *기본 스탯*이라 합산 시 2배가 된다.
- **`GetComponent<T>() ?? AddComponent<T>()` 는 동작하지 않는다.** 위와 같은 "가짜 null" 함정이다.
  `var c = go.GetComponent<T>(); if (c == null) c = go.AddComponent<T>();` 로 쓸 것.
- **`GameManager.Instance.XxxMgr` 를 `Awake()` 에서 캐시하지 말 것** (I-8, I-38).
  그 참조들은 `GameManager.Start()` 에서 채워지는데 **모든 `Awake` 는 모든 `Start` 보다 먼저** 돈다.
  null 이 잡혀도 예외가 안 나고 **기능만 조용히 죽는다** (Z 키 건물 설치가 통째로 안 먹었다).
  `Start()` 로 미루거나, 첫 사용 시점에 지연 조회하는 프로퍼티로 감쌀 것.
- 🔴 **키 입력은 `UnityEngine.Input` 이 아니라 `Keyboard.current` 로 읽는다** (D83).
  이 프로젝트는 `ProjectSettings.activeInputHandler = 1`, 즉 **Input System 패키지 전용**이다.
  그 모드에서 옛 `Input.GetKeyDown` 은 **호출될 때마다 `InvalidOperationException` 을 던진다.**
  `Update()` 에 넣으면 **매 프레임** 터져 콘솔이 잠기고 **에디터가 통째로 멎는다** — 실제로 그랬다.
  ```csharp
  var kb = UnityEngine.InputSystem.Keyboard.current;
  if (kb == null) return;                                  // 키보드가 없을 수 있다
  if (kb[UnityEngine.InputSystem.Key.F7].wasPressedThisFrame) { ... }
  ```
  `DevPanel`·`LevelUpManager` 가 이미 이 방식을 쓴다. **새로 쓰기 전에 기존 코드를 먼저 볼 것.**
- UI에 표시되는 **문자열은 영문**으로 쓴다. 주석·문서는 한국어.

---

## 4. 진행 방식

- **단계별로 진행하고 각 단계마다 짧게 보고**한다. 한 번에 몰아서 하지 않는다
- **고치기 전에 실제 씬/애셋 상태를 먼저 읽어 검증**한다. 문서를 믿고 바로 수정하지 않는다
- 지시 내용과 실제 상태가 어긋나면 **임의로 진행하지 말고 질문**한다

### Unity MCP 함정

| 증상 | 대응 |
|---|---|
| `Unity_ReadConsole` 이 로그를 놓침 | `Types: ["All"]` 를 항상 넘긴다. `FilterText` 는 신뢰하지 말 것 |
| 메뉴 실행 실패 | `MenuPath` 에 `->` 를 **문자 그대로** 넘긴다 (`&gt;` 로 이스케이프되면 실패) |
| `"Unity not detected"` | 도메인 리로드 중. `sleep 15~20` 후 `ManageEditor(GetState)` |
| C# 수정 반영 | 파일 저장 → `Assets/Refresh` → `sleep 15` → 콘솔 확인 → `File/Save`. 🔴 **`Assets/Refresh` 는 재컴파일을 보장하지 않는다** — 아래 두 줄을 같이 볼 것 (D27) |
| 🔴 **에러 0 인데 새 필드가 없다** | `Assets/Refresh` 가 **컴파일을 건너뛴 것**이다 (D27). `CompilationPipeline.RequestScriptCompilation()` 을 직접 부르고, 반영 여부는 콘솔이 아니라 **로드된 어셈블리에서 새 심볼을 조회해** 확인한다. 스크립트에 `public const int Version` 을 두고 매번 조회하는 게 확실하다 — 그 가드가 실제로 **구버전 코드로 신버전 결과를 적을 뻔한 것**을 막았다 |
| 🔴 **컴파일 에러가 `Error` 로 안 잡힌다** | Unity 컴파일 에러는 `Log` 타입으로 온다 (D27). `Types:["Error"]` 는 **0건**인데 `Types:["All"]` + `FilterText:"error CS"` 로 보면 **11건**이었다. ⇒ 컴파일 확인은 반드시 `["All"]` 로 한다 |
| `RunCommand` 에서 `System.Reflection` / `AssetDatabase.DeleteAsset` | 금지됨. 다른 경로를 찾을 것 |
| **플레이 중에 고친 C# 이 안 먹음** | 컴파일이 플레이 종료까지 밀린다. **플레이 모드를 껐다 켜고** 다시 검증할 것 |
| `Unity_Camera_Capture` 를 인자 없이 호출 | 게임 카메라가 아니라 **씬 뷰**가 찍힌다. `Camera.main.gameObject.GetInstanceID()` 를 넘길 것 |
| CSV Import 가 `InvalidOperationException` | `MarkSceneDirty` 는 **플레이 모드에서 못 쓴다.** `ManageEditor(Stop)` 먼저. 일시정지(Paused)도 플레이 모드다 |
| AI 가 만든 스프라이트의 "투명 배경" | **가짜다** (I-41). 알파가 전부 255 이고 체커 무늬가 RGB 에 그려져 있다. 알파 채널 min/max 로 확인 후 재생성할 것 |

런타임 검증은 **임시 MonoBehaviour + `[TAG]` Debug.Log** 패턴을 쓰고,
확인이 끝나면 **스크립트와 씬 오브젝트를 반드시 지운다.**

---

## 5. 버전 관리

한 작업(= `SETUP_STATUS.md` 의 `n차` 한 절)이 **커밋 하나**다.
코드·애셋·문서를 쪼개지 말고 **같이** 담는다 — 나중에 "왜 이 수치가 이렇게 됐나"를
커밋 하나만 보고 알 수 있어야 한다.

```
<한 줄 요약>

<원인 / 배경 — 무엇이 문제였고 왜 이렇게 고쳤는지>

<이슈 번호가 있으면 본문에 적는다 (U3 / C2 / D4 / B1)>
```

- **커밋은 사용자가 시키지 않아도 작업 끝에 한다.** 단, **푸시는 물어보고** 한다
- `git commit` 전에 **`git status` 로 의도치 않은 파일이 섞였는지 확인**할 것.
  특히 Unity 가 건드린 `ProjectSettings/*.asset`, `Assets/**/*.meta` 가 조용히 딸려 온다
- **`--no-verify` / `--amend` / `push --force` 금지.** 되돌릴 수 없다

### 커밋하지 않는 것 (`.gitignore`)

| 대상 | 이유 |
|---|---|
| `Library/` `Temp/` `Logs/` `UserSettings/` | 에디터가 재생성한다 |
| `*.csproj` `*.slnx` | IDE 가 재생성한다 |
| `GeneratedAssets/` (약 100MB) | Unity AI 생성 캐시. **결과물은 `Assets/Game/Sprites` 에 따로 들어가 있다** |

> 줄바꿈·바이너리 취급은 `.gitattributes` 가 잡는다. `.unity`/`.prefab`/`.asset` 은
> **텍스트(YAML)** 라 diff 가 보인다. `.png`/`.ttf` 는 `binary` 로 못박아 두었다 —
> 안 그러면 줄바꿈 정규화가 파일을 망가뜨린다.

### 애셋을 지울 때

이제 **커밋된 것은 되돌릴 수 있다.** 다만 히스토리에는 영구히 남으므로
**큰 바이너리(수십 MB)를 넣었다 지우는 건 피할 것.** 넣기 전에 판단한다.
