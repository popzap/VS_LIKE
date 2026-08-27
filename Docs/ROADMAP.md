# ROADMAP — "완성된 뱀서라이크"까지 남은 것

> **작성:** 2026-08-28 · **기준 커밋:** `1f9b992`
> 이 문서는 **직접 코드·씬·애셋을 읽어 확인한 결과**다. 추정한 부분은 `(추정)` 으로 표시했다.
>
> | 문서 | 역할 |
> |---|---|
> | `SETUP_STATUS.md` | 이미 **끝난** 일의 이력 |
> | `TODO.md` | 지금 굴러가는 **백로그** (버그·미검증·결정 대기) |
> | **`ROADMAP.md` (이 문서)** | **장르 완성도 갭 분석.** "뱀서라이크로서 아직 없는 것" |
> | `BALANCE.md` | 수치를 고치는 법 |

---

## 0. 한 줄 요약

**뼈대는 다 섰다. 살이 없다.**

런 루프(직업 선택 → 스테이지 맵 → 웨이브 → 레벨업 3택 → 상점 → 클리어/사망)는
전 구간이 실제로 돌아간다. 그런데 **소리가 단 한 개도 없고**, 적은 6종이 전부 같은 행동을 하며,
화면에는 남은 시간도 킬 수도 안 보인다. 지금 상태를 한 문장으로 줄이면
**"조용한 프로토타입"** 이다.

### 확인된 현황 수치

| 항목 | 수 | 비고 |
|---|---:|---|
| 오디오 파일 (`.wav`/`.mp3`/`.ogg`) | **0** | `Assets/` 전체 검색 결과 0건 |
| 적 종류 | 6 | Slime · Goblin · Zombie · Wolf · Demon · Ogre |
| 적 **행동** 종류 | **1** | 전부 `MoveTowardsPlayer()` 직선 추격 |
| 무기 | 5 | Sword · Bow · Gun · Fireball · Bomb |
| 무기 **진화** | **0** | `Evolv` 문자열이 코드 전체에 없음 |
| 아이템 / 패시브 | 20 / 10 | |
| 건물 | 5 | Turret · Village · Farm · Restaurant · Bombard |
| 직업 | 3 | |
| 웨이브 | 6 | Normal×3 · Elite×2 · Boss×1 |
| 필드 픽업 | **2** | ExpDrop, HealPickup |
| 파티클 이펙트 | **0** | `ParticleSystem` 을 쓰는 스크립트 없음 |

---

## 1. 오디오 — 0에서 시작한다 (최우선)

### 지금 상태 (확인함)

- `Assets/` 안에 **오디오 파일이 한 개도 없다.**
- `Assets/Scripts/UI/AudioManager.cs` 는 **40줄이고 재생 함수가 없다.**
  `SetBGMVolume` / `SetSFXVolume` 두 개뿐 — 볼륨 *적용*만 하고 소리를 *내지* 않는다.
- `AudioMixer` 가 없어서 SFX 볼륨을 `AudioListener.volume`(전역 마스터)로 대신하고 있다.
  그래서 **SFX 슬라이더를 0으로 내리면 BGM 도 같이 꺼진다.** (`AudioManager.cs:35-39`)
- 옵션 패널의 BGM/SFX 슬라이더는 PlayerPrefs 에 값만 저장한다. 들을 소리가 없다.

### 해야 할 일

| # | 작업 | 왜 |
|---|---|---|
| A-1 | **`AudioMixer` 도입** — Master / BGM / SFX 3그룹 | 지금 구조는 SFX 를 줄이면 BGM 도 줄어드는 버그다. 소리를 넣기 **전에** 고쳐야 한다 |
| A-2 | `AudioManager` 에 **재생 API 추가** — `PlaySfx(AudioClip, float pitchJitter)`, `PlayBgm(AudioClip, bool loop)`, 크로스페이드 | 현재 재생 경로 자체가 없다 |
| A-3 | **SFX 원샷 풀링** — 뱀서라이크는 초당 수십 번 피격음이 난다. `AudioSource.PlayClipAtPoint` 는 매번 GameObject 를 만든다 | 안 하면 적 100마리 구간에서 GC 가 튄다 |
| A-4 | **동일 프레임 중복 컷** — 같은 클립이 한 프레임에 N개 겹치면 1~2개만 재생 | 안 하면 피격음이 찢어진다(클리핑) |
| A-5 | BGM 확보 — 최소 4곡: 메인메뉴 / 일반 웨이브 / 보스 / 상점·맵 | |
| A-6 | SFX 확보 — 아래 표 |

