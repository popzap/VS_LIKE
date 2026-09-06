# 버그 리포트 (공용)

> **발견자 ≠ 담당자.** 내 파트가 아닌 버그를 발견해도 여기에 적고 **고치지 않는다**
> (`CLAUDE.md` §1 "작업 중 새 문제를 발견했을 때" 그대로).
>
> 예외: 지금 하는 작업이 그 버그 때문에 **검증 불가능한 경우** →
> 사용자에게 알리고 허락을 받은 뒤에 고친다.

**쓰기 직전에 이 파일을 다시 읽는다.** 맨 아래 번호 +1 을 쓰고,
[`BOARD.md`](BOARD.md) §3 의 다음 번호도 같이 올린다.

---

| # | 발견일 | 발견자 | 담당 | 증상 / 재현 절차 | 상태 |
|---|---|---|---|---|---|
| **B1** | 2026-08-30 | 사용자(실플레이) | **DEV** (사용자 결정 2026-08-30) | 엘리트 외곽선이 실루엣과 어긋난다. **원인 둘** — ① 기준 크기 512 하드코딩(주범) ② 옆 프레임 알파 번짐 | `수정됨(D15)` |
| **B2** | 2026-08-30 | 사용자(실플레이) | **CONTENT** | **원인 확정** — Goblin·Slime 걷기 시트가 깨졌다. 16칸 중 12칸이 프레임이 아니라 **작은 캐릭터 9~16마리 뭉치** | `수정됨(C1/D4)` |
| **B3** | 2026-08-30 | 사용자(실플레이) | **DEV** | Turret·Restaurant 가 설치된 상태에서 Village 를 얻고 `Z` 를 누르면 **Village 가 아니라 Turret·Restaurant 가 한 번 더 설치된다** | ✅ `수정됨 (D39 — C안: 대기열을 신규/증설 두 구간으로)` |
| **B4** | 2026-08-30 | CONTENT | **DEV** | 일시정지·옵션창 글자 6곳이 **빈칸으로 나온다.** 씬에 한글이 남아 있는데 폰트가 Static 115자라 한글이 없다 | `수정됨(D5)` |
| **B5** | 2026-08-30 | DEV(D9 검증 중) | **DEV** | **웨이브 밖에서 적이 죽으면 `WaveManager.OnEnemyKilled` 가 NRE 를 던진다** (`_currentWaveData` 가 null). 지금 게임 경로로는 안 난다. 🔴 **2026-08-31 증상 추가 — 광역기가 첫 사망자에서 통째로 멈춘다(D20)** | `수정됨(D35)` |
| **B6** | 2026-08-30 | DEV(D14 검증 중) | **DEV** | **Dev 패널로 무기 레벨을 내리면 무기가 사라질 수 있다.** `무기 슬롯 꽉 참` 경고 3회 — `CanAcquire` 가 된다고 한 걸 `AddOrUpgradeWeapon` 이 거절한다 | `수정됨(D18)` |
| **B7** | 2026-08-31 | DEV(D26 작업 중) | **DEV** | **승급 4종(`Sentinel`·`Doomlord`·`Warden`·`Aegis`) 초상·걷기 시트의 임포트 설정이 T1 3종과 다르다.** 필터가 `Bilinear` 이고 **압축이 켜져 있고** `maxTextureSize` 가 2048 이다 — 픽셀아트가 뭉개진 채로 2배 해상도로 들어온다. `I-61` 이 PPU 만 고치고 나머지를 놓쳤다 | `수정됨(D35)` |

| **B8** | 2026-09-02 | DEV(D27 조사 중) | **DEV** | **적이 뭉치면 무리 분리(separation)가 스스로 약해진다.** `EnemyBase.GetSeparation()` 의 이웃 버퍼가 **12칸 고정**인데 이 오버로드는 넘치는 이웃을 **경고 없이 버린다** | 🟡 `절반 수정(D27) — 버퍼 12→64 로 적 ≤400 에서 절단 0. 다만 겹침 자체는 거의 안 줄었다(0.5유닛내 84.6→85.1 %) — 원인은 밀도다`

| **B9** | 2026-09-02 | DEV(D27 측정 중) | **DEV** | ~~게임이 `state=Wave` 인데 `timeScale=0` 으로 영구히 얼어붙는다~~ 🔴 **오진이었다.** 실제로는 `StageClearUI` 가 Continue 를 기다리며 정상적으로 멈춘 상태였다 (사용자 지적, 2026-09-02) | `✘ 철회(오진) — 잠재 결함 관찰만 남긴다` |
| **B10** | 2026-09-03 | DEV(D29 착수 중) | **DEV** | **레벨이 한 번에 여러 번 오르면 카드를 1장만 받는다.** `ExperienceManager.CollectXp:145` 의 `while` 이 레벨마다 `TriggerLevelUp()` 을 부르는데 `LevelUpManager.ShowLevelUpPanel:53` 에 **큐가 없어** 패널이 덮어써진다. 나머지 레벨의 카드가 **조용히 사라진다** | `수정됨(D29)` |
| **B11** | 2026-09-04 | DEV(D40 검증 중) | **DEV** | **상점 NPC 대사 5줄이 전부 빈칸(□)으로 나온다.** 코드 기본값은 영문인데 `[SerializeField] npcDialogues` 라 **씬에 직렬화된 옛 한글 값이 코드를 덮는다.** `B4`(D5 에서 씬 6곳 영문화)가 **이 배열을 놓쳤다** | ✅ `수정됨 (D43 — 씬 + 프리팹 원본 둘 다)` |

| **B17** | 2026-09-06 | 사용자 실플레이 | **DEV** | **상점에 오래 있다 레벨업하면 상태 기계가 꼬인다.** 예외는 0 — `Editor.log` 스택으로만 잡힌다. 원인 셋이 겹쳤다: ① `VillageBuilding` 이 상점에서도 XP 를 준다(설계) ② 🔴 `ShopRoot`[2] 가 `LevelUpPanel`[1] **위에 그려져** 카드가 숨고 클릭을 상점이 먹는다 ③ 🔴 `OnLevelUpCompleted` 가 **열 때 찍은** `_stateBeforeLevelUp`(=Shop)을 뒤늦게 되돌려 **`Wave` 에서 `Shop` 으로 끌어낸다** — 상점이 없는데 상태만 `Shop` | `수정됨(D95)` |
| **B16** | 2026-09-06 | DEV(D88 화면 검증 중) | **DEV** | **HUD 아이템 칸의 아이콘이 테두리 밖으로 삐져나온다.** `Prefab_ItemChip` 의 `Icon` 이 `m_SizeDelta 64x64` **고정**이라 칸 크기를 안 따라간다. `D87` 이 HUD 칸을 36→**50** 으로 바꿔 **50 칸에 64 아이콘**이 됐다 (사방 7 캔버스px = 화면 **3.2px**). 빈 칸은 `icon a=0.00` 이라 안 보일 뿐 **크기는 같다** | `수정됨(D91)` |

> 상태값: `열림` · `확인중` · `수정됨(D3)` · `재현안됨` · `보류(사유)`
> **줄을 지우지 않는다.** 닫혀도 그대로 둔다 — 재발했을 때 근거가 된다.

---

## 적을 때

`재현 절차`는 **다른 세션이 그대로 따라할 수 있게** 쓴다.
"진화가 가끔 안 됨" 이 아니라 이렇게:

> Warrior 시작 → Gun Lv5 + Turret Lv3 → Z 로 Turret 설치 →
> 다가가도 `[E] PROMOTE` 가 안 뜸. 콘솔에 `[EVO]` 로그 없음

---

## 담당 판정 기준

| 증상 | 담당 |
|---|---|
| 예외 · null · 로직이 틀림 | DEV |
| 씬/프리팹 배선 누락 · 참조 끊김 · 캔버스 순서 | DEV |
| 그림/소리가 **없거나 잘못된 파일**임 · 스프라이트 잘림 · 알파 이상 | CONTENT |
| CSV 값이 SO 에 안 들어감 | 임포터면 DEV, 값이 틀렸으면 CONTENT |
| **너무 세다 / 약하다 / 답답하다** | 버그 아님 → [`../TUNING.md`](../TUNING.md) |
| **아직 안 만들었다** | 버그 아님 → [`../TODO.md`](../TODO.md) |

> 마지막 두 줄이 중요하다. 여기는 **"동작하나?"**(답이 예/아니오)만 적는다.
> **"느낌이 맞나?"**(답이 숫자)는 `TUNING.md`, **"아직 없다"**는 `TODO.md` 다.

---

# 상세

## B1 — 엘리트 외곽선이 실루엣과 어긋난다

**발견 경위:** 2026-08-30, `TODO.md` §1 실플레이 중. 승급(`[E] PROMOTE`)은 정상 동작했고
겉모습도 바뀌었다 — 그 김에 눈에 들어온 것들이다.

**증상:** 엘리트 적의 외곽선이 스프라이트 실루엣을 따라가지 않는다.

> ⚠️ **"굵기가 마음에 안 든다"가 아니다.** 굵기·색 취향은 `TUNING.md` 소관이다.
> 여기 적은 건 **선이 그림과 다른 자리에 그려진다**는 뜻이다 (예/아니오로 답할 수 있다).

### 🔴 원인 확정 (2026-08-30, DEV) — **B2 와 다른 버그다**

`Assets/Game/Shaders/SpriteOutline.shader` 129~134행:

```hlsl
half SampleAlpha(float2 uv)
{
    // 스프라이트 UV 밖은 아틀라스의 다른 그림일 수 있다. 0 으로 막는다.
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return 0;
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
}
```

**주석이 말하는 일을 이 코드는 하지 않는다.** `IN.uv` 는 **텍스처 전체 기준**이다.
1024×1024 시트에서 잘라 온 256×256 프레임의 uv 는 `0~1` 이 아니라 예컨대 `x∈[0.25,0.5]` 다.
그래서 `0~1` 검사는 **한 번도 걸리지 않고**, 외곽선이 **옆 칸(다음 걷기 프레임)의 알파**를
그대로 빨아들인다. 걸을 때마다 이웃 프레임이 바뀌니 **선이 그림과 따로 논다.**

**왜 이제야 보이나:** I-58 전에는 적이 **1장짜리 단독 png** 였다. 그때는 uv 가 진짜 `0~1` 이라
이 검사가 우연히 맞게 동작했다. 시트로 바꾼 순간 전제가 깨졌다.

**수치로 확인 (엘리트 `_OutlineWidth` 14, `_OutlineTexSize` 512):**
`d = 14/512 = 0.0273` uv → 1024 텍스처에서 **28 텍셀**. 프레임 여백이 28px 보다 좁으면 넘어간다.

| 적 | 프레임 bbox | 칸(256) 여백 | 넘어가나 |
|---|---|---|---|
| Wolf | 227×129 | 약 **14px** | 🔴 **넘어간다** |
| Demon | 최대 **256**×211 | **0px** | 🔴 **넘어간다** |
| Ogre | 192×202 | 약 32px | 아슬아슬 |
| Zombie | 130×190 | 약 63px | 안 넘어감 |

> ℹ️ `_OutlineTexSize` 가 `512` 로 고정인 것도 같이 봐야 한다. 시트는 **1024** 라
> 실제 두께가 의도의 2배다. 다만 이건 **두께 문제(TUNING 감)** 이고,
> 여기 적은 **"선이 딴 데 그려진다"** 와는 별개다.

### 고치는 법 — uv 를 스프라이트 칸 안으로 가둔다

셰이더가 **이 프레임이 텍스처의 어느 사각형인지** 알아야 한다. 두 가지 길이 있다:

| 안 | 방법 | 대가 |
|---|---|---|
| **A** | `MaterialPropertyBlock` 으로 `_SpriteRect(x,y,w,h)` 를 넘기고 `SampleAlpha` 가 그 안으로 clamp | `EnemyVisual` 이 **프레임을 바꿀 때마다** 갱신해야 한다(= 매 프레임). 정확하다 |
| **B** | 시트를 안 쓰고 프레임을 **낱장 png** 로 나눈다 | 셰이더를 안 고쳐도 된다. 애셋 수가 6×16 = 96장으로 는다 |

⚠️ **파일 소유가 갈린다.** 원인은 **로직 버그(→ DEV)** 인데
`Assets/Game/Shaders/` 는 **CONTENT 소유**다. A안은 `EnemyVisual.cs`(DEV) 도 같이 고쳐야 한다.

### ✅ 담당 = DEV (사용자 결정, 2026-08-30)

**`Assets/Game/Shaders/SpriteOutline.shader` 만 예외로 DEV 가 직접 고친다.**
셰이더와 `EnemyVisual.cs` 를 쪼개면 **어느 쪽이 원인인지 검증할 수 없다** —
`MaterialPropertyBlock` 으로 값을 넘기는 쪽과 받는 쪽이 한 몸이라 그렇다.
CONTENT 는 이 파일을 **건드리지 않는다.**

