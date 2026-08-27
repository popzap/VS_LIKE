# VS_LIKE — 밸런스 데이터 가이드

> **작성:** 2026-08-26 · **최종 갱신:** 2026-08-27 (11차 — 건물 1.30유닛 · 곡사포 폭탄 수치 · 폭발 시트 규격)
> 이 문서는 **수치를 어디서 어떻게 고치는가**를 설명한다.
> 완료 이력은 [`SETUP_STATUS.md`](SETUP_STATUS.md), 남은 작업은 [`TODO.md`](TODO.md).

---

## 0. 요약 — 수치를 고치고 싶으면

1. `Assets/Game/Balance/*.csv` 를 엑셀·메모장 아무거나로 연다
2. 값을 고친다
3. Unity 메뉴 **`Game/Balance/Import CSV -> ScriptableObjects`** 실행
4. 콘솔에 `[BalanceImporter] Import 완료` 와 각 테이블 행 수가 찍히면 반영 완료

**인스펙터에서 직접 고치지 말 것.** 다음 Import 때 CSV 값으로 덮어써진다.
부득이 인스펙터에서 고쳤다면 **`Game/Balance/Export ScriptableObjects -> CSV`** 로 CSV에 되돌린 뒤 커밋한다.

---

## 1. 파이프라인 구조

```
Assets/Game/Balance/*.csv        ← 원본(authoring source). 사람이 고치는 곳
        │
        │  Game/Balance/Import CSV -> ScriptableObjects
        ▼
Assets/Game/{EnemyData,WeaponData,BuildingData,PassiveData,ItemData,WaveData,ClassData}/*.asset
Assets/Scenes/SampleScene.unity  ← Economy / SceneWiring / Events 는 씬 컴포넌트에 직접 기록
        │
        │  Game/Balance/Export ScriptableObjects -> CSV   (역방향, SO 전용)
        ▼
Assets/Game/Balance/*.csv
```

| 파일 | 코드 |
|---|---|
| CSV 파서 (런타임 겸용) | `Assets/Scripts/Balance/CsvTable.cs` |
| 에디터 임포터/익스포터 | `Assets/Editor/BalanceImporter.cs` |

### CSV 문법

| 규칙 | 예 |
|---|---|
| 첫 줄이 헤더. **열 순서는 무관**, 이름으로 찾는다 | `Id,MaxHp,MoveSpeed` |
| `#` 로 시작하는 줄은 주석 | `# 여기는 설명` |
| 배열은 `\|` 로 구분 | `10\|15\|22\|30\|40` |
| 값에 쉼표가 필요하면 큰따옴표 (`""` = 따옴표 1개) | `"a, b"` |
| **빈 칸은 기존 값 유지** (0으로 덮지 않는다) | |
| 색은 `#RRGGBB` | `#6FE06F` |

### Id 열의 의미

`Id` 가 곧 **생성될 애셋 파일명**이다. `Enemies.csv` 의 `Id=Wolf` → `Assets/Game/EnemyData/Wolf.asset`.

> ⚠️ **Id 를 바꾸면 새 애셋이 생기고 옛 애셋은 남는다.** 이름을 바꿀 땐 옛 `.asset` 을 직접 지워야 한다.
> 또 다른 테이블(`Items.csv` 의 `RefId`, `Waves.csv` 의 `Spawns`)이 Id로 참조하므로 같이 고쳐야 한다.

---

## 2. 테이블별 안내

### `Enemies.csv` — 적

적은 **전부 `Enemy_Goblin.prefab` 하나를 공유**하고 `Sprite`(종별 고유 그림) / `SizeScale`(크기) 로 구분한다.

| Id | 스프라이트 | 크기 | HP | 속도 | 접촉피해 | 방어 | XP | 골드 | 역할 |
|---|---|---|---|---|---|---|---|---|---|
| Slime | `Sprites/Enemies/Slime` | 0.90 | 45 | 1.4 | 6 | 1 | 4 | 1 | 느리고 무른 초반 잡몹 |
| Goblin | `Sprites/Enemies/Goblin` | 1.00 | 30 | 2.2 | 8 | 0 | 3 | 1 | 기준점 |
| Zombie | `Sprites/Enemies/Zombie` | 1.05 | 70 | 1.6 | 12 | 2 | 6 | 2 | 맷집형 |
| Wolf | `Sprites/Enemies/Wolf` | 0.85 | 40 | 4.2 | 10 | 0 | 6 | 2 | 빠르고 약함 (거리 압박) |
| Demon | `Sprites/Enemies/Demon` | 1.25 | 160 | 2.8 | 20 | 4 | 22 | 8 | 후반 위협 |
| Ogre | `Sprites/Enemies/Ogre` | 1.60 | 220 | 1.2 | 25 | 6 | 18 | 6 | 보스 소재 |

`Elite*Mult` / `Boss*Mult` 는 **배율**이다. 엘리트/보스 스테이지에서 같은 적을 재활용할 때 곱해진다.

**`Tint` 는 이제 전부 `#FFFFFF` 다.** 한 장을 공유하던 시절엔 종을 구분하는 수단이었지만,
`Tint` 는 스프라이트에 **곱해지는** 색이라 컬러 그림 위에 쓰면 탁해진다. 지금은 연출용 예비 열이다.

