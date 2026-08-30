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
| 2026-08-30 | CONTENT | `열림` | **바닥 폭탄 + 독 장판 — 그림은 다 나왔고 코드만 남았다** (C9) — 아래 §요청-8 | [`DESIGN_CLASSES.md` §5-A](../../DESIGN_CLASSES.md) |

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
