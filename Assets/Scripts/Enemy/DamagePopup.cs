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
        _elapsed += Time.deltaTime;
        transform.Translate(Vector3.up * 1.5f * Time.deltaTime);
        float alpha = Mathf.Lerp(1f, 0f, _elapsed / _lifetime);
        text.alpha = alpha;
        if (_elapsed >= _lifetime) _pool.Return(gameObject);
    }
}
