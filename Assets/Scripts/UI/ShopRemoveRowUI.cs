using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 왼쪽 제거 패널의 아이템 한 줄
/// </summary>
public class ShopRemoveRowUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image            iconImage;
    [SerializeField] private TextMeshProUGUI  nameText;
    [SerializeField] private TextMeshProUGUI  subText;       // 카테고리 + 레벨
    [SerializeField] private TextMeshProUGUI  refundText;    // "+3G"
    [SerializeField] private Button           removeButton;
    [SerializeField] private GameObject       confirmOverlay; // 2단계 확인 (선택)

    private ItemData    _item;
    private ShopManager _manager;
    private bool        _awaitingConfirm;

    // ── 세팅 ─────────────────────────────────────────────────────

    public void Setup(ItemData item, int refundAmount, ShopManager manager)
    {
        _item    = item;
        _manager = manager;
        _awaitingConfirm = false;
        if (confirmOverlay) confirmOverlay.SetActive(false);

        if (iconImage) iconImage.sprite = item.Icon;
        nameText.text   = item.ItemName;
        refundText.text = $"+{refundAmount}G";

        string catLabel = item.Category switch
        {
            ItemCategory.Weapon   => "WEAPON",
            ItemCategory.Building => "BUILDING",
            ItemCategory.Passive  => "PASSIVE",
            _                     => ""
        };
        string levelLabel = item.CurrentLevel > 0 ? $"Lv.{item.CurrentLevel}" : "Lv.1";
        subText.text = $"{catLabel} · {levelLabel}";

        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);
    }

    // ── 클릭 처리 (2단계 확인) ───────────────────────────────────

    private void OnRemoveClicked()
    {
        if (!_awaitingConfirm)
        {
            // 1차 클릭: 확인 오버레이 표시
            _awaitingConfirm = true;
            if (confirmOverlay) confirmOverlay.SetActive(true);
            else ExecuteRemove(); // confirmOverlay 없으면 바로 실행
        }
        else
        {
            // 2차 클릭: 실행
            ExecuteRemove();
        }
    }

    private void ExecuteRemove() => _manager.RemoveItem(_item);
}
