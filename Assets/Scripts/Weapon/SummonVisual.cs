using UnityEngine;

/// <summary>
/// 소환수 몸통의 그림 담당. 16프레임을 끊임없이 돌리고, 진행 방향에 따라 좌우만 뒤집는다.
///
/// <para>🔴 <see cref="EnemyVisual"/> 을 재사용하지 않는다. 그쪽은 <c>Rigidbody2D</c> 의
/// 속도를 읽어 프레임을 넘기는데, 소환수에는 강체가 없다. 대신 <see cref="SummonWeapon"/> 이
/// 매 프레임 <see cref="SetVelocity"/> 로 실제 이동량을 넣어 준다.</para>
///
/// <para>🔴 <b>위아래 흔들림을 코드로 넣지 않는다.</b> 드래곤 ±1.6px · 문어 ±1.5px 가
/// 그림에 이미 그려져 있다. 코드가 또 흔들면 두 번 흔들린다.</para>
///
/// <para>ping-pong 도 필요 없다. 16프레임이 <c>sin(2π·i/16)</c> 이라 15 → 0 이 이음매 없이 붙는다.</para>
/// </summary>
public class SummonVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [Tooltip("순환 재생할 16프레임. 순서대로 넣어야 한다.")]
    [SerializeField] private Sprite[] frames;
    [Tooltip("초당 프레임. 16장이므로 12 면 한 바퀴가 약 1.33초다.")]
    [SerializeField] private float frameRate = 12f;
    [Tooltip("이보다 느리게 움직이면 방향을 안 바꾼다. 제자리에서 덜덜 떠는 걸 막는다.")]
    [SerializeField] private float flipDeadzone = 0.15f;

    private float _timer;
    private int   _index;
    private bool  _facingRight = true;

    /// <summary>
    /// 풀에서 다시 꺼내질 때 이전 소환수의 프레임·방향이 남아 있으면 안 된다.
    /// 시작 프레임은 일부러 흩뜨린다 — 소환수 여러 마리가 같은 박자로 날갯짓하면 복제처럼 보인다.
    /// </summary>
    private void OnEnable()
    {
        _timer       = 0f;
        _index       = (frames != null && frames.Length > 0) ? Random.Range(0, frames.Length) : 0;
        _facingRight = true;

        if (sr != null)
        {
            sr.flipX = false;
            if (frames != null && frames.Length > 0) sr.sprite = frames[_index];
        }
    }

    /// <summary>이동 속도를 알려 준다. 좌우 뒤집기에만 쓴다 — 프레임 속도는 바꾸지 않는다.</summary>
    public void SetVelocity(Vector2 v)
    {
        if (sr == null) return;
        if (Mathf.Abs(v.x) < flipDeadzone) return;

        bool right = v.x > 0f;
        if (right == _facingRight) return;

        _facingRight = right;
        sr.flipX     = !right;
    }

    private void Update()
    {
        if (sr == null || frames == null || frames.Length == 0 || frameRate <= 0f) return;

        _timer += Time.deltaTime;
        float step = 1f / frameRate;
        if (_timer < step) return;

        // 프레임이 크게 튀어도 밀리지 않게 넘어간 만큼을 한 번에 소화한다.
        int advance = Mathf.FloorToInt(_timer / step);
        _timer -= advance * step;

        _index    = (_index + advance) % frames.Length;
        sr.sprite = frames[_index];
    }
}
