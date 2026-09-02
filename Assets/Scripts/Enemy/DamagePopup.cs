using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshPro text;
    private ObjectPool _pool;
    private float _lifetime = 0.8f;
    private float _elapsed;

    public void Setup(float dmg, ObjectPool pool)
    {
        _pool   = pool;
        _elapsed = 0;
        text.text = Mathf.RoundToInt(dmg).ToString();
    }

    private void Update()
    {
        // ℹ️ text.alpha 를 매 프레임 쓰면 TextMeshPro 가 버텍스 컬러를 다시 올린다.
        //    실측 개당 1.6 µs · 프레임당 0.12~0.22 ms 로 압도적이지 않았다
        //    (D27 · Docs/PERF.md §8-8).
        _elapsed += Time.deltaTime;
        transform.Translate(Vector3.up * 1.5f * Time.deltaTime);
        float alpha = Mathf.Lerp(1f, 0f, _elapsed / _lifetime);
        text.alpha = alpha;
        if (_elapsed >= _lifetime) _pool.Return(gameObject);
    }
}