### 필요한 SFX 목록 (코드상 호출 지점이 이미 있는 것 위주)

| 분류 | 클립 | 트리거 위치 |
|---|---|---|
| 무기 | 검 휘두름 / 화살 / 총성 / 화염구 / 폭탄 발사 | `WeaponBase.Fire()` 파생 5종 |
| 타격 | 적 피격, 치명타, 폭발 | `EnemyBase.TakeDamage()`, `AoeProjectile` |
| 적 | 사망(소·중·대), 보스 등장 포효 | `EnemyBase.Die()` — 지금 `OnDeath()` 가 **빈 함수**다 (`EnemyBase.cs:152`) |
| 플레이어 | 피격, 사망, 회복 | `PlayerController.cs:123` 넉백 지점 |
| 픽업 | 경험치 오브, 레벨업 팡파레, 회복 | `ExpDrop`, `ExperienceManager.OnLevelUp` |
| 건물 | 설치, 파괴, 포탑 발사, 곡사포 발사 | `BuildingManager`, `TurretBuilding`, `BombardBuilding` |
| UI | 커서 이동, 선택, 취소, 카드 뒤집기, 구매, 리롤 | `ItemCardUI`, `ShopCardUI`, `ClassCardUI` |
| 결과 | 웨이브 클리어, 스테이지 클리어, 게임오버 | `StageClearUI`, `RunEndUI` |

> **작업 순서 권고:** A-1 → A-2 → A-3/A-4 를 먼저 (배관), 그 다음 클립을 붓는다.
> 배관 없이 클립부터 넣으면 나중에 전부 다시 연결해야 한다.

---

## 2. 코어 루프 — 뱀서라이크의 "맛"이 빠져 있다

### 2-1. 적이 전부 똑같이 움직인다 ★★★

`EnemyBase.MoveTowardsPlayer()` (`EnemyBase.cs:110`) 하나가 전부다.
6종은 **프리팹도 하나를 공유**하고(`Enemies.csv` 전 행이 `Enemy_Goblin.prefab`),
스프라이트 · `SizeScale` · 스탯만 다르다. Ogre 는 큰 고블린, Wolf 는 빠른 고블린이다.

뱀서라이크에서 적의 재미는 **행동의 차이**에서 온다. 필요한 것:

| 행동 | 붙일 적 | 구현 |
|---|---|---|
| 원거리 투사체 | Demon | `RangedEnemy : EnemyBase` — 사거리 유지 + 주기 발사 |
| 돌진(charge) | Wolf | 조준 정지 → 대시 → 경직 |
| 무리 유지(flocking) | Goblin | 서로 겹치지 않게 분리력. **지금은 다 겹쳐서 한 덩어리로 뭉친다** |
| 분열 | Slime | 사망 시 소형 2마리 |
| 소환 | Ogre(보스) | 주기적 잡몹 소환 |
| 자폭 | 신규 | 접근 → 정지 → 폭발 |

> `EnemyBase.MoveTowardsPlayer()` 가 이미 `protected virtual` 이라 **상속만 하면 된다.**
> 구조는 준비돼 있고 파생 클래스가 없을 뿐이다.

### 2-2. 웨이브에 적이 섞여 나오지 않는다 ★★★

`Waves.csv` 의 `Spawns` 는 **순차 소환**이다. 주석에도 명시돼 있다:
"Goblin 18마리를 다 뿌린 뒤 Zombie 로 넘어간다."

그래서 `Normal3` 는 Zombie 36 → Wolf 24 → Demon 8 → Goblin 40 순서로 나온다.
합계 86.4초인데 `SurvivalTime` 이 90초라 **Goblin 이 거의 안 나오고 웨이브가 끝난다.**

> 이것이 사용자가 말한 "몬스터가 전 종류 안 나온다"의 실제 원인이다.
> 데이터 문제가 아니라 **소환기 설계** 문제다.

**해야 할 일:** `WaveSpawnEntry` 에 `StartTime` / `EndTime` (또는 `Weight`)을 추가해
**동시 병렬 소환**으로 바꾼다. 시간에 따라 가중치가 옮겨가면 자연스러운 난이도 곡선이 된다.

