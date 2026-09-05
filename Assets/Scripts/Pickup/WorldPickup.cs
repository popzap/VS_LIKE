using UnityEngine;

/// <summary>픽업의 종류. 프리팹마다 하나씩 고정해 둔다.</summary>
public enum PickupKind
{
    /// <summary>보물상자. 엘리트/보스가 떨군다. 먹으면 아이템 카드를 한 번 더 고른다.</summary>
    Chest,
    /// <summary>자석. 필드에 흩어진 경험치를 전부 끌어온다.</summary>
    Magnet,
    /// <summary>폭탄. 화면 안의 적을 한 번에 때린다. 지우지는 않는다 — 보스급은 살아남는다.</summary>
    Bomb,
    /// <summary>무적. 몇 초 동안 아무 피해도 안 받는다. <b>탈출용</b>이지 전투용이 아니다.</summary>
    Invincible,
    /// <summary>공속. 몇 초 동안 무기 쿨다운이 짧아진다.</summary>
    Haste,
    /// <summary>골드. 즉시 재화가 들어온다.</summary>
    Gold,
    /// <summary>
    /// 이동 속도. 몇 초 동안 빨라진다 (D68 · 사용자 요구 12에서 *"이속증가(추가해)"*).
    /// <b>enum 은 정수 직렬화라 맨 뒤에 붙였다</b> — 중간에 끼우면 기존 프리팹의 kind 가 밀린다.
    /// </summary>
    Swift
}

/// <summary>
/// 바닥에 떨어져 있다가 플레이어가 다가오면 획득되는 물건.
///
/// <para>경험치 구슬(<see cref="ExpDrop"/>)과 로직이 비슷하지만 합치지 않았다.
/// 구슬은 초당 수십 개가 생겼다 사라지는 "숫자"고, 이쪽은 한 판에 몇 개 안 나오는
/// "사건"이라 획득 시 하는 일(패널 열기·시간 정지)이 완전히 다르다.</para>
///
/// <para>끌려오는 범위는 <see cref="StatBlock.PickupRadius"/> 를 그대로 쓴다.
/// 구슬처럼 8유닛짜리 넉넉한 보정을 주지 않는 이유는, 상자는 <b>일부러 가지러 가는</b>
/// 목표물이어야 하기 때문이다. 알아서 날아오면 화면에 놓아 둔 의미가 없다.</para>
/// </summary>
public class WorldPickup : MonoBehaviour
{
    [SerializeField] private PickupKind kind;

    [Tooltip("이 거리 안에 들어오면 획득한다")]
    [SerializeField] private float touchRadius = 0.6f;

    [Tooltip("끌려오는 속도")]
    [SerializeField] private float moveSpeed = 5f;

    // ── 종류별 수치 ──────────────────────────────────────────────
    //
    // 프리팹마다 kind 가 하나로 고정돼 있으므로, 자기 kind 가 안 쓰는 칸은 그냥 비어 있다.
    // 종류마다 클래스를 나누지 않은 이유는 하는 일이 "먹으면 한 번 터진다" 하나뿐이고,
    // 나누면 프리팹 4개에 스크립트 4개가 따로 붙어 배선 실수가 늘기 때문이다.

    [Header("Bomb")]
    [Tooltip("화면 폭탄의 피해량. Demon(160+방어4)은 죽고 Ogre(220)는 살아남는 값이 기준이다")]
    [SerializeField] private float bombDamage = 200f;

    [Tooltip("화면 폭탄의 반경(유닛). ortho 6 기준 화면 반높이 6 · 반너비 10.67")]
    [SerializeField] private float bombRadius = 10f;

    [Header("Invincible / Haste")]
    [Tooltip("무적 지속(초). 5초를 넘기지 말 것 — 넘으면 회피가 의미를 잃는다")]
    [SerializeField] private float invincibleDuration = 3f;

    [Tooltip("공속 지속(초)")]
    [SerializeField] private float hasteDuration = 8f;

    [Tooltip("공속 버프의 쿨다운 배율 가산분. 음수가 빠름 (-0.5 = 쿨다운 절반)")]
    [SerializeField] private float hasteAttackSpeed = -0.5f;

    [Header("Swift (D68)")]
    [Tooltip("이속 버프 지속(초).")]
    [SerializeField] private float swiftDuration = 8f;

    [Tooltip("이속 버프의 이동속도 가산분(유닛/초). 기본 MoveSpeed 4 기준 +2 는 1.5배다.")]
    [SerializeField] private float swiftMoveSpeed = 2f;

    [Header("Gold")]
    [Tooltip("골드 픽업 금액. GoldGain 배율이 여기에 곱해진다")]
    [SerializeField] private int goldAmount = 15;

    private Transform   _player;
    private PlayerStats _playerStats;   // 🔴 매 프레임 GetComponent 하지 않는다 (D27)
    private bool        _collected;