> ✅ `Tint` 는 이제 **그대로 적용된다.** 엘리트/보스의 색 곱셈은 I-26 에서 **외곽선**으로 대체됐다.
> 등급 색/폭은 CSV 가 아니라 `EnemyBase.ApplyRankOutline()` 의 상수다 (일반 0 · 엘리트 14 · 보스 12).

#### 스프라이트 규격 (I-25 · I-26 · I-27)

| 항목 | 값 |
|---|---|
| 원본 | 1024×1024 RGBA (Unity AI `gpt-image-1-5`, 픽셀아트) |
| `maxTextureSize` | 512 |
| `filterMode` | **Point** (Bilinear 이면 픽셀이 뭉개진다) |
| `textureCompression` | Uncompressed |
| `spritePixelsPerUnit` | **적·건물 1024** (rect 512 → 월드 **0.5유닛**) |
| `spriteMeshType` | **적 6종은 `FullRect`** — 기본값 `Tight` 는 메시가 알파에 붙어 외곽선이 잘린다 (I-26) |
| 바라보는 방향 | **오른쪽**. `EnemyVisual` 이 왼쪽으로 갈 때만 `flipX` 를 켠다 (I-26) |

#### ⚠️ 월드 크기 계산법 — PPU 함정 (I-27)

```
월드 크기 = (축소된 rect) / spritePixelsPerUnit
```

`maxTextureSize` 가 원본보다 작으면 Unity 는 **텍스처만 줄이고 `spritePixelsPerUnit` 은 그대로 둔다.**
즉 `maxTextureSize` 를 절반으로 낮추면 **스프라이트도 절반으로 작아진다.**
크기를 유지하려면 **PPU 도 같이 절반으로** 낮춰야 한다. (I-27 은 이걸 반대로 알고 있어
직업 그림이 적과 똑같은 0.5 유닛이 되어 주인공이 화면에서 안 보였다.)

현재 월드에 놓이는 스프라이트 전부:

| 대상 | 경로 | rect | PPU | 스프라이트 | 프리팹 scale | **화면 크기** |
|---|---|---|---|---|---|---|
| 적 6종 | `Sprites/Enemies` | 512 | 1024 | 0.50 | 1.0 | **0.50** |
| 건물 5종 | `Sprites/Buildings` | 512 | 1024 | 0.50 | **2.6** | **1.30** (11차) |
| 경험치 오브 | `Game/ICON/Exp_Orb.gif` | 150 | **460** | 0.33 | 1.0 | **0.33** (I-33) |
| 직업 정지그림 3종 | `Sprites/Classes/*.png` | 512 | 512 | 1.00 | 1.0 | **1.00** |
| 직업 걷기 프레임 | `Sprites/Classes/Walk/*_Walk.png` | 256 | 256 | 1.00 | 1.0 | **1.00** |
| 바닥 타일 10종 | `Sprites/Tiles/*.png` | 256 | 256 | 1.00 | — | **1.00** (= Grid `cellSize`) |
| 폭발 프레임 16장 | `Sprites/Effects/Explosion.png` | 256 | 256 | 1.00 | **반경에 비례** | **반경 ÷ 0.4 × 1.0** (11차) |
| 폭탄 | `Sprites/Weapons/Bomb.png` | — | — | 2.00 | **0.225** | **0.45** (11차) |

> 적은 `EnemyBase` 가 등급에 따라 스케일을 곱한다 — 엘리트 ×1.3(0.65) · 보스 ×2(1.00).
> **플레이어(1.00)는 일반 몹의 2배, 보스와 같은 크기다.** 이 비율은 아직 실전투에서 안 봤다.
>
> 아이콘·초상화는 UGUI `Image` 로만 쓰여 PPU 가 표시 크기에 영향을 주지 않는다.
>
> ⚠️ **건물 크기는 PPU 가 아니라 프리팹 `m_LocalScale` 로 잡는다.** PPU 를 고치면 같은 시트를
> 공유하는 다른 용도(아이콘 등)까지 딸려 움직이기 때문이다. 5개 프리팹
> (`Building_Turret/Bombard/Village/Farm/Restaurant`) 의 scale 을 **같이** 고칠 것.

#### 바닥 타일 규격 (I-28)

| 항목 | 값 |
|---|---|
| 원본 | 1024×1024 **RGB(불투명)** — `GenerateImage` + `gemini-3.1-flash-texture` (이음매 없음) |
| `maxTextureSize` / PPU | **256 / 256** → 타일 1장 = **1×1 월드 유닛** |
| `filterMode` | **Bilinear** (바닥은 확대되지 않으므로 Point 보다 부드럽다) |
| `spriteMeshType` / `wrapMode` | `FullRect` / `Clamp` |
| `alphaIsTransparency` | **false** (알파가 없다) |
| `Tile.colliderType` | **None** — 바닥은 물리에 참여하지 않는다 |

**타일 색 평준화가 핵심이다.** 독립 생성이라 평균 휘도가 0.164~0.325 로 2배까지 벌어져
그냥 깔면 알록달록한 **퀼트**가 된다. 타일마다 `Tile.color` 배율로 평균을 한 목표색
`(0.155, 0.166, 0.116)` 에 맞춘다. 배율 = `clip(목표 / 평균, 0, 1)`.

