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

    // 🔴 <b>이게 없어서 판매가 안 됐다</b> (D86 · 사용자: *"보유 아이템 판매 작동 안하는거 같아"*).
    //    `confirmOverlay` 안에 확인 버튼이 있는데 <b>아무도 리스너를 안 붙였다</b> —
    //    X 를 누르면 오버레이가 뜨고, 그 안의 버튼은 <b>눌러도 아무 일이 없었다.</b>
    //    사용자에게는 "판매가 안 된다" 로 보인다.
    [Tooltip("확인 오버레이 안의 '진짜 파는' 버튼. 비어 있으면 X 를 한 번 더 눌러야 한다.")]
    [SerializeField] private Button           confirmButton;

    [Tooltip("확인을 취소하는 버튼(선택). 비어 있으면 X 를 다시 눌러 취소한다.")]
    [SerializeField] private Button           cancelButton;

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

        // 🔴 오버레이 안의 버튼도 반드시 잇는다 (D86). 프리팹에 버튼은 있는데
        //    리스너가 없어서 "판매가 안 된다" 로 보였다.
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ExecuteRemove);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelConfirm);
        }
    }

    /// <summary>확인을 물린다. 오버레이가 뜬 채로 갇히지 않게 하는 유일한 길이다.</summary>
    private void CancelConfirm()
    {
        _awaitingConfirm = false;
        if (confirmOverlay) confirmOverlay.SetActive(false);
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
            // 🔑 2차로 <b>X 를 다시 누르면 취소</b>다 (D86).
            //    예전에는 여기서 바로 팔았는데, 확인 버튼이 죽어 있는 상태에서
            //    그게 유일한 판매 경로였다 — 즉 <b>"취소하려고 X 를 다시 누르면 팔렸다."</b>
            //    파는 건 오버레이의 확인 버튼이 한다.
            if (confirmButton != null) CancelConfirm();
            else                       ExecuteRemove();   // 확인 버튼이 없으면 예전 동작
        }
    }

    private void ExecuteRemove() => _manager.RemoveItem(_item);
}
