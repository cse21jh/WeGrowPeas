using System.Collections;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    [SerializeField] private ShopUI shopUI;

    public IEnumerator ShopPhase()
    {
        // 6일마다만 상점 오픈
        if (GameManager.Instance.stage % 6 != 0)
            yield break;

        // UI 열기
        shopUI.Open();

        // UI에서 "구매 완료" / "닫기" 버튼이 눌릴 때까지 대기
        bool closed = false;
        shopUI.OnShopClosed += () => closed = true;

        while (!closed)
            yield return null;
    }
}