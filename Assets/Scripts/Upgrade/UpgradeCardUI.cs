using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour
{
    public Image iconSprite;
    public TextMeshProUGUI upgradeInfo;

    public void Set(Upgrade upgradeData)
    {
        iconSprite.sprite = upgradeData.Icon;
        upgradeInfo.text = upgradeData.Explanation;
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
