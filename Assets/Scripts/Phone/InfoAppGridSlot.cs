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
    [SerializeField] private GameObject coolerIndicator;
    [SerializeField] private GameObject sprinklerIndicator;
    [SerializeField] private GameObject absorbFertilizerIndicator;

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
            sb.AppendLine("- 황금 흙: 모든 저항력 90% 고정, 이동 불가");
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
            sb.AppendLine($"- 비료: {fertilizerType} 저항력 +5%");
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
            sb.AppendLine("- 페트병: 사망 1회 방지, 이동 불가");
            hasEffect = true;
        }

        // 4. 고추
        bool isChili = IsAffectedByChiliPepper(gridIndex, grid);
        if (chiliIndicator) chiliIndicator.SetActive(isChili);

        if (isChili)
        {
            sb.AppendLine("- 매운 맛: 우성 형질 저항력 +20%");
            hasEffect = true;
        }

        // 5. 냉각기
        bool isFrozen = IsPlantFrozen(gridIndex, grid);
        if (coolerIndicator) coolerIndicator.SetActive(isFrozen);

        if (isFrozen)
        {
            sb.AppendLine("- 냉각기: 식물 빙결 상태 (피해 면역)");
            hasEffect = true;
        }

        // 6. 스프링클러
        bool isSprinkler = grid.IsAffectedBySprinkler(gridIndex);
        if (sprinklerIndicator) sprinklerIndicator.SetActive(isSprinkler);

        if (isSprinkler)
        {
            sb.AppendLine("- 스프링클러: 수분 공급 (비료 시너지 효과 포함)");
            hasEffect = true;
        }

        // 7. 저항력흡수비료
        bool isAbsorbFertilizer = grid.HasAbsorbFertilizer(gridIndex);
        if (absorbFertilizerIndicator) absorbFertilizerIndicator.SetActive(isAbsorbFertilizer);

        if (isAbsorbFertilizer)
        {
            sb.AppendLine("- 저항력 흡수 비료: 주변 식물의 저항력을 지속 흡수");
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

    private bool IsPlantFrozen(int gridIndex, Grid grid)
    {
        if (grid == null) return false;
        return grid.IsPlantFrozen(gridIndex);
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
