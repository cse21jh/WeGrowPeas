using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InfoApp : BasePhoneApp
{
    [Header("Tabs")]
    [SerializeField] private Button characteristicsTabButton;
    [SerializeField] private Button purchasesTabButton;
    [SerializeField] private Button gridInfoTabButton;

    [Header("Panels")]
    [SerializeField] private GameObject characteristicsPanel;
    [SerializeField] private GameObject purchasesPanel;
    [SerializeField] private GameObject gridInfoPanel;

    [Header("Characteristics UI")]
    [SerializeField] private Image currentPlantIcon;
    [SerializeField] private TMP_Text currentPlantName;
    [SerializeField] private Transform plantAbilityContainer;
    [SerializeField] private InfoAppItemSlot plantAbilitySlotPrefab; // Reusing ItemSlot for list items
    [SerializeField] private Transform generalAbilityContainer; // Horizontal Layout
    [SerializeField] private InfoAppItemSlot generalAbilitySlotPrefab; // Circular slot

    [Header("Purchases UI")]
    [SerializeField] private Transform purchasesContainer;
    [SerializeField] private InfoAppItemSlot purchaseSlotPrefab;

    [Header("Grid Info UI")]
    [SerializeField] private Transform gridContainer;
    [SerializeField] private InfoAppGridSlot gridSlotPrefab;
    [SerializeField] private int gridColumnCount = 4; // Default column count

    [Header("Description UI")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text descriptionText;

    private Dictionary<string, ItemData> itemLookup;

    private void Start()
    {
        // Tab Button Listeners
        if (characteristicsTabButton) characteristicsTabButton.onClick.AddListener(ShowCharacteristics);
        if (purchasesTabButton) purchasesTabButton.onClick.AddListener(ShowPurchases);
        if (gridInfoTabButton) gridInfoTabButton.onClick.AddListener(ShowGridInfo);

        // Build Item Lookup Dictionary once
        BuildItemLookup();
    }

    public override void OnCreate(PhoneManager phone)
    {
        base.OnCreate(phone);
    }

    private void OnEnable()
    {
        OnShow();
    }

    public override void OnShow()
    {
        base.OnShow();
        ShowCharacteristics(); // Default tab
    }

    private void BuildItemLookup()
    {
        itemLookup = new Dictionary<string, ItemData>();
        
        // Fixed Items
        // Accessing private field via ShopManager instance (assuming they are accessible or we iterate known items)
        // ShopManager doesn't expose fixedItems publicly as a list, but we can try loading from Resources if needed
        // Or we rely on ShopManager having a way to get item data.
        // For now, let's load all items from Resources as a fallback/primary method.
        var allItems = Resources.LoadAll<ItemData>("Data/Item Data");
        foreach (var item in allItems)
        {
            if (item != null && !itemLookup.ContainsKey(item.name))
            {
                itemLookup.Add(item.name, item);
            }
            // Also add by DisplayName if unique? Maybe not, duplicate display names possible.
            // PurchaseHistory uses item.name (or DisplayName?) -> ShopManager says: 
            // var key = string.IsNullOrEmpty(data.DisplayName) ? data.name : data.DisplayName;
            // So we need to match that logic.
            string key = string.IsNullOrEmpty(item.DisplayName) ? item.name : item.DisplayName;
            if (!itemLookup.ContainsKey(key))
            {
                itemLookup.Add(key, item);
            }
        }
    }

    private void ShowCharacteristics()
    {
        SetActivePanel(characteristicsPanel);
        ClearDescription();

        // 0. Plant Info (Top Left)
        if (AbilityManager.Instance != null)
        {
            var plantType = AbilityManager.Instance.GetCurrentPlantType();
            var info = AbilityManager.Instance.GetPlantInfo(plantType);
            
            if (currentPlantName != null) 
            {
                // info가 유효하면 그 이름 사용, 아니면 기존 CurrentPlantName 사용 (Fallback)
                if (!string.IsNullOrEmpty(info.plantName))
                    currentPlantName.text = info.plantName;
                else
                    currentPlantName.text = AbilityManager.Instance.CurrentPlantName;
            }

            if (currentPlantIcon != null)
            {
                if (info.icon != null)
                {
                    currentPlantIcon.sprite = info.icon;
                    currentPlantIcon.gameObject.SetActive(true);
                }
                else
                {
                    currentPlantIcon.gameObject.SetActive(false);
                }
            }
        }

        // 1. Plant Abilities
        ClearContainer(plantAbilityContainer);
        if (AbilityManager.Instance != null && plantAbilitySlotPrefab != null)
        {
            var allPlantAbilities = AbilityManager.Instance.GetAllPlantAbility();
            var currentPlantAbilities = AbilityManager.Instance.CurrentPlantAbility;
            var currentPlantType = AbilityManager.Instance.GetCurrentPlantType();

            foreach (var ability in allPlantAbilities)
            {
                if (ability == null) continue;
                if (ability.type != currentPlantType) continue; // Filter by current plant type

                // Check if ability is learned (in currentPlantAbilities)
                // Assuming ability instances are unique or compare by name/ID
                // PlantAbilityData is ScriptableObject so instance check *might* work if not instantiated runtime copies
                // But better to check by name or reference if 'currentPlantAbilities' holds references to 'allPlantAbilities' elements.
                
                var learnedAbility = currentPlantAbilities.Find(a => a.abilityName == ability.abilityName);
                
                int level = (learnedAbility != null) ? learnedAbility.level : 0;
                string desc = ability.description; // Use base description

                var slot = Instantiate(plantAbilitySlotPrefab, plantAbilityContainer);
                slot.Setup(ability.icon, ability.abilityName, level, desc, UpdateDescription, ClearDescription);
                
                // Use Level Bar visualization
                // Assuming max level is 5 for now
                slot.SetupLevel(level, 5); 
                
                slot.gameObject.SetActive(true);
            }
        }

        // 2. General Abilities
        ClearContainer(generalAbilityContainer);
        if (AbilityManager.Instance != null && generalAbilitySlotPrefab != null)
        {
            var generalAbilities = AbilityManager.Instance.CurrentGeneralAbility;
            int maxSlots = 3; // Fixed max slots for general abilities? Or dynamic?
            // Assuming 3 for now based on request "Use lock icon if not available"
            
            for (int i = 0; i < maxSlots; i++)
            {
                var slot = Instantiate(generalAbilitySlotPrefab, generalAbilityContainer);
                
                if (i < generalAbilities.Count && generalAbilities[i] != null)
                {
                    var ability = generalAbilities[i];
                    bool isUnlocked = AbilityManager.Instance.IsGeneralAbilityDataUnlocked.ContainsKey(ability.name) 
                                      && AbilityManager.Instance.IsGeneralAbilityDataUnlocked[ability.name];
                    
                    // If locked logic is needed, handled here. 
                    // But CurrentGeneralAbility usually contains *equipped* abilities.
                    // If the request implies showing "Potential" slots vs "Equipped", we might need logic adjustment.
                    // "General characteristics ... lock icon if slot is unavailable"
                    // If AbilityManager manages slots, we check that. 
                    // Let's assume CurrentGeneralAbility has the equipped ones.
                    
                    slot.Setup(ability.icon, ability.abilityName, 0, ability.description, UpdateDescription, ClearDescription, !isUnlocked);
                }
                else
                {
                    // Empty or Locked Slot
                     // How to determine if it's just empty or locked? 
                     // AbilityManager.GeneralAbilityPoint seems to track available points/slots?
                     // Let's simplified: If index >= GeneralAbilityPoint, it's Locked. Else Empty.
                     bool isLocked = (i >= AbilityManager.Instance.GetGeneralAbilityPoint());
                     string desc = isLocked ? "잠김" : "빈 슬롯";
                     slot.Setup(null, "", 0, desc, UpdateDescription, ClearDescription, isLocked);
                }
                slot.gameObject.SetActive(true);
            }
        }
    }

    private void ShowPurchases()
    {
        SetActivePanel(purchasesPanel);
        ClearDescription();
        ClearContainer(purchasesContainer);

        if (ShopManager.Instance != null && purchaseSlotPrefab != null)
        {
            foreach (var kvp in ShopManager.Instance.PurchaseHistory)
            {
                string itemName = kvp.Key;
                int count = kvp.Value;

                if (count <= 0) continue;

                if (itemLookup.TryGetValue(itemName, out ItemData itemData))
                {
                    var slot = Instantiate(purchaseSlotPrefab, purchasesContainer);
                    slot.Setup(itemData.Icon, itemData.DisplayName, count, itemData.Description, UpdateDescription, ClearDescription);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    // Fallback if ItemData not found (shouldn't happen if Lookup is complete)
                    var slot = Instantiate(purchaseSlotPrefab, purchasesContainer);
                    slot.Setup(null, itemName, count, "설명 없음", UpdateDescription, ClearDescription);
                    slot.gameObject.SetActive(true);
                }
            }
        }
    }

    private void ShowGridInfo()
    {
        SetActivePanel(gridInfoPanel);
        ClearDescription();
        ClearContainer(gridContainer);

        if (GameManager.Instance != null && GameManager.Instance.grid != null && gridSlotPrefab != null)
        {
            Grid grid = GameManager.Instance.grid;
            int maxCol = grid.GetMaxCol();
            int totalCells = maxCol * 4;
            
            for (int i = 0; i < totalCells; i++)
            {
                var slot = Instantiate(gridSlotPrefab, gridContainer);

                slot.Setup(i, grid, UpdateDescription, ClearDescription);
            }

            // Force rebuild layout to ensure ContentSizeFitter and RectMask2D work correctly
            // Ensure ContentSizeFitter exists to work with ScrollRect
            var csf = gridContainer.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = gridContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Check if parent has RectMask2D which might cause issues
            var parentMask = gridContainer.GetComponentInParent<RectMask2D>();
            if (parentMask != null)
            {
                // If persistent issues occur, suggest switching to Mask component
                // Debug.LogWarning("[InfoApp] GridContainer is under a RectMask2D. If items are invisible, try replacing RectMask2D with a standard Mask component on the Viewport.");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainer.GetComponent<RectTransform>());
        }
    }

    private void SetActivePanel(GameObject activePanel)
    {
        if (characteristicsPanel) characteristicsPanel.SetActive(characteristicsPanel == activePanel);
        if (purchasesPanel) purchasesPanel.SetActive(purchasesPanel == activePanel);
        if (gridInfoPanel) gridInfoPanel.SetActive(gridInfoPanel == activePanel);
    }

    private void UpdateDescription(string text)
    {
        if (descriptionPanel) descriptionPanel.SetActive(true);
        if (descriptionText) descriptionText.text = text;
    }

    private void ClearDescription()
    {
        if (descriptionPanel) descriptionPanel.SetActive(false);
        if (descriptionText) descriptionText.text = "";
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