### 2-3. 무기 진화가 없다 ★★★

뱀서라이크의 핵심 보상 구조다. 코드에 `Evolv` 문자열이 **한 번도 없다.**

- 무기 Lv.MAX + 특정 패시브 보유 → 진화형 해금
- 진화는 **보물상자**에서 나온다 (아래 2-4)
- `ItemData` 에 `EvolvesInto` / `RequiredPassive` 필드 추가 + `Items.csv` 열 추가

### 2-4. 필드 픽업이 2종뿐이다 ★★

지금: `ExpDrop_Small`, `HealPickup`. 이게 전부다.

| 추가할 것 | 효과 |
|---|---|
| **보물상자** | 엘리트 처치 시 드랍. 열면 무기 진화 / 다중 레벨업 |
| **자석** | 화면의 경험치 전부 흡수 |
| **화면 폭탄** | 화면 내 전멸 |
| **골드 코인** | 지금 `CurrencyDrop` 은 즉시 지급 (추정 — 필드 드랍 프리팹이 없음) |
| 경험치 **중/대** 오브 | 지금 Small 하나뿐이라 후반 레벨업 속도가 안 붙는다 |

### 2-5. 보스가 큰 잡몹이다 ★★

`Boss1` = `Ogre` + `BossHpMult 7` / `BossDamageMult 2`. 패턴이 없다.
`EnemyBase.ApplyRankOutline()` 로 **빨간 외곽선**이 붙는 게 시각적 차별의 전부다(I-26).

필요: 페이즈 전환, 텔레그래프(예고 표시) 있는 광역기, 소환, 등장 연출, **보스 HP 바 UI**.

---

## 3. UI/UX — HUD 가 반쪽이다

### 3-1. HUD 에 정보가 없다 ★★★

`HUDManager.cs` 의 직렬화 필드 전부: 포트레이트 · HP · 레벨 · XP · 옵션 버튼.
씬(`SampleScene.unity`)에서 10개가 배선돼 있고 정상 동작한다.

**없는 것:**

| 없는 표시 | 왜 필요한가 |
|---|---|
| **생존 시간 타이머** | 클리어 조건이 `UseTimerClear`/`SurvivalTime` 인데 **화면에 남은 시간이 안 보인다.** 언제 끝나는지 알 수 없다 |
| **킬 카운터** | 뱀서라이크의 기본 피드백 |
| **골드** | 상점 진입 전까지 얼마 있는지 모른다 |
| **보유 무기/패시브 아이콘 줄** | 뭘 들고 있는지 확인할 방법이 인게임에 없다 |
| 설치 대기 건물 표시 | `TODO.md` 에 이미 등재 |

> 타이머·킬·골드는 각각 `WaveManager`, `EnemyBase`, `PlayerStats`(추정)에 값이 이미 있다.
> **표시만 붙이면 된다** — 가성비가 가장 좋은 작업이다.

### 3-2. 미할당 슬롯 (애셋 없어서 비어 있음) ★

`UI Canvas.prefab` 기준 `{fileID: 0}` 인 필드:
`faceHealthy` · `faceNormal` · `faceWorried` · `faceCritical` (표정 4장),
`levelUpAnimator` · `levelUpEffect` (레벨업 연출).

`UpdatePortrait()` 는 코드가 다 돼 있고 **스프라이트만 넣으면 켜진다.**

### 3-3. 없는 화면 ★★

| 화면 | 상태 |
|---|---|
| **메타 강화 화면** | `GameState.MetaScreen` 은 **enum 값만 존재**하고 UI·스크립트가 없다. `SaveData`(Currency/UpgradeLevels/UnlockedCharacters)와 `UpgradeDefinition` SO 는 이미 있다 — **화면만 없다** |
| 해상도 / 그래픽 설정 | 옵션에 볼륨 슬라이더 2개뿐 |
| 키 바인딩 | 없음 |
| 도감 / 컬렉션 | 없음 |
| 통계 (누적 킬·플레이 시간) | `SaveData.TotalRuns`/`TotalKills` 는 쌓이는데 **보여주는 곳이 없다** |
| 튜토리얼 / 조작 안내 | 없음. 특히 **Z = 건물 설치**를 알려주는 곳이 아무 데도 없다 |
| 크레딧 | 없음 |

