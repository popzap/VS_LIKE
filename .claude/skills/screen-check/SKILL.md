---
name: screen-check
description: 게임 화면을 직접 캡처해서 눈으로 판정한다. 사용자에게 스크린샷을 요청하는 대신 쓴다. UI 레이아웃·겹침·글자 크기·가시성·색 대비처럼 "코드로는 통과인데 화면에서 틀린" 것을 잡을 때. 트리거 — 화면 확인, 스크린샷 찍어, UI 검증, 겹치는지 봐, 잘 보이는지 확인, 레이아웃 확인, 이거 실제로 어떻게 보여.
---

# screen-check — 화면을 내 눈으로 판정한다

## 왜 이게 필요한가

이 프로젝트에서 **UI 화면은 원리적으로 Unity MCP 로 못 찍혔다.**

- `UI Canvas` 는 `ScreenSpaceOverlay` 다. `Unity_Camera_Capture` 는 카메라를 렌더 타깃에
  그리는 방식이라 **오버레이가 한 픽셀도 안 나온다** (기록 #120).
- `cam.Render()` / `ScreenCapture.CaptureScreenshot` 은 **에디터에 포커스가 없어 프레임이 안 돌아**
  같은 그림만 반복해서 뱉는다 (파일 크기가 전부 같아서 알았다).

그래서 `SETUP_STATUS.md` 기록 #114 는 이렇게 끝난다 — *"화면은 사용자 눈에 맡긴다"*.
**이 스킬이 그 줄을 은퇴시킨다.**

그리고 이건 취향 문제가 아니다. 화면을 못 봐서 **틀린 보고를 할 뻔한 게 최소 세 번**이다:

| | 숫자는 통과했는데 |
|---|---|
| `D56` | 대비가 3.1배 좋아졌다고 보고할 뻔했다. 실제로는 `Image.color` 가 **곱셈**이라 실루엣이 안 됐다 |
| `D86` | `activeSelf == true` 라 "화살표 뜬다"고 판정했다. 게임뷰에서 **19px** 이라 안 보였다 |
| `D85` | 숫자가 통과한 것을 **사용자가 화면으로 잡았다** (세 번째) |

---

## 두 가지 캡처 경로

**A 를 기본으로 쓴다.** A 가 막히면 B 로 간다. **둘은 보는 것이 다르다.**

| | A · 데스크톱 캡처 | B · `renderMode` 치환 |
|---|---|---|
| 무엇을 찍나 | **화면에 합성된 진짜 픽셀** | 카메라가 렌더한 그림 |
| 오버레이 UI | ✅ 보인다 | ✅ 보인다 (치환했으므로) |
| **게임뷰 실제 해상도** | ✅ 그대로 | ✅ **RT 를 게임뷰 크기로 물리면** 그대로 (§B) |
| **키를 눌러야 열리는 화면** | ✅ 진짜로 누른다 | ❌ **못 연다** (§B 마지막) |
| 사용자 승인 | 세션마다 1회 필요 | 불필요 |
| 에디터 크롬(탭·인스펙터) | 같이 찍힌다 | 안 찍힌다 |

> 🔑 **B 의 약점은 해상도가 아니라 입력이다.** 해상도는 `RenderTexture` 를 게임뷰 크기로
> 물려서 닫았다(§B). 남은 진짜 차이는 **`private` 메서드로만 열리는 화면**(TAB 정보창 등)이고,
> 그건 **A 만 열 수 있다.**

---

## `RunCommand` 로 코드를 넣을 때 — **여기서 세 번 막혔다**

컴파일 실패는 왕복 한 번씩을 그냥 버린다. 쓰기 전에 이 셋을 확인할 것.

| 함정 | 증상 | 대응 |
|---|---|---|
| `result.Log` 인자 제한 | `{3}` 이후가 안 채워진다 (기록 #127) | **인자를 3개까지만.** 넘으면 `"a=" + x + " b=" + y` 로 **문자열을 직접 만들어** 하나만 넘긴다 |
| `Image` 이름 충돌 | `CS0118: 'Image' is a namespace but is used like a type` | `using UnityEngine.UI` 로는 안 풀린다 |
| 별칭도 충돌 | `using UI = UnityEngine.UI;` → `CS0234: ... in 'Unity.AI.Assistant.UI'` | 🔑 내 코드는 `Unity.AI.Assistant.…` 안에 감싸여 컴파일된다. **`global::UnityEngine.UI.Image` 로 못박을 것** |

```csharp
var img = t.GetComponent<global::UnityEngine.UI.Image>();   // 이렇게만 통한다
result.Log("chip " + w + "x" + h + " | icon " + iw + " a=" + a);   // 인자 1개
```

---

## 경로 A — 데스크톱 캡처 (기본)

### A-1. 권한 요청

```
mcp__computer-use__request_access(apps: ["Unity"], reason: "<무엇을 확인하는지>")
```

- **세션마다 한 번** 사용자가 승인해야 한다. 거부되면 되묻지 말고 **B 로 간다.**
- 승인 안 된 창은 스크린샷에서 **단색 사각형으로 가려진다** (`screenshotFiltering: "mask"`).
  Unity 창이 통째로 회색이면 권한이 없는 것이다.
- ⚠️ **Unity 가 "IDE" 등급으로 잡히면 클릭만 되고 타이핑이 막힌다.**
  첫 실행 때 `list_granted_applications` 로 tier 를 확인해 이 파일에 적어 둘 것.
  → 현재 확인된 값: **미확인** (첫 승인 시 여기 갱신)

### A-2. 화면 상태를 만든다 — **키를 누르지 말고 로직을 부른다**

기록 #114: **MCP 로 주입한 키 입력은 게임에 도달하지 않는다.**
`InputSystem.QueueStateEvent` + `InputSystem.Update()` 는 전이가 그 자리에서 소비돼
다음 `MonoBehaviour.Update` 에 `wasPressedThisFrame` 이 안 남는다.

⇒ **화면 진입은 `Unity_RunCommand` 로 메서드를 직접 호출한다.**

```csharp
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (!EditorApplication.isPlaying) { result.LogError("play mode required"); return; }

        // 예: 일시정지 메뉴는 GameState.Wave 에서만 열린다
        var gm = GameManager.Instance;
        gm.ChangeState(GameState.Wave);
        Object.FindAnyObjectByType<PauseMenuUI>().Open();

        result.Log("opened");
    }
}
```

### A-3. 상태를 얼어붙게 한다 — **이걸 빼면 시험이 오염된다**

기록 #122: **MCP 호출 사이에는 프레임이 간다.** `Open()` 을 부르고 다음 호출에서 봤더니
이미 `LevelUp` 으로 넘어가 패널이 닫혀 있었다.

`D85` 는 화살표를 재는 데 **네 번 실패했고 전부 시험 오염이었다**
(보스가 다가옴 · 물리로 밀림 · `GameState=LevelUp` · 레벨업 큐).
세 개를 다 고정하고서야 쟀다:

```csharp
Physics2D.simulationMode = SimulationMode2D.Script;  // 물리 정지
Time.timeScale = 0f;                                  // 로직 정지
GameManager.Instance.ChangeState(GameState.Wave);     // 상태 고정
```

캡처가 끝나면 **반드시 되돌린다** (`SimulationMode2D.FixedUpdate` · `timeScale 1`).

에디터 자체를 멈추려면 `Unity_ManageEditor(Action: "Pause")` — **멈춰도 캡처는 된다.**

### A-4. 찍는다

```
mcp__computer-use__screenshot(scale: 1)
```

작은 글자·테두리는 **`zoom` 으로 다시 본다.** 아끼지 말 것 —
`D86` 의 "빈 박스가 흰 쿼드였다"는 확대해야 보였다.

```
mcp__computer-use__zoom(region: [x0, y0, x1, y1])
```

여러 화면을 순회할 때는 `computer_batch` 로 묶는다 (왕복 1회).
사용자에게 보여줄 장면만 `save_to_disk: true` → `SendUserFile` 로 보낸다.

---

## 경로 B — `renderMode` 치환 (권한 없이)

**플레이 모드 한정으로** 오버레이를 카메라 공간으로 옮겨 찍는다.
플레이를 끄면 되돌아가므로 **디스크에는 안 남는다.**

```csharp
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // 🔴 씬을 건드리는 RunCommand 의 필수 가드 (기록 #118)
        //    MarkSceneDirty/SaveScene 은 플레이 모드에서 예외를 던지고,
        //    플레이 모드의 씬 변경은 플레이를 끄면 전부 되돌아간다.
        if (!EditorApplication.isPlaying) { result.LogError("play mode required"); return; }

        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            c.renderMode  = RenderMode.ScreenSpaceCamera;
            c.worldCamera = Camera.main;
            c.planeDistance = 1f;
            result.Log("swapped {0}", c.gameObject);
        }
    }
}
```

### 🔴 그리고 **`Unity_Camera_Capture` 를 그냥 쓰지 말 것**

```
Unity_Camera_Capture(cameraInstanceID: <Camera.main.gameObject.GetInstanceID()>)
```

인자 없이 부르면 **씬 뷰**가 찍히므로 카메라 ID 는 반드시 넘긴다. 하지만 —
**이 툴은 크기 인자가 없고 `1920×1080` 을 뱉는다.** 게임뷰가 856×498 이면
나온 그림은 **2.2배 확대판**이다. 그걸로 *"붙어 보이나 · 작아서 안 보이나"* 를 판정하면
`D86` 과 **정확히 같은 실수**가 된다. 빠르게 눈으로 훑을 때만 쓴다.

### ✅ 실해상도로 찍는 법 — RenderTexture 를 게임뷰 크기로 물린다

```csharp
int W = Screen.width, H = Screen.height;      // 게임뷰 실크기
var cam = Camera.main;
var rt  = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); rt.Create();

var prevT = cam.targetTexture; var prevA = RenderTexture.active;
cam.targetTexture = rt;
cam.Render();                                  // RT 로 그리면 포커스와 무관하게 새로 그린다
RenderTexture.active = rt;

var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
cam.targetTexture = prevT; RenderTexture.active = prevA;

File.WriteAllBytes(path, tex.EncodeToPNG());
```

PNG 를 `Read` 로 읽으면 **사용자가 보는 것과 같은 그림**이 된다.
🔑 **저장할 때 픽셀 해시도 같이 찍는다** — 같은 그림이 반복해 나오는 함정을 잡는다 (기록 #230).

끝나면 `git diff --stat` 으로 **씬에 안 남았는지 확인한다.**

### 🔴 B 로 못 하는 것 — 키 입력이 필요한 화면

`StatsPanelUI`(TAB) 처럼 **여는 메서드가 `private`** 이고 `timeScale == 1` 을 요구하면
B 로는 열 수 없다 — 리플렉션은 이 프로젝트에서 금지고 키 주입은 기록 #114 로 막혀 있다.
**이런 화면은 경로 A 만 열 수 있다.** 못 열었으면 "못 봤다"고 적고 넘어간다.

---

## 판정 규칙 — 숫자 하나로는 통과시키지 않는다

### 🔴 규칙 1. `activeSelf` 는 "보인다"가 아니다

`D86` 이 여기서 틀렸다. **캔버스 좌표를 화면 픽셀로 환산해서** 같이 적는다.

#### 🔴 `scaleFactor` 를 **계산하지 말고 읽어라**

```csharp
var cs = Object.FindAnyObjectByType<Canvas>().rootCanvas;
float f = cs.scaleFactor;                       // 이게 정답이다
result.Log("screen=" + Screen.width + "x" + Screen.height + " scaleFactor=" + f);
// 실제 화면 px = 캔버스 px × f
```

**왜 읽어야 하나 — 계산하다 실제로 틀렸다.**
이 씬의 `CanvasScaler` 는 `m_ScreenMatchMode: 1` = **`Expand`** 다
(`MatchWidthOrHeight` 는 **0**, `Shrink` 는 2). 그래서 옆에 있는
`m_MatchWidthOrHeight: 0.5` 는 **쓰이지도 않는 값**인데, 그걸 보고
`MatchWidthOrHeight` 용 log2 보간식을 썼다가 **0.4532 / 실제 0.4458** 로 어긋났다.

```
Expand            → min(W/1920, H/1080)                       ← 이 씬
MatchWidthOrHeight→ 2^( (1-m)·log2(W/1920) + m·log2(H/1080) )
Shrink            → max(W/1920, H/1080)
```

> 🔴 **그리고 검산이 순환이었다.** "과거 기록 `0.453` 과 일치한다"고 적었는데,
> 856×498 을 `Expand` 로 계산하면 0.4458 이다 ⇒ **그 기록도 같은 잘못된 식으로 나온 값**이었다.
> 같은 실수끼리 맞춰 놓고 "두 건이 검증한다"고 썼다.
> **런타임 값을 읽으면 모드가 무엇이든 이 질문 자체가 사라진다.**

**환산 결과가 20px 미만이면 "안 보인다"로 판정한다.**

#### ⚠️ 그리고 **rect 크기는 잉크 양이 아니다**

`96px` 짜리 사각형이라도 그 안을 다 칠하지 않는다 —
`BossArrowGraphic` 의 삼각형은 자기 사각형의 **약 30 %** 만 칠한다.
크기로 통과선을 재면 **실제보다 후하게 판정된다.** 도형·아이콘처럼 여백이 많은 것은
`rect` 대신 **칠해진 픽셀 수**를 세고 **등가 정사각형 한 변**(`√잉크면적`)으로 환산해 비교한다.

### 🔴 규칙 2. 배선 여부와 동작 여부는 다르다

`D86` 의 상점 판매 버그는 **배선만 봤으면 통과였다**.
행을 만들어 **버튼을 실제로 눌러 봤기 때문에** 잡혔다.

⇒ 버튼은 `onClick.GetPersistentEventCount()` 가 아니라
**`button.onClick.Invoke()` 를 부르고 전후 상태를 비교**한다.

### 🔴 규칙 3. 개선 지표가 좋아져도 화면을 본다

`D56` — 대비가 3.1배 좋아졌는데 실루엣은 되지 않았다.
**숫자만 봤으면 개선이라고 보고했을 것이다.**

### 규칙 4. 겹침은 세서 적는다

`RectTransform` 월드 코너로 사각형 교차를 세고 **겹치는 쌍 개수**를 적는다.
화면으로도 같은 자리를 확대해 확인한다. 둘이 어긋나면 화면이 맞다.

### 🔴 규칙 5. 픽셀로 잴 때는 **대조군을 먼저 세운다**

렌더된 픽셀에는 **안티에일리어싱·글로우·그림자**가 섞여 있다. 그걸 모르고 세면
**멀쩡한 것이 결함으로 잡힌다.**

> 실제로 당했다 — "아이콘이 테두리 밖으로 나온다"를 재려고 칸 밖 픽셀을 셌더니
> **아이콘이 없는 빈 칸까지 22px 씩** 나왔다. 그대로 적었으면 **오진이었다**.
> 테두리 AA 2px 를 버리고 다시 재니 찬 칸 1~6 · **빈 칸 0·0·0** 이었다 (`B16`).

**대조군은 "그 효과가 없어야 하는 같은 종류의 것"으로 고른다:**

| 재는 것 | 대조군 |
|---|---|
| 아이콘이 칸 밖으로 나오나 | **빈 칸** (아이콘이 없으니 0 이어야 한다) |
| 이 연출이 뜨나 | 조건을 안 만족시킨 같은 오브젝트 |
| 이 색이 튀나 | 안 건드린 이웃 칸 |

대조군이 **0 이 아니면 측정 방법이 틀린 것**이다. 숫자를 해석하지 말고 방법을 고친다.

**그리고 눈으로 본 것을 숫자가 뒤집으면 숫자를 의심한다** — 위 사례에서
"활이 밖으로 나온다"는 눈이 맞았고 **첫 숫자가 틀렸다.**

---

## 끝낼 때 (빠뜨리면 다음 작업이 오염된다)

1. `Time.timeScale = 1f` · `Physics2D.simulationMode = SimulationMode2D.FixedUpdate`
2. 경로 B 를 썼으면 → `git diff --stat` 으로 씬에 안 남았는지 확인
3. 임시 `MonoBehaviour` · 씬 오브젝트 · 캡처 파일 **삭제**
4. `Unity_ManageEditor(GetState)` 로 플레이 모드 상태를 **다시 확인** (기록 #119 —
   내 기록은 실제 상태가 아니다). 임의로 끄지 말고 **물어보고** 끈다

---

## 함정 표

| 증상 | 원인 · 대응 |
|---|---|
| Unity 창이 회색 사각형 | 권한 미승인. `request_access` 재요청 |
| 같은 그림만 반복해서 나옴 | 포커스 없어 프레임 정지. **RT 로 그리면(§B) 포커스와 무관하게 새로 그려진다.** 해시로 확인할 것 (#230) |
| **캡처가 1920×1080 으로 나옴** | `Unity_Camera_Capture` 는 크기 인자가 없다. **확대판으로 판정하면 `D86` 과 같은 실수** → §B 의 RT 방식 |
| 창이 안 열림 (`TryOpen` 이 `private`) | B 로는 못 연다. **A 만 가능** — 못 봤으면 "못 봤다"고 적는다 |
| 대조군까지 결함으로 잡힘 | 측정 방법이 틀린 것이다 (규칙 5). AA 를 걸러라 |
| `CS0118 / CS0234` (`Image`) | `global::UnityEngine.UI.Image` 로 못박을 것 (§RunCommand) |
| UI 가 한 픽셀도 없음 | `Camera_Capture` + `ScreenSpaceOverlay`. 경로 A 또는 B |
| 씬 뷰가 찍힘 | `Unity_Camera_Capture` 를 인자 없이 불렀다. `cameraInstanceID` 를 넘길 것 |
| 패널이 이미 닫혀 있음 | MCP 호출 사이에 프레임이 갔다 (#122). `Pause` + `timeScale 0` |
| `InvalidOperationException` | `MarkSceneDirty` 를 플레이 모드에서 불렀다 (#118). 일시정지도 플레이 모드다 |
| 키를 눌러도 안 먹음 | #114. 로직을 직접 호출할 것 |
| 사람이 Game 뷰를 클릭해 결과가 튐 | 측정 중 Game 뷰를 만지지 말 것 (#1091 기록) |

---

## 보고 형식

```
### <화면 이름>  (게임뷰 WxH · scaleFactor 0.xxx)

| 항목 | 캔버스 | 실제 화면 | 판정 |
|---|---|---|---|
| 보스 화살표 | 96px | 43.5px | ✅ 보인다 (>20) |
| 겹치는 쌍   | —    | 0        | ✅ |
```

**"확인했다"로 끝내지 말 것.** 무엇을 어떤 수로 쟀는지, 그리고
**화면에서 눈으로 무엇을 봤는지** 둘 다 적는다.
