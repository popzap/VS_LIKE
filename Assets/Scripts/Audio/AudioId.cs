/// <summary>
/// 효과음 키. <see cref="AudioLibrary"/> 가 이 키로 클립을 들고 있고,
/// 호출부는 <c>AudioManager.Play(SfxId.EnemyHit)</c> 처럼 키만 넘긴다.
///
/// <para>문자열이 아니라 enum 인 이유 — 오타가 컴파일 에러로 잡히고,
/// 인스펙터에서 드롭다운으로 고를 수 있다. 대신 <b>기존 항목의 값을 바꾸지 말 것.</b>
/// 직렬화된 <c>AudioLibrary.asset</c> 은 이름이 아니라 정수값을 저장하므로
/// 값이 바뀌면 매핑이 그 자리에서 어긋난다.</para>
///
/// <para>반대로 <b>분류별 빈 번호에 끼워 넣는 것은 안전하다.</b>
/// <c>AudioLibrary</c> 는 배열 인덱스가 아니라 <c>Id</c> 값으로 매핑하므로
/// (<c>AudioLibrary.cs</c> <c>_sfxMap[e.Id] = e</c>) 새 항목을 중간에 넣어도
/// 기존 값이 하나도 밀리지 않는다. 분류 안에 모아 두는 편이 읽기 좋다.</para>
/// </summary>
public enum SfxId
{
    None = 0,

    // 무기
    WeaponFire  = 1,   // 단일 투사체 발사
    WeaponCast  = 2,   // 범위 무기 시전
    Explosion   = 3,   // 범위 폭발이 실제로 터지는 순간
    WeaponSwing = 4,   // 근접 무기 휘두르기 (MeleeWeapon) — 발사음이 아니다
    ToxinSpill  = 5,   // 장판 무기 전개 (FieldWeapon) — 액체가 쏟아져 끓는 소리
    // ⚠️ 촉수 클립은 "때리는 소리"가 아니라 "휘두르다 0.1초에 때리는 소리"다.
    //    SummonWeapon.lashHitDelay 를 바꾸면 클립도 다시 구워야 한다 (Tools/Audio/gen_tentacle_lash.py HIT_AT).
    TentacleLash = 6,  // 소환수 문어의 촉수 후리기 (SummonWeapon)
    DragonSpit   = 7,  // 소환수 드래곤의 화염구 뱉기 (SummonWeapon) — 착탄 폭발음(Explosion)이 아니다

    // 전투
    EnemyHit      = 10,
    EnemyDie      = 11,
    PlayerHit     = 12,
    PlayerDie     = 13,
    EnemyDieElite = 14,   // 엘리트·보스 공용 사망음. 15 는 EnemyDieBoss 자리로 비워 둔다
    EnemyShoot    = 16,   // 원거리 적의 발사
    Crit          = 17,   // 치명타 — ⚠️ 클립·호출 미배선 (WeaponBase 가 치명타 여부를 버린다)

    // 픽업 · 성장
    XpPickup    = 20,
    LevelUp     = 21,
    ChestOpen   = 22,
    Magnet      = 23,
    Heal        = 24,     // 회복(식당 힐템 · 힐 픽업)

    // 건물 · 진행
    BuildingPlace = 30,
    WaveClear     = 31,
    BuildingFire  = 32,   // 터렛·곡사포 발사 (공용)
    BossAppear    = 33,   // 보스 등장 — 처치음이 아니다

    // UI
    UiSelect    = 40,
    UiCancel    = 41,     // 리롤 · 취소 · 되돌리기
}

/// <summary>배경음 키. 값 관리 규칙은 <see cref="SfxId"/> 와 같다.</summary>
public enum BgmId
{
    None       = 0,
    MainMenu   = 1,   // 메인 메뉴 · 직업 선택
    WaveNormal = 2,   // 일반 웨이브
    WaveBoss   = 3,   // 보스 웨이브
    Shop       = 4,   // 상점 · 스테이지 맵
}
