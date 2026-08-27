using UnityEngine;
using UnityEngine.InputSystem;

// ────────────────────────────────────────────────────────────────────────────
//  PlayerController  —  이동 + 무기/건물 관리
// ────────────────────────────────────────────────────────────────────────────
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;

    [Header("피격 반응")]
    [Tooltip("넉백 초기 속도. 이동 속도(기본 4)보다 커야 밀려나는 게 보인다.")]
    [SerializeField] private float knockbackForce = 9f;
    [Tooltip("넉백 동안 이동 입력이 막히는 시간(초).")]
    [SerializeField] private float knockbackTime  = 0.15f;
    [Tooltip("맞은 직후 붉게 물드는 시간(초). 이후에는 무적이 끝날 때까지 깜빡인다.")]
    [SerializeField] private float hitFlashTime   = 0.12f;
    [SerializeField] private float blinkInterval  = 0.07f;
    [SerializeField] private Color hitColor       = new(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float shakeMagnitude = 0.18f;

    private PlayerStats    _stats;
    private WeaponManager  _weaponManager;
    private BuildingManager _buildingManager;
    private CameraController _camera;
    private bool _inputEnabled = true;

    // 피격 연출 상태
    private float _knockbackTimer;
    private float _hitTimer;      // 남은 연출 시간 (= 무적 시간)
    private float _hitDuration;   // 연출 총 길이 (경과 시간 계산용)
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _stats           = GetComponent<PlayerStats>();
        _weaponManager   = GetComponent<WeaponManager>();
        _camera          = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (sr != null) _baseColor = sr.color;
    }

    private void Update()
    {
        // 입력이 막혀 있거나 죽은 뒤에도 색은 원래대로 돌려놔야 하므로 먼저 처리한다.
        TickHitFeedback();

        if (!_inputEnabled || _stats.IsDead) return;

        var kb = Keyboard.current;

        // ── 이동 ────────────────────────────────────────────
        Vector2 input = Vector2.zero;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  input.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    input.y += 1f;
        }
        // 넉백 중에는 이동 입력을 무시한다. 그러지 않으면 아래 한 줄이 넉백 속도를
        // 곧바로 덮어써서 밀려나는 게 한 프레임도 보이지 않는다.
        if (_knockbackTimer > 0f)
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero,
                                                    knockbackForce / knockbackTime * Time.deltaTime);
        else
            rb.linearVelocity = input.normalized * _stats.Final.MoveSpeed;

        // ── 스프라이트 반전 ──────────────────────────────────
        if (input.x != 0) sr.flipX = input.x < 0;

        // ── 건물 설치 (Z) ────────────────────────────────────
        // 고르는 단계가 없다. 대기열 맨 앞(가장 먼저 얻은 건물)이 바로 옆에 선다.
        if (kb != null && kb.zKey.wasPressedThisFrame)
        {
            var bm = BuildingMgr;
            if (bm != null) bm.PlaceNext(transform.position);
        }
    }

    /// <summary>
    /// <see cref="BuildingManager"/> 를 처음 쓸 때 찾아서 캐시한다.
    ///
    /// <para><b>Awake 에서 잡으면 안 된다.</b> <see cref="GameManager.BuildingMgr"/> 는
    /// <c>GameManager.Start()</c> 에서 채워지는데 모든 <c>Awake</c> 는 모든 <c>Start</c> 보다
    /// 먼저 돌기 때문에 늘 null 이 잡힌다. 그래서 Z 키 설치가 통째로 죽어 있었다 (I-38).</para>
    /// </summary>
    private BuildingManager BuildingMgr
    {
        get
        {
            if (_buildingManager != null) return _buildingManager;

            var gm = GameManager.Instance;
            _buildingManager = gm != null ? gm.BuildingMgr : null;
            if (_buildingManager == null)
                _buildingManager = FindFirstObjectByType<BuildingManager>();

            return _buildingManager;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>직업별 외형 교체. <see cref="PlayerStats.ApplyClass"/> 가 호출한다.</summary>
    public void ApplyBodySprite(Sprite sprite)
    {
        if (sr == null || sprite == null) return;
        sr.sprite = sprite;
    }

    // ── 피격 연출 ────────────────────────────────────────────

    /// <summary>피격 시 <see cref="PlayerStats.TryTakeHit"/> 가 호출한다.</summary>
    /// <param name="dir">가해자로부터 멀어지는 방향 (정규화됨).</param>
    /// <param name="invincibleTime">깜빡임을 유지할 시간 = 무적 시간.</param>
    public void PlayHitFeedback(Vector2 dir, float invincibleTime)
    {
        _knockbackTimer = knockbackTime;
        _hitDuration    = invincibleTime;
        _hitTimer       = invincibleTime;

        rb.linearVelocity = dir * knockbackForce;
        _camera?.Shake(shakeMagnitude, 0.2f);
    }

    private void TickHitFeedback()
    {
        if (_knockbackTimer > 0f) _knockbackTimer -= Time.deltaTime;
        if (_hitTimer <= 0f) return;

        _hitTimer -= Time.deltaTime;
        if (sr == null) return;

        if (_hitTimer <= 0f) { sr.color = _baseColor; return; }

        float elapsed = _hitDuration - _hitTimer;
        if (elapsed < hitFlashTime)
        {
            sr.color = hitColor;                        // 맞은 순간 — 붉게
        }
        else
        {
            // 무적이 끝날 때까지 깜빡여서 "지금은 안 맞는다"를 알린다
            var c = _baseColor;
            c.a = Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0 ? 0.35f : 1f;
            sr.color = c;
        }
    }

    // 경험치 흡수 범위 기즈모
    private void OnDrawGizmosSelected()
    {
        if (_stats == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _stats.Final.PickupRadius);
    }
}