> `Tile.color` 는 정점 스트림의 `Color32` 라 **1을 넘는 배율은 잘린다.**
> 밝히는 조정이 안 되므로 **목표색은 가장 어두운 타일 이하**로 잡아야 한다.

등장 비중은 `GroundTiler.tiles` 의 `Weight` 다 (인스펙터, CSV 아님).
`Tile_Grass` 40 · `GrassPebble` 18 · `Weeds` 13 · `Dirt` 10 · `Gravel` 7 ·
`Roots` 5 · `CrackedEarth` 3 · `MossyCobble` 2 · `StoneSlab` 1 · `Flagstone` 1.
**평범한 타일에 비중을 몰아야** 바닥이 산만해지지 않는다.

#### 걷기 스프라이트시트 규격 (I-29)

| 항목 | 값 |
|---|---|
| 생성 | `GenerateSpritesheet` + `video-seedance-1-pro` + **`referenceImageInstanceId`** |
| 배치 | 1024×1024 = **4×4 그리드 · 256px · 16프레임** (`frame_0` ~ `frame_15`) |
| `spriteMode` / `spriteMeshType` | Multiple / `FullRect` |
| `maxTextureSize` / PPU | 1024 / 256 → 프레임 1장 = **1×1 월드 유닛** |
| `filterMode` | Point |

> **`referenceImageInstanceId` 를 반드시 넘길 것.** 안 넘기면 매번 다른 캐릭터가 나온다.
> 참조로 쓸 그림이 없으면 `GenerateSprite` 로 한 장 먼저 뽑아 그 `FileInstanceID` 를 넘긴다.
>
> ⚠️ **`GenerateSpritesheet` 출력에는 알파가 없다.** 흰 배경이 진짜 불투명 픽셀이라
> 그대로 쓰면 캐릭터가 흰 사각형으로 렌더된다. 프레임마다 테두리에 닿은 연결 성분만
> 지우는 후처리가 필요하다 (→ [`SETUP_STATUS.md`](SETUP_STATUS.md) 2-11).
>
> ⚠️ **"투명 배경"이라고 나와도 가짜다 (I-41).** 모델이 **체커보드 무늬를 RGB 에 그려 넣고**
> 알파는 전부 255 로 채운다. `alphaIsTransparency` 를 켜도 소용없다.
> 확인법: 알파 채널의 min/max 가 `(255, 255)` 면 가짜다.
> 복구법은 [`SETUP_STATUS.md`](SETUP_STATUS.md) 2-13 — **테두리 flood fill 만으로는 부족하다.**
> 연기 안쪽에 갇힌 체커 구멍이 흰 덩어리로 남으므로 **체커 회색(184,184,181)±16 픽셀을 전부 씨앗으로**
> 잡아 flood fill 해야 한다. 씨앗 방식이라야 흰 섬광 중심(체커와 안 이어짐)이 살아남는다.

#### 폭발 스프라이트시트 규격 (I-40 · I-41)

| 항목 | 값 |
|---|---|
| 경로 | `Assets/Game/Sprites/Effects/Explosion.png` |
| 배치 | 1024×1024 = **4×4 그리드 · 256px · 16프레임** (`frame_0` ~ `frame_15`) |
| `maxTextureSize` / PPU | 1024 / **256** → 프레임 1장 = **1×1 월드 유닛** |
| `filterMode` / 압축 | Point / Uncompressed · mipmap 끔 |

> 옛 `Game/ICON/폭발 애니메이션 시퀀스.png` 는 **불투명 배경 30컷 몽타주**라 쓸 수 없어 폐기했다.

### `Weapons.csv` — 무기

배열 인덱스 = 레벨-1 (Lv1~Lv5).

| Id | 구현 | 프리팹 | 성격 |
|---|---|---|---|
| Sword | `ProjectileWeapon` | `Weapon_Sword` + `Proj_Bullet` | 기준점. 중간 사거리/피해 |
| Bow | `ProjectileWeapon` | `Weapon_Sword` + `Proj_Bullet` | 사거리 길고 투사체 수가 빨리 늘어남 |
| Gun | `ProjectileWeapon` | `Weapon_Sword` + `Proj_Bullet` | 쿨 0.45→0.22. 저피해 연사 |
| Fireball | `AoeWeapon` | `Weapon_Aoe` + `Proj_Aoe(Boom)` | 가장 가까운 적 위치에 폭발 |
| Bomb | `AoeWeapon` | `Weapon_Aoe` + `Proj_Aoe(Boom)` | 쿨 길고 피해 큼 |

> `AoeWeapon` 은 `ProjectileCount` / `ProjectileSpeed` 를 **쓰지 않는다.**
> 폭발 반경은 `Weapon_Aoe.prefab` 의 `explosionRadius`(**2**) × `ProjectileSize` 다.
> **날아가는 시간이 없다** — 목표 지점에 즉시 터진다. 비행하는 건 곡사포뿐이다(11차).
>
> ⚠️ 11차부터 폭발 그림이 **반경에 맞춰 커진다** (`scale = 반경 ÷ 0.4`).
> 예전엔 반경과 무관하게 항상 0.5유닛이라 피해 범위와 그림이 따로 놀았다.
> 그림을 정직하게 만드는 순간 **반경 3 = 지름 6유닛**(화면의 3분의 1)이 드러나서
> 곡사포와 같은 **2 로 낮췄다** (11차). 면적이 56% 줄었으므로 Fireball/Bomb 의
> 실효 DPS 는 아래 표보다 낮다 — 다수 적 상황에서 재측정 필요.

