using System;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EconomyFeedbackStyle",
    menuName = "WeGrowPeas/UI/Economy Feedback Style")]
public sealed class EconomyFeedbackStyle : ScriptableObject
{
    public const string ResourcesPath = "UI/EconomyFeedbackStyle";

    [Header("금액 텍스트")]
    [SerializeField, InspectorName("획득 / 식물 가치 상승")]
    private EconomyFeedbackAmountTextStyle gainAmount = new EconomyFeedbackAmountTextStyle(
        22f,
        new Color(1f, 0.82f, 0.18f, 1f));

    [SerializeField, InspectorName("골드 소모")]
    private EconomyFeedbackAmountTextStyle spendAmount = new EconomyFeedbackAmountTextStyle(
        20f,
        new Color(1f, 0.35f, 0.22f, 1f));

    [Header("도형 효과")]
    [SerializeField, InspectorName("HUD + 색상")]
    private Color hudPlusColor = new Color(0.48f, 1f, 0.58f, 0.42f);

    [Header("식물 가치 상승 위치")]
    [SerializeField, InspectorName("시작 위치 (식물 기준 / 월드)")]
    [Tooltip("식물의 월드 위치에 더해지는 오프셋입니다. X는 좌우, Y는 위아래 위치를 조절합니다.")]
    private Vector3 plantValueStartWorldOffset = new Vector3(0f, 0.9f, 0f);

    [SerializeField, InspectorName("최종 위치 (시작점 기준 / UI)")]
    [Tooltip("시작 위치에서 최종 위치까지 이동할 UI 오프셋입니다. X와 Y를 모두 조절할 수 있습니다.")]
    private Vector2 plantValueEndUiOffset = new Vector2(0f, 64f);

    public EconomyFeedbackAmountTextStyle GainAmount => gainAmount;
    public EconomyFeedbackAmountTextStyle SpendAmount => spendAmount;
    public Color HudPlusColor => hudPlusColor;
    public Vector3 PlantValueStartWorldOffset => plantValueStartWorldOffset;
    public Vector2 PlantValueEndUiOffset => plantValueEndUiOffset;

    private void OnValidate()
    {
        if (gainAmount == null)
        {
            gainAmount = new EconomyFeedbackAmountTextStyle(
                22f,
                new Color(1f, 0.82f, 0.18f, 1f));
        }

        if (spendAmount == null)
        {
            spendAmount = new EconomyFeedbackAmountTextStyle(
                20f,
                new Color(1f, 0.35f, 0.22f, 1f));
        }

        gainAmount.ClampValues();
        spendAmount.ClampValues();
    }
}

[Serializable]
public sealed class EconomyFeedbackAmountTextStyle
{
    [SerializeField, InspectorName("폰트 에셋")]
    [Tooltip("비워 두면 메인 코인 UI의 TMP 폰트 에셋을 사용합니다.")]
    private TMP_FontAsset fontAsset;

    [SerializeField, InspectorName("머티리얼 프리셋")]
    [Tooltip("선택한 폰트 에셋으로 만든 TMP Material Preset을 지정하세요. 비워 두면 폰트 에셋의 기본 머티리얼을 사용합니다.")]
    private Material materialPreset;

    [SerializeField, InspectorName("폰트 스타일")]
    private FontStyles fontStyle = FontStyles.Normal;

    [SerializeField, Min(1f), InspectorName("폰트 크기")]
    private float fontSize = 22f;

    [SerializeField, InspectorName("색상")]
    private Color color = Color.white;

    public TMP_FontAsset FontAsset => fontAsset;
    public Material MaterialPreset => materialPreset;
    public FontStyles FontStyle => fontStyle;
    public float FontSize => Mathf.Max(1f, fontSize);
    public Color Color => color;

    public EconomyFeedbackAmountTextStyle()
    {
    }

    public EconomyFeedbackAmountTextStyle(float fontSize, Color color)
    {
        this.fontSize = fontSize;
        this.color = color;
    }

    internal void ClampValues()
    {
        fontSize = Mathf.Max(1f, fontSize);
    }
}