> ⚠️ **B2 수정(C1)은 B1 을 고치지 않는다.** 오히려 알아 둘 것 —
> Demon(최대 bbox **256** = 여백 0) · Wolf(**230** = 여백 13)는 **일부러 안 줄였다.**
> 줄이면 여백이 확보돼 번짐이 가려지지만 **적의 화면상 크기가 바뀐다.**
> 즉 위 표의 "넘어간다" 판정은 C1 뒤에도 **그대로 유효하다.**
> 반대로 셰이더를 고치고 나면 **여백 제약 자체가 사라진다** —
> 앞으로 만들 시트에 "여백 30px 이상"을 요구하지 않아도 된다는 뜻이다.

### ✅ 수정됨 (2026-08-30, D15) — **위 진단은 절반만 맞았다**

A안대로 고치고 **실제로 재 봤더니** 원인이 하나가 아니었다. 순서를 바꿔 적는다.

#### ① 진짜 주범 — `_OutlineTexSize` 가 `512` 로 굳어 있었다

선 굵기는 `d = _OutlineWidth / _OutlineTexSize` 의 **uv 거리**다.
시트는 **1024** 인데 셰이더 기본값이 512 라 **의도의 2배(28텍셀)** 로 그려지고 있었다.
그런데 시트의 **한 프레임 자체가 130~230텍셀**뿐이다:

| 적 | 프레임 높이(텍셀) | 28텍셀 선이 차지하는 비율 |
|---|---|---|
| Slime | 118 | **23.8 %** |
| Wolf | 133 | **21.1 %** |
| Goblin | 135 | **20.8 %** |
| Demon | 190 | 14.7 % |
| Zombie | 194 | 14.4 % |
| Ogre | 206 | 13.6 % |

낱장 png 시절(512 텍스처)엔 같은 값이 **14텍셀 = 키의 2.7 %** 였다.
즉 I-58 이 적을 시트로 바꾼 순간 **선이 실루엣을 통째로 덮는 보라 덩어리**가 됐다.
사용자가 "선이 그림과 다른 자리에 있다"고 느낀 것의 대부분이 이것이다.

> 위 §"원인 확정"에서 이 항목을 *"두께 문제라 TUNING 감"* 으로 미뤄 뒀는데, **틀렸다.**
> 512 는 취향값이 아니라 **텍스처 크기와 안 맞는 코드 결함**이다.
> 취향값은 그 다음 문제인 `_OutlineWidth`(=몇 텍셀로 할지) 쪽이고, 그건 [`../TUNING.md`](../TUNING.md) 로 넘겼다.

#### ② 옆 프레임 번짐 — 있긴 한데 **처음 예측보다 훨씬 드물다**

위 표는 프레임이 **256 격자 칸에 꽉 찬다**고 가정했는데, 실제 `Sprite.textureRect` 는
**타이트 슬라이싱**이라 칸이 아니다 (예: Wolf frame_5 = `(274.08, 580.08, 223.90, 117.85)` — 소수점).
내 여백 + 이웃 여백이 합쳐지므로 28텍셀을 넘는 경우가 대부분이다.
프레임 96장을 전수 조사한 결과:

| 적 | 번지는 픽셀 | 판정 |
|---|---|---|
| Demon f14 | 2616 px | 🔴 실제로 번진다 |
| Wolf | 0~수 px | 사실상 없음 |
| 그 외 4종 | 0 px | 없음 |

그래도 고쳤다 — **드물다는 건 안 고쳐도 된다는 뜻이 아니고**, 앞으로 시트를 더 빽빽하게
채우면 바로 재발한다. `_SpriteRect` 가 있으면 "여백 30px 이상" 같은 제약을 CONTENT 에
요구하지 않아도 된다.

#### 고친 내용

| 파일 | 무엇 |
|---|---|
| `Assets/Game/Shaders/SpriteOutline.shader` | `_SpriteRect` 프로퍼티 신설. `SampleAlpha` 의 `0~1` 검사를 **이 사각형 검사**로 교체 |
| `Assets/Scripts/Enemy/EnemyVisual.cs` | `ApplySpriteRect(Sprite)` 신설 — 스프라이트가 바뀔 때만 `_SpriteRect` + `_OutlineTexSize`(=실제 텍스처 크기)를 MPB 로 넘긴다 |

⚠️ **풀 재사용 함정을 같이 막았다.** `Setup` 에서 `_rectSprite = null` 로 캐시를 깨고 **항상**
다시 넘긴다 — 안 그러면 늑대였던 개체가 슬라임으로 재활용될 때 **남의 칸**이 남는다.

#### 검증 (플레이 모드, 살아있는 적 14마리)

```
살아있는 적 14   _SpriteRect 불일치 0   _OutlineTexSize 틀림 0   외곽선 씌운 수 4
Enemy_Goblin(Clone) frame_10 tex=1024 want=(0.5147,0.3165,0.7363,0.4511) got=(동일) texSize=1024
```

서로 **다른 프레임 3종**(`frame_2`·`frame_10`·`frame_14`)이 각자 자기 사각형과 일치했다 =
스폰 때 한 번이 아니라 **프레임마다 따라간다.** 게임 카메라 캡처에서도 보라 선이
실루엣에 붙어 있는 것을 눈으로 확인했다. 콘솔 에러·경고 **0**.

---

## B2 — 움직일 때 걷기 시트 4×4 가 통째로 보인다

**증상:** 일부 적이 **이동 중**에 한 프레임이 아니라 **4×4 격자 그림 전체**로 보인다.
정지 중에는 정상으로 보인다.

**재현 절차:** 노말 웨이브 진입 → 적이 플레이어를 향해 걸어올 때 관찰.
(어느 적 종류인지는 아직 특정 못 함 — 사용자 표현은 "몇몇 몹")

### 🔴 원인 확정 (2026-08-30, DEV) — **애셋이 깨졌다. 코드는 정상이다**

먼저 코드·배선이 멀쩡함을 확인했다:

- `EnemyData.WalkFrames` 는 **6종 전부 정확히 16장** 배선됨 → 슬라이스는 됐다
- `.meta` 슬라이스는 **4×4 · 256×256 · `frame_0`~`frame_15`** 로 정확하다
- `EnemyVisual.StepFrames` 는 `_frames[_frameIndex]` **한 장만** 그린다

**진짜 원인:** AI 로 생성한 시트 자체가 깨져 있다. 칸마다 알파 덩어리 수를 세어 보면:

| 적 | 정상 칸 (덩어리 1개) | 깨진 칸 | 깨진 칸의 내용 |
|---|---:|---:|---|
| **Goblin** | **4** (f0~f3) | **12** | 칸 하나에 작은 고블린 **16마리** |
| **Slime** | **4** (f0~f3) | **12** | 칸 하나에 작은 슬라임 **9마리** |
| Demon | 13 | 3 (f10·f14·f15) | 작은 보석 몇 개가 섞임 |
| Wolf | 15 | 1 (f15) | 구석에 작은 늑대 1마리 |
| Ogre | **16** | 0 | ✅ 정상 |
| Zombie | **16** | 0 | ✅ 정상 |

즉 **Goblin·Slime 은 쓸 수 있는 프레임이 4장뿐**이고 나머지 12칸은 "축소판 여러 마리"다.
걸을 때 `StepFrames` 가 16칸을 순서대로 도니 **12/16 확률로 그 뭉치가 화면에 뜬다** —
사용자가 본 "4×4 이미지"가 정확히 이것이다. 정지 시엔 `_idleFrame`(f0, 정상)으로 고정되므로
멀쩡해 보이는 것도 맞아떨어진다. "몇몇 몹"인 이유도 6종 중 2종만 심하게 깨져서다.

**담당 = CONTENT.** 판정 기준표의 *"그림이 없거나 **잘못된 파일**임"* 에 해당한다.
코드는 고칠 게 없다.

> ❌ **기각된 가설:** 처음엔 "시트가 4방향×4프레임인데 코드가 일렬로 돈다"고 봤다.
> Ogre·Zombie 가 16칸 모두 **같은 방향**의 걷기 프레임인 것을 눈으로 확인해 기각했다.
> 방향 축은 애초에 없고, 좌우는 `flipX` 로 처리하는 게 맞다.

### ✅ 수정본 제출 — C1 (2026-08-30, CONTENT)

DEV 의 원인 분석을 **그대로 믿지 않고 다시 셌다.** 결과는 거의 일치했고 한 칸만 달랐다:

| 적 | DEV 집계 | CONTENT 재검 | 판정 |
|---|---|---|---|
| Goblin | 정상 4 | 덩어리 수로는 **f4 도 1개** | ❌ 내가 틀렸다 — f4 를 **눈으로 보니 이중노출 잔상**이다. 덩어리가 붙어 있어 수가 속았다. **DEV 가 맞다 (정상 4장)** |
| Slime | 정상 4 | 정상 4 | 일치 |
| Demon | 깨진 칸 f10·f14·f15 | 잔여물이 있는 칸 f14·f15 | 잔여물 제거로 처리 |
| Wolf | 깨진 칸 f15 | f15 | 일치 |

> 🔑 **덩어리 수만으로는 부족하다.** 붙어 있는 잔상은 1개로 세어진다. **눈으로 봐야 한다.**

**고친 방법 — 적마다 다르다.** 파일은 `_Incoming/Enemies/Walk/` 에 있다.

| 적 | 방법 | 왜 |
|---|---|---|
| Goblin·Slime | 정상 4프레임을 **4번 반복**해 16칸을 채움 | 쓸 수 있는 프레임이 4장뿐이다. 16칸을 반복으로 채우면 `StepFrames` 가 일렬로 돌아도 **깨끗한 4프레임 루프**가 된다 → **코드 수정 불필요** |
| Demon·Wolf | 16프레임 유지, **가장 큰 덩어리의 15% 미만인 잔여물만 삭제** | 진짜 애니메이션 프레임 16장이 살아 있다. 날개 끝 같은 큰 부속은 남기려고 비율 기준을 썼다 |

지운 픽셀: Demon **4,830** · Wolf **2,470** · Goblin·Slime **0**(반복이라 지울 게 없다).

**넘기기 전 확인한 것**

- 4장 전부 `alpha min=0 max=255` → **I-41 가짜 투명 아님**
- 64칸(16×4) **전수** 알파 덩어리 **정확히 1개**
- Goblin f0~f3 바닥 y **전부 189**, Slime f0~f3 바닥 y **전부 173** → 반복해도 발이 안 튄다
- **4장 다 눈으로 봤다.** 격자·보석·잔여 늑대 전부 사라졌다

**임포트·실플레이 검증은 DEV 몫** → [`REQ/DEV.md`](REQ/DEV.md) 요청-1.
🔴 그 요청의 핵심은 **`.meta` 를 살리고 png 만 덮어쓰는 것**이다. 지웠다 넣으면
PPU 가 1024 로 돌아가고(I-58) 스프라이트 GUID 가 바뀌어 `WalkFrames` 가 통째로 끊긴다.

### ✅ 임포트·검증 완료 — D4 (2026-08-30, DEV)

png 4장만 덮어썼고 `.meta` 6개는 타임스탬프까지 그대로다. 5개 판정 기준 전부 통과 —
PPU 512 · 슬라이스 16 · `WalkFrames` 6종 `None` 0 · `bounds` 불변 · **실플레이 격자 0회.**
상세는 [`DONE/D4.md`](DONE/D4.md).

> ⚠️ **이 수정은 B1 을 고치지 않는다.** Demon(최대 256)·Wolf(최대 230)는 여전히 여백이
> 28px 미만이라 외곽선 번짐이 남는다. **일부러 안 줄였다** — 줄이면 적의 화면상 크기가
> 바뀌는데 그건 버그 수정이 아니라 연출 변경이다. Goblin(149×132)·Slime(175×114)은
> 200 이하라 기준을 만족한다 (Ogre 202 · Zombie 198 과 같은 급).

---

## B3 — 새 건물을 얻어도 `Z` 가 예전 건물을 다시 세운다

**증상:** Turret 과 Restaurant 가 이미 설치된 상태에서 레벨업으로 **Village 를 얻고** `Z` 를 눌렀더니,
Village 가 아니라 **Turret 과 Restaurant 가 한 채씩 더** 설치됐다.

**재현 절차:**
1. Warrior 로 시작 (건물 5칸)
2. Turret 을 얻어 `Z` 로 설치 → Restaurant 을 얻어 `Z` 로 설치
3. **Turret / Restaurant 을 레벨업 카드로 한 번 이상 더 찍는다**
4. Village 를 새로 얻는다
5. `Z` → **Village 가 아니라 Turret 이 먼저 나온다**

**읽어서 확인한 것 (2026-08-30, DEV) — 코드상 근거:**

`BuildingManager.UnlockBuilding` (38~51행)

```csharp
int allowed = data.GetMaxCount(level);
for (int owned = PlacedCount(data) + PendingCountOf(data); owned < allowed; owned++)
    _pendingQueue.Add(data);
```

