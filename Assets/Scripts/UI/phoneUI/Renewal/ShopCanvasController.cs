using DG.Tweening;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCanvasController : MonoBehaviour
{

    [SerializeField] private RectTransform layerOpenRect;
    [SerializeField] private RectTransform layerCloseRect;

    [SerializeField] private RectTransform itemListLayer;
    [SerializeField] private RectTransform descriptionLayer;

    [SerializeField] private float layerMoveDuration = 0.25f;
    [SerializeField] private Ease layerMoveEase = Ease.InOutSine;


    [SerializeField] private Image[] showBtns;
    [SerializeField] private TextMeshProUGUI[] showBtnTexts;

    [SerializeField] private TMP_FontAsset pretendard_Bold;
    [SerializeField] private Color boldColor;
    [SerializeField] private TMP_FontAsset pretendard_Medium;
    [SerializeField] private Color mediumColor;



    /// <summary>
    /// 
    /// "도와줘요 현민에몽 준하에몽"
    ///
    /// 로테이션 아이템들 리롤하는 함수임
    /// 알잘딱하게,,, 구현 오네가이,,,
    /// 
    /// </summary>
    public void Reroll()
    {

    }

    public void ShowAll()
    {
        // 전체 아이템 필터링

        UpdateItems(
            // 필요한 매개변수 넣기
            );

        SetMenuStyle(0);
    }

    public void ShowFixed()
    {
        // 고정 아이템 필터링

        UpdateItems(
            // 필요한 매개변수 넣기
            );

        SetMenuStyle(1);
    }

    public void ShowRotation()
    {
        // 로테이션 아이템 필터링

        UpdateItems(
            // 필요한 매개변수 넣기
            );

        SetMenuStyle(2);
    }

    private void SetMenuStyle(int mainBtn)
    {
        foreach (var btn in showBtns)
        {
            btn.color = new Color(1f, 1f, 1f, 0.25f);
        }
        foreach (var btn in showBtnTexts)
        {
            btn.font = pretendard_Medium;
            btn.color = mediumColor;
        }

        showBtns[mainBtn].color = new Color(1f, 1f, 1f, 1f);
        showBtnTexts[mainBtn].font = pretendard_Bold;
        showBtnTexts[mainBtn].color = boldColor;
    }



    public void ShowItemDetailPanel()
    {
        // 유일하게 내가 미리 구현해두는 함수

        descriptionLayer.gameObject.SetActive(true);
        descriptionLayer.DOAnchorPosY(layerOpenRect.anchoredPosition.y, layerMoveDuration).SetEase(layerMoveEase).OnComplete(() =>
        {
            itemListLayer.gameObject.SetActive(false);
        });
    }

    public void CloseItemDetailPanel()
    {
        // 유일하게 내가 미리 구현해두는 함수

        itemListLayer.gameObject.SetActive(true);
        descriptionLayer.DOAnchorPosY(layerCloseRect.anchoredPosition.y, layerMoveDuration).SetEase(layerMoveEase).OnComplete(() =>
        {
            descriptionLayer.gameObject.SetActive(false);
        });
    }



    /// <summary>
    /// 
    /// "도와줘요 현민에몽 준하에몽"
    /// 
    /// </summary>
    public void BuyItem()
    {

    }


    /// <summary>
    /// 
    /// "도와줘요 현민에몽 준하에몽"
    /// 
    /// 얘는 기본적으로 ShowAll, ShowFixed, ShowRotation에서 전부 호출하는 기본 메서드이므로
    /// 각 함수에서 이 함수를 호출할 때 매개변수에다가 미리 필터링된 결과값들을 넣어주면 됨
    /// 
    /// </summary>
    private void UpdateItems(
        // 여기다가 필요한 매개변수를 넣으면 됨
        // 근데 아이템의 sprite, 이름, 가격, 구매 제한 수량, 태그 등등이 필요함
        )
    {
        // 대충 받아 온 배열을 가지고 foreach 돌리면서 ItemController의 SetItemDetail() 호출하면 됨
        // 해당 함수도 도와줘요잉
    }


}
