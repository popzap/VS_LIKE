# DEV 세션에 들어온 요청

> 코드 · 씬 배선 · **에디터가 필요한 모든 것**(컴파일 · CSV Import · 애셋 임포트 · 실플레이 검증).
> CONTENT 의 작업은 **거의 항상** 여기에 요청을 하나 남긴다. 끝냈으면 **잊지 말 것.**
>
> **쓰기 직전에 이 파일을 다시 읽는다.** 맨 아래에 한 줄 추가한다.

---

| 요청일시 | 요청자 | 상태 | 요청 내용 | 참조 |
|---|---|---|---|---|
| 2026-08-30 | CONTENT | `닫힘(D4)` | **걷기 시트 4종 덮어쓰기 + 재임포트** (B2 수정본) — 아래 §요청-1 | [`BUGS.md` B2](../BUGS.md) · [`DONE/D4.md`](../DONE/D4.md) |
| 2026-08-30 | CONTENT | `닫힘(D6)` | **`CombatFeel` 컴포넌트 신설 + `EnemyBase` 상수 제거** (C2) — 아래 §요청-2 | [`TUNING.md` §1](../../TUNING.md) · [`DONE/D6.md`](../DONE/D6.md) |
| 2026-08-30 | CONTENT | `닫힘(D8)` | **`SfxId` 7종 추가 + 호출부 배선 + 클립 생성**(엘리트 사망음 포함) (C3) — 아래 §요청-3 | [`TODO.md` §3](../../TODO.md) · [`DONE/D8.md`](../DONE/D8.md) |
| 2026-08-30 | CONTENT | `닫힘(D4)` | **`TODO.md` §1 갱신** — 스크린샷으로 승급 경로 2건이 확인됐다 — 아래 §요청-4 | [`TODO.md` §1](../../TODO.md) |
| 2026-08-30 | CONTENT | `닫힘(D7)` | **`Weapons.csv` Import 1회** — 진화 무기 3종의 전용 아이콘이 안 쓰이고 있었다 (C6) — 아래 §요청-5 | [`DESIGN_CLASSES.md` §6](../../DESIGN_CLASSES.md) · [`DONE/D7.md`](../DONE/D7.md) |
| 2026-08-30 | CONTENT | `닫힘(D9)` | **화살 PNG 임포트(PPU 512) + `Proj_Arrow.prefab` 신설 + `Weapons.csv` Import** (C7) — 아래 §요청-6 | [`DESIGN_CLASSES.md` §6 1단계](../../DESIGN_CLASSES.md) · [`DONE/D9.md`](../DONE/D9.md) |
| 2026-08-30 | CONTENT | `닫힘(D10)` | **수리검 + 검 근접화 — 그림은 다 나왔고 코드만 남았다** (C8) — 아래 §요청-7 · ⏸ **⑤는 CSV 대기** → [`REQ/CONTENT.md`](CONTENT.md) 요청-4 | [`DESIGN_CLASSES.md` §5-A](../../DESIGN_CLASSES.md) · [`DONE/D10.md`](../DONE/D10.md) |
| 2026-08-30 | CONTENT | `닫힘(D11)` | **바닥 폭탄 + 독 장판 — 그림은 다 나왔고 코드만 남았다** (C9) — 아래 §요청-8 | [`DONE/D11.md`](../DONE/D11.md) · [`REQ/CONTENT.md` 요청-5](CONTENT.md) |
| 2026-08-30 | CONTENT | `닫힘(D12)` | 🔴 **CSV Import 1회 — 수리검 3줄 + 검 근접화를 CSV 에 넣었다.** D10 의 판정 ⑤가 이걸로 닫힌다 (C12) — 아래 §요청-9 | [`DONE/D10.md`](../DONE/D10.md) · [`REQ/CONTENT.md` 요청-4](CONTENT.md) |
| 2026-08-30 | CONTENT | `닫힘(D12)` | 🔴 **독 장판 CSV + 바닥 폭탄 켜기 — 요청-5(D11) 의 나머지 절반.** ⚠️ **요청-9 와 같은 Import 한 번으로 둘 다 끝난다** (C15) — 아래 §요청-10 | [`DONE/D11.md`](../DONE/D11.md) · [`REQ/CONTENT.md` 요청-5](CONTENT.md) |
| 2026-08-30 | CONTENT | `닫힘(D13)` | **소환수 2종 (드래곤·문어) — 그림 5장은 다 나왔고 코드·프리팹만 남았다** (C16) · §6 **5단계**. ⚠️ CSV 는 프리팹이 생긴 뒤 CONTENT 가 채운다 — 아래 §요청-11 | [`DONE/D13.md`](../DONE/D13.md) · [`REQ/CONTENT.md` 요청-7](CONTENT.md) |
| 2026-08-30 | CONTENT | `닫힘(D14)` | **무기 전용 SFX 2종 배선 — `WeaponSwing`(검) + `ToxinSpill`(독 장판)** (C17·C18). 🔴 요청-6 이 부탁한 것 + 그때 같이 나온 것. **작다 — D13 중간에 끼워도 된다** — 아래 §요청-12 | [`REQ/CONTENT.md` 요청-6](CONTENT.md) · [`TUNING.md` §2-C-2](../../TUNING.md) · **회신** [`DONE/D14.md`](../DONE/D14.md) · [`REQ/CONTENT.md` 요청-8](CONTENT.md) |
| 2026-08-30 | CONTENT | `열림` | 🔴 **소환수 CSV 3파일 Import + 촉수 그림 교체 + SFX 볼륨 2값** (C19) · §6 **5단계 마감**. 요청-7·8 의 답이다. ⚠️ 반경은 **안 줄였다** — 문제는 크기가 아니라 그림의 가운데였다 — 아래 §요청-13 | [`REQ/CONTENT.md` 요청-7·8](CONTENT.md) · [`TUNING.md` §3](../../TUNING.md) |

> 상태값: `열림` · `진행중` · `닫힘(D3)` · `보류(사유)`
> 처리했으면 상태만 바꾼다. **줄을 지우지 않는다.**

---

## 요청-1 — 걷기 시트 4종 덮어쓰기 + 재임포트 (B2)

**무엇을** — `_Incoming/Enemies/Walk/` 의 png 4장을 `Assets/Game/Sprites/Enemies/Walk/` 의
**같은 이름 파일 위에 덮어쓴다.** Ogre·Zombie 는 손대지 않는다 (원래 정상이라 안 만들었다).

| 파일 | 고친 내용 |
|---|---|
| `Goblin_Walk.png` | 정상 4프레임(f0~f3)을 **4번 반복**해 16칸을 채웠다 |
| `Slime_Walk.png` | 위와 같음 |
| `Demon_Walk.png` | 16프레임 전부 유지. 떠다니던 **보석 잔여물 4,830px 제거** |
| `Wolf_Walk.png` | 16프레임 전부 유지. f15 구석의 **작은 늑대 2,470px 제거** |

**🔴 `.meta` 를 지우지 말 것 — png 만 덮어쓴다.**

4장 모두 **1024×1024 그대로**다. 기존 `.meta` 에 이미
`spriteMode: 2` · `spritePixelsToUnits: 512` · `frame_0`~`frame_15` 가 들어 있으니
**png 만 갈아끼우면 슬라이스도 PPU 도 스프라이트 GUID 도 그대로 유지된다.**
→ `EnemyData.WalkFrames` 의 16개 참조가 **끊기지 않는다.**

> ⚠️ 반대로 **png 를 지웠다가 새로 넣으면** `.meta` 가 재생성되면서
> PPU 가 기본값 **1024** 로 돌아가고(= 적이 걷는 순간 **크기가 절반**이 된다, I-58)
> 스프라이트 GUID 가 새로 발급돼 **`WalkFrames` 16칸이 통째로 `None`** 이 된다.

**왜** — B2. 코드·슬라이스·배선은 정상이고 애셋만 깨져 있었다(DEV 원인 확정 그대로).
`StepFrames` 가 16칸을 순서대로 도는데 Goblin·Slime 은 12칸이 "축소판 여러 마리"였다.

**Goblin·Slime 을 왜 반복으로 채웠나** — 쓸 수 있는 프레임이 **4장뿐**이기 때문이다.
16칸을 4프레임 반복으로 채우면 `EnemyVisual` 이 일렬로 돌아도 **깨끗한 4프레임 루프**가 된다.
→ **코드 수정이 필요 없다.** 루프가 이어지는 것도 확인했다(아래 참조).

**어떻게 확인하나 (판정 기준)**

1. 임포트 후 **인스펙터에서 PPU 가 512 인지** 확인. 4장 전부
2. 슬라이스가 `frame_0`~`frame_15` **16개**로 남아 있는지
3. `EnemyData` 6종의 `WalkFrames` 가 **전부 16장이고 `None` 이 하나도 없는지**
   (Goblin·Slime·Demon·Wolf 는 물론 Ogre·Zombie 도 같이 본다)
4. 🔴 **실플레이** — 노말 웨이브에서 적이 **걸어올 때** 축소판 격자가 **한 번도 안 뜬다.**
   Goblin·Slime 은 4프레임이 매끄럽게 반복된다
5. 크기 확인은 `.meta` 로 손계산하지 말고 **`sprite.bounds.size` 를 찍어서** 볼 것
   (20차에 손계산으로 놓쳤다가 21차/I-58 에서 잡힌 적이 있다)

**내가 확인한 것 (넘기기 전)**

- 4장 전부 `alpha min=0 max=255` → **I-41 가짜 투명 아님**
- 칸마다 알파 덩어리 **정확히 1개** (16칸 × 4장 = 64칸 전수)
- Goblin f0~f3 바닥 y **전부 189**, Slime f0~f3 바닥 y **전부 173** → 반복해도 발이 안 튄다
  (Slime 은 높이가 114/78/108/113 으로 눌렸다 펴지는 정상 사이클이다)
- 4장 다 **눈으로 봤다.** 격자·보석·잔여 늑대 전부 사라졌다

**⚠️ 이 요청은 B1 을 고치지 않는다.** 별개 버그다(셰이더 uv). 덧붙여 Demon·Wolf 는
`bbox` 가 여전히 **200px 을 넘는다**(Demon 최대 256, Wolf 최대 230) — 즉 **여백이 28px 미만**이라
B1 의 외곽선 번짐은 그대로다. **줄이지 않았다:** 줄이면 적의 화면상 크기가 바뀌는데
그건 버그 수정이 아니라 **연출 변경**이라 내 임의로 할 일이 아니다.
Goblin·Slime 은 `149×132` / `175×114` 로 **200 이하** — 기준을 만족한다
(참고로 정답 예시인 Ogre 는 202, Zombie 는 198 이다).

---

## 요청-2 — `CombatFeel` 씬 컴포넌트 신설 + `EnemyBase` 상수 제거 (C2)

**무엇을** — 적 타격감 수치 12개를 `EnemyBase.cs` 의 `const` 에서 빼내
`Economy.csv` 로 옮긴다. **CSV 12줄은 이미 넣었다.** 코드와 씬 배선이 남았다.

### 왜 이 구조인가 (다른 두 길은 막혀 있다)

| 시도 | 왜 안 되나 |
|---|---|
| `Economy.csv` 에 `EnemyBase,knockbackForce,6` | `BalanceImporter.FindSceneComponent`(893행)가 `FindObjectsByType` 로 **씬만 훑는다.** 적은 **프리팹 1개를 공유**하므로 씬에 없다 → `! 씬에 EnemyBase 없음` |
| `Enemies.csv` 에 열 추가 | 흔들림·히트스톱은 **엘리트/보스 여부**로 갈리는데 등급은 `EnemyData` 가 아니라 **소환 시점 인자**다(`Initialize(data, isElite, isBoss)`). 같은 숫자를 6줄에 복붙하게 된다 |

→ **씬 컴포넌트 하나**에 모으면 임포터를 **고치지 않아도 된다.** 기존 3열 규칙에 그대로 걸린다.

### 1) `CombatFeel.cs` 신설 — 필드 이름이 CSV 와 **글자까지 같아야 한다**

`Field` 열은 `SerializedProperty` 경로다. 이름이 한 글자라도 다르면
Import 로그에 `! CombatFeel.xxx 필드 없음` 이 찍히고 **조용히 건너뛴다.**

| 필드 | 기본값 | 원래 위치 |
|---|---:|---|
| `enemyKnockbackForce` | `6` | `EnemyBase.cs:354` |
| `enemyKnockbackTime` | `0.1` | `EnemyBase.cs:355` |
| `eliteKnockbackResist` | `0.4` | `EnemyBase.cs:384` |
| `bossKnockbackResist` | `0` | `EnemyBase.cs:384` |
| `deathPopTime` | `0.14` | `EnemyBase.cs:435` |
| `deathPopScale` | `1.25` | `EnemyBase.cs:450` |
| `eliteShakeMagnitude` | `0.2` | `EnemyBase.cs:469` |
| `eliteShakeDuration` | `0.25` | `EnemyBase.cs:469` |
| `eliteHitstop` | `0.05` | `EnemyBase.cs:471` |
| `bossShakeMagnitude` | `0.45` | `EnemyBase.cs:469` |
| `bossShakeDuration` | `0.5` | `EnemyBase.cs:469` |
| `bossHitstop` | `0.09` | `EnemyBase.cs:471` |

🔴 **기본값을 반드시 위 표대로 넣을 것.** 12줄 전부 **지금 동작하는 값 그대로**라
**이번 작업의 판정 기준이 "화면이 하나도 안 바뀐다"** 이기 때문이다.
기본값이 다르면 컴포넌트를 못 찾았을 때 조용히 다른 감각으로 굴러간다.

> ℹ️ 넉백 저항 2줄(`eliteKnockbackResist`·`bossKnockbackResist`)은 원래 표에 없던 것을
> 새로 뺐다. `EnemyBase.cs:384` 의 `IsBoss ? 0f : IsElite ? 0.4f : 1f` 도 감각으로 정한
> 숫자인데 상수로 박혀 있었다. **잡몹의 `1f` 는 빼지 않았다** — 그건 "저항 없음"이라는
> 기준값이지 튜닝 대상이 아니다.

### 2) `EnemyBase` 가 **쓰는 시점에** 읽게 할 것

🔴 **`Awake()` 에서 캐시하지 말 것 (`CLAUDE.md` §3, I-8/I-38).**
`CombatFeel` 이 씬 오브젝트라 참조 시점이 어긋나면 **예외 없이 기능만 조용히 죽는다.**
`TakeDamage` / `Die` 안에서 그때그때 읽거나, 지연 조회 프로퍼티로 감쌀 것.

**씬에 `CombatFeel` 이 없어도 적이 예외를 던지면 안 된다.** 없으면 위 표의 기본값으로
굴러가야 한다 — 적은 게임 내내 도는 코드라 여기서 터지면 전투가 통째로 멈춘다.

### 3) 씬 배선

`GameManager` 오브젝트에 붙인다. **씬에 딱 하나만 있어야 한다** —
`FindSceneComponent` 는 **처음 찾은 하나만** 집으므로 둘이면 어느 쪽이 먹었는지 알 수 없다.