`GetMaxCount` 는 **레벨에 따라 커진다.** 그래서 Turret 을 레벨업하면 그 순간
`_pendingQueue` 에 Turret 이 **말없이 더 쌓인다.** 그 뒤 Village 를 얻으면 큐 **맨 뒤**에 붙고,
`PlaceNext` 는 `_pendingQueue[0]` 부터 꺼내므로(FIFO, 75~107행) 밀린 Turret 이 먼저 나온다.

**⚠️ 즉 이건 예외도 null 도 아니다. 설계대로 도는 중이다.** 진짜 문제는 이것이다:

- 플레이어에게 **큐가 보이지 않는다.** `BuildingManager.NextPending` 은 이미 공개돼 있는데
  **HUD 가 안 쓴다.** 그래서 `Z` 를 누르기 전엔 무엇이 나올지 알 방법이 없다
- 레벨업 카드는 "Village 획득"이라고 말해 놓고 `Z` 는 다른 걸 준다 —
  **UI 가 한 약속을 조작이 안 지킨다**

**고치는 방향이 하나가 아니었다 — 사용자가 B 를 골랐다 (2026-08-30).**

| 안 | 내용 | 대가 |
|---|---|---|
| **A** | `Z` 를 누르면 **가장 최근에 얻은 것**부터 (LIFO) | 밀린 Turret 2채가 영영 안 나올 수 있다 |
| **B** ✅ **적용됨 (D2)** | HUD 에 `NextPending` 을 표시만 한다 | 동작은 그대로. **가장 작고 되돌리기 쉽다** |
| **C** | 새로 **해금된** 건물은 큐 맨 앞에 넣고, 레벨업 증설분은 뒤에 | 규칙이 둘로 갈려 설명이 길어진다 |
| **D** | 설치할 건물을 플레이어가 고른다 (새 UI) | 범위가 크다 → `TODO.md` 감 |

### ✅ B안 적용 — D2 (2026-08-30)

HUD 에 `[Z] Build: Turret  (+3 queued)` 를 띄운다. 상세·검증 로그는 [`DONE/D2.md`](DONE/D2.md).

🔴 **줄을 `수정됨` 이 아니라 `보류` 로 뒀었다.** 표시가 붙었을 뿐 **큐 순서는 그대로**였다 —
"Village 를 얻었는데 Turret 이 나온다"가 여전히 일어났다. 다만 **일어나기 전에 보였다.**

### ✅ C안 적용 — D39 (2026-09-04) · **닫음**

사용자 지시로 순서까지 고쳤다. **대기열을 두 구간으로 나눈다.**

| 구간 | 무엇 | 넣는 곳 |
|---|---|---|
| 앞 | **처음 해금된 건물** | `_pendingQueue.Insert(_freshCount++, data)` |
| 뒤 | **레벨업 증설분**(`MaxCount` 증가분) | `_pendingQueue.Add(data)` |

🔑 **원안의 "큐 맨 앞에 넣는다"는 그대로 쓰지 않았다.** `index 0` 에 넣으면
**나중에 해금한 건물이 아직 못 세운 먼저 해금한 건물을 추월한다.**
그래서 "맨 앞"이 아니라 **신규 구간의 끝**(`_freshCount`)에 넣는다 — 구간 안에서는 FIFO 가 유지된다.

🔴 **`_freshCount` 는 장부다.** `_pendingQueue` 를 건드리는 경로가 전부 같이 맞춰야 한다 —
`PlaceNext`(맨 앞 제거 → `RemoveFront()` 헬퍼로 통일) · `LockBuilding`(상점 제거, 임의 위치라
신규 구간 안에서 몇 개가 빠지는지 먼저 센다) · `ResetRunState`(0 으로).

**검증 (플레이 모드, 2026-09-04)** — 버그 리포트의 재현 절차 그대로.

```
1. Turret 해금      → NextPending=Turret   → Z 설치됨
2. Restaurant 해금  → NextPending=Restaurant → Z 설치됨
3. Turret Lv2 · Restaurant Lv2 (증설)  → 대기 1개, 맨 앞=Turret
4. Village 해금     → NextPending=Village   ← 🔑 여기서 뒤집힌다
5. Farm 도 해금     → NextPending=Village   ← 추월하지 않는다
6. Z 순서 = Village > Farm > Turret
```

🔑 **3번이 이 검증의 대조군이다.** Village 를 얻기 직전 맨 앞이 **`Turret`** 이었고,
4번에서 **`Village`** 로 바뀌었다. 구버전이면 4번이 `Turret` 으로 찍힌다 —
즉 이 시험은 **아무 값이나 통과시키지 않는다** (`D34` 의 교훈).

`LockBuilding` 장부도 따로 시험했다:

```
신규3(Village,Farm,Bombard) + 증설1(Turret)  → 맨 앞=Village (대기 4)
상점에서 Village 제거                        → 맨 앞=Farm    (대기 3)
Village 재해금 후 Z 순서 = Farm > Bombard > Village > Turret   (기대값과 일치)
```

콘솔 Error·Warning **0건**.

---

## B17 — 🔴 **상점에 오래 있다 레벨업하면 게임이 상태 기계째 꼬인다** (사용자 실플레이 · 2026-09-06)

**증상:** 사용자 — *"상점을 키고 오래 있으니까 레벨업하면서 게임이 고장났어"*

**예외는 하나도 안 났다.** 그래서 콘솔만 보면 멀쩡하고, `Editor.log` 의 **스택 트레이스**로만 잡힌다.

### 재현된 사슬 (실제 로그 · 스택 포함)

```
[ShopManager] 상점 열림 · State → Shop        StageMapUI.OnNodeClicked
[ShopManager] 구매: Turret (125G 소모)
State → LevelUp                               ExperienceManager.TriggerLevelUp:179
                                              <- ExperienceManager.CollectXp:171
                                              <- VillageBuilding.OnCooldownElapsed:11
                                              <- BuildingBase.Update:65
State → StageMap                              ShopManager.CloseShop:140
[ShopManager] 상점 닫힘
State → Wave                                  GameManager.OnStageNodeSelected:261
State → Shop                                  🔴 GameManager.OnLevelUpCompleted:453
                                              <- LevelUpManager.HidePanel:172
                                              <- LevelUpManager.SelectItem:221
```

### 🔴 원인 셋이 겹쳤다

**① `VillageBuilding` 이 상점에서도 XP 를 준다.**
`BuildingBase.Update` 의 쿨다운이 상점 화면에서도 돌아서 **오래 있을수록** 레벨업이 터진다.
*"오래 있으니까"* 가 이것이다. (이 자체는 `D54` 주석이 이미 인정한 설계다)

**② 🔴 `ShopRoot` 가 `LevelUpPanel` 보다 위에 그려진다.**

```
[0] HUD
[1] LevelUpPanel     <- 레벨업 카드
[2] ShopRoot         <- 🔴 상점이 그 위를 덮는다
```

`LevelUpPanel/DimBG` 는 `raycastTarget=True` 지만 **자기보다 아래만** 막는다.
⇒ 레벨업 카드가 **상점 UI 뒤에 숨고**, 클릭은 **상점이 먼저 먹는다.**
사용자는 카드를 못 보고 상점의 나가기를 눌렀고, 맵으로 나가 다음 노드까지 골랐다.

**③ 🔴 `OnLevelUpCompleted` 가 낡은 상태를 되돌린다.**

```csharp
public void OnLevelUpCompleted() => ChangeState(_stateBeforeLevelUp);
```

`_stateBeforeLevelUp` 은 **열 때** 찍힌다(`Shop`). 그 사이 상점이 닫히고 노드가 진행되고
웨이브까지 들어갔는데, 뒤늦게 카드를 고르자 **게임을 `Wave` 에서 `Shop` 으로 끌어냈다.**
⇒ **상점이 없는데 상태만 `Shop`** 이라 HUD·입력·진행이 전부 어긋난다. 이게 *"고장"* 이다.

### 🔑 이 버그의 성질

**세 개 중 하나만 고쳐도 이 사슬은 끊긴다.** 다만 고치는 곳에 따라 게임이 달라진다:

| 안 | 무엇을 고치나 | 대가 |
|---|---|---|
| **A** | `LevelUpPanel` 을 맨 위로 올린다(형제 순서 / `sortingOrder`) | 가장 작다. 레벨업 중에는 상점을 못 만진다 — **원래 그래야 맞다** |
| **B** | `OnLevelUpCompleted` 가 **지금 상태가 아직 `LevelUp` 일 때만** 되돌린다 | 안전망. 다른 경로로 상태가 바뀌어도 안 덮어쓴다 |
| **C** | 상점·맵에서는 레벨업을 **미뤘다가** 전투 복귀 시 띄운다 | 가장 크다. 큐가 필요하고 `B10`(다중 레벨업)과 얽힌다 |

🔑 **A 와 B 는 성격이 다르다** — A 는 *"그 일이 일어나지 않게"*, B 는 *"일어나도 안 깨지게"* 다.
`B14`·`B15` 에서 배운 대로 **둘 다 하는 게 맞다.** A 만 하면 다른 입구(예: `DevPanel`,
이벤트 패널)가 생길 때 같은 사슬이 되살아난다.

⚠️ **①은 고치지 않는 쪽을 권한다.** 상점에서 XP 가 도는 건 마을 건물의 설계 의도고,
막으면 *"상점에 있는 동안 마을이 논다"* 가 된다. **표시 순서와 복귀 규칙**만 고치면 된다.

## ✅ 해결 (D95, 2026-09-06) — **①은 사용자 판단이 옳았다**

### 🔑 내 권고가 틀렸다

나는 *"①은 고치지 않는 쪽을 권한다 — 막으면 상점에 있는 동안 마을이 논다"* 고 적었다.
사용자가 정정했다:

> 상점 노드에 있을때는 마을나 농장같은 건물이 작동하면 안되지 / 전투 노드에 있을때만 작동해야해

**맞는 말이다.** 나는 *"기존 동작을 지키는 것"* 을 기본값으로 놓고 판단했는데,
애초에 **마을·농장이 전투 밖에서 도는 것 자체가 이상하다.** 터렛·곡사포도 마찬가지다 —
맵 화면에서 쏠 적이 없다. ⇒ **셋 중 가장 위쪽(원인)을 끊는 답이었다.**

### 고친 것 셋

| | 무엇 | 어디 |
|---|---|---|
| **①** | 건물 쿨다운이 **`GameState.Wave` 에서만** 돈다 | `BuildingBase.Update` |
| **②** | `LevelUpPanel` 형제 순서 **1 → 7** (상점 위 · 일시정지 아래) | 씬 |
| **③** | `OnLevelUpCompleted` 가 **아직 `LevelUp` 일 때만** 되돌린다 | `GameManager` |

🔑 **타이머를 리셋하지 않고 멈춘다.** 전투로 돌아오면 멈춘 자리에서 이어진다 —
리셋하면 상점을 들를 때마다 건물이 한 박자씩 손해를 본다.

🔑 **②③ 은 성격이 다르다.** ①이 원인을 없애지만, ②는 *"그 일이 일어나도 안 보이게"*,
③은 *"일어나도 안 깨지게"* 다. `B14`·`B15` 에서 배운 대로 **셋 다 했다** —
①만 하면 다른 XP 경로(이벤트 보상 등)가 생길 때 같은 사슬이 되살아난다.

### 검증 (플레이 모드 · 실시간 20초씩)

```
[대조군] 전투 아님(StageMap) · timeScale 1 로 20초  ->  Lv 1 · XP 0     <- 안 돈다
[본시험] 전투(Wave) 로 20초                        ->  Lv 1 · XP 2     <- 돈다

사슬 재현:
① 상점 위에서 레벨업: state = LevelUp
② 상점 닫고 전투 진입: state = Wave
[Warning] 레벨업이 끝났는데 상태가 이미 Wave 다 — 'Shop' 로 되돌리지 않는다 (B17)
③ 뒤늦게 카드 선택 후: state = Wave        <- 예전엔 Shop 으로 끌려갔다
[대조군] 평범한 전투 중 레벨업 -> Wave · timeScale 1
```

**판정 5/5.** 🔑 **대조군이 판정의 절반이다** — *"상점에서 안 오른다"* 만으로는
*"아예 안 도는 것"* 과 구별되지 않는다. `timeScale 1` 을 명시적으로 확인해
**시간이 흐르는데도 안 오른다**를 봤고, 전투로 바꾸니 **XP 2** 로 올랐다.

⚠️ **`Restaurant`·`Farm` 도 같이 멈춘다.** 의도한 것이다 — 사용자 지시가
*"마을이나 농장같은 건물"* 이었고, 전투 밖에서 도는 건물은 없어야 맞다.

---

## B16 — 🔴 **HUD 아이템 아이콘이 테두리 밖으로 나온다** (칸은 50인데 아이콘이 64)

