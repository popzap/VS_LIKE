# CONTENT 세션에 들어온 요청

> 그림 · 아이콘 · 타일 · 머티리얼 · 셰이더 · 오디오 · 폰트 · 밸런스 수치.
>
> **쓰기 직전에 이 파일을 다시 읽는다.** 맨 아래에 한 줄 추가한다.

---

| 요청일시 | 요청자 | 상태 | 요청 내용 | 참조 |
|---|---|---|---|---|
| 2026-08-30 | DEV | `닫힘(C1)` | **Goblin·Slime 걷기 시트 재생성** (16칸 중 12칸이 깨졌다) — 아래 §요청-1 | [`BUGS.md` B2](../BUGS.md) |
| 2026-08-30 | DEV | `닫힘(C10)` | **신규 SFX 6종의 볼륨·피치를 `TUNING.md` 에 등재** (D8 산출) — 아래 §요청-2 | [`DONE/D8.md`](../DONE/D8.md) |
| 2026-08-30 | DEV | `닫힘(C11)` | **화살 크기(PPU 512) 체감 항목을 `TUNING.md` §3 에 실측값으로 확정** (D9 산출) — 아래 §요청-3 | [`DONE/D9.md`](../DONE/D9.md) |
| 2026-08-30 | DEV | `닫힘(C12)` | 🔴 **수리검 CSV 3줄 추가 + 검을 근접으로 재해석** — 프리팹은 D10 이 다 만들어 놨다. **CSV 가 들어와야 게임에 나온다** — 아래 §요청-4 | [`DONE/D10.md`](../DONE/D10.md) |
| 2026-08-30 | DEV | `닫힘(C15)` | 🔴 **독 장판 CSV 4곳 + 바닥 폭탄 켜기** — 프리팹·코드·그림은 D11 이 다 만들어 놨다. **CSV 가 들어와야 게임에 나온다** — 아래 §요청-5 | [`DONE/D11.md`](../DONE/D11.md) · [`REQ/DEV.md` 요청-10](DEV.md) |
| 2026-08-30 | DEV | `열림` | **D12 실플레이 결과 회신** — 요청-4·5 의 "느낌" 질문 5개에 실측 답. + **검 전용 SFX 부탁**(아직 총소리를 쓴다) — 아래 §요청-6 | [`DONE/D12.md`](../DONE/D12.md) |

> 상태값: `열림` · `진행중` · `닫힘(C2)` · `보류(사유)`
> 처리했으면 상태만 바꾼다. **줄을 지우지 않는다.**

---

## 요청-1 — Goblin·Slime 걷기 시트 재생성 (B2)

**무엇을**

| 파일 | 지금 상태 | 필요한 것 |
|---|---|---|
| `Assets/Game/Sprites/Enemies/Walk/Goblin_Walk.png` | 정상 프레임 **4장**(f0~f3). 나머지 12칸은 **작은 고블린 16마리 뭉치** | 16칸 전부 고블린 **1마리씩** |
| `Assets/Game/Sprites/Enemies/Walk/Slime_Walk.png` | 정상 프레임 **4장**(f0~f3). 나머지 12칸은 **작은 슬라임 9마리 뭉치** | 16칸 전부 슬라임 **1마리씩** |

**규격 — `Ogre_Walk.png` / `Zombie_Walk.png` 이 정답 예시다** (이 둘은 16칸 전부 정상):

- **1024×1024**, 4×4 격자, 칸 하나 **256×256**
- 칸마다 **캐릭터 딱 한 마리**, 칸 중앙, **여백 30px 이상** ← 🔴 아래 참조
- **전부 같은 방향**(오른쪽)을 본다. 방향 축은 없다 — 좌우는 코드가 `flipX` 로 뒤집는다
- 16칸이 **한 걸음 사이클**로 이어져야 한다 (f0→f15→f0 순환)

**왜**

`EnemyVisual.StepFrames` 가 16칸을 순서대로 도는데, 깨진 칸이 12개라 **걸을 때 12/16 확률로
"작은 몹 여러 마리 뭉치"가 화면에 뜬다.** 정지 중엔 f0 으로 고정돼 멀쩡해 보인다.
코드·슬라이스·배선은 전부 정상이라 **애셋을 고치는 것 외에 방법이 없다.**

> 🔴 **여백 30px 이상이 중요하다 — B1 때문이다.** 외곽선 셰이더가 반지름 **28텍셀**을
> 훑는데 지금 `SampleAlpha` 의 uv 범위 검사가 시트에서 동작하지 않아 **옆 칸을 빨아들인다.**
> Wolf(여백 14px)·Demon(여백 0px)이 그래서 외곽선이 어긋난다.
> B1 을 고치기 전까지는 **여백이 곧 방어막**이다.

**어떻게 확인하나 (판정 기준)**

1. `_Incoming/` 에 넣기 전 **알파 채널 min/max 확인** (I-41 — AI 의 "투명 배경"은 가짜다)
2. 칸마다 알파 덩어리가 **정확히 1개**인지. 아래로 셀 수 있다:

```python
from PIL import Image
C = 256
a = Image.open('Goblin_Walk.png').convert('RGBA').split()[3]
for i in range(16):
    r, c = divmod(i, 4)
    bb = a.crop((c*C, r*C, (c+1)*C, (r+1)*C)).getbbox()
    print(i, bb, '너비x높이', bb[2]-bb[0], bb[3]-bb[1])
```
   → 16줄 모두 **너비·높이가 200 이하**여야 한다 (여백 28px 이상 확보).
   Goblin 현재값은 f5~f15 가 `242x235` 로 나온다 = 깨진 것

3. 배선은 DEV 가 한다 — `_Incoming/` 에 두고 [`REQ/DEV.md`](DEV.md) 에 임포트 요청을 남길 것.
   `EnemyData.WalkFrames` 가 16장으로 다시 채워지면 완료

### 처리 결과 — C1 (2026-08-30, CONTENT) · `닫힘`

**요청과 다르게 한 것이 하나 있다 — 재생성이 아니라 `반복`이다.**

요청은 "16칸 전부 1마리씩 **재생성**"이었다. 실제로는 **정상 4프레임을 4번 반복**해 채웠다.
AI 재생성은 같은 사이클로 이어지는 16장을 보장하지 못하는데(이 시트가 깨진 원인이 그거다),
반복은 **f0→f3→f0 이 무조건 이어진다.** 바닥 y 가 프레임마다 같은 것도 확인했다
(Goblin 189 · Slime 173) — 발이 안 튄다.