### 어떻게 확인하나 (판정 기준)

1. 컴파일 에러 **0**
2. `Game/Balance/Import CSV -> ScriptableObjects` 실행 →
   로그에 `Economy.csv` **적용 수가 12 늘어난다.**
   🔴 `! 씬에 CombatFeel 없음` 이나 `! CombatFeel.xxx 필드 없음` 이 **한 줄도 없어야 한다**
   (⚠️ Import 는 **플레이 모드에서 못 돈다.** 일시정지도 플레이 모드다)
3. 인스펙터에서 `CombatFeel` 12개 값이 위 표와 **일치**
4. 🔴 **실플레이 — 아무것도 안 바뀌어야 한다.** 적을 때렸을 때 밀리는 정도,
   죽을 때 부풀었다 사라지는 속도가 **전과 같으면 성공**이다
5. **바뀌는 것을 한 번은 확인할 것** — `enemyKnockbackForce` 를 `6` → `20` 으로
   고치고 Import 후 플레이. 적이 눈에 띄게 멀리 날아가면 배선이 산 것이다.
   **확인 뒤 반드시 `6` 으로 되돌리고 Import 를 다시 돌린다**
   (⚠️ 4번만으로는 "안 바뀐 것"과 "안 읽힌 것"을 구분할 수 없다)
6. 엘리트/보스 흔들림·히트스톱은 **노말 웨이브에서 검증되지 않는다** — Elite/Boss 노드로 갈 것.
   못 가면 그 항목만 `미검증`으로 남기고 알려 줄 것

**나(CONTENT)가 한 것:** `Economy.csv` §적 타격감 12줄 · `TUNING.md` §1 갱신.
값의 근거와 "무엇을 보고 얼마나 고치나"는 [`TUNING.md`](../../TUNING.md) §1·§2-A 에 있다.

---

## 요청-3 — 소리 없는 트리거 배선 + 엘리트 사망음 (C3)

> ✅ **처리 완료 — D8 (2026-08-30, 31차).** 상세: [`DONE/D8.md`](../DONE/D8.md)
> - `SfxId` 7종 추가 · 호출부 6곳 배선 · 클립 6개 생성 + `AudioLibrary` 등록(14→20) · `AudioId.cs:8` 주석 수정
> - 판정 ①② 통과 · ④ 는 **노말 웨이브에서 재초기화 우회로로 증명**(요청서는 불가능하다고 봤다)
> - 🔴 **판정 ③⑤ 는 못 했다** — 이 머신은 `sfx_vol=0.00` · `bgm_vol=0.00` 이라 **게임이 무음**이다.
>   버그가 아니라 저장된 설정이다. 볼륨을 올린 뒤 사용자가 들어 봐야 한다
> - `Heal` 은 **요청서와 다른 위치**(`RestaurantBuilding` → `HealPickup`)에 넣었다 — 사용자 승인
> - `Crit` 은 지시대로 **값 `17` 만 예약**하고 클립·호출은 2단계로 미뤘다

**무엇을** — 지금 **소리가 아예 안 나는 지점 6곳**에 효과음을 넣고,
**엘리트·보스만 다른 사망음**을 쓰게 한다.

### 🔴 왜 이게 통째로 DEV 요청인가

`Assets/Game/Audio/` 는 CONTENT 소유지만 **이 작업은 내가 아무것도 만들 수 없다.**

- 클립 생성은 **Unity AI = 에디터**다. 나는 에디터를 만지지 않는다
- `AudioLibrary.asset` 등록도 **인스펙터(.asset)** 라 DEV 소유다
- 즉 내가 낼 수 있는 건 **스펙뿐**이다. 아래가 그 스펙이다

⚠️ **생성 포인트 잔액을 먼저 확인할 것** (`TODO.md` §6). 클립 **7개**가 필요하다.
잔액이 모자라면 우선순위표의 위에서부터 끊고 알려 줄 것.

### 1) `SfxId` 에 7개 추가 — **기존 값은 건드리지 않는다**

`AudioId.cs` 는 값이 **명시적 숫자**이고 `AudioLibrary` 는 **배열 인덱스가 아니라 `Id` 값**으로
매핑한다(`AudioLibrary.cs:70` `_sfxMap[e.Id] = e`). 그래서 **분류별 빈 번호에 끼워 넣어도 안전하다** —
기존 값이 하나도 안 밀리기 때문이다. 주석의 "끝에 추가한다"는 *값을 밀지 말라*는 뜻이다.

| 값 | 이름 | 분류 | 무엇 |
|---:|---|---|---|
| `14` | `EnemyDieElite` | 전투 | **엘리트·보스 공용 사망음** |
| `16` | `EnemyShoot` | 전투 | 원거리 적의 발사 |
| `17` | `Crit` | 전투 | 치명타 (⚠️ 배관 선행 — 아래 3단계) |
| `24` | `Heal` | 픽업·성장 | 식당 회복 |
| `32` | `BuildingFire` | 건물·진행 | 터렛·곡사포 발사 (공용) |
| `33` | `BossAppear` | 건물·진행 | 보스 등장 포효 |
| `41` | `UiCancel` | UI | 리롤 · 취소 · 되돌리기 |

> `15` 는 **비워 둔다.** 엘리트와 보스를 갈라야 할 때 `EnemyDieBoss` 자리다 (아래 2단계 참조).

📝 **같이 고쳐 주기 바란다 — `AudioId.cs:8` 의 주석이 사실과 다르다.**
`"새 항목은 끝에 추가한다"` 라고 적혀 있는데, 실제 구현은 `Id` 값 매핑이라 **빈 번호 삽입이 안전하다.**
이 주석 때문에 나도 한 번 "끝에 붙여야 한다"고 잘못 판단했다. 다음 사람도 똑같이 걸린다.
→ `"기존 항목의 값을 바꾸지 말 것. 분류별 빈 번호에 끼워 넣는 것은 안전하다"` 로.
(`Docs/BALANCE.md` 쪽 같은 문장은 C4 에서 이미 고쳤다.)

### 2) 호출부 배선 — 어디에 한 줄씩 넣나

| 값 | 넣을 곳 | 비고 |
|---|---|---|
| `EnemyDieElite` | `EnemyBase.cs:418` | `Play(IsElite \|\| IsBoss ? SfxId.EnemyDieElite : SfxId.EnemyDie)` |
| `EnemyShoot` | `EnemyBase.FireProjectile()` (`:263~278`) | |
| `Heal` | `RestaurantBuilding.OnCooldownElapsed()` (`:23~35`) | |
| `BuildingFire` | `TurretBuilding.Attack()` (`:16~31`) · `BombardBuilding.Attack()` (`:27~44`) | 둘이 **같은 키**를 쓴다 |
| `BossAppear` | 보스 소환 시점 (`WaveManager`) | 등장 **직후 1회**. 처치음이 아니다 |
| `UiCancel` | `ShopManager` 리롤 · `LevelUpManager` 리롤 | 구매/선택은 기존 `UiSelect` 유지 |

🔴 **`Farm`·`Village` 산출에는 소리를 넣지 말 것.** 일부러 뺐다 —
주기적으로 계속 울려서 **정보가 아니라 잔소리**가 된다.
이건 `TODO.md` §3 대로 `DamagePopupManager` 로 `+2 XP` / `+1 G` 를 띄우는 게 맞다.

🔴 **터렛이 `WeaponFire` 를 재사용하면 안 되는 이유** — `AudioManager` 의 중복 컷이
**0.04초**다(`AudioManager.cs:44`). 터렛과 플레이어가 같은 키를 쓰면 터렛 소리가
**플레이어 발사음을 잡아먹는다.** 내 무기가 나갔는지 모르게 된다. 그래서 별도 키다.

### 3) `Crit` 만 선행 작업이 있다 — **2단계로 나눌 것**

`WeaponBase.CalculateDamage()`(`:60~66`)가 치명타를 **내부에서 굴리고 결과를 버린다.**
밖에서는 그 타격이 치명타였는지 알 수 없다 (`TODO.md` §3 에 이미 적혀 있다).
`ProjectileBase`/`AoeProjectile` 까지 플래그를 배관해야 한다.

→ **`Crit` 은 나머지 6개와 같이 하지 말고 뒤로 뺄 것.** 배관이 별건이라
한 커밋에 섞으면 "소리가 안 나는 게 배선 탓인지 배관 탓인지" 구분이 안 된다.
`SfxId` 값 `17` 만 미리 잡아 두고 클립·호출은 나중에.

### 4) `AudioLibrary.asset` 등록값 — 이 숫자로 시작할 것

기존 표에서 읽어낸 규칙은 **"자주 울릴수록 작고, 피치를 더 흔든다"** 이다
(`WeaponFire` 0.35/0.10 · `EnemyHit` 0.28/0.12 · `XpPickup` 0.22/0.14 ↔ 일회성인
`PlayerDie` 0.90/0 · `LevelUp` 0.85/0). 그 규칙에 맞춰 잡았다.

| 값 | Volume | PitchJitter | 근거 |
|---|---:|---:|---|
| `BuildingFire` | **0.22** | **0.14** | **가장 반복적이다.** 터렛이 최대 7채까지 쉬지 않고 쏜다. `XpPickup` 과 같은 급으로 깔았다 |
| `EnemyShoot` | 0.30 | 0.12 | `EnemyHit`(0.28) 급. 원거리 웨이브에서 계속 울린다 |
| `Crit` | 0.45 | 0.08 | `EnemyHit` 위로 **뚫고 올라와야** 정보가 된다 |
| `Heal` | 0.50 | 0.05 | 주기적이지만 드물다 |
| `UiCancel` | 0.45 | 0.03 | `UiSelect`(0.55)보다 **일부러 낮게**. 취소가 확정보다 크면 안 된다 |
| `EnemyDieElite` | **0.75** | 0.04 | `EnemyDie`(0.40)보다 확실히 커야 "무거운 게 죽었다"가 된다 |
| `BossAppear` | **0.95** | 0 | 게임에서 가장 큰 순간. `PlayerDie`(0.90)보다 위 |

### 어떻게 확인하나 (판정 기준)

1. 컴파일 에러 **0**
2. `AudioLibrary.DescribeMissing()` 이 **`(none)`** 을 반환
   (`Crit` 을 2단계로 미뤘다면 `Crit` **하나만** 남는 게 정상)
3. 실플레이 — **소리로 판정한다. 로그로는 안 된다**
   - 터렛을 세우고 가만히 서 있으면 **터렛이 쏠 때마다 소리가 난다**
   - 원거리 적(Demon)이 쏠 때 소리가 난다
   - 식당이 회복시킬 때 소리가 난다
   - 리롤을 눌렀을 때 **구매음과 다른 소리**가 난다
4. 🔴 **엘리트·보스 사망음이 잡몹과 다르게 들린다** — Elite/Boss 노드로 가야 한다.
   노말 웨이브에서는 **검증할 수 없다**
5. 🔴 **플레이어 발사음이 터렛 소리에 묻히지 않는지** — 터렛 3채 이상 세우고 확인.
   묻히면 `BuildingFire` 볼륨을 0.22 → 0.15 로 먼저 내린다 (`AudioLibrary.asset`)

### 사용자 결정 사항 (2026-08-30) — 기존 `TODO.md` 항목을 **대체한다**

`TODO.md` §3 에 **"적 6종이 같은 사망음을 쓴다 → `EnemyData` 에 `SfxId` 열을 두자"**
라고 적혀 있는데, **그 방향이 아니다.** 사용자 지시는 이렇다:

> **잡몹은 전부 같은 소리. 엘리트·보스만 다르게.**

그래서 `EnemyData` 에 열을 추가하지 않는다. 이유:

- 종류별로 가르면 **클립이 6개** 필요하다. 엘리트 방식은 **1개**다
- 사망음은 초당 수십 번 울린다. 6종이 섞이면 구분이 아니라 **소음**이 된다
- 이미 `PlayDeathImpact`(`EnemyBase.cs:466`)가 **엘리트/보스에만** 흔들림·히트스톱을 건다.
  같은 경계를 쓰면 **연출과 소리가 한 몸으로** 움직인다
- 등급은 `EnemyData` 가 아니라 **소환 시점 인자**라(`Initialize(data, isElite, isBoss)`)
  애초에 데이터 열로 표현할 수 없다

→ **`TODO.md` §3 의 그 줄은 이 요청으로 닫아 주기 바란다** (`TODO.md` 는 DEV 통합 담당).

> ℹ️ **보스와 엘리트를 가르지 않은 것도 지시대로다.** 나중에 보스만 따로 필요해지면
> 예약해 둔 `15` = `EnemyDieBoss` 를 쓰면 된다. 지금 나누지 않는 이유는
> 클립 1개로 먼저 들어 보고 정하는 게 싸기 때문이다.

---

## 요청-4 — 실플레이 스크린샷 2장으로 확인된 것 (`TODO.md` §1 갱신)

사용자가 2026-08-30 실플레이 스크린샷 2장을 줬다. **`TODO.md` 는 DEV 통합 담당**이라
여기에 남긴다. 아래 3건은 **화면에서 직접 확인된 것**이다.

| `TODO.md` 위치 | 문구 | 스크린샷이 보여 준 것 |
|---|---|---|
| §1 (`[E] PROMOTE` 확인) | "다가가면 `[E] PROMOTE — Sentinel` 이 뜨는지" | ✅ **떴다.** `[E] PROMOTE — Sentinel` 그대로 |
| §1 (진화 안내 4단 우선순위) | "①과 ③만 화면에서 확인할 수 있다" | ✅ ① 확인. 추가로 **④의 승급 쪽**도 확인 — `PROMOTION READY  Sentinel — stand by your Turret and press E` |
| §1 (안내 문구가 난전에서 보이는가) | HUD 하단 `y=140` | ✅ 데미지 팝업과 겹치는 프레임이 있지만 **읽힌다** |

**②(`[E] EVOLVE`)는 여전히 안 뜬다 — `TODO.md` 의 서술이 맞다.**
확인차 진화 SO 3종의 재료 GUID 를 `ItemData/*.meta` 로 풀어 봤다:
Excalibur=Sword+Damage · Windforce=Bow+CritChance · Devastator=Gun+Fireball.
**셋 다 건물 재료가 없다.** `EvolutionData.IsFinalEvolution => AltarBuilding != null`
(`:54`)이므로 제단 무기 진화는 **데이터상 성립하지 않는다.** 버그가 아니라 콘텐츠 공백이다.

> ⚠️ 나도 한 번 헷갈렸다 — `[E] PROMOTE` 와 `[E] EVOLVE` 는 **둘 다 노란 `[E]` 가 터렛 옆에**
> 뜬다. 앞으로 이 경로를 검증할 때는 **단어까지 확인**해야 구분된다.

### 같이 보이는 것 둘 (요청 아님 — 참고)

- **콘솔에 `\uAE30`(기) · `\uC635`(옵) · `\uC158`(션) 폰트 경고가 그대로 있다** → **B4 미처리.**
  경고 문구가 내가 `BUGS.md` 에 적어 둔 판정 기준과 **글자까지 같다.**
  영문화가 끝나면 이 경고가 **0건**이 되는 것으로 판정하면 된다
