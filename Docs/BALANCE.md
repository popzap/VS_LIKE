# VS_LIKE — 밸런스 데이터 가이드

> **작성:** 2026-08-26 · **최종 갱신:** 2026-08-29 (24차 — `Classes.csv` 에 `Tier` 열 / I-61)
> 이 문서는 **수치를 어디서 어떻게 고치는가**를 설명한다.
> 완료 이력은 [`SETUP_STATUS.md`](SETUP_STATUS.md), 남은 작업은 [`TODO.md`](TODO.md).
>
> 📌 **"어떤 수치를 왜 고쳐야 하는가"는 [`TUNING.md`](TUNING.md) 에 있다.**
> 여기는 **방법**, 거기는 **판단 목록**이다. 플레이하며 조절할 때는 그쪽을 펴 놓고 이쪽을 참조한다.

---

## 0. 요약 — 수치를 고치고 싶으면

1. `Assets/Game/Balance/*.csv` 를 엑셀·메모장 아무거나로 연다
2. 값을 고친다
3. Unity 메뉴 **`Game/Balance/Import CSV -> ScriptableObjects`** 실행
4. 콘솔에 `[BalanceImporter] Import 완료` 와 각 테이블 행 수가 찍히면 반영 완료

**인스펙터에서 직접 고치지 말 것.** 다음 Import 때 CSV 값으로 덮어써진다.
부득이 인스펙터에서 고쳤다면 **`Game/Balance/Export ScriptableObjects -> CSV`** 로 CSV에 되돌린 뒤 커밋한다.

> ⚠️ **딱 하나 예외가 있다 — 오디오 음량.**
> `Assets/Game/Audio/AudioLibrary.asset` 은 **CSV 임포터가 건드리지 않는다.**
> 소리 크기·피치는 여기서 **인스펙터로 직접** 고치는 게 맞다 (→ §2 마지막).
>
> 🔤 **CSV 에 문자열을 쓸 때**: 폰트가 **Static** 이라 문자표에 없는 글자는 **빈칸으로 나온다** (I-60).
> 지금 굽혀 있는 건 **ASCII 전부 + 기호 20개**뿐이다. 한글·이모지는 없다 (→ §2 마지막).

---

## 1. 파이프라인 구조

