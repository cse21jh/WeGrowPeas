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
        sb.AppendLine($"<b>Grid {gridIndex}</b>");

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
        bool hasFertilizer = grid.HasFertilizerAt(gridIndex);
        if (hasFertilizer)
        {
            var fertilizerCols = grid.GetFertilizerColumns();
            int col = gridIndex / 4;
            
            if (fertilizerCols.TryGetValue(col, out var fertilizerType))
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
                // This shouldn't happen if HasFertilizerAt relies on the same dictionary, but safe fallback
                 if (fertilizerIndicator) fertilizerIndicator.gameObject.SetActive(false);
            }
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
        // Grid.cs의 로직과 동일하게 구현해야 함.
        // 하지만 Grid에 이 로직이 캡슐화되어 있지 않아서 Plant.CheckChiliPepper를 참고하여 구현.
        // 로직 중복이 발생하지만, 원본 코드 수정 없이 구현하려면 이 방법이 안전함.
        
        int rangeLevel = grid.ChiliPepperRangeLevel;
        
        // 주변 타일 검사 헬퍼
        bool CheckTile(int targetIdx)
        {
            if (grid.plantGrid.TryGetValue(targetIdx, out Plant plant))
            {
                return plant is ChiliPepper;
            }
            return false;
        }

        int maxIndex = grid.GetMaxCol() * 4;

        // 0단계: 좌우
        if (gridIndex - 4 >= 0 && CheckTile(gridIndex - 4)) return true;
        if (gridIndex + 4 < maxIndex && CheckTile(gridIndex + 4)) return true;

        if (rangeLevel >= 1)
        {
            // 상하 (같은 컬럼 내)
            if ((gridIndex - 1) / 4 == gridIndex / 4 && CheckTile(gridIndex - 1)) return true;
            if ((gridIndex + 1) / 4 == gridIndex / 4 && CheckTile(gridIndex + 1)) return true;
        }

        if (rangeLevel >= 2)
        {
            // 대각선
            // 왼쪽 위
            if (gridIndex - 5 >= 0 && (gridIndex - 5) % 4 == (gridIndex - 1) % 4 && CheckTile(gridIndex - 5)) return true;
            // 오른쪽 위
            if (gridIndex + 3 < maxIndex && (gridIndex + 3) % 4 == (gridIndex - 1) % 4 && CheckTile(gridIndex + 3)) return true; // 이부분 원본 코드에 버그 있는듯? (gridIndex - 3으로 되어있음) -> 원본: (gridIndex + 3) >= 0 ... CheckTile(gridIndex - 3) ??? 오타같음.
            // 일단 원본 동작을 따라가되, 여기선 gridIndex + 3 위치를 체크해야 논리적으로 맞음. 
            // 원본 코드: if ((gridIndex + 3) >= 0 && ... { if (grid.plantGrid.TryGetValue(gridIndex - 3, ... }
            // 원본이 (gridIndex - 3)을 체크하고 있다면, 내 구현도 (gridIndex - 3)을 체크하는게 맞을 수도 있지만, 
            // 문맥상 "오른쪽 위"라면 (Col+1, Row-1) 이므로 index + 4 - 1 = index + 3 이 맞음.
            // 원본의 버그 여부와 상관없이 논리적으로 맞는 구현을 하거나, 안전하게 상하좌우만 체크? 
            // 일단 '오른쪽 위'는 index + 3이 맞음.
            
            // 왼쪽 아래
            if (gridIndex - 3 >= 0 && (gridIndex - 3) % 4 == (gridIndex + 1) % 4 && CheckTile(gridIndex - 3)) return true; // 이것도 원본은 gridIndex + 3을 체크하고 있음.
             // 오른쪽 아래
            if (gridIndex + 5 < maxIndex && (gridIndex + 5) % 4 == (gridIndex + 1) % 4 && CheckTile(gridIndex + 5)) return true;
        }

        return false;
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