Lv1 **단일 대상** DPS 대략치 — Sword 6.7 / Bow 7.3 / Gun 11.1 / Fireball 8.2 / Bomb 8.6.
Gun 이 단일 대상 DPS는 높지만 사거리·투사체 크기가 작고, AoE 둘은 다수 적에서 실효 DPS가 뒤집힌다.

### `Passives.csv` — 패시브

**값은 누적이 아니라 "그 레벨일 때의 총 보너스"다.** Lv3이면 3번째 값 하나만 더해진다.
패시브 하나는 자기 스탯 한 줄만 채우고 나머지는 0으로 둔다 (효과를 읽기 쉽게).

| Id | 대상 | Lv1 → Lv5 | 단위 |
|---|---|---|---|
| Damage | `Damage` | +0.10 → +0.55 | 배율 가산 (0.1 = 피해 +10%) |
| MoveSpeed | `MoveSpeed` | +0.30 → +1.60 | 절대값 (기본 4) |
| AttackSpeed | `AttackSpeed` | −0.06 → −0.30 | 쿨다운 배수 가산. **음수가 빠름** |
| MaxHp | `MaxHp` | +25 → +175 | 절대값 (기본 100) |
| Armor | `Armor` | +1 → +7 | 절대값. 받는 피해 감산 |
| PickupRadius | `PickupRadius` | +0.6 → +4.0 | 절대값 (기본 2) |
| XpGain | `XpGain` | +0.12 → +0.75 | 배율 가산 |
| GoldGain | `GoldGain` | +0.15 → +1.00 | 배율 가산 |
| CritChance | `CritChance` | +0.04 → +0.25 | 확률 가산 (기본 0.05) |
| BuildingCooldown | `BuildingCooldown` | −0.10 → −0.42 | 건물 주기 배수 가산. **음수가 빠름** |

> `MoveSpeed` 는 예전에 이름과 달리 체력·공격력·치명타를 같이 주는 만능 패시브였다. 2026-08-26에 바로잡았다.

> ⚠️ **"누적이 아니다"는 규칙이 실제로 지켜지기 시작한 건 I-32 부터다.** 그전에는
> `LevelUpManager` 가 레벨업마다 새 `PassiveEffect` 를 쌓아 Lv1~Lv5 값이 전부 더해졌다.
> 지금 표의 값들은 **누적되던 시절에 감으로 잡은 것**이라 재조정 대상이다
> ([`TODO.md`](TODO.md) §3).
> 패시브를 추가하는 경로는 **`PlayerStats.AddOrUpgradePassive(data, level)` 하나뿐**이다.
> `_activePassives` 에 직접 `new PassiveEffect` 를 넣지 말 것.

**배수형 패시브는 음수 반전에 주의.** `BuildingCooldown` 은 `1 + 보너스` 로 쓰이므로
보너스 합이 −1 을 넘으면 배율이 음수가 된다. `BuildingBase.Cooldown` 의 `Mathf.Max(0.1f, mult)`
하한이 크래시는 막지만 **건물이 10배 빨라지는 형태로 조용히 망가진다.** Lv5 합이 −0.42 인
현재 값은 안전하지만, 새 열을 추가할 때 같은 함정을 다시 밟지 말 것.

### `Buildings.csv` — 설치형 건물

| Id | 프리팹 / 스크립트 | 하는 일 | 쿨다운 Lv1→Lv5 | `Output` Lv1→Lv5 | `MaxCount` |
|---|---|---|---|---|---|
| Turret | `Building_Turret` / `TurretBuilding` | 가장 가까운 적에게 투사체 | 1.2 → 0.6 | — (`Damage` 6→26) | 1→3 |
| Bombard | `Building_Bombard` / `BombardBuilding` | 폭탄을 **쏜다** → 착탄 지점 반경 2 광역 | 3.0 → 1.6 | — (`Damage` 20→80) | 1→2 |
| Village | `Building_Village` / `VillageBuilding` | 경험치 자동 획득 | 6 → 4 | 2 → 10 XP | 1→3 |
| Farm | `Building_Farm` / `FarmBuilding` | 골드 자동 획득 (`GoldGain` 배율 적용) | 12 → 8 | 1 → 6 G | 1→3 |
| Restaurant | `Building_Restaurant` / `RestaurantBuilding` | 회복 아이템을 떨군다 | 60 → 40 | 15 → 85 HP | 1→2 |

- `AttackCooldown` = 건물이 **한 번 일하는 주기(초)**. 전투 건물은 발사 간격, 비전투 건물은 산출 주기.
  실제 주기 = `AttackCooldown × PlayerStats.Final.BuildingCooldown` (하한 0.05초).
