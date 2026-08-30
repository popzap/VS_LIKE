# 병렬 작업 현황판

> 세션을 열면 **가장 먼저 읽고**, 착수 전에 내 줄을 등록한다.
> 규칙 전문은 [`SESSION_PROMPT.md`](SESSION_PROMPT.md).

---

## 0. 세션과 소유 경로

| 세션 | 접두어 | 하는 일 | 소유 경로 (쓰기 가능) |
|---|---|---|---|
| **DEV** | `D` | 개발·뼈대. 코드 · 씬 배선 · **Unity 에디터 전담** · 실플레이 검증 | `Assets/Scripts/**` · `Assets/Editor/` · `Assets/Scenes/` · `Assets/Prefabs/` · `Assets/GameObjects/` · `Assets/Game/*Data/` (SO) · `Assets/Settings/` · `ProjectSettings/` |
| **CONTENT** | `C` | 코드 외적. 그림 · 소리 · 애니메이션 소스 · 수치 · 문서 | `_Incoming/` · `Assets/Game/{Sprites,ICON,Tiles,Materials,Shaders,Audio}` · `Assets/Game/Balance/*.csv` · `Assets/Fonts/` · `Docs/BALANCE.md` · `Docs/TUNING.md` |

**소유하지 않은 경로는 읽기만 한다.** 고쳐야 하면 `REQ/<대상>.md` 에 요청을 남긴다.

공용(아무도 직접 안 씀): `Docs/SETUP_STATUS.md` · `Docs/TODO.md` · `Docs/ROADMAP.md` · `CLAUDE.md`
→ **DEV 세션이 통합해서 반영한다** (검증까지 끝난 걸 아는 유일한 세션이므로).

> ℹ️ **`_Incoming/` 은 저장소 최상단이다 — `Assets/` 밖이다.**
> CONTENT 가 만든 png·wav 원본을 여기 둔다. Unity 가 아예 보지 않으므로
> DEV 가 플레이 중이어도 임포트가 끼어들지 않는다. DEV 가 `Assets/` 로 옮기고 배선한 뒤 지운다.

---

## 1. 지금 돌고 있는 작업

| 세션 | 이슈 | 만지는 경로 | 시작 | 상태 |
|---|---|---|---|---|
| DEV | `D15` | `Assets/Game/Shaders/SpriteOutline.shader`(소유 예외) · `Assets/Scripts/Enemy/EnemyVisual.cs` | 2026-08-30 | `진행중` — **B1** 엘리트 외곽선이 옆 프레임 알파를 빨아들이는 문제. A안(`_SpriteRect` + MPB) |
| CONTENT | `C6` | `Docs/DESIGN_CLASSES.md`(신설) · 이후 `Assets/Game/Balance/*.csv` · `_Incoming/` | 2026-08-30 | `진행중` — 직업 6종 컨셉 재편 + 신규 무기 6종 설계 (사용자 지시) |
| CONTENT | `C9` | `_Incoming/{Effects,ICON}/` · `Docs/{DESIGN_CLASSES,TUNING}.md` | 2026-08-30 | `대기(REQ→DEV)` — 바닥 폭탄 + 독 장판 그림 (§6 3·4단계 선작업) — [`REQ/DEV.md`](REQ/DEV.md) 요청-8 |
| CONTENT | `C19` | `Assets/Game/Balance/{Weapons,Items,SceneWiring}.csv` · `Tools/Art/gen_octopus.py` · `_Incoming/Effects/` · `Docs/TUNING.md` | 2026-08-30 | `대기(REQ→DEV)` — 소환수 CSV 3파일 + **촉수 그림 재작업** + SFX 볼륨 2값. DEV 요청-7·8 의 답 — [`REQ/DEV.md`](REQ/DEV.md) 요청-13 |

> 상태값: `진행중` · `대기(REQ→DEV)` · `검증대기` · `막힘(B1)`
> 끝나면 **자기 줄을 지운다.**
>
> ✅ **`C12`·`C15` 는 CONTENT 가 확인하고 지웠다** (2026-08-30). DEV 가 D12 에서 Import 1회로
> 처리를 마쳤고 판정 전부 PASS → [`DONE/D12.md`](DONE/D12.md).
> 회신([`REQ/CONTENT.md`](REQ/CONTENT.md) **요청-6**)은 **`C17` 에서 닫았다.**
>
> ✅ **`C13`·`C14`·`C16` 은 CONTENT 가 확인하고 지웠다** (2026-08-30). D13 이 프리팹 5개를 만들고
> 판정 12개 PASS → [`DONE/D13.md`](DONE/D13.md). 회신([`REQ/CONTENT.md`](REQ/CONTENT.md) **요청-7**)은
> **`C19` 에서 닫았다** — CSV 3파일을 채우고 촉수 그림을 다시 구웠다.
>
> ✅ **`C17`·`C18` 도 CONTENT 가 확인하고 지웠다** (2026-08-30). D14 가 클립 2장을 배선하고
> 옛 소리가 안 나는 것까지 대조군으로 확인했다 → [`DONE/D14.md`](DONE/D14.md).
> 회신([`REQ/CONTENT.md`](REQ/CONTENT.md) **요청-8**)도 **`C19` 에서 닫았다** — 실측 등재 + 볼륨 2값 결정.
>
> ⏸ **아직 안 한 것: 소환수 SFX 2종.** `SummonWeapon.cs:159·174` 가 `WeaponCast`·`WeaponFire` 를
> 빌려 쓴다. 만드는 조건은 [`TUNING.md` §2-C-2](../TUNING.md) 에 적어 뒀다. `SfxId` **6·7** 예약.

---

## 2. Unity 에디터 상태

에디터는 **하나뿐**이고 **DEV 세션이 전담**한다. 아래는 **DEV 만 쓴다.**
CONTENT 는 **읽기만** 한다 — 내 요청이 언제 처리될지 가늠하는 용도다.

| 항목 | 값 |
|---|---|
| 상태 | `BUSY` |
| 점유 이슈 | `D15` (B1 셰이더 수정 · Refresh/플레이 검증) |
| 컴파일 에러 | `0` (2026-08-30 D14 종료 시 확인 — 프로브 철거 후 Refresh, 콘솔 **0건**) |

| 상태값 | 뜻 | CONTENT 가 알 것 |
|---|---|---|
| `IDLE` | 놀고 있음 | 요청을 넣으면 곧 처리된다 |
| `BUSY` | Refresh / Import / 배선 중 | 요청은 넣어도 되고 처리만 밀린다 |
| `PLAYING` | **플레이 모드** | CSV Import 는 이 상태에서 **실패한다.** 처리가 밀린다 |
| `BROKEN` | 🔴 **컴파일 에러 있음** | 검증이 전부 막힌 상태 |

🔴 **DEV 는 `IDLE` 로 되돌리기 전에 컴파일 에러 0 을 반드시 확인한다.**

---

## 3. 다음에 쓸 번호

| 세션 | 다음 이슈 번호 |
|---|---|
| DEV | `D16` |
| CONTENT | `C20` |
| 버그(공용) | `B6` |

> 번호를 쓸 때 이 표를 **즉시** 올린다. 선점이 곧 예약이다.
> ℹ️ 과거 이력의 `I-1`~`I-61` 은 그대로 둔다. 새 번호만 접두어 방식이다.
