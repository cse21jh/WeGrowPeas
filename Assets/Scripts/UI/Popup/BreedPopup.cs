using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

[System.Serializable]
public struct TraitGeneImages
{
    public Image geneImage1;
    public Image geneImage2;
}

public class BreedPopup : BasePopup
{
    [Header("Panel References")]
    [SerializeField] private RectTransform maximizedPanel;
    [SerializeField] private RectTransform minimizedPanel;

    [Header("Buttons")]
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button maximizeButton;
    [SerializeField] private Button maxCloseButton;
    [SerializeField] private Button minCloseButton;

    [Header("Resistance Text Elements (Size 8: NaturalDeath, Pest, Wind, Flood, HeavyRain, Cold, Drought, Heat)")]
    [SerializeField] private TextMeshProUGUI[] resistanceTexts;

    [Header("Gene Sprites")]
    [SerializeField] private Sprite greyGeneSprite;
    [SerializeField] private Sprite goldenGeneSprite;

    [Header("Trait Gene Images (Size 8: NaturalDeath, Pest, Wind, Flood, HeavyRain, Cold, Drought, Heat)")]
    [SerializeField] private TraitGeneImages[] traitGeneImages;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private Vector3 maxPanelOriginalScale = Vector3.one;
    private Vector3 minPanelOriginalScale = Vector3.one;
    private bool isMinimized = false;

    protected override void Awake()
    {
        base.Awake();

        if (maximizedPanel != null) maxPanelOriginalScale = maximizedPanel.localScale;
        if (minimizedPanel != null) minPanelOriginalScale = minimizedPanel.localScale;

        if (minimizedPanel != null) minimizedPanel.gameObject.SetActive(false);

        if (minimizeButton != null) minimizeButton.onClick.AddListener(MinimizePanel);
        if (maximizeButton != null) maximizeButton.onClick.AddListener(MaximizePanel);

        if (maxCloseButton != null) maxCloseButton.onClick.AddListener(Close);
        if (minCloseButton != null) minCloseButton.onClick.AddListener(Close);
    }

    public override void Open()
    {
        base.Open();

        if (isMinimized)
        {
            if (maximizedPanel != null) maximizedPanel.gameObject.SetActive(false);
            if (minimizedPanel != null)
            {
                minimizedPanel.gameObject.SetActive(true);
                minimizedPanel.localScale = minPanelOriginalScale;
            }
        }
        else
        {
            if (maximizedPanel != null)
            {
                maximizedPanel.gameObject.SetActive(true);
                maximizedPanel.localScale = maxPanelOriginalScale;
            }
            if (minimizedPanel != null)
            {
                minimizedPanel.gameObject.SetActive(false);
            }
        }
    }

    /// 식물 오브젝트를 입력받아 저항력 UI 및 유전자 유무 상태를 갱신합니다.
    public void Setup(Plant plant, System.Action onClose = null)
    {
        onCloseCallback = onClose;

        if (plant == null) return;

        // 8개 각 형질별 저항력 수치 텍스트 표시
        if (resistanceTexts != null)
        {
            int count = Mathf.Min(resistanceTexts.Length, 8);
            for (int i = 0; i < count; i++)
            {
                if (resistanceTexts[i] != null)
                {
                    // Plant.cs의 GetResistanceValue(int traitNum)를 호출해 실시간 저항력 획득
                    float val = plant.GetResistanceValue(i);
                    resistanceTexts[i].text = $"{(val * 100f):F0}%";
                }
            }
        }

        // 8개 각 형질별 유전자(Genetics) 표시 갱신
        if (traitGeneImages != null)
        {
            int count = Mathf.Min(traitGeneImages.Length, 8);
            for (int i = 0; i < count; i++)
            {
                TraitType type = (TraitType)i;
                int genetics = GetGeneticsValue(plant, type);

                Image img1 = traitGeneImages[i].geneImage1;
                Image img2 = traitGeneImages[i].geneImage2;

                if (img1 != null && img2 != null)
                {
                    switch (genetics)
                    {
                        case 0:
                            img1.sprite = greyGeneSprite;
                            img2.sprite = greyGeneSprite;
                            break;
                        case 1:
                            img1.sprite = goldenGeneSprite;
                            img2.sprite = greyGeneSprite;
                            break;
                        case 2:
                            img1.sprite = goldenGeneSprite;
                            img2.sprite = goldenGeneSprite;
                            break;
                        default:
                            img1.sprite = greyGeneSprite;
                            img2.sprite = greyGeneSprite;
                            break;
                    }
                }
            }
        }
    }

    /// 식물 객체로부터 특정 형질의 genetics(유전자 수치) 값을 반환하는 헬퍼 메서드
    private int GetGeneticsValue(Plant plant, TraitType type)
    {
        List<GeneticTrait> traits = plant.GetGeneticTrait();
        if (traits != null)
        {
            foreach (var t in traits)
            {
                if (t.traitType == type)
                {
                    return t.genetics;
                }
            }
        }
        return 0; // 찾을 수 없거나 형질 정보가 없으면 기본값인 0으로 반환
    }

    public void MinimizePanel()
    {
        if (maximizedPanel == null || minimizedPanel == null) return;
        isMinimized = true;

        // PopupHideController와 동일한 방식으로 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = maximizedPanel.localPosition;
        targetPos.y = targetPos.y + maxOffset - minOffset;
        minimizedPanel.localPosition = targetPos;

        maximizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                maximizedPanel.gameObject.SetActive(false);

                minimizedPanel.gameObject.SetActive(true);
                minimizedPanel.localScale = Vector3.zero;
                minimizedPanel.DOScale(minPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }

    public void MaximizePanel()
    {
        if (maximizedPanel == null || minimizedPanel == null) return;
        isMinimized = false;

        // PopupHideController와 동일한 방식으로 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = minimizedPanel.localPosition;
        targetPos.y = targetPos.y + minOffset - maxOffset;
        maximizedPanel.localPosition = targetPos;

        minimizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                minimizedPanel.gameObject.SetActive(false);

                maximizedPanel.gameObject.SetActive(true);
                maximizedPanel.localScale = Vector3.zero;
                maximizedPanel.DOScale(maxPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }
}
