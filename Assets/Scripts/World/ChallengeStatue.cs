using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>도전 석상</b> — 다가가 <c>E</c> 를 누르면 <b>그 자리에서 엘리트가 깨어난다</b>
/// (D123 · 사용자가 고름 · <see cref="RiftEnemy"/> 와 한 쌍).
///
/// <para>🔑 <b>균열과 정반대다.</b> 균열은 <i>"안 가면 나빠지는"</i> 자리(강제)이고,
/// 석상은 <i>"가면 걸 수 있는"</i> 자리(선택)다. 둘 다 한 지점에 발을 묶는데
/// <b>이유가 반대</b>라서 같이 있을 때 판이 재미있어진다.</para>
///
/// <para>🔑 <b>이 게임에 없던 것은 "내가 고른 위험"이다.</b> 지금까지 난이도는
/// 층(<see cref="LayerScaling"/>)과 시간이 정했고 플레이어는 받기만 했다.
/// <c>D121</c>(현상금)이 <b>패시브로</b> 연 그 문을 석상은 <b>자리로</b> 연다 —
/// 준비가 됐다고 느낄 때, 원하는 순간에만 누른다.</para>
///
/// <para>🔵 <b>엘리트를 새로 만들지 않았다.</b> <see cref="WaveManager.SpawnMinion"/> 에
/// <c>isElite</c> 를 넘기면 기존 등급 배율(체력 ×2.5 · 피해 ×1.5 · 경험치 ×3)과
/// <b>보라 외곽선 · 사망 시 보물상자</b>가 전부 그대로 따라온다.</para>
///
/// <para>🔴 <b>콜라이더도 물리도 없다.</b> 적 콜라이더가 <c>isTrigger</c> 라 트리거를 달면
/// 지나가는 모든 것이 들어온다(<c>D117</c>·<c>D119</c> 가 같은 이유로 물리를 버렸다).
/// 필요한 것은 <b>"플레이어가 가까이 있나"</b> 하나뿐이라 거리로 잰다 —
/// <see cref="EvolutionManager"/> 의 제단 판정과 같은 방식이다.</para>
///
/// <para>🔴 <b>반경은 넉넉해야 한다.</b> <c>B18</c> 에서 제단 반경(2.2)이
/// 건물 설치 링(최대 2.51)보다 좁아 <b>E 가 안 닿는</b> 일이 실제로 있었다.</para>
/// </summary>
public class ChallengeStatue : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("도전")]
    [Tooltip("이 거리 안에서 E 를 누르면 발동한다(유닛). 🔴 좁으면 안 닿는다 (B18).")]
    [SerializeField] private float useRadius = 2.8f;

    [Tooltip("깨어나는 엘리트 수.")]
    [SerializeField] private int eliteCount = 1;

    [Tooltip("엘리트가 석상에서 흩어져 나오는 반경(유닛).")]
    [SerializeField] private float spawnScatter = 1.2f;

    [Tooltip("도전을 이겨 냈을 때 주는 런 골드. 🔑 상자는 엘리트가 죽으면서 따로 떨군다.")]
    [SerializeField] private int bountyGold = 40;

    [Header("표시")]
    [Tooltip("쓰고 난 뒤의 색. 🔵 지우지 않고 어둡게 둔다 — '여기는 이미 썼다'가 지도가 된다.")]
    [SerializeField] private Color spentTint = new(0.42f, 0.42f, 0.46f, 1f);

    [Tooltip("아직 쓸 수 있을 때 룬이 숨쉬는 주기(초). 0 이면 안 깜빡인다.")]
    [SerializeField] private float pulsePeriod = 1.6f;

    /// <summary>지금 판에 서 있는 석상들. 디렉터와 안내 UI 가 본다.</summary>
    public static readonly List<ChallengeStatue> Active = new();

    private SpriteRenderer _sr;
    private Color          _baseColor;
    private readonly List<EnemyBase> _summoned = new();
    private bool _used;
    private bool _paid;

    /// <summary>아직 도전할 수 있나 (검증용).</summary>
    public bool Ready => !_used;

    /// <summary>도전이 진행 중인가 — 불러낸 엘리트가 아직 살아 있다 (검증용).</summary>
    public bool Fighting => _used && !_paid;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
    }

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable() => Active.Remove(this);

    /// <summary>
    /// <b>플레이어가 지금 쓸 수 있는 석상</b>을 돌려준다 (없으면 null).
    ///
    /// <para>🔑 안내 UI 와 <see cref="PlayerController"/> 가 <b>같은 함수</b>를 본다 —
    /// 갈라 두면 *"[E] 가 떴는데 눌러도 아무 일이 없다"* 가 생긴다.</para>
    /// </summary>
    public static ChallengeStatue NearestUsable(Vector2 origin)
    {
        ChallengeStatue best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < Active.Count; i++)
        {
            var s = Active[i];
            if (s == null || !s.Ready) continue;
            float sqr = ((Vector2)s.transform.position - origin).sqrMagnitude;
            if (sqr > s.useRadius * s.useRadius || sqr >= bestSqr) continue;
            best = s; bestSqr = sqr;
        }
        return best;
    }

    /// <summary>가장 가까운 석상에 도전한다. 눌렀는데 아무 석상도 없으면 <c>false</c>.</summary>
    public static bool TryChallenge(Vector2 origin)
    {
        var s = NearestUsable(origin);
        if (s == null) return false;
        return s.Challenge();
    }

    private bool Challenge()
    {
        if (_used) return false;

        var gm = GameManager.Instance;
        if (gm == null || gm.WaveManager == null) return false;   // 🔴 ?. 금지 (I-24)
        var wm = gm.WaveManager;
        if (!wm.IsWaveActive) return false;

        var data = wm.ChallengeEnemy();
        if (data == null) return false;

        _used = true;
        for (int i = 0; i < Mathf.Max(1, eliteCount); i++)
        {
            Vector2 at = (Vector2)transform.position + Random.insideUnitCircle * spawnScatter;
            if (ArenaBounds.Enabled) at = ArenaBounds.Clamp(at, 1f);

            // 🔑 상한을 무시한다 — 내가 부른 것이 판이 붐빈다는 이유로 안 나오면
            //    누른 사람 입장에서는 <b>버튼이 고장 난 것</b>이다.
            var e = wm.SpawnMinion(data, at, ignoreCap: true, isElite: true);
            if (e != null) _summoned.Add(e);
        }

        if (_summoned.Count == 0) { _used = false; return false; }   // 하나도 못 불렀으면 되돌린다

        AudioManager.Play(SfxId.BossAppear);   // 🟡 자리표시 — 석상 전용 소리는 아직 없다
        Tint(spentTint);
        return true;
    }

    /// <summary>
    /// 🔵 불러낸 엘리트가 <b>다 죽었을 때</b> 한 번만 보상을 준다.
    ///
    /// <para>🔴 엘리트 쪽에 콜백을 달지 않는다 — 적은 풀에서 재사용되므로
    /// 죽음 이벤트를 걸어 두면 <b>다음 생애의 적</b>이 이 석상에 보상을 물어 온다.
    /// 여기서 <b>내가 부른 목록만</b> 들여다보는 쪽이 안전하다.</para>
    /// </summary>
    private void Update()
    {
        if (!_used)
        {
            Pulse();
            return;
        }
        if (_paid) return;

        for (int i = _summoned.Count - 1; i >= 0; i--)
        {
            var e = _summoned[i];
            // 🔴 풀로 돌아가 비활성이 된 것도 "없어진" 것으로 본다.
            if (e == null || e.Dead || !e.isActiveAndEnabled) _summoned.RemoveAt(i);
        }
        if (_summoned.Count > 0) return;

        _paid = true;
        var gm = GameManager.Instance;
        if (gm != null && bountyGold > 0) gm.GrantGold(bountyGold);
        AudioManager.Play(SfxId.ChestOpen);    // 🟡 자리표시
    }

    /// <summary>🔵 아직 쓸 수 있는 석상만 숨을 쉰다 — 그게 "여기 눌러도 된다"는 유일한 신호다.</summary>
    private void Pulse()
    {
        if (_sr == null || pulsePeriod <= 0f) return;
        float k = Mathf.PingPong(Time.time / pulsePeriod, 1f);
        var c = _baseColor;
        c.r = Mathf.Lerp(_baseColor.r, 1f, k * 0.35f);
        c.g = Mathf.Lerp(_baseColor.g, 0.85f, k * 0.2f);
        c.b = Mathf.Lerp(_baseColor.b, 0.85f, k * 0.2f);
        _sr.color = c;
    }

    private void Tint(Color c)
    {
        if (_sr != null) _sr.color = c;
    }
}