```
Assets/Game/Balance/*.csv        ← 원본(authoring source). 사람이 고치는 곳
        │
        │  Game/Balance/Import CSV -> ScriptableObjects
        ▼
Assets/Game/{EnemyData,WeaponData,BuildingData,PassiveData,ItemData,WaveData,ClassData,EvolutionData}/*.asset
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
| Wolf | `Sprites/Enemies/Wolf` | 0.85 | 40 | 4.2 | 10 | 0 | 6 | 2 | 빠르고 약함 → **돌진형** |
| Demon | `Sprites/Enemies/Demon` | 1.25 | 160 | 2.8 | 20 | 4 | 22 | 8 | 후반 위협 → **원거리형** |
| Ogre | `Sprites/Enemies/Ogre` | 1.60 | 220 | 1.2 | 25 | 6 | 18 | 6 | 보스 소재 |

`Elite*Mult` / `Boss*Mult` 는 **배율**이다. 엘리트/보스 스테이지에서 같은 적을 재활용할 때 곱해진다.

#### `WalkSheet` 열 — 걷기 애니메이션 (I-58)

`Sprites/Enemies/Walk/{Id}_Walk.png` (4×4 = 16프레임)을 적으면 `EnemyData.WalkFrames` 로 잘려
들어가고 `EnemyVisual` 이 프레임을 넘긴다. **비우면 `Sprite` 한 장으로 동작한다** —
셰이더 바운스는 어느 쪽이든 그대로 걸리므로 시트가 없는 적을 새로 넣어도 어색하지 않다.

| | |
|---|---|
| PPU | **512 필수.** 프레임이 256px 이므로 512 라야 `Sprite`(0.5유닛)와 크기가 같다 |
| 걸음 속도 | `EnemyData.MoveSpeed`(등급 배율 포함)를 1배로 보고 실제 속도에 비례. 0.5~2.5배로 제한 |
| 정지 시 | `frame_0` 로 고정. Charger 의 **예고·경직 구간**이 여기 걸린다 |
| 개체 차이 | 시작 프레임을 무작위로 어긋나게 준다 — 안 그러면 무리가 발을 맞춰 한 몸으로 보인다 |

> ⚠️ **생성 직후 PPU 기본값은 텍스처 크기와 같은 1024 다.** 그대로 두면 프레임이 0.25유닛이라
> **애니메이션이 켜지는 순간 적이 절반으로 줄어든다.** 20차에 실제로 1024 로 커밋했다가 21차에 잡았다.

#### `AI` 열 — 행동 방식 (I-47)

`AI` 열 하나로 행동이 바뀐다. **프리팹은 여전히 `Enemy_Goblin.prefab` 하나를 공유**한다
(컴포넌트 타입은 런타임에 못 바꾸므로 상속이 아니라 데이터로 갈랐다 → `SETUP_STATUS.md` 2-18).

| 값 | 행동 | 쓰는 열 |
|---|---|---|
| `Chaser` | 플레이어에게 직진. **기본값** (빈 칸이면 이것) | — |
| `Ranged` | `PreferredRange` 를 유지하며 투사체 발사 | `ProjectilePrefab` `PreferredRange` `AttackCooldown` `ProjectileSpeed` `ProjectileDamage` |
| `Charger` | 멈춰 조준(예고) → 고속 돌진 → 경직 | `ChargeRange` `ChargeWindup` `ChargeSpeedMult` `ChargeDuration` `ChargeRecover` `ChargeCooldown` |

대소문자는 구분하지 않는다(`ranged` 도 된다). **이름이 아예 틀리면** 콘솔에 경고가 뜨고
기존 값을 유지한다 — 조용히 `Chaser` 가 되지 않는다.

**원거리 (`Ranged`) 수치**

| 열 | Demon | 의미 |
|---|---:|---|
| `PreferredRange` | 7 | 이 거리를 유지한다. 더 멀면 접근, **70%(4.9) 보다 가까우면 후퇴**, 사이에서는 옆걸음 |
| `AttackCooldown` | 2.5 | 발사 간격(초) |
| `ProjectileSpeed` | 6 | 플레이어 이동속도(4)보다 빨라야 맞는다 |
| `ProjectileDamage` | 12 | **0 이면 `ContactDamage` 를 그대로 쓴다.** 접촉(20)보다 낮게 잡았다 — 원거리는 안전하니까 |
| `ProjectilePrefab` | `Prefabs/Proj_EnemyBolt` | 비어 있으면 **발사 자체를 안 한다** (그냥 거리만 유지) |

> 적탄 **수명은 CSV 열이 없다.** `PreferredRange × 1.6 ÷ ProjectileSpeed` 로 자동 계산된다
> (사거리의 1.6배까지 날아가고 사라진다). 사거리를 늘리면 사정거리도 같이 늘어난다.
>
> ⚠️ 적탄은 **콜라이더가 아니라 거리**(`HitRadius 0.45`)로 맞힌다. Projectile 레이어는
> 플레이어를 때리라고 만든 게 아니라서, 레이어 행렬을 건드리면 기존 무기 판정까지 흔들린다.

**돌진 (`Charger`) 수치**

| 열 | Wolf | 의미 |
|---|---:|---|
| `ChargeRange` | 6 | 이 안에 들어오면 돌진 시작 |
| `ChargeWindup` | 0.45 | 멈춰서 조준하는 **예고** 시간. 이게 없으면 그냥 빠른 적일 뿐 **피할 수가 없다** |
| `ChargeSpeedMult` | 3.0 | `MoveSpeed` 배율 (4.2 → 12.6) |
| `ChargeDuration` | 0.4 | 돌진 지속 |
| `ChargeRecover` | 0.6 | 돌진 후 경직. **플레이어의 반격 기회** |
| `ChargeCooldown` | 3.0 | 다음 돌진까지 |

> **돌진 거리 = `MoveSpeed × ChargeSpeedMult × ChargeDuration`** = 4.2 × 3.0 × 0.4 ≈ **5유닛**.
> 6유닛(≈화면 1/3)을 넘으면 예고를 봐도 사실상 회피 불가라 3.5 → **3.0** 으로 낮췄다.
> 반대로 예고를 0.3초 아래로 내리면 반응할 시간이 없다.

**무리 분리(겹침 방지)는 CSV 열이 아니다.** `EnemyBase` 의 상수이고 **전 종류에 적용**된다
(반경 `0.85 × localScale` · 가중치 `0.9` · **4스텝마다** 갱신 · 이웃 버퍼 12).
가중치가 1을 넘으면 분리가 추격을 이겨 **적이 다가오지 않는다.**

**빈 칸은 "기존 값 유지"이지 0 이 아니다.** `Chaser` 행의 돌진/원거리 열을 비워 두는 건
그래서 안전하다 — 애초에 그 행동을 안 타므로 값이 무엇이든 상관없다.

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
| 직업 걷기 프레임 | `Sprites/Classes/Walk/*_Walk.png` (7종) | 256 | 256 | 1.00 | 1.0 | **1.00** |
| 적 걷기 프레임 | `Sprites/Enemies/Walk/*_Walk.png` (6종) | 256 | **512** | 0.50 | 1.0 | **0.50** (I-58) |
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
| Fireball | `AoeWeapon` | `Weapon_Aoe` + **`Proj_Fireball`** → `Proj_Aoe(Boom)` | 불덩이가 **직선으로 날아가** 착탄 지점에서 폭발 |
| Bomb | `AoeWeapon` | `Weapon_Aoe` + `Proj_Aoe(Boom)` | 쿨 길고 피해 큼. **즉시 폭발** |

> `AoeWeapon` 은 `ProjectileCount` 를 **쓰지 않는다.**
> 폭발 반경은 `Weapon_Aoe.prefab` 의 `explosionRadius`(**2**) × `ProjectileSize` 다.
>
> #### `TravelPrefab` — 날아가는 몸체 (16차 / I-52)
>
> AoE 무기 전용 열이다. **폭발(`ProjectilePrefab`) 이 터지기 전에 목표까지 날아가는 몸체**를 지정한다.
>
> | `TravelPrefab` | `ProjectileSpeed` | 결과 |
> |---|---|---|
> | 비어 있음 | (무시) | 목표 지점에서 **즉시 폭발** — Bomb |
> | 프리팹 지정 | `> 0` | 몸체가 날아간 뒤 **착탄 지점에서 폭발** — Fireball (속도 **11**) |
> | 프리팹 지정 | `0` | 즉시 폭발로 **되돌아간다** (안 날아가는 투사체가 생기는 사고를 막는 안전장치) |
>
> 지정하는 프리팹에는 **`BombProjectile` 컴포넌트가 있어야 한다.** 없으면 즉시 폭발로 떨어진다.
> Fireball 과 Bomb 은 **`Weapon_Aoe.prefab` 하나를 공유**하므로, 이 갈림길은 프리팹이 아니라
> **CSV(데이터)에서** 갈라야 한다. 프리팹 필드로 만들면 폭탄까지 같이 날아간다.
>
> `Proj_Fireball` 은 곡사포 폭탄(`Proj_Bomb`)을 복제해 `arcHeight = 0` / `spinSpeed = 0`
> 으로 맞춘 것이다 — 마법 투사체는 포물선이나 회전 없이 **곧게** 날아가는 편이 읽기 쉽다.
> ⚠️ 속도 11 은 **감으로 넣은 자리표시값**이다 (사거리 12 기준 약 1.1초).
>
> ⚠️ 날아가는 동안 적이 움직이면 **빗나간다.** 목표를 Transform 이 아니라 **좌표로 굳혀** 쏘기
> 때문인데, 이건 의도된 것이다 — 적이 죽으면 그 Transform 은 풀에서 재사용돼 엉뚱한 자리로 간다.
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

총 **25종**: 무기 5 + 건물 5 + 패시브 10 + **진화 결과 5**.
가격대는 패시브 5~7G, 무기 8~11G, 건물 10~12G.

> ⚠️ **진화 결과 5종(Excalibur/Windforce/Devastator/Sentinel/Doomsday)은 이 표에 있지만
> `SceneWiring.csv` 의 `LevelUpManager,allItems` 에는 없다.** 넣으면 레벨업 3택과 상점에
> 그냥 뽑혀 나와 진화라는 절차 자체가 무의미해진다. `MaxLevel = 1`, `ShopPrice = 0`(미사용).
> 새 진화를 추가할 때도 **`allItems` 에는 절대 넣지 말 것.**

**레벨업 카드 3장은 가중치 비복원 추첨으로 뽑는다** (`LevelUpManager.PickCandidates`).
후보 = 미보유 전체 + 보유 중 최대레벨이 아닌 것. 보유 아이템의 가중치는 `OwnedWeight = 2`,
신규는 1 이다. 즉 **육성 쪽으로 기울되 신규도 항상 섞인다.**
예전처럼 보유 업그레이드로 슬롯을 먼저 채우면 아이템 3개만 들고 있어도 카드가 고정된다 (I-34).
"같은 것만 나온다"면 `OwnedWeight` 를 낮추고, "새 것만 나와서 못 큰다"면 올린다.

> 아이템 Id `Speed` 는 이동속도 패시브다. 기존 애셋 이름을 유지하려고 `MoveSpeed` 로 안 바꿨다
> (`RefId` 는 `MoveSpeed` 를 가리킨다).

### `Evolutions.csv` — 무기 진화 레시피 (17차 / I-54)

한 행이 레시피 하나다. **재료 카테고리를 섞을 수 있다** — 패시브+무기, 무기+무기, **건물+무기**
셋 다 같은 행 모양으로 표현된다. `ItemData` 가 세 종류를 모두 담는 단일 타입이라 가능한 구조다.

| 열 | 뜻 |
|---|---|
| `Id` | 애셋 파일명 (`Assets/Game/EvolutionData/<Id>.asset`) |
| `EvolutionName` / `Description` | 표시용 |
| `Ingredients` | **`Items.csv` 의 Id** 를 `|` 로 나열. 개수 제한 없음 |
| `RequiredLevels` | 각 재료의 최소 레벨. **`Ingredients` 와 같은 순서·같은 개수** |
| `ResultItem` | `Items.csv` 의 Id. 진화 결과 아이템 |

현재 3종:

| Id | 재료 | 조합 유형 | 전달 경로 |
|---|---|---|---|
| Excalibur | Sword Lv5 + Damage Lv3 | 무기 + **패시브** | 보물상자 |
| Windforce | Bow Lv5 + CritChance Lv3 | 무기 + **패시브** | 보물상자 |
| Devastator | Gun Lv5 + Fireball Lv5 | 무기 + **무기** | 보물상자 |

> ⚠️ **19차(I-56)에 무기 + 건물 조합이 여기서 빠졌다.** 이제 그 조합은 무기가 아니라
> **직업**이 되며 [`ClassEvolutions.csv`](#classevolutionscsv--직업-승급-레시피-19차--i-56) 가 담당한다.
> 그래서 `Evolutions.csv` 에는 **현재 제단 레시피가 하나도 없다** — 아래 "전달 경로" 절의
> 건물 분기는 구조로 남아 있지만 지금은 아무 행도 타지 않는다.

#### 전달 경로는 열이 아니라 재료가 정한다

`DeliveryKind` 같은 플래그 열은 **없다.** `EvolutionData.IsFinalEvolution` 이
"재료에 건물이 섞여 있는가"를 보고 스스로 판단한다.

- 건물이 **없으면** → 보물상자를 열 때 **확정으로** 그 카드 하나만 뜬다
  (`ExperienceManager.GrantChestReward` 가 진화를 평범한 카드보다 **먼저** 준다)
- 건물이 **있으면** → 그 건물이 **제단**이다. 필드에 실제로 세워 둔 건물 반경
  `EvolutionManager.altarRadius`(**2.2**) 안에서 **E** 를 눌러야 완성된다

> 왜 열을 안 뒀나 — 열을 두면 "건물이 재료인데 상자 경로"라는 **데이터로만 깨질 수 있는 상태**가
> 생긴다. 제단이 될 건물이 없는데 제단에서 기다리게 되는 것이다. 재료에서 유도하면 그 상태가
> 애초에 표현 불가능하다. 제단 전용 프리팹도 필요 없다 — 이미 세워 둔 건물이 그대로 제단이다.

#### 소모 규칙 — 무기만 사라진다

`EvolutionManager.Evolve()` 는 **`Category == Weapon` 인 재료만** 회수한다.
패시브와 건물은 레벨 그대로 남는다.

- **건물을 남기는 이유**: "제단 앞에서 눌렀더니 제단이 증발"하는 꼴이 된다.
  건물은 필드에 실제로 서 있는 물건이라 사라지면 화면에서 눈에 띄게 어색하다
- **패시브를 남기는 이유**: 패시브는 스탯 한 줄이라 회수해도 플레이어가 뭘 잃었는지 못 느끼고,
  체감으로는 그냥 약해지기만 한다
- **무기를 먼저 비우는 순서에는 이유가 있다** — 무기 슬롯이 6칸이라 꽉 찬 상태에서
  결과를 먼저 넣으면 `WeaponManager` 가 **조용히 거절**한다 (경고 로그만 남는다)

#### 진화 무기 수치 — 전부 자리표시값

`Weapons.csv` 아래쪽 "진화 무기" 블록이다. **레벨이 없다** — 배열은 값 1개뿐이고
`Items.csv` 의 `MaxLevel` 도 1 이다.

기준은 **재료 무기의 Lv5 대비 약 1.7배**. 근거가 있는 값이 아니라 "확실히 세다고 느껴질 만큼"으로
잡은 감이다. 실사격 후 조정할 것 (→ [`TODO.md`](TODO.md) §3).

| Id | Damage | Cooldown | ProjectileCount | Range | 성격 |
|---|---:|---:|---:|---:|---|
| Excalibur | 70 | 0.55 | 5 | 16 | 광범위 다중 타격 |
| Windforce | 55 | 0.40 | 6 | 20 | 최다 투사체 + 최장 사거리 |
| Devastator | 34 | 0.15 | 4 | 17 | 최속 연사 |

> **20차(I-57)에 전용 아이콘 3장을 넣었다** (`Sprites/Weapons/{Excalibur,Windforce,Devastator}.png`,
> PPU 512 / `maxTextureSize` 512 — `Sword.png` 와 같은 규격). 그전에는 재료 무기의 아이콘을
> 그대로 써서 레벨업 카드에서 `Sword` 와 `Excalibur` 가 같은 그림이었다.
> `WeaponPrefab` 은 여전히 기존 `Weapon_Sword` / `Weapon_Aoe` 를 재사용한다.

#### 임포트 순서 주의

`Evolutions.csv` 는 **Items 다음의 별도 단계**에서 임포트된다.
`LoadById` 가 쓰는 `AssetDatabase.LoadAssetAtPath` 는 **같은 `StartAssetEditing` 블록 안에서
방금 만든 애셋을 못 본다.** 그래서 Items 단계가 `SaveAssets()+Refresh()` 로 끝난 뒤에야
Evolutions 가 돈다. 새로 아이템을 참조하는 표를 추가할 때도 같은 자리에 넣을 것.

`Ingredients` 와 `RequiredLevels` 의 **개수가 어긋나면 임포트 로그에 `!` 경고**가 뜬다.
경고를 무시하면 `GetRequiredLevel` 이 조용히 1 로 떨어져 **아무 때나 진화**하게 된다.

### `Waves.csv` — 웨이브

**`Spawns` 문법:** `적Id*마리수@스폰간격초:시작초-종료초` 를 `|` 로 이어 붙인다.

```
Slime*30@2:0-60|Goblin*36@1.4:12-60
```

> ✅ **I-46 으로 순차 → 병렬이 됐다.** 항목마다 코루틴이 하나씩 돌기 때문에 여러 종이 섞여 나온다.
> 예전에는 앞 항목을 다 뿌려야 다음이 시작돼, `(마리수 × 간격)` 합이 `SurvivalTime` 을 넘으면
> **뒤쪽 적이 아예 등장하지 못했다** (`Normal3` 의 Goblin 40마리).

- 시간창(`:시작-종료`)을 **생략하면 웨이브 내내** 소환한다
- **종료초를 넘기면 남은 마리수를 버린다.** 이게 난이도 곡선을 만든다 —
  초반 잡몹이 중반에 끊기고 후반 강적으로 교대한다
- 따라서 `마리수 × 간격` 이 시간창 길이보다 크면 **실제 소환 수는 그보다 적다. 의도된 것이다**

**`MaxAlive` — 동시 생존 상한**

병렬 소환으로 바꾸면서 **필수**가 됐다. 없으면 여러 항목이 동시에 쏟아져 프레임이 무너진다.

- 상한이 차면 소환은 **취소가 아니라 대기**한다 (자리가 나면 마저 나온다)
- **엘리트/보스 오버라이드는 상한을 무시한다.** 안 그러면 킬 클리어 웨이브가 영영 안 끝난다
- 소환 반경의 **1.9배**보다 멀어진 적은 **죽이지 않고** 플레이어 주변으로 재배치한다
  (지우면 경험치·골드가 증발한다). 단 **보스는 제외** — 등 뒤에 갑자기 나타나면 반칙이다

**`EliteTime` / `BossTime`** — 오버라이드를 소환할 시각(웨이브 시작 후 초).
엘리트가 여러 마리면 `EliteTime` 부터 1.5초 간격으로 이어 나온다.
(예전에는 "`Spawns` 를 다 뿌린 뒤"였는데, 병렬이 되면서 그 시점이 사라졌다)

| Id | 사용처 | 클리어 | `MaxAlive` | 소환 구성 |
|---|---|---|---:|---|
| Normal1 | 노말 풀 | 60초 생존 | 60 | Slime 30 (0~60s) · Goblin 36 (12~60s) |
| Normal2 | 노말 풀 | 75초 생존 | 80 | Goblin 30 (0~62s) · Zombie 36 (10~72s) · Wolf 16 (35~75s) |
| Normal3 | 노말 풀 | 90초 생존 | 110 | Zombie 36 · Wolf 24 (15~) · Demon 10 (30~) · Goblin 34 (40~) |
| Elite1 | 엘리트 풀 | 90초 생존 | 110 | Wolf 24 (0~55s) · Zombie 30 · Demon 12 (25~) + 엘리트 Demon×2 @45s |
| Elite2 | 엘리트 풀 | 100초 생존 | 130 | Zombie 32 · Demon 16 (12~) · Wolf 24 (30~) + 엘리트 Ogre×2 @50s |
| Boss1 | 보스 고정 | 19킬 (보스 포함) | 90 | Zombie 12 (0~50s) · Demon 6 (5~53s) + 보스 Ogre @3s |

> 시간창 덕분에 **웨이브 안에서 적 구성이 바뀐다** — Normal2 는 Goblin 으로 시작해
> Zombie 가 섞이고 마지막 40초에 Wolf 가 들어온다.

**⚠️ 시간창은 `KillTarget` 을 깨뜨릴 수 있다.**
시간창이나 `MaxAlive` 때문에 소환 수가 줄면 킬 목표를 **영영 못 채운다.**
그래서 Boss1 만 시간창 안에 마리수가 다 들어가도록 맞췄다 —
Zombie `12×4=48 < 50`, Demon `6×8=48 < 53−5`. `KillTarget=19` = 보스 1 + 잡몹 18.
그래도 막히지 않게 **타이머 240초를 안전망으로** 켜 둔다.

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
| Sentinel *(T2)* | – | +20 | – | +0.15 | −0.05 | +0.1 | – | +0.03 | +1 | – |
| Doomlord *(T2)* | – | +20 | −0.2 | +0.25 | – | +0.35 | – | – | +1 | – |
| Warden *(T2)* | – | +50 | −0.2 | +0.1 | – | – | +0.5 | – | +3 | – |
| Aegis *(T3)* | – | +60 | – | +0.15 | −0.05 | – | – | – | +3 | – |

> **T2/T3 는 승급 직업이다.** 조건은 [`ClassEvolutions.csv`](#classevolutionscsv--직업-승급-레시피-19차--i-56) 에 있고,
> 여기 행은 "승급하면 무엇이 **더해지나**"만 적는다.
> `StartingWeapon` 은 비워 둔다 — 승급은 무기를 새로 주지 않는다.
>
> ⚠️ 승급 직업을 `SceneWiring.csv` 의 `GameManager,classes` 에 넣지 말 것. 시작 선택 화면에 떠 버린다.
> (`Tier` 로도 걸러지지만 경고가 뜬다. `Tier` 는 실수를 잡는 안전망이지 배선을 대신하지 않는다.)

#### `Tier` 열 — 승급 단계 (24차 / I-61)

`1` = 시작 직업, `2` 이상 = 승급으로만 도달하는 직업. **한 열이 두 가지를 동시에 정한다.**

| 하는 일 | 어디서 | 무슨 뜻인가 |
|---|---|---|
| ① **승급 배타** | `EvolutionManager.IsClassSatisfied` → `PlayerStats.HasTier` | 같은 티어는 한 런에 **하나만**. Sentinel 을 타면 Doomlord·Warden 이 잠긴다 |
| ② **선택 화면 차단** | `ClassSelectUI` → `CharacterClassData.IsPromotionOnly` | `Tier > 1` 이면 시작 직업 목록에서 자동으로 빠진다 |

> **왜 배타인가** — 이래야 직업이 **"빌드가 도달하는 지점"** 이 된다. 다 모을 수 있으면
> 갈래가 사라지고, 사슬 보너스가 전부 누적돼 후반이 일방적으로 세진다.
> **배타가 들어간 뒤로 T2 개수 = 빌드 갈래 수**다 (지금 3개).

> ⚠️ **새 T2 를 추가할 때 `Tier` 를 안 적으면 기본값이 `1`** 이라
> **시작 화면에 뜨고 배타도 안 걸린다.** 가장 흔한 사고 지점이다.

> ℹ️ **왜 `ClassEvolutions.csv` 가 아니라 여기인가** — 배타 판정은 "내 사슬에 같은 티어가 있나"를
> 묻는데, 사슬(`PlayerStats.ClassChain`)이 들고 있는 건 레시피가 아니라 이 직업들이다.
> 게다가 **T1 은 자기를 만든 레시피가 아예 없어** 레시피 쪽에 두면 역추적이 성립하지 않는다.
>
> ℹ️ `IsPromotionOnly` 는 **CSV 열이 아니라 `Tier > 1` 파생 프로퍼티**다. 같은 사실을 말하는 열이
> 둘이면 어긋날 수 있고, 어긋나면 T2 로 런이 시작된다.

#### `Bonus*Slots` — 소지 상한 (18차 / I-55, 19차에 개명)

**직업을 가르는 두 번째 축이다.** `Bonus*` 가 "얼마나 센가"라면 이쪽은 "무엇을 할 수 있는가"다.
스탯 차이는 몇 퍼센트라 체감이 흐리지만, 무기를 3종밖에 못 드는 것과 5종을 다 드는 것은
런 전체의 모양이 달라진다.

> ⚠️ **세는 단위는 "아이템 종류 수"이지 레벨이 아니다.** `Sword Lv5` 도 **1칸**이다.
> **이미 보유 중인 것의 레벨업은 상한과 무관하게 계속 뜬다** — 상한은 "칸을 새로 차지하는가"만 막는다.
>
> ⚠️ **`BonusBuildingSlots` 는 건물 "종류" 수다.** "같은 건물을 몇 채 세우나"는
> `Buildings.csv` 의 `MaxCount` 이며 완전히 다른 값이다.
>
> ⚠️ **총량이 아니라 더하는 값이다** (19차에 `Max*Slots` → `Bonus*Slots` 로 개명한 이유).
> 기본 상한이 0 이라 T1 에서는 "총량 == 더하는 값"이지만, 승급하면 **직업 사슬 전체가 합산**된다.
> `Warrior`(건물 5) → `Warden`(+2) = **7칸**. 승급 행에 "총 7"을 적으면 12칸이 된다.

| Id | 무기 | 패시브 | 건물 | 성격 |
|---|---:|---:|---:|---|
| Warrior | **3** | 5 | **5** | 무기는 적게 들고 **건물을 전부** 세운다. 진지 구축형 |
| Ranger | **5** | 4 | **2** | **무기 전종**을 들지만 건물은 거의 못 세운다. 순수 화력형 |
| Mage | 3 | **8** | 3 | 패시브로 큰다. 무기·건물은 좁고 스탯을 두껍게 쌓는다 |
| Sentinel *(T2)* | +0 | +1 | +1 | 포탑 운용. 진지를 한 겹 더 두른다 |
| Doomlord *(T2)* | +1 | +1 | +0 | 화력 집중. 건물은 안 늘고 무기가 한 칸 는다 |
| Warden *(T2)* | +0 | +1 | +2 | 정착지 방위. 건물 폭이 가장 크게 는다 |
| Aegis *(T3)* | +1 | +1 | +1 | 전 축 확장 |

> **현재 콘텐츠 총량은 무기 5 · 건물 5 · 패시브 10 이다.** 합이 그 이상이면 제한이 없는 것과 같다.
> Warrior 의 건물 5 와 Ranger 의 무기 5 는 **의도적으로 상한 없음**이다 —
> "이 직업은 이 축이 자유롭다"를 표현한 것이지 실수가 아니다.
> 승급 보너스도 T1 에 따라 넘칠 수 있다 (Warrior + Warden = 건물 7 > 5). 손해는 아니지만
> **그 조합에서는 아무 효과도 없다** — 승급 수치는 아직 실사격 전 자리표시값이다 (→ [`TODO.md`](TODO.md) §3).
>
> 무기 진화는 재료를 **먼저 소모**하고 결과를 주므로 무기 칸이 꽉 차 있어도 성사된다
> (→ `Evolutions.csv` 의 소모 규칙). 순서를 뒤집으면 꽉 찬 상태에서 진화가 조용히 실패한다.
> **직업 승급은 아무것도 소모하지 않고 칸도 먹지 않는다** — 결과가 아이템이 아니기 때문이다.

**상한이 걸리면 어떻게 되나** — 그 카테고리의 **신규** 아이템이 레벨업 카드와 상점 진열에서
아예 빠진다. 두 화면 모두 `LevelUpManager.PickCandidates` 하나를 지나므로 거기서 같이 막힌다.
"카드가 3장 안 뜬다"면 상한을 의심할 것 — 후보 풀이 3개보다 적어진 것이다.

> ⚠️ 값을 0 으로 두지 말 것. Ranger 의 건물 2 처럼 **작게** 잡는 건 개성이지만, 0 이면
> 그 카테고리가 통째로 사라져 아이템 풀이 얇아지고 카드가 반복된다.

`SlotLimit` 은 **직업 사슬이 비면 `int.MaxValue`** 를 돌려준다. 실제 런은 반드시 직업을 받으므로
이 경로는 직업 없이 씬을 직접 재생할 때만 탄다 — 거기서 상한을 걸면 원인 모를 "카드가 안 뜬다"가 된다.

### `ClassEvolutions.csv` — 직업 승급 레시피 (19차 / I-56)

**조합을 결과별로 갈라 둔 표다.**

| 조합 | 결과 | 전달 경로 | 표 |
|---|---|---|---|
| 무기 + 무기 · 무기 + 패시브 | **진화 무기** | 보물상자 | `Evolutions.csv` |
| 무기 + **건물** | **직업(T2·T3…)** | **그 건물 앞 `E`** | **`ClassEvolutions.csv`** |

| 열 | 뜻 |
|---|---|
| `Id` | 애셋 파일명 (`Assets/Game/ClassEvolutionData/<Id>.asset`) |
| `EvolutionName` / `Description` | 표시용 |
| `FromClass` | `Classes.csv` 의 Id. **비우면 아무 직업에서나** 승급할 수 있다 (T1→T2 는 보통 비운다) |
| `Ingredients` | `Items.csv` 의 Id 를 `|` 로 나열. **건물이 하나는 있어야 한다** |
| `RequiredLevels` | 각 재료의 최소 레벨. `Ingredients` 와 같은 순서·같은 개수 |
| `ResultClass` | `Classes.csv` 의 Id. **사슬 끝에 덧붙는다** (교체가 아니다) |

현재 4종:

| Id | From | 재료 | 제단 | 결과 |
|---|---|---|---|---|
| Sentinel | *(아무 직업)* | Gun Lv5 + **Turret Lv3** | Turret | Sentinel (T2) |
| Doomlord | *(아무 직업)* | Bomb Lv5 + **Bombard Lv3** | Bombard | Doomlord (T2) |
| Warden | *(아무 직업)* | Sword Lv5 + **Village Lv3** | Village | Warden (T2) |
| Aegis | **Sentinel** | Turret Lv5 + Armor Lv3 | Turret | Aegis (T3) |

#### 왜 사슬인가 — 승급은 갈아타기가 아니라 쌓기다

`PlayerStats` 가 직업을 **하나가 아니라 목록**(`ClassChain`)으로 들고, 스탯과 소지 상한을
**사슬 전체에 대해 합산**한다. 하나만 들고 교체하면 T2 로 올라가는 순간 T1 의 보너스와 칸이
사라져 **승급이 손해가 되는 경우**가 생긴다 (Warrior 의 건물 5칸을 잃는 식).

```
Warrior            maxHp 130  armor 2  slots W3/P5/B5
  ↓ +Sentinel
Warrior+Sentinel   maxHp 150  armor 3  slots W3/P6/B6
  ↓ +Aegis
… +Aegis           maxHp 210  armor 6  slots W4/P7/B7
```

#### 세 가지 규칙

- **재료를 소모하지 않는다.** 무기 진화는 무기를 먹고 더 센 무기를 돌려주니 교환이 성립하지만,
  승급은 돌려주는 게 직업이라 재료까지 가져가면 순수한 손해가 된다
- **체력을 가득 채우지 않는다.** 승급은 필드에서 전투 중에 일어나므로 완전 회복이 붙으면
  "위험할 때 승급을 아껴 두는" 이상한 운용이 생긴다. 대신 **늘어난 최대치만큼은 그대로 얹는다** —
  안 그러면 최대 체력이 올라도 현재 체력이 그대로라 승급이 눈에 안 띈다
- **`FromClass` 는 "현재 직업"이 아니라 "거쳐 왔는가"를 본다.** 끝(현재 직업)만 보면
  같은 T2 에서 T3 두 갈래가 갈릴 때 하나를 타는 순간 다른 쪽이 영영 막힌다
- **같은 티어는 하나만 (24차 / I-61).** T2 를 하나 타면 다른 T2 레시피는 전부 후보에서 빠진다.
  판정은 `Classes.csv` 의 [`Tier`](#tier-열--승급-단계-24차--i-61) 열이 하며, 이 레시피 표에는 티어가 없다

> ⚠️ **제단(건물 재료)이 없으면 임포트 로그에 `!` 경고**가 뜬다. 승급은 건물 앞에서만 하므로
> 건물 재료가 없는 행은 **어디서도 발동하지 않는 죽은 레시피**다.
>
> `Evolutions.csv` 와 달리 여기서는 건물이 선택이 아니라 **필수**다.

#### 승급 직업의 외형

`Portrait`/`BodySprite`/`WalkSheet` 를 비우면 **승급 전 모습을 그대로 유지한다**
(`PlayerStats.ApplyClassVisual` 이 빈 값으로 덮지 않는다). 빈 값으로 덮으면 플레이어가
프리팹 기본 스프라이트로 되돌아가 버리기 때문이다.
**20차(I-57)에 T2/T3 4종의 그림 3열을 전부 채웠다.** 비어 있던 동안은 승급해도 화면이
그대로였다 — 게임에서 가장 큰 성취가 로그로만 존재했다.
⚠️ **`BodySprite` 만 채우는 것으로는 안 된다.** `PlayerVisual` 이 `sr.sprite` 를 걷기
프레임 0 으로 덮어쓰므로 **`WalkSheet` 이 있어야** 겉모습이 실제로 바뀐다.
`Portrait` 는 선택 화면 전용이라 승급 직업에는 기능상 필요 없지만, 나중에 승급 연출/도감에
쓸 원화를 같은 이름으로 남겨 두는 편이 낫다고 보아 4장 모두 채웠다.

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

**배열만 다루는 표가 아니다.** 값 하나짜리 참조와 숫자도 쓸 수 있다 (14차에 추가된 3행):

| Component | Field | 값 | 의미 |
|---|---|---|---|
| `ExperienceManager` | `chestPrefab` | `Prefabs/Pickup_Chest.prefab` | 엘리트/보스가 **확정**으로 떨구는 보물상자 |
| `ExperienceManager` | `magnetPrefab` | `Prefabs/Pickup_Magnet.prefab` | 잡몹이 확률로 떨구는 자석 |
| `ExperienceManager` | `magnetDropChance` | `0.006` | 잡몹 1마리당 자석 드랍 확률 |

> 자석 확률은 **웨이브당 200~300마리가 죽는다**는 전제로 잡았다 (기대값 1~2개).
> `MaxAlive` 나 시간창을 크게 바꿔 처치 수가 달라지면 이 값도 같이 봐야 한다.
>
> 상자는 확률이 아니라 확정이다 — 확률로 하면 "엘리트를 잡았다"는 사건이 흐려진다.
> 대신 **엘리트/보스는 자석을 안 떨군다.** 둘이 겹치면 뭘 먹었는지 알 수 없다.
> 상자를 열면 **레벨업 패널이 재사용**되고 **레벨은 오르지 않는다** (경험치가 아니라 보상이므로).

### `AudioLibrary.asset` — 소리 크기 (⚠️ **CSV 가 아니다**)

경로: `Assets/Game/Audio/AudioLibrary.asset` · 인스펙터에서 **직접 고친다.**

이 프로젝트에서 **인스펙터 직접 수정이 정당한 유일한 애셋**이다.
CSV 임포터가 이 파일을 대상으로 잡지 않으므로 Import 를 돌려도 덮어써지지 않는다.

| 열 | 의미 |
|---|---|
| `Id` | `SfxId` / `BgmId` **enum**. 코드가 이 키로 찾는다 |
| `Clip` | `.wav` 애셋 |
| `Volume` | 0~1. **이 소리 하나의 크기.** `AudioManager.SfxVolume`(전역 슬라이더)과 곱해진다 |
| `PitchJitter` | 0~0.3. 재생할 때마다 피치를 ±이만큼 흔든다. 반복되는 소리가 기계음처럼 들리는 걸 막는다 |

**현재 값과 그 근거** — 핵심은 **울리는 빈도에 반비례**한다는 것이다.

| 소리 | Vol | Jitter | 왜 이 값인가 |
|---|---:|---:|---|
| `XpPickup` | 0.22 | 0.14 | **가장 자주** 운다. 자석 하나면 한 프레임에 수십 개. 제일 낮고 제일 많이 흔든다 |
| `EnemyHit` | 0.28 | 0.12 | 초당 수십 번. 여기가 크면 게임 전체가 시끄러워진다 |
| `WeaponFire` | 0.35 | 0.10 | 쿨다운마다. 볼리당 **1회만** 울린다(코드에서 보장) |
| `WeaponCast` | 0.45 | 0.08 | 광역기라 발사보다 덜 잦고 더 무겁다 |
| `EnemyDie` | 0.40 | 0.10 | 피격보다 드물고, 사건으로 들려야 한다 |
| `Explosion` | 0.55 | 0.08 | 화면을 덮는 연출과 짝이라 두껍게 |
| `UiSelect` | 0.55 | 0.03 | 확정 4곳에서만. 흔들면 싸구려로 들린다 |
| `BuildingPlace` | 0.60 | 0.05 | **성공 경로에서만** 운다 (실패와 구분되어야 한다) |
| `Magnet` · `PlayerHit` | 0.70 | 0.03 / 0.05 | **놓치면 안 되는 정보.** 피격은 특히 |
| `ChestOpen` | 0.80 | 0.03 | 보상 사건 |
| `LevelUp` · `WaveClear` · `PlayerDie` | 0.85~0.90 | **0** | **런에서 몇 번 안 나는 소리.** 매번 똑같이 들려야 "그 소리"가 된다 |

> ⚠️ **`Id` 열의 enum 은 중간에 끼워 넣지 말 것.** 직렬화는 이름이 아니라 **정수**를 저장하므로
> 값이 밀리면 아래 매핑이 **통째로** 어긋난다. 새 소리는 `AudioId.cs` 의 **끝에** 추가한다.

전역 슬라이더(`AudioManager` 의 `BgmVolume` 0.8 / `SfxVolume` 1.0)는 **계통 전체**를 움직인다.
개별 소리가 튀면 여기가 아니라 위 표를 고친다.

---

### `Pretendard SDF.asset` — 폰트 문자표 (⚠️ **CSV 가 아니다**)

경로: `Assets/Fonts/Pretendard SDF.asset` · **23차(I-60)부터 `atlasPopulationMode = Static`.**

Static 이라 **문자표에 없는 글자는 빈칸으로 나온다.** 현재 굽혀 있는 것은 **115자**:

| 구간 | 내용 |
|---|---|
| ASCII 32~126 | 95자 **전부** |
| 기호 (실사용 확인) | `…`U+2026 `—`U+2014 `□`U+25A1 `·`U+00B7 `▲`U+25B2 `→`U+2192 |
| 기호 (여유) | `←` `▼` `–` `×` `•` `★` `☆` `©` `®` `°` `±` `≤` `≥` `∞` |

**한글은 없다.** UI 문자열은 영문이라는 규칙(CLAUDE.md §3) 때문에 뺐다.
한글이나 이모지를 UI 에 쓰려면 **먼저 폰트를 다시 구워야 한다.**

#### 다시 굽는 법

`Unity_RunCommand` 로 아래를 돌린다. **문자만 다시 채우고 애셋 자체는 그대로 둔다.**

```csharp
var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard SDF.asset");
fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;   // 굽는 동안만 Dynamic
fa.ClearFontAssetData(true);
fa.TryAddCharacters(target, out string missing);        // target = 기존 115자 + 새 문자
fa.atlasPopulationMode = AtlasPopulationMode.Static;    // 다시 잠근다
EditorUtility.SetDirty(fa); AssetDatabase.SaveAssets();
```

> 🔴 **`TMP_FontAsset.CreateFontAsset` 으로 새로 만들지 말 것.**
> GUID 가 바뀌어 씬·프리팹의 폰트 참조가 **전부** 끊긴다.
>
> ⚠️ `target` 에 **기존 115자를 반드시 다시 포함**할 것. `ClearFontAssetData` 가 전부 지운다.
>
> ⚠️ 한글을 넣으면 아틀라스가 폭발한다 — `samplingPointSize` 90 · `padding` 9 라
> 1024×1024 한 장에 **약 81자**밖에 안 들어간다. 대량으로 넣어야 하면
> `isMultiAtlasTexturesEnabled`(현재 `true`)로 장수를 늘리거나 Dynamic 으로 되돌리는 쪽이 낫다.
> 단 **Dynamic 으로 되돌리면 커밋마다 25만 줄 diff 가 돌아온다** (그게 I-60 의 이유였다).

#### 왜 Static 인가

Dynamic 은 런타임에 처음 만난 글리프를 그 자리에서 굽고 애셋을 dirty 로 만든다.
즉 **플레이만 해도 파일이 바뀐다.** 그 대신 Dynamic 은 **빠진 글리프를 숨긴다** —
굽기 전 이 폰트에는 ASCII 가 72자뿐이었는데(23자 누락) 아무도 몰랐다.

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

> `Final` 계산은 `base + meta + class + passive` 다 (`class` = **직업 사슬 전체**의 `Classes.csv` `Bonus*` 합).
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
| 소환한 마리수보다 적게 나옴 | 시간창(`:시작-종료`)을 넘겨 잘렸거나 `MaxAlive` 상한에서 대기 중이다. **의도된 동작** |
| 킬 클리어 웨이브가 안 끝남 | `KillTarget` 이 실제 소환 수보다 크다. 시간창·`MaxAlive` 로 줄어든 만큼 낮출 것 |
| 적이 다가오지 않고 맴돔 | 무리 분리 가중치(`0.9`)가 추격을 이겼다. `EnemyBase` 상수이며 CSV 열이 아니다 |
| `AI` 를 바꿨는데 그대로임 | 오타다. 콘솔에 `[CSV] 'AI' 열의 값 '...' 는 EnemyAI 에 없다` 경고가 찍힌다 |
| `Ranged` 적이 안 쏨 | `ProjectilePrefab` 이 비었다. 그러면 거리만 유지하고 발사하지 않는다 |
| 값을 고쳤는데 반영이 안 됨 | Import 를 안 돌렸거나, 인스펙터에서 고친 뒤 Import 로 덮어써졌다 |
| Import 후 참조가 비어 있음 | 콘솔의 `! ... 를 찾을 수 없음` 확인. `RefId`/`Spawns` 의 Id 오타 |
| 새로 만든 `.asset` 이 "Missing script" | ScriptableObject 클래스는 **동명 파일**에 있어야 한다 (`WaveData` 가 이 문제였다) |
| 인게임 스탯이 CSV 값의 2배 | 보너스 `StatBlock` 을 `new StatBlock()` 으로 만들었다. **`StatBlock.Zero()`** 를 쓸 것 |
| 레벨업 카드가 3장 안 뜸 / 같은 것만 반복 | 그 직업 사슬의 `Bonus*Slots` 합이 찼다. 신규 아이템이 후보에서 빠진 것이다 (**의도된 동작**) |
| 새 무기를 골랐는데 안 생김 | 무기 상한이 찼다. `[WeaponManager] 무기 슬롯 꽉 참` 경고가 찍히면 `CanAcquire` 를 우회한 경로가 있다는 뜻이다 |
| 승급했는데 칸이 예상보다 많음 | `Bonus*Slots` 를 **총량**으로 적었다. 사슬 전체가 합산되므로 **더하는 값**만 적을 것 |
| 승급 레시피가 어디서도 안 뜸 | `Ingredients` 에 **건물이 없다.** 임포트 로그의 `! ... 제단이 정해지지 않는다` 경고를 볼 것 |
| 승급 직업이 시작 선택 화면에 뜸 | `SceneWiring.csv` 의 `GameManager,classes` 에 넣었다. 승급 직업은 `classEvolutions` 로만 참조된다 |
