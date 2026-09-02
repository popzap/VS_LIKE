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
        // 🔬 D27 계측 (임시). ExpDrop 이 개당 0.9 µs 로 나와 BehaviourUpdate 를
        //    설명하지 못했다 — 남은 후보가 여기다. TextMeshPro.alpha 를 매 프레임
        //    쓰면 버텍스 컬러가 다시 올라간다.
        long _t0 = PerfCounters.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;

        _elapsed += Time.deltaTime;
        transform.Translate(Vector3.up * 1.5f * Time.deltaTime);
        float alpha = Mathf.Lerp(1f, 0f, _elapsed / _lifetime);
        text.alpha = alpha;

        if (PerfCounters.On)
        {
            PerfCounters.PopupUpdateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _t0;
            PerfCounters.PopupUpdateCalls++;
        }

        if (_elapsed >= _lifetime) _pool.Return(gameObject);
    }
}
