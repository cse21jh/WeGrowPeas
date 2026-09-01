using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점의 뷰 비의존 로직(인벤토리 생성/구매 플로우/리롤). MonoBehaviour가 아니므로 어떤 UI에서도 사용 가능.
/// 기존 ShopUI와 신규 ShopCanvasController(Renewal)가 같은 로직을 공유하기 위해 분리했다.
///
/// 사용:
///   var shop = new ShopController(showError: msg => ...);
///   var inv = shop.GetInventory();
///   shop.Buy(itemData, onPurchased: () => 슬롯갱신());
/// </summary>
public class ShopController
{
    private ShopManager shopManager;
    private Grid grid;
    private EconomyManager economy;
    private PlacementController placement;

    private ShopContext ctx;
    private readonly ShopSession session = new ShopSession();

    private readonly Action<string> showError;

    public const int RerollPrice = 500;

    public ShopController(Action<string> showError = null)
    {
        this.showError = showError ?? DefaultShowError;
        InitializeIfNeeded();
    }

    public ShopContext Context { get { InitializeIfNeeded(); return ctx; } }

    /// <summary>이번 상점(세션)에서 이미 구매한 아이템인가. (비스택 1회 제한 표시용)</summary>
    public bool WasBoughtThisShop(ItemData data) => data != null && session.WasBought(data);

    // ── 인벤토리 ──────────────────────────────────────────────────────────────
    // 이번 상점 방문의 목록을 캐시한다. 탭 전환/구매 갱신마다 GenerateInventory를 다시 부르면
    // 아이템의 InitializePrice가 매번 실행돼(SO의 Price를 직접 수정하는 아이템 존재) 값이 흔들리므로,
    // 리롤/일일 리롤 때만 재생성한다.
    private ShopInventory cachedInventory;
    private int cachedStage = -1;

    /// <summary>현재 스테이지 기준 상점 목록(고정/로테이션). 리롤 전까지 동일한 목록을 반환.</summary>
    public ShopInventory GetInventory()
    {
        InitializeIfNeeded();
        if (shopManager == null) return new ShopInventory();

        int stage = GameManager.Instance != null ? GameManager.Instance.stage : 1;

        // 스테이지가 바뀌었는데 DailyReroll이 호출되지 않은 경우도 재생성
        if (cachedInventory == null || cachedStage != stage)
        {
            cachedInventory = shopManager.GenerateInventory(ctx, stage);
            cachedStage = stage;
        }
        return cachedInventory;
    }

    /// <summary>캐시를 버려 다음 <see cref="GetInventory"/>에서 목록을 새로 생성하게 한다.</summary>
    public void InvalidateInventory()
    {
        cachedInventory = null;
        cachedStage = -1;
    }

    /// <summary>더 이상 구매할 수 없게 된 아이템을 현재 목록에서 제거한다.</summary>
    private void RemoveFromInventory(ItemData data)
    {
        if (cachedInventory == null || data == null) return;
        cachedInventory.Fixed.Remove(data);
        cachedInventory.Rotation.Remove(data);
    }