- `Output` = 쿨다운 1회당 산출량. 전투 건물은 `Damage` 를 쓰므로 0.
- `MaxCount` = 그 레벨에서 동시에 존재 가능한 개수. **해금/레벨업 시 이 수만큼 설치 대기열에 쌓인다.**
- **식당은 회수 전까지 다음 쿨다운이 돌지 않는다** (`CooldownActive => _pending == null`).
  즉 실질 주기 = `쿨다운 + 플레이어가 밟으러 가는 시간`.

> 건물은 고르는 단계 없이 **`Z` 키로 대기열 맨 앞(=가장 먼저 얻은 것)** 부터 플레이어 옆에 선다.
> 설치 간격/여유는 CSV 가 아니라 씬의 `BuildingManager.placeDistance`(**1.8**) /
> `placeClearRadius`(**0.6**) — `Economy.csv` 로 관리한다.
> 밀리는 무게는 프리팹의 `Rigidbody2D` (`mass 5` / `linearDamping 10`) 라 CSV 밖이다.

#### 설치 간격은 건물 크기에 매여 있다 (11차)

건물을 scale 1.8 → **2.6** 으로 키우면서 `placeDistance` 도 1.2 → **1.8** 로 같이 올렸다.

```
건물 지름   = 콜라이더 반경 0.22222224 × scale 2.6 × 2 ≈ 1.156
링 이웃 간격 = 0.765 × placeDistance = 0.765 × 1.8    ≈ 1.377   ✅ 지름보다 큼
```

> ⚠️ **건물 scale 을 바꾸면 `placeDistance` 를 반드시 다시 계산할 것.**
> 링 이웃 간격이 지름보다 작으면 `TryFindSpot` 이 고른 칸에 건물이 서로 겹쳐 박히고,
> `Rigidbody2D` 가 서로 밀어내며 튄다. `placeClearRadius`(0.6) 는 **반경**(0.578) 보다
> 살짝 크게 둔다 — 작으면 겹치는 자리를 "비었다"고 판단한다.

#### 곡사포 폭탄 (11차 · CSV 밖 — 프리팹 인스펙터)

| 어디 | 필드 | 값 | 의미 |
|---|---|---|---|
| `Building_Bombard` | `bombSpeed` | 6 | 폭탄 비행 속도(유닛/초). 사거리 끝(3.5)까지 **약 0.6초** |
| `Building_Bombard` | `aoeRadius` | 2 | 폭발 피해 반경 |
| `Proj_Bomb` | `arcHeight` | 1.2 | 포물선 정점 높이. 0 이면 직선 |
| `Proj_Bomb` | `spinSpeed` | 540 | 비행 중 회전(도/초) |
| `Proj_Aoe(Boom)` | `frameRate` | 24 | 16프레임 ÷ 24 = **0.67초** 재생 |
| `Proj_Aoe(Boom)` | `fadeOutPortion` | 0.4 | 마지막 40% 를 알파로 뺀다. 안 빼면 연기 덩어리가 툭 사라진다 |
| `Proj_Aoe(Boom)` | `spriteRadiusAtScaleOne` | 0.4 | **scale 1 일 때 이 그림이 덮는 반경.** 폭발이 `반경 ÷ 0.4` 로 자동 확대된다 |

> **폭탄은 목표를 `Transform` 이 아니라 좌표로 받는다.** 날아가는 동안 적이 죽으면 그
> `Transform` 은 풀에서 재사용돼 엉뚱한 자리로 옮겨 가기 때문이다.
> 대신 **움직이는 적은 빗나간다** — 의도된 거래다.
>
> ⚠️ **`spriteRadiusAtScaleOne` 은 모든 폭발에 공통으로 걸린다.** `Proj_Aoe(Boom)` 하나를
> 곡사포와 `AoeWeapon`(Fireball/Bomb) 이 같이 쓴다. 이 값을 만지면 **양쪽 그림이 같이** 움직인다.
> 한쪽만 키우고 싶으면 `spriteRadiusAtScaleOne` 이 아니라 각자의 반경을 조절할 것.

### `Items.csv` — 레벨업 선택지 / 상점 상품

레벨업 카드와 상점 슬롯은 **모두 이 표에서 뽑힌다.**

- `Category` = `Weapon` / `Building` / `Passive`
- `RefId` = 해당 테이블의 Id (`Weapons.csv` / `Buildings.csv` / `Passives.csv`)
- `Description` 은 UI에 그대로 나오므로 **영문 유지**
- 제거 환급은 `ShopPrice × ShopManager.refundRate(0.5)`

총 **20종**: 무기 5 + 건물 5 + 패시브 10.
가격대는 패시브 5~7G, 무기 8~11G, 건물 10~12G.

**레벨업 카드 3장은 가중치 비복원 추첨으로 뽑는다** (`LevelUpManager.PickCandidates`).
후보 = 미보유 전체 + 보유 중 최대레벨이 아닌 것. 보유 아이템의 가중치는 `OwnedWeight = 2`,
신규는 1 이다. 즉 **육성 쪽으로 기울되 신규도 항상 섞인다.**
예전처럼 보유 업그레이드로 슬롯을 먼저 채우면 아이템 3개만 들고 있어도 카드가 고정된다 (I-34).
"같은 것만 나온다"면 `OwnedWeight` 를 낮추고, "새 것만 나와서 못 큰다"면 올린다.

