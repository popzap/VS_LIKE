using System.Collections;
using UnityEngine;

/// <summary>
/// 폭발 그 자체 — 생성되는 순간 주변에 피해를 주고, 폭발 스프라이트를 한 바퀴 재생한 뒤
/// 풀로 돌아간다.
///
/// <para><b>날아가지 않는다.</b> 이동은 <see cref="BombProjectile"/> 이 하고, 도착하면
/// 이 오브젝트를 꺼내 <see cref="Initialize"/> 를 부른다. <see cref="AoeWeapon"/> 처럼
/// 목표 지점에 바로 터뜨리는 쪽은 이 오브젝트만 쓴다.</para>
/// </summary>
public class AoeProjectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    [Header("폭발 애니메이션")]
    [Tooltip("순서대로 재생할 프레임. 비워 두면 스프라이트를 바꾸지 않고 fallbackDuration 만큼만 떠 있는다.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;
    [SerializeField] private float fallbackDuration = 0.15f;
    [Tooltip("마지막 몇 할을 알파로 사라지게 할지. 시트 마지막 프레임이 연기 덩어리라 그냥 끄면 툭 사라진다.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeOutPortion = 0.4f;

    [Header("크기")]
    [Tooltip("scale 1 일 때 이 스프라이트가 덮는 반경(월드 유닛). 폭발 반경에 맞춰 자동으로 확대된다.")]
    [SerializeField] private float spriteRadiusAtScaleOne = 0.5f;

    private float      _damage;
    private float      _radius;
    private ObjectPool _pool;

    public void Initialize(float dmg, float radius, ObjectPool pool)
    {
        _damage = dmg; _radius = radius; _pool = pool;

        if (spriteRadiusAtScaleOne > 0f)
            transform.localScale = Vector3.one * (radius / spriteRadiusAtScaleOne);

        StartCoroutine(Explode());
    }

    private IEnumerator Explode()
    {
        // 피해는 터지는 순간 바로 넣는다. 연출이 끝난 뒤에 넣으면 그 사이에
        // 폭심을 지나쳐 빠져나간 적이 그냥 살아남는다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius,
                            LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<EnemyBase>();
            // 폭심에서 바깥으로 밀어낸다.
            if (enemy != null) enemy.TakeDamage(_damage, transform.position);
        }

        if (frames != null && frames.Length > 0 && sr != null && frameRate > 0f)
        {
            float step  = 1f / frameRate;
            int fadeFrom = Mathf.Clamp(Mathf.CeilToInt(frames.Length * (1f - fadeOutPortion)),
                                       1, frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                sr.color  = new Color(1f, 1f, 1f,
                            i < fadeFrom ? 1f
                                         : 1f - (i - fadeFrom + 1f) / (frames.Length - fadeFrom + 1f));
                yield return new WaitForSeconds(step);
            }
            sr.color = Color.white;   // 다음에 풀에서 꺼내 쓸 때를 위해 되돌린다
        }
        else
        {
            yield return new WaitForSeconds(fallbackDuration);
        }

        _pool.Return(gameObject);
    }
}