### 3-4. 게임패드가 사실상 미구현 ★★

`Assets/Input/InputSystem_Actions.inputactions` 에 Player/UI 맵과
Keyboard&Mouse / Gamepad / Touch 스킴이 정의돼 있다. 그런데 코드는
`Keyboard.current` / `Mouse.current` 를 **직접** 읽는다
(`PlayerController.cs:50-59`, `PauseMenuUI.cs:57`, `StageClearUI.cs:82`).

→ **에셋만 있고 연결이 안 됐다.** 패드로는 못 논다.
`PlayerInput` 컴포넌트 + 액션 콜백으로 갈아타야 한다.

---

## 4. 게임 필(Game Feel) — 때리는 맛이 없다 ★★★

확인한 사실:

- **적 피격 플래시는 이미 있다** — `EnemyVisual.Flash()` (`_FlashAmount` 0.12초 감쇠, I-26).
  `EnemyBase.cs:124` 에서 호출된다. 이 항목은 **이미 끝났다.**
- `ParticleSystem` 을 쓰는 스크립트가 **하나도 없다.**
- `CameraController.Shake()` 는 존재하지만 **호출부가 딱 한 곳** —
  `PlayerController.cs:128` (플레이어가 맞았을 때)뿐이다.
  **적을 때릴 때는 화면이 안 흔들린다.**
- `EnemyBase.OnDeath()` 는 `{ }` **빈 함수**다 (`EnemyBase.cs:152`). 죽으면 그냥 사라진다.
- 히트스톱(hitstop) 없음. `Time.timeScale` 은 일시정지에만 쓰인다.

| # | 작업 | 효과 |
|---|---|---|
| ~~F-1~~ | ~~적 피격 플래시~~ | **이미 완료** (I-26) |
| F-2 | **적 넉백** — 플레이어는 넉백이 있는데 적은 없다 | 밀리는 감각 |
| F-3 | **사망 파티클** — `OnDeath()` 를 채운다 | 지금은 그냥 증발한다 |
| F-4 | **치명타 시 짧은 히트스톱** (0.03초) | 무게감 |
| F-5 | **적 처치 시 미세 `Shake()`** | 이미 함수가 있다. 호출만 하면 된다 |
| F-6 | 무기 발사 머즐 플래시, 투사체 트레일 | |
| F-7 | 레벨업 시 화면 플래시 + 시간 감속 | |
| F-8 | 경험치 오브가 플레이어 쪽으로 **가속하며 빨려드는** 연출 | |

> 이 묶음은 **콘텐츠 추가 없이 체감을 가장 크게 바꾼다.**
> 오디오 배관 다음 순위로 권한다.

---

## 5. 메타 진행 — 자료구조만 있고 게임에 안 붙어 있다 ★★

| 있는 것 | 없는 것 |
|---|---|
| `SaveData` (Currency / TotalRuns / TotalKills / UpgradeLevels / UnlockedCharacters / UnlockedSkins) | 이 값을 **쓰는 화면** |
| `UpgradeDefinition` SO (비용 배열까지) | `UpgradeDef` **애셋이 0개** |
| JSON 저장/로드 (`MetaProgressionManager.cs:197,214`) | 런 종료 시 재화 정산 흐름 |
| 직업 3종 | **해금 조건 / 해금 흐름** |

**해야 할 일:** 재화 획득 → 런 종료 정산 → 메타 화면에서 소비 → 다음 런 반영.
이 고리가 닫혀야 "한 판 더" 가 생긴다. 지금은 죽으면 아무것도 안 남는다.

---

## 6. 성능 — 아직 문제 없지만 지금 손봐야 싼 것

| 항목 | 현재 | 위험 |
|---|---|---|
| 적 풀링 | ✅ `enemyPool` 사용 | — |
| 데미지 팝업 풀링 | ✅ `DamagePopupManager` | — |
| 조준 | `Physics2D.OverlapCircleAll` (`WeaponBase.cs:35`) | **매 발사마다 배열을 새로 할당**한다. `OverlapCircleNonAlloc` 또는 `ContactFilter2D` 버전으로 교체 |
| 적 수 상한 | **없음** | `Waves.csv` 는 순차라 지금은 안 터지지만, 2-2(병렬 소환)를 하면 즉시 문제가 된다. **상한 + 화면 밖 적 재활용**을 같이 넣을 것 |
| 적끼리 충돌 | 분리력 없음 | 다 겹쳐서 한 덩어리가 된다 (2-1 flocking) |
| 텍스처 | 폭발 시트 Uncompressed 1024 | 종류가 늘면 아틀라스 필요 |

