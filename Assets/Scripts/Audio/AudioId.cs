/// <summary>
/// 효과음 키. <see cref="AudioLibrary"/> 가 이 키로 클립을 들고 있고,
/// 호출부는 <c>AudioManager.Play(SfxId.EnemyHit)</c> 처럼 키만 넘긴다.
///
/// <para>문자열이 아니라 enum 인 이유 — 오타가 컴파일 에러로 잡히고,
/// 인스펙터에서 드롭다운으로 고를 수 있다. 대신 <b>기존 항목의 값을 바꾸거나
/// 중간에 끼워 넣지 말 것.</b> 직렬화된 <c>AudioLibrary.asset</c> 은 이름이 아니라
/// 정수값을 저장하므로 순서가 밀리면 매핑이 통째로 어긋난다. 새 항목은 끝에 추가한다.</para>
/// </summary>
public enum SfxId
{
    None = 0,

    // 무기
    WeaponFire  = 1,   // 단일 투사체 발사
    WeaponCast  = 2,   // 범위 무기 시전
    Explosion   = 3,   // 범위 폭발이 실제로 터지는 순간

    // 전투
    EnemyHit    = 10,
    EnemyDie    = 11,
    PlayerHit   = 12,
    PlayerDie   = 13,

    // 픽업 · 성장
    XpPickup    = 20,
    LevelUp     = 21,
    ChestOpen   = 22,
    Magnet      = 23,

    // 건물 · 진행
    BuildingPlace = 30,
    WaveClear     = 31,

    // UI
    UiSelect    = 40,
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
