using UnityEngine;
using DG.Tweening;

public class UIAnimationManager : MonoBehaviour
{
    [SerializeField] private CameraManager[] camManagers;
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private RectTransform panelTransformOrigin;
    [SerializeField] private RectTransform panelTransformMoved;
    [SerializeField] private Ease panelEase;
    [SerializeField] private float panelMoveDuration = 0.5f;


    public void SwitchCameras(CameraManager.CameraType type)
    {
        foreach (var camManager in camManagers)
        {
            camManager.SwitchCamera(type, panelMoveDuration);
        }

        if (type == CameraManager.CameraType.Wide)
        {
            targetPanel.DOAnchorPos(panelTransformMoved.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }else if(type == CameraManager.CameraType.Normal)
        {
            targetPanel.DOAnchorPos(panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);
        }
    }





}
