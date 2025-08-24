using UnityEngine;
using DG.Tweening;

public class UIAnimationManager : MonoBehaviour
{
    [SerializeField] public CameraManager[] camManagers;
    [SerializeField] private RectTransform upgrade_targetPanel;
    [SerializeField] private RectTransform upgrade_panelTransformOrigin;
    [SerializeField] private RectTransform upgrade_panelTransformMoved;
    [SerializeField] private RectTransform shop_targetPanel;
    [SerializeField] private RectTransform shop_panelTransformOrigin;
    [SerializeField] private RectTransform shop_panelTransformMoved;
    [SerializeField] private Ease panelEase;
    [SerializeField] private float panelMoveDuration = 0.5f;


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

            shop_targetPanel.DOAnchorPos(shop_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }
        else if (type == CameraManager.CameraType.Upgrade)
        {
            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformMoved.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }
        else if (type == CameraManager.CameraType.Shop)
        {
            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformMoved.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }
    }





}
