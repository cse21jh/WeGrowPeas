using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemController : MonoBehaviour
{
    [SerializeField] private Image itemImage;

    [SerializeField] private GameObject detail_ItemType;
    [SerializeField] private GameObject detail_ItemGrade;
    [SerializeField] private GameObject detail_ItemLimit;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemPrice;
    [SerializeField] private TextMeshProUGUI itemPurchaseLimit;

    [SerializeField] private GameObject[] itemTags;
    [SerializeField] private TextMeshProUGUI[] itemTagTexts;



    #region
    /// <summary>
    /// 
    /// "도와줘요 현민에몽 준하에몽"
    /// 
    /// </summary>
    /// <param name="image"></param>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="countLimit"></param>
    /// <param name="tags"> string으로 일단 임시 설정해 두었으나, 필요시 별도 클래스 활용할 것 </param>
    public void SetItemDetail(Sprite image, string name, int price, int countLimit, string[] tags)
    {

    }
    #endregion



}
