using System.Collections;
using UnityEngine;

/// <summary>
/// 휘두름 호 한 번. 6프레임을 순서대로 재생하고 풀로 돌아간다.
///
/// <para><b>피해는 주지 않는다.</b> 판정은 <see cref="MeleeWeapon"/> 이 한 번만 굴린다 —
/// 여기서도 때리면 연타와 겹쳐 두 배로 들어간다.</para>
///
/// <para>🔴 <b>회전 중심이 프레임 중앙이고, 호는 오른쪽 반쪽에만 그려져 있다.</b>
/// 그래서 이 오브젝트를 플레이어 자리에 놓고 <c>z</c> 만 적 방향으로 돌리면
/// 그 방향을 베는 그림이 된다. 위치나 피벗을 건드리면 호가 엉뚱한 데서 돈다.</para>
/// </summary>
public class SwingArcFx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    [Header("애니메이션")]
    [Tooltip("왼쪽 → 오른쪽 순서 그대로. SwingArc_0 이 치켜든 순간, SwingArc_5 가 다 벤 순간이다.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 30f;

    [Header("크기")]
    [Tooltip("scale 1 일 때 호의 바깥 반지름(월드 유닛). 사거리에 맞춰 자동으로 확대된다. " +
             "그림의 실측값이다 — 셀 256px / 바깥 반지름 107.4px / PPU 100 = 1.074.")]
    [SerializeField] private float spriteRadiusAtScaleOne = 1.074f;

    private ObjectPool _pool;

    /// <param name="radius">호의 바깥 끝이 닿을 거리. 무기의 실제 사거리를 넘긴다.</param>
    public void Initialize(float radius, ObjectPool pool)
    {
        _pool = pool;

        // 그림이 사거리를 속이면 안 된다. 바깥 끝이 정확히 radius 에 오도록 맞춘다.
        if (spriteRadiusAtScaleOne > 0f)
            transform.localScale = Vector3.one * (radius / spriteRadiusAtScaleOne);

        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        if (frames != null && frames.Length > 0 && sr != null && frameRate > 0f)
        {
            float step = 1f / frameRate;
            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(step);
            }
        }

        _pool.Return(gameObject);
    }
}
