using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconSprite;
    public TextMeshProUGUI upgradeInfo;

    private int slotIndex;
    private UpgradeManager upgradeManager;

    public void Set(Upgrade upgradeData, int index, UpgradeManager mngr)
    {
        iconSprite.sprite = upgradeData.Icon;
        upgradeInfo.text = upgradeData.Explanation;

        slotIndex = index;
        upgradeManager = mngr;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        upgradeManager.SelectUpgrade(slotIndex);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
