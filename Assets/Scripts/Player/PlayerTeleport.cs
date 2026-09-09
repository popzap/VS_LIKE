using UnityEngine;

/// <summary>
/// 자동 순간이동 — 일정 시간마다 <b>바라보는 쪽</b>으로 훌쩍 건너뛴다
/// (D113 · 사용자 요구 <i>"일정 시간마다 텔레포트 (캐릭터가 향한 방향으로)"</i>).
///
/// <para>🔑 <b>이 게임에서 이건 방어 기술이다.</b> 뱀서라이크에서 플레이어의 유일한
/// 방어 동사는 <i>"걸어서 벗어난다"</i> 인데, 둘러싸이면 걸을 곳이 없다.
/// 순간이동은 <b>걷지 않고 벗어나는</b> 유일한 수단이라 포위를 푼다 (D110 의 미끼와 짝이다).</para>
///
/// <para>🔴 <b>바라보는 쪽 = 마지막으로 움직인 쪽</b>이다
/// (<see cref="PlayerController.LastMoveDir"/>). 지금 누르고 있는 키가 아니다 —
/// 손을 떼도 캐릭터는 어딘가를 향하고 있어야 한다.</para>
///
/// <para>🔴 <b>벽 밖으로는 못 나간다.</b> <see cref="ArenaBounds"/> 가 안쪽으로 붙인다.
/// 안 붙이면 아레나가 있는데도 순간이동만 밖으로 나가 <b>적이 못 따라오는 자리</b>가 생긴다.</para>
///
/// <para>🔵 <b>전투 중에만 돈다.</b> 상점·맵에서도 돌면 UI 를 보는 동안 캐릭터가 혼자 튄다.</para>
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerTeleport : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 3;   // 2 = 발밑 게이지용 창구 · 3 = 도약 프레임 Remaining 복구 (D126)

    [Tooltip("한 번에 건너뛰는 거리(유닛). 기본 이동 속도가 4 이므로 5 면 약 1.25초를 번다.")]
    [SerializeField] private float distance = 5f;

    private PlayerStats      _stats;
    private PlayerController _controller;
    private Rigidbody2D      _rb;
    private float            _timer;

    /// <summary>이 판에서 실제로 뛴 횟수 (검증·통계용).</summary>
    public int JumpCount { get; private set; }

    /// <summary>다음 도약까지 남은 시간(초). 기능이 없으면 0.</summary>
    public float Remaining { get; private set; }

    /// <summary>지금 도약 주기(초). 패시브가 없으면 0 — 곧 <b>기능이 없다</b>는 뜻이다 (D126).</summary>
    public float Interval => _stats != null ? _stats.Final.TeleportInterval : 0f;

    /// <summary>블링크를 가지고 있나. 게이지는 이게 거짓이면 아예 안 그린다 (D126).</summary>
    public bool HasBlink => Interval > 0f;

    /// <summary>
    /// 다음 도약까지의 <b>차오름</b> (0 = 방금 썼다 · 1 = 준비됐다) — 발밑 게이지가 읽는다 (D126).
    ///
    /// <para>🔵 <c>Remaining</c> 을 그대로 안 쓴 이유는 <b>방향</b>이다 — 남은 시간은 줄어들고
    /// 게이지는 차오른다. 뒤집는 자리를 게이지 쪽에 두면 다음에 표시가 하나 더 생길 때 또 뒤집는다.</para>
    /// </summary>
    public float Charge01
    {
        get
        {
            float iv = Interval;
            if (iv <= 0f) return 0f;
            return Mathf.Clamp01(1f - Remaining / iv);
        }
    }

    private void Awake()
    {
        _stats      = GetComponent<PlayerStats>();
        _controller = GetComponent<PlayerController>();
        _rb         = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float interval = _stats != null ? _stats.Final.TeleportInterval : 0f;
        if (interval <= 0f || _stats.IsDead) { _timer = 0f; Remaining = 0f; return; }

        // 🔴 GameManager.Instance 는 Awake 에서 캐시하지 않는다 (I-8/I-38) — 여기서 그때그때 본다.
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameState.Wave) return;   // 🔵 전투 중에만

        _timer += Time.deltaTime;
        Remaining = Mathf.Max(0f, interval - _timer);
        if (_timer < interval) return;

        _timer = 0f;
        // 🔴 도약한 <b>그 프레임</b>에도 남은 시간을 다시 채워 둔다 (D126 실측).
        //    안 하면 Remaining 이 0 인 채로 한 프레임이 지나가서 발밑 게이지가
        //    <b>튄 직후에 가득 찬 것처럼</b> 깜빡인다 — 실제로 그렇게 나왔다(직후 채움 1.00).
        Remaining = interval;
        Jump();
    }

    private void Jump()
    {
        Vector2 dir = _controller != null ? _controller.LastMoveDir : Vector2.right;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;

        Vector2 from = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 to   = ArenaBounds.Clamp(from + dir.normalized * distance);

        // 🔴 위치는 Rigidbody2D 로 옮긴다. transform.position 만 바꾸면 물리가 다음
        //    FixedUpdate 에 옛 자리로 되돌린다 (D111 에서 같은 자리를 밟았다).
        if (_rb != null) _rb.position = to;
        transform.position = to;

        JumpCount++;
        AudioManager.Play(SfxId.Blink);   // D116 · 전용 소리 (Magnet 을 빌려 쓰던 자리 — 뜻이 반대였다)
    }
}