**발견 경위:** `D88`(`/screen-check`)로 `D87`·`D89`·`D90` 의 결과를 화면으로 판정하다가
확대본에서 활·총·터렛이 상자 밖으로 걸치는 것이 보였다. **지적 밖에서 나왔다.**

### 🔴 원인 확정 — 아이콘이 칸 크기를 안 따라간다

플레이 모드에서 실제 `RectTransform` 을 읽었다:

```
Chip_Sword | chip 50x50 | icon 64x64 a=1.00 psa=True spr=Sword | frame 50x50 a=0.95
Chip_Empty | chip 50x50 | icon 64x64 a=0.00 psa=True spr=null  | frame 50x50 a=0.55
```

`Prefab_ItemChip.prefab` 의 `Icon` 은 `m_SizeDelta: {x: 64, y: 64}` **고정**이고
앵커가 위-가운데(`pivot 0.5,1` · `anchoredPosition y=-4`)다. 칸이 **76**(레벨업 카드·TAB 격자)일
때는 64 가 들어갔는데, `D87` 이 **HUD 줄의 칸만 36 → 50** 으로 바꾸면서
**50 짜리 칸에 64 짜리 아이콘**이 됐다. 사방 7 캔버스px = 게임뷰(856x498 · `scaleFactor 0.4532`)에서
**3.2px** 씩 넘친다.

> 🔑 **`D87` 이 만든 게 아니라 `D87` 이 드러낸 것이다.** 칸이 36 이던 시절엔 사방 14px 로
> **더 심했다.** 그때는 아무도 화면을 못 봤다.

### 판정 — 빈 칸을 대조군으로 썼다

렌더 타깃을 **실제 게임뷰 크기(856x498)로 맞춰** 그린 뒤, 테두리 AA 2px 를 버리고
칸 사각형 **2px 바깥**의 비배경 픽셀을 셌다:

| 칸 | 상태 | 2px 밖 픽셀 | 뻗은 범위 |
|---|---|---|---|
| R1 무기 1·2·3 | 찬 칸 | 5 · 6 · 1 | `dx -5..24` · `dy -5..25` |
| R2 패시브 1·2 | 찬 칸 | 1 · 3 | `dy -5..26` |
| R3 건물 1·2 | 찬 칸 | 1 · 1 | `dy -5..-3` |
| **R2·R3 빈 칸 3개** | **빈 칸** | **0 · 0 · 0** | — |

🔑 **빈 칸 세 개가 전부 0 이라 이건 아이콘이다** — 테두리 AA 가 아니다.
🔴 **첫 측정은 AA 를 안 걸러 빈 칸까지 22px 씩 나왔다.** 그대로 적었으면 오진이었다 —
대조군이 그것을 잡았다.

### 고치는 법 (제안 · 아직 안 고쳤다)

`Icon` 을 부모에 맞춰 늘린다 — `anchorMin (0,0)` · `anchorMax (1,1)` · `sizeDelta (0,0)`
(안쪽 여백이 필요하면 `offsetMin/Max` 로). 그러면 칸 크기를 바꿔도 따라간다.
⚠️ **같은 프리팹을 레벨업 카드·TAB 격자도 쓴다** — 거기서는 아이콘이 76 칸을 꽉 채우게 되므로
여백을 얼마로 둘지는 **그 두 화면도 같이 보고** 정해야 한다.

## ✅ 해결 (D91, 2026-09-06) — **두 세션이 따로 같은 결론에 닿았다**

🔑 **`D91` 은 이 문서를 못 보고 고쳤다.** 사용자가 *"아이템이 제대로 테두리 안에 안들어가"* 라고
지적해서 프리팹을 열어 봤고, **원인도 고치는 법도 위와 같았다.**
서로 다른 입구(화면 캡처 / 사용자 지적)로 들어와 **같은 `m_SizeDelta: {x: 64, y: 64}`** 에 닿았다.

| | `D88`(이 문서) | `D91`(고친 쪽) |
|---|---|---|
| 발견 경위 | `/screen-check` 확대본 — **지적 밖에서** | 사용자 지적 |
| 원인 | `Icon` `sizeDelta 64x64` 고정 · 앵커 위-가운데 | **같음** |
| 고치는 법 | 부모에 맞춰 늘리고 `offsetMin/Max` 로 여백 | **같음** (여백 **8** → 아이콘 34x34) |
| 판정 | **렌더 픽셀** — 칸 2px 밖 비배경 픽셀 수, **빈 칸 3개를 대조군**으로 | `RectTransform` 사방 여백 ≥ 0 · **13/13** |

### 🔑 이 문서가 더 정확했던 것 두 가지

1. **"`D87` 이 만든 게 아니라 드러낸 것"** — `D91` 커밋은 *"`D90` 이 테두리를 분리한 덕에 드러났다"* 로만 적었는데,
   그건 **테두리를 덮는다**는 현상의 시작점일 뿐이다. **넘침 자체는 칸이 36 이던 시절에 사방 14px 로 더 심했다.**
   ⇒ 이쪽 서술이 맞다.
2. **판정 방법** — `D91` 은 `RectTransform` 좌표만 비교했다. 그건 *"의도한 사각형이 안에 있나"* 이지
   *"그려진 픽셀이 안에 있나"* 가 아니다. 이 문서는 **실제 렌더 픽셀**을 셌고,
   🔴 **첫 측정이 AA 때문에 빈 칸까지 22px 로 나온 것을 대조군이 잡았다.**

### 🔴 반대로 이 문서를 한 곳 정정한다 — **쓰는 화면이 넷이다**

*"레벨업 카드·TAB 격자도 쓴다"* 고 적혀 있는데, guid `f3a9e597...` 를 실제로 참조하는 곳은 이렇다.
**레벨업 카드는 이 프리팹을 안 쓴다.**

| 필드 | 스크립트 | `SetCompact` | `D91` 영향 |
|---|---|---|---|
| `itemChipPrefab` | `HUDManager` | `true` | ✅ 아이콘을 칸에 맞춤 + 테두리 |
| `chipPrefab` | `StatsPanelUI` (TAB) | 안 부름 | 그대로 |
| `itemChipPrefab` | `RunEndUI` | `false` | 그대로 (조기 반환) |
| `itemIconPrefab` | `StageClearUI` | 안 부름 | 그대로 |

⇒ ⚠️ 로 적힌 *"여백을 얼마로 둘지 그 화면들도 같이 보고 정해야 한다"* 는 걱정은
**`D91` 이 HUD 로만 범위를 좁혀서 해소됐다** — 나머지 셋은 프리팹 값(64 · 위쪽 정렬) 그대로다.
`SetCompact(bool)` 이 이미 `HUDManager` 에서만 `true` 로 불리고 있어 **새 플래그 없이** 갈렸다.

> 🔑 **병렬 세션의 교훈** — 같은 결함을 두 번 진단하는 낭비가 있었지만, 두 판정 방법이 달라
> **서로를 검산했다.** 다만 `D91` 이 착수 전에 `BUGS.md` 를 봤으면 원인 추적은 건너뛸 수 있었다.
> ⇒ **UI 를 고치기 전에 `BUGS.md` 를 먼저 훑는다.**

---

## B15 — 🔴 **`ExpDrop.Collect` 이 `NullReferenceException` 을 던진다** (사용자 플레이 중 실측)

**언제:** 2026-09-05 00:13:50~51 · 사용자 플레이테스트 중 · **2건**

```
NullReferenceException: Object reference not set to an instance of an object
ExpDrop.Collect () (at Assets/Scripts/Experience/ExpDrop.cs:125)
ExpDrop.Update ()  (at Assets/Scripts/Experience/ExpDrop.cs:118)
```

125번 줄은 `ExperienceManager.Instance.CollectXp(_amount)` 다.
⇒ **`ExperienceManager.Instance` 가 null 인 순간에 구슬이 회수됐다.**

🔴 **결과가 조용하지 않다** — 예외가 `Collect()` 중간에서 터지므로 `_collected = true` 는 이미 섰지만
**풀 반납(`SharedPool.Return`)이 안 된다.** 구슬이 살아 있는 채로 화면에 남고, XP 도 안 들어간다.

## ✅ 해결 (D84, 2026-09-06)

🔑 **`B14` 와 같은 가족이다** — `ExperienceManager.Instance` 는 **정적 필드**라
플레이 중 재컴파일(도메인 리로드) 뒤에 null 이 되고 `Awake` 는 다시 안 돈다.

🔑 **고침의 핵심은 예외를 없애는 게 아니라 순서다.** `_collected = true` 가 먼저 서 있으므로
예외가 나면 **반납이 건너뛰어지고 다음 프레임엔 바로 돌아간다** — 구슬이 영원히 남는다.
⇒ XP 지급을 가드로 감싸 **반납이 예외와 무관하게** 돌게 했다. 매니저가 없으면 경고만 남기고 반납한다.

🟢 **같은 자리에서 세 번째를 찾았다** — `PlayerStats.Die()` 가 `GameManager.Instance.OnPlayerDied()` 를
가드 없이 부르고 있었다. `B14` 재현 중에 실제로 터졌다. 죽음은 핵심 경로라 여기서 예외가 나면
**정산·클리어 화면이 통째로 안 뜬다**(`I-24` 와 같은 모양). 같이 고쳤다.

---

## B14 — 🔴 **`EnemyVisual` 이 매 `LateUpdate` 마다 예외를 던진다** (사용자 플레이 중 47건)

**언제:** 2026-09-05 00:13:49~57 · 사용자 플레이테스트 중 · **8초 동안 47건**

```
ArgumentNullException: Value cannot be null.  Parameter name: dest
UnityEngine.Renderer.GetPropertyBlock (MaterialPropertyBlock properties)
EnemyVisual.ApplySpriteRect (Sprite s) (at Assets/Scripts/Enemy/EnemyVisual.cs:143)
EnemyVisual.StepFrames ()               (at Assets/Scripts/Enemy/EnemyVisual.cs:176)
EnemyVisual.LateUpdate ()               (at Assets/Scripts/Enemy/EnemyVisual.cs:160)

... 그리고 같은 예외가 UpdateFlash() (212) 에서도
```

`dest` = `_sr.GetPropertyBlock(_mpb)` 의 `_mpb` 다. ⇒ **`_mpb` 가 null 인데 `_sr` 은 살아 있다.**

`LateUpdate` 의 가드는 `if (_sr == null) return;` **하나뿐이라** 이 상태를 못 막는다:

```csharp
private void LateUpdate()
{
    if (_sr == null) return;      // 🔴 _mpb 는 안 본다
    UpdateFacing(); StepFrames(); UpdateFlash();
}
```

## ✅ 원인 확정 · 해결 (D84, 2026-09-06)

🔑 **빠뜨린 조건은 "플레이 중 재컴파일"이었다.** 에디터 기본값이 *Recompile And Continue Playing* 이라
플레이 도중 C# 을 고치면 **도메인 리로드**가 돈다. 그때:

```
_sr   (UnityEngine.Object 참조)  ->  복원된다
_mpb  (순수 C# 객체)             ->  null 이 된다
Awake / Setup                    ->  다시 안 돈다
```

⇒ 가드가 `_sr` 만 봐서 그 상태를 통과했고 `GetPropertyBlock(null)` 이 불렸다.
**두 필드가 "같이 사라진다"는 게 내 잘못된 가정이었다.**

🟡 **그래서 성격이 다르다** — **빌드된 게임에서는 안 난다**(리로드가 없다).
개발 중에만 나지만, 콘솔을 뒤덮어 **진짜 에러를 가린다.** 사용자 플레이에서 47건이 난 것도
그 옆에서 DEV 가 C# 을 고치고 있었기 때문이다.

**고침** — 가드를 하나 더 다는 대신 **지연 생성 프로퍼티**로 바꿨다. 리로드 뒤에도 **스스로 낫는다.**
`EnemyBase` 는 이미 이 방식이었다. `PlayerVisual` 도 같은 모양이라 **함께** 고쳤다(아직 안 터졌을 뿐).

**판정** — 적 9마리가 살아 있는 상태에서 **플레이 중 재컴파일을 일으켜** 도메인 리로드를 통과시켰다.
`ArgumentNullException` **0건**.

⚠️ **비용이 크다** — 적 하나가 프레임마다 예외를 두 번 던진다. 적이 수백 마리인 장르에서
이건 성능 문제이기도 하다(`TODO §1-B` 의 성능 항목과 같이 볼 것).

🟡 **`D79` 가 원인일 가능성은 낮다** — `D79` 는 `PlayerVisual` 과 셰이더를 건드렸고
`EnemyVisual` 은 안 건드렸다. 다만 `SizeScale` 변경으로 적이 더 많이/크게 보이게 된 것과
**시점이 겹치므로** 그 이전 판에서도 났는지 확인이 필요하다.

---

## B13 — 🟡 **`base`/`meta` 의 `Luck` 이 최종 스탯에 안 들어간다** (미확인 · 코드로 확정)

