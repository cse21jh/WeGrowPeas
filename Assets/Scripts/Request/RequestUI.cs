using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RequestUI : MonoBehaviour
{
   [SerializeField] private RectTransform popupParent;  // ÆË¾÷ UI ºÎ¸ð


    public void OnClickShowPopup()
    {
        popupParent.gameObject.SetActive(true);
        popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        //popupParent.GetComponent<ShopPopupController>().SetItemInfo(data, this, slot);
    }

    public void OnClickHidePopup()
    {
        popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            popupParent.gameObject.SetActive(false);
        });
    }
}
