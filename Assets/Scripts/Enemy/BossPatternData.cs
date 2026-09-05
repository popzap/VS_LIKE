using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  BossPatternData  —  보스 한 종의 페이즈·기술 파라미터
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 보스가 <b>큰 잡몹이 아니게</b> 만드는 값들 (D31).
///
/// <para>🔴 <b>이 클래스는 자기 파일에 단독으로 있어야 한다</b> (I-19).
/// 다른 파일에 얹으면 새 <c>.asset</c> 의 <c>m_Script</c> 가 <c>0</c> 으로 박히고
/// 재임포트로도 복구되지 않는다.</para>
///
/// <para>배열은 전부 <b>페이즈 인덱스</b>로 읽는다 — <c>[0]</c> 이 1페이즈다.
/// 길이가 모자라면 <b>마지막 값을 계속 쓴다</b>(<see cref="At(float[],int)"/>).
/// 페이즈를 하나 늘렸는데 값 배열을 안 늘려도 조용히 죽지 않게 하려는 것이다.</para>
///
/// <para>원본은 <c>Assets/Game/Balance/Bosses.csv</c> 다. 인스펙터에서 고치지 말 것.</para>
/// </summary>
[CreateAssetMenu(fileName = "BossPattern", menuName = "Game/BossPatternData")]
public class BossPatternData : ScriptableObject
{
    [Tooltip("이 패턴을 쓸 EnemyData 의 Id. 임포터가 이 값으로 EnemyData.BossPattern 을 채운다")]
    public string EnemyId;

    [Header("페이즈")]
    [Tooltip("페이즈가 바뀌는 HP 비율. 0.7|0.4 면 70 % 아래에서 2페이즈, 40 % 아래에서 3페이즈. " +
             "🔴 내림차순이어야 한다")]
    public float[] PhaseThresholds = { 0.7f, 0.4f };

    [Tooltip("페이즈별 이동속도 배수. 뒤로 갈수록 빨라지는 게 보통이다")]
    public float[] PhaseSpeedMult = { 1f, 1.15f, 1.35f };

    [Header("내려찍기 (예고 있는 광역기)")]
    [Tooltip("예고가 떠 있는 시간(초). 🔴 이만큼은 피할 수 있어야 한다 — 0 이면 피할 수 없는 공격이 된다")]
    public float SlamWindup = 0.9f;

    [Tooltip("피해 반경(월드 유닛). 예고 원의 크기와 같다")]
    public float SlamRadius = 3.2f;

    [Tooltip("피해량. 접촉 피해와 별개다")]
    public float SlamDamage = 22f;

    [Tooltip("페이즈별 재사용 대기(초). 짧을수록 압박이 세다")]
    public float[] SlamCooldown = { 5f, 3.8f, 2.6f };

    [Header("소환")]
    [Tooltip("불러낼 잡몹의 EnemyData Id. 비우면 소환하지 않는다. 임포터가 이 값으로 SummonEnemy 를 채운다")]
    public string SummonEnemyId = "Goblin";

    /// <summary>
    /// 실제로 소환할 적. <b>임포터가 <see cref="SummonEnemyId"/> 로 찾아 꽂는다.</b>
    ///
    /// <para>🔴 런타임에 Id 로 찾지 않는다 — <c>AssetDatabase</c> 는 빌드에 없고,
    /// 웨이브 소환 목록에서 이름으로 뒤지는 방식은 <b>그 웨이브에 없는 적을 못 부른다</b>
    /// (D31 에서 실제로 이걸로 소환이 통째로 안 돌았다). 참조를 미리 꽂아 두는 게 맞다.</para>
    /// </summary>
    public EnemyData SummonEnemy;

    [Tooltip("페이즈별 1회 소환 마릿수")]
    public int[] SummonCount = { 3, 4, 6 };

    [Tooltip("페이즈별 소환 대기(초)")]
    public float[] SummonCooldown = { 12f, 9f, 7f };

    [Tooltip("보스 주변 이 반경 안에 놓는다")]
    public float SummonRadius = 3.5f;

