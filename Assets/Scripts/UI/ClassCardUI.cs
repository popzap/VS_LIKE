using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 직업 선택 화면의 네모 칸 하나. <see cref="ClassSelectUI"/> 가 목록 수만큼 복제해 쓴다.
///
/// <para>일러스트가 아직 없으므로 <c>Portrait</c> 가 비면 아이콘을 감추고 이름만 보여준다.
/// 애셋이 생기면 <c>Classes.csv</c> 의 <c>Portrait</c> 열만 채우면 그대로 뜬다.</para>
/// </summary>
public class ClassCardUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Button           button;
    [SerializeField] private Image            portraitImage;
    [SerializeField] private TextMeshProUGUI  nameText;
    [Tooltip("선택된 칸에만 켜지는 테두리/하이라이트")]
    [SerializeField] private GameObject       selectedFrame;
    [Tooltip("해금되지 않은 직업을 덮는 오버레이 (자물쇠 등)")]
    [SerializeField] private GameObject       lockedOverlay;

    public CharacterClassData Data  { get; private set; }
    public int                Index { get; private set; }

    public void Setup(CharacterClassData data, int index, bool unlocked, System.Action<int> onClick)
    {
        Data  = data;
        Index = index;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(data.ClassName) ? data.name : data.ClassName;

        // 일러스트 미준비 상태에서 흰 사각형이 뜨는 걸 막는다.
        if (portraitImage != null)
        {
            portraitImage.sprite  = data.Portrait;
            portraitImage.enabled = data.Portrait != null;
        }

        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                AudioManager.Play(SfxId.UiSelect);
                onClick(index);
            });
            button.interactable = unlocked;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