> 아이템 Id `Speed` 는 이동속도 패시브다. 기존 애셋 이름을 유지하려고 `MoveSpeed` 로 안 바꿨다
> (`RefId` 는 `MoveSpeed` 를 가리킨다).

### `Waves.csv` — 웨이브

**`Spawns` 문법:** `적Id*마리수@스폰간격초` 를 `|` 로 이어 붙인다.

```
Zombie*36@0.8|Wolf*24@0.9|Demon*8@1.5
```

> ⚠️ 항목은 **동시가 아니라 순차**다. Zombie 36마리를 다 뿌린 뒤(28.8초) Wolf로 넘어간다.
> 따라서 `(마리수 × 간격)`의 합이 `SurvivalTime` 을 넘으면 뒤쪽 항목은 등장하지 못한다.

| Id | 사용처 | 클리어 | 스폰 총량 / 소요 | 총 XP | 총 골드 |
|---|---|---|---|---|---|
| Normal1 | 노말 풀 | 60초 생존 | 64마리 / 47초 | 216 | 64 |
| Normal2 | 노말 풀 | 75초 생존 | 72마리 / 60초 | 342 | 114 |
| Normal3 | 노말 풀 | 90초 생존 | 108마리 / 86초 | 608 | 208 |
| Elite1 | 엘리트 풀 | 90초 생존 | 54마리 + 엘리트 Demon×2 | 496 | 124 |
| Elite2 | 엘리트 풀 | 100초 생존 | 64마리 + 엘리트 Ogre×2 | 592 | 172 |
| Boss1 | 보스 고정 | 19킬 (보스 포함) | 18마리 + 보스 Ogre×1 | 384 | 84 |

- `EliteOverride` — Elite 스테이지에서 `Spawns` 를 다 뿌린 뒤 `EliteCount` 마리 추가 소환 (엘리트 배율 적용)
- `BossOverride` — Boss 스테이지에서 `Spawns` 를 다 뿌린 뒤 1마리 소환 (보스 배율 적용)
- Boss1은 `UseKillClear` 로 클리어하되, **적이 안 죽고 사라지는 경우에 대비해 타이머 240초를 안전망으로 켜 두었다.**
  `KillTarget=19` = `Spawns` 총 18마리 + 보스 1

### `Events.csv` — 이벤트 노드

씬의 `EventManager.events` 배열에 **직접 기록**된다 (SO 아님).
`TriggerRandomWave=1` 이면 보상을 준 뒤 노말 웨이브가 하나 시작되고, `0` 이면 곧바로 다음 노드로 넘어간다.

| Title | XP | 골드 | 웨이브 |
|---|---|---|---|
| Abandoned Cache | 0 | 12 | – |
| Ancient Shrine | 40 | 0 | – |
| Wandering Merchant | 0 | 20 | – |
| Ambush | 0 | 0 | ✔ |
| Cursed Offering | 60 | 10 | ✔ |

### `Classes.csv` — 직업(클래스)

**런 시작 시 무기를 주는 유일한 경로다.** 이 표가 비면 플레이어는 맨손으로 시작한다.
`StartingWeapon` 에는 `Weapons.csv` 의 `Id` 를 적는다.

`Bonus*` 는 `PlayerStats.baseStats`(→ `Economy.csv`) **위에 더해지는 차이값**이다. 0 이면 영향 없음.
직업이 기본 스탯 전체를 들고 있으면 원본이 둘로 갈라지므로 일부러 차이값으로만 뒀다.

> ⚠️ `BonusAttackSpeed` 는 **쿨다운 배율**이라 값이 낮을수록 빠르다. 빠르게 하려면 **음수**를 넣어야 한다.

| Id | 시작 무기 | HP | 이속 | 피해 | 공속 | 투사체크기 | 흡수범위 | 치명 | 방어 | XP |
|---|---|---|---|---|---|---|---|---|---|---|
| Warrior | Sword | +30 | −0.3 | – | – | – | – | – | +2 | – |
| Ranger | Bow | −15 | +0.6 | – | −0.1 | – | +0.5 | +0.05 | – | – |
| Mage | Fireball | −25 | – | +0.2 | – | +0.2 | – | – | −1 | +0.1 |

#### 그림 열 세 개 (I-29 로 `WalkSheet` 추가)

| 열 | 쓰이는 곳 | 현재 값 |
|---|---|---|
| `Portrait` | 선택 화면 우측 패널 (UGUI) | `Sprites/Classes/{Id}.png` — 큰 원화 |
| `BodySprite` | 런 시작 시 플레이어 스프라이트 교체 (인게임) | `Sprites/Classes/Walk/{Id}_Walk.png` — 시트를 적으면 **첫 프레임**이 잡힌다 |
| `WalkSheet` | 걷기 프레임 전체 → `CharacterClassData.WalkFrames` | 같은 시트 경로. **비우면 정지 그림 한 장**으로 동작한다(바운스는 그대로) |

> ⚠️ **`WalkFrames` 를 인스펙터에서 직접 채우지 말 것.** `WalkSheet` 에서 잘라 오므로
> 다음 Import 때 통째로 덮어써진다. 임포터는 `LoadAllAssetsAtPath` 로 하위 스프라이트를
> 전부 모아 **이름의 숫자 꼬리표(`frame_0`…`frame_15`) 순으로 정렬**한다 —
> `LoadAssetAtPath` 는 첫 장만 돌려주기 때문이다.