**증상(예측):** `Economy.csv` 에 `PlayerStats,baseStats.Luck` 행을 넣거나
메타 업그레이드에 `Luck` 을 추가하면 **그 값이 아무 일도 안 한다.**

> ⚠️ **아직 안 터진다.** 메타 업그레이드에 `Luck` 이 없고(`Upgrades.csv` 의 `StatKey` 7종에 없다)
> `Economy.csv` 에 `baseStats.Luck` 행도 없다. **둘 중 하나만 생기면 그 순간 터진다.**

### 원인 — `RecalculateStats` 가 필드별 나열이다

`PlayerStats.RecalculateStats` 는 `Final` 을 이렇게 만든다.

```csharp
Final = new StatBlock
{
    MaxHp = baseStats.MaxHp + meta.MaxHp,
    ...
    BuildingCooldown = baseStats.BuildingCooldown + meta.BuildingCooldown,
    HpRegen          = baseStats.HpRegen + meta.HpRegen,   // D74 에서 추가
    // 🔴 Luck 이 없다
};
```

`Luck` 이 목록에 없으므로 **기본 생성자 값(0)** 이 그대로 남는다.
패시브의 `Luck` 은 뒤이어 `p.Apply(Final)` 이 더하므로 **패시브만 작동한다** —
그래서 지금까지 안 걸렸다.

### 🔑 이 함정은 반복된다

`D74` 가 `HpRegen` 을 넣으면서 **똑같이 밟았다.** `StatBlock` 에 필드를 더하고
`PassiveEffect.Apply` 까지 고쳤는데 `Final.HpRegen` 이 **0** 이었다 —
이 나열에서 빠졌기 때문이다. 런타임 판정이 아니었으면 못 봤다.

| 안 | 무엇 | 대가 |
|---|---|---|
| **A** | `Luck` 한 줄을 더한다 | 다음 필드에서 또 밟는다 |
| **B** | `StatBlock.Add(a, b)` 같은 합산 메서드를 두고 세 곳(여기 · `PassiveEffect` · `CharacterClassData`)이 같이 쓴다 | 손대는 파일이 늘어난다 |

🔑 **B 가 맞아 보인다** — 이 나열은 지금까지 **두 번** 사람을 속였다(`Luck`·`HpRegen`).
다만 스탯 합산은 게임 전체가 매달린 자리라 **바꾸면 전 스탯을 다시 재야 한다.**

⚠️ **사용자 결정 없이 안 고쳤다** (`CLAUDE.md` §1). 발견 경위는 `SETUP_STATUS.md` 2-100.

---

## B12 — ✅ **해결됨 (D71, 2026-09-05)** · 슬롯이 꽉 차면 상점에서 골드만 사라졌다

**증상(예측):** 무기/패시브/건물 칸이 꽉 찬 상태에서 그 카테고리의 **새 아이템**을 상점에서 사면
**골드는 빠지고 아이템은 안 들어온다.** 그리고 콘솔에는 **구매 성공으로 찍힌다.**

> ⚠️ **아직 실제로 목격한 적은 없다.** 2026-09-05 실플레이 3판에서 상점을 **3번 열었는데
> 구매가 0건**이라 이 경로를 아무도 안 밟았다. 코드를 읽다가 찾았다.

**재현 절차 (미시도):**
1. 무기 칸을 꽉 채운다 (Warrior 는 3칸)
2. 상점 노드에 들어가 **안 가진 무기**를 산다
3. 골드가 줄었는데 무기가 안 늘어난다

### 🔴 원인 — `CanAcquire` 가 상점 어디에도 없다

레벨업 카드 풀은 이 검사를 **한다** — `LevelUpManager.cs:263`
```csharp
if (!CanAcquire(item)) continue;   // 꽉 찬 카테고리의 신규 아이템은 카드에 띄우지 않는다
```

그런데 상점은 **세 곳 다 안 한다:**

| 곳 | 지금 | 있어야 할 것 |
|---|---|---|
| `ShopManager.RollShopSlots` | 재고를 아무 아이템에서나 뽑는다 | 못 받는 아이템은 **진열하지 말아야** 한다 |
| `ShopCardUI.cs:80` | `buyButton.interactable = 골드 >= 가격` | **골드만 본다.** 슬롯 여유를 안 본다 |
| `ShopManager.Purchase` | 아래 순서 | — |

```csharp
public void Purchase(ShopSlot slot)
{
    if (!CanPurchase(slot)) return;          // 골드/품절만 본다

    GameManager.Instance.SpendRunGold(slot.Price);   // ① 골드가 먼저 빠지고
    slot.IsSold = true;                              // ② 품절 처리까지 된 뒤에

    GameManager.Instance.LevelUpManager.ApplyItemFromShop(slot.Item);
    //                                   └─ ApplyItem 안에서 `if (!CanAcquire(item)) return;`
    //                                      ③ 조용히 아무것도 안 한다

    Debug.Log($"[ShopManager] 구매: ...");   // ④ 그래도 성공이라고 찍는다
}
```

🔴 **셋이 겹쳐서 조용해진다.** 버튼이 막지 않고, 거절이 로그를 안 남기고,
성공 로그는 그대로 찍힌다 — **로그만 보면 정상 구매다.**

### 🔑 왜 지금까지 안 걸렸나

`ApplyItem` 의 그 `return` 은 원래 **레벨업 경로를 막으려고** 넣은 것이다
(*"보유 목록에는 있는데 무기는 없는"* 상태를 막은 D18 계열 수정).
레벨업 쪽은 **카드에 아예 안 띄우는** 방어가 위에 하나 더 있어서 그 `return` 까지 안 간다.
**상점만 그 방어가 없이 같은 `return` 을 공유한다.**

> ✅ **`D71` 에서 닫혔다.** `Purchase` 가 **줄 수 있는지 먼저 묻고**, 못 주면 골드에 손대지 않는다.
> 판정 대조군: 만렙 아이템 구매 시도에 **골드 2000 → 2000 그대로**, `IsSold=False`, 경고 로그.
> 진열도 `CanAcquire(i) && !i.IsMaxLevel` 로 거른다 — `CanAcquire` 만 쓰면 **만렙까지 통과한다.**
> 상세는 `SETUP_STATUS.md` 2-97.

### ✅ 사용자 결정 (2026-09-05) — **D안: 못 사는 상황이면 진열을 갈아 끼운다**

> 사용자: *"아이템 슬롯 제한에 걸려서 더이상 구매 못하면 힐템이나 골드 이전(런 골드 전환) 만 뜨는식으로 하면되"*

🔑 **아래 A~C 와 결이 다르다.** 셋 다 *"못 사게 막는다"* 인데, D안은 **살 게 없으면 다른 걸 판다** 다.
막기만 하면 상점 노드를 밟은 게 통째로 헛수고가 되는데, 이쪽은 **상점이 계속 쓸모 있다.**

**필요한 것:**

| 무엇 | 어디 |
|---|---|
| `RollShopSlots` 가 `CanAcquire` 로 후보를 거른다 | `ShopManager` (= A안) |
| 후보가 **하나도 없으면** 대체 상품으로 채운다 | 신설 |
| 대체 상품 ① **회복** — 골드로 체력을 산다 | `HealPickup` 은 있지만 상점 상품은 없다 |
| 대체 상품 ② **런 골드 → 메타 골드 전환** | `EventManager` 의 `EventKind.Exchange`(E7)가 이미 하는 일이다 — 규칙을 빌려 온다 |

🔴 **`ShopSlot` 이 지금은 `ItemData` 만 들 수 있다.** 회복·전환은 아이템이 아니라 **서비스**라
슬롯에 종류가 하나 붙어야 한다(`ShopSlotKind { Item, Heal, Exchange }`).
그게 이 작업의 실제 크기다 — `ShopCardUI` 도 아이템이 아닌 칸을 그릴 줄 알아야 한다.

✅ **`D71` 에서 전부 만들었다.** `ShopSlotKind { Item, Heal, Exchange }` · `MakeHealSlot` · `MakeExchangeSlot` · `ShopCardUI.SetupService`.

---

### (참고) 처음에 적었던 세 안 — 전부 "막는" 쪽이라 D안에 밀렸다

| 안 | 무엇 | 대가 |
|---|---|---|
| **A** | `RollShopSlots` 가 `CanAcquire` 로 거른다 | 진열이 비는 판이 생길 수 있다 |
| **B** | `ShopCardUI` 가 버튼을 막는다 | 왜 못 사는지 안 보인다 — 문구가 필요하다 |
| **C** | `Purchase` 가 `ApplyItem` **성공 여부를 받아** 실패 시 골드를 되돌린다 | `ApplyItem` 이 `void` 라 반환형을 바꿔야 한다 |

🔑 **A + B 를 같이 하는 게 맞아 보인다** — 못 살 물건은 애초에 안 뜨고,
그래도 뜬 경우(레벨업으로 방금 칸이 찼다면) 버튼이 막힌다.
**C 는 안전망**이라 셋 다 해도 손해가 없다.

⚠️ **사용자 결정 없이 안 고쳤다** (`CLAUDE.md` §1). 발견 경위는 `SETUP_STATUS.md` 2-87.

---

## B11 — 상점 NPC 대사가 전부 빈칸으로 나온다 (`B4` 가 놓친 자리)

**증상:** 상점을 열면 오른쪽 `MERCHANT` 칸의 대사 3줄이 **전부 `□`** 다.
콘솔에 TMP 경고가 **25건** 찍힌다.

```
The character with Unicode value 좋 was not found in the [Pretendard SDF]
font asset or any potential fallbacks. It was replaced by Unicode character □
in text object [NpcDialogueText].
```

**재현 절차:** 상점 노드에 들어간다(또는 `ShopManager.OpenShop`). 오른쪽 패널을 본다.

**🔴 원인 (실측 확정, 2026-09-04) — 코드는 이미 영문이다. 씬이 코드를 덮고 있다.**

`ShopUI.cs:47` 의 **코드 기본값은 영문**이다:

```csharp
[SerializeField] private string[] npcDialogues = new[]
{
    "Only the finest goods here.
Nothing you fancy?
I can reroll the lot.",
    ...
};
```

그런데 `[SerializeField]` 라서 **씬에 저장된 값이 이깁니다.** 씬을 실측하니 5줄이 전부 한글이었다:

```
[0] 🔴 한글 | 좋은 물건만 골라왔지. / 마음에 드는 게 없으면 / 리롤도 해드릴 수 있어…
[1] 🔴 한글 | 오늘 특가! 빨리 사지 않으면 / 다음 손님이 가져가지.
[2] 🔴 한글 | 뭔가 찾고 있나? 내가 도와줄 수 있어. / 물론 공짜는 아니지만.
[3] 🔴 한글 | 강한 적들이 기다리고 있다네. / 준비를 단단히 하게나.
[4] 🔴 한글 | 제거 비용은 저렴해. 짐은 가볍게 / 다니는 게 좋다고 생각하거든.
```

폰트는 `I-60` 이후 `Pretendard SDF` = **Static · 115자**(ASCII + 기호)라 **한글 글리프가 없다.**

> 🔑 **`B4` 와 같은 병이고, `B4` 가 이 자리를 놓쳤다.**
> `B4` 는 *"씬에 한글이 남아 있다"* 로 진단하고 `D5`(28차)에서 **씬 6곳**을 영문화해 닫았는데,
> 그 6곳은 전부 **`TextMeshProUGUI.text`** 였다. 이건 **`string[]` 배열 필드**라
> 텍스트 컴포넌트를 훑는 방식으로는 안 잡힌다.
>
> ⚠️ **그래서 이 자리는 지금도 더 있을 수 있다.** 닫을 때 `text` 뿐 아니라
> **모든 `string`/`string[]` 직렬화 필드**를 한글 코드포인트(U+AC00~U+D7A3)로 훑어야 한다.

**고치는 법 (아직 안 했다):** 씬의 `ShopUI.npcDialogues` 5칸을 코드 기본값과 같은 영문으로 덮는다.
코드는 손댈 필요가 없다. 🔴 **씬 파일이라 DEV 몫**이다.

> `CLAUDE.md` §1 *"작업 중 새 문제를 발견했을 때 — 임의로 고치지 말고 먼저 적고 사용자에게 알린다"*
> 에 따라 `D40` 에서는 **등재만 하고 손대지 않았다.**

### ✅ 수정 — D43 (2026-09-04) · **씬만 고쳤으면 부활했을 것이다**

사용자 지시로 영문화했다. 씬의 `ShopUI.npcDialogues` 5칸을 코드 기본값과 같은 문자열로 덮었다.
**코드는 손대지 않았다** — 원래 영문이었다.

```
[B11] 한글 줄 5 -> 0
[B11-V] 화면에 실제로 들어간 대사 = "Stronger foes are waiting. / Come prepared, friend."
```

