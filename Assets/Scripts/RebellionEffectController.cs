using DG.Tweening;
using UnityEngine;

public class RebellionEffectController : MonoBehaviour
{
    [Header("Arrows")]
    [SerializeField] private RectTransform upArrow;
    [SerializeField] private RectTransform downArrow;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementDistance = 200f;
    [SerializeField, Min(0.01f)] private float movementDuration = 0.5f;

    [Header("Ease")]
    [SerializeField] private bool useAnimationCurve = true;
    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private Ease movementEase = Ease.InOutSine;

    private Vector2 _upArrowStartPosition;
    private Vector2 _downArrowStartPosition;

    private Tween _upArrowTween;
    private Tween _downArrowTween;

    private void Awake()
    {
        if (upArrow != null)
            _upArrowStartPosition = upArrow.anchoredPosition;

        if (downArrow != null)
            _downArrowStartPosition = downArrow.anchoredPosition;
    }

    public void PlayArrowAnimation()
    {
        ResetArrowPosition();

        if (upArrow == null || downArrow == null)
        {
            Debug.LogWarning("화살표 RectTransform이 연결되지 않았습니다.", this);
            return;
        }

        upArrow.anchoredPosition = _upArrowStartPosition;
        downArrow.anchoredPosition = _downArrowStartPosition;

        float upTargetY = _upArrowStartPosition.y + movementDistance;
        float downTargetY = _downArrowStartPosition.y - movementDistance;

        _upArrowTween = upArrow.DOAnchorPosY(
            upTargetY,
            movementDuration
        );

        _downArrowTween = downArrow.DOAnchorPosY(
            downTargetY,
            movementDuration
        );

        ApplyEase(_upArrowTween);
        ApplyEase(_downArrowTween);
    }

    public void ResetArrowPosition()
    {
        KillTweens();

        if (upArrow != null)
            upArrow.anchoredPosition = _upArrowStartPosition;

        if (downArrow != null)
            downArrow.anchoredPosition = _downArrowStartPosition;
    }

    private void ApplyEase(Tween tween)
    {
        if (useAnimationCurve && movementCurve != null)
            tween.SetEase(movementCurve);
        else
            tween.SetEase(movementEase);
    }

    private void KillTweens()
    {
        _upArrowTween?.Kill();
        _downArrowTween?.Kill();

        _upArrowTween = null;
        _downArrowTween = null;
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
