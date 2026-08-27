using UnityEngine;

/// <summary>
/// 씬에 떨어지는 경험치 오브젝트
/// </summary>
public class ExpDrop : MonoBehaviour
{
    private int   _amount;
    private float _pickupRadius;
    private Transform _player;
    private bool  _collected;

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float magnetActivationRange = 8f; // 근처 오면 자동 이동

    public void Initialize(int amount)
    {
        _amount    = amount;
        _collected = false;
        _player    = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        var stats  = _player.GetComponent<PlayerStats>();
        float radius = stats != null ? stats.Final.PickupRadius : 2f;

        // 흡수 반경 이내 → 자동 이동
        if (dist < Mathf.Max(radius, magnetActivationRange))
            transform.position = Vector2.MoveTowards(transform.position, _player.position, moveSpeed * Time.deltaTime);

        // 획득
        if (dist < 0.3f) Collect();
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;
        ExperienceManager.Instance.CollectXp(_amount);
        FindFirstObjectByType<ObjectPool>()?.Return(gameObject);
        // 간단하게 비활성화
        gameObject.SetActive(false);
    }
}
