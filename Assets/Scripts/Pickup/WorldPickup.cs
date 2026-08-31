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
    Gold
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

    [Header("Gold")]
    [Tooltip("골드 픽업 금액. GoldGain 배율이 여기에 곱해진다")]
    [SerializeField] private int goldAmount = 15;

    private Transform _player;
    private bool      _collected;

    private void OnEnable()
    {
        // 풀에서 재사용되므로 Initialize 를 따로 두지 않고 켜질 때마다 초기화한다.
        _collected = false;
        var p = GameObject.FindGameObjectWithTag("Player");
        _player = p != null ? p.transform : null;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        // 레벨업 패널이 떠 있는 동안에는 먹히지 않게 한다.
        // timeScale 이 0 이어도 Update 는 계속 돌기 때문에, 막지 않으면
        // 상자를 밟은 순간 패널이 이미 떠 있는 상태에서 또 한 번 열려 상태가 꼬인다.
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.LevelUp) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        var   stats  = _player.GetComponent<PlayerStats>();
        float radius = stats != null ? stats.Final.PickupRadius : 2f;

        if (dist < radius)
            transform.position = Vector2.MoveTowards(
                transform.position, _player.position, moveSpeed * Time.deltaTime);

        if (dist < touchRadius) Collect();
    }

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

            case PickupKind.Invincible:
                AudioManager.Play(SfxId.BuffPickup);
                if (PlayerStats.Current != null)
                    PlayerStats.Current.GrantInvincibility(invincibleDuration);
                break;

            case PickupKind.Haste:
                AudioManager.Play(SfxId.BuffPickup);
                if (PlayerStats.Current != null)
                    PlayerStats.Current.GrantHaste(hasteDuration, hasteAttackSpeed);
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
        var pool = FindFirstObjectByType<ObjectPool>();
        if (pool != null) pool.Return(gameObject);
        else              gameObject.SetActive(false);
    }
}
