using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ShopManager : Singleton<ShopManager>
{
    // 코드가 직접 찾는 아이템의 구매 이력 키. ItemData.PurchaseKey와 같은 값(에셋 이름)이다.
    public const string ShopContactKey = ShopState.ShopContactKey;
    public const string LandDeedKey = "AddSoilItem";
    public const string BreedingKitKey = "BreedCountItemData";

    [Header("Inventory (Serialized here)")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[4];  // 상단 고정 4종
    [SerializeField] private List<ItemData> rotationPool = new(); // 하단 로테이션 풀 (자동으로 Rotation 폴더에서 로드됨)
    [SerializeField] private int rotationCount = 4;                    // 하단 슬롯 개수

    /// <summary>이번 판의 상점 상태. 판마다 새로 만들어진다.</summary>
    public ShopState State => GameRun.Current.Shop;
    public Dictionary<string, int> PurchaseHistory => State.PurchaseHistory;
    public int DailyRerollCount => State.DailyRerollCount;

    /// <summary>
    /// 신용카드: 3슬롯 이상 구매한 날이면 소모 비용 * refundPercent 를 환급액으로 반환한다.
    /// 호출과 동시에 일일 구매 집계를 초기화하므로 자유시간 시작 시 1회만 호출할 것.
    /// </summary>
    public int ConsumeDailyPurchaseRefund(float refundPercent) => State.ConsumeDailyPurchaseRefund(refundPercent);


    /// <summary>
    /// 매일 상점 무료 리롤 가능 횟수를 추가합니다.
    /// </summary>
    public void AddDailyRerollCount(int count) => State.AddDailyRerolls(count);

    /// <summary>
    /// 리롤을 사용합니다. 리롤 횟수가 남아있으면 true를 반환합니다.
    /// </summary>
    public bool UseReroll()
        => GameManager.Instance?.Tutorial?.CanUseShop != false && State.UseFreeReroll();

    void Awake()
    {
        LoadRotationItems();
    }

    /// <summary>
    /// Resources/Data/Item Data/Rotation 폴더에서 모든 ItemData를 자동으로 로드하여 rotationPool에 추가합니다.
    /// rotationPool이 비어있을 때만 자동으로 로드합니다.
    /// </summary>
    private void LoadRotationItems()
    {
        // rotationPool이 이미 설정되어 있으면 자동 로드하지 않음
        if (rotationPool != null && rotationPool.Count > 0)
        {
            return;
        }

        // Resources 폴더 기준 상대 경로로 모든 ItemData 로드
        ItemData[] items = Resources.LoadAll<ItemData>(ResourcePaths.RotationItems);
        
        if (items != null && items.Length > 0)
        {
            rotationPool.Clear(); // 명확성을 위해 비움 (이미 비어있지만)
            rotationPool.AddRange(items);
            Debug.Log($"Rotation 아이템 {items.Length}개 자동 로드 완료");
        }
        else
        {
            Debug.LogWarning("Rotation 폴더에서 아이템을 찾을 수 없습니다. 경로를 확인해주세요: Resources/Data/Item Data/Rotation");
        }
    }

    

    // ShopUI가 호출해 쓰는 진입점: 현재 스테이지/컨텍스트로 인벤토리 생성
    public ShopInventory GenerateInventory(ShopContext ctx, int currentDay)
        => State.Generate(fixedItems, rotationPool, rotationCount, ctx, currentDay,
            // 해금 시스템(해금 전엔 상점에 안 뜸)과 메타 해금(새벽 클리어·인게임 사건)
            item => UnlockManager.IsAvailable(item) && item.IsMetaUnlocked());

    /// <summary>
    /// 게임 고유 시드를 초기화합니다. 새 게임일 때만 호출됩니다.
    /// </summary>
    public void InitializeGameSeed() => Debug.Log($"[ShopManager] 게임 고유 시드: {State.UniqueSeed}");

    public bool TryPurchase(ShopContext ctx, ItemData data, ShopInventory inventory)
        => GameManager.Instance?.Tutorial?.CanUseShop != false && data != null && State.TryPurchase(data, ctx, inventory);

    public string ReturnMostPurchasedItem()
    {
        if (PurchaseHistory.Count == 0) return null;

        var mostSold = PurchaseHistory
        .Where(item => item.Key != LandDeedKey && item.Key != BreedingKitKey)
        .OrderByDescending(item => item.Value)
        .FirstOrDefault();

        return mostSold.Key;
    }

    /// <summary>구매 이력과 시드를 저장 데이터에 담는다. <see cref="LoadShopManager"/>와 짝.</summary>
    public void SaveShopManager(ShopSave save) => State.Save(save);

    public void LoadShopManager(ShopSave saveData) => State.Load(saveData);

    /// <summary>
    /// 게임 고유 시드를 반환합니다.
    /// </summary>
    public int GetGameUniqueSeed() => State.UniqueSeed;
}
