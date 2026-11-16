using System;
using UnityEngine;

/// <summary>
/// 치장으로 그림이 바뀌는 부위. 아이템(<see cref="CustomizeItemData.parts"/>)은 부위마다 그림을 하나씩 갖고,
/// 씬의 <see cref="CustomizeTarget"/>은 자기가 맡은 부위의 그림을 찾아 끼운다.
///
/// 각 부위의 기본 그림(원본 파일)은 아래 주석에 적어 둔다.
/// 에셋에는 숫자로 저장되므로 이름만 바꿔도 연결은 유지된다.
///
/// 새 부위를 추가할 때는 아직 안 쓴 숫자를 붙이고(기존 숫자를 바꾸면 에셋의 값이 다른 부위로 바뀐다)
/// <see cref="CustomizePartInfo.CategoryOf"/>에 종류를 적는다.
/// </summary>
public enum CustomizePart
{
    // 토양
    SoilGround = 0,   // 텃밭 흙 (지금은 흙+통나무 테두리가 한 장: Background/garden_4.png)
    SoilCell = 1,     // 텃밭 칸 32개 (지금은 그림 없음. 칸마다 흙 타일을 쓸 경우)

    // 목재
    WoodBorder = 10,  // 텃밭 테두리 나무밑동 (garden_4에서 분리 예정, 아직 연결된 대상 없음)
    // 11: 예전 WoodFence(울타리). UI의 UIFence로 옮겼으므로 다시 쓰지 않는다.

    // 풀밭
    GrassTexture = 20, // 바닥 풀잎 텍스처, 풀 메시 (Grass/grassss.png)
    Bush1 = 22,        // 모퉁이 수풀, 모퉁이마다 2개씩 (Background/BushBlur_1.png)
    Bush2 = 23,        // 모퉁이 수풀, 모퉁이마다 1개씩 (Background/BushBlur_2.png)

    // 케이스
    PhoneSmall = 30,  // 접힌 휴대전화, 오른쪽 Tab 버튼 (Legacy/ReNew/Btn_phoneToggleNew1.png)
    PhoneLarge = 31,  // 펼친 휴대전화 프레임 (Renewal/Frame1.png)

    // UI
    UIFence = 40,        // 형질 울타리, 화면 아래 캔버스 울타리 (Background/fence_re.png)
    UITimeBar = 41,      // 타임바 바탕 (Legacy/ReNew/BG_TimeBar.png)
    UIBtnWood = 42,      // 나무 버튼, 여러 씬·프리팹 (Legacy/ReNew/Btn_wood.png) - 에디터 도구로 일괄 연결
    UIBaseWood = 43,     // 나무 받침 (Legacy/ReNew/Base_wood.png) - 에디터 도구로 일괄 연결
    UIBaseLongWood = 44, // 긴 나무 받침 (Legacy/ReNew/Base_longWood.png) - 에디터 도구로 일괄 연결
}

public static class CustomizePartInfo
{
    /// <summary>부위가 속한 치장 종류.</summary>
    public static CustomizeCategory CategoryOf(CustomizePart part)
    {
        switch (part)
        {
            case CustomizePart.SoilGround:
            case CustomizePart.SoilCell:
                return CustomizeCategory.Soil;
            case CustomizePart.WoodBorder:
                return CustomizeCategory.Wood;
            case CustomizePart.GrassTexture:
            case CustomizePart.Bush1:
            case CustomizePart.Bush2:
                return CustomizeCategory.Grass;
            case CustomizePart.PhoneSmall:
            case CustomizePart.PhoneLarge:
                return CustomizeCategory.PhoneCase;
            case CustomizePart.UIFence:
            case CustomizePart.UITimeBar:
            case CustomizePart.UIBtnWood:
            case CustomizePart.UIBaseWood:
            case CustomizePart.UIBaseLongWood:
                return CustomizeCategory.UI;
            default:
                return CustomizeCategory.Decoration;
        }
    }
}

/// <summary>아이템 하나가 한 부위에 쓰는 그림.</summary>
[Serializable]
public class CustomizePartSprite
{
    [Tooltip("이 그림을 끼울 부위.")]
    public CustomizePart part;

    [Tooltip("끼울 그림. 비우면 원래 그림을 그대로 쓴다.")]
    public Sprite sprite;
}
