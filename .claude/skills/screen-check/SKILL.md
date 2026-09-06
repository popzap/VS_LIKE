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
| **게임뷰 실제 해상도** | ✅ **그대로** | ❌ 렌더 타깃 크기 (거짓) |
| 사용자 승인 | 세션마다 1회 필요 | 불필요 |
| 에디터 크롬(탭·인스펙터) | 같이 찍힌다 | 안 찍힌다 |

> 🔑 **"뜬다 vs 보인다" 판정은 A 로만 할 수 있다.** B 는 렌더 타깃 크기로 찍히므로
> `D86` 의 19px 문제를 **재현하지 못한다** — 즉 B 로는 그 버그를 못 잡는다.

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

그 다음 **게임 카메라를 명시해서** 찍는다 — 인자 없이 부르면 **씬 뷰**가 찍힌다:

```
Unity_Camera_Capture(cameraInstanceID: <Camera.main.gameObject.GetInstanceID()>)
```

끝나면 `git diff --stat` 으로 **씬에 안 남았는지 확인한다.**

---

## 판정 규칙 — 숫자 하나로는 통과시키지 않는다

### 🔴 규칙 1. `activeSelf` 는 "보인다"가 아니다

`D86` 이 여기서 틀렸다. **캔버스 좌표를 화면 픽셀로 환산해서** 같이 적는다.

`CanvasScaler` = `ScaleWithScreenSize` · 기준 **1920×1080** · `MatchWidthOrHeight` **0.5**:

```
scaleFactor = 2 ^ ( 0.5·log2(W/1920) + 0.5·log2(H/1080) )
실제 px     = 캔버스 px × scaleFactor
```

> 검산 — 게임뷰 856×498 → `scaleFactor 0.4532`.
> 본문 25pt → **11.3px** (`D56` 기록과 일치), 화살표 44 → **19.9px** (`D86` 기록과 일치).

**환산 결과가 20px 미만이면 "안 보인다"로 판정한다.**

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
| 같은 그림만 반복해서 나옴 | 포커스 없어 프레임 정지. 경로 A 를 쓸 것 (A 는 창이 앞에 온다) |
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
