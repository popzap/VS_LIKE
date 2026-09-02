using UnityEngine;

/// <summary>
/// 🔬 <b>임시 (D27).</b> <c>Update()</c> 가 <b>비어 있는</b> 컴포넌트.
///
/// <para>가설 H-A 를 재기 위한 것이다 — <c>BehaviourUpdate</c> 의 미설명분이
/// "어떤 코드가 비싸서"가 아니라 <b>"Update 를 호출하는 것 자체의 고정 비용 × 객체 수"</b>
/// 인지 확인한다. Unity 는 네이티브에서 매니지드 콜백을 부를 때마다 전이 비용을 낸다.</para>
///
/// <para><b>본문이 비어 있는 것이 핵심이다.</b> 무엇이든 넣으면 그 비용이 섞여
/// 기울기가 "호출 단가"가 아니게 된다.</para>
///
/// <para>측정이 끝나면 <b>이 파일을 지운다</b> (<c>CLAUDE.md</c> §4).</para>
/// </summary>
public class PerfDummyUpdater : MonoBehaviour
{
    // 🔴 비어 있어야 한다. 컴파일러가 통째로 지우지 않도록 Unity 메시지 메서드로 둔다 —
    //    Unity 는 리플렉션으로 찾아 호출하므로 빈 몸통이어도 호출은 실제로 일어난다.
    private void Update() { }
}
