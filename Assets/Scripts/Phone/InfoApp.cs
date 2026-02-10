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

        // 1. Plant Abilities
        ClearContainer(plantAbilityContainer);
        if (AbilityManager.Instance != null && plantAbilitySlotPrefab != null)
        {
            var plantAbilities = AbilityManager.Instance.CurrentPlantAbility;
            foreach (var ability in plantAbilities)
            {
                if (ability == null) continue;
                var slot = Instantiate(plantAbilitySlotPrefab, plantAbilityContainer);
                slot.Setup(ability.icon, ability.abilityName, ability.level, ability.description, UpdateDescription, ClearDescription);
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

            // Update GridLayoutGroup constraint if needed
            GridLayoutGroup glg = gridContainer.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4; // Rows are dynamic, Columns are fixed to 4 (per column in Grid.cs logic usually 4 rows per col? No, Grid.cs: 4 rows per col, maxCol cols.)
                // Wait, Grid.cs structure: index = col * 4 + row. 
                // Visual representation: usually we want Columns to be vertical.
                // If GridLayoutGroup fills Horizontal first:
                // Cell 0, 1, 2, 3 ...
                // Grid.cs index: 0 is (Col 0, Row 0), 1 is (Col 0, Row 1).
                // So if we want to visually match the game grid (Active Grid is Horizontal?):
                // Game View: Low index left?
                // Let's assume standard iteration 0 to MAX matches the visual order 
                // or we adhere to "Col * 4 + Row". 
                // If the game world is: Cols are X axis, Rows are Y axis (4 rows).
                // Then gridIndex 0,1,2,3 are the first Column (Vertical strip).
                // To visualize this in a GridLayoutGroup that usually fills Rows first:
                // We might need to rearrange parsing or setup LayoutGroup to "Start Axis: Vertical".
                // If "Start Axis: Vertical", then elements 0,1,2,3 go down, then next column.
                // That matches Grid.cs indexing perfectly!
                
                glg.startAxis = GridLayoutGroup.Axis.Vertical;
                glg.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                glg.constraintCount = 4; 
            }

            for (int i = 0; i < totalCells; i++)
            {
                var slot = Instantiate(gridSlotPrefab, gridContainer);
                
                // Ensure the GameObject and components are enabled
                slot.gameObject.SetActive(true);
                slot.enabled = true; 
                
                // Force enable Image if present on root (covers the case where baseImage is root)
                var img = slot.GetComponent<Image>();
                if (img) img.enabled = true;

                slot.Setup(i, grid, UpdateDescription, ClearDescription);
            }
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
