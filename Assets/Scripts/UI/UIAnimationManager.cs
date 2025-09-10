using UnityEngine;
using DG.Tweening;

public class UIAnimationManager : MonoBehaviour
{
    [SerializeField] public CameraManager[] camManagers;
    [Space(10)]
    [Header("Upgrade Panel")]
    [SerializeField] private RectTransform upgrade_targetPanel;
    [SerializeField] private RectTransform upgrade_panelTransformOrigin;
    [SerializeField] private RectTransform upgrade_panelTransformMoved;

    [Space(10)]
    [Header("Shop Panel")]
    [SerializeField] private RectTransform shop_targetPanel;
    [SerializeField] private RectTransform shop_panelTransformOrigin;
    [SerializeField] private RectTransform shop_panelTransformMoved;
    [SerializeField] private RectTransform shop_panelTransformFinish;

    [Space(10)]
    [Header("Newspaper Panel")]
    [SerializeField] private RectTransform newspaper_targetPanel;
    [SerializeField] private RectTransform newspaper_panelTransformOrigin;
    [SerializeField] private RectTransform newspaper_panelTransformMoved;

    [Space(10)]
    [Header("Animation Settings")]
    [SerializeField] private Ease panelEase;
    [SerializeField] private float panelMoveDuration = 0.5f;
    [SerializeField] private float shopPanelMoveDuration = 2.0f;
    [SerializeField] private Newspaper newspaper;

    public void SwitchCameras(CameraManager.CameraType type)
    {
        foreach (var camManager in camManagers)
        {
            camManager.SwitchCamera(type, panelMoveDuration);
        }

        if (type == CameraManager.CameraType.Normal)
        {
            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformFinish.anchoredPosition, shopPanelMoveDuration)
                .SetEase(panelEase);
        }
        else if (type == CameraManager.CameraType.Upgrade)
        {
            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformMoved.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformFinish.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }
        else if (type == CameraManager.CameraType.Shop)
        {

            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformMoved.anchoredPosition, shopPanelMoveDuration)
                .SetEase(panelEase);

            ShowNewspaper();

            Debug.Log(shop_panelTransformMoved.ToString());
        }
    }

    public void ResetShopPanelPosition()
    {
        shop_targetPanel.anchoredPosition = new Vector2(shop_panelTransformOrigin.anchoredPosition.x, shop_panelTransformOrigin.anchoredPosition.y); // Reset position before moving
        Debug.Log(shop_targetPanel.anchoredPosition.ToString() + shop_panelTransformOrigin.anchoredPosition.ToString());
    }

    public void ShowNewspaper()
    {
        if (newspaper.UpdateNewspaper())
            newspaper_targetPanel.DOAnchorPos(newspaper_panelTransformMoved.anchoredPosition, panelMoveDuration).SetEase(panelEase); ;
    }

    public void HideNewspaper()
    {
        newspaper_targetPanel.DOAnchorPos(newspaper_panelTransformOrigin.anchoredPosition, panelMoveDuration).SetEase(panelEase); ;
    }

}