    // ── 구매 ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// 아이템 구매. 세션 제한 → 구매 조건 → 골드 → 효과 시작(플로우 분기) → 결제/커밋 순으로 처리한다.
    /// 성공 시 <paramref name="onPurchased"/> 호출(UI 갱신용). 배치/선택형은 완료 시점에 호출된다.
    /// </summary>
    public void Buy(ItemData data, Action onPurchased = null)
    {
        InitializeIfNeeded();
        if (data == null) return;

        // 상점 세션 1회 제한
        if (data.OnePerShopIfNotStackable && !data.IsStackable && session.WasBought(data))
        {
            showError("구매 불가");
            return;
        }

        // 구매 가능 체크(중복 상태/해금 등)
        if (!data.CanPurchase(ctx, out string why))
        {
            showError(why ?? "구매 불가");
            return;
        }

        if (data.GetDisplayPrice() > (economy != null ? economy.GetGold() : 0))
        {
            showError("구매 불가");
            return;
        }

        data.StartEffect(ctx,
            onReady: () =>
            {
                SoundManager.Instance?.PlayEffect("Button");
                switch (data.FlowType)
                {
                    case ShopFlowType.Instant:
                        TryChargeAndCommit(data, onPurchased);
                        break;

                    case ShopFlowType.PlaceOnTile:
                        UIManager.Instance?.Popup?.ShowGuide(data.GetPlacementGuide());
                        placement?.BeginTilePlacement(
                            ctx,
                            validate: pos => data.ValidatePosition(ctx, pos, out _),
                            onConfirm: pos =>
                            {
                                UIManager.Instance?.Popup?.HideGuide();
                                data.SetPlacedPosition(pos);
                                TryChargeAndCommit(data, onPurchased);
                            },
                            onCancel: () =>
                            {
                                UIManager.Instance?.Popup?.HideGuide();
                                data.Cancel(ctx);
                            });
                        break;

                    case ShopFlowType.SelectExistingPlant:
                        if (placement == null)
                        {
                            Debug.LogError("[ShopController] PlacementController is null!");
                            showError("배치 컨트롤러를 찾을 수 없습니다");
                            return;
                        }
                        try
                        {
                            UIManager.Instance?.Popup?.ShowGuide(data.GetPlacementGuide());
                            placement.BeginPlantSelection(
                                ctx,
                                validate: plant =>
                                {
                                    try { return data.ValidateTarget(ctx, plant, out _); }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"[ShopController] ValidateTarget error: {e.Message}");
                                        return false;
                                    }
                                },
                                onConfirm: plant =>
                                {
                                    UIManager.Instance?.Popup?.HideGuide();
                                    try
                                    {
                                        data.SetSelectedPlant(plant);
                                        TryChargeAndCommit(data, onPurchased);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"[ShopController] onConfirm error: {e.Message}\n{e.StackTrace}");
                                        showError($"구매 처리 중 오류가 발생했습니다: {e.Message}");
                                    }
                                },
                                onCancel: () =>
                                {
                                    UIManager.Instance?.Popup?.HideGuide();
                                    try { data.Cancel(ctx); }
                                    catch (Exception e) { Debug.LogError($"[ShopController] onCancel error: {e.Message}"); }
                                });
                        }
                        catch (Exception e)
                        {
                            UIManager.Instance?.Popup?.HideGuide();
                            Debug.LogError($"[ShopController] BeginPlantSelection error: {e.Message}\n{e.StackTrace}");
                            showError($"식물 선택 모드 시작 중 오류가 발생했습니다: {e.Message}");
                        }
                        break;
                }
            },
            onError: err => showError(err ?? "구매 불가"));
    }

    private void TryChargeAndCommit(ItemData data, Action onPurchased)
    {
        if (shopManager.TryPurchase(ctx, data, out _))
        {
            if (!data.IsStackable) session.MarkBought(data);

            // 전체 구매 제한 도달 or 이번 상점 1회 제한 소진 → 목록에서 제거
            bool reachedTotalLimit = !data.CanPurchaseByLimit();
            bool reachedShopLimit = data.OnePerShopIfNotStackable && !data.IsStackable && session.WasBought(data);
            if (reachedTotalLimit || reachedShopLimit) RemoveFromInventory(data);

            onPurchased?.Invoke();
        }
    }

    // ── 리롤 ──────────────────────────────────────────────────────────────────
    /// <summary>리롤 버튼 라벨(무료 횟수 or 가격).</summary>
    public string GetRerollLabel()
    {
        InitializeIfNeeded();
        int free = shopManager != null ? shopManager.DailyRerollCount : 0;
        return free > 0 ? $"무료 {free}회" : $"{RerollPrice} G";
    }

    /// <summary>사용자가 리롤 요청. 성공 시 true(호출 측에서 목록 다시 그림).</summary>
    public bool TryReroll()
    {
        InitializeIfNeeded();
        if (shopManager == null) return false;

        // 무료 횟수 우선 사용
        if (shopManager.UseReroll())
        {
            SoundManager.Instance?.PlayEffect("Button");
            shopManager.IncrementRerollCount();
            session.ClearThisShop();
            InvalidateInventory();
            return true;
        }

        if (economy != null && economy.HasGold(RerollPrice))
        {
            economy.SpendGold(RerollPrice, GoldFeedbackReason.ShopReroll);
            SoundManager.Instance?.PlayEffect("Button");
            shopManager.IncrementRerollCount();
            session.ClearThisShop();
            InvalidateInventory();
            return true;
        }

        showError("골드가 부족합니다.");
        return false;
    }

    /// <summary>매일 자동 리롤(스테이지 전환 시). 호출 후 목록을 다시 그릴 것.</summary>
    public void DailyReroll()
    {
        InitializeIfNeeded();
        if (shopManager == null)
        {
            Debug.LogError("[ShopController] DailyReroll: ShopManager.Instance is null!");
            return;
        }

        shopManager.ResetRerollCount();
        shopManager.ResetDailyRerollCount();
        session.ClearThisShop();
        InvalidateInventory();
    }

    // ── 초기화 ────────────────────────────────────────────────────────────────
    private void InitializeIfNeeded()
    {
        if (shopManager == null) shopManager = ShopManager.Instance;

        if (grid == null)
            grid = GameManager.Instance != null ? GameManager.Instance.grid : UnityEngine.Object.FindAnyObjectByType<Grid>();

        if (economy == null)
            economy = GameManager.Instance != null ? GameManager.Instance.economyManager : UnityEngine.Object.FindAnyObjectByType<EconomyManager>();

        if (placement == null)
        {
            var placementObj = GameObject.Find("PlacementController");
            if (placementObj != null) placement = placementObj.GetComponent<PlacementController>();
            if (placement == null) placement = UnityEngine.Object.FindAnyObjectByType<PlacementController>();
        }

        if (ctx == null)
        {
            ctx = new ShopContext
            {
                Grid = grid,
                Economy = economy,
                Session = session,
                Shop = shopManager,
                ShowError = showError,
            };
        }
        else
        {
            // 씬 로드 순서에 따라 뒤늦게 잡히는 참조 갱신
            ctx.Grid = grid;
            ctx.Economy = economy;
            ctx.Shop = shopManager;
        }
    }

    private static void DefaultShowError(string msg)
    {
        PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
        {
            title = "Error",
            message = msg,
            duration = 3.5f
        });
    }

    /// <summary>상점 1회 방문 동안의 구매 이력(비스택 1회 제한용).</summary>
    private class ShopSession
    {
        private readonly HashSet<ItemData> once = new();
        public bool WasBought(ItemData e) => once.Contains(e);
        public void MarkBought(ItemData e) => once.Add(e);
        public void ClearThisShop() => once.Clear();
    }
}
