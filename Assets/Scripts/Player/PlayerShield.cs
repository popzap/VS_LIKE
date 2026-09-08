using UnityEngine;

/// <summary>
/// 보호막 — <b>원거리 피격 한 번</b>을 막고 일정 시간 뒤 다시 찬다
/// (D113 · 사용자 요구 <i>"보호막(원거리 공격 막아주는 쉴드 - 일정 시간마다 젠, lv 높으면 쿨타임 짧아짐)"</i>).
///
/// <para>🔑 <b>주기는 패시브가 정하고, 이 컴포넌트는 늘 붙어 있다.</b>
/// <see cref="StatBlock.ShieldInterval"/> 이 <c>0</c> 이면 <b>기능 자체가 없다</b> —
/// 패시브를 먹어야 값이 들어온다. 컴포넌트를 붙였다 뗐다 하지 않는 이유는,
/// 그러면 "언제 붙이나"를 정하는 코드가 또 필요해지고 그게 조용히 틀어지기 때문이다.</para>
///
/// <para>🔴 <b>접촉 피해는 안 막는다.</b> 요구가 "원거리 공격"이었고, 접촉까지 막으면
/// 무적 픽업(<see cref="PlayerStats.GrantInvincibility"/>)과 구분이 사라진다.
/// 무엇이 원거리인지는 <see cref="PlayerStats.TryTakeHit"/> 의 <c>ranged</c> 인자가 정한다.</para>
///
/// <para>🔵 막았을 때 <b>무적 시간을 주지 않는다.</b> 주면 보호막 한 장이
/// 그 뒤 0.5초의 공격까지 통째로 지워 버려 "한 번 막는다"가 아니게 된다.</para>
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerShield : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("연출")]
    [Tooltip("막이 차 있을 때 플레이어를 감싸는 고리의 반지름(유닛).")]
    [SerializeField] private float ringRadius = 0.75f;
    [Tooltip("고리 두께(유닛).")]
    [SerializeField] private float ringWidth = 0.09f;
    [SerializeField] private Color ringColor = new(0.45f, 0.8f, 1f, 0.85f);
    [Tooltip("고리를 몇 조각으로 그리는지. 낮으면 각져 보인다.")]
    [SerializeField] private int ringSegments = 40;
    [Tooltip("스프라이트 정렬 순서. 플레이어보다 앞에 그려야 보인다.")]
    [SerializeField] private int sortingOrder = 25;

    private PlayerStats  _stats;
    private LineRenderer _ring;
    private float        _timer;
    private bool         _charged;

    /// <summary>지금 막이 서 있는가. HUD 가 물어볼 수 있게 열어 둔다.</summary>
    public bool IsCharged => _charged;

    /// <summary>이 판에서 보호막이 실제로 막아 낸 횟수 (검증·통계용).</summary>
    public int BlockedCount { get; private set; }

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        BuildRing();
    }

    private void Update()
    {
        // 🔴 GameManager.Instance.XxxMgr 를 Awake 에서 캐시하지 않는다 (I-8/I-38).
        //    여기서는 스탯만 보므로 캐시할 것도 없다.
        float interval = _stats != null ? _stats.Final.ShieldInterval : 0f;

        if (interval <= 0f)
        {
            // 패시브가 없다 = 기능이 없다. 상태를 남겨 두면 나중에 먹었을 때 공짜로 한 장 들고 시작한다.
            if (_charged || _timer > 0f) { _charged = false; _timer = 0f; }
            ShowRing(false);
            return;
        }

        if (!_charged)
        {
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer   = 0f;
                _charged = true;
                AudioManager.Play(SfxId.ShieldUp);     // D116 · 전용 소리 (BuffPickup 을 빌려 쓰던 자리)
            }
        }

        ShowRing(_charged);
    }

    /// <summary>
    /// 원거리 피격을 한 번 먹는다. 막았으면 <c>true</c>.
    /// <see cref="PlayerStats.TryTakeHit"/> 에서만 부른다.
    /// </summary>
    public bool TryAbsorb()
    {
        if (!_charged) return false;

        _charged = false;
        _timer   = 0f;                 // 🔴 여기서 0 으로 되돌린다 — 안 하면 다음 장이 즉시 찬다
        BlockedCount++;
        AudioManager.Play(SfxId.ShieldBlock);   // D116 · PlayerHit 의 반대말 (UiCancel 을 빌려 쓰던 자리)
        ShowRing(false);
        return true;
    }

    // ── 고리 ────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 스프라이트를 안 쓰고 <see cref="LineRenderer"/> 로 그린다 — 애셋 의존이 없다.
    /// 🔴 셰이더는 <b>URP 2D 것</b>이라야 한다. 빌트인 <c>Sprites/Default</c> 는
    /// <c>isSupported</c> 가 <b>true 인데도 URP 2D 렌더러가 한 픽셀도 안 그린다</b> (D111 에서 겪었다).
    /// </summary>
    private void BuildRing()
    {
        var go = new GameObject("ShieldRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _ring = go.AddComponent<LineRenderer>();
        _ring.useWorldSpace  = false;
        _ring.loop           = true;
        _ring.positionCount  = Mathf.Max(8, ringSegments);
        _ring.startWidth     = ringWidth;
        _ring.endWidth       = ringWidth;
        _ring.startColor     = ringColor;
        _ring.endColor       = ringColor;
        _ring.sortingOrder   = sortingOrder;
        _ring.textureMode    = LineTextureMode.Stretch;
        _ring.alignment      = LineAlignment.View;

        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
              ?? Shader.Find("Sprites/Default");
        _ring.material = new Material(sh);

        int n = _ring.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ringRadius);
        }
        _ring.enabled = false;
    }

    private void ShowRing(bool on)
    {
        if (_ring != null && _ring.enabled != on) _ring.enabled = on;
    }
}