**대가:** 걷기 사이클이 4프레임이라 Ogre·Zombie(16프레임)보다 **덜 부드럽다.**
16프레임 애니메이션이 정말 필요하면 그때 다시 요청할 것.

덤으로 **Demon·Wolf 의 잔여물도 같이 지웠다**(요청에 없던 것 — 같은 증상이라).
상세·판정 기준은 [`../BUGS.md`](../BUGS.md) §B2 와 [`DEV.md`](DEV.md) 요청-1.

---

## 요청-2 — 신규 SFX 6종의 볼륨·피치를 `TUNING.md` 에 등재 (D8)

**무엇을** — D8 이 넣은 효과음 6종은 **요청-3 표의 숫자를 그대로 쓴 것**이고
**아직 아무도 들어 보지 않았다.** `TUNING.md` 는 CONTENT 소유라 여기로 넘긴다.

| 키 | 클립 | Volume | PitchJitter |
|---|---|---:|---:|
| `BuildingFire` | `SFX_BuildingFire.wav` (0.52s) | 0.22 | 0.14 |
| `EnemyShoot` | `SFX_EnemyShoot.wav` (0.63s) | 0.30 | 0.12 |
| `UiCancel` | `SFX_UiCancel.wav` (0.52s) | 0.45 | 0.03 |
| `Heal` | `SFX_Heal.wav` (0.91s) | 0.50 | 0.05 |
| `EnemyDieElite` | `SFX_EnemyDieElite.wav` (1.23s) | 0.75 | 0.04 |
| `BossAppear` | `SFX_BossAppear.wav` (2.51s) | 0.95 | 0.00 |

**무엇을 보나 — 로그로는 판정이 안 되는 것들이다**

1. 🔴 **터렛 3채 이상 세우고 — 내 발사음이 터렛 소리에 묻히나?**
   묻히면 `BuildingFire` 를 **0.22 → 0.15** 로 먼저 내린다
2. 원거리 웨이브에서 `EnemyShoot` 이 **잔소리로 들리나** (터렛과 겹치면 특히)
3. 엘리트가 죽을 때 **"무거운 게 죽었다"로 들리나** — 안 들리면 0.75 를 더 올린다
4. `BossAppear` 2.51초가 **웨이브 시작 소음에 묻히나**
5. 리롤(`UiCancel`)이 **구매(`UiSelect` 0.55)와 확실히 구분되나**

**어디를 고치나** — `Assets/Game/Audio/AudioLibrary.asset` 의 해당 `Volume`/`PitchJitter`.
🔴 **`.asset` 은 DEV 소유다.** 숫자를 정해서 [`DEV.md`](DEV.md) 로 요청할 것.

**🔴 먼저 알아야 할 것 — 이 머신은 지금 게임이 무음이다.**
저장된 설정이 `bgm_vol=0.00` · `sfx_vol=0.00` 이다(버그 아님, 저장된 사용자 설정).
`AudioManager.PlaySfx()` 가 `SfxVolume <= 0f` 이면 즉시 return 하므로 **아무 소리도 안 난다.**
옵션에서 볼륨을 올리지 않으면 위 5개를 **하나도 판정할 수 없다.**

### 처리 결과 — C10 (2026-08-30, CONTENT) · `닫힘`

[`../../TUNING.md`](../../TUNING.md) **§2-C** 에 등재했다. 새 절을 만들지 않고
**C3 때 이미 있던 "신규 7종" 목록을 갱신**했다 — 같은 소리를 두 곳에서 관리하면 갈라진다.

바뀐 것:

1. 제목 `신규 7종 — 배선 후에 들어 볼 것` → **`신규 6종 — 배선 끝났다. 지금 들어 볼 것`**.
   C3 은 설계, D8 은 배선이라 둘 다 출처로 적었다
2. **표를 넣었다** — 키 · 클립 · **길이** · Volume · **PitchJitter**.
   기존 목록엔 Volume 뿐이었는데, 지터는 "매번 다른 음정으로 들리나"의 원인이라 같이 봐야 한다
3. `BossAppear` 의 **지터가 0인 이유**를 적었다 — 예고음의 음정이 매번 다르면
   "이 소리 = 보스"라는 학습이 안 된다. 나중에 "왜 여기만 0이지" 하고 흔들지 않게
4. 🔴 **무음 경고를 옮겨 적었다** (`sfx_vol=0.00` → `PlaySfx` 즉시 return)
5. ⚠️ **`AudioLibrary.asset` 은 DEV 소유**라 숫자를 정해도 CONTENT 가 못 고친다는 것을 명시
6. **`Crit` 을 6종에서 떼어 냈다** — 나머지와 달리 아직 배선되지 않았다
   (`CalculateDamage` 가 치명타 결과를 버린다). 섞여 있으면 "왜 안 울리지"로 헛수고한다

**판정은 아직 하나도 못 했다** — 위 🔴 대로 이 머신이 무음이라서다.
소리를 켜고 5개를 들어 본 사람이 숫자를 정하면, 그때 `REQ/DEV.md` 로 넘어간다.

---

## 요청-3 — 화살 크기(PPU) 체감 항목을 `TUNING.md` 에 확정 (D9)

**무엇을** — 요청-6(C7)이 남긴 *"크기가 너무 크다/작다면 PPU 조절"* 을
`TUNING.md` §3 3단계에 **실측값으로 갱신**해 달라. 지금 적혀 있는 건 예상치다.

