using UnityEngine;

/// <summary>
/// <b>사이렌</b> — 일정 주기로 울려 사거리 안의 적을 <b>완전히 멈춘다</b>
/// (D108 · 사용자 요구 "일정시간 마다 적들을 멈춤").
///
/// <para>🔴 <b>슬로우로 대신하지 않았다.</b> <c>ApplySlow(0f, …)</c> 가 가장 싼 길처럼 보이는데
/// <see cref="FreezerBuilding"/> 과 <b>같이 놓으면 영구 정지 버그</b>가 난다 —
/// 냉각탑이 <c>_slowUntil</c> 을 계속 밀어 주는 동안 <c>ApplySlow</c> 는 더 약한 값으로 덮지 못한다.
/// 그래서 <see cref="EnemyBase.ApplyStun"/> 을 따로 뒀다. 자세한 값 추적은 그쪽 주석에 있다.</para>
///
/// <para>🔑 <b>냉각탑과 역할이 다르다.</b> 냉각탑은 <b>계속 조금</b>, 사이렌은 <b>가끔 완전히</b> 다.
/// 냉각탑은 카이팅을 편하게 만들고, 사이렌은 <b>포위를 한 번 끊는다</b> —
/// 둘 다 쿨다운을 줄이는 <c>BuildingCooldown</c> 패시브를 받지만
/// <b>체감이 커지는 쪽은 사이렌</b>이다(주기가 곧 효과다).</para>
///
/// <para><b>CSV 필드를 이렇게 읽는다</b> (<c>Buildings.csv</c>):</para>
/// <list type="bullet">
/// <item><c>Output</c> = <b>정지 시간(초)</b></item>
/// <item><c>AttackCooldown</c> = <b>울리는 주기(초)</b></item>
/// <item><c>AttackRange</c> = 반경 · <c>Damage</c> = <b>안 쓴다</b> (0)</item>
/// </list>
///
/// <para>🔴 <c>Output</c> 이 <c>AttackCooldown</c> 에 가까워지면 <b>상시 정지</b>가 된다.
/// 그래서 <see cref="maxUptimeRatio"/> 로 <b>주기 대비 비율</b>에 상한을 건다 —
/// CSV 를 잘못 채워도 게임이 멈추지는 않는다.</para>
/// </summary>
public class SirenBuilding : BuildingBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Tooltip("정지 시간이 주기의 몇 배를 넘지 못하게 할지. 0.6 이면 최대 60 % 시간만 멈춘다.")]
    [SerializeField, Range(0.1f, 0.95f)] private float maxUptimeRatio = 0.6f;

    [Tooltip("울릴 때 낼 소리. 비워 두면 소리가 없다.")]
    [SerializeField] private bool playSound = true;

    protected override void OnCooldownElapsed()
    {
        // 🔴 base 를 부르면 안 된다 — 기본 구현은 "가장 가까운 하나를 때린다" 다.
        float raw = Data.GetOutput(Level);
        if (raw <= 0f) return;                                  // CSV 가 비었으면 아무 일도 안 한다

        float duration = Mathf.Min(raw, Cooldown * maxUptimeRatio);

        var hits  = FindEnemiesInRange();
        int stunned = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            var e = hits[i].GetComponent<EnemyBase>();
            if (e == null) continue;
            e.ApplyStun(duration);
            stunned++;
        }

        // 🔑 아무도 안 걸렸으면 소리를 내지 않는다 — 빈 벌판에서 계속 울리면 소음이다.
        if (playSound && stunned > 0) AudioManager.Play(SfxId.BuildingFire);
    }
}
