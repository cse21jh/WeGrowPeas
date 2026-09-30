using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시작 화면의 프로필 선택 창. 카드를 누르면 그 프로필로 전환하고 닫는다.
/// 루트는 항상 켜 두고 <see cref="panel"/>만 켜고 끈다(회상 창과 같은 구조).
///
/// 프리팹은 Tools/Profile/Build Profile Select UI 메뉴가 만든다.
/// </summary>
public class ProfileSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ProfileCardUI[] cards;
    [SerializeField] private Button backButton;

    [Header("삭제 확인")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private int pendingDeleteIndex = -1;

    private static ProfileSelectUI openInstance;
    private static int escapeConsumedFrame = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    /// <summary>
    /// 이번 프레임의 ESC를 이 창이 가져갔는가. 설정창이 같은 ESC로 열리지 않도록 확인한다.
    /// </summary>
    public static bool BlocksEscape => openInstance != null || escapeConsumedFrame == Time.frameCount;

    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ConfirmDelete);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(HideConfirm);

        if (panel != null) panel.SetActive(false);
        HideConfirm();
    }

    private void OnEnable()
    {
        SaveManager.OnProfileChanged += Refresh;
    }

    private void OnDisable()
    {
        SaveManager.OnProfileChanged -= Refresh;
        if (openInstance == this) openInstance = null;
    }

    private void Update()
    {
        if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;

        escapeConsumedFrame = Time.frameCount;
        if (confirmPopup != null && confirmPopup.activeSelf) HideConfirm();
        else Close();
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        openInstance = this;
        HideConfirm();
        Refresh();
    }

    public void Close()
    {
        HideConfirm();
        if (panel != null) panel.SetActive(false);
        if (openInstance == this) openInstance = null;
    }

    public void Refresh()
    {
        if (cards == null) return;

        int current = ProfileStore.CurrentIndex;
        for (int i = 0; i < cards.Length && i < ProfileStore.ProfileCount; i++)
        {
            if (cards[i] == null) continue;
            int index = i;
            cards[i].Bind(ProfileStore.GetSummary(index), index == current,
                onSelect: () => Select(index),
                onDelete: () => AskDelete(index));
        }
    }

    private void Select(int index)
    {
        var sm = SaveManager.Instance;
        if (sm == null)
        {
            Debug.LogWarning("[ProfileSelectUI] SaveManager가 없습니다.");
            return;
        }

        if (sm.SwitchProfile(index)) Close();
    }

    // ── 삭제 ─────────────────────────────────────────────────────────────────

    private void AskDelete(int index)
    {
        pendingDeleteIndex = index;
        if (confirmText != null)
            confirmText.text = $"세이브 프로필 {index + 1}을(를) 삭제할까요?\n" +
                               "진행·도감·회상·이어하기가 모두 지워지며 되돌릴 수 없습니다.";
        if (confirmPopup != null) confirmPopup.SetActive(true);
    }

    private void ConfirmDelete()
    {
        int index = pendingDeleteIndex;
        HideConfirm();
        if (index < 0 || SaveManager.Instance == null) return;

        SaveManager.Instance.DeleteProfile(index);
        Refresh(); // 현재 프로필이 아니면 OnProfileChanged가 오지 않는다
    }

    private void HideConfirm()
    {
        pendingDeleteIndex = -1;
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }
}