---

## 7. 콘텐츠 볼륨 — 뱀서라이크 기준으로 얼마나 모자란가

| 항목 | 현재 | 최소 목표 | 비고 |
|---|---:|---:|---|
| 무기 | 5 | 8~10 (+진화형) | |
| 패시브 | 10 | 10~12 | **거의 충분** |
| 적 | 6 | 12~15 | 종류보다 **행동 분화**가 먼저 |
| 보스 | 1 | 3 | 스테이지 층별 1체 |
| 스테이지/맵 | 1 씬 | 3~4 테마 | 타일셋 교체로 가능 |
| 직업 | 3 | 6~8 | 해금 대상 |
| 아이템(레벨업 풀) | 20 | 30+ | |

---

## 8. 권장 진행 순서

체감 대비 비용이 좋은 순서다.

### 1단계 — "게임처럼 보이게" (가장 싸고 효과가 큼)
1. **HUD 에 타이머 · 킬 수 · 골드 추가** (3-1) — 값이 이미 있다, 표시만
2. **게임 필 F-2 ~ F-5** (4장) — 적 넉백 · 사망 파티클 · 치명타 히트스톱 · 처치 시 흔들림
3. **오디오 배관 A-1 ~ A-4** (1장) — AudioMixer + 재생 API + 풀링

### 2단계 — "소리를 붓는다"
4. BGM 4곡 + SFX 1차 세트 (무기 · 타격 · 사망 · UI)

### 3단계 — "루프에 재미를 넣는다"
5. **웨이브 병렬 소환 + 적 수 상한** (2-2, 6장) — 둘은 같이 해야 한다
6. **적 행동 분화 3종** (2-1) — 원거리(Demon) · 돌진(Wolf) · 무리 분리(Goblin)
7. **보물상자 + 자석 픽업** (2-4)

### 4단계 — "다시 하게 만든다"
8. **무기 진화** (2-3)
9. **메타 강화 화면** (5장) — 재화 고리 닫기
10. **보스 패턴** (2-5) + 보스 HP 바

### 5단계 — 마감
11. 게임패드 실배선 (3-4)
12. 없는 화면들 (3-3) — 설정 확장 · 통계 · 튜토리얼
13. 콘텐츠 볼륨 확대 (7장)

---

## 9. 이 문서를 쓰면서 확인한 것 / 바로잡은 것

| 확인 항목 | 결과 |
|---|---|
| `Assets/Resources/Data/Weapons/` 에 Staff·Pistol·Flamethrower 무기가 있는가 | **없다.** `Assets/Resources/` 폴더 자체가 없다. 무기는 `Assets/Game/WeaponData/` 의 5종 |
| 엘리트/보스가 색 곱셈으로 구분되는가 | **아니다.** I-26 대로 **외곽선**이다 (`EnemyBase.cs:83-93`) |
| `Assets/Scripts/Event/EventLog.cs` 가 있는가 | **없다** |
| CanvasScaler 가 Constant Pixel Size 800×600 으로 되돌아갔는가 (I-4 회귀?) | **아니다.** 씬 오버라이드에서 `m_UiScaleMode: 1`(ScaleWithScreenSize) · `1920×1080` · match 0.5 로 정상 |
| `HUDManager` 필드가 전부 미할당인가 | **아니다.** 프리팹에서는 전부 `{fileID: 0}` 이지만 **씬 오버라이드로 10개가 배선**돼 있다. 비어 있는 건 `TODO.md` §4 에 적힌 표정 4장 + 레벨업 연출 2개뿐 |
| `GameState` enum 실제 항목 | MainMenu · StageMap · Wave · LevelUp · Shop · Event · Paused · GameOver · Victory · MetaScreen (10개, `ClassSelect` 없음) |

> 마지막 두 줄은 **회귀 의심 신고를 검증해서 기각한 것**이다. 문서(`SETUP_STATUS.md` I-4,
> `TODO.md` §4)가 맞고 실제 상태와 일치한다. 고칠 것 없음.
