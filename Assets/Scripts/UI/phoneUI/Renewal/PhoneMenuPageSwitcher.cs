using UnityEngine;

/// <summary>
/// 선택된 하단 메뉴 인덱스에 맞춰 페이지를 활성화한다.
/// </summary>
public sealed class PhoneMenuPageSwitcher : MonoBehaviour
{
    [Tooltip(
        "BottomMenuCarousel의 Items와 동일한 순서로 페이지를 등록합니다.")]
    [SerializeField]
    private GameObject[] pages;

    /// <summary>
    /// UnityEvent<int>에 연결할 페이지 전환 함수.
    /// </summary>
    public void ShowPage(int pageIndex)
    {
        if (pages == null ||
            pageIndex < 0 ||
            pageIndex >= pages.Length)
        {
            Debug.LogWarning(
                $"잘못된 페이지 인덱스입니다: {pageIndex}",
                this);

            return;
        }

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == pageIndex);
            }
        }
    }
}
