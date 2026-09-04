using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  LayerScaling  —  층이 깊어질수록 세지는 전역 배율 (ROADMAP §3 결정 4 · B안)
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 층(Layer) 하나가 늘 때마다 적의 <b>체력 · 접촉 피해 · 소환량</b>에 곱해지는 배율을 들고 있다.
///
/// <para><b>왜 필요했나</b> — <see cref="WaveManager.StartWave"/> 가 노드 타입만 보고
/// 웨이브 풀에서 <b>무작위로</b> 뽑기 때문에 <b>1층에서 Normal3(108마리)이 나오고
/// 9층에서 Normal1(64마리)이 나올 수 있었다.</b> 난이도 곡선이 존재하지 않았다.</para>
///
/// <para>🔑 <b>(A) 웨이브에 <c>MinLayer</c>/<c>MaxLayer</c> 열을 다는 안은 나중이다.</b>
/// 웨이브가 6개뿐이라 지금 넣으면 층당 선택지가 거의 없다.
/// 전역 배율은 데이터가 거의 안 들고 층이 몇 개든 자동으로 늘어난다.</para>
///
/// <para>🔴 <b>정적 클래스인 이유</b> — <see cref="EnemyBase.Initialize"/> 가 이 값을 읽는데,
/// 거기서 <c>GameManager.Instance.XxxMgr</c> 를 타면 I-8/I-38 의 <c>Awake</c> 순서 함정에
/// 걸린다. 값을 쓰는 쪽(<see cref="WaveManager"/>)이 웨이브 시작 때 한 번 밀어 넣고
/// 읽는 쪽은 조회만 한다.</para>
///
/// <para>⚠️ <b>배수 자체는 여기 없다.</b> 계수는 <c>Economy.csv</c> 의 <c>WaveManager</c> 행이
/// 들고 있고 이 클래스는 계산 결과만 보관한다 — 수치는 CONTENT 소관이다.</para>
/// </summary>
public static class LayerScaling
{
    /// <summary>0 = 첫 층. <see cref="StageNode.Layer"/> 와 같은 기준이다.</summary>
    public static int Layer { get; private set; }

    public static float HpMult    { get; private set; } = 1f;
    public static float DamageMult{ get; private set; } = 1f;
    public static float SpawnMult { get; private set; } = 1f;

    /// <summary>
    /// 웨이브가 시작될 때 <see cref="WaveManager"/> 가 부른다.
    /// </summary>
    /// <param name="layer">0 부터 세는 층 번호.</param>
    /// <param name="hpGrowth">층당 체력 증가율 (0.10 = 층마다 +10 %).</param>
    /// <param name="damageGrowth">층당 접촉 피해 증가율.</param>
    /// <param name="spawnGrowth">층당 소환량 증가율.</param>
    public static void Set(int layer, float hpGrowth, float damageGrowth, float spawnGrowth)
    {
        Layer      = Mathf.Max(0, layer);
        HpMult     = 1f + Mathf.Max(0f, hpGrowth)     * Layer;
        DamageMult = 1f + Mathf.Max(0f, damageGrowth) * Layer;
        SpawnMult  = 1f + Mathf.Max(0f, spawnGrowth)  * Layer;
    }

    /// <summary>새 런 시작. 안 되돌리면 <b>다음 런 1층이 지난 런 10층 난이도로 시작한다.</b></summary>
    public static void Reset()
    {
        Layer      = 0;
        HpMult     = 1f;
        DamageMult = 1f;
        SpawnMult  = 1f;
    }

    /// <summary>개수에 <see cref="SpawnMult"/> 를 곱한다. 원래 0 이었으면 0 으로 둔다.</summary>
    public static int ScaleCount(int baseCount)
    {
        if (baseCount <= 0) return baseCount;
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * SpawnMult));
    }
}
