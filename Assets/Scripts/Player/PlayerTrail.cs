using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 <b>지나온 길</b>을 짧게 기억한다. 소환수가 꼬리처럼 그 길을 되짚어 오게 하려고 둔다 (D19).
///
/// <para>왜 "플레이어 기준 오프셋"이 아니라 길인가 — 오프셋은 플레이어가 방향을 꺾어도
/// 소환수가 <b>옆으로 미끄러진다.</b> 길을 되짚으면 꺾인 자리에서 같이 꺾여서
/// 뱀/기차처럼 보인다. 이게 사용자가 말한 "꼬리처럼 따라오는" 그림이다.</para>
///
/// <para>🔴 <b>씬에 배선하지 않는다.</b> <see cref="Of"/> 가 없으면 붙인다.
/// 소환수는 무기 획득 시점에 생기는데 그때 씬 배선이 돼 있으리라는 보장이 없고,
/// 배선을 요구하면 <c>SceneWiring.csv</c>(CONTENT 소유)까지 건드려야 한다.</para>
/// </summary>
[DisallowMultipleComponent]
public class PlayerTrail : MonoBehaviour
{
    [Tooltip("이만큼 움직여야 자국을 하나 남긴다. 작을수록 길이 매끄럽지만 점이 많아진다.")]
    [SerializeField] private float minStep = 0.08f;

    [Tooltip("기억하는 길의 총 길이(유닛). 맨 뒤 소환수가 설 거리보다 넉넉해야 한다.")]
    [SerializeField] private float maxLength = 14f;

    [Tooltip("한 프레임에 이보다 많이 움직였으면 순간이동으로 보고 길을 새로 깐다.")]
    [SerializeField] private float teleportThreshold = 3f;

    /// <summary>0 = 가장 오래된 자국, 마지막 = 가장 최근. 현재 위치는 여기 안 들어간다.</summary>
    private readonly List<Vector2> _points = new();

    private float _length;
    private int   _recordedFrame = -1;

    /// <summary>
    /// <paramref name="player"/> 의 길 기록기를 얻는다. 없으면 붙인다.
    ///
    /// <para>🔴 <c>GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()</c> 로 쓰면 안 된다 —
    /// 파괴된 컴포넌트는 C# 기준 null 이 아니라 <b>"가짜 null"</b> 이라 <c>??</c> 가 그냥 통과시킨다
    /// (<c>CLAUDE.md</c> §3).</para>
    /// </summary>
    public static PlayerTrail Of(Transform player)
    {
        if (player == null) return null;

        var trail = player.GetComponent<PlayerTrail>();
        if (trail == null) trail = player.gameObject.AddComponent<PlayerTrail>();
        return trail;
    }

    // ── 기록 ────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <see cref="LateUpdate"/> 에만 의존하면 안 된다. <see cref="SummonWeapon"/> 도 LateUpdate 에서
    /// 읽는데 <b>둘의 실행 순서가 보장되지 않는다.</b> 그래서 <see cref="SampleBack"/> 이 먼저 불려도
    /// 그 자리에서 기록하도록 프레임 번호로 잠근다 — 어느 쪽이 먼저 와도 한 프레임에 딱 한 번이다.
    /// </summary>
    private void LateUpdate() => Record();

    private void Record()
    {
        if (_recordedFrame == Time.frameCount) return;
        _recordedFrame = Time.frameCount;

        Vector2 head = transform.position;

        if (_points.Count == 0) { Seed(head); return; }

        float moved = Vector2.Distance(_points[^1], head);

        // 런을 새로 시작하거나 스테이지가 바뀌면 플레이어가 통째로 옮겨진다.
        // 그때 옛 길을 그대로 두면 소환수가 화면 밖 옛 자리까지 줄을 서서 끌려간다.
        if (moved >= teleportThreshold) { Seed(head); return; }
        if (moved <  minStep) return;

        _points.Add(head);
        _length += moved;

        // 뒤쪽 자국부터 버린다. 남는 길이가 maxLength 밑으로 내려가지 않는 선까지만.
        while (_points.Count >= 2)
        {
            float oldest = Vector2.Distance(_points[0], _points[1]);
            if (_length - oldest < maxLength) break;
            _points.RemoveAt(0);
            _length -= oldest;
        }
    }

    /// <summary>
    /// 길을 곧게 깔아 둔다. 빈 상태로 두면 런이 시작하는 순간
    /// <b>소환수가 전부 플레이어 발밑에 겹쳐 있다가</b> 움직여야 풀린다.
    /// </summary>
    private void Seed(Vector2 head)
    {
        _points.Clear();

        // 왼쪽으로 깐다 — 플레이어 기본 바라보는 방향이 오른쪽이라 그 뒤가 왼쪽이다.
        int steps = Mathf.Max(2, Mathf.CeilToInt(maxLength / Mathf.Max(minStep, 1e-4f)));
        for (int i = steps; i >= 1; i--) _points.Add(head + Vector2.left * (minStep * i));

        _points.Add(head);
        _length = minStep * steps;
    }

    // ── 조회 ────────────────────────────────────────────────────

    /// <summary>
    /// 지금 위치에서 길을 따라 <paramref name="distance"/> 유닛만큼 <b>거슬러 올라간</b> 지점.
    ///
    /// <para>직선 거리가 아니라 <b>길 위의 거리</b>다. 그래서 플레이어가 꺾으면
    /// 따라오는 쪽도 같은 자리에서 꺾는다.</para>
    ///
    /// <para>기억한 길보다 더 뒤를 물으면 가장 오래된 구간의 방향으로 <b>곧게 늘여서</b> 답한다.
    /// 없다고 플레이어 자리를 돌려주면 소환수들이 순간적으로 발밑에 뭉친다.</para>
    /// </summary>
    public Vector2 SampleBack(float distance)
    {
        Record();

        Vector2 head = transform.position;
        if (distance <= 0f || _points.Count == 0) return head;

        Vector2 prev = head;
        float   acc  = 0f;

        for (int i = _points.Count - 1; i >= 0; i--)
        {
            Vector2 p   = _points[i];
            float   seg = Vector2.Distance(prev, p);

            if (acc + seg >= distance)
            {
                float t = seg > 1e-5f ? (distance - acc) / seg : 0f;
                return Vector2.Lerp(prev, p, t);
            }

            acc  += seg;
            prev  = p;
        }

        Vector2 back = _points.Count >= 2 ? _points[0] - _points[1] : Vector2.left;
        if (back.sqrMagnitude < 1e-6f) back = Vector2.left;
        return prev + back.normalized * (distance - acc);
    }
}
