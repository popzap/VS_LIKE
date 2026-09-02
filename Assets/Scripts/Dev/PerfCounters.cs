/// <summary>
/// 🔬 <b>임시 계측 (D27).</b> 프레임마다 "무슨 일이 몇 번 일어났는지"를 센다.
///
/// <para>왜 필요한가: 구간 프로파일링은 <b>전 프레임 평균</b>이라
/// 250 ms 짜리 프레임 몇 개가 묻힌다. 스파이크의 원인을 찾으려면
/// <b>느린 프레임에 무엇이 몰려 있었는지</b>를 봐야 한다.</para>
///
/// <para><see cref="On"/> 이 꺼져 있으면 분기 하나 값이다.
/// 원인 특정이 끝나면 <b>이 파일과 증가 코드 3곳을 지운다</b> (<c>CLAUDE.md</c> §4).
/// 증가 코드가 있는 곳: <c>ObjectPool.Get</c> · <c>ObjectPool.CreateNew</c> ·
/// <c>EnemyBase.Die</c> · <c>DamagePopupManager.Show</c>.</para>
/// </summary>
public static class PerfCounters
{
    public static bool On;

    /// <summary>풀에서 꺼낸 횟수.</summary>
    public static int PoolGets;

    /// <summary>🔴 풀이 비어 <c>Instantiate</c> 를 부른 횟수. H1 의 결정적 증거.</summary>
    public static int PoolCreates;

    /// <summary>적이 죽은 횟수.</summary>
    public static int Deaths;

    /// <summary>데미지 팝업이 뜬 횟수.</summary>
    public static int Popups;

    // ── 무기 질의 (H4·H5) ───────────────────────────────────────

    /// <summary><c>OverlapCircleAll</c> 호출 수.</summary>
    public static int WeaponQueryCount;

    /// <summary>그 질의들이 돌려준 콜라이더 총 수. 적이 많을수록 커진다.</summary>
    public static int WeaponQueryHits;

    /// <summary>질의 자체에 쓴 시간 (Stopwatch 틱).</summary>
    public static long WeaponQueryTicks;

    /// <summary>
    /// <c>WeaponBase.FindNearestEnemy()</c> <b>전체</b>에 쓴 시간.
    /// 질의 + 그 뒤의 O(n) 선형 탐색(<c>Vector2.Distance</c> = sqrt)까지 포함한다.
    /// <c>WeaponQueryTicks</c> 와의 차이가 <b>탐색 비용</b>이다.
    /// </summary>
    public static long WeaponScanTicks;

    // ── ExpDrop / WorldPickup 의 Update 단가 (A/B) ──────────────

    /// <summary>
    /// 🔬 켜면 <c>ExpDrop</c>·<c>WorldPickup</c> 이 <b>최적화 전 코드 경로</b>로 돈다
    /// (매 프레임 <c>GetComponent</c> + <c>Vector2.Distance</c>).
    ///
    /// <para>🔴 이게 필요한 이유: 최적화 전/후를 <b>다른 실행</b>으로 비교하면
    /// 인스턴스 수가 달라져 비교가 오염된다. 실제로 한 번 그렇게 나왔다
    /// (BEFORE 966개 vs AFTER 306개). 같은 실행 안에서 시행마다 경로만 뒤집으면
    /// <b>인스턴스 수가 같은 조건</b>에서 단가만 비교할 수 있다.</para>
    /// </summary>
    public static bool SlowPath;

    /// <summary><c>Update</c> 본문에 쓴 누적 시간 (Stopwatch 틱).</summary>
    public static long DropUpdateTicks;

    /// <summary><c>Update</c> 가 실제로 돈 횟수 (= 인스턴스 × 프레임).</summary>
    public static long DropUpdateCalls;

    /// <summary>프레임마다 하네스가 부른다. 값을 읽어 간 뒤 0으로 되돌린다.</summary>
    public static void ResetFrame()
    {
        PoolGets    = 0;
        PoolCreates = 0;
        Deaths      = 0;
        Popups      = 0;

        WeaponQueryCount = 0;
        WeaponQueryHits  = 0;
        WeaponQueryTicks = 0;
        WeaponScanTicks  = 0;
    }

    /// <summary>A/B 누적치만 따로 초기화한다 (프레임마다가 아니라 시행마다).</summary>
    public static void ResetDropTrial()
    {
        DropUpdateTicks = 0;
        DropUpdateCalls = 0;
    }

    /// <summary>
    /// 🔬 <c>Physics2D.OverlapCircleAll</c> 을 감싸 시간과 반환 수를 잰다.
    ///
    /// <para>꺼져 있으면 원본을 그대로 부른다 — 분기 하나 값이다.
    /// 계측이 끝나면 호출부를 <c>Physics2D.OverlapCircleAll</c> 로 되돌리고 이 파일을 지운다.</para>
    /// </summary>
    public static UnityEngine.Collider2D[] OverlapCircleAll(
        UnityEngine.Vector2 point, float radius, int layerMask)
    {
        if (!On) return UnityEngine.Physics2D.OverlapCircleAll(point, radius, layerMask);

        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var hits = UnityEngine.Physics2D.OverlapCircleAll(point, radius, layerMask);
        WeaponQueryTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;

        WeaponQueryCount++;
        WeaponQueryHits += hits.Length;
        return hits;
    }
}