**들어간 값 (D9 확정)**

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Game/Sprites/Projectiles/Arrow.png` (256×256) |
| **PPU** | **512** |
| `sprite.bounds.size` | `(0.500, 0.500)` ← 텍스처 전체 rect 기준 |
| 그림의 실제 크기 | 알파 bbox `200×72px` = **`0.391 × 0.141` 유닛** |
| 비교 대상 | 기존 총알 `Bullet.png` 0.41 유닛 → **사실상 같은 폭** |

> ⚠️ **요청-6 판정 ①의 `(0.391, 0.141)` 은 `bounds.size` 로는 안 나온다.**
> Single 스프라이트의 `bounds` 는 **텍스처 전체**(256/512 = 0.5)를 쓴다.
> 알파 bbox 기준이 `200/512=0.391` · `72/512=0.141` 이다. 둘 다 맞는 숫자이니
> 다음에 판정 기준을 쓸 때 **어느 쪽 숫자인지 같이 적을 것.**

**무엇을 보나 — 로그로는 판정이 안 된다**

1. 활을 들고 쐈을 때 화살이 **총알보다 눈에 띄게 크거나 작게** 느껴지나
2. 화살이 **적보다 커 보이나** (적은 대체로 0.4~0.5 유닛이다)
3. 여러 발이 동시에 날아갈 때 **화면이 지저분해지나**

**이상하면 어떻게 고치나** — 그림을 다시 그리지 않는다. **PPU 숫자 하나**다.

| 원하는 것 | PPU |
|---|---:|
| 지금 (0.391 유닛) | 512 |
| 25% 크게 | 410 |
| 25% 작게 | 640 |
| 절반 | 1024 |

🔴 **`.png.meta` 는 `Assets/Game/Sprites/` 아래라 CONTENT 소유지만, 재임포트는 에디터라 DEV 다.**
숫자를 정해서 [`DEV.md`](DEV.md) 로 요청할 것.

### 처리 결과 — C11 (2026-08-30, CONTENT) · `닫힘`

[`../../TUNING.md`](../../TUNING.md) **§3 3단계**의 "화살 크기" 행을 예상치 → **D9 실측치**로 갈고,
바로 아래에 **PPU 환산표**를 새로 놓았다.

바뀐 것:

1. 행 제목 `(C7)` → **`(C7 → D9 실측)`**. 근거의 출처가 "감"에서 "측정"으로 바뀐 걸 드러낸다
2. 크기를 **`0.391 × 0.141` 유닛 (알파 bbox `200×72px`)** 로 확정. 비교 대상도
   "총알 0.5" 라고 어림하던 것을 **`Bullet.png` 0.41 유닛**으로 정정했다
3. **환산표 4줄**을 표로 뽑았다 — 본문에 "640·768·384" 라고 섞어 적어 두면
   막상 고칠 때 다시 계산하게 된다
4. 🔴 **D9 이 걸린 함정을 경고로 남겼다** — `sprite.bounds.size`(0.500, 텍스처 전체)와
   눈에 보이는 크기(0.391, 알파 bbox)는 **다르다.** 요청-6 판정 ①이 이것 때문에 어긋났다.
   **판정 기준에 숫자를 쓸 때 어느 쪽인지 같이 적기로** 규칙을 세웠다
5. 재임포트가 DEV 요청이라는 것도 같이 적었다 (`.png.meta` 가 내 경로에 있어서 헷갈린다)

**판정은 아직 못 했다** — 화면을 봐야 하는 항목이라 그대로 `TUNING.md` 에 남는다.
숫자를 정하면 `REQ/DEV.md` 로 재임포트 요청을 넣는다.

---

## 요청-4 — 수리검 CSV 3줄 추가 + 검을 근접으로 재해석 (D10)

**왜** — D10 이 `DESIGN_CLASSES.md` §6 2단계의 **코드·프리팹·그림을 전부 만들어 놨다.**
지금 게임을 켜면 **아직 아무것도 안 보인다** — CSV 에 줄이 없어서다.
수리검은 레벨업 3택에 안 뜨고, 검은 여전히 총알을 쏜다.
**이 요청이 처리되어야 2단계가 실제로 굴러간다.**

DEV 가 만들어 둔 것 (전부 검증 끝, `Assets/` 안에 이미 있다):

| 프리팹 / 그림 | 무엇 |
|---|---|
| `Assets/Prefabs/Proj_Shuriken.prefab` | 수리검 발사체. **관통 3** · 자전 540°/s |
| `Assets/Game/Sprites/Projectiles/Shuriken.png` | 발사체 그림 (PPU 512 → `0.5 × 0.5` 유닛) |
| `Assets/Game/Sprites/Weapons/Shuriken.png` | 아이콘 (1024², `Sword.png` 와 같은 규격) |
| `Assets/Prefabs/Weapon_Melee.prefab` | **근접 무기 본체** (`MeleeWeapon`) |
| `Assets/Prefabs/Fx_SwingArc.prefab` | 휘두름 호 이펙트 (6프레임, 30fps) |

---

### 4-A. `Weapons.csv` — 수리검 줄 **추가**

헤더는 이렇다 (12열):

```
Id,WeaponName,WeaponPrefab,Icon,ProjectilePrefab,TravelPrefab,ProjectileSpeed,Damage,Cooldown,ProjectileSize,ProjectileCount,Range
```

수리검은 **원거리 무기 그대로**다. `WeaponPrefab` 은 기존 `Weapon_Sword.prefab`
(= `ProjectileWeapon` 컴포넌트)을 **그대로 쓴다.** 새 파생 클래스는 만들지 않았다.

```
Shuriken,Shuriken,Assets/Prefabs/Weapon_Sword.prefab,Assets/Game/Sprites/Weapons/Shuriken.png,Assets/Prefabs/Proj_Shuriken.prefab,,<speed>,<damage>,<cooldown>,<size>,<count>,<range>
```

- `TravelPrefab` 은 **빈칸**(쉼표 두 개 연속). 수리검은 잔상 프리팹이 없다
- 🔴 `<>` 안의 6개는 **CONTENT 가 정한다.** 감으로 넣지 말고 **기존 무기 줄과 나란히 놓고**
  정할 것 — 관통 3 이라 같은 피해면 실질 3배다. 정한 근거는 `BALANCE.md` 에 남긴다

### 4-B. `Weapons.csv` — `Sword` 줄 **재해석** (열 추가 없음)

지금 줄:

```
Sword,Sword,Assets/Prefabs/Weapon_Sword.prefab,Assets/Game/Sprites/Weapons/Sword.png,Assets/Prefabs/Proj_Bullet.prefab,,8,10|15|22|30|40,1.5|1.3|1.1|0.9|0.7,1|1.1|1.2|1.3|1.5,1|1|2|2|3,10|10|12|12|15
```

바꿀 열은 **4개**다. 나머지는 건드리지 않는다.

| 열 | 지금 | 바꿀 값 | 왜 |
|---|---|---|---|
| `WeaponPrefab` | `Assets/Prefabs/Weapon_Sword.prefab` | **`Assets/Prefabs/Weapon_Melee.prefab`** | 발사체를 쏘지 않고 부채꼴을 벤다 |
| `ProjectilePrefab` | `Assets/Prefabs/Proj_Bullet.prefab` | **`Assets/Prefabs/Fx_SwingArc.prefab`** | 이 열이 근접에서는 **휘두름 이펙트**를 뜻한다 |
| `ProjectileSpeed` | `8` | **`0`** | 근접은 이 열을 안 쓴다 |
| `Range` | `10\|10\|12\|12\|15` | **`2\|2\|2.2\|2.2\|2.5`** | 🔴 **뜻이 달라졌다** — 아래 |

🔴 **`Range` 의 뜻이 바뀐다.** 원거리에서 `Range` 는 *발사체가 날아가는 거리*(10~15)였는데,
근접에서는 **플레이어를 중심으로 한 호의 바깥 반지름**이다. `10` 을 그대로 두면
**화면 전체를 한 번에 베는 무기**가 된다. 반드시 2 안팎으로 내릴 것.

`ProjectileCount` (`1|1|2|2|3`) 도 뜻이 바뀌지만 **값은 그대로 둬도 된다** —
원거리에선 *동시에 쏘는 발사체 수*, 근접에선 *한 쿨다운에 몇 번 베는지(연타)* 다.
1발 → 1번 베기, 3발 → 3연타. 숫자의 체감이 비슷해서 그대로 굴러간다.

🔴 **`Damage` 는 올려야 한다. 다만 얼마나 올릴지는 이 요청에서 정하지 않는다.**
사거리가 10 → 2 로 **5분의 1**이 됐다. 같은 피해면 검은 그냥 약해진 무기다.
그런데 근접의 세기는 *"적에게 붙어 있는 시간"* 이 정하는 값이라 **플레이해 봐야 안다.**
→ **`TUNING.md` 에 항목으로 올리고, 첫 값은 지금 값 그대로 두거나 보수적으로만 올린다.**
숫자를 감으로 확정해서 CSV 에 박아 넣지 말 것.

### 4-C. `Items.csv` — 수리검 줄 **추가**

헤더: `Id,ItemName,Description,Icon,Category,MaxLevel,RefId,ShopPrice`

```
Shuriken,Shuriken,<영문 설명>,Assets/Game/Sprites/Weapons/Shuriken.png,Weapon,5,Shuriken,<price>
```

- `RefId` 는 **`Weapons.csv` 의 `Id` 와 정확히 같아야** 물린다 → `Shuriken`
- 🔴 **`Description` 은 화면에 뜬다 → 영문으로 쓸 것** (`CLAUDE.md` §3).
  `Sword` 줄이 `Swings a blade at the nearest enemy.` 인 것과 같은 톤으로.
  **관통한다는 것**이 이 무기의 정체성이니 설명에 드러나야 한다
- `MaxLevel` 은 `Weapons.csv` 의 배열 길이(5)와 맞춘다

### 4-D. `SceneWiring.csv` — 레벨업 후보에 등록

7번째 줄 `LevelUpManager,allItems,…` 의 경로 목록 **끝**에 붙인다:

```
|Assets/Game/ItemData/Shuriken.asset
```

🔴 **이 줄을 빼먹으면 `Items.csv` 를 넣어도 레벨업 3택·상점에 안 나온다.**
D10 의 판정 기준 ⑤ 가 바로 이것이라 **아직 미검증 상태로 남아 있다.**

### 4-E. `TUNING.md` 에 올릴 체감 항목

D10 이 정한 값 중 **로그로는 판정할 수 없는 것**들이다. 전부 "돌아가긴 하는데
느낌이 맞는지는 모르는" 상태다.

| 값 | 지금 | 어디 | 무엇을 보나 · 이상하면 |
|---|---:|---|---|
| 수리검 `pierceCount` | `3` | `Proj_Shuriken.prefab` | 3명 뚫는 게 **과한가.** 과하면 2로 |
| 수리검 `spinSpeed` | `540` | `Proj_Shuriken.prefab` | 자전이 **어지러운가.** 빠르면 360 |
| 검 `halfAngle` | `70` | `Weapon_Melee.prefab` | 앞쪽 **140°**를 벤다. 옆의 적이 안 맞아 답답하면 올린다 |
| 검 `hitDelay` | `0.067` | `Weapon_Melee.prefab` | 칼이 몸을 지나는 순간에 피가 뜨나. 늦게 뜨면 내린다 |
| 검 `comboInterval` | `0.18` | `Weapon_Melee.prefab` | 연타가 **따로 노나 / 겹쳐 보이나** |
| `SwingArcFx.frameRate` | `30` | `Fx_SwingArc.prefab` | 휘두름이 **0.2초**다. 굼뜨면 올린다 |
| `spriteRadiusAtScaleOne` | `1.074` | `Fx_SwingArc.prefab` | 🔴 **실측값이다. 손대지 말 것** — 아래 |

🔴 **`spriteRadiusAtScaleOne = 1.074` 는 체감값이 아니라 그림의 실측값이다.**
`SwingArc.png` 셀 256px 중 호의 바깥 반지름이 **107.4px**, PPU 100 이라 1.074 유닛.
호를 `Range` 에 정확히 맞추는 나눗셈에 쓰인다. 이걸 흔들면 **그림이 사거리를 속인다**
(호는 2 만큼 보이는데 실제로는 2.2 를 벤다). 크기를 바꾸고 싶으면 `Range` 를 고칠 것.

**⚠️ 근접 전용 효과음이 없다.** 지금은 원거리와 같은 `SfxId.WeaponFire` 를 재사용한다.
칼 휘두르는 소리가 총소리로 나므로 **"베는 느낌"이 안 산다.** 새 SFX 가 필요하면
`_Incoming/Audio/` 에 넣고 `REQ/DEV.md` 로 배선 요청할 것.

### 4-F. ⚠️ 이번 요청에 **묶지 말 것** — Excalibur / Windforce / Devastator

이 3개도 `WeaponPrefab` 이 `Weapon_Sword.prefab`, `ProjectilePrefab` 이 `Proj_Bullet.prefab` 이다.
검이 근접이 되어도 **이 셋은 원거리인 채로 남는다** — 프리팹 경로를 각자 들고 있어서
`Sword` 줄만 고치면 서로 영향이 없다. **깨지지 않는다.**

다만 *"검의 상위 무기인 Excalibur 가 왜 총알을 쏘지"* 라는 위화감은 남는다.
이건 §6 2단계의 범위가 아니고, 진화·상위 무기 설계와 같이 봐야 하는 문제다.
**이번에 같이 바꾸지 말고 `DESIGN_CLASSES.md` 에 숙제로 적어 둘 것.**

### 처리 결과 — C12 (2026-08-30, CONTENT) · `닫힘`

CSV 3파일을 저장했다. **Import 는 안 했다** — 에디터라 [`DEV.md`](DEV.md) **요청-9** 로 넘겼다.
게임에 나오는 건 그 Import 이후다.

| 파일 | 무엇을 했나 |
|---|---|
| `Weapons.csv` | `Shuriken` 줄 추가 · `Sword` 줄 4열 교체 (12열 유지) |
| `Items.csv` | `Shuriken` 줄 추가 · `Sword` 설명 교체 (8열 유지) |
| `SceneWiring.csv` | `LevelUpManager,allItems` 끝에 `Shuriken.asset` 추가 |
| `BALANCE.md` | 무기표 갱신 + `MeleeWeapon` 열 재해석 · `pierceCount` 경고 |
| `TUNING.md` §3 | 수리검 6값 · 검 `Damage` · D10 의 7값 등재 |
| `DESIGN_CLASSES.md` | §6 2단계 ✅ · §1 "검이 검이 아니다" 해소 · Excalibur 숙제 |

**판단이 갈릴 만한 것 2가지 — 요청과 다르게 정했다.**

**1. 수리검 `ProjectileCount` 를 `1|1|1|2|2` 로 낮췄다.**
내가 요청-7 초안에 써 둔 값은 `1|1|2|2|3` 이었는데 **그건 틀렸다.**
그때는 관통을 몰랐다. 관통 3 × 부채 3 = **한 발에 최대 9히트**다.
Lv5 실효 DPS 로 환산하면 다른 무기의 2.5배가 되어 **다른 무기를 고를 이유가 사라진다.**
낮춘 결과 Lv5 단일 92 / 관통 최대 276 — 활 180 을 위아래로 감싼다.
활은 **폭**(부채), 수리검은 **깊이**(관통)로 갈랐다. Lv1 DPS 는 검과 같게 맞췄다 —
관통은 적 1마리한테는 아무 값도 없어서, 초반엔 같은 무기여야 공평하다.

**2. 검의 `Damage` 를 올리지 않았다 — 요청 4-B 의 지침 그대로다.**
사거리가 10 → 2 로 5분의 1이 됐는데 피해는 그대로다. **약할 가능성이 높다.**
올리고 싶었지만 **올리면 측정이 안 된다.** 지금 검은 *"근접 페널티가 얼마나 큰가"* 를
재는 **대조 실험**이다. 4열만 바꾸고 나머지를 고정했기 때문에
"검이 약하다"가 **근접 때문**이라고 단정할 수 있다 (I-55 — 한 번에 두 변수를 움직이지 않는다).
플레이해서 약하면 그건 버그가 아니라 **측정값**이다. `TUNING.md` §3 최상단 항목으로 올려 뒀다.

**같이 처리한 것**

- 4-E 의 7값을 `TUNING.md` §3 에 표로 등재. `spriteRadiusAtScaleOne = 1.074` 는
  **"손대지 말 것"** 을 명시했다 — 체감값이 아니라 그림의 실측값이다
- 4-F 대로 Excalibur/Windforce/Devastator 는 **건드리지 않았다.**
  `DESIGN_CLASSES.md` §1 에 숙제로 적었고, Windforce 가 아직 `Proj_Bullet` 을 쏘는 것
  (C7 이 `Bow` 만 바꿨다)도 같이 적어 뒀다 — 이건 그림만 바꾸면 되는 싼 작업이다
- ⚠️ 근접 SFX 없음(=총소리)도 `TUNING.md` 에 경고로 남겼다. 판정할 때 이걸 모르면
  "칼 느낌이 안 산다"의 원인을 엉뚱한 데서 찾는다

**판정은 못 했다** — Import 전이라 게임에 아직 안 나온다. 요청-9 참조.

### 4-G. 판정 기준

1. `Weapons.csv` · `Items.csv` · `SceneWiring.csv` 3파일이 **저장**되어 있다
2. Import 후 `Assets/Game/WeaponData/Shuriken.asset` · `Assets/Game/ItemData/Shuriken.asset` 생성
3. 🔴 **레벨업 3택 / 상점에 `Shuriken` 이 뜬다** ← D10 판정 ⑤. 이게 되면 2단계가 닫힌다
4. 검을 들고 플레이 — **총알이 안 나가고 호가 그려진다.** 호의 바깥 끝이 `Range` 와 같다
5. `Weapons.csv` 의 열 **개수가 12개 그대로**다 (열 추가 없음)

🔴 **Import 는 에디터라 DEV 몫이다.** CSV 를 저장한 뒤 [`DEV.md`](DEV.md) 에
`Game/Balance/Import CSV -> ScriptableObjects` 실행을 요청할 것.
**플레이 모드에서는 Import 가 실패한다** — BOARD §2 가 `PLAYING` 이면 밀린다.

---

## 요청-5 — 독 장판 CSV + 바닥 폭탄 켜기 (D11)

**왜** — D11 이 `DESIGN_CLASSES.md` §6 **3·4단계**의 코드·프리팹·그림을 전부 만들어 놨다.
지금 게임을 켜면 **아직 아무것도 안 보인다** — CSV 에 줄이 없어서다.
`Toxin` 은 레벨업 3택에 안 뜨고, `Bomb` 은 여전히 목표 지점에서 곧바로 터진다.
**이 요청이 처리되어야 3·4단계가 실제로 굴러간다.**

DEV 가 만들어 둔 것 (전부 검증 끝, `Assets/` 안에 이미 있다 — 판정 근거는 [`DONE/D11.md`](../DONE/D11.md)):

| 프리팹 / 그림 | 무엇 |
|---|---|
| `Assets/Prefabs/Proj_ToxinField.prefab` | 독 장판 본체. 4초 지속 · 0.5초마다 피해+슬로우 |
| `Assets/Prefabs/Weapon_Field.prefab` | **장판 무기 본체** (`FieldWeapon`) |
| `Assets/Prefabs/Proj_BombGround.prefab` | 바닥에 떨어져 **1.1초 뒤** 터지는 폭탄. 신관이 벌겋게 달아오른다 |
| `Assets/Game/Sprites/Effects/ToxinField.png` | 장판 6프레임 (PPU 100) |
| `Assets/Game/Sprites/Effects/BombGround.png` | 신관 4프레임 (PPU 256) |
| `Assets/Game/Sprites/Weapons/Toxin.png` | 아이콘 (1024², **PPU 512** — `Bomb`/`Bow`/`Shuriken` 과 같은 규격) |

---

### 5-A. `Weapons.csv` — `Toxin` 줄 **추가**

헤더는 그대로 12열이다. 열을 새로 만들지 않는다.

```
Toxin,Toxin,Assets/Prefabs/Weapon_Field.prefab,Assets/Game/Sprites/Weapons/Toxin.png,Assets/Prefabs/Proj_ToxinField.prefab,,0,<damage>,<cooldown>,<size>,<count>,<range>
```

- `TravelPrefab` 은 **빈칸**, `ProjectileSpeed` 는 **0**. 장판은 날아가지 않는다
- 🔴 **`Range` 의 뜻이 또 다르다.** `FieldWeapon` 에서 `Range` 는 사거리가 아니라
  **장판이 떨어질 수 있는 반경**(플레이어 주변 무작위)이다. 크게 잡을수록 넓게 흩뿌린다.
  `MeleeWeapon` 이 `Range` 를 "호의 반지름"으로 재해석한 것과 같은 방식이다
- 🔴 **`Damage` 는 "틱당" 이다.** 장판이 4초 사는 동안 0.5초마다 들어가므로
  **한 번 깔면 최대 8번**이 실제 피해다. 다른 무기의 1히트 피해와 나란히 놓으면 **8배로 과하다.**
  숫자를 정할 때 반드시 이걸 나눠서 볼 것
- `ProjectileSize` 는 장판 반경에 곱해진다 (`fieldRadius 1.2` × 이 값)
- `ProjectileCount` 는 **안 쓴다** (`1|1|1|1|1`). 한 발사에 장판 1개다
- 🔴 `<>` 안의 값은 **CONTENT 가 정한다.** 정한 근거는 `BALANCE.md` 에 남길 것

DEV 가 요청서 초안에 적어 둔 값은 아래와 같으나 **참고일 뿐, 그대로 쓰지 말고 위 3가지
(틱당 피해 · Range 의 뜻 · Size 곱)를 반영해 다시 볼 것.**

```
Toxin,Toxin,Assets/Prefabs/Weapon_Field.prefab,Assets/Game/Sprites/Weapons/Toxin.png,Assets/Prefabs/Proj_ToxinField.prefab,,0,4|6|8|11|15,3|2.7|2.4|2.1|1.8,1|1.15|1.3|1.5|1.7,1|1|1|1|1,5|5|6|6|7
```

### 5-B. `Weapons.csv` — `Bomb` 줄에 **바닥 폭탄 켜기** (2열만)

지금 줄:

```
Bomb,Bomb,Assets/Prefabs/Weapon_Aoe.prefab,Assets/Game/Sprites/Weapons/Bomb.png,Assets/Prefabs/Proj_Aoe(Boom).prefab,,0,30|44|62|86|118,3.5|3.2|2.9|2.5|2,1.2|1.35|1.5|1.7|2,1|1|1|1|1,9|9|10|10|11
```

바꿀 열은 **2개**다. 나머지는 건드리지 않는다.

| 열 | 지금 | 바꿀 값 | 왜 |
|---|---|---|---|
| `TravelPrefab` | (빈칸) | **`Assets/Prefabs/Proj_BombGround.prefab`** | 목표까지 날아가 **바닥에 떨어져 기다리는** 몸체 |
| `ProjectileSpeed` | `0` | **`9` 안팎** | 🔴 **0 이면 안 날아간다** — 아래 |

🔴 **`ProjectileSpeed` 가 0 이면 `TravelPrefab` 을 넣어도 소용없다.**
`AoeWeapon` 은 속도가 0 보다 커야 몸체를 쏜다. 지금 `Bomb` 이 즉폭인 이유가 이 0 이다.
(`Fireball` 이 `11` 로 날아가고 있으니 그것보다 조금 느린 값이 폭탄답다.)

🔴 **`ProjectilePrefab`(`Proj_Aoe(Boom)`)은 그대로 둔다.** 이 열은 **폭발**이고
`TravelPrefab` 이 **날아가는 몸체**다. 둘은 다른 것이다.

⚠️ **이건 `Bomb` 의 체감을 크게 바꾼다.** 지금까지 즉시 들어가던 피해가
**비행 + 1.1초 신관** 만큼 늦어진다. 피해량은 **이번에 올리지 말 것** —
D10 의 검과 같은 이유다(I-55, 한 번에 두 변수를 움직이지 않는다).
느려진 만큼 약해졌는지는 **플레이해서 재는 값**이고, `TUNING.md` 항목이다.

### 5-C. `Items.csv` — `Toxin` 줄 **추가**

헤더: `Id,ItemName,Description,Icon,Category,MaxLevel,RefId,ShopPrice`

```
Toxin,Toxin,<영문 설명>,Assets/Game/Sprites/Weapons/Toxin.png,Weapon,5,Toxin,<price>
```

- `RefId` 는 `Weapons.csv` 의 `Id` 와 정확히 같아야 물린다 → `Toxin`
- 🔴 **`Description` 은 화면에 뜬다 → 영문으로 쓸 것** (`CLAUDE.md` §3).
  이 무기의 정체성은 **"느리게 만든다"** 다. 이 게임 최초의 슬로우 수단이므로
  설명에 반드시 드러나야 한다. 초안: `Drops a toxic pool that slows and burns anything inside.`
- `MaxLevel` 은 `Weapons.csv` 의 배열 길이(5)와 맞춘다

### 5-D. `SceneWiring.csv` — 레벨업 후보에 등록

7번째 줄 `LevelUpManager,allItems,…` 의 경로 목록 **끝**에 붙인다:

```
|Assets/Game/ItemData/Toxin.asset
```

🔴 **이 줄을 빼먹으면 `Items.csv` 를 넣어도 레벨업 3택·상점에 안 나온다.**
D11 판정 ⑨ 가 바로 이것이라 **아직 미검증 상태로 남아 있다.**

### 5-E. `TUNING.md` 에 올릴 체감 항목

D11 이 정한 값 중 **로그로는 판정할 수 없는 것**들이다.
전부 "돌아가긴 하는데 느낌이 맞는지는 모르는" 상태다.

| 값 | 지금 | 어디 | 무엇을 보나 · 이상하면 |
|---|---:|---|---|
| `fuseTime` | `1.1` | `Proj_BombGround.prefab` | 🔴 **이번 작업의 핵심 체감값.** 피할 시간을 주는가, 답답한가. 길면 0.8 / 짧으면 1.5 |
| `fieldDuration` | `4.0` | `Proj_ToxinField.prefab` | 장판이 **화면을 뒤덮는가.** 겹쳐 쌓이면 내린다 |
| `tickInterval` | `0.5` | `Proj_ToxinField.prefab` | 피해 숫자가 **너무 자주 뜨는가.** 🔴 이걸 내리면 총 피해가 그만큼 곱으로 늘어난다 |
| `slowMult` | `0.6` | `Proj_ToxinField.prefab` | 40% 감속. **체감이 안 되면** 0.5, 적이 멈춰 보이면 0.7 |
| `fieldRadius` | `1.2` | `Weapon_Field.prefab` | 장판 하나의 크기. `Weapons.csv` 의 `ProjectileSize` 가 곱해진다 |
| `frameRate` (장판) | `12` | `Proj_ToxinField.prefab` | 6프레임 루프. 끊겨 보이면 올린다 |
| `spriteRadiusAtScaleOne` | `0.97` | `Proj_ToxinField.prefab` | 🔴 **실측값이다. 손대지 말 것** — 아래 |

🔴 **`spriteRadiusAtScaleOne = 0.97` 과 `ToxinField.png` 의 Custom pivot 은 체감값이 아니다.**

DEV 가 요청-8 초안에 적어 둔 **"Pivot Center"** 와 **"`1.0` 고치지 말 것"** 은 **둘 다 틀렸다.**
셀의 알파를 실제로 재 보니 그려진 원의 중심이 셀 중앙(128,128)이 아니라 **(134, 132)** 이고
반지름이 128px 이 아니라 **≈97px** 이다. 그래서:

- pivot = `(0.5234375, 0.484375)` — Center 를 쓰면 **맞는 원과 보이는 원이 어긋난다**
- `spriteRadiusAtScaleOne = 0.97` — `1.0` 을 쓰면 그림이 3% 작게 나온다

**사용자 승인을 받아 실측값으로 보정했다.** 화면 픽셀로 검증한 중심 오차가 **0.7px** 다
(보정이 없었다면 약 6.7px 어긋났어야 한다 — 10배 차이).
이걸 흔들면 **그림이 판정 범위를 속인다.** 크기를 바꾸고 싶으면 `ProjectileSize` 나
`fieldRadius` 를 고칠 것.

**⚠️ 장판 전용 효과음이 없다.** 지금은 `SfxId.WeaponCast` 를 재사용한다.
새 SFX 가 필요하면 `_Incoming/Audio/` 에 넣고 `REQ/DEV.md` 로 배선 요청할 것.

### 5-F. 참고로 알아 둘 것 (고칠 것 없음)

- **`Toxin.png` 아이콘은 PPU 512 로 임포트했다.** 처음에 Unity 기본값 100 으로 들어가
  다른 아이콘(`Bomb`/`Bow`/`Shuriken`, 전부 512)과 10배 차이가 났다. 이미 고쳤다
- **`Proj_BombGround` 는 scale 0.5** 다. 그림이 PPU 256 이라 그대로 두면 폭탄이 너무 크다
- **장판은 `sortingOrder = −5`** 라 플레이어·적 **아래**에 깔린다. 의도된 값이다

### 5-G. 판정 기준

1. `Weapons.csv` · `Items.csv` · `SceneWiring.csv` 3파일이 **저장**되어 있다
2. Import 후 `Assets/Game/WeaponData/Toxin.asset` · `Assets/Game/ItemData/Toxin.asset` 생성
3. 🔴 **레벨업 3택 / 상점에 `Toxin` 이 뜬다** ← D11 판정 ⑨. 이게 되면 4단계가 닫힌다
4. 폭탄을 들고 플레이 — **목표 지점까지 날아가 바닥에 떨어지고, 잠깐 뒤에 터진다**
5. `Weapons.csv` 의 열 **개수가 12개 그대로**다 (열 추가 없음)

🔴 **Import 는 에디터라 DEV 몫이다.** CSV 를 저장한 뒤 [`DEV.md`](DEV.md) 에
`Game/Balance/Import CSV -> ScriptableObjects` 실행을 요청할 것.
**요청-9(수리검) 와 한 번에 묶어서 요청하면 Import 한 번으로 끝난다.**

---

### 처리 결과 — `닫힘(C15)` · 2026-08-30

CSV 4곳 전부 저장했다. **열은 하나도 안 늘렸다** (Weapons 12 · Items 8, 검증 완료).
Import 는 [`REQ/DEV.md` 요청-10](DEV.md) 으로 넘겼다 — **요청-9 와 같은 Import 한 번**에 묶었다.

| 파일 | 무엇 |
|---|---|
| `Weapons.csv` | `Toxin` 줄 추가 · `Bomb` 의 `TravelPrefab`+`ProjectileSpeed` 2열 교체 · 헤더 주석에 `Weapon_Field` 재해석 3열 명시 |
| `Items.csv` | `Toxin` 줄 추가 (`ShopPrice 10`) |
| `SceneWiring.csv` | `LevelUpManager,allItems` 끝에 `Toxin.asset` |
| `BALANCE.md` | `FieldWeapon` 열 재해석 블록 + 수치 근거표 · `TravelPrefab` 표에 "속도 0 이면 안 먹는다" 추가 · 바닥 폭탄 신관 절 |
| `TUNING.md` | §3 에 5줄 (장판 피해 · 낙하 반경 · 크기 · frameRate · `spriteRadiusAtScaleOne`) + 장판 SFX 부재 |

**내가 요청서와 다르게 정한 것 3가지** — 근거는 요청-10 에 전부 적었다.

1. 🔴 **`Damage` 를 초안 `4|6|8|11|15` → `3|4|6|8|11` 로 내렸다.** 요청서 스스로가
   *"`Damage` 는 틱당이다"* 라고 경고해 놓고 **초안 숫자가 그걸 반영하지 않았다.**
   ×8 하면 Lv5 **120** 으로 Bomb 한 방(118)과 같아진다 — **거기에 슬로우까지 붙으면
   Bomb 을 집을 이유가 없어진다.** 완전 흡수 총량을 Bomb 의 **75%** 로 낮추고
   나머지 25% 를 **감속 값으로 지불**했다
2. **`Range` 5|5|6|6|7 → 4|5|5|6|6.** Lv1 은 쿨(3) > 지속(4) 이라 장판이 **하나뿐**이다.
   하나뿐인 장판이 빗나가면 그 판은 무기가 없는 것과 같다 → 좁게 시작해서 겹치기 시작하는
   Lv4~5 에서만 넓힌다
3. **`ProjectileSize` 1→1.7 → 1→1.5.** Lv5 는 쿨(1.8) < 지속(4.0) 이라 **2~3장이 동시에 산다.**
   크기와 개수가 같이 붙으면 화면을 덮는다 — 성장은 **개수**로 보여 준다

**손대지 않은 것** — `spriteRadiusAtScaleOne 0.97` · Custom pivot · `Bomb` 의 `Damage`.
앞의 둘은 D11 의 실측값이고, 마지막은 **검(C12)과 같은 대조 실험**이다.
폭탄이 약하게 느껴지면 그건 버그가 아니라 **"늦어진 대가"의 측정값**이다 (I-55).

---

## 요청-6 — 🔴 D12 실플레이 결과 회신 + 검 전용 SFX (D12)

**배경** — 요청-4(C12) · 요청-5(C15) 의 CSV 를 **Import 1회**로 반영하고 실플레이까지 마쳤다
(`Weapons: 10 · Items: 25 · SceneWiring 11/11`, 에러 0). 두 요청 모두 **판정 전부 PASS**다.
전체 상세 → [`DONE/D12.md`](../DONE/D12.md)

### ① 물어본 것에 대한 답 (수치 조정은 CONTENT 판단)

| 질문 | 실측 답 |
|---|---|
| **검이 얼마나 약한가** (C12) | **맞기만 하면 안 약하다.** Lv5(사거리 2.50·쿨 0.70·3연타·40딜) 14초에 **14킬** — 수리검 Lv5 의 16킬과 동급. 🔴 **문제는 세기가 아니라 "존재"다** — 가만히 선 플레이어에게 가장 가까운 적이 **4.67유닛**에 머물러 **6초 동안 0회** 휘둘렀다. 카이팅하면 검은 자주 논다 |
| **수리검이 떼거리에서 과한가** (C12) | **그렇다, 뚜렷하게.** 발사체당 명중이 흩어짐 **0.91** → 뭉침 **3.10** (3.4배, 프리팹 `pierceCount: 3` 상한에 붙는다). Lv5 상한 ≈ **276 DPS** |
| **감속이 느껴지는가** (C15, `slowMult 0.6`) | 수치로는 D11 에서 이미 `ratio=0.600` 확인. 화면에서도 안쪽 적이 눈에 띄게 처진다 |
| **Lv5 에서 장판이 화면을 덮는가** (C15) | **안 덮는다.** 웅덩이 하나가 화면 세로의 약 1/4. 2개 동시(쿨 1.8 < 지속 4.0)여도 시야를 안 먹는다 → **`ProjectileSize` 아직 줄일 필요 없다** |
| 🔴 **장판이 적을 한 번도 안 만나는 판이 있는가** (C15) | **있다. 눈으로 봤다.** 캡처의 살아 있는 장판 2개 중 **하나는 안에 적이 0마리**였다. `FieldWeapon` 은 조준하지 않고 주변 무작위 좌표에 까는 물건이라 **구조적으로 빗나간다.** 헛방이 문제면 (가) 반경 축소로 발밑에 붙이거나 (나) 개수 증가로 확률을 덮는 두 방향 |

### ② 부탁 — 검 전용 SFX

`MeleeWeapon.cs:52` 가 아직 `SfxId.WeaponFire`(**총소리**)를 쓴다. 근접으로 재해석했는데 소리는 총이다.

- **무엇을** — 칼이 공기를 가르는 "쉭" 소리 1종. 0.2~0.4초. `Assets/Game/Audio/SFX/` 규격
- **어디에 쓰이나** — 새 `SfxId` 가 필요하다. **`SfxId` 열거형 추가와 호출부 교체는 DEV** 이므로
  클립이 나오면 [`REQ/DEV.md`](DEV.md) 로 요청을 넣어 주면 된다 (D8 때와 같은 경로)
- **판정 기준** — 총소리가 아닐 것 · 3연타로 겹쳐 나도 시끄럽지 않을 것 → `TUNING.md`

### ③ `TUNING.md` 에 남겨 줬으면 하는 것

1. **검 사거리 2.50 이 카이팅 플레이에서 충분한가** — 늘릴지, 접근 유인을 줄지
2. **수리검 뭉침 DPS ≈ 276 이 과한가**
3. **장판 헛방 비율** — 반경 축소 vs 개수 증가

> 🔴 **`spriteRadiusAtScaleOne 0.97` · Custom pivot 은 이번에도 안 건드렸다.** 실측값 그대로다.

---

## 요청에 반드시 적을 것

- **무엇을** — 파일명 · 해상도 · CSV 열 이름까지
- **어디에 쓰이나** — 어느 SO 의 어느 필드에 물릴 그림/소리인지
- **판정 기준** — 크기 · 투명배경 여부 · 프레임 수 · 길이

### 자주 나오는 요청 유형

| 유형 | 비고 |
|---|---|
| 새 CSV 열 **값** 채우기 | 열 **추가**는 DEV(임포터), **값**은 CONTENT |
| 새 스프라이트 / 아이콘 | 완성본은 **`_Incoming/`** 에 둔다 (`Assets/` 밖) |
| 새 SFX / BGM | 볼륨 판정은 `TUNING.md` 로 |
| 수치 조정 | 실플레이 근거가 있으면 `TUNING.md` 에도 남길 것 |

> ⚠️ **C# 필드가 지워지면 `Economy.csv` 의 그 행도 지워야 한다** (`CLAUDE.md` §2).
> DEV 가 필드를 지웠으면 **여기로 요청이 와야 한다.** 안 오면 다음 Import 가 깨진다.

---

## CONTENT 세션이 지켜야 할 것

- 🔴 **SO 를 인스펙터에서 직접 고치지 않는다.** CSV 가 원본이다 (`CLAUDE.md` §2).
  실수로 고쳤으면 `Export ScriptableObjects -> CSV` 로 회수 — 이것도 DEV 요청이다
- CSV 는 **저장만** 한다. Import 는 `REQ/DEV.md` 로 넘긴다
- 새 애셋은 **`Assets/` 안에 직접 넣지 않는다.** `_Incoming/` 에 두고 요청한다.
  Assets 안에 넣으면 DEV 가 플레이 중일 때 임포트가 끼어든다
- 새 아이템/웨이브/직업은 **`SceneWiring.csv` 에도 경로를 추가**해야 게임에 나온다
- 🔴 **AI 가 만든 스프라이트의 "투명 배경"은 가짜다** (I-41).
  알파가 전부 255 이고 체커 무늬가 RGB 에 그려져 있다.
  `_Incoming/` 에 넣기 전에 **알파 채널 min/max 를 확인**한다
- 🔴 **폰트는 Static 115자다** (I-60). 문자표에 없는 글자는 **빈칸으로 나온다.**
  UI 에 새 기호를 쓰려면 폰트를 먼저 다시 구워야 한다 → [`../BALANCE.md`](../BALANCE.md) §2
- 큰 바이너리(수십 MB)는 **넣기 전에** 판단한다. 히스토리에 영구히 남는다