- 스크린샷 1의 화면 오른쪽에 **작은 점들이 격자로 뭉친 것**이 보인다 — **B2 그대로다.**
  요청-1(시트 덮어쓰기)이 들어가면 사라져야 한다. **임포트 후 같은 자리를 다시 볼 것**

---

## 요청-5 — `Weapons.csv` Import 1회 (진화 아이콘 3종) (C6)

> ✅ **`닫힘(D7)`** (2026-08-30) — Import 1회로 guid 3개 교체 완료. 판정 4개 전부 통과.
> `git diff` 가 **파일당 `Icon` 한 줄뿐**이라 다른 수치는 안 움직였다.
> `TODO.md` §4 두 곳(`:328` 항목 · I-57 절 문장)도 같이 닫았다 → [`DONE/D7.md`](../DONE/D7.md)

**무엇을** — `Assets/Game/Balance/Weapons.csv` 를 Import 해서
진화 무기 3종의 `WeaponData.Icon` 을 전용 스프라이트로 갈아 끼운다.

**왜** — 전용 아이콘이 **이미 있는데 안 쓰이고 있었다.**
`Assets/Game/Sprites/Weapons/{Excalibur,Windforce,Devastator}.png` 가 **I-57(`a85cc9f`)에**
들어왔는데 `Weapons.csv` 의 `Icon` 열은 계속 재료 무기(Sword/Bow/Gun)를 가리키고 있었다.
확인 근거 — `Assets/Game/WeaponData/Excalibur.asset:17` 의 `Icon` guid 가
`009d725f…` = **`Sword.png` 의 guid** 다 (`Excalibur.png` 는 `c35a002a…`).

**수치는 한 글자도 안 건드렸다.** 바뀐 건 `Icon` 열 3칸과 주석 한 줄뿐이다.

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | Import 후 `Assets/Game/WeaponData/Excalibur.asset` 의 `Icon` guid 가 **`c35a002a740ab5c4aa5a673ce063905a`** 로 바뀐다 |
| ② | Windforce·Devastator 도 각각 `Windforce.png` · `Devastator.png` 의 guid 를 가리킨다 |
| ③ | `Damage`/`Cooldown` 등 나머지 필드가 **안 바뀐다** (Excalibur `70` / `0.55` 그대로) |
| ④ | 콘솔에 `[BalanceImporter] Import 완료` · 에러 0 |

> ℹ️ **실플레이 검증은 필요 없다.** 진화 아이템은 보물상자에서만 나와 화면에 띄우기가 비싸다.
> guid 3개면 충분하다 — 아이콘은 SO 필드를 그대로 그리는 경로라 중간에 가공이 없다.

**같이 닫아 줄 것** — `TODO.md` §4 의 "진화 무기 전용 아이콘이 없어 재료 것을 쓴다" 항목.
그림이 없던 게 아니라 **배선이 빠져 있던 것**이었다.

> ⚠️ Import 는 **플레이 모드에서 실패한다** (`MarkSceneDirty`). `ManageEditor(Stop)` 먼저.
> `D6`(요청-2) 가 어차피 Import 를 돌리므로 **같은 판에 묶어도 된다** — 다만 그 경우
> 위 ③(수치 불변)을 `CombatFeel` 변경과 헷갈리지 않게 볼 것. 둘은 다른 CSV 다.

---

## 요청-6 — 화살 스프라이트 임포트 + `Proj_Arrow.prefab` 신설 (C7)

> ✅ **D9 로 닫혔다 (2026-08-30, DEV).** 상세: [`DONE/D9.md`](../DONE/D9.md)
>
> - ①②③ 순서 지켰다. 프리팹을 먼저 만들고 Import 했으므로 `ProjectilePrefab` 이 `0` 이 된 적은 없다
> - 🔴 **④를 눈으로 봤다.** 플레이 모드에서 화살 4개를 +X/+Y/−X/−Y 로 세우고
>   게임 카메라(`Camera.main`)로 캡처했다. **네 방향 모두 촉이 진행 방향을 향한다.** 뒤집힘 없음
> - ⑤도 봤다. `Proj_Bullet` 을 대조군으로 같이 쏴서 **이동 거리가 2.37 로 완전히 같았다**
>   (둘 다 표적에서 0.63 떨어진 지점에서 소멸). 스택 트레이스로
>   `OnTriggerEnter2D → TakeDamage → Die` 까지 확인
> - ⚠️ **판정 ① 의 기대값 `(0.391, 0.141)` 은 실제로 `(0.500, 0.500)` 이 나온다 — 정상이다.**
>   Single 스프라이트의 `bounds.size` 는 **텍스처 전체 rect** 기준이라 256/512 = 0.5 다.
>   요청서 숫자는 **알파 bbox**(200×72px) 기준이라 `200/512=0.391` · `72/512=0.141` 이다.
>   둘 다 맞는 숫자이고 **PPU 는 정확히 512** 다 — ①이 잡으려던 "PPU 1024" 는 아니다
>   (1024 였다면 0.25 가 나왔다). 그림의 실제 폭 0.391 은 기존 총알 0.41 과 사실상 같다
> - `_Incoming/Projectiles/Arrow.png` 삭제 완료. `Shuriken.png` 는 요청-7 용이라 남겼다
> - PPU 체감 조절은 [`REQ/CONTENT.md`](CONTENT.md) 요청-3 으로 넘겼다 (`TUNING.md` 는 CONTENT 소유)
> - 덤으로 버그 하나를 적었다 — [`BUGS.md` B5](../BUGS.md). 게임 경로에선 안 나므로 **고치지 않았다**

**무엇을** — 3단계다. **순서가 중요하다.**

| 순 | 할 일 |
|---|---|
| ① | `_Incoming/Projectiles/Arrow.png` → `Assets/Game/Sprites/Projectiles/Arrow.png` 로 옮기고 임포트 |
| ② | `Assets/Prefabs/Proj_Bullet.prefab` 을 **복제**해서 `Assets/Prefabs/Proj_Arrow.prefab` 으로 만들고, `SpriteRenderer.Sprite` 만 위 `Arrow.png` 로 바꾼다 |
| ③ | `Assets/Game/Balance/Weapons.csv` 를 Import (`Bow` 행의 `ProjectilePrefab` 이 `Proj_Arrow.prefab` 을 가리키도록 이미 고쳐 뒀다) |

🔴 **②를 ③보다 먼저 해야 한다.** 프리팹이 없는 상태로 Import 하면
`BowData.ProjectilePrefab` 이 **null 로 덮인다** — 활이 아무것도 안 쏘게 된다.

### ① 임포트 설정 (이것만 다르면 안 된다)

| 항목 | 값 | 이유 |
|---|---|---|
| **Pixels Per Unit** | **512** | 캔버스 256px ÷ 512 = **0.5 유닛** — 지금 총알(50px @ PPU 100)과 **같은 폭**이다. 기본값 1024 로 들어가면 절반이 된다 (I-58 / B2 와 같은 함정) |
| Filter Mode | **Point (no filter)** | 픽셀아트다. Bilinear 면 뭉갠다 |
| Compression | **None** | 얇은 화살대(4px)가 압축에 뭉개진다 |
| Sprite Mode | Single | 시트가 아니다 |
| Pivot | Center | `ProjectileBase.cs:25` 가 이 점을 중심으로 회전시킨다. **화살대 중간이 회전축이어야** 한다 |
| Alpha Is Transparency | ✅ | |

### ② 프리팹 — 복제해서 스프라이트만 바꾼다

**새로 만들지 말고 복제할 것.** `Proj_Bullet` 이 가진 것들을 빠뜨리면 조용히 안 맞는다:

| 요소 | 값 | 빠뜨리면 |
|---|---|---|
| Layer | `8` | 충돌 매트릭스에서 빠져 적을 못 맞힌다 |
| Tag | `Projectile` | |
| `Rigidbody2D` | Kinematic · GravityScale 0 · Constraints `4`(회전 고정) | 트리거가 안 돈다 |
| `CircleCollider2D` | **IsTrigger ✅ · Radius 0.5** | `OnTriggerEnter2D` 가 안 불린다 |
| `ProjectileBase` | guid `4bd4e01df5e3f2d45953279a99475255` | |
| `SpriteRenderer.DrawMode` | `0` (Simple) | Sliced 면 크기가 이상해진다 |

바꾸는 건 **`m_Sprite` 한 줄뿐**이다 (현재 `1f4e1489…` = `ICON/Bullet.png`).

**왜** — 활이 **총알 아이콘**을 쏘고 있었다. `Proj_Bullet` 이 그리는 건
`Assets/Game/ICON/Bullet.png` (`icons8-총알-50_0`) — icons8 벡터 아이콘이라
픽셀아트인 이 게임과 결이 다르고, 무엇보다 **활에서 총알이 나간다.**
`DESIGN_CLASSES.md` §6 **1단계**이고 **코드가 0줄**인 유일한 항목이라 먼저 잡았다.

그림은 `Bow.png` 에서 팔레트를 직접 뽑아 그렸다 (외곽선 `(3,1,23)` · 나무 4단 · 강철 4단 · 깃 3단).
**오른쪽(+X)을 향한다** — `ProjectileBase.cs:25-26` 이 `Mathf.Atan2(dir.y, dir.x)` 로 회전시키고
`Vector2.right` 로 전진시키기 때문이다. 방향이 모호하면 **기능 실패**라
깃을 줄이고 촉을 길게 빼서 앞뒤가 헷갈리지 않게 두 번 다시 그렸다.

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | `Arrow.png` 임포트 후 스프라이트 `bounds.size` 가 **약 `(0.391, 0.141)`** — 세로가 0.07 근처면 **PPU 가 1024 로 들어간 것**이다 |
| ② | `Assets/Game/WeaponData/Bow.asset` 의 `ProjectilePrefab` guid 가 **`Proj_Arrow.prefab` 의 guid** 다. **`0` 이면 ②를 ③보다 늦게 한 것** |
| ③ | `Bow` 의 나머지 수치가 안 바뀐다 — `ProjectileSpeed 14` · `Damage 8|12|17|24|33` |
| ④ | **실플레이**: 활을 들고 적을 향해 쏘면 **화살이 날아가는 방향으로 촉이 향한다.** 위·아래·왼쪽으로 쏴도 뒤집히지 않는다 |
| ⑤ | 화살이 적에 닿으면 사라지고 피해가 들어간다 (콜라이더가 살아 있다) |
| ⑥ | 콘솔에 `[BalanceImporter] Import 완료` · 에러 0 |

> ④가 **이 요청의 본체**다. ①②③은 ④가 실패했을 때 어디서 틀렸는지 가르는 용도다.
> 나는 화면을 못 보므로 **④를 봤는지 아닌지**를 명확히 적어 줄 것.

> ⚠️ Import 는 **플레이 모드에서 실패한다** (`MarkSceneDirty`). `ManageEditor(Stop)` 먼저.
> ⚠️ 옮긴 뒤 **`_Incoming/Projectiles/Arrow.png` 는 지운다** (규칙대로).
> ℹ️ **B1(가장자리 번짐)** 때문에 좌우 여백을 28px 두었다. 화살 앞뒤가 번지면 B1 이지 그림이 아니다.

**같이 봐 줄 것** — 크기가 "너무 크다/작다"면 그건 그림이 아니라 **PPU 조절**이다.
[`TUNING.md`](../../TUNING.md) §3 3단계에 적어 뒀다. 지금 고치지 말고 값만 알려 줄 것.

---

## 요청-7 — 수리검 신규 + 검 근접화 (C8 / `DESIGN_CLASSES.md` §6 2단계)

> ✅ **D10 으로 닫혔다 (2026-08-30, DEV).** 상세: [`DONE/D10.md`](../DONE/D10.md)
>
> - 그림 3장 임포트 · 프리팹 3개 신설 · `MeleeWeapon`·`SwingArcFx` 신설 ·
>   `ProjectileBase` 에 관통+자전. **기존 파일 수정은 `ProjectileBase.cs` 하나뿐이다**
> - 🔴 **⑦(호의 중심·크기)을 눈으로 봤다.** 6프레임은 0.2초라 그냥 찍으면 한 장밖에 안 잡혀서,
>   **프레임 전부를 플레이어 중심에 겹쳐 세워** 부채꼴을 한 장으로 만들고 반지름 **정확히 2.0** 위에
>   빨간 점 13개, 플레이어 자리에 초록 점을 찍어 게임 카메라로 캡처했다.
>   **오목한 안쪽이 초록 점을 감싸고, 바깥 흰 테두리가 빨간 점들 위에 정확히 얹혀 있다.**
> - 🔴 **③(관통 회귀)은 `Proj_Bullet` 을 대조군으로 같이 쏴서 봤다.** 일렬 3마리에서
>   총알은 **1번째** 적 0.63 앞, 수리검은 **3번째** 적 0.63 앞에서 소멸.
>   기존 프리팹은 `pierce=1 spin=0` 이고 `.prefab` **파일 자체가 미변경**이다
> - ⚠️ **요청서의 "셀 반지름 128px" 전제가 틀렸다** — 실측 **107.4px** 이다.
>   `localScale = Range` 로 뒀으면 **호가 사거리를 7% 부풀려 보여 준다**(계약 ② 위반).
>   `spriteRadiusAtScaleOne = 1.074` 로 나눠서 맞췄다. **이 값은 실측이니 흔들지 말 것**
> - ⚠️ **자전을 넣으면 발사체가 나선을 그린다** — 이동이 `Translate(Space.Self)` 였다.
>   월드 `_direction` 으로 분리했고, **기존 발사체에게는 수식이 완전히 같다**
> - ⏸ **⑤(레벨업 3택·상점에 Shuriken)만 못 닫았다 — CSV 3줄이 없어서다.**
>   → [`REQ/CONTENT.md`](CONTENT.md) **요청-4** 로 넘겼다. **CSV 가 들어와야 게임에 나온다**
> - `_Incoming/{Projectiles/Shuriken.png, ICON/Shuriken.png, Effects/SwingArc.png}` 삭제 완료.
>   `BombGround.png`·`ToxinField.png`·`ICON/Toxin.png` 는 **요청-8 용이라 남겼다**

> 🟡 **지금 당장 하라는 요청이 아니다.** 요청-6(화살) 이 닫힌 뒤에 연다.
> **그림은 이미 다 나왔다** — 남은 게 코드뿐이라 여기 미리 적어 둔다.
> 순서를 지키는 이유: 화살은 *기존 무기의 그림만* 바꾼 것이라, 그게 먼저 통과해 있어야
> 여기서 뭔가 안 될 때 "신규 배선이 문제"라고 가릴 수 있다.

### 넘긴 그림 4장

