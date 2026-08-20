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

        // 예전에는 축소 트윈을 걸고 OnComplete에서 껐는데, 바로 아래에서 즉시 끄기 때문에
        // 그 연출은 보이지도 않으면서 트윈만 살아남았다.
        // 그 트윈이 뒤늦게 완료되면 "그 사이 다시 연 팝업"을 꺼 버려서, 열자마자 닫히는 일이 생겼다.
        // 남은 트윈을 정리하고 그냥 끈다.
        if (self != null) self.DOKill();

        gameObject.SetActive(false);
    }
}