    private void OnEnable()
    {
        // 풀에서 재사용되므로 Initialize 를 따로 두지 않고 켜질 때마다 초기화한다.
        _collected = false;
        var p = GameObject.FindGameObjectWithTag("Player");
        _player = p != null ? p.transform : null;

        // 여기서 한 번만 잡는다. 켜질 때마다 다시 잡으므로 플레이어가 교체돼도 따라간다.
        _playerStats = _player != null ? _player.GetComponent<PlayerStats>() : null;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        // 레벨업 패널이 떠 있는 동안에는 먹히지 않게 한다.
        // timeScale 이 0 이어도 Update 는 계속 돌기 때문에, 막지 않으면
        // 상자를 밟은 순간 패널이 이미 떠 있는 상태에서 또 한 번 열려 상태가 꼬인다.
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.LevelUp) return;

        // sqrt 를 쓰지 않는다 — 거리는 비교에만 쓰인다. 스탯도 캐시된 것을 쓴다.
        float distSqr = ((Vector2)transform.position - (Vector2)_player.position).sqrMagnitude;
        float radius  = _playerStats != null ? _playerStats.Final.PickupRadius : 2f;

        if (distSqr < radius * radius)
            transform.position = Vector2.MoveTowards(
                transform.position, _player.position, moveSpeed * Time.deltaTime);

        if (distSqr < touchRadius * touchRadius) Collect();
    }

    /// <summary>필드 드랍 버프의 지속시간 배율 (D68). 특전이 없으면 1 이다.</summary>
    private static float BuffMult =>
        PlayerStats.Current != null ? PlayerStats.Current.BuffDurationMult : 1f;

    private void Collect()
    {
        _collected = true;

        switch (kind)
        {
            case PickupKind.Chest:
                AudioManager.Play(SfxId.ChestOpen);
                ExperienceManager.Instance.GrantChestReward();
                break;

            case PickupKind.Magnet:
                AudioManager.Play(SfxId.Magnet);
                ExpDrop.PullAllToPlayer();
                break;

            case PickupKind.Bomb:
                AudioManager.Play(SfxId.BombPickup);
                DetonateBomb();
                break;

            // 🔑 지속시간에 진화 특전 배율을 곱한다 (D68 · DoubleBuffDuration).
            //    🔴 곱셈은 여기서 한 번만 한다 — PlayerStats 안에서 또 곱하면
            //    겹쳐 먹을 때 Mathf.Max 가 이미 늘어난 값과 비교해 배율이 두 번 먹는다.
            case PickupKind.Invincible:
                AudioManager.Play(SfxId.BuffPickup);
                if (PlayerStats.Current != null)
                    PlayerStats.Current.GrantInvincibility(invincibleDuration * BuffMult);
                break;

            case PickupKind.Haste:
                AudioManager.Play(SfxId.BuffPickup);
                if (PlayerStats.Current != null)
                    PlayerStats.Current.GrantHaste(hasteDuration * BuffMult, hasteAttackSpeed);
                break;

            case PickupKind.Swift:
                AudioManager.Play(SfxId.BuffPickup);
                if (PlayerStats.Current != null)
                    PlayerStats.Current.GrantSwift(swiftDuration, swiftMoveSpeed);
                break;

            case PickupKind.Gold:
                AudioManager.Play(SfxId.GoldPickup);
                if (GameManager.Instance != null) GameManager.Instance.GrantGold(goldAmount);
                break;
        }

        Despawn();
    }

    /// <summary>
    /// 화면 안의 적을 한 번에 때린다.
    ///
    /// <para>폭심은 <b>플레이어</b>이지 픽업이 아니다. 픽업은 끌려오다 먹히므로 터지는 순간의
    /// 위치가 플레이어 근처이긴 하지만 정확히 같지는 않고, "화면을 쓸었다"로 읽히려면
    /// 원의 중심이 화면 중심(= 카메라가 따라다니는 플레이어)이어야 한다.</para>
    /// </summary>
    private void DetonateBomb()
    {
        Vector2 center = _player != null ? (Vector2)_player.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, bombRadius, LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<EnemyBase>();
            // 넉백 방향을 위해 폭심을 넘긴다 — 전부 바깥으로 밀린다.
            if (enemy != null) enemy.TakeDamage(bombDamage, center);
        }
    }

    private void Despawn()
    {
        // 🔴 씬 전체 순회를 하지 않는다 (D27). EnemyBase 가 같은 이유로 이미
        //    캐시를 쓰고 있었는데(EnemyBase.cs:598 주석) 여기엔 안 옮겨져 있었다.
        if (SharedPool != null) SharedPool.Return(gameObject);
        else                    gameObject.SetActive(false);
    }

    // ── 오브젝트 풀 ──────────────────────────────────────────────
    //
    // 파괴된 오브젝트는 Unity 가 == null 을 true 로 만들어 주므로 씬을 다시 로드해도
    // 알아서 다시 찾는다. (?. 는 그 판정을 못 한다 — I-24)

    private static ObjectPool _sharedPool;

    private static ObjectPool SharedPool
    {
        get
        {
            if (_sharedPool == null) _sharedPool = FindFirstObjectByType<ObjectPool>();
            return _sharedPool;
        }
    }
}
