using UnityEngine;

/// <summary>
/// <b>냉각탑</b> — 사거리 안의 적을 계속 느리게 만든다 (D108 · 사용자 요구 "주변 반경 적들 슬로우").
///
/// <para>🔑 <b>새 시스템이 하나도 필요 없었다.</b> <see cref="EnemyBase.ApplySlow"/> 가 이미 있고
/// (독 장판 <c>ToxinField</c> 가 쓴다) "매 틱 다시 걸어 주면 유지, 안 걸어 주면 저절로 풀림"
/// 이라는 계약까지 그대로 맞는다 — <b>적이 범위를 벗어났는지 아무도 추적할 필요가 없다.</b></para>
///
/// <para>🔴 <b>슬로우는 곱해지지 않는다.</b> <c>ApplySlow</c> 가 "가장 센 것 하나만" 적용한다 —
/// 냉각탑 두 대가 겹쳐도 0.6 x 0.6 = 0.36 이 되지 않는다. 겹쳐 놓아도 <b>세지지 않는다</b>는 뜻이라,
/// 냉각탑을 여러 대 짓는 값어치는 <b>세기가 아니라 넓이</b>다. (겹치면 세지게 하려면
/// <c>ApplySlow</c> 쪽 계약을 바꿔야 하고, 그러면 장판 2개에 적이 사실상 멈춘다.)</para>
///
/// <para><b>CSV 필드를 이렇게 읽는다</b> (<c>Buildings.csv</c>):</para>
/// <list type="bullet">
/// <item><c>Output</c> = <b>감속률</b> 0~1. 0.35 면 속도가 65 %가 된다</item>
/// <item><c>AttackCooldown</c> = <b>다시 걸어 주는 주기</b>. 짧을수록 촘촘하지만 체감은 같다</item>
/// <item><c>AttackRange</c> = 반경 · <c>Damage</c> = <b>안 쓴다</b> (0)</item>
/// </list>
///
/// <para>🔵 <b>그래서 이 건물만 <c>BuildingCooldown</c> 패시브가 거의 무의미하다.</b>
/// 지속시간을 주기보다 길게 잡아 <b>틈이 안 생기게</b> 하므로, 주기가 짧아져도 결과가 같다.
/// 대신 적이 범위를 나간 뒤 슬로우가 풀리는 데 걸리는 시간이 짧아진다 — 그게 유일한 차이다.</para>
/// </summary>
public class FreezerBuilding : BuildingBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Tooltip("지속시간을 주기의 몇 배로 걸어 줄지. 1 보다 커야 펄스 사이에 틈이 안 생긴다.")]
    [SerializeField] private float durationMultiplier = 1.35f;

    [Tooltip("감속률의 하한/상한. CSV 값이 튀어도 적이 완전히 멈추거나 무의미해지지 않게 막는다.")]
    [SerializeField] private float minSlowMultiplier = 0.25f;

    protected override void OnCooldownElapsed()
    {
        // 🔴 base 를 부르면 안 된다 — 기본 구현은 "가장 가까운 하나를 때린다" 다.
        //    이 건물은 범위 전체에 같은 일을 한다.
        float amount = Mathf.Clamp01(Data.GetOutput(Level));
        if (amount <= 0f) return;                       // CSV 가 비었으면 아무 일도 안 한다

        float mult     = Mathf.Max(minSlowMultiplier, 1f - amount);
        float duration = Cooldown * Mathf.Max(1.05f, durationMultiplier);

        var hits = FindEnemiesInRange();
        for (int i = 0; i < hits.Length; i++)
        {
            var e = hits[i].GetComponent<EnemyBase>();
            if (e != null) e.ApplySlow(mult, duration);
        }
    }
}
