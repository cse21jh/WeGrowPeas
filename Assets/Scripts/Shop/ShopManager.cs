using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ShopInventory
{
    public List<ItemData> Fixed = new();
    public List<ItemData> Rotation = new();
}
public class ShopManager : Singleton<ShopManager>
{
    [Header("Inventory (Serialized here)")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[4];  // 상단 고정 4종
    [SerializeField] private List<ItemData> rotationPool = new(); // 하단 로테이션 풀 (자동으로 Rotation 폴더에서 로드됨)
    [SerializeField] private int rotationCount = 4;                    // 하단 슬롯 개수

    // itemId → 구매 개수
    private Dictionary<string, int> purchaseHistory = new Dictionary<string, int>();
    public Dictionary<string, int> PurchaseHistory => purchaseHistory;

    // 매일 무료 리롤 가능 횟수 (기본 0)
    private int dailyRerollCount = 0;
    public int DailyRerollCount => dailyRerollCount;

    // 게임별 고유 시드 (게임마다 다르게 생성, 저장/불러오기 시 유지)
    private int gameUniqueSeed = -1;
    
    // 날짜별 시드 관리 (날짜 -> 시드 매핑)
    private Dictionary<int, int> shopSeeds = new Dictionary<int, int>();
    
    // 현재 날짜의 리롤 횟수 (리롤할 때마다 증가하여 다른 시드 사용)
    private int currentRerollCount = 0;

    // 확률 증가 이벤트 (Probability Event) - 모든 아이템 확률 동일하게
    // 이 플래그가 true면 가중치를 강제로 8로 설정
    public bool isProbabilityEqualized = false;

    // 신용카드: 상점 1회 방문(하루) 동안의 구매 슬롯 수 / 소모 골드. 다음 자유시간에 정산·초기화.
    public const int CreditCardRequiredPurchaseCount = 3;
    private int dailyPurchaseCount = 0;
    private int dailySpentGold = 0;
    public int DailyPurchaseCount => dailyPurchaseCount;
    public int DailySpentGold => dailySpentGold;

    /// <summary>
    /// 신용카드: 3슬롯 이상 구매한 날이면 소모 비용 * refundPercent 를 환급액으로 반환한다.
    /// 호출과 동시에 일일 구매 집계를 초기화하므로 자유시간 시작 시 1회만 호출할 것.
    /// </summary>
    public int ConsumeDailyPurchaseRefund(float refundPercent)
    {
        int refund = 0;
        if (refundPercent > 0f && dailyPurchaseCount >= CreditCardRequiredPurchaseCount)
            refund = Mathf.RoundToInt(dailySpentGold * refundPercent);

        dailyPurchaseCount = 0;
        dailySpentGold = 0;
        return refund;
    }


    /// <summary>
    /// 매일 상점 무료 리롤 가능 횟수를 추가합니다.
    /// </summary>
    public void AddDailyRerollCount(int count)
    {
        dailyRerollCount += count;
    }

    /// <summary>
    /// 리롤을 사용합니다. 리롤 횟수가 남아있으면 true를 반환합니다.
    /// </summary>
    public bool UseReroll()
    {
        if (dailyRerollCount > 0)
        {
            dailyRerollCount--;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 매일 초기화 시 호출 (GameManager에서 호출)
    /// 상점 연락처 구매 횟수만큼 무료 리롤 횟수를 추가합니다.
    /// </summary>
    public void ResetDailyRerollCount()
    {
        // 사용한 무료 리롤 횟수는 리셋 (매일 초기화)
        dailyRerollCount = 0;
        
        // 상점 연락처 구매 횟수 확인 및 무료 리롤 추가
        const string shopContactKey = "상점 연락처";
        if (purchaseHistory.ContainsKey(shopContactKey))
        {
            int purchaseCount = purchaseHistory[shopContactKey];
            if (purchaseCount > 0)
            {
                // 상점 연락처는 구매 1회당 매일 무료 리롤 1회 추가
                dailyRerollCount += purchaseCount;
                Debug.Log($"[ShopManager] 상점 연락처 구매 {purchaseCount}회: 매일 무료 리롤 {purchaseCount}회 추가");
            }
        }
    }

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
        ItemData[] items = Resources.LoadAll<ItemData>("Data/Item Data/Rotation");
        
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
    {
        var inv = new ShopInventory();

        // 상단: 고정
        foreach (var it in fixedItems)
        { 
            if (it)
            {
                inv.Fixed.Add(it);
                it.InitializePrice(ctx);
            }
        }

        // 하단: 로테이션 (ItemData가 해금/가중치 제공)
        var candidates = new List<ItemData>();
        foreach (var it in rotationPool) // 이제 rotationPool은 List<ItemData>
        {
            if (!it) continue;
            if (!UnlockManager.IsAvailable(it)) continue;        // 해금 시스템(해금 전엔 상점에 안 뜸)
            if (!it.IsMetaUnlocked()) continue;                  // 메타 해금(새벽 클리어·인게임 사건)
            if (!it.IsRotationUnlockOk(ctx)) continue;           // 스테이지 등 로테이션 해금 조건(각 아이템 override)
            if (it.GetRotationWeight(ctx) <= 0) continue;        // 가중치 0 이하는 제외
            if (!it.CanPurchaseByLimit()) continue;              // 구매 제한에 도달한 아이템 제외
            candidates.Add(it);
        }

        // 시드 기반 deterministic 선택
        int seed = GetShopSeed(currentDay);
        System.Random rng = new System.Random(seed);
        
        // 가중치 기반 중복 없이 N개 추첨 (시드 기반)
        inv.Rotation = Game.Util.WeightedRandom.PickWithoutReplacement(
            candidates,
            it => {
                int weight = Mathf.Max(0, it.GetRotationWeight(ctx));
                // 확률 증가 이벤트 활성화 시, 가중치가 0보다 큰 아이템은 모두 8로 고정
                if (isProbabilityEqualized && weight > 0) return 8;
                return weight;
            },
            rng,
            rotationCount
        );

        return inv;
    }

    /// <summary>
    /// 게임 고유 시드를 초기화합니다. 새 게임일 때만 호출됩니다.
    /// </summary>
    public void InitializeGameSeed()
    {
        if (gameUniqueSeed == -1)
        {
            // 새 게임 시작 시 랜덤 시드 생성
            gameUniqueSeed = UnityEngine.Random.Range(0, int.MaxValue);
            Debug.Log($"[ShopManager] 새 게임 고유 시드 생성: {gameUniqueSeed}");
        }
    }

    /// <summary>
    /// 현재 날짜의 상점 시드를 가져옵니다. 없으면 생성합니다.
    /// </summary>
    private int GetShopSeed(int day)
    {
        // 게임 고유 시드가 없으면 초기화
        if (gameUniqueSeed == -1)
        {
            InitializeGameSeed();
        }

        // 날짜와 리롤 횟수를 조합하여 고유한 키 생성
        int seedKey = day * 1000 + currentRerollCount;
        
        if (!shopSeeds.ContainsKey(seedKey))
        {
            // 새로운 시드 생성 (게임 고유 시드 + 날짜 기반으로 deterministic하게)
            int baseSeed = gameUniqueSeed + day * 12345 + currentRerollCount * 67890;
            shopSeeds[seedKey] = baseSeed;
        }
        
        return shopSeeds[seedKey];
    }

    /// <summary>
    /// 리롤 시 호출하여 리롤 횟수를 증가시킵니다.
    /// </summary>
    public void IncrementRerollCount()
    {
        currentRerollCount++;
    }

    /// <summary>
    /// 날짜가 변경될 때 호출하여 리롤 횟수를 리셋합니다.
    /// </summary>
    public void ResetRerollCount()
    {
        currentRerollCount = 0;
        // 날짜가 바뀌면 확률 증가 이벤트 초기화
        isProbabilityEqualized = false;
    }

    public bool TryPurchase(ShopContext ctx, ItemData data, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "아이템 없음";
            return false;
        }

        int price = data.GetDisplayPrice();

        // 골드 체크
        if (!ctx?.Economy?.HasGold(price) ?? true)
        {
            error = "골드 부족";
            return false;
        }

        // 결제
        ctx.Economy.SpendGold(price);
        GameEvents.RaiseShopBought(data);

        // 신용카드 정산용 일일 집계
        dailyPurchaseCount++;
        dailySpentGold += price;

        // 히스토리(종류별 개수 집계) : 이름(or asset name) 기준
        var key = string.IsNullOrEmpty(data.DisplayName) ? data.name : data.DisplayName;
        if (purchaseHistory.ContainsKey(key)) purchaseHistory[key]++;
        else purchaseHistory[key] = 1;

        CodexProgress.Discover(CodexProgress.Category.Item, data.UnlockId); // 도감: 아이템 발견

        // 효과 확정 반영
        data.Commit(ctx);

        return true;
    }

    public string ReturnMostPurchasedItem()
    {
        if (purchaseHistory.Count == 0) return null;

        var mostSold = purchaseHistory
        .Where(item => item.Key != "농장 확장" && item.Key != "교배 키트")
        .OrderByDescending(item => item.Value)
        .FirstOrDefault();

        return mostSold.Key;
    }

    public void LoadShopManager(SaveData saveData)
    {
        for(int i = 0; i < saveData.itemName.Count; i++)
        {
            var key = saveData.itemName[i];

            purchaseHistory[key] = saveData.itemPurchaseCount[i];
        }

        // 게임 고유 시드 로드
        gameUniqueSeed = saveData.gameUniqueShopSeed;
        if (gameUniqueSeed == -1)
        {
            // 저장 데이터에 시드가 없으면 새로 생성 (이전 버전 호환성)
            InitializeGameSeed();
        }
        else
        {
            Debug.Log($"[ShopManager] 게임 고유 시드 로드: {gameUniqueSeed}");
        }

        // 시드 로드
        shopSeeds.Clear();
        if (saveData.shopSeedDays != null && saveData.shopSeeds != null)
        {
            for (int i = 0; i < saveData.shopSeedDays.Count && i < saveData.shopSeeds.Count; i++)
            {
                shopSeeds[saveData.shopSeedDays[i]] = saveData.shopSeeds[i];
            }
        }
    }

    /// <summary>
    /// 특정 아이템의 구매 횟수를 반환합니다.
    /// </summary>
    /// <param name="data">구매 횟수를 확인할 아이템 데이터</param>
    /// <returns>구매 횟수, 아이템이 없거나 구매 이력이 없으면 0을 반환</returns>
    public int GetItemPurchaseCount(ItemData data)
    {
        if (data == null) return 0;

        // TryPurchase와 동일한 키 생성 방식 사용
        var key = string.IsNullOrEmpty(data.DisplayName) ? data.name : data.DisplayName;
        return purchaseHistory.ContainsKey(key) ? purchaseHistory[key] : 0;
    }

    /// <summary>
    /// 저장을 위해 시드 딕셔너리를 반환합니다.
    /// </summary>
    public Dictionary<int, int> GetShopSeeds()
    {
        return shopSeeds;
    }

    /// <summary>
    /// 게임 고유 시드를 반환합니다.
    /// </summary>
    public int GetGameUniqueSeed()
    {
        // 시드가 없으면 초기화
        if (gameUniqueSeed == -1)
        {
            InitializeGameSeed();
        }
        return gameUniqueSeed;
    }
}