🔴 **그런데 씬만 고치면 다시 살아난다.** 고친 뒤 씬 전체를 훑었더니 0건이었는데,
**프리팹까지 훑으니 `Assets/GameObjects/UI Canvas.prefab` 원본에 한글 5줄이 그대로 있었다.**
씬의 `UI Canvas` 는 그 프리팹의 인스턴스라 내가 고친 것은 **오버라이드**였다 —
누가 오버라이드를 되돌리거나 프리팹을 다시 인스턴스화하면 **한글이 그대로 부활한다.**
⇒ 프리팹 원본도 같이 덮었다.

**전수 조사 결과 (스캐너에 대조군을 넣었다):**

| 대상 | 문자열 필드 | 한글 |
|---|---|---|
| 씬 (컴포넌트 624개) | 263 | **0** |
| 프리팹 61개 | 69 | **0** (수정 전 5) |

> 🔑 **스캐너가 배열 안까지 보는지를 먼저 증명했다.** `SerializedProperty` 순회가
> `string[]` 원소를 안 들여다보면 *"한글 0건"* 은 **깨끗하다는 뜻이 아니라 못 봤다는 뜻**이다.
> 그래서 방금 넣은 영문 문자열(`"For a price"`)을 **대조군**으로 같이 찾게 했고,
> `ShopUI.npcDialogues.Array.data[2]` 로 잡히는 것을 확인한 뒤에야 0건을 믿었다.
> — `B4` 가 *"씬 6곳 다 고쳤다"* 로 닫히고도 이 자리를 놓쳤던 이유가 바로 이것이다.

---

## B4 — 일시정지·옵션창 글자가 빈칸으로 나온다

**증상:** 일시정지 패널과 옵션 패널의 글자 **6곳이 화면에 안 보인다**(빈칸).
버튼 자체는 눌리므로 배선은 멀쩡하다. **글자만** 안 그려진다.

**재현 절차:** Wave 중 `ESC` → 일시정지 패널의 제목·Resume·Option·Quit 이 전부 빈칸.
Option 을 눌러 열리는 패널의 제목·Close 도 빈칸.

**원인 (확정):** 씬에 **한글 문자열이 남아 있다.** 폰트는 23차(I-60)부터
`Pretendard SDF.asset` = **Static · 115자**(ASCII 32~126 + 기호 20)라 **한글 글리프가 없다.**
문자표에 없는 글자는 빈칸으로 나온다 — I-60 이 남긴 알려진 제약이다.

콘솔에도 그대로 찍힌다:

```
The character with Unicode value \uC635 was not found in the [Pretendard SDF]
font asset or any potential fallbacks. It was replaced by Unicode character \u25A1
```

(`\uC635`=옵, `\uC158`=션, `\uC885`=종, `\uB8CC`=료, `\uB2EB`=닫, `\uAE30`=기 …)

**⚠️ `CLAUDE.md` §3 위반이기도 하다** — "UI에 표시되는 문자열은 영문으로 쓴다".
즉 이건 폰트 문제이기 전에 **씬에 한글이 들어간 것 자체가 규칙 위반**이다.

**고치는 방향 — 영문화로 확정 (사용자, 2026-08-30).**
폰트를 다시 굽지 않는다. 한글 6곳을 영문으로 바꾼다. 근거:

- 이 6개를 위해 한글 글리프를 넣으면 아틀라스가 다시 커진다 (I-60 이 55MB→4.8MB 로 줄인 걸 되돌린다)
- 씬의 **다른 곳은 이미 전부 영문**이다. 이 6곳만 빠진 것이다
- 쓸 단어가 이미 씬 안에 있다 — `Continue`(11297행) · `Options`(12947행) · `Quit`(481행) · `CLOSE`(1577행)

**🔴 고칠 위치 — `Assets/Scenes/SampleScene.unity` (DEV 소유).**
경로는 하이어라키를 직접 파싱해 뽑았다. 전부 `UI Canvas` 아래다.

| 씬 행 | 현재 (이스케이프) | 읽으면 | 하이어라키 경로 | **바꿀 값** |
|---:|---|---|---|---|
| 12562 | `"\uC77C\uC2DC\uC815\uC9C0"` | 일시정지 | `PausePanel/Card/TitleText` | `PAUSED` |
| 13296 | `"\uACC4\uC18D\uD558\uAE30"` | 계속하기 | `PausePanel/Card/ResumeButton/Label` | `Continue` |
| 3141 | `"\uC635\uC158"` | 옵션 | `PausePanel/Card/OptionButton/Label` | `Options` |
| 5364 | `"\uC885\uB8CC"` | 종료 | `PausePanel/Card/QuitButton/Label` | `Quit` |
| 4186 | `"\uC635\uC158"` | 옵션 | `OptionSubPanel/Card/TitleText` | `OPTIONS` |
| 4444 | `"\uB2EB\uAE30"` | 닫기 | `OptionSubPanel/Card/CloseButton/Label` | `CLOSE` |

> ℹ️ 제목 2개만 대문자다. 씬의 다른 패널 제목이 대문자라 맞춘 것이다
> (`SELECT CLASS` · `LEVEL UP` · `MERCHANT` · `SHOP`). 버튼 라벨은 `Continue`/`Options`/`Quit`
> 처럼 첫 글자만 대문자 — 이것도 씬의 기존 버튼(`Retry` · `Start` · `Back` · `Reroll`)과 같은 규칙이다.

**판정 기준:** `ESC` → 6곳이 **전부 읽히는 영문**으로 보이고,
콘솔에 `was not found in the [Pretendard SDF] font asset` 경고가 **0건**.

> ⚠️ **행 번호로 찾지 말 것.** 씬을 저장하면 밀린다. `m_text` 값이나 하이어라키 경로로 찾을 것.
> 인스펙터에서 고쳐도 되고 YAML 을 직접 고쳐도 된다 (`.unity` 는 DEV 소유).

### ✅ 수정·검증 완료 — D5 (2026-08-30, DEV)

6곳 전부 위 표대로 바꾸고 씬을 저장했다. `git diff --stat` = **6 insertions(+) / 6 deletions(-)**.

> ℹ️ **위 표의 행 번호는 실제로 밀려 있었다.** 실제 위치는 3278·4323·4581·5501·12699·13433 —
> 경고문이 그대로 맞았다. 하이어라키 경로로 찾아 6/6 을 정확히 잡았다.

판정 기준 결과:

| # | 기준 | 결과 |
|---|---|---|
| ① | 6곳이 영문 | 전부 `ascii=True` ✅ |
| ② | 폰트 문자표에 전부 있음 | 6곳 `HasCharacters=True · missing=[]` ✅ |
| ③ | 실제로 글리프가 그려짐 | `ForceMeshUpdate` 후 `characterCount == visible` (빈칸 0) ✅ |
| ④ | 🔴 실플레이 육안 | 캡처 2장 — `PAUSED/Continue/Options/Quit` · `OPTIONS/BGM/SFX/CLOSE` ✅ |
| ⑤ | 🔴 폰트 경고 0건 | 플레이 전체 로그 **7건 전부 `Log`** · Warning 0 · Error 0 ✅ |

상세는 [`DONE/D5.md`](DONE/D5.md).

> ⚠️ **씬에 한글이 또 들어오면 같은 버그가 재발한다.** 폰트는 여전히 Static 115자다.
> 새 UI 문자열을 넣을 때 `CLAUDE.md` §3 "UI 문자열은 영문" 을 지키면 된다.

---

## B5 — 웨이브 밖에서 적이 죽으면 NRE

**증상:**

```
NullReferenceException
  WaveManager.OnEnemyKilled (EnemyBase enemy)   (WaveManager.cs:139)
  EnemyBase.Die ()                               (EnemyBase.cs:421)
  EnemyBase.TakeDamage (single, nullable)        (EnemyBase.cs:374)
  ProjectileBase.OnTriggerEnter2D (Collider2D)   (ProjectileBase.cs:40)
```

**원인:** `WaveManager.cs:139`

```csharp
if (_currentWaveData.UseKillClear && _killCount >= _currentWaveData.KillTarget)
```

`_currentWaveData` 는 웨이브가 시작될 때 채워진다. 웨이브가 도는 중이 아닌데
적이 죽으면 **null 참조**다. `EnemyBase.Die()` 는 `GameManager.Instance?.WaveManager.OnEnemyKilled(this)`
로 부르는데 `?.` 는 `GameManager.Instance` 만 막아 준다.

**🔴 예외가 `Die()` 중간에서 터지므로 뒤가 통째로 안 돈다** — `PlayDeathImpact` · 사망음 ·
`DeathPopRoutine` 이 전부 건너뛰어져 **시체가 살아 있는 채로 화면에 남는다.**
(D9 검증 때 실제로 표적이 `active=True` 로 남았다.)

**재현 절차 (지금은 이 길뿐이다):**
1. 플레이 모드 진입 → `MainMenu` 상태 그대로 둔다 (웨이브 시작 안 함)
2. 풀에 있는 적을 하나 켜서 `Initialize(EnemyData)` 로 세운다
3. 그 적을 죽인다 → 위 예외

**⚠️ 지금 게임 경로로는 안 난다.** 적은 `WaveManager` 가 소환하고, 그때는 항상
`_currentWaveData` 가 있다. **그래서 우선순위가 낮다.** 다만 앞으로
"웨이브 사이에도 남아 있는 적" · "튜토리얼용 더미" · "웨이브 클리어 직후 잔존 적" 같은 게
생기면 **바로 터진다** — 사망 처리는 게임 내내 도는 경로라 여기서 멈추면 눈에 띈다.

**고치는 법 (한 줄):** `if (_currentWaveData != null && _currentWaveData.UseKillClear && …)`
🔴 **고치지 않았다.** D9 는 화살 임포트 작업이고 이건 그 범위 밖이다 (`SESSION_PROMPT.md` §5).

### 🔴 증상 추가 — 광역기가 첫 사망자에서 멈춘다 (2026-08-31, DEV / D20 검증 중)

위의 *"우선순위 낮음"* 은 **적 하나가 죽는 경우**만 본 판정이다. **여러 적을 한 번에 때리는
경로**에서는 피해가 훨씬 크다. D20 의 폭탄 픽업(`WorldPickup.DetonateBomb`)이 그렇다:

```csharp
foreach (var h in hits)          // ← 예외가 이 루프 밖으로 나간다
    enemy.TakeDamage(bombDamage, center);
```

`TakeDamage` → `Die()` → `OnEnemyKilled` NRE 가 **루프를 통째로 걷어찬다.** 결과:

| | 실제로 본 것 (플레이 모드, `MainMenu` 상태) |
|---|---|
| 뒤쪽 적 | Demon 이 죽는 순간 예외 → **같은 반경 안의 Ogre 는 피해를 아예 안 받았다** |
| 픽업 | `Collect()` 가 `Despawn()` 전에 끊겨 **폭탄이 바닥에 그대로 남는다.** `_collected=true` 라 다시 먹히지도 않는다 |

**같은 모양(`OverlapCircleAll` → `foreach` → `TakeDamage`)이 4곳 더 있다** — 전수로 찾아 확인했다:

| 파일 | 행 | 무엇 |
|---|---|---|
| `Weapon/AoeProjectile.cs` | 51·55 | 폭발 투사체 |
| `Weapon/MeleeWeapon.cs` | 69·77 | 근접 휘두르기 |
| `Weapon/SummonWeapon.cs` | 235·239 | 소환수 광역 공격 |
| `Weapon/ToxinField.cs` | 82·88 | 독장판 **(틱마다 돈다)** |

즉 웨이브 밖에서 광역기가 도는 상황이 생기면 **전부 같은 식으로 반쯤 먹는다.**
(`BuildingBase.cs:80` 은 루프 밖 단일 대상이라 이 증상이 없다.)

> ℹ️ 정상 경로(웨이브 중)에서는 여전히 안 난다. 다만 "우선순위 낮음"의 근거였던
> *"사망 처리 한 건이 덜 도는 정도"* 는 **틀렸다** — 광역기에서는 **다른 적들의 피해가 통째로 사라진다.**

### ✅ 수정됨 (D35, 2026-09-03) — 한 줄 가드 + 같은 모양 하나 더

`WaveManager.OnEnemyKilled` 에 `_currentWaveData != null` 을 넣었다.
그리고 **`SpawnMinion`(D31 에 내가 만든 공개 API)에도 같은 가드를 넣었다** — 같은 모양이었다.

**검증은 광역 사례로 했다** (그게 이 버그의 실제 피해다). 웨이브를 시작하지 않은 `MainMenu`
상태에서 Demon·Ogre 를 한 폭발 반경에 겹쳐 놓고 `OverlapCircleAll` → `foreach` → `TakeDamage`:

| | 결과 |
|---|---|
| 루프가 때린 수 | **2** (고치기 전이면 첫 사망자에서 예외가 나 **1** 에서 끊긴다) |
| Demon | `dead=true` · 사망 연출까지 돌고 **정상적으로 꺼졌다** |
| Ogre | **HP 26** = 220 − 200 + 방어 6 — **피해를 제대로 받았다** (안 고쳤으면 220) |
| 활성 시체 | **0구** (예외로 끊기면 시체가 살아 있는 채로 남는다) |
| 콘솔 `NullReference` | **0건** |