    [Header("소환 돌진 (B-1 · D73)")]
    // 🔑 소환과 돌진을 **하나의 기술**로 묶었다. 따로 두면 둘 다 "가끔 일어나는 일" 인데,
    //    묶으면 **쫄이 나타난 것 자체가 돌진의 예고**가 되어 플레이어가 읽을 수 있다.
    [Tooltip("돌진 전 멈춰 서서 노려보는 시간(초). 🔴 이만큼은 피할 수 있어야 한다")]
    public float ChargeWindup = 0.75f;

    [Tooltip("돌진 속도(월드 유닛/초). 플레이어 기본 이동 3.7~4.6 보다 빨라야 위협이 된다")]
    public float ChargeSpeed = 11f;

    [Tooltip("돌진이 지속되는 시간(초). 속도 × 이 값 = 돌진 거리")]
    public float ChargeDuration = 0.55f;

    [Tooltip("돌진 뒤 멈춰 있는 시간(초). 🔴 플레이어의 반격 기회다 — 0 이면 계속 밀린다")]
    public float ChargeRecover = 0.9f;

    [Tooltip("페이즈별 소환 돌진 대기(초). 0 이하면 이 기술을 안 쓴다")]
    public float[] ChargeCooldown = { 9f, 7f, 5f };

    [Tooltip("돌진과 함께 부르는 잡몹 수(페이즈별). SummonEnemy 를 그대로 쓴다")]
    public int[] ChargeSummonCount = { 2, 3, 4 };

    [Header("예고 일직선 (B-1 · D73)")]
    // 🔑 새 시스템을 안 만들었다 — BossSlam(예고→폭발→풀 반환)을 **줄 세워** 놓는다.
    //    D37 의 지뢰밭이 같은 부품을 썼고, 그건 이미 검증된 코드다.
    [Tooltip("예고가 떠 있는 시간(초). 🔴 0 이면 피할 수 없는 공격이 된다")]
    public float LineWindup = 1.1f;

    [Tooltip("직선의 길이(월드 유닛). 화면 반너비가 약 10.7 이라 이보다 길면 화면을 가른다")]
    public float LineLength = 12f;

    [Tooltip("직선의 굵기 = 각 마디의 반경(월드 유닛)")]
    public float LineRadius = 1.4f;

    [Tooltip("직선을 이루는 마디 수. 많을수록 촘촘하지만 예고 오브젝트도 그만큼 늘어난다")]
    public int LineSegments = 7;

    [Tooltip("마디 하나의 피해량. 🔴 마디가 겹치므로 실제로는 여러 번 맞을 수 있다 — 낮게 둔다")]
    public float LineDamage = 14f;

    [Tooltip("페이즈별 일직선 대기(초). 0 이하면 이 기술을 안 쓴다")]
    public float[] LineCooldown = { 8f, 6f, 4.5f };

    [Header("등장")]
    [Tooltip("등장 시 화면 흔들림 세기 / 길이")]
    public float EntryShakeMagnitude = 0.55f;
    public float EntryShakeDuration  = 0.7f;

    // ── 읽기 도우미 ──────────────────────────────────────────────
    //
    // 🔴 배열 길이가 페이즈 수보다 짧아도 예외를 내지 않는다.
    //    CSV 를 만지는 사람이 페이즈만 늘리는 실수는 반드시 일어난다.

    public static float At(float[] a, int i)
        => a == null || a.Length == 0 ? 0f : a[Mathf.Clamp(i, 0, a.Length - 1)];

    public static int At(int[] a, int i)
        => a == null || a.Length == 0 ? 0 : a[Mathf.Clamp(i, 0, a.Length - 1)];

    /// <summary>페이즈 수 = 임계값 수 + 1.</summary>
    public int PhaseCount => (PhaseThresholds != null ? PhaseThresholds.Length : 0) + 1;

    /// <summary>HP 비율(0~1)이 몇 페이즈인지. 0 이 1페이즈다.</summary>
    public int PhaseOf(float hpFraction)
    {
        if (PhaseThresholds == null) return 0;
        int p = 0;
        foreach (float th in PhaseThresholds)
            if (hpFraction <= th) p++;
        return p;
    }
}