| 파일 | 크기 | 임포트 설정 | 옮길 곳 |
|---|---|---|---|
| `_Incoming/Projectiles/Shuriken.png` | 256×256 | **PPU 512** · Point · 압축 None · Single · Pivot Center | `Assets/Game/Sprites/Projectiles/` |
| `_Incoming/ICON/Shuriken.png` | 1024×1024 | 기존 `Weapons/*.png` 와 **똑같이** (`Sword.png.meta` 복사가 제일 안전) | `Assets/Game/Sprites/Weapons/` |
| `_Incoming/Effects/SwingArc.png` | **1536×256** | **PPU 100** · Point · 압축 None · **Multiple** · Grid By Cell Size **256×256** · Pivot Center | `Assets/Game/Sprites/Effects/` |
| (요청-6 의 `Arrow.png`) | | | |

`SwingArc.png` 는 **가로 6프레임 한 줄**이다 (`Explosion.png` 의 4×4 와 다르다).
Grid 로 자르면 `SwingArc_0` … `SwingArc_5` 가 **왼쪽→오른쪽** 순으로 나온다. 그 순서가 재생 순서다.

전부 `alpha 0..255` 를 확인했다 — **I-41 가짜 투명이 아니다.**
여백은 수리검 40px · 호 24px 이상이라 **B1(가장자리 번짐) 여유 30px 규칙 안**이다.

---

### A. 수리검 — 새 무기 (관통)

🔑 **새 `WeaponBase` 파생을 만들지 마라.** 수리검은 `ProjectileWeapon` 그대로 쓴다.
필요한 건 **관통 하나뿐**이고, 그건 투사체 쪽 문제다.

**제안하는 최소 변경** — `ProjectileBase` 에 필드 하나:

```csharp
[SerializeField] int pierceCount = 1;   // 1 = 지금 동작(첫 적에 사라진다)
```

`OnTriggerEnter2D` 에서 적을 때릴 때마다 1 깎고, **0 이 되면** `Despawn()`.
🔑 **기본값을 `1` 로 두면 기존 프리팹 3종(`Proj_Bullet`·`Proj_Arrow`·`Proj_Aoe`)의 동작이
한 글자도 안 바뀐다.** CSV 열도, `Initialize()` 시그니처도 안 건드린다.

> ⚠️ **같은 적을 여러 번 때리지 않게 할 것.** 트리거는 한 콜라이더 안에서 여러 프레임
> 반복해 들어온다. 맞힌 적을 목록에 담아 두거나, 풀에서 꺼낼 때 그 목록을 비울 것.
> 안 하면 관통 3이 **한 마리를 3번** 때리고 끝난다.

**만들 것**

1. `Assets/Prefabs/Proj_Shuriken.prefab` — `Proj_Bullet` 복제 → 스프라이트만 교체
   (Layer `8` · Tag `Projectile` · Rigidbody2D Kinematic · CircleCollider2D **IsTrigger·Radius 0.5** · `ProjectileBase`)
   - `pierceCount` = **3**
   - 회전: `BombProjectile.spinSpeed`(540) 와 같은 방식으로 **계속 돌린다.**
     🔑 수리검 그림은 **4회 대칭이라 방향이 없다** — 진행 방향 회전은 필요 없고 자전만 하면 된다

2. 아래 CSV 두 줄은 **프리팹이 생긴 뒤에 CONTENT 가 넣는다.** 여기 미리 적어 두는 건
   수치를 미리 보고 이상하면 말해 달라는 뜻이다. **DEV 가 CSV 를 고치지는 말 것.**

```
# Weapons.csv (12필드)
Shuriken,Shuriken,Assets/Prefabs/Weapon_Sword.prefab,Assets/Game/Sprites/Weapons/Shuriken.png,Assets/Prefabs/Proj_Shuriken.prefab,,16,6|9|13|18|25,0.9|0.8|0.7|0.6|0.5,0.9|0.95|1|1.1|1.2,1|1|2|2|3,12|12|14|14|16

# Items.csv (8필드)
Shuriken,Shuriken,Fast piercing stars that cut through several foes.,Assets/Game/Sprites/Weapons/Shuriken.png,Weapon,5,Shuriken,9

# SceneWiring.csv — LevelUpManager,allItems 끝에 이어 붙인다
|Assets/Game/ItemData/Shuriken.asset
```

> ⚠️ `SceneWiring.csv` 에 안 넣으면 **레벨업 3택에도 상점에도 안 나온다.** 조용히 없는 무기가 된다.

---

### B. 검 — 근접 호로 개조

🔴 **지금 검은 검이 아니다.** `Weapons.csv` 의 `Sword` 행이
`Weapon_Sword.prefab`(= `ProjectileWeapon`) 으로 `Proj_Bullet` 을 **사거리 10** 에 쏜다.
화면 절반 밖의 적을 총알로 맞히는 무기다.

**만들 것** — `MeleeWeapon : WeaponBase`. `Fire()` 에서:

1. `FindNearestEnemy()` 로 방향을 구한다 (`WeaponBase` 가 이미 준다)
2. 그 방향으로 **꽉 찬 부채꼴** 판정 — `OverlapCircleAll(플레이어, Range)` 후
   **각도 차 ≤ 70°** 인 적만 남긴다 (호 그림이 쓸고 지나가는 각이 142° 다)
3. `TakeDamage(dmg, from)` 의 `from` 에 **플레이어 위치**를 넘긴다 —
   그래야 적이 플레이어 반대편으로 밀린다. 호 중심을 넘기면 방향이 이상해진다
4. 휘두름 이펙트를 하나 띄운다 (아래 C)

**바뀌는 CSV** (역시 CONTENT 가 넣는다 — 여기 미리 보여 주는 것뿐):

```
Sword,Sword,Assets/Prefabs/Weapon_Melee.prefab,…,Assets/Prefabs/Fx_SwingArc.prefab,,0,<Damage 재조정>,<Cooldown>,…,1|1|2|2|3,2|2|2.2|2.2|2.5
```

- `WeaponPrefab` → 새 `Weapon_Melee.prefab`
- `ProjectilePrefab` 열을 **휘두름 이펙트 프리팹**으로 재해석한다 (투사체가 없으니 이 칸이 논다)
- `ProjectileSpeed` 는 안 쓴다 → `0`
- `Range` **10|10|12|12|15 → 2|2|2.2|2.2|2.5**
- `ProjectileCount` 는 **연타 횟수**로 재해석 (Lv5 = 3연타)

> 🔴 **`Damage` 는 반드시 같이 올려야 한다.** 사거리를 10→2 로 자르면 같은 수치가 아니다.
> 다만 **얼마나** 올릴지는 플레이해야 안다 → [`TUNING.md`](../../TUNING.md) §3 **1단계**.
> **이 요청에서 감으로 정하지 말 것.**

> ⚠️ **Excalibur 도 같이 봐야 한다.** 진화한 검인데 설명이
> "A wide arc that cleaves five foes" 다. 지금은 `Weapon_Sword.prefab` 을 쓴다.
> 검이 근접이 되면 **Excalibur 만 원거리로 남는다.** 다만 이번 요청에 묶지는 말 것 —
> 기본 검이 먼저 통과해야 진화 쪽 실패를 가릴 수 있다

---

### C. 휘두름 호 이펙트 — 그림이 코드에 거는 조건 3개

`Fx_SwingArc.prefab` (SpriteRenderer + 6프레임 재생 + 자동 반납/파괴).
🔴 아래 3개는 **취향이 아니라 계약**이다. 어기면 **보이는 것과 맞는 것이 어긋난다.**

| # | 조건 | 어기면 |
|---|---|---|
| ① | **회전 중심 = 프레임 중앙 = 플레이어 위치.** 호는 오른쪽 반쪽에만 그려져 있다. 플레이어에 붙이고 적 방향으로 `z` 만 돌린다 | 호가 엉뚱한 데서 돈다 |
| ② | **바깥 반지름 = scale 1 에서 정확히 1.0 유닛** (셀 256px / 반지름 100px / PPU 100). `localScale = Range` 로 둘 것 — `AoeProjectile.spriteRadiusAtScaleOne = 0.5` 와 **같은 방식**이다 | 그림이 사거리를 속인다 |
| ③ | **피해 판정은 프레임 2~3 에서 한 번만.** 6프레임 내내 판정하지 말 것 | 연타 설계(`ProjectileCount`)와 겹쳐 **두 배로** 때린다 |

프레임별 각도 (0°=오른쪽, +가 위 · 위→아래로 벤다):
`70→48` · `72→18` · `68→−22` · **`34→−60`(베는 순간)** · `−4→−70` · `−40→−72`

> ℹ️ 호의 **안쪽은 비어 있다** (가장 두꺼울 때도 반지름 0.36~1.0 만 채운다).
> 그래도 **판정은 도넛으로 만들지 말 것** — 붙어 있는 적이 안 맞으면 근접 무기가 아니다.

---

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | `SwingArc.png` 가 **6장으로 잘린다** (`SwingArc_0`…`_5`). 프레임 하나의 `bounds.size` 가 **약 `(2.56, 2.56)`** |
| ② | `Shuriken.png` 스프라이트 `bounds.size` 가 **약 `(0.5, 0.5)`** — 0.25 면 **PPU 가 1024 로 들어간 것** |
| ③ | **관통 회귀 검사**: 기존 `Bow`/`Gun`/`Fireball` 이 **예전처럼 첫 적에서 사라진다.** `pierceCount` 기본값 1 이 안 먹으면 여기서 걸린다 |
| ④ | **관통 동작**: 수리검 한 발이 일렬로 선 적 **3마리**를 뚫는다. 🔴 **한 마리를 3번** 때리는 게 아니다 — 적 3마리의 HP 가 각각 줄어야 한다 |
| ⑤ | 레벨업 3택과 상점에 **Shuriken 이 나온다** (`SceneWiring.csv` 배선 확인) |
| ⑥ | **실플레이 — 검**: 적이 붙었을 때만 맞는다. **화면 반대편 적이 안 맞는다** (지금은 맞는다) |
| ⑦ | **실플레이 — 호**: 호가 **플레이어를 중심으로** 적 쪽을 향해 그려지고, 호 바깥 끝이 **실제로 맞는 거리와 같다** |
| ⑧ | 적이 **플레이어 반대 방향으로** 밀린다 (`from` = 플레이어) |
| ⑨ | 콘솔 에러 0 |

> ⑦이 이 요청의 본체다. 나는 화면을 못 보므로 **⑦을 봤는지**를 명확히 적어 줄 것.
> ③은 빠뜨리기 쉬운데, 여기서 깨지면 **무기 4종이 동시에 이상해진다.**

**같이 남겨 줄 것** — 검의 `Damage`/`Cooldown` 과 수리검의 `pierceCount`(3) 는
전부 **감으로 넣은 값**이다. 플레이하면서 "세다/약하다"만 알려 주면 CONTENT 가
[`TUNING.md`](../../TUNING.md) §3 에 반영한다. **DEV 가 CSV 를 직접 고치지는 말 것.**

---

## 요청-8 — 바닥 폭탄 + 독 장판 (C9) · §6 3·4단계

> ✅ **처리 완료 — D11 (2026-08-30, DEV).** 판정 ①~⑧·⑩ 전부 통과.
> 상세는 [`DONE/D11.md`](../DONE/D11.md).
>
> **요청서와 다르게 한 것 2가지 (사용자 승인)** — `ToxinField.png` 의
> **Pivot Center** 와 **`spriteRadiusAtScaleOne = 1.0`** 은 그림과 맞지 않았다.
> 실측 중심이 셀 중앙이 아닌 `(134, 132)px`, 반지름이 128px 이 아닌 `≈97px` 이라
> **Custom pivot `(0.5234, 0.4844)` + `0.97`** 로 보정했다. 화면 픽셀 검증 오차 **0.7px**.
>
> **판정 ⑨(레벨업 3택·상점)만 남았다** — CSV 는 CONTENT 몫이라
> [`REQ/CONTENT.md` 요청-5](CONTENT.md) 로 넘겼다. 🔴 **그게 들어와야 게임에 나온다.**
> 체감값 7개(`fuseTime` · `fieldDuration` · `tickInterval` · `slowMult` 등)도 거기 있다.

> 🟡 **지금 당장 하라는 요청이 아니다.** 요청-7 이 닫힌 **뒤에** 연다.
> 미리 적어 두는 이유는 **그림이 코드보다 먼저 끝났기 때문**이다 —
> 규격을 잊기 전에 못 박아 둔다. 계약 전문은
> [`DESIGN_CLASSES.md` §5-A](../../DESIGN_CLASSES.md).

**넘기는 그림 3장**

| `_Incoming/` | → `Assets/` | 임포트 |
|---|---|---|
| `Effects/BombGround.png` (1024×256) | `Game/Sprites/Effects/BombGround.png` | **PPU 256** · Multiple · Grid By Cell Size **256×256** · Point · Compression None |
| `Effects/ToxinField.png` (1536×256) | `Game/Sprites/Effects/ToxinField.png` | **PPU 100** · Multiple · Grid By Cell Size **256×256** · Point · Compression None |
| `ICON/Toxin.png` (1024×1024) | `Game/Sprites/Weapons/Toxin.png` | 기존 `Weapons/` 아이콘과 동일 (Single) |

셋 다 Alpha Is Transparency 켬. 🔴 **`ToxinField` 의 PPU 는 100 이다** — 256 으로 들어가면
장판이 **2.56배 작아지고**, 아래 B-3 반지름 계약이 통째로 깨진다.

---

### A. 바닥 폭탄 (W2) — 🔑 **새 클래스가 필요 없다**

코드를 읽고 확인했다. `Bomb` 은 이미 `Weapon_Aoe`(`AoeWeapon`) 를 쓰고,
`AoeWeapon.LaunchTravel()` 은 `TravelPrefab` + `ProjectileSpeed > 0` 이면
**`BombProjectile` 을 목표 좌표로 던진 뒤 터뜨린다.** 곡사포가 이미 그렇게 동작한다.

즉 **"던져서 바닥에 놓는다"는 이미 있다.** 없는 건 **도착 후 기다리는 것** 하나뿐이다.

**제안하는 최소 변경 — `BombProjectile` 에 필드 2개**

```cs
[Tooltip("도착한 뒤 터지기까지 기다리는 시간(초). 0 이면 즉시 터진다(곡사포).")]
[SerializeField] private float fuseTime = 0f;
[Tooltip("신관 동안 남은 시간에 비례해 넘길 그림. 비워 두면 안 바뀐다.")]
[SerializeField] private Sprite[] fuseFrames;
```

🔑 **기본값 0 이 곡사포를 그대로 지킨다.** `pierceCount = 1`(요청-7) 과 같은 방식이다 —
기존 프리팹(`Proj_Bomb`)은 손대지 않아도 예전과 똑같이 동작한다.

`Update()` 에서 `_progress >= 1f` 일 때 바로 `Explode()` 하는 대신:

1. 위치를 `_target` 에 고정하고 **회전을 멈춘다** (`transform.rotation = Quaternion.identity`).
   🔴 `spinSpeed` 가 계속 돌면 **바닥에 놓인 폭탄이 팽이처럼 돈다**
