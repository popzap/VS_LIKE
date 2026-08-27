using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveSpawnEntry
{
    public EnemyData Enemy;
    public int       Count;
    public float     SpawnInterval = 0.5f;
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

    [Header("엘리트 / 보스 오버라이드")]
    public EnemyData EliteOverride;   // Elite 스테이지에서 추가되는 특수 적
    public EnemyData BossOverride;    // Boss 스테이지 보스
    public int       EliteCount = 1;
}
