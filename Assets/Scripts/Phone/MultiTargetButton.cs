using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Multi Target Button")]
public class MultiTargetButton : Button
{
    [Header("추가 타겟 그래픽")]
    [SerializeField] private Graphic[] additionalTargetGraphics;

    protected override void DoStateTransition(
        SelectionState state,
        bool instant)
    {
        // 기존 Target Graphic은 기본 Button이 처리
        base.DoStateTransition(state, instant);

        if (!gameObject.activeInHierarchy)
            return;

        // 현재 구현은 Color Tint 전용
        if (transition != Transition.ColorTint)
            return;

        Color targetColor = state switch
        {
            SelectionState.Normal => colors.normalColor,
            SelectionState.Highlighted => colors.highlightedColor,
            SelectionState.Pressed => colors.pressedColor,
            SelectionState.Selected => colors.selectedColor,
            SelectionState.Disabled => colors.disabledColor,
            _ => Color.white
        };

        targetColor *= colors.colorMultiplier;

        float duration = instant ? 0f : colors.fadeDuration;

        foreach (Graphic graphic in additionalTargetGraphics)
        {
            if (graphic == null || graphic == targetGraphic)
                continue;

            graphic.CrossFadeColor(
                targetColor,
                duration,
                true,
                true
            );
        }
    }
}