2. `fuseTime` 만큼 센 뒤 `Explode()`
3. 그동안 `fuseFrames[floor(경과/fuseTime * 4)]` 로 그림을 갈아 준다 (4장, 마지막 인덱스 clamp)

> ⚠️ **프레임은 시간이 아니라 게이지다.** 0=차가움 → 3=벌겋게 달아오름 순으로 그렸다.
> 일정 간격 루프로 돌리면 **언제 터지는지를 그림이 알려 주지 못한다.**

**새 프리팹** — `Assets/Prefabs/Proj_BombGround.prefab`
: `Proj_Bomb.prefab` 을 **복제**해서 만드는 게 가장 안전하다 (요청-6 과 같은 이유 —
  Layer·Rigidbody2D·풀 반환까지 딸려 온다). 바꿀 것:

| 항목 | 값 |
|---|---|
| `SpriteRenderer.sprite` | `BombGround_0` (첫 프레임) |
| `BombProjectile.fuseTime` | **1.1** (감으로 넣은 값 — `TUNING.md` §3 3단계에 등재해 뒀다) |
| `BombProjectile.fuseFrames` | `BombGround_0` … `_3` 4장 |
| `BombProjectile.spinSpeed` | **0 으로 만들지 말고 그대로 둔다** — 날아가는 동안엔 돌아야 한다. 멈추는 건 위 ①이 한다 |

⚠️ **접촉 기폭은 넣지 않는다.** 적이 밟고 지나가도 안 터진다 (§4 W2). 시간만으로 터진다.

**CSV** — `Bomb` 행의 `TravelPrefab` 을 채우고 `ProjectileSpeed` 를 0 → 9 로 올린다.
🔴 **CSV 는 CONTENT 가 쓴다.** 프리팹이 생긴 뒤에 알려 주면 그때 넣는다
(없는 경로로 Import 하면 `BombData.TravelPrefab` 이 null 로 덮인다 — 요청-6 과 같은 함정).

---

### B. 독 장판 (W5) — 🔴 **`EnemyBase` 를 건드리는 유일한 요청**

**B-1. `EnemyBase` 의 슬로우 — 상태 2개**

```cs
private float _slowMult = 1f;   // 1 = 슬로우 없음. 작을수록 느리다
private float _slowUntil;       // 이 시각을 넘기면 저절로 풀린다

public void ApplySlow(float mult, float duration)
{
    // "가장 센 것 하나만" — 겹친 장판 2개가 0.6*0.6=0.36 이 되면 적이 멈춘다
    if (Time.time > _slowUntil || mult < _slowMult) _slowMult = mult;
    _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
}
```

🔑 **장판이 틱마다 다시 걸고, 시간이 지나면 저절로 풀리게 한다.**
이러면 "적이 장판에서 나갔다"를 **아무도 추적하지 않아도 된다** —
안 걸어 주면 알아서 풀린다. Enter/Exit 짝을 맞추는 코드는 반드시 어딘가에서 어긋난다.

적용은 **속도를 덮어쓰지 말고 곱한다.** `MoveSpeed` 를 직접 깎으면 복구할 원본이 사라진다:

| 파일 · 줄 | 지금 | 바꿀 것 |
|---|---|---|
| `EnemyBase.cs:164` | `Steer(...) * MoveSpeed` | `* CurrentSpeed` |
| `EnemyBase.cs:254` | `Steer(desired) * MoveSpeed` | `* CurrentSpeed` |
| `EnemyBase.cs:333` | `MoveSpeed * Data.ChargeSpeedMult` | `CurrentSpeed * Data.ChargeSpeedMult` |

`protected float CurrentSpeed => MoveSpeed * (Time.time <= _slowUntil ? _slowMult : 1f);`

> 🔴 **`Setup()` 에서 `_slowMult = 1f; _slowUntil = 0f;` 로 반드시 초기화할 것.**
> 적은 풀에서 재사용된다. 안 지우면 **다음 웨이브의 멀쩡한 적이 느린 채로 태어난다** —
> 그리고 이건 로그에 아무것도 안 남아서 "적이 왜 이렇게 느리지"로만 보인다

**B-2. 새 무기 — `FieldWeapon : WeaponBase` + `ToxinField`**

`AoeWeapon` 을 고치지 말고 **새로 만든다.** 노리는 곳이 다르다 —
`AoeWeapon` 은 *가장 가까운 적*, 이건 *플레이어 주변 무작위 좌표*다 (지시: "랜덤 바닥에").

```cs
// FieldWeapon.Fire()
Vector2 spot = (Vector2)transform.position + Random.insideUnitCircle * Data.GetRange(Level);
// → Pool.Get(Data.ProjectilePrefab, spot, ...) → ToxinField.Initialize(...)
```

`ToxinField` 가 할 일:

1. `localScale = Vector3.one * radius` — **`radius` 가 곧 그림 반지름이다** (아래 B-3)
2. `tickInterval` 마다 `Physics2D.OverlapCircleAll(spot, radius, Enemy)` →
   - `TakeDamage(damage)` — 🔴 **`from` 을 넘기지 말 것.** 넘기면 초당 여러 번 밀려서
     적이 장판 밖으로 튕겨 나간다. 기본값이 `null` 이라 **그냥 생략하면 된다**
   - `ApplySlow(slowMult, tickInterval * 1.6f)` — 틱보다 조금 길게 걸어야 틱 사이에 안 풀린다
3. `fieldDuration` 이 지나면 `Pool.Return`
4. 6프레임을 **그냥 루프**로 돌린다 (이음매 없이 순환하게 그렸다. 핑퐁 아님)

**프리팹 2개**

| 프리팹 | 내용 |
|---|---|
| `Assets/Prefabs/Weapon_Field.prefab` | `FieldWeapon` 하나만. `Weapon_Aoe.prefab` 복제가 편하다 |
| `Assets/Prefabs/Proj_ToxinField.prefab` | `SpriteRenderer`(`ToxinField_0`, **`sortingOrder` 를 적보다 아래로**) + `ToxinField`. **콜라이더 없음** — 판정은 `OverlapCircleAll` 로 한다 |

`ToxinField` 의 자리표시 값 (전부 감이다. `TUNING.md` §3 3단계에 등재해 뒀다):

| 필드 | 값 | 왜 |
|---|---|---|
| `fieldDuration` | 4.0s | 다음 장판이 깔릴 때까지 겹치는 구간이 생겨야 "지대"로 느껴진다 |
| `tickInterval` | 0.5s | 초당 2번. 더 잦으면 데미지 팝업이 화면을 덮는다 |
| `slowMult` | 0.6 | 40% 감속. 0.5 아래로 내리면 적이 사실상 멈춘다 |
| `spriteRadiusAtScaleOne` | **1.0** | 🔴 **그림이 정한 값이다. 고치지 말 것** (아래) |

**B-3. 반지름 계약** — `AoeProjectile.spriteRadiusAtScaleOne = 0.5` 와 **같은 방식**이다.

`ToxinField.png` 는 셀 256px 중 반지름 100px, PPU 100 → **scale 1 에서 정확히 1.0 유닛**.
따라서 `localScale = radius` 로 두면 **그린 원이 곧 맞는 원**이다.
🔑 이걸 안 하면 그림이 거짓말을 하고, 플레이어는 "밖에 있었는데 맞았다"고 느낀다.

⚠️ 몸통 알파가 **205**(불투명 아님)다. 장판 위에 선 적이 비쳐 보여야 해서 그렇게 그렸다.
진하게/옅게는 `color` 의 **알파가 아니라 색**으로 조정할 것 — 알파를 더 낮추면 초록이 탁해진다.

---

### C. CSV — **CONTENT 가 쓴다. 아래는 초안이니 그대로 붙여 넣지 말 것**

프리팹이 생긴 뒤 알려 주면 CONTENT 가 넣고, 그다음 DEV 가 Import 한다.

```
# Weapons.csv (12필드) — Damage 는 "틱당" 이다
Toxin,Toxin,Assets/Prefabs/Weapon_Field.prefab,Assets/Game/Sprites/Weapons/Toxin.png,Assets/Prefabs/Proj_ToxinField.prefab,,0,4|6|8|11|15,3|2.7|2.4|2.1|1.8,1|1.15|1.3|1.5|1.7,1|1|1|1|1,5|5|6|6|7

# Weapons.csv — Bomb 행 교체 (TravelPrefab 채움 + ProjectileSpeed 0 -> 9)
Bomb,Bomb,Assets/Prefabs/Weapon_Aoe.prefab,Assets/Game/Sprites/Weapons/Bomb.png,Assets/Prefabs/Proj_Aoe(Boom).prefab,Assets/Prefabs/Proj_BombGround.prefab,9,30|44|62|86|118,3.5|3.2|2.9|2.5|2,1.2|1.35|1.5|1.7|2,1|1|1|1|1,9|9|10|10|11

# Items.csv (8필드)
Toxin,Toxin,Drops a toxic pool that slows and burns anything inside.,Assets/Game/Sprites/Weapons/Toxin.png,Weapon,5,Toxin,9

# SceneWiring.csv — LevelUpManager,allItems 끝에 이어 붙인다
|Assets/Game/ItemData/Toxin.asset
```

`Toxin` 의 `Range`(5~7)는 사거리가 아니라 **장판이 떨어질 수 있는 반경**이다.
크게 잡으면 적이 없는 빈 땅에 깔린다.

---

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | `BombGround.png` 4장 · `ToxinField.png` 6장으로 잘린다. **`ToxinField` 한 장의 `bounds.size` 가 약 `(2.56, 2.56)`** — `(1.0, 1.0)` 이면 PPU 가 256 으로 들어간 것 |
| ② | 🔴 **곡사포 회귀 검사**: `Bombard` 건물의 폭탄이 **예전과 똑같이** 날아가 즉시 터진다. `fuseTime` 기본값 0 이 안 먹으면 여기서 걸린다 |
| ③ | 🔴 **적 속도 회귀 검사**: 장판을 한 번도 안 밟은 적이 **평소 속도로 걷는다.** 특히 **여러 웨이브를 넘긴 뒤**에도 (풀 재사용 초기화 확인) |
| ④ | **실플레이 — 폭탄**: 던져진 폭탄이 바닥에 **멈춰 서고**(돌지 않고) 점점 **벌겋게 달아오르다** 터진다. 언제 터질지 눈으로 안다 |
| ⑤ | **실플레이 — 장판**: 안에 들어간 적이 **눈에 띄게 느려지고**, 나오면 **돌아온다** |
| ⑥ | 🔴 **장판 2개를 겹쳐도 적이 멈추지 않는다** (가장 센 것 하나만 적용) |
| ⑦ | **도트 피해로 적이 밀리지 않는다** — 장판 안의 적이 제자리에서 닳는다. 튕겨 나가면 `from` 을 넘긴 것이다 |
| ⑧ | **그림 반지름 = 맞는 반지름** — 초록 원 밖의 적은 안 닳는다 |
| ⑨ | 레벨업 3택과 상점에 **Toxin 이 나온다** |
| ⑩ | 콘솔 에러 0 |

> ②③이 이 요청에서 가장 위험한 부분이다. 둘 다 **새 기능이 아니라 기존 것을 깨는 쪽**이고,
> 특히 ③은 깨져도 예외가 안 나서 **"적이 좀 느린데?" 로만 보인다.**
> ⑤⑥⑦은 나(CONTENT)는 화면을 못 보므로 **봤는지를 명확히 적어 줄 것.**

**같이 남겨 줄 것** — `fuseTime`(1.1) · `fieldDuration`(4.0) · `tickInterval`(0.5) ·
`slowMult`(0.6) 은 전부 감으로 넣은 값이다. "길다/짧다"만 알려 주면 CONTENT 가
[`TUNING.md`](../../TUNING.md) §3 에 반영한다. **DEV 가 CSV 를 직접 고치지는 말 것.**

---

## 요청-9 — CSV Import 1회 (C12) · **요청-4(D10) 의 나머지 절반**

> ✅ **닫힘 (D12, 2026-08-30).** Import 1회로 요청-10 과 같이 반영했다 —
> `Weapons: 10 · Items: 25 · SceneWiring 11/11`, 에러 0. **판정 ①~⑧ 전부 PASS.**
> 핵심 증거: 검 `SWORD proj=0 arc=13 hits=19 kills=14` (총알 0, 호로만 벤다) ·
> 수리검 관통 `hitsPerProj` 흩어짐 **0.91** vs 뭉침 **3.10** (프리팹 `pierceCount: 3` 과 일치) ·
> 레벨업 60회 추첨(180장)에서 `Shuriken` 4·6·8·11장.
> "느낌" 질문 3개 답 + 전체 상세 → [`DONE/D12.md`](../DONE/D12.md)

> 🔴 **이건 지금 해 달라는 요청이다** (요청-8 보다 먼저). 5분짜리이고, 이게 되면
> **§6 2단계가 닫힌다.** D10 이 만든 코드·프리팹·그림이 지금 **게임에 안 나오는 상태**다.

**무엇을** — `Game/Balance/Import CSV -> ScriptableObjects` **1회**.
CSV 3파일은 저장해 뒀다. 새 파일도, 새 열도 없다.

| 파일 | 무엇을 했나 |
|---|---|
| `Weapons.csv` | `Shuriken` 줄 **추가** · `Sword` 줄 **4개 열 교체** |
| `Items.csv` | `Shuriken` 줄 **추가** · `Sword` 의 `Description` 교체 |
| `SceneWiring.csv` | `LevelUpManager,allItems` 끝에 `Shuriken.asset` **1개 추가** |

⚠️ **플레이 모드에서는 Import 가 실패한다** (`MarkSceneDirty`). `ManageEditor(Stop)` 먼저.

---

### 내가 정한 값과 그 근거 — **동의 안 되면 고치지 말고 알려 줄 것**

**A. 수리검 (신규)**

```
Shuriken,Shuriken,Assets/Prefabs/Weapon_Sword.prefab,Assets/Game/Sprites/Weapons/Shuriken.png,Assets/Prefabs/Proj_Shuriken.prefab,,16,6|9|13|17|23,0.9|0.8|0.7|0.6|0.5,0.9|0.95|1|1.1|1.2,1|1|1|2|2,12|12|14|14|16
```

🔑 **`ProjectileCount` 를 일부러 낮게(1→2) 잡았다.** 요청-7 초안엔 `1|1|2|2|3` 이라고 썼는데
**내가 틀렸다.** 관통 3 위에 부채꼴 3 을 얹으면 **한 발이 최대 9번** 맞는다.
Bow Lv5(`60×3 = 180`) 대비 이론상 450 이 나와 다른 무기가 전부 무의미해진다.

| Lv5 | 단일 DPS | ×동시발사 | 관통 최대 |
|---|---:|---:|---:|
| Sword | 57 | 171 | — |
| Bow | 60 | **180** | — |
| Gun | 86 | 172 | — |
| **Shuriken** | 46 | 92 | **276** |

Lv1 DPS 는 **6.7 로 Sword 와 똑같이** 맞췄다 — 관통은 단일 대상에서 값이 0 이라
기준선을 남들과 같이 두고 **적이 뭉칠 때만** 이득이 나게 했다.
**폭은 Bow, 깊이는 Shuriken.** 이렇게 갈라야 둘이 같은 무기가 안 된다.

