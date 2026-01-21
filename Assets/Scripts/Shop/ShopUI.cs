using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;
using DG.Tweening;

public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform fixedItemsParent;     // 고정 아이템 슬롯 부모
    [SerializeField] private Transform rotationItemsParent;  // 로테이션 아이템 슬롯 부모
    [SerializeField] private ItemSlot itemSlotPrefab;
    [SerializeField] private TMP_Text footerText;       // 화면 하단 정보/에러 표기 텍스트
    [SerializeField] private RectTransform popupParent;  // 팝업 UI 부모
    [SerializeField] private Button rerollButton;        // 리롤 버튼
    [SerializeField] private TMP_Text rerollButtonText;  // 리롤 버튼 텍스트 (무료 횟수 표시용)

    // auto-resolved services
    private Grid grid;
    private EconomyManager economy;
    private PlacementController placement;

    // 생성된 슬롯들 (갱신 시 접근용)
    private readonly List<ItemSlot> slots = new();

    private ShopContext ctx;
    private ShopSession session;

    private ShopManager shopManager;
    private UIAnimationManager animationManager;

    private void Awake()
    {
        shopManager = ShopManager.Instance;
        animationManager = FindAnyObjectByType<UIAnimationManager>();
        session = new ShopSession();
        // Resolve services: prefer GameManager singletons, fallback to scene lookups
        if (GameManager.Instance != null)
        {
            grid = GameManager.Instance.grid;
            economy = GameManager.Instance.economyManager;
        }
        else
        {
            grid = FindAnyObjectByType<Grid>();
            economy = FindAnyObjectByType<EconomyManager>();
        }

        // PlacementController has no global singleton — try common locations first
        var placementObj = GameObject.Find("PlacementController");
        if (placementObj != null) placement = placementObj.GetComponent<PlacementController>();
        if (placement == null) placement = FindAnyObjectByType<PlacementController>();
        
        // PlacementController에 guideText 설정
        if (placement != null && footerText != null)
        {
            placement.SetGuideText(footerText);
        }
        
        ctx = new ShopContext
        {
            Grid = grid,
            Economy = economy,
            Session = session,
            Shop = shopManager,
            //ShowInfo = ShowInfo,
            ShowError = ShowError,
        };

        BuildShop();
        UpdateRerollButton();
    }

    private void OnDestroy()
    {
        // 객체 파괴 시 진행 중인 애니메이션 kill
        if (popupParent != null)
        {
            popupParent.DOKill();
        }
    }

    public void BuildShop()
    {
        // 초기화 확인
        if (shopManager == null || ctx == null)
        {
            InitializeIfNeeded();
            if (shopManager == null || ctx == null)
            {
                Debug.LogError("[ShopUI] BuildShop: shopManager or ctx is null!");
                return;
            }
        }

        // 고정 아이템과 로테이션 아이템을 각각 다른 부모에 배치
        if (fixedItemsParent != null)
            ClearChildren(fixedItemsParent);
        if (rotationItemsParent != null)
            ClearChildren(rotationItemsParent);
        slots.Clear();

        ShopInventory inv;
        // ShopManager에서 인벤토리 생성
        if (GameManager.Instance == null)
            inv = shopManager.GenerateInventory(ctx, 1);
        else
            inv = shopManager.GenerateInventory(ctx, GameManager.Instance.stage);

        // 고정 아이템을 fixedItemsParent에 생성
        for (int i = 0; i < inv.Fixed.Count; i++)
        {
            var data = inv.Fixed[i];
            if (data == null) continue;
            if (fixedItemsParent != null)
                MakeSlot(fixedItemsParent, data);
        }

        // 로테이션 아이템을 rotationItemsParent에 생성
        foreach (var data in inv.Rotation)
        {
            if (rotationItemsParent != null)
                MakeSlot(rotationItemsParent, data);
        }
    }

    private void MakeSlot(Transform parent, ItemData data)
    {
        var slot = Instantiate(itemSlotPrefab, parent);
        slot.gameObject.SetActive(true); // 프리팹이 비활성화되어 있을 수 있으므로 활성화
        slot.Bind(this, data);
        slots.Add(slot);
    }

    public void OnClickShowPopup(ItemData data, ItemSlot slot)
    {
        if (popupParent == null) return;
        
        // 진행 중인 애니메이션 kill
        popupParent.DOKill();
        
        popupParent.gameObject.SetActive(true);
        popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        
        var popupController = popupParent.GetComponent<ShopPopupController>();
        if (popupController != null)
        {
            popupController.SetItemInfo(data, this, slot);
        }
    }

    public void OnClickHidePopup()
    {
        if (popupParent == null) return;
        
        // 진행 중인 애니메이션 kill
        popupParent.DOKill();
        
        popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (popupParent != null)
            {
                popupParent.gameObject.SetActive(false);
            }
        });
    }

    public void OnClickBuy(ItemData data, ItemSlot slot)
    {
        // 상점 세션 1회 제한 처리
        if (data.OnePerShopIfNotStackable && !data.IsStackable && session.WasBought(data))
        {
            ShowError("구매 불가");
            return;
        }

        // 구매 가능 체크 (중복 상태/해금 등)
        if (!data.CanPurchase(ctx, out string why))
        {
            ShowError(why ?? "구매 불가");
            return;
        }

        if (data.GetDisplayPrice() > (economy != null ? economy.GetGold() : 0))
        {
            ShowError("구매 불가");
            return;
        }

        // 효과 시작 → 플로우 분기
        data.StartEffect(ctx, onReady: () =>
        {
            SoundManager.Instance.PlayEffect("Button");
            switch (data.FlowType)
            {
                case ShopFlowType.Instant:
                    TryChargeAndCommit(data, slot);
                    break;

                case ShopFlowType.PlaceOnTile:
                    placement?.BeginTilePlacement(
                        ctx,
                        validate: (pos) => data.ValidatePosition(ctx, pos, out _),
                        onConfirm: (pos) =>
                        {
                            data.SetPlacedPosition(pos);
                            TryChargeAndCommit(data, slot);
                        },
                        onCancel: () => data.Cancel(ctx)
                    );
                    break;

                case ShopFlowType.SelectExistingPlant:
                    if (placement == null)
                    {
                        Debug.LogError("[ShopUI] PlacementController is null!");
                        ShowError("배치 컨트롤러를 찾을 수 없습니다");
                        return;
                    }
                    try
                    {
                        placement.BeginPlantSelection(
                            ctx,
                            validate: (plant) => 
                            {
                                try
                                {
                                    return data.ValidateTarget(ctx, plant, out _);
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"[ShopUI] ValidateTarget error: {e.Message}");
                                    return false;
                                }
                            },
                            onConfirm: (plant) =>
                            {
                                try
                                {
                                    data.SetSelectedPlant(plant);
                                    TryChargeAndCommit(data, slot);
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"[ShopUI] onConfirm error: {e.Message}\n{e.StackTrace}");
                                    ShowError($"구매 처리 중 오류가 발생했습니다: {e.Message}");
                                }
                            },
                            onCancel: () => 
                            {
                                try
                                {
                                    data.Cancel(ctx);
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"[ShopUI] onCancel error: {e.Message}");
                                }
                            }
                        );
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[ShopUI] BeginPlantSelection error: {e.Message}\n{e.StackTrace}");
                        ShowError($"식물 선택 모드 시작 중 오류가 발생했습니다: {e.Message}");
                    }
                    break;
            }
        },
        onError: (err) => ShowError(err ?? "구매 불가"));
    }

    private void TryChargeAndCommit(ItemData data, ItemSlot slot)
    {

        if (shopManager.TryPurchase(ctx, data, out var err))
        {
            if (!data.IsStackable) session.MarkBought(data);
            slot.OnPurchased();
            
            // 전체 구매 제한에 도달했는지 확인
            bool reachedTotalLimit = !data.CanPurchaseByLimit();
            
            // 일일 구매 제한에 도달했는지 확인 (OnePerShopIfNotStackable && !IsStackable)
            bool reachedDailyLimit = data.OnePerShopIfNotStackable && !data.IsStackable && session.WasBought(data);
            
            // 전체 제한 또는 일일 제한에 도달했으면 ItemSlot 제거
            if (reachedTotalLimit || reachedDailyLimit)
            {
                RemoveItemSlot(slot);
            }
            //ShowInfo($"{data.DisplayName} 구매 완료");
        }
    }

    /// <summary>
    /// ItemSlot을 제거합니다 (구매 제한에 도달한 경우).
    /// </summary>
    private void RemoveItemSlot(ItemSlot slot)
    {
        if (slot == null) return;

        // slots 리스트에서 제거
        slots.Remove(slot);

        // GameObject 제거
        if (slot.gameObject != null)
        {
            Destroy(slot.gameObject);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    /*
    public void ClearInfo()
    {
        if (footerText == null) return;
        footerText.text = "";
    }

    private void ShowInfo(string msg)
    {
        if (footerText == null) return;
        footerText.color = Color.white;
        footerText.text = msg;
    }
    */

    private void ShowError(string msg)
    {
        PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
        {
            title = "Error",
            message = msg,
            duration = 3.5f
        });
    }

    /// <summary>
    /// 리롤 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    public void OnClickReroll()
    {
        PhoneManager.Instance.PhoneTouchEffect();
        const int rerollPrice = 500; // 리롤 가격

        // 무료 리롤 횟수가 있으면 무료로 리롤
        if (shopManager.UseReroll())
        {
            SoundManager.Instance.PlayEffect("Button");
            shopManager.IncrementRerollCount(); // 리롤 횟수 증가 (다른 시드 사용)
            session.ClearThisShop(); // 세션 초기화 (구매 이력 리셋)
            BuildShop();
            UpdateRerollButton();
            return;
        }

        // 무료 횟수가 없으면 골드 지불
        if (economy != null && economy.HasGold(rerollPrice))
        {
            economy.SpendGold(rerollPrice);
            SoundManager.Instance.PlayEffect("Button");
            shopManager.IncrementRerollCount(); // 리롤 횟수 증가 (다른 시드 사용)
            session.ClearThisShop(); // 세션 초기화 (구매 이력 리셋)
            BuildShop();
            UpdateRerollButton();
        }
        else
        {
            ShowError("골드가 부족합니다.");
        }
    }

    /// <summary>
    /// 리롤 버튼 UI를 업데이트합니다 (무료 횟수 표시).
    /// </summary>
    private void UpdateRerollButton()
    {
        if (rerollButton == null) return;

        // shopManager 초기화 확인
        if (shopManager == null)
        {
            shopManager = ShopManager.Instance;
            if (shopManager == null)
            {
                Debug.LogError("[ShopUI] UpdateRerollButton: ShopManager.Instance is null!");
                return;
            }
        }

        int freeRerollCount = shopManager.DailyRerollCount;
        
        if (rerollButtonText != null)
        {
            if (freeRerollCount > 0)
            {
                rerollButtonText.text = $"리롤 (무료 {freeRerollCount}회)";
            }
            else
            {
                rerollButtonText.text = "리롤 (500골드)";
            }
        }

        // 리롤 버튼 리스너 설정
        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnClickReroll);
    }

    /// <summary>
    /// 매일 자동으로 상점을 리롤합니다 (GameManager에서 호출).
    /// </summary>
    public void DailyReroll()
    {
        // 초기화되지 않았다면 초기화 (Awake 전에 호출될 수 있음)
        InitializeIfNeeded();
        
        // shopManager가 여전히 null이면 에러
        if (shopManager == null)
        {
            Debug.LogError("[ShopUI] DailyReroll: ShopManager.Instance is null!");
            return;
        }

        // session이 null이면 초기화
        if (session == null)
        {
            session = new ShopSession();
        }

        shopManager.ResetRerollCount(); // 날짜 변경 시 리롤 횟수 리셋
        session.ClearThisShop(); // 세션 초기화 (구매 이력 리셋)
        BuildShop();
        UpdateRerollButton();
    }

    /// <summary>
    /// 필요한 필드가 초기화되지 않았다면 초기화합니다.
    /// </summary>
    private void InitializeIfNeeded()
    {
        // shopManager 초기화
        if (shopManager == null)
        {
            shopManager = ShopManager.Instance;
        }

        // session 초기화
        if (session == null)
        {
            session = new ShopSession();
        }

        // ctx 초기화
        if (ctx == null)
        {
            // services 초기화
            if (grid == null)
            {
                if (GameManager.Instance != null)
                {
                    grid = GameManager.Instance.grid;
                }
                else
                {
                    grid = FindAnyObjectByType<Grid>();
                }
            }

            if (economy == null)
            {
                if (GameManager.Instance != null)
                {
                    economy = GameManager.Instance.economyManager;
                }
                else
                {
                    economy = FindAnyObjectByType<EconomyManager>();
                }
            }

            ctx = new ShopContext
            {
                Grid = grid,
                Economy = economy,
                Session = session,
                Shop = shopManager,
                ShowError = ShowError,
            };
        }
    }

    private class ShopSession
    {
        private HashSet<ItemData> once = new();
        public bool WasBought(ItemData e) => once.Contains(e);
        public void MarkBought(ItemData e) => once.Add(e);
        public void ClearThisShop() => once.Clear();
    }
}
