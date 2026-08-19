using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈 화면 위젯을 눌렀을 때 하단 메뉴 버튼을 누른 것과 동일하게 앱을 연다.
///
/// 위젯은 PhoneHomeGrid가 프리팹에서 생성하므로
/// 씬 오브젝트 참조를 들고 있으면 그리드가 다시 생성될 때 끊긴다.
/// 그래서 참조 없이 PhoneManager 싱글톤을 통해 동작한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhoneWidgetMenuButton : MonoBehaviour
{
    [Header("Button")]

    [Tooltip("비워두면 같은 오브젝트의 Button(MultiTargetButton)을 사용합니다.")]
    [SerializeField]
    private Button button;

    [Header("Target")]

    [Tooltip(
        "열고 싶은 하단 메뉴의 인덱스입니다. " +
        "0 메신저 / 1 국세청 / 2 홈 / 3 상점 / 4 퀘스트")]
    [SerializeField]
    private int menuIndex = PhoneManager.MENU_INDEX_TAX;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            Debug.LogError(
                $"{name}: PhoneWidgetMenuButton에 Button이 없습니다.",
                this);

            return;
        }

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        PhoneManager phone = PhoneManager.Instance;

        if (phone == null)
        {
            Debug.LogWarning(
                $"{name}: PhoneManager를 찾을 수 없습니다.",
                this);

            return;
        }

        /*
         * 하단 버튼 클릭과 같은 경로를 탄다.
         * 캐러셀이 선택 강조를 처리하고, 그 결과로 PhoneManager.OpenAppByIndex가 호출된다.
         */
        phone.SelectBottomMenu(menuIndex);
    }
}
