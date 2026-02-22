using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Text;

public class InfoAppGridSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image baseImage;
    [SerializeField] private GameObject goldSoilIndicator;
    [SerializeField] private Image fertilizerIndicator; // 색상 변경용
    [SerializeField] private GameObject petBottleIndicator;
    [SerializeField] private GameObject chiliIndicator;

    [Header("Colors")]
    [SerializeField] private Color[] fertilizerColors; // WaveType 순서와 일치해야 함

    private string tooltipText;
    private Action<string> onHover;
    private Action onHoverExit;

    public void Setup(int gridIndex, Grid grid, Action<string> hoverCallback, Action hoverExitCallback)
    {
        onHover = hoverCallback;
        onHoverExit = hoverExitCallback;

        if (grid == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Grid {gridIndex}");     // <b> 태그 제거 (폰트 깨짐 발생)

        bool hasEffect = false;
        
        // 1. 황금 흙
        bool isGoldSoil = grid.HasGoldSoil(gridIndex);
        if (goldSoilIndicator) goldSoilIndicator.SetActive(isGoldSoil);
        
        if (isGoldSoil)
        {
            sb.AppendLine("- <color=yellow>황금 흙</color>: 모든 저항력 90% 고정, 이동 불가");
            hasEffect = true;
        }

        // 2. 비료
        // 2. 비료
        if (grid.TryGetFertilizerType(gridIndex, out var fertilizerType))
        {
            if (fertilizerIndicator)
            {
                fertilizerIndicator.gameObject.SetActive(true);
                int typeIndex = (int)fertilizerType;
                if (fertilizerColors != null && typeIndex >= 0 && typeIndex < fertilizerColors.Length)
                {
                    fertilizerIndicator.color = fertilizerColors[typeIndex];
                }
            }
            sb.AppendLine($"- <color=green>비료</color>: {fertilizerType} 저항력 +5%");
            hasEffect = true;
        }
        else
        {
            if (fertilizerIndicator) fertilizerIndicator.gameObject.SetActive(false);
        }

        // 3. 페트병
        bool isPetBottle = grid.HasPetBottle(gridIndex);
        if (petBottleIndicator) petBottleIndicator.SetActive(isPetBottle);

        if (isPetBottle)
        {
            sb.AppendLine("- <color=blue>페트병</color>: 사망 1회 방지, 이동 불가");
            hasEffect = true;
        }

        // 4. 고추
        bool isChili = IsAffectedByChiliPepper(gridIndex, grid);
        if (chiliIndicator) chiliIndicator.SetActive(isChili);

        if (isChili)
        {
            sb.AppendLine("- <color=red>매운 맛</color>: 우성 형질 저항력 +20%");
            hasEffect = true;
        }

        if (!hasEffect)
        {
            sb.AppendLine("효과 없음 (일반 토양)");
        }

        tooltipText = sb.ToString();
    }

    private bool IsAffectedByChiliPepper(int gridIndex, Grid grid)
    {
        if (grid == null) return false;
        return grid.IsAffectedByChiliPepper(gridIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