- `Portrait` 를 비우면 선택 화면에 `No Illustration` 자리표시가 다시 뜬다.
- `ModelPrefab` 은 여전히 **빈 칸**이다 (3D 모델용 예비 열, 쓰는 코드 없음).
- 초상화와 인게임 몸통을 다르게 하려면 `BodySprite` 만 다른 경로로 바꾸면 된다.
- Export 시 `WalkSheet` 는 `WalkFrames[0]` 의 경로로 복원된다 (프레임이 전부 같은 `.png` 출신이므로).

> 목록은 `SceneWiring.csv` 의 `GameManager,classes` 로 배선한다. **여기 없는 직업은 화면에 안 뜬다.**
> 메인 메뉴 Start → 직업 선택 화면에서 고른다. 아무것도 안 골랐을 때만 `defaultClassIndex`(0=Warrior).
> `UnlockedByDefault=0` 이면 카드에 잠금 오버레이가 뜨고 선택이 막힌다.
> `UnlockCost` 는 **아직 읽히지 않는다** — 골드로 해금하는 흐름이 없다.

### `Economy.csv` — 씬 컴포넌트의 진행 수치

SO가 아니라 **씬 컴포넌트의 직렬화 필드**를 직접 쓴다.
`Field` 는 SerializedProperty 경로라 `baseStats.MaxHp` 같은 중첩도 된다. `Note` 열은 임포터가 무시한다.

> ⚠️ **C# 필드를 지우면 `Economy.csv` 의 해당 행도 같이 지울 것.** 임포터가 잡을 대상이 없어진다.
> 실제로 `LevelUpManager,rerollCostIncrease` 를 이렇게 정리했다 (I-24).

### `SceneWiring.csv` — 애셋 참조 배열 배선

`LevelUpManager.allItems`, `WaveManager.normalWaves/eliteWaves/bossWave`, `GameManager.classes` 를 CSV로 관리한다.
**아이템·웨이브·직업을 추가하면 여기에도 경로를 추가**해야 게임에 등장한다.

> 씬 안의 오브젝트(`ObjectPool`, `Transform` 등)는 애셋 경로로 지정할 수 없어 여기서 다루지 않는다.
> 그건 여전히 인스펙터에서 드래그해야 한다.

---

## 3. 게임 진행 수치 (전부 임시값)

### 3-1. 골드 (`GameManager` / `EnemyData.CurrencyDrop`)

게임플레이 골드는 전부 `GameManager.GrantGold()` 를 거치며 여기서 `GoldGain` 배율이 곱해진다.

| 수입원 | 값 |
|---|---|
| 적 처치 | Slime/Goblin 1 · Zombie/Wolf 2 · Ogre 6 · Demon 8 |
| 노말 노드 클리어 | 8 |
| 엘리트 노드 클리어 | 20 |
| 보스 노드 클리어 | 60 |
| 이벤트 | 0 / 10 / 12 / 20 |

| 지출처 | 값 |
|---|---|
| 상점 아이템 | 5~12 (`Items.csv` 의 `ShopPrice`) |
| 상점 리롤 | 3 + 1×리롤횟수 (`baseRerollCost` + `rerollCostIncrease`) |
| 레벨업 리롤 | 1 고정 (`rerollCost`) — **레벨업 1회당 1번만** 가능해 비용 증가 열이 없다 |
| 아이템 제거 환급 | 구매가의 50% (수입) |

**10층 런 기준 대략** — 노말 7 + 엘리트 2 + 보스 1 이면
클리어 보상 `8×7 + 20×2 + 60 = 156G`, 처치 보상 `≈ 100×7 + 150×2 + 84 ≈ 1,084G`.
→ **처치 보상이 압도적**이다. 상점에서 매번 다 사고도 남는다.

> 🔴 **미결정:** 지금은 런 중 골드와 메타(영구) 골드가 **`MetaProgression.Currency` 하나**를 공유한다.
> 분리할지 말지 정해야 위 수치를 제대로 잡을 수 있다. → `TODO.md` §2

### 3-2. 경험치 (`ExperienceManager.xpThresholds`)

```
Lv1→2  5      Lv6→7  80
Lv2→3  10     Lv7→8  110
Lv3→4  20     Lv8→9  150
Lv4→5  35     Lv9→10 200
Lv5→6  55     Lv10→11 260
```

Lv10까지 누적 725 XP. Lv11 이상은 `260 + 현재레벨×50` 으로 계산된다 (`ExperienceManager.XpToNext`).
획득 XP에는 `PlayerStats.Final.XpGain` 배율이 곱해진다.

Normal1 하나만 완주해도 216 XP → **약 Lv6**. 초반 레벨업이 매우 빠르다.
`XpDrop` 을 낮추거나 `xpThresholds` 를 올리는 조정이 필요할 수 있다.

### 3-3. 플레이어 기본 스탯 (`PlayerStats.baseStats`)