**B. 검 — `Range` 만 바꾸고 `Damage` 는 안 건드렸다**

```
Sword,Sword,Assets/Prefabs/Weapon_Melee.prefab,Assets/Game/Sprites/Weapons/Sword.png,Assets/Prefabs/Fx_SwingArc.prefab,,0,10|15|22|30|40,1.5|1.3|1.1|0.9|0.7,1|1.1|1.2|1.3|1.5,1|1|2|2|3,2|2|2.2|2.2|2.5
```

바뀐 열은 요청서가 지정한 **4개뿐**이다: `WeaponPrefab` · `ProjectilePrefab` ·
`ProjectileSpeed`(8→0) · `Range`(10→2).

🔴 **`Damage`·`Cooldown`·`Size`·`Count` 는 한 자리도 안 올렸다. 일부러다.**
요청서 §4-B 가 *"감으로 확정해서 박아 넣지 말 것"* 이라고 했고, 그보다 더 큰 이유가 있다 —
**지금 검은 대조 실험이다.** 사거리 하나만 움직였으므로, 약해졌다면 그건 **순수하게
근접이 된 대가**다. 피해를 같이 올렸으면 그 값을 영원히 못 잰다 (I-55 와 같은 함정).

⚠️ 그래서 **플레이하면 검이 약할 가능성이 높다. 그게 버그가 아니라 측정값이다.**
얼마나 약한지를 알려 주면 `TUNING.md` §3 에 반영한다.

**C. `Sword` 의 설명문도 갈았다** — `Swings a blade at the nearest enemy.` →
`Cleaves a wide arc right in front of you. Short reach.`
사거리가 5분의 1이 됐는데 카드에 그 말이 없으면 **플레이어가 모르고 집는다.**

---

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | `[BalanceImporter] Import 완료` · **에러 0** |
| ② | `Assets/Game/WeaponData/Shuriken.asset` · `Assets/Game/ItemData/Shuriken.asset` **생성됨** |
| ③ | `Shuriken.asset` 의 `ProjectilePrefab` guid 가 **`0` 이 아니다** (`Proj_Shuriken` 을 가리킨다) |
| ④ | `Sword.asset` 의 `WeaponPrefab` = `Weapon_Melee` · `ProjectilePrefab` = `Fx_SwingArc` · `Range[0]` = **2** |
| ⑤ | 🔴 **D10 판정 ⑤ — 레벨업 3택 / 상점에 `Shuriken` 이 뜬다.** 이게 되면 §6 2단계가 닫힌다 |
| ⑥ | **실플레이 — 검**: 총알이 안 나가고 **호가 그려진다** |
| ⑦ | **실플레이 — 수리검**: 뭉친 적을 향해 쏠 때 **여러 마리가 같이 닳는다** |
| ⑧ | `Weapons.csv` 열 **12개** · `Items.csv` 열 **8개** 그대로 (열 추가 없음 — 내가 이미 확인했다) |

**같이 남겨 줄 것 — 이번엔 "느낌"이 산출물이다.**

1. **검이 얼마나 약한가** (위 B). "못 쓰겠다"인지 "좀 아쉽다"인지만 알려 주면 된다
2. **수리검이 떼거리에서 과한가** — 판정은 한 마리 잡을 때가 아니라 **몰려올 때** 본다
3. ⚠️ **검이 총소리를 낸다** (`SfxId.WeaponFire` 재사용, D10). 베는 맛의 절반은 소리라
   1번을 판정할 때 이걸 감안할 것. 새 SFX 가 필요하면 그때 요청한다

**DEV 가 CSV 를 직접 고치지는 말 것.** 숫자는 CONTENT 가 `TUNING.md` 근거와 같이 바꾼다.

---

## 요청-10 — 독 장판 CSV + 바닥 폭탄 켜기 (C15) · **요청-5(D11) 의 나머지 절반**

> ✅ **닫힘 (D12, 2026-08-30).** 요청-9 와 **같은 Import 1회**로 반영. `WeaponData/Toxin` ·
> `ItemData/Toxin` 생성, `Bomb.asset` 의 `TravelPrefab` = `Proj_BombGround`. **판정 ①~⑧ 전부 PASS.**
> ⑥ 폭탄 — 게임 카메라 캡처 2장으로 확인(비행 중 / 착탄 후 **벌겋게 달아오른** 상태).
> ⑦ 장판 — 캡처 1장에 초록 웅덩이 **2개 동시**, 안쪽 적에 피해 9/11/9/6 누적.
> (Lv5 쿨 1.8 < 지속 4.0 이라 겹치는 게 정상 — 예고한 그대로다)
> 🔴 **Q4 답: 적을 한 번도 안 만나는 장판이 실제로 있었다** — 캡처의 두 웅덩이 중 하나는 안이 비어 있었다.
> "느낌" 질문 4개 답 + 전체 상세 → [`DONE/D12.md`](../DONE/D12.md)

> 🔴 **요청-9 와 같은 Import 한 번으로 둘 다 끝난다.** 따로 돌리지 말 것.
> 이게 되면 **D11 판정 ⑨ 가 닫히고 §6 3·4단계가 실제로 굴러간다.**
>
> ⚠️ **D12(요청-9)를 이미 돌리고 있다면 — 이 요청도 같이 들어갔다.**
> 요청-9 와 **같은 3개 파일**을 고쳤고, 이 글을 쓰는 시점에 이미 저장돼 있다.
> 따로 Import 를 또 돌릴 필요는 없고, **아래 판정 ②~⑦ 만 추가로 확인**하면 된다.
> (`Toxin.asset` 이 생겼는지 · `Bomb.asset` 의 속도가 9 인지)

**무엇을** — `Game/Balance/Import CSV -> ScriptableObjects` **1회** (요청-9 와 공유).
CSV 3파일은 저장해 뒀다. **새 파일도, 새 열도 없다.**

| 파일 | 무엇을 했나 |
|---|---|
| `Weapons.csv` | `Toxin` 줄 **추가** · `Bomb` 줄의 **`TravelPrefab`+`ProjectileSpeed` 2열만** 교체 |
| `Items.csv` | `Toxin` 줄 **추가** |
| `SceneWiring.csv` | `LevelUpManager,allItems` 끝에 `Toxin.asset` **1개 추가** |

⚠️ **플레이 모드에서는 Import 가 실패한다** (`MarkSceneDirty`). `ManageEditor(Stop)` 먼저.

---

### 내가 정한 값과 그 근거 — **동의 안 되면 고치지 말고 알려 줄 것**

**A. 독 장판 (신규)**

```
Toxin,Toxin,Assets/Prefabs/Weapon_Field.prefab,Assets/Game/Sprites/Weapons/Toxin.png,Assets/Prefabs/Proj_ToxinField.prefab,,0,3|4|6|8|11,3|2.7|2.4|2.1|1.8,1|1.1|1.2|1.35|1.5,1|1|1|1|1,4|5|5|6|6
```

🔴 **`Damage` 를 요청서 초안(`4|6|8|11|15`)보다 내렸다.** 요청서 스스로가 짚은
*"`Damage` 는 틱당이다"* 를 초안 숫자가 반영하지 않고 있었다. 8을 곱해 보면:

| | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 |
|---|---:|---:|---:|---:|---:|
| 초안 `4\|6\|8\|11\|15` → 8틱 | 32 | 48 | 64 | 88 | **120** |
| **내가 넣은 값** → 8틱 | **24** | 32 | 48 | 64 | **88** |
| Bomb 의 한 방 | 30 | 44 | 62 | 86 | 118 |

초안대로면 **완전 흡수 총량이 Bomb 의 한 방과 같아진다.** 거기에 **슬로우까지** 붙으면
Bomb 을 집을 이유가 사라진다. **75% 로 낮추고 나머지 25% 를 감속 값으로 지불**했다.

**B. `Range` 를 `5|5|6|6|7` → `4|5|5|6|6` 으로 좁혔다**

Lv1 은 **쿨 3초 > 지속 4초** 라 장판이 한 번에 **하나뿐**이다. 하나뿐인 장판이
5유닛 밖에 떨어지면 그 판은 무기가 **아무 일도 안 한다.** 좁게 시작해서,
장판이 겹치기 시작하는 Lv4~5 에서만 넓힌다.

**C. `ProjectileSize` 증가폭도 완만하게** (`1→1.5`, 초안 `1→1.7`)

Lv5 는 **쿨 1.8 < 지속 4.0** 이라 **2~3장이 동시에 산다.** 크기와 개수가 같이 붙으면
화면을 덮는다. 성장은 **개수(지대)** 로 보여 주고 크기는 덜 움직인다.

**D. 폭탄 — 2열만 갈고 `Damage` 는 안 건드렸다**

```
Bomb,...,Assets/Prefabs/Proj_Aoe(Boom).prefab,Assets/Prefabs/Proj_BombGround.prefab,9,30|44|62|86|118,...
```

요청서가 지정한 `TravelPrefab` + `ProjectileSpeed`(0→**9**) **2열뿐**이다.
🔴 **피해는 한 자리도 안 올렸다** — 검(C12)과 같은 대조 실험이다. 비행 + 신관 1.1초만큼
늦어졌으니 올려야 하는 건 맞지만, 같이 올리면 **늦어진 대가를 영원히 못 잰다** (I-55).
⚠️ **그래서 폭탄이 약하게 느껴질 가능성이 높다. 그건 버그가 아니라 측정값이다.**

**E. 설명문** — `Spills a toxic pool nearby. Enemies inside are slowed and keep taking damage.`
이 게임 **최초의 감속 수단**이라 카드에 "slowed" 가 반드시 보여야 한다.
`ShopPrice` 는 **10** (Fireball 10 · Bomb 11 · Shuriken 9 사이).

---

### 어떻게 확인하나 (판정 기준)

| # | 기준 |
|---|---|
| ① | `[BalanceImporter] Import 완료` · **에러 0** |
| ② | `Assets/Game/WeaponData/Toxin.asset` · `Assets/Game/ItemData/Toxin.asset` **생성됨** |
| ③ | `Toxin.asset` 의 `WeaponPrefab` = `Weapon_Field` · `ProjectilePrefab` = `Proj_ToxinField` — **guid 가 `0` 이 아니다** |
| ④ | `Bomb.asset` 의 `TravelPrefab` = `Proj_BombGround` · `ProjectileSpeed` = **9** |
| ⑤ | 🔴 **D11 판정 ⑨ — 레벨업 3택 / 상점에 `Toxin` 이 뜬다.** 이게 되면 §6 4단계가 닫힌다 |
| ⑥ | **실플레이 — 폭탄**: 목표까지 **날아가 바닥에 떨어지고**, 벌겋게 달아오른 뒤 터진다 |
| ⑦ | **실플레이 — 장판**: 초록 웅덩이가 플레이어 **주변 아무 데나** 깔리고, 안에 든 적이 **눈에 띄게 느려진다** |
| ⑧ | `Weapons.csv` 열 **12개** · `Items.csv` 열 **8개** 그대로 (열 추가 없음 — 내가 이미 확인했다) |

**같이 남겨 줄 것 — 이번에도 "느낌"이 산출물이다.**

1. **폭탄이 얼마나 답답한가** — `fuseTime 1.1` + 비행이 **피할 시간을 주는 재미**인지
   **그냥 느린 무기**인지. 이 하나가 3단계의 성패다
2. **감속이 느껴지는가** — `slowMult 0.6` 이 눈에 보이나. 안 보이면 장판은 그냥 약한 도트다
3. **Lv5 에서 장판이 화면을 덮는가** — 덮으면 `ProjectileSize` 를 먼저 내린다
4. 🔴 **장판이 적을 한 번도 안 만나는 판이 있는가** — 있으면 `Range` 를 더 좁힌다.
   조준하지 않는 무기라 **"빗나갔다"가 구조적으로 가능**하다

**DEV 가 CSV 를 직접 고치지는 말 것.** 숫자는 CONTENT 가 `TUNING.md` 근거와 같이 바꾼다.
🔴 `spriteRadiusAtScaleOne 0.97` · Custom pivot 은 **실측값이라 손대지 않았고, 앞으로도 안 댄다.**

---

## 요청-11 — 소환수 2종 (드래곤·문어) (C16) · §6 **5단계**

> ✅ **닫힘 (D13, 2026-08-30).** 판정 ①~⑫ **전부 PASS** · 콘솔 0건 → [`DONE/D13.md`](../DONE/D13.md).
> 프리팹 5개가 생겼다 — **CSV 를 쓸 수 있다.** 경로와 체감 미판정 항목은
> [`REQ/CONTENT.md` 요청-7](CONTENT.md) 로 넘겼다.
> ℹ️ 11-B 의 경로 `Assets/Scripts/Weapons/` · `Assets/Scripts/Visual/` 는 **없는 폴더**라
> 둘 다 `Assets/Scripts/Weapon/` 에 넣었다.

> **그림은 5장 다 나왔다. 남은 건 코드·프리팹뿐이다.**
> 설계 전문은 [`DESIGN_CLASSES.md`](../../DESIGN_CLASSES.md) §4 W6·W7 · §5-A ("드래곤의 계약 4줄" · "문어의 계약 4줄").
> ⚠️ **CSV 는 이번에 넣지 않는다.** 프리팹 경로가 아직 없어서다. 프리팹이 생기면 **CONTENT 가 채운다.**
> 이번 요청은 **임포트 + 스크립트 + 프리팹**까지다.

### 11-A. 애셋 임포트 5장

| `_Incoming/` 원본 | 옮길 곳 | 임포트 설정 |
|---|---|---|
| `Summons/Dragon_Fly.png` | `Assets/Game/Sprites/Summons/Dragon_Fly.png` | 🔴 **PPU 512** · Point(no filter) · Uncompressed · **Multiple** · Grid By Cell Size **256×256** → `_0`~`_15` |
| `Summons/Octopus_Idle.png` | `Assets/Game/Sprites/Summons/Octopus_Idle.png` | 🔴 **PPU 512** · 위와 동일 · `_0`~`_15` |
| `Effects/TentacleLash.png` | `Assets/Game/Sprites/Effects/TentacleLash.png` | **PPU 100** · Point · Uncompressed · Multiple · Grid **256×256** → 6칸 `_0`~`_5` · **pivot Center** |
| `ICON/Dragon.png` | `Assets/Game/Sprites/Weapons/Dragon.png` | **PPU 512** · Point · Uncompressed · **Single** |
| `ICON/Octopus.png` | `Assets/Game/Sprites/Weapons/Octopus.png` | 위와 동일 |

- 🔴 **PPU 512 를 빼먹으면 I-58 / B2 가 그대로 재현된다.** 4×4 시트는 Unity 가 1024 로 짐작해서
  **소환수가 절반 크기로 나온다.** 걷기 시트 4종에서 이미 한 번 당했다.
