using UnityEngine;
using DG.Tweening;
using System.Collections;

public class UIAnimationManager : MonoBehaviour
{
    [SerializeField] public CameraManager[] camManagers;
    [SerializeField] private GameObject[] camFollowTargets;
    [SerializeField] private bool isFollowingPhoneMode = false;

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
    [SerializeField] private float endingDelay = 5f;

    public void SwitchCameras(CameraManager.CameraType type)
    {
        foreach (var camManager in camManagers)
        {
            if(type != CameraManager.CameraType.Ending)
            {
                camManager.SwitchCamera(type, panelMoveDuration);
            }
            else
            {
                StartCoroutine(SwitchCamDelay(endingDelay, camManager, type));
            }
        }

        if (type == CameraManager.CameraType.Normal)
        {
            upgrade_targetPanel.DOAnchorPos(upgrade_panelTransformOrigin.anchoredPosition, panelMoveDuration)
                .SetEase(panelEase);

            shop_targetPanel.DOAnchorPos(shop_panelTransformFinish.anchoredPosition, shopPanelMoveDuration)
                .SetEase(panelEase).OnComplete(() =>
                {
                    shop_targetPanel.anchoredPosition = new Vector2(shop_panelTransformOrigin.anchoredPosition.x, shop_panelTransformOrigin.anchoredPosition.y);
                });
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

            shop_targetPanel.DOAnchorPos(shop_panelTransformMoved.anchoredPosition, shopPanelMoveDuration)
                .SetEase(panelEase);

            ShowNewspaper();

            Debug.Log(shop_panelTransformMoved.ToString());
        }
        else if (type == CameraManager.CameraType.Ending)
        {
            FindAnyObjectByType<LetterController>().StartEndLetter();
        }
    }

    private IEnumerator SwitchCamDelay(float delay, CameraManager camManger, CameraManager.CameraType type)
    {
        yield return new WaitForSeconds(delay);

        camManger.SwitchCamera(type, panelMoveDuration);
        FindAnyObjectByType<UIFadeController>().FadeOut();
    }

    public void ResetShopPanelPosition()
    {
        DOTween.Kill(shop_targetPanel); // Kill any ongoing tweens on the target panel
        shop_targetPanel.anchoredPosition = new Vector2(shop_panelTransformOrigin.anchoredPosition.x, shop_panelTransformOrigin.anchoredPosition.y); // Reset position before moving
        //Debug.Log(shop_targetPanel.anchoredPosition.ToString() + shop_panelTransformOrigin.anchoredPosition.ToString());
    }

    public void ShowNewspaper()
    {
        if (newspaper.UpdateNewspaper())
            newspaper_targetPanel.DOAnchorPos(newspaper_panelTransformMoved.anchoredPosition, panelMoveDuration).SetEase(panelEase);
    }

    public void HideNewspaper()
    {
        newspaper_targetPanel.DOAnchorPos(newspaper_panelTransformOrigin.anchoredPosition, panelMoveDuration).SetEase(panelEase);
        newspaper.ClearArticle();
    }

}
