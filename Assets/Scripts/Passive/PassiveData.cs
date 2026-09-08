using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  PassiveData  —  패시브 수치 정의 ScriptableObject
// ────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "PassiveData", menuName = "Game/PassiveData")]
public class PassiveData : ScriptableObject
{
    [Header("기본")]
    public string PassiveName;

    [Header("레벨별 수치 보너스 (덧셈)")]
    public float[] BonusMaxHp          = { 20, 40, 60, 80, 100 };
    public float[] BonusMoveSpeed      = { 0, 0, 0.2f, 0.4f, 0.6f };
    public float[] BonusDamage         = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
    public float[] BonusAttackSpeed    = { -0.05f, -0.10f, -0.15f, -0.20f, -0.25f }; // 음수 = 빠름
    public float[] BonusProjectileSize = { 0, 0, 0.1f, 0.2f, 0.3f };
    public float[] BonusPickupRadius   = { 0.5f, 1f, 1.5f, 2f, 2.5f };
    public float[] BonusCritChance     = { 0.02f, 0.04f, 0.06f, 0.08f, 0.1f };
    public float[] BonusArmor          = { 1, 2, 3, 5, 7 };
    public float[] BonusXpGain         = { 0, 0, 0, 0, 0 };   // 1.0 = +100%
    public float[] BonusGoldGain       = { 0, 0, 0, 0, 0 };
    public float[] BonusBuildingCooldown = { 0, 0, 0, 0, 0 }; // 음수 = 건물이 빨라짐

    /// <summary>픽업 드랍 확률 배율에 더해진다. 최종확률 = 기본확률 × (1 + Luck). 1.0 이면 정확히 2배.</summary>
    public float[] BonusLuck            = { 0, 0, 0, 0, 0 };

    [Tooltip("초당 체력 재생 가산분 (D74). 최대 체력이 아니라 '초당 몇' 이다")]
    public float[] BonusHpRegen         = { 0, 0, 0, 0, 0 };

    [Tooltip("소환 무기 하나당 몸통을 몇 마리 더 두는지 (D113). 0 = 지금까지대로 한 마리")]
    public float[] BonusSummonCount     = { 0, 0, 0, 0, 0 };

    [Tooltip("자동 순간이동 주기(초). 0 = 기능 없음. 🔴 작을수록 좋다 — 레벨이 오르면 내려간다 (D113)")]
    public float[] BonusTeleportInterval = { 0, 0, 0, 0, 0 };

    [Tooltip("보호막 재생 주기(초). 0 = 기능 없음. 🔴 작을수록 좋다 (D113)")]
    public float[] BonusShieldInterval   = { 0, 0, 0, 0, 0 };

    [Tooltip("🔴 적 최대 체력 가산 배율 (D121). 0.5 = 적 체력 1.5배. " +
             "이 게임에서 유일하게 '나를 나쁘게 만드는' 값이다")]
    public float[] BonusEnemyHp          = { 0, 0, 0, 0, 0 };

    private float Get(float[] arr, int lv) => arr.Length == 0 ? 0 :
        arr[Mathf.Clamp(lv - 1, 0, arr.Length - 1)];

    public float GetMaxHp(int lv)          => Get(BonusMaxHp, lv);
    public float GetMoveSpeed(int lv)      => Get(BonusMoveSpeed, lv);
    public float GetDamage(int lv)         => Get(BonusDamage, lv);
    public float GetAttackSpeed(int lv)    => Get(BonusAttackSpeed, lv);
    public float GetProjectileSize(int lv) => Get(BonusProjectileSize, lv);
    public float GetPickupRadius(int lv)   => Get(BonusPickupRadius, lv);
    public float GetCritChance(int lv)     => Get(BonusCritChance, lv);
    public float GetArmor(int lv)          => Get(BonusArmor, lv);
    public float GetXpGain(int lv)         => Get(BonusXpGain, lv);
    public float GetGoldGain(int lv)       => Get(BonusGoldGain, lv);
    public float GetBuildingCooldown(int lv) => Get(BonusBuildingCooldown, lv);
    public float GetLuck(int lv)           => Get(BonusLuck, lv);
    public float GetHpRegen(int lv)        => Get(BonusHpRegen, lv);
    public float GetSummonCount(int lv)      => Get(BonusSummonCount, lv);
    public float GetTeleportInterval(int lv) => Get(BonusTeleportInterval, lv);
    public float GetShieldInterval(int lv)   => Get(BonusShieldInterval, lv);
    public float GetEnemyHp(int lv)          => Get(BonusEnemyHp, lv);
}