- `Assets/Game/Sprites/Summons/` 는 **새 폴더**다.
- `TentacleLash` 는 **pivot 을 Center 그대로 둔다.** 독 장판(`ToxinField`)과 달리
  내가 캔버스 정중앙 기준으로 그려서 **실측 보정이 필요 없다** (아래 11-D 참고).
- 옮긴 뒤 `_Incoming/` 원본은 지운다(`git mv` 로 옮기면 한 번에 끝난다).

### 11-B. 새 스크립트 2개

**① `Assets/Scripts/Weapons/SummonWeapon.cs` — `WeaponBase` 상속**

```
OnInitialized()  →  bodyPrefab 을 Pool 에서 꺼내 씬에 낳는다 (🔴 부모를 붙이지 않는다)
Update/Fire()    →  판정 기준점이 플레이어가 아니라 "소환수 몸통의 위치"
OnDisable()      →  몸통을 Pool 에 돌려준다
```

- 🔑 **`OnDisable()` 이면 기존 파일을 한 줄도 안 고쳐도 된다.**
  `ObjectPool.Return` 이 `obj.SetActive(false)` 를 부르므로, `WeaponManager.RemoveWeapon` →
  `weaponPool.Return(w.gameObject)` 가 곧 `OnDisable` 이다.
  `WeaponBase` 에 `OnRemoved()` 같은 훅을 새로 팔 필요가 없다.
- **이게 없으면 상점에서 무기를 환불했을 때 소환수만 필드에 영원히 남는다.**
- 🔴 **`FindNearestEnemy()` 를 반드시 `override` 한다.** `WeaponBase` 의 기본 구현은
  `transform.position`(= 플레이어) 에서 찾는다. 소환수는 **몸통 위치**에서 찾아야 한다.

**몸통이 지켜야 할 것**

| 규칙 | 이유 |
|---|---|
| HP · Collider · `EnemyBase` **없음** | 적이 아니다. 맞지도 막지도 않는다 |
| 🔴 `Rigidbody2D` **없음** | 넉백에 휩쓸려 날아간다 |
| `Lerp` 로 `player.position + offset` 을 따라간다 | "따라다닌다"는 느낌이 여기서 나온다 |
| **소환수별 offset 각도**를 프리팹 필드로 | 소환사는 무기 슬롯이 5칸이라 **겹침이 반드시 생긴다** |
| 이동 방향에 따라 `flipX` | 이 프로젝트의 스프라이트는 **오른쪽을 본다** |

**② `Assets/Scripts/Visual/SummonVisual.cs` — 16프레임 루프 재생기**

- ❌ **`EnemyVisual` 을 재사용하지 말 것.** `Rigidbody2D` + `EnemyBase` 에 묶여 있다.
- ❌ **위아래 흔들림(bob)을 코드로 넣지 말 것.** 드래곤 ±1.6px · 문어 ±1.5px 로
  **그림에 이미 그려져 있다.** 코드가 또 흔들면 두 번 흔들린다.
- ping-pong 불필요 — 16프레임이 `sin(2π·i/16)` 이라 **이음매 없이 순환**한다.

### 11-C. 프리팹 4개 (+1)

| 프리팹 | 무엇 |
|---|---|
| `Assets/Prefabs/Summon_Dragon.prefab` | 몸통. `SpriteRenderer` + `SummonVisual` (Collider·RB 없음) |
| `Assets/Prefabs/Summon_Octopus.prefab` | 몸통. 동일 |
| `Assets/Prefabs/Weapon_SummonDragon.prefab` | **안 보이는** 로직 오브젝트. `SummonWeapon` + `bodyPrefab` |
| `Assets/Prefabs/Weapon_SummonOctopus.prefab` | 동일 |
| `Assets/Prefabs/Fx_TentacleLash.prefab` | 촉수 이펙트. `SwingArcFx` 재사용 (아래) |

- 🔴 **몸통을 무기 오브젝트의 자식으로 두지 말 것.** `WeaponManager` 가 무기를 플레이어 밑에
  `SetParent` 하므로, 자식이면 소환수가 **플레이어에 뻣뻣하게 붙어 다닌다.** 따라오는 느낌이 죽는다.
- `sortingOrder` 는 플레이어·적과 같은 대역. 장판의 `-5` 가 아니다 — **소환수는 바닥이 아니다.**

### 11-D. 재사용 — 새로 만들 게 생각보다 적다

| 필요한 것 | 이미 있는 것 |
|---|---|
| 촉수 이펙트 | **`SwingArcFx` 를 그대로.** `spriteRadiusAtScaleOne = 0.9951` 만 넣으면 된다 |
| 드래곤 화염구 비행 | `Proj_Fireball` |
| 드래곤 착탄 폭발 | `Proj_Aoe(Boom)` |
| 발사 → 비행 → 폭발 흐름 | `AoeWeapon.LaunchTravel` 의 모양 |
| 문어 360° 링 판정 | `MeleeWeapon.Strike` 에서 `Vector2.Angle(dir, to) > halfAngle` **한 줄만 빼면** 링이 된다 |

- 🔑 **`spriteRadiusAtScaleOne = 0.9951`** — `SwingArcFx` 의 `1.074` 는 그림이 판정보다 커서
  나눗셈을 강요당한 값이었다. 촉수는 내가 **처음부터 규격에 맞춰 그렸다.**
  그래서 `localScale = Range` 가 그대로 맞는다.
- 🔴 **촉수는 코드로 z 회전시키지 말 것.** 6프레임에 **9°/frame 회전이 이미 구워져 있다.**
- 피해는 **frame 3 (최대 도달 프레임) 에 한 번만.** 휘두름 호와 같은 이유다.

### 11-E. CSV 열 배치 (참고용 — 이번에 넣지 않는다)

프리팹이 생기면 CONTENT 가 채운다. **DEV 는 이 표를 "열이 모자라지 않는다"는 확인용으로만 본다.**

| 열 | 드래곤 | 문어 |
|---|---|---|
| `ProjectilePrefab` | `Proj_Aoe(Boom)` (착탄 폭발) | `Fx_TentacleLash` (촉수) |
| `TravelPrefab` | `Proj_Fireball` (비행) | 비움 |
| `ProjectileSpeed` | > 0 | `0` |
| `Range` | 화염구 **탐색** 반경 | 촉수 **링** 반경 |

- 🔴 `bodyPrefab` · offset 각도 · 폭발 반경은 **CSV 가 아니라 프리팹 필드**다.
  `Weapons.csv` 는 **12열 그대로** 유지된다. 열 추가 없음.
- 방향만 미리 말해 두면 — 드래곤은 파이어볼(8.2 DPS) 근처, 문어는 검(6.7) 근처지만 **360°** 다.

### 판정 기준

| # | 확인할 것 |
|---|---|
| ① | 5장 임포트 완료 · 시트 2장이 **16칸**, 촉수가 **6칸**으로 갈라짐 |
| ② | 🔴 시트 2장 PPU **512** · 촉수 **100** · 아이콘 **512** |
| ③ | 🔴 **크기**: 소환수가 오우거의 **약 65%**. 절반이면 PPU 를 놓친 것이다 (I-58/B2) |
| ④ | 소환수가 플레이어를 **따라오고**, 멈추면 **곁에 선다** |
| ⑤ | 🔴 적이 소환수를 **통과한다** (막히지 않는다) |
| ⑥ | 🔴 넉백에 소환수가 **날아가지 않는다** |
| ⑦ | 드래곤이 **자기 위치에서** 화염구를 쏜다 (플레이어 위치가 아니다) |
| ⑧ | 문어가 **자기 뒤쪽 적도** 때린다 (360°) |
| ⑨ | 촉수 이펙트의 **바깥 끝 = 실제 판정 반경** (그림만 크지 않다) |
| ⑩ | 소환수 2마리를 같이 들었을 때 **서로 겹쳐 있지 않다** |
| ⑪ | 🔴 상점에서 **환불하면 몸통도 사라진다** |
| ⑫ | 같은 무기를 다시 고르면 **레벨업**이지 두 마리가 되지 않는다 · 콘솔 에러 **0** |

**같이 남겨 줄 것**

1. 🔴 **프리팹 경로 4개** — 이게 있어야 내가 CSV 를 쓴다. **제일 급하다**
2. **따라오는 느낌** — `Lerp` 계수가 굼뜬가 / 찰싹 붙는가
3. `SummonVisual` 의 `frameRate` 실제 값 (숨쉬기가 빠른가 느린가)
4. **크기 판정** — 크거나 작으면 `Tools/Art/gen_*.py` 의 `TARGET_H` 를 고쳐 **다시 그린다.**
   🔴 `localScale` 로 늘리지 말 것. 픽셀이 뭉개진다
5. **소환수 SFX 가 없다** — 화염구 발사음 · 촉수 휘두름음 둘 다 없다. 나중에 채운다

---

## 요청-12 — 무기 전용 SFX 2종 배선 (C17·C18) · **요청-6 이 부탁한 것 + 하나 더**

> [`REQ/CONTENT.md`](CONTENT.md) **요청-6 ②** 의 답이다. 클립 2개를 만들어 `_Incoming/Audio/` 에 뒀다.
> 요청-6 은 검만 부탁했지만 **장판도 같은 병**이라 같이 만들었다 — 배선 구조가 완전히 같아서
> 따로 하면 **같은 작업을 두 번** 하게 된다.
> **작업량이 작다** — D13(소환수) 중간에 끼워 넣어도 되고, 소환수 프리팹 만들 때 같이 해도 된다.

### 무엇을 — 2세트, 구조가 똑같다

| # | 파일 | 할 일 |
|---|---|---|
| ① | `_Incoming/Audio/SFX_WeaponSwing.wav` → `Assets/Game/Audio/` | 옮기고 임포트 (44100Hz · 16bit · 스테레오 · **0.28초**) |
| ② | `_Incoming/Audio/SFX_ToxinSpill.wav` → `Assets/Game/Audio/` | 〃 (**0.70초**). 둘 다 기존 `SFX_*.wav` 와 같은 규격이다 |
| ③ | `Assets/Scripts/Audio/AudioId.cs` | `enum SfxId` 의 **무기** 블록에 `WeaponSwing = 4` · `ToxinSpill = 5` 추가. 🔴 **4·5 가 비어 있는 걸 확인했다** (1·2·3 다음이 10 이다) |
| ④ | `Assets/Game/Audio/AudioLibrary.asset` | 항목 2개 — `WeaponSwing` **0.30 / 0.12** · `ToxinSpill` **0.38 / 0.10** (`Volume` / `PitchJitter`) |
| ⑤ | `Assets/Scripts/Weapon/MeleeWeapon.cs:52` | `SfxId.WeaponFire` → `SfxId.WeaponSwing` **한 줄** |
| ⑥ | `Assets/Scripts/Weapon/FieldWeapon.cs:32` | `SfxId.WeaponCast` → `SfxId.ToxinSpill` **한 줄** |

- 옮긴 뒤 `_Incoming/Audio/` 원본은 지운다.
- 🔴 **⑤⑥ 외의 호출부는 건드리지 않는다.** 확인해 뒀다 —
  `ProjectileWeapon.cs:26` 의 `WeaponFire` 는 맞고(원거리는 그대로 총소리),
  `AoeWeapon.cs:28` 의 `WeaponCast` 도 맞다(폭탄 시전).
  ⚠️ **`SummonWeapon.cs` 도 이번엔 그대로 둔다** — D13 이 지금 그 파일을 쓰고 있다 (아래 5번).

### 왜

둘 다 **남의 소리를 빌려 쓰고 있다.**

| 무기 | 지금 나는 소리 | 실제로 하는 일 |
|---|---|---|
| 검 (D10 에서 근접으로 재해석) | `WeaponFire` = **총소리** | 칼을 휘두른다 |
| 독 장판 (D11 신설) | `WeaponCast` = **마법 시전음** | 독을 바닥에 쏟는다 |

🔴 **`TUNING.md` §3 의 근접 7값과 장판 값들은 이게 배선된 뒤에 판정하는 게 맞다.**
안 그러면 **소리 탓을 수치 탓으로 오해한다** — "안 시원하다"의 원인이 `halfAngle` 이 아니라
총소리일 수 있는데, 그 상태로 숫자를 흔들면 엉뚱한 값이 굳는다.

### 숫자의 근거 — **CONTENT 가 미리 재 놨다**

클립은 `Tools/Art/` 의 그림과 같은 방식으로 **절차 생성**했다
(`Tools/Audio/gen_sword_swish.py` · `gen_toxin_splash.py`). 파라미터를 고쳐 다시 구울 수 있다.

| 잰 것 | 검 `WeaponSwing` | 독 장판 `ToxinSpill` |
|---|---|---|
| **겹침** | **3연타 합성 피크 0.400 = 1타와 동일.** 간격 0.247s(`hitDelay 0.067`+`comboInterval 0.18`)가 클립 0.28s 보다 넓어 겹침이 **0.033s** 뿐이고 그 구간은 이미 페이드아웃 | **겹치지 않는다.** Lv5 쿨 **1.8s** > 클립 0.70s. 4장 연속 재생 피크도 0.420 그대로 |
| 0~200Hz | **0.0%** — 총소리 저역 "쿵"이 없다 | 1% |
| 200~800Hz | **1%** | **11%** ← 🔑 **두 소리를 가르는 대역**(11배) |
| 시작부 | 첫 10ms 피크 **0.006**(전체의 1/67) — 클릭 없음. **트랜지언트가 곧 총소리**라 일부러 없앴다 | 앞 50ms 가 **전체 피크** — 철퍽이 가장 큰 순간이다 |
| 피크 / RMS | 0.400 / 0.053 | 0.420 / 0.054 |
| 모노 합산 피크 | 0.344 | 0.383 — 좌우 상쇄 없음 |

**둘이 서로 안 헷갈리게 만든 게 설계 목표다.** 고역(3~9kHz)은 둘 다 노이즈 기반이라
비슷할 수밖에 없어서, **길이(0.28 ↔ 0.70)** · **스윕 방향(700→5200 올라감 ↔ 900→260 내려감)** ·
**200~800Hz(1% ↔ 11%)** 세 축으로 갈라 뒀다.

`Volume`/`PitchJitter` 는 **기존 사다리에 끼워 넣은 값**이다 (`AudioLibrary.asset` 실측 확인):

`BuildingFire` 0.22/0.14 · `EnemyHit` 0.28/0.12 · **`WeaponSwing` 0.30/0.12** ·
`WeaponFire` 0.35/0.10 · **`ToxinSpill` 0.38/0.10** · `WeaponCast` 0.45/0.08

- 검이 `WeaponFire` 아래인 건 Lv5 **3연타**라 실제로 3배 자주 울리기 때문이다
- 장판이 `WeaponCast`(빌려 쓰던 자리) 아래인 건 쿨 3.0→1.8초로 **한 판 내내 깔리기** 때문이다

### 판정 기준

