using UnityEngine;

/// <summary>
/// <b>화톳불</b> — 곁에 서 있으면 <b>피해가 오른다</b> (D120 · 사용자 요구
/// "화톳불 (근처에 있으면 데미지 증가)").
///
/// <para>🔑 <b>건물 9종이 전부 "설치하면 알아서 일한다"였다.</b>
/// 그래서 <b>플레이어가 그 자리에 있을 이유가 하나도 없었다</b> — 지어 놓고 걸어가면 끝이다.
/// <c>D110</c> 이 진단한 문제(*"건물은 못 걷는 유일한 것"*)를 세 번째 각도로 푼다:</para>
///
/// <list type="table">
///   <item><b>미끼</b>(D110) — 적을 <b>부른다</b> (전장이 건물에 온다)</item>
///   <item><b>바리케이드</b>(D117) — 공간을 <b>자른다</b> (지나갈 수 없게 한다)</item>
///   <item><b>화톳불</b>(D120) — 플레이어를 <b>붙든다</b> (있고 싶게 만든다)</item>
/// </list>
///
/// <para>🔑 <b>여기가 이 게임에 없던 긴장이다</b> — 뱀서라이크에서 플레이어는 늘 움직인다.
/// 화톳불은 <i>"안전하지만 약하게 도망칠까"</i> 와 <i>"강하지만 서 있을까"</i> 를 부딪히게 한다.
/// 서 있으려면 적을 막아야 하고, 그러면 <b>미끼·벽·보호막을 쓸 이유</b>가 생긴다.</para>
///
/// <para>🔑 <b>새 갱신 루프를 안 만들었다.</b> <see cref="BuildingBase"/> 의 쿨다운을
/// 짧게(0.25초) 두고 <b>매 틱 버프를 다시 걸어 준다</b> — 벗어나면 아무도 안 걸어 주므로
/// 저절로 꺼진다. <see cref="FreezerBuilding"/> 이 슬로우를 유지하는 방식과 같다.
/// 🔴 <b>"나갔다"를 알아채는 코드가 없다</b> — 그런 코드는 늘 한쪽을 빠뜨린다.</para>
///
/// <para><b>CSV 필드를 이렇게 읽는다</b> (<c>Buildings.csv</c>):</para>
/// <list type="bullet">
/// <item><c>AttackRange</c> = <b>온기 반경</b>. 이 안에 있으면 버프를 받는다</item>
/// <item><c>Output</c> = <b>피해 가산분</b> (0.25 = +25 %)</item>
/// <item><c>AttackCooldown</c> = <b>갱신 주기</b>. 🔴 레벨이 올라도 줄이지 않는다 —
///       이건 세기가 아니라 <b>얼마나 촘촘히 확인하나</b>다</item>
/// <item><c>Damage</c> = <b>안 쓴다</b> (0). 화톳불은 때리지 않는다</item>
/// </list>
/// </summary>
public class CampfireBuilding : BuildingBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public new const int Version = 1;

    [Header("온기 표시")]
    [Tooltip("반경을 그리는 고리의 두께(유닛).")]
    [SerializeField] private float ringWidth = 0.07f;
    [Tooltip("고리를 몇 조각으로 그리는지.")]
    [SerializeField] private int ringSegments = 48;
    [Tooltip("플레이어가 밖에 있을 때의 고리 색.")]
    [SerializeField] private Color coldColor = new(1f, 0.55f, 0.2f, 0.35f);
    [Tooltip("플레이어가 안에 있을 때의 고리 색. 🔑 이 차이가 '지금 받고 있다'를 알려 준다.")]
    [SerializeField] private Color warmColor = new(1f, 0.75f, 0.3f, 0.9f);
    [SerializeField] private int sortingOrder = 3;

    /// <summary>
    /// 버프를 걸어 주는 시간의 배수. 🔴 <b>1 보다 커야 한다</b> —
    /// 정확히 주기만큼 주면 다음 갱신 직전에 한 프레임 꺼져 <b>피해가 깜빡인다.</b>
    /// (<see cref="FreezerBuilding"/> 이 슬로우 지속시간에 쓴 것과 같은 이유다.)
    /// </summary>
    private const float holdMultiplier = 1.8f;

    private LineRenderer _ring;
    private bool _wasWarm;

    /// <summary>온기 반경. <c>Buildings.csv</c> 의 <c>AttackRange</c> 다.</summary>
    private float WarmRadius => Data != null ? Mathf.Max(0.5f, Data.GetRange(Level)) : 1f;

    /// <summary>피해 가산분. <c>Buildings.csv</c> 의 <c>Output</c> 이다.</summary>
    private float DamageBonus => Data != null ? Mathf.Max(0f, Data.GetOutput(Level)) : 0f;

    /// <summary>지금 플레이어가 온기 안에 있나 (검증용).</summary>
    public bool PlayerInside
    {
        get
        {
            var st = PlayerStats.Current;
            if (st == null) return false;
            return Vector2.Distance(st.transform.position, transform.position) <= WarmRadius;
        }
    }

    protected override void OnInitialized() { BuildRing(); DrawRing(); }
    protected override void OnUpgraded()    { DrawRing(); }

    protected override void OnCooldownElapsed()
    {
        var st = PlayerStats.Current;
        if (st == null || st.IsDead) return;
        if (!PlayerInside) return;

        // 🔑 매 틱 다시 걸어 준다. 벗어나면 아무도 안 걸어 주므로 저절로 꺼진다.
        st.GrantCampfire(DamageBonus, Cooldown * holdMultiplier);
    }

    /// <summary>
    /// 🔵 고리 색으로 <b>지금 받고 있는지</b>를 알려 준다.
    /// 안 그러면 플레이어는 자기가 강해졌는지 알 방법이 없다 — 숫자가 화면에 없다.
    ///
    /// <para>🔴 <c>Update</c> 를 선언하지 않는다 — <see cref="BuildingBase"/> 가 그걸로
    /// 쿨다운을 돌린다. 가리면 <b>화톳불이 영영 발동하지 않는다.</b></para>
    /// </summary>
    private void LateUpdate()
    {
        if (_ring == null) return;
        bool warm = PlayerInside;
        if (warm == _wasWarm) return;      // 바뀔 때만 만진다
        _wasWarm = warm;
        var c = warm ? warmColor : coldColor;
        _ring.startColor = c;
        _ring.endColor   = c;
    }

    // ── 고리 ────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 셰이더는 <b>URP 2D 것</b>이라야 한다. 빌트인 <c>Sprites/Default</c> 는
    /// <c>isSupported</c> 가 <b>true 인데도 URP 2D 렌더러가 한 픽셀도 안 그린다</b> (D111).
    /// </summary>
    private void BuildRing()
    {
        if (_ring != null) return;

        var go = new GameObject("WarmRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _ring = go.AddComponent<LineRenderer>();
        _ring.useWorldSpace = false;
        _ring.loop          = true;
        _ring.startWidth    = ringWidth;
        _ring.endWidth      = ringWidth;
        _ring.sortingOrder  = sortingOrder;
        _ring.textureMode   = LineTextureMode.Stretch;
        _ring.alignment     = LineAlignment.View;
        _ring.startColor    = coldColor;
        _ring.endColor      = coldColor;

        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
              ?? Shader.Find("Sprites/Default");
        _ring.material = new Material(sh);
    }

    /// <summary>🔴 반경이 바뀌면 고리도 바꾼다 — 안 그러면 <b>그림이 범위를 속인다.</b></summary>
    private void DrawRing()
    {
        if (_ring == null) return;
        int n = Mathf.Max(12, ringSegments);
        _ring.positionCount = n;
        // 🔴 부모의 스케일을 되돌린다 — 건물 프리팹이 1 이 아니면 고리가 같이 늘어난다.
        float sx = Mathf.Max(1e-4f, transform.lossyScale.x);
        float sy = Mathf.Max(1e-4f, transform.lossyScale.y);
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * WarmRadius / sx,
                                             Mathf.Sin(a) * WarmRadius / sy, 0f));
        }
    }
}
