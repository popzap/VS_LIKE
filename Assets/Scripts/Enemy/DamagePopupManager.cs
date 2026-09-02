using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private ObjectPool pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Show(Vector2 worldPos, float damage)
    {
        if (popupPrefab == null) return;
        var go = pool.Get(popupPrefab, worldPos + Random.insideUnitCircle * 0.3f, Quaternion.identity);
        go.GetComponent<DamagePopup>()?.Setup(damage, pool);
    }
}