| 스탯 | 기본값 | 비고 |
|---|---|---|
| MaxHp | 100 | |
| MoveSpeed | 4 | 가장 빠른 적 Wolf(4.2)보다 느리다 — 의도적 |
| Damage | 1 | 무기 피해 **배율** |
| AttackSpeed | 1 | 무기 쿨다운 **배율**. 낮을수록 빠름 |
| ProjectileSize | 1 | |
| PickupRadius | 2 | |
| CritChance | 0.05 | |
| CritMultiplier | 1.5 | |
| Armor | 0 | 받는 피해 감산. 최소 피해 1은 보장 |
| XpGain | 1 | 배율 |
| GoldGain | 1 | 배율 |

> `Final` 계산은 `base + meta + class + passive` 다 (`class` = `Classes.csv` 의 `Bonus*`).
> 여기서 **`meta`·`class`·`passive` 는 전부 "보너스"라 0 에서 시작해야 한다.**
> `StatBlock` 의 필드 초기값은 위 표의 기본값이므로, 보너스 블록을 만들 때는 반드시
> **`StatBlock.Zero()`** 를 써야 한다. `new StatBlock()` 을 쓰면 기본값이 한 번 더 더해져
> 전 스탯이 2배가 된다 (I-21 이 정확히 이 버그였다).

### 3-4. 접촉 피해 — "한 방" + 무적 시간

`ContactDamage` 는 **1회 피해량**이다. 적에 닿으면 그 값이 통째로 들어가고 곧바로
`PlayerStats.invincibleTime`(기본 **0.6초**) 무적이 걸려 연타를 막는다.
계속 붙어 있으면 무적이 풀리는 즉시 다시 맞는다.

> **적 하나가 계속 붙어 있을 때 초당 피해 = `ContactDamage ÷ invincibleTime`**
> Goblin(8) → 13.3 · Zombie(12) → 20 · Demon(20) → 33.3 · Ogre(25) → 41.7
>
> 여러 마리가 동시에 붙어도 **무적은 플레이어 1인분이라 초당 피해가 늘지 않는다.**
> 무적 시간은 "다굴 즉사"를 막는 안전장치이므로, 난이도는 적 수보다 `ContactDamage` 로 조절해야 한다.

방어력은 `Mathf.Max(1, raw - Armor)` 로 적용되므로 **한 방당 최소 1은 항상 들어간다.**

**피격 반응 수치 (`Economy.csv`)**

| Component | Field | 값 | 의미 |
|---|---|---|---|
| `PlayerStats` | `invincibleTime` | 0.6 | 무적 시간(초). **올리면 전체 난이도가 크게 내려간다** |
| `PlayerController` | `knockbackForce` | 9 | 넉백 초기 속도. `MoveSpeed`(4)보다 커야 밀려나는 게 보인다 |
| `PlayerController` | `knockbackTime` | 0.15 | 넉백 동안 이동 입력이 막히는 시간(초). 밀려나는 거리 ≈ 0.7 |
| `PlayerController` | `hitFlashTime` | 0.12 | 맞은 직후 붉게 물드는 시간(초) |
| `PlayerController` | `blinkInterval` | 0.07 | 무적 중 깜빡임 주기(초) |
| `PlayerController` | `shakeMagnitude` | 0.18 | 피격 시 카메라 흔들림 강도 |

> `hitColor` 는 `Color` 타입이라 CSV가 지원하지 않는다. 바꾸려면 인스펙터에서.

### 3-5. 메타 진행

| 항목 | 값 |
|---|---|
| 캐릭터 해금 | 30G |
| 스킨 해금 | 15G |
| 영구 강화(`UpgradeDefinition`) | **애셋 0개** — 미구현 |

---

## 4. 흔한 실수

| 증상 | 원인 |
|---|---|
| 새 아이템이 레벨업/상점에 안 나옴 | `SceneWiring.csv` 의 `LevelUpManager.allItems` 에 경로를 안 넣었다 |
| 새 웨이브가 안 뽑힘 | `SceneWiring.csv` 의 `normalWaves`/`eliteWaves` 에 안 넣었다 |
| 시작 무기가 없음 | `Classes.csv` 의 `StartingWeapon` 이 비었거나 `SceneWiring.csv` 의 `GameManager,classes` 가 비었다 |
| 직업을 만들었는데 선택 화면에 안 뜸 | `SceneWiring.csv` 의 `GameManager,classes` 에 `.asset` 경로를 추가하지 않았다 |
| 직업 카드가 회색으로 잠겨 있음 | `Classes.csv` 의 `UnlockedByDefault` 가 0. 해금 흐름이 아직 없으므로 1로 둘 것 |
| 웨이브 뒤쪽 적이 안 나옴 | `Spawns` 는 순차 소환이다. `(마리수×간격)` 합 > `SurvivalTime` |
| 값을 고쳤는데 반영이 안 됨 | Import 를 안 돌렸거나, 인스펙터에서 고친 뒤 Import 로 덮어써졌다 |
| Import 후 참조가 비어 있음 | 콘솔의 `! ... 를 찾을 수 없음` 확인. `RefId`/`Spawns` 의 Id 오타 |
| 새로 만든 `.asset` 이 "Missing script" | ScriptableObject 클래스는 **동명 파일**에 있어야 한다 (`WaveData` 가 이 문제였다) |
| 인게임 스탯이 CSV 값의 2배 | 보너스 `StatBlock` 을 `new StatBlock()` 으로 만들었다. **`StatBlock.Zero()`** 를 쓸 것 |
