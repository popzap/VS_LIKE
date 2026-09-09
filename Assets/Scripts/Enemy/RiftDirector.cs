using UnityEngine;

/// <summary>
/// <b>균열을 언제·어디에 여는지</b> 정한다 (D122).
///
/// <para>🔵 <see cref="RiftEnemy"/> 와 갈라 놓은 이유 — 균열 <b>하나</b>가 하는 일(뱉기·닫히기)과
/// 균열을 <b>여는</b> 일은 수명이 다르다. 균열은 죽으면 사라지지만 여는 쪽은 판 내내 살아 있어야 한다.
/// <see cref="BuildingManager"/> 와 건물의 관계와 같다.</para>
///
/// <para>🔴 <b>보스 웨이브에는 안 연다.</b> 보스전은 이미 그 층의 내용물이고,
/// 거기에 균열까지 얹으면 <b>화면을 볼 수가 없다.</b></para>
///
/// <para>🔴 <b>플레이어 발밑에는 안 연다.</b> 균열은 *"가야 하는 곳"* 이라야 한다 —
/// 서 있는 자리에 열리면 그냥 "적이 더 나온다"가 되고, 지리는 또 사라진다.</para>
/// </summary>
public class RiftDirector : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("무엇을 여는가")]
    [Tooltip("균열의 EnemyData. Enemies.csv 의 Rift 행이며 SceneWiring.csv 가 채운다.")]
    [SerializeField] private EnemyData riftData;

    [Header("언제")]
    [Tooltip("웨이브가 시작되고 첫 균열까지(초). 🔴 시작하자마자 열리면 무기도 없이 마주친다.")]
    [SerializeField] private float firstDelay = 25f;

    [Tooltip("그다음부터 여는 간격(초).")]
    [SerializeField] private float interval = 40f;

    [Tooltip("동시에 열려 있을 수 있는 수. 🔴 늘리면 '어디로 갈까'가 아니라 '어디로 도망갈까'가 된다.")]
    [SerializeField] private int maxActive = 2;

    [Header("어디에")]
    [Tooltip("플레이어로부터 이만큼은 떨어뜨린다(유닛). 화면 세로 절반이 약 6 이다.")]
    [SerializeField] private float minDistance = 9f;

    [Tooltip("이보다 멀리는 안 연다 — 찾을 수 없는 균열은 없는 것과 같다.")]
    [SerializeField] private float maxDistance = 14f;

    [Tooltip("균열끼리 이만큼은 떨어뜨린다(유닛).")]
    [SerializeField] private float riftSpacing = 8f;

    [Tooltip("자리를 못 찾으면 몇 번까지 다시 뽑나.")]
    [SerializeField] private int placeTries = 12;

    private float _nextOpenAt = -1f;

    /// <summary>지금 열려 있는 균열 수 (검증용).</summary>
    public int ActiveRifts => RiftEnemy.Active.Count;

    /// <summary>다음 균열까지 남은 초 (검증용). 웨이브가 없으면 -1.</summary>
    public float SecondsToNext => _nextOpenAt < 0f ? -1f : Mathf.Max(0f, _nextOpenAt - Time.time);

    private void Update()
    {
        // 🔴 GameManager.Instance.WaveManager 를 Awake 에서 캐시하지 않는다 (I-8/I-38).
        var gm = GameManager.Instance;
        if (gm == null || gm.WaveManager == null) return;
        var wm = gm.WaveManager;

        if (!wm.IsWaveActive || wm.IsBossWave)
        {
            _nextOpenAt = -1f;                 // 웨이브가 끝나면 시계를 처음으로 되돌린다
            return;
        }

        if (_nextOpenAt < 0f) { _nextOpenAt = Time.time + Mathf.Max(0f, firstDelay); return; }
        if (Time.time < _nextOpenAt) return;

        // 🔴 상한에 걸렸으면 <b>시계를 미룬다</b> — 여기서 그냥 return 하면
        //    하나가 닫히는 순간 밀린 것들이 한꺼번에 열린다.
        _nextOpenAt = Time.time + Mathf.Max(5f, interval);
        if (RiftEnemy.Active.Count >= Mathf.Max(1, maxActive)) return;

        TryOpen(wm);
    }

    private void TryOpen(WaveManager wm)
    {
        if (riftData == null || riftData.Prefab == null) return;

        var player = PlayerStats.Current;
        if (player == null) return;
        Vector2 p = player.transform.position;

        for (int i = 0; i < Mathf.Max(1, placeTries); i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            Vector2 at = p + dir * Random.Range(minDistance, Mathf.Max(minDistance, maxDistance));

            // 🔴 아레나 밖에 열면 <b>닿을 수 없는 균열</b>이 된다 — 벽이 플레이어를 막는다 (D111).
            if (ArenaBounds.Enabled)
            {
                Vector2 inside = ArenaBounds.Clamp(at);
                // 밀려 들어온 자리가 플레이어와 너무 가까워졌으면 그 방향은 버린다.
                if (Vector2.Distance(inside, p) < minDistance * 0.8f) continue;
                at = inside;
            }

            if (TooCloseToOtherRift(at)) continue;

            // 🔑 상한을 무시하고 연다 — 균열은 잡몹이 아니라 <b>잡몹이 나오는 자리</b>다.
            var spawned = wm.SpawnMinion(riftData, at, ignoreCap: true);
            // 🟡 자리표시 — 균열이 열리는 전용 소리는 아직 없다 (기습 시작음을 빌린다).
            if (spawned != null) AudioManager.Play(SfxId.AmbushStart);
            return;
        }
    }

    private bool TooCloseToOtherRift(Vector2 at)
    {
        float sqr = riftSpacing * riftSpacing;
        var list = RiftEnemy.Active;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            if (((Vector2)list[i].transform.position - at).sqrMagnitude < sqr) return true;
        }
        return false;
    }
}