> ℹ️ `D31` 이 지적한 대로 **재현 창은 좁다** — `_currentWaveData` 는 `StartWave` 에서만 채워지고
> 절대 `null` 로 돌아가지 않으므로, 이 버그는 **한 세션의 첫 웨이브 전에만** 난다.
> 그래도 고친 이유는 사망 처리가 **게임 내내 도는 경로**이고, 여기서 멈추면
> "다른 적들의 피해가 통째로 사라지는" 형태로 드러나기 때문이다.

---

## B6 — 무기 칸이 하나 모자란다 (시작 무기가 장부에 없다)

**증상:** Dev 패널에서 무기 레벨을 내리거나 `+` 를 누르면 콘솔에

```
[WeaponManager] 무기 슬롯 꽉 참 (N/N) — <무기 이름>
```

경고가 뜨고 **무기가 안 들어온다.** 버튼은 `canAdd` 판정을 통과해서 **눌리는 상태**였다.
즉 UI 는 "된다"고 했는데 실행이 거절한다.

**재현 절차:**
1. 아무 직업으로 플레이 시작 (시작 무기 1자루가 자동 지급된다)
2. 레벨업/상점으로 무기를 **슬롯 상한까지** 더 얻는다
3. Dev 패널(`F1`)에서 아무 무기의 `-` 나 `Max` 를 누른다 → 위 경고

### 🔴 원인 확정 (2026-08-30, DEV) — **장부가 둘인데 서로 안 맞는다**

무기 개수를 세는 곳이 **두 군데**고, 세는 대상이 다르다.

| 세는 쪽 | 무엇을 세나 | 코드 |
|---|---|---|
| `LevelUpManager.CanAcquire` | `_inventory` 의 Weapon 항목 수 | `LevelUpManager.cs:251` |
| `WeaponManager.AddOrUpgradeWeapon` | `_weapons` 딕셔너리 크기 | `WeaponManager.cs:34` |

**시작 무기가 이 둘을 어긋나게 만든다.** `GameManager.cs:228` 이

```csharp
WeaponManager.Instance?.AddOrUpgradeWeapon(cls.StartingWeapon, ...);
```

으로 **`WeaponManager` 에 직접** 넣는다. `LevelUpManager._inventory` 는 거치지 않는다.
그래서 매 판 시작부터 `_weapons.Count == CountOwned(Weapon) + 1` 이다.

상한이 `N` 일 때 인벤토리 무기가 `N-1` 자루면 `CanAcquire` 는 **아직 자리가 있다**고 답하지만
`_weapons` 는 이미 `N` 이라 `AddOrUpgradeWeapon` 이 경고를 찍고 `return` 한다.

**⚠️ 이건 `WeaponManager.cs:29~32` 주석이 "다시 생기면 로그로 드러나야 한다"고 적어 둔
바로 그 상태다.** 로그는 제 일을 했다 — 이제 원인을 막을 차례다.

> ℹ️ **Dev 패널은 범인이 아니라 목격자다.** `DevPanel.SetLevel`(`DevPanel.cs:228`)이
> `RemoveItemFull` → `ApplyItemFromShop` × N 으로 레벨을 재구성하기 때문에
> 이 어긋남이 한 번에 여러 번 드러났을 뿐이다. **레벨업 카드로도 같은 일이 난다.**

**고치는 방향 — 아직 안 골랐다. 사용자 판단이 필요하다.**

| 안 | 내용 | 대가 |
|---|---|---|
| **A** | 시작 무기도 `LevelUpManager` 를 거쳐 지급한다 | 장부가 하나로 합쳐진다. 시작 무기가 상점 환급·진화 대상이 되는지 같이 봐야 한다 |
| **B** | `CanAcquire` 가 `_inventory` 대신 `WeaponManager._weapons` 를 센다 | 작다. 대신 무기만 규칙이 달라진다 |
| **C** | 시작 무기를 슬롯 상한에서 면제한다 (상한 +1) | 밸런스가 바뀐다 — 실질 무기 칸이 한 칸 는다 |

🔴 **고치지 않았다.** D15 는 외곽선 작업이고 이건 그 범위 밖이다 (`SESSION_PROMPT.md` §5).

---

## B7 — 승급 4종 그림이 뭉개진 채로 들어와 있다 (임포트 설정만 다르다)

**증상:** 승급 직업 4종(`Sentinel`·`Doomlord`·`Warden`·`Aegis`)의 초상·걷기 시트가
T1 3종(`Warrior`·`Ranger`·`Mage`)보다 **흐릿하고 색 경계에 압축 잡티가 있다.**
예외도 경고도 나지 않는다 — **설정값만 다르다.**

**재현 절차:** `Assets/Game/Sprites/Classes/` 에서 `Warrior.png` 와 `Sentinel.png` 를
인스펙터로 나란히 열고 `Filter Mode` · `Compression` · `Max Size` 를 비교한다.

### 실측 (2026-08-31, D26 작업 중)

| | PPU | `maxTextureSize` | `filterMode` | 압축 |
|---|---:|---:|---|---|
| `Warrior` · `Ranger` · `Mage` | 1024 | 512 | `Point` | 없음 |
| `Sentinel` · `Doomlord` · `Warden` · `Aegis` | 1024 | 🔴 **2048** | 🔴 **`Bilinear`** | 🔴 **켜짐** |

걷기 시트도 같은 상태다.

### 원인

`I-61` 이 이 4종의 **PPU 만** 규약값으로 고치고 `filterMode`·`textureCompression`·
`maxTextureSize` 를 손대지 않았다. PPU 가 맞으니 **크기는 정상으로 보이고**,
차이는 선명도로만 드러나서 눈에 안 띈 것이다.

> 🟡 유효 `maxTextureSize` 는 `.meta` 최상단 값이 아니라
> `platformSettings[buildTarget: DefaultTexturePlatform]` 안의 값이다.
> 최상단만 읽으면 T1 3종도 2048 로 보인다 — **여기서 한 번 틀렸다.**

**고치는 법은 이미 있다.** D26 에서 T1 3종과 신규 3종에 쓴 `Unity_RunCommand` 를
경로만 바꿔 4종에 돌리면 된다 (`GetDefaultPlatformTextureSettings` → `maxTextureSize`·
`textureCompression` 설정 → `SetPlatformTextureSettings` → `SaveAndReimport`).

### ✅ 수정됨 (D35, 2026-09-03)

`D26` 에서 T1 3종에 쓴 명령을 경로만 바꿔 승급 4종의 **초상 + 걷기 시트**에 돌렸다.
결과 — **7종이 전부 같은 설정으로 수렴했다:**

```
초상      Warrior/Ranger/Mage/Sentinel/Doomlord/Warden/Aegis
          ppu=1024 maxTS=512  filter=Point comp=Uncompressed loaded=512x512
걷기 시트 (같은 7종)
          ppu=256  maxTS=1024 filter=Point comp=Uncompressed loaded=1024x1024
```

🔑 **`filterMode` 만 보면 안 된다** — `maxTextureSize` 와 압축도 같이 틀어져 있었고,
그 둘은 `.meta` 최상단이 아니라 `platformSettings[DefaultTexturePlatform]` 안에 있다(D26 의 함정).
`I-61` 이 PPU 만 고치고 지나간 것도 같은 이유로 보인다.

---

## B8 — 적이 뭉치면 무리 분리가 스스로 약해진다 (이웃 12칸 무언 절단)

**증상:** 적이 빽빽하게 몰릴수록 서로 밀어내는 힘이 **약해진다.** 예외도 경고도 안 난다.
겉보기에는 "많이 모이면 겹쳐 보인다" 라 *체감* 문제로 오해하기 쉽지만,
**"모든 이웃이 분리 계산에 들어가는가?"** 라는 예/아니오 질문이라 여기 적는다.

### 코드상 근거 (2026-09-02, DEV / D27 조사 중)

`Assets/Scripts/Enemy/EnemyBase.cs`

```csharp
private static readonly Collider2D[] NeighborBuf = new Collider2D[12];
...
int n = Physics2D.OverlapCircle(pos, r, _enemyFilter, NeighborBuf);   // :244
```

`Collider2D[]` 를 받는 이 오버로드는 **배열을 늘리지 않는다.** 반환값 `n` 은 `NeighborBuf.Length`
에서 잘린다 (`List<Collider2D>` 오버로드는 늘어나지만 그건 매번 할당한다). 결과:

| | |
|---|---|
| 상한 | **12개.** 그중 하나는 **자기 자신**이라 실질 이웃은 **11마리** |
| 넘치면 | 조용히 버려진다. `n == 12` 인지 확인하는 코드가 **없다** |
| 어느 것이 버려지나 | **모른다.** Unity 는 이 오버로드의 순서를 보장하지 않는다 — 거리순이 아니다 |

마지막 줄이 핵심이다. 분리 힘은 `sum += away / (d * d)` 로 **가까울수록 압도적으로 크다**(`:259`).
그런데 버려지는 대상이 임의라서 **가장 가까운 = 가장 세게 밀어내야 할 이웃이 빠질 수 있다.**

### 왜 자기강화 루프인가

```
적이 뭉친다 → 0.85 반경 안 이웃이 12를 넘는다 → 일부가 계산에서 빠진다
   → 분리 벡터가 실제보다 작아진다 → 덜 밀어낸다 → 더 뭉친다 ↺
```

즉 **분리가 가장 필요한 순간에 정확히 그 기능이 꺼진다.** 적이 적을 때는 절대 안 보인다.

### 🔑 이건 성능 우회책 안에 들어 있던 결함이다

`:225-227` 주석이 말하듯 이 코드의 목적은 **질의 횟수를 줄이는 것**이었다
(개체마다 위상을 어긋내 4스텝에 1회). 고정 버퍼도 같은 이유 — **할당을 없애려고** 쓴 것이다.
그 최적화의 부산물로 **정합성 상한이 생겼는데 아무도 그걸 상한이라고 적지 않았다.**

> ⚠️ 12 는 취향값이 아니다. "이웃을 몇 마리까지 볼지"를 정한 게 아니라
> **"배열을 몇 칸으로 할지"** 를 정한 값이 우연히 판정 규칙이 된 것이다.
> 그래서 `TUNING.md` 가 아니라 여기다.

### 고치는 법 — 세 갈래

| 안 | 방법 | 대가 |
|---|---|---|
| **A** | 버퍼를 32~64 로 키운다 | 한 줄. 근본은 안 고쳐지고 **상한만 옮긴다.** 질의 비용도 그대로 |
| **B** | `n == Length` 일 때 경고를 찍는다 | 진단만. **증상은 그대로** — 다만 "언제 넘치나"를 측정할 수 있다 |
| **C** | 물리 질의를 걷어내고 **공간 해시**로 이웃을 찾는다 | 상한이 사라지고 초당 ≈2,500회 질의도 없어진다. **범위가 크다** |

🔴 **아직 안 골랐다.** 다만 `D27` 이 성능 작업으로 **C** 를 보고 있고, C 를 하면 이 버그가 **같이** 닫힌다.
**B 를 먼저 넣어 "실제로 12를 넘기는가"를 측정하는 것이 순서다** — 안 넘긴다면 C 의 근거가 약해진다.

### ✅ 실측 확인 (2026-09-02, D27) — **추론이 맞았다**

`EnemyBase` 에 계측을 넣고 `n` 의 히스토그램을 찍었다. 상세는 [`../PERF.md`](../PERF.md) §7-C.

| 적 수 | 포화(`n == 12`) 비율 | `n` 평균 |
|---:|---|---:|
| 200 | 2.09 / 3.17 / 4.29 % | 4.25 ~ 4.52 |
| 400 | **12.50 / 18.01 / 19.48 %** | 6.53 ~ 7.06 |
| 800 | 🔴 **57.54 ~ 65.82 %** | 9.69 ~ 10.27 |

**히스토그램이 분포가 아니라 벽을 보여 준다** (적 800):

```
n=  1    2    3    4    5    6    7    8    9   10   11      12
  831 1564 1577 1903 2124 2546 2609 2678 2467 2320 2229   26617   ← 벽
```

`n` 은 8 부근에서 정점을 찍고 완만히 줄다가 **12에서 10배 넘게 치솟는다.**
자연스러운 분포라면 12 는 11 보다 작아야 한다. 이 봉우리는 **12 이상이 전부 12로 접힌 것**이다.

⚠️ **적 렌더러를 꺼도 같은 비율이 나온다**(57~61 %) = 그리는 것과 무관한 **로직 결함**이다.

### 🔑 고치는 이유는 성능이 아니다

같은 측정에서 **분리 계산 전체가 프레임의 19 %** 였다(적 800). 즉 이걸 **0으로 만들어도**
프레임은 19 % 밖에 안 준다. 적 400(실제 게임 상한 130 보다도 위)에서는 5.5~5.9 % 다.

