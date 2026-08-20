using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정보 팝업(Popup_Info). 정보 앱의 "특성" 탭을 대체한다.
/// 이번 런에서 고른 식물과 그 식물의 특성 레벨, 일반 특성을 보여준다.
///
/// 값은 <see cref="AbilityManager"/>에서 읽으며 표시 규칙은 정보 앱(<see cref="InfoApp"/>)과 같다.
/// 행은 PlantTraitRow / CommonTraitRow가 채운다. (프리팹에 붙여 인스펙터에서 연결)
/// 특성 설명은 마우스를 올리면 <see cref="HoverTooltip"/>으로 뜬다.
/// </summary>
public class InfoPopup : MonoBehaviour
{
    [Header("현재 식물")]
    [SerializeField] private Image plantImage;
    [SerializeField] private TMP_Text plantNameText;
    [Tooltip("식물 설명. 없으면 생략")]
    [SerializeField] private TMP_Text plantDescriptionText;

    [Header("식물 특성 (PlantTrait > Scroll View > Viewport > Content)")]
    [SerializeField] private Transform plantTraitContent;
    [SerializeField] private GameObject plantTraitPrefab;

    [Header("일반 특성 (CommonTrait > Scroll View > Viewport > Content)")]
    [SerializeField] private Transform commonTraitContent;
    [SerializeField] private GameObject commonTraitPrefab;

    private readonly List<GameObject> spawned = new List<GameObject>();

    public void Open()
    {
        // 꺼져 있으면 켜는 것만으로 OnEnable이 Refresh를 부른다.
        if (gameObject.activeSelf) Refresh();
        else gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void OnEnable() => Refresh();

    /// <summary>팝업 내용을 다시 만든다.</summary>
    public void Refresh()
    {
        Clear(plantTraitContent);
        Clear(commonTraitContent);
        spawned.Clear();

        var ability = AbilityManager.Instance;
        if (ability == null) return;

        PlayablePlantType plantType = ability.GetCurrentPlantType();
        PlantInfoData info = ability.GetPlantInfo(plantType);

        ShowPlant(ability, info);
        ShowPlantTraits(ability, plantType);
        ShowGeneralTraits(ability);
    }

    // ── 현재 식물 ─────────────────────────────────────────────────────────────

    private void ShowPlant(AbilityManager ability, PlantInfoData info)
    {
        if (plantNameText != null)
        {
            // info가 없으면 AbilityManager가 들고 있는 이름으로 대체 (정보 앱과 같은 규칙)
            plantNameText.text = (info != null && !string.IsNullOrEmpty(info.plantName))
                ? info.plantName
                : ability.CurrentPlantName;
        }

        if (plantImage != null)
        {
            Sprite icon = info != null ? info.icon : null;
            plantImage.sprite = icon;
            plantImage.enabled = icon != null;
        }

        if (plantDescriptionText != null)
            plantDescriptionText.text = info != null ? info.description : "";
    }

    // ── 식물 특성 ─────────────────────────────────────────────────────────────

    /// <summary>현재 식물의 특성을 전부 보여주고, 배운 만큼 레벨 바를 채운다.</summary>
    private void ShowPlantTraits(AbilityManager ability, PlayablePlantType plantType)
    {
        if (plantTraitContent == null || plantTraitPrefab == null) return;

        var learned = ability.CurrentPlantAbility;

        foreach (var data in ability.GetAllPlantAbility())
        {
            if (data == null || data.type != plantType) continue;

            var owned = learned != null ? learned.Find(a => a != null && a.abilityName == data.abilityName) : null;
            int level = owned != null ? owned.level : 0;

            GameObject row = Spawn(plantTraitPrefab, plantTraitContent);

            var view = row.GetComponent<PlantTraitRow>();
            if (view == null)
            {
                Debug.LogWarning("[InfoPopup] Plant Trait Prefab에 PlantTraitRow가 없습니다. " +
                                 "프리팹에 컴포넌트를 붙이고 인스펙터에서 연결하세요.");
                continue;
            }

            view.Setup(data.icon, data.abilityName, level, AbilityManager.MaxPlantAbilityLevel);
            SetTooltip(row, data.abilityName, data.description, level);
        }
    }

    // ── 일반 특성 ─────────────────────────────────────────────────────────────

    /// <summary>보유한 일반 특성을 칸 수만큼 보여준다. 아직 열리지 않은 칸은 잠김으로.</summary>
    private void ShowGeneralTraits(AbilityManager ability)
    {
        if (commonTraitContent == null || commonTraitPrefab == null) return;

        var owned = ability.CurrentGeneralAbility;
        int unlockedSlots = ability.GetGeneralAbilityPoint();

        for (int i = 0; i < AbilityManager.MaxGeneralAbilitySlots; i++)
        {
            GameObject row = Spawn(commonTraitPrefab, commonTraitContent);

            var view = row.GetComponent<CommonTraitRow>();
            if (view == null)
            {
                Debug.LogWarning("[InfoPopup] Common Trait Prefab에 CommonTraitRow가 없습니다. " +
                                 "프리팹에 컴포넌트를 붙이고 인스펙터에서 연결하세요.");
                continue;
            }

            bool hasAbility = owned != null && i < owned.Count && owned[i] != null;
            bool locked = i >= unlockedSlots;

            if (hasAbility)
            {
                var data = owned[i];
                view.Setup(data.icon, data.abilityName);
                SetTooltip(row, data.abilityName, data.description, 0);
            }
            else
            {
                string label = locked ? "잠김" : "빈 슬롯";
                view.Setup(null, label);
                SetTooltip(row, label,
                    locked ? "유전자를 모아 특성 칸을 열 수 있습니다." : "아직 특성을 고르지 않았습니다.", 0);
            }
        }
    }

    // ── 행 채우기 헬퍼 ────────────────────────────────────────────────────────

    private GameObject Spawn(GameObject prefab, Transform parent)
    {
        GameObject row = Instantiate(prefab, parent);
        row.SetActive(true);
        spawned.Add(row);
        return row;
    }

    private static void SetTooltip(GameObject row, string name, string description, int level)
    {
        var hover = row.GetComponent<UIHoverHandler>();
        if (hover == null) hover = row.AddComponent<UIHoverHandler>();

        string header = level > 0 ? $"{name} Lv{level}" : name;
        string content = string.IsNullOrEmpty(description) ? header : $"{header}\n{description}";

        hover.Setup(() => HoverTooltip.ShowFor(content), HoverTooltip.HideCurrent);
    }

    /// <summary>에디터에서 넣어 둔 예시 행도 함께 비운다.</summary>
    private static void Clear(Transform content)
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);

            // Destroy는 프레임 끝에 처리되므로 먼저 떼어낸다.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