| # | 확인할 것 |
|---|---|
| ① | 클립 **2개**가 `Assets/Game/Audio/` 에 있고 `AudioLibrary.asset` 에 물렸다 |
| ② | `WeaponSwing` **0.30 / 0.12** · `ToxinSpill` **0.38 / 0.10** |
| ③ | `SfxId.WeaponSwing = 4` · `ToxinSpill = 5` · 🔴 **기존 번호를 하나도 안 바꿨다** (바꾸면 `AudioLibrary` 배열이 통째로 어긋난다) |
| ④ | `MeleeWeapon.cs` 에 `WeaponFire` 가, `FieldWeapon.cs` 에 `WeaponCast` 가 **더 이상 없다** |
| ⑤ | 컴파일 에러 **0** |
| ⑥ | 🔴 **실플레이 — 검을 휘두르면 총소리가 아닌 "쉭"** |
| ⑦ | 🔴 **실플레이 — 장판이 깔릴 때 마법음이 아닌 "철퍽 + 치익"** |
| ⑧ | 🔴 **Lv5 Sword 3연타가 뭉개지거나 시끄럽지 않다.** Lv1 은 1연타라 판정이 안 된다 |
| ⑨ | 🔴 **둘을 같이 들었을 때 구분된다** (검사 + 독 조합으로 한 판) |
| ⑩ | 원거리 무기(파이어볼·화살)와 폭탄은 **소리가 그대로다** |

**같이 남겨 줄 것** — `TUNING.md` §2 **C-2** 의 체크박스에 답을 준다.

1. **각각 남의 소리로 안 들리는가** (검=총소리 / 장판=마법음). 1번 판정 기준이다
2. 🔴 **장판이 한 판 내내 거슬리지 않는가** — **30번쯤 들은 뒤에 판정할 것.**
   한 번 듣고 괜찮은 건 판정이 아니다. 거슬리면 `Volume` 을 0.30 으로 먼저 내린다
3. **`EnemyHit`(0.28) 에 묻히지 않는가** — 묻히면 검을 0.35 로
4. **길이가 맞는가** — 검이 "탁"으로 들리면 `DUR` 0.28 → 0.34,
   장판 꼬리가 늘어지면 0.70 → 0.55 로 **내가 다시 굽는다.**
   ⚠️ 검은 **0.247s 를 넘길 수 없고**(3연타가 겹친다), 장판은 **0.30s 아래로 못 내린다**(거품이 안 들어간다)
5. ⚠️ **소환수 2종(D13)도 전용 소리가 없다.** `SummonWeapon` 이 화염구에 `WeaponCast`,
   촉수에 `WeaponFire` 를 빌려 쓰는 걸 봤다. **이번엔 일부러 안 건드렸다** — D13 이 그 파일을
   쓰고 있어 충돌한다. **D13 이 끝나면 같은 방식으로 만든다** (요청-11 "같이 남겨 줄 것" 5번)

---

## 요청-13 — 소환수 CSV + 촉수 그림 교체 + SFX 볼륨 2값 (C19) · §6 **5단계 마감**

> [`REQ/CONTENT.md` 요청-7](CONTENT.md)(D13) 과 [요청-8](CONTENT.md)(D14) 에 대한 답이다.
> **한 번의 Import + 한 번의 재임포트로 전부 끝난다.**
> 🔑 **Import 는 마지막에 한 번만 한다** — ①②③ 을 다 고친 뒤에.

### ① CSV 3파일 — 값은 다 채웠다. **Import 만 해 주면 된다**

| 파일 | 무엇을 넣었나 |
|---|---|
| `Assets/Game/Balance/Weapons.csv` | `SummonDragon` · `SummonOctopus` **2줄** (Toxin 아래, 진화 무기 위) |
| `Assets/Game/Balance/Items.csv` | 같은 Id 2줄 (`Category=Weapon` · `MaxLevel=5` · 가격 12 / 11) |
| `Assets/Game/Balance/SceneWiring.csv` | `LevelUpManager,allItems` 끝에 `SummonDragon.asset\|SummonOctopus.asset` 추가 |

```
Game/Balance/Import CSV -> ScriptableObjects
```

⚠️ **플레이 모드(일시정지 포함)에서는 실패한다** — `MarkSceneDirty` 가 못 돈다. `Stop` 먼저.

**넣은 값**

| 열 | `SummonDragon` | `SummonOctopus` |
|---|---|---|
| `WeaponPrefab` | `Weapon_SummonDragon.prefab` | `Weapon_SummonOctopus.prefab` |
| `ProjectilePrefab` | `Proj_Aoe(Boom).prefab` | `Fx_TentacleLash.prefab` |
| `TravelPrefab` | `Proj_Fireball.prefab` | *(비움)* |
| `ProjectileSpeed` | `11` | `0` |
| `Damage` | `9\|13\|18\|25\|34` | `7\|10\|14\|19\|26` |
| `Cooldown` | `1.4\|1.25\|1.1\|0.95\|0.8` | `1.2\|1.1\|1\|0.9\|0.8` |
| `ProjectileSize` | `0.7\|0.75\|0.8\|0.9\|1` | `1\|1\|1\|1\|1` (**안 쓰이는 열**) |
| `Range` | `7\|7\|8\|8\|9` (탐색 반경) | `1.8\|2\|2.2\|2.4\|2.6` (링 반경) |

- **12열 그대로다.** 열 추가 없음. `ProjectileCount` 는 두 모드 다 안 써서 `1` 로 채웠다
- 🔴 **몸통 프리팹은 CSV 에 없다.** 무기 프리팹의 `bodyPrefab` 에 이미 물려 있다
- 드래곤 `Range` 를 Fireball(12~14)보다 좁게 잡은 이유는 **화면 세로 절반이 6** 이라
  12 를 주면 화면 끝 적을 저격하는 **포대**가 되기 때문이다. "곁에 있는 동료"가 아니게 된다
- 🔴 문어 `Range` 는 **네가 검증한 2.5 를 그대로 두고 Lv5 만 2.6** 이다.
  줄이자는 제안(요청-7 ⑥-3)을 **안 받았다** — 이유는 아래 ②

### ② 🔴 촉수 그림 교체 — **반경이 아니라 그림이 문제였다**

`_Incoming/Effects/TentacleLash.png` → `Assets/Game/Sprites/Effects/TentacleLash.png` **덮어쓰기**.

⚠️ **임포트 설정은 지금 것 그대로 쓰면 된다** (PPU 100 · Multiple 6×1 · Center pivot · Point · Uncompressed).
크기·칸 수·바깥 반지름이 **하나도 안 바뀌었다.** 덮어쓰고 재임포트만 하면 된다.

**왜 반경을 안 줄였나** — 네 지적("반경 2.5 는 문어 그림의 9배")은 맞지만 **결론이 반대다.**

| 재 본 것 | 값 |
|---|---|
| 잉크 밀도 (원반 대비) | 촉수 **13.7%** vs 검 호 **13.6%** — 🔑 **거의 같다.** "빽빽해서"가 아니다 |
| 🔴 가운데 빛 원반 (배율 2.512) | **0.384 유닛** |
| 문어 몸통 **전체** | **0.281 유닛** |
| → 비율 | 🔴 **장식 하나가 문어보다 2.7배 크다** |

**촉수가 어디서 나오는지 알려 주려던 빛이, 정작 그 자리에 선 문어를 덮고 있었다.**
반경이 아니라 **가운데**가 범인이다.

그리고 반경 2.5 는 줄이면 안 된다 — **D12 가 반대를 증명했다:**
최근접 적이 **4.67유닛**에 있고 검(사거리 2.5)은 **6초간 0회** 휘둘렀다.
**이 게임에서 2.5 유닛은 크지 않다.** 네가 실측한 후리기 14회 / 44피격도 그 반경에서 나온 값이다.

**고친 것 3가지** (전부 `Tools/Art/gen_octopus.py` 안에서 끝난다)

| | 전 | 후 |
|---|---|---|
| 가운데 빛 원반 | 반지름 10~17.8px **채운 원** | **삭제.** 얇은 테두리 링만 남김 |
| 촉수 시작 반지름 | 14px | **20px** (`R_IN`) — 문어가 보일 구멍 |
| 뿌리 굵기 (`BASE_W`) | `7.0 / 6.4 / 6.0 / 5.6 / 4.6 / 3.2` | **절반** `4.2 / 3.8 / 3.5 / 3.3 / 2.7 / 1.9` |

**실측 (스크립트가 직접 출력한다)**

```
lash f0 바깥  44.95px  안쪽구멍 13.21px  잉크 12.7%
lash f3 바깥  99.51px  안쪽구멍 16.14px  잉크  7.3%   <- 최대 신장 프레임
=> 최대 반지름 99.51 px = 0.9951 유닛 (PPU 100)
```

🔑 **`0.9951` 이 그대로다.** 바깥 반지름 계약을 지켰으므로 **너는 아무것도 안 고쳐도 된다** —
`Fx_TentacleLash.prefab` 의 `spriteRadiusAtScaleOne 0.9951` · `frameRate 30` ·
`Weapon_SummonOctopus.prefab` 의 `lashHitDelay 0.1` **전부 유지**.

> ℹ️ **`Octopus_Idle.png` 와 `Octopus.png`(아이콘)은 다시 안 보냈다.**
> 스크립트가 셋을 같이 굽지만, 재생성본이 네가 이미 임포트한 것과 **바이트 단위로 같은 걸 확인**하고 지웠다.
> `_Incoming/` 에는 `Effects/TentacleLash.png` **한 장만** 있다.

### ③ SFX 볼륨 2값 — `AudioLibrary.asset` 만 고친다

D14 의 계산에 답한다. **클립은 안 바꾼다. 재임포트도 필요 없다.**

| `SfxId` | Volume | | 왜 |
|---|---|---|---|
| `WeaponSwing`(4) | 0.30 → **0.35** | 🔺 올림 | 네 근거를 받았다 — 휘두름 1회에 **부채꼴 안의 적이 전부** 타격음을 낸다. 중복 컷은 *같은 클립* 0.04초 창이라 **다른 적의 타격은 안 막는다.** 벤 소리는 한 번의 타격음이 아니라 **그 합**보다 커야 한다 |
| `ToxinSpill`(5) | 0.38 → **0.32** | 🔻 내림 | 아래 |

**장판을 내리는 근거 — "얼마나 자주"가 아니라 "전체 시간의 몇 %냐"**

| | 클립 | 최소 쿨 | **점유율** |
|---|---:|---:|---:|
| 독 장판 | 0.701s | 1.8s (Lv5) | 🔴 **39%** |
| 검 (3연타 묶음) | 0.28s×3 | 0.7s | 8% |
| 총·활 | ~0.15s | 0.22s | 5% 남짓 |

**한 판의 39% 동안 계속 울리는 소리는 이것뿐이다.** `자주 울릴수록 작게` 규칙을
점유율로 읽으면 사다리 아래여야 맞다. 0.32 → 실제 출력 **0.099** 로
`EnemyHit`(0.087) 위 · `WeaponFire`(0.109) 아래에 앉는다.

> 🟡 **둘 다 예측이지 판정이 아니다.** 30회 청취는 여전히 열려 있다
> ([`TUNING.md` §2-C-2](../../TUNING.md)). 네 D14 판정 ②가 못 한 것도 이거다 —
> 그 판은 짧아 장판이 **1회**만 울렸다. 30회 = **90초 연속 플레이**다.

### 판정 기준

| # | 확인할 것 |
|---|---|
| ① | `Assets/Game/WeaponData/{SummonDragon,SummonOctopus}.asset` 이 생겼고 `m_Script` 가 **0 이 아니다** |
| ② | `Assets/Game/ItemData/` 에도 2개가 생겼고 `LevelUpManager.allItems` 가 **24개**다 (22 → 24) |
| ③ | 🔴 **레벨업 3택 / 상점에 Dragon · Octopus 가 실제로 뜬다.** 아이콘이 각자 것이다 |
| ④ | 드래곤을 먹으면 **화염구가 몸통에서 나가** 적 위치에서 터진다 (D13 판정 ⑦ 재확인) |
| ⑤ | 문어를 먹으면 **촉수가 돌고 링 안 적이 맞는다.** Lv1 반경 **1.8** 로 시작한다 |
| ⑥ | 🔴 **후리는 동안 문어 몸통이 보인다** — ②의 핵심 판정이다. 가운데가 비었나 |
| ⑦ | 촉수 바깥 끝이 여전히 사거리와 일치한다 (`scale × 0.9951 = Range`) — 그림을 갈았어도 안 틀어졌나 |
| ⑧ | `AudioLibrary` 의 4번=0.35 · 5번=0.32 |
| ⑨ | 컴파일 에러 **0** · 콘솔 **0건** |

### 같이 남겨 줄 것

1. 🔴 **드래곤 `Range 7~9` 가 포대처럼 느껴지나** — 화면 세로 절반이 6 이다.
   너무 멀리서 쏘면 5~6 으로 내린다. **CSV 한 값이다**
2. **소환수 2마리를 동시에 들었을 때 몇 마리인지 세어지나** — `offsetAngle 90/210`,
   간격 2.08. 5마리면 1.41 로 좁아진다 (`TUNING.md` 에 항목을 만들어 뒀다)
3. **후릴 때 플레이어 캐릭터가 가려지는 순간이 있나** — `Range 1.8` 부터 이미
   `offsetDistance 1.2` 를 넘어 **플레이어가 링 안에 선다**
4. **소환수만 들고도 판이 굴러가나 / 반대로 플레이어가 할 일이 없어지나** —
   피해·쿨은 형제 무기 비교로 잡은 **자리표시**다. 실플레이 근거가 나오면 그게 첫 근거다
5. ⚠️ **소환수 SFX 2종은 아직이다.** 요청-8 ③ 에 네가 적어 준 조건
   (점유율 · `lashHitDelay 0.1` 에 피크 맞추기 · `Explosion` 과 저역 안 겹치기 ·
   촉수는 검과도 갈리기)을 그대로 받아 **다음에 만든다.** `SfxId` **6·7** 을 비워 둘 것

---

## 요청에 반드시 적을 것

- **무엇을** — 파일 경로 · 오브젝트 · 필드 이름까지
- **왜** — 어떤 작업의 일부인지 (이슈 번호)
- **어떻게 확인하나** — 성공/실패 판정 기준.
  이게 없으면 DEV 가 "됐다"고 말할 근거가 없다

### 자주 나오는 요청 유형

| 유형 | 판정 기준 예 |
|---|---|
| CSV Import | 대상 SO 의 해당 필드가 CSV 값과 일치 |
| 새 애셋 임포트 + 배선 | `_Incoming/` 의 png 가 지정 필드에 물림 |
| 새 CSV **열** 추가 | 임포터 + SO 필드 + 사용처 **셋 다** 필요하다 (값 채우기는 CONTENT) |
| 씬 오브젝트 추가/이동 | 컴포넌트가 붙고 참조가 채워짐 |
| 실플레이 검증 | `TODO.md` §1 또는 `TUNING.md` 항목 문구를 **그대로 인용**할 것 |

> ⚠️ 배선을 요청할 때는 **어느 SO 의 어느 필드**인지까지 적는다.
> "Sentinel 그림 붙여 주세요" 로는 DEV 가 `portrait` 인지 `walkFrames` 인지 알 수 없다.
