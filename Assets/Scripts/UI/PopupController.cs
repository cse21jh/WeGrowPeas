using DG.Tweening;
using UnityEngine;

public class PopupController : MonoBehaviour
{
    [SerializeField] private RectTransform self;
    [SerializeField] private float easeDuration;
    [SerializeField] private Ease ease;



    public void ClosePopup()
    {
        SoundManager.Instance.PlayEffect("Button");

        if(self != null)
        {
            self.DOSizeDelta(Vector2.zero, easeDuration).SetEase(ease).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        gameObject.SetActive(false);
    }
}