**`B8` 을 고치는 이유는 프레임이 아니라 "적이 겹쳐 보이는 것"이다.**
`EnemyBase.cs:199-200` 이 애초에 무리 분리를 넣은 이유가 그것이고,
그 기능이 **가장 필요한 순간(뭉쳤을 때)에 정확히 꺼지고 있었다.**

---

## B9 — ✘ 철회 (오진) — `timeScale = 0` 은 버그가 아니라 **스테이지 클리어 창**이었다

> 🔴 **이 항목은 오진이다.** 아래에 원래 보고와 정정을 **둘 다** 남긴다 —
> 지워 버리면 "왜 그렇게 판단했는지"가 사라져서 같은 실수를 또 한다.

### 처음 보고한 것 (2026-09-02, D27 측정 중)

측정이 5분 넘게 진행이 없어서 상태를 찍었다.

```
살아있는 적=511 / EnemyBase 총=511
ObjectPool 자식 수=7220
state=Wave  timeScale=0  frameCount=11515
```

`state` 가 `Wave` 인데 `timeScale` 이 `0` 이라 **게임이 얼어붙었다**고 판단했고,
`OnLevelUpCompleted` → `ResumeWave()` 경로의 비대칭을 원인으로 지목했다.

### 🔴 정정 — 사용자 지적으로 원인이 드러났다 (2026-09-02)

> *"영구정지하는거는 레벨업하면 뜨는 선택지를 안고르거나 스테이지 클리어시 멈추는거 때문에 그럴거야"*

코드를 다시 봤더니 맞았다. `Assets/Scripts/UI/StageClearUI.cs:113`

```csharp
public void Show(StageNode clearedNode)
{
    ...
    _waitingForInput = true;
    Time.timeScale   = 0f;      // ← 멈춘다
    // 🔴 그런데 ChangeState 를 하지 않는다 — state 는 Wave 그대로다
}
```

`:194` `OnContinue()` 에서만 `Time.timeScale = 1f` + `ChangeState(StageMap)` 을 한다.

**즉 `state = Wave` + `timeScale = 0` 은 버그가 아니라
"스테이지 클리어 결과창이 Continue 를 기다리는 정상 상태"다.**
사람이 플레이하면 버튼을 누르면 그만이다. 얼어붙은 것은 **누를 사람이 없는 측정 하네스**였다.

### 왜 못 알아봤나

내 하네스의 복구 로직이 이렇게 돼 있었다:

```csharp
if (gm.CurrentState == GameState.Wave || gm.CurrentState == GameState.LevelUp) return false;
```

**`state == Wave` 면 "정상"으로 보고 그냥 넘어간다.** `Wave` 이면서 멈춰 있을 수 있다는
경우의 수를 상정하지 않았다. 게임 상태 기계에 **"UI 가 멈춰 세운 Wave"** 라는 칸이 있는데
내 모델에는 없었던 것이다.

> 🔑 **관측이 틀린 게 아니라 해석이 틀렸다.** `state=Wave · timeScale=0` 은 실제 값이었다.
> 거기에 내가 아는 코드 경로(`ResumeWave` 비대칭)를 갖다 붙였을 뿐이다.
> **설명이 그럴듯하면 검증을 건너뛰게 된다** — `StageClearUI` 를 열어 보기만 했으면 5분이면 끝났다.

### 🟡 그래도 남는 것 — `ResumeWave()` 비대칭은 **여전히 잠재 결함**이다

`WaveManager.cs:106`

```csharp
public void ResumeWave()
{
    if (!_waveActive) return;      // ← 웨이브가 안 돌면 timeScale 을 안 되돌린다
    _wavePaused = false;
    Time.timeScale = 1f;
}
```

`GameManager.cs:430` `OnLevelUpCompleted()` 는 `_stateBeforeLevelUp == Wave` 일 때
복구를 전적으로 이 함수에 맡긴다. 멈추는 쪽(`PauseWave`)은 **무조건** `timeScale = 0` 을 만드는데
푸는 쪽만 조건부다.

🔴 **다만 이번 관측은 이것의 증거가 아니다.** 이게 실제로 터지는 경로가 있는지는 **모른다.**
`StageClearUI` 가 열려 있으면 그쪽이 `:194` 에서 되돌려 주므로 대부분 구제된다.

**등재는 유지하되 상태는 `철회`다.** 실플레이에서 진짜 정지를 만나면 그때 다시 연다.

### 곁가지 — 풀이 7,220개까지 자란다

같은 관측에서 `ObjectPool` 의 자식이 **7,220개**였다 (씬 워밍업 설정은 5종 · 합계 110개).
이건 정지와 무관하고, 장시간 실행에서 씬이 무거워지는 것과 관련이 있다
([`../PERF.md`](../PERF.md) §7-H 함정 ⑪ — 같은 세션 시행 1 = 47 fps → 시행 2 = **3 fps**).
"아직 안 판 것"이므로 [`../TODO.md`](../TODO.md) 소관이다.

### 🟡 절반 수정 (2026-09-02, D27) — 버퍼 12 → 64

상세와 전체 수치는 [`../PERF.md`](../PERF.md) §8. 요약:

| 적 수 | 절단 비율 (12 → 64) | 분리 비용 (프레임 대비) |
|---:|---|---|
| **130** *(실제 `MaxAlive` 상한)* | **0.9 % → 0 %** | 1.3 % → 1.1 % (차이 없음) |
| **400** | **21 % → 0 %** | 7.3 % → 7.0 % (차이 없음) |
| 800 *(게임에 없는 조건)* | 71 % → 1.8 % | 🔴 **28 % → 41 %** |

🔴 **측정 전에 못박은 성공 조건 중 2개는 실패했다** (적 800에서 포화 0 · 프레임 +5 % 이내).
그래도 64 를 택한 것은 **판단**이지 기준 통과가 아니다 — 800 은 `Waves.csv` 어디에도 없고
실제로 나오는 구간에서는 비용 차이가 측정 노이즈 안이다.

### 🔴 그런데 겹침 자체는 거의 안 줄었다 — **절단은 증상이지 원인이 아니었다**

| | buf 12 | buf 64 |
|---|---:|---:|
| 평균 최근접 거리 (적 800) | 0.3144 | 0.3400 (+8.1 %) |
| **`0.5유닛내` 비율** | **84.6 %** | **85.1 %** |

**적 800에서 85 % 가 여전히 0.5유닛 안에 이웃을 두고 있다.** 분리 반경이 0.85 인데 실제 간격은
그 3분의 1 이고, 최소 거리는 0.005~0.03 — **사실상 완전히 겹친 쌍**이 남아 있다.

원인은 **밀도**이고, 밀도의 원인은 *"적 콜라이더는 트리거라 반발이 없고, 조향으로만 밀어낸다"* 는
설계다 (`EnemyBase.cs:203-205` 주석이 그 선택의 이유를 적어 두었다).
**버퍼를 아무리 키워도 이 설계는 안 바뀐다.**

⇒ **줄을 닫지 않는다.** 남은 것은 [`../TODO.md`](../TODO.md) 와 [`../TUNING.md`](../TUNING.md) 로 나눠 넘긴다:
- 반경 `0.85` · 가중치 `0.9` · 4스텝 주기가 이 밀도에 맞는 값인가 → **`TUNING.md`** (답이 숫자다)
- 적끼리 물리 반발을 줄 것인가 → **설계 결정** → `TODO.md`
- 공간 해시는 **질의 비용만** 없앤다. 적 800 기준 질의 4.35 ms / `1/d²` 누산 4.54 ms 로 반반이라
  얻을 수 있는 상한이 **절반**이다 → `TODO.md`

---

## B10 — 레벨이 한 번에 여러 번 오르면 **카드를 1장만 받는다**

**증상:** 경험치가 한 번에 크게 들어와 레벨이 2단계 이상 오르면, 오른 레벨 수만큼이 아니라
**카드 선택 창이 한 번만** 뜬다. 나머지 레벨의 선택권은 **아무 표시 없이 사라진다.**
예외도 경고도 없다 — 레벨 숫자는 정상으로 올라가고 스탯도 오르지만 **아이템만 못 받는다.**

**재현 절차:**
1. Dev 패널(백틱)에서 경험치를 크게 준다 (또는 낮은 레벨에서 보물상자를 연달아 연다)
2. 한 번의 획득으로 레벨이 2 이상 오르게 만든다
3. 카드 창이 **한 번만** 뜬다. `[GameManager] State → LevelUp` 로그는 오른 횟수만큼 찍힌다

### 원인 확정 (2026-09-03, DEV)

`ExperienceManager.cs:145`

```csharp
while (CurrentXp >= XpToNext)
{
    CurrentXp -= XpToNext;
    CurrentLevel++;
    OnLevelUp?.Invoke(CurrentLevel);
    TriggerLevelUp();          // ← 루프마다 부른다
}
```

`TriggerLevelUp()` → `LevelUpManager.ShowLevelUpPanel()` (`LevelUpManager.cs:53`) 는

```csharp
_currentChoices = PickItems(cards.Length);
RefreshPanel();
levelUpPanel.SetActive(true);
```

**직전 호출이 만든 후보를 그냥 덮어쓴다.** 대기열도, "몇 번 남았는지" 세는 값도 없다.
루프가 3번 돌면 패널이 3번 다시 만들어지고 **마지막 것만 화면에 남는다.**
플레이어가 카드를 하나 고르면 `SelectItem` → `HidePanel` 로 창이 닫히고 **거기서 끝난다.**

**얼어붙지는 않는다.** `GameManager.ChangeState:117` 의

```csharp
if (newState == GameState.LevelUp && CurrentState != GameState.LevelUp)
    _stateBeforeLevelUp = CurrentState;
```

가드가 두 번째 호출에서 `_stateBeforeLevelUp` 이 `LevelUp` 으로 덮이는 것을 막아 준다.
그래서 증상은 **정지가 아니라 조용한 손실**이다 — 더 찾기 어려운 종류다.

### 지금은 왜 잘 안 보이나

구슬 하나가 주는 경험치가 작고 `ExpDrop` 이 **개체마다 따로** `CollectXp` 를 부르기 때문에
한 호출로 2레벨이 오르는 일이 드물다. 보물상자·`VillageBuilding` 처럼
**큰 덩어리로 들어오는 경로**에서만 가끔 난다.

🔴 **[`REQ/DEV.md` 요청-21](REQ/DEV.md)(구슬 원거리 회수)이 이걸 상시화한다.**
그 요청은 회수한 구슬 수백 개를 **합산해 `CollectXp` 를 한 번만** 부르라고 한다
(각자 부르면 그것대로 비용이라서다 — 맞는 요구다). 그러면 한 호출로 여러 레벨이 오르는 게
예외가 아니라 **기본 동작**이 된다.

⇒ **요청-21 은 이 버그를 먼저 닫지 않으면 플레이어 손해를 늘리는 변경이 된다.**
CONTENT 도 요청-21 §3 에서 *"`LevelUpManager` 가 큐를 처리하는지 확인해 줘 —
못 하면 이 요청은 거기서 막힌다"* 고 미리 짚었다. **확인 결과 큐는 없다.**

**고치는 방향** — `LevelUpManager` 에 대기 횟수를 두고 `HidePanel` 에서 남은 만큼 다시 여는 것이
가장 작다. 다만 **레벨업 중에 또 레벨업이 들어오는 경로**(진화 제안 `ShowForcedChoices`,
보물상자 `GrantChestReward`)와 섞이므로 순서 규칙을 같이 정해야 한다.

### ✅ 수정됨 (D29, 2026-09-03) — 사용자 판단으로 요청-21 보다 먼저 고쳤다

`LevelUpManager` 에 `_pendingLevelUps` 를 두고 **떠 있는 패널을 덮지 않게** 했다.
`HidePanel` 이 하나씩 소진하며 남아 있으면 다시 연다.

🔑 **설계 도중에 상호작용 하나를 더 잡았다.** 처음 안은 `HidePanel` 에서 무조건 하나 깎는 것이었는데,
**진화 제안 패널이 떠 있는 동안 레벨업이 들어오면** 진화 카드를 고른 것이 레벨업 빚을 갚은 것으로
세어져 **대기 중인 레벨업이 그대로 사라진다.** `_panelIsForced` 로 패널 종류를 구분해
**진화는 빚을 만들지도 갚지도 않게** 했다.

검증 (플레이 모드 실측):

```
[B10] CollectXp(40) 후  Lv=1 → 4  state=LevelUp timeScale=0
[B10] HidePanel 1회  state=LevelUp
[B10] HidePanel 2회  state=LevelUp
[B10] HidePanel 3회  state=Wave  timeScale=1
```

3레벨이 올랐고 **`HidePanel` 을 세 번 다 써야** `Wave` 로 돌아온다.
수정 전이라면 **1회에 끝났을 것**이고, 그게 곧 카드 2장 유실이다.
전문 [`DONE/D29.md`](DONE/D29.md).
