using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 안의 소환 항목 하나. <b>항목끼리는 동시에(병렬) 진행된다.</b>
///
/// <para>예전에는 순차였다 — 앞 항목의 마리수를 다 뿌려야 다음 항목이 시작됐다.
/// 그래서 (마리수 x 간격) 의 합이 <c>SurvivalTime</c> 을 넘으면 뒤쪽 적은
/// <b>아예 등장하지 못했다.</b> Normal3 의 Goblin 40마리가 그랬다.
/// 지금은 각 항목이 자기 시간창 안에서 따로 돌기 때문에 여러 종이 섞여 나온다.</para>
/// </summary>
/// <summary>
/// 적이 <b>어떻게 나타나는가</b> (D50). 어디서 나오는지가 아니라 <b>어떤 모양으로</b> 나오는지다.
///
/// <para>🔑 예전에는 하나뿐이었다 — 소환 반경 원 위 <b>아무 데나 한 마리씩</b>.
/// 그래서 적이 늘 "사방에서 하나씩 흘러들어오는" 똑같은 모습이었다.</para>
/// </summary>
public enum SpawnPattern
{
    /// <summary>흩뿌리기 — 원 위 무작위, 간격을 두고 한 마리씩. <b>예전 동작 그대로다.</b></summary>
    Scatter,

    /// <summary>포위 — 원 위에 <b>고르게 한꺼번에</b> 놓는다. 쫓아오면서 자연히 좁혀 온다.</summary>
    Ring,

    /// <summary>부대 — <b>한 방향에서</b> 대형(줄×칸)을 이뤄 뭉쳐 온다.</summary>
    Squad,

    /// <summary>땅굴 — 플레이어 <b>가까이</b>에서 예고 뒤에 솟아오른다. 🔴 예고가 이 패턴의 전부다.</summary>
    Burrow
}

[System.Serializable]
public class WaveSpawnEntry
{
    public EnemyData Enemy;
    public int       Count;
    public float     SpawnInterval = 0.5f;

    /// <summary>웨이브 시작 후 이 초가 지나야 소환을 시작한다.</summary>
    public float StartTime;

    /// <summary>이 초를 넘기면 남은 마리수가 있어도 그만둔다. <c>0</c> 이면 제한 없음.</summary>
    public float EndTime;

    /// <summary>
    /// 이 항목이 나타나는 모양 (D50). <c>Waves.csv</c> 에서는 맨 끝에 <c>~Ring</c> 처럼 붙인다 —
    /// <c>Goblin*36@1.4:12-60~Ring</c>. <b>안 적으면 <see cref="SpawnPattern.Scatter"/></b> 라
    /// 기존 줄은 그대로 돈다.
    /// </summary>
    public SpawnPattern Pattern = SpawnPattern.Scatter;
}

/// <summary>
/// 웨이브(스테이지 1회분) 정의.
///
/// <para>ScriptableObject 는 <b>클래스명과 같은 이름의 파일</b>에 있어야 Unity 가 MonoScript 를 찾는다.
/// 예전엔 WaveManager.cs 안에 같이 있었는데, 그러면 새로 만든 .asset 의 m_Script 가 0 으로 기록되고
/// "No script asset for WaveData" 경고가 뜬다. 그래서 파일을 분리했다.</para>
///
/// 수치는 <c>Assets/Game/Balance/Waves.csv</c> 가 원본이다. 인스펙터에서 직접 고치지 말고
/// CSV 를 고친 뒤 <c>Game/Balance/Import CSV -&gt; ScriptableObjects</c> 를 실행한다.
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Game/WaveData")]
public class WaveData : ScriptableObject
{
    [Header("클리어 조건")]
    public bool  UseTimerClear = true;   // 시간 생존
    public float SurvivalTime  = 120f;   // 초 (UseTimerClear = true 일 때)
    public bool  UseKillClear  = false;  // 킬 카운트
    public int   KillTarget    = 0;

    [Header("스폰")]
    public List<WaveSpawnEntry> Spawns = new();
    public float                SpawnRadius = 20f;   // 플레이어 기준 소환 반경

    /// <summary>
    /// 동시에 살아 있을 수 있는 적의 최대 수. 차 있으면 소환이 <b>대기</b>한다(취소가 아니다).
    ///
    /// <para>순차 소환일 때는 필요 없었다 — 한 번에 한 종류만 나왔으니까.
    /// 병렬로 바꾸면 여러 항목이 동시에 쏟아져 상한이 없으면 프레임이 무너진다.
    /// 엘리트·보스 오버라이드는 이 상한을 무시한다 (보스가 못 나오면 웨이브가 끝나지 않는다).</para>
    /// </summary>
    public int MaxAlive = 100;

    [Header("엘리트 / 보스 오버라이드")]
    public EnemyData EliteOverride;   // Elite 스테이지에서 추가되는 특수 적
    public EnemyData BossOverride;    // Boss 스테이지 보스
    public int       EliteCount = 1;

    /// <summary>엘리트를 소환할 시각(웨이브 시작 후 초). 여러 마리면 1.5초 간격으로 이어 나온다.</summary>
    public float EliteTime = 40f;

    /// <summary>보스를 소환할 시각(웨이브 시작 후 초).</summary>
    public float BossTime = 2f;
}
