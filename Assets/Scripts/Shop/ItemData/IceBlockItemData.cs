// Assets/Scripts/Shop/Items/ItemData_Adrenaline.cs
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Item_IceBlock", menuName = "Shop/Item/IceBlock")]
public class ItemData_IceBlock : ItemData
{
    [Header("Effect")]

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 4;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "냉각 방패";
        if (Price <= 0) Price = 1500;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    { 
        if(ctx.Grid.HasIceBlock())
        {
            reason = "이미 냉각방패를 보유하고 있습니다";
            return false;
        }
        reason = null; 
        return true; 
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    { onReady?.Invoke(); }

    public override void Commit(ShopContext ctx)
    {
        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[Fertilizer] Grid not found.");
            return;
        }
        g.BuyIceBlock();
        ctx.ShowInfo?.Invoke($"{DisplayName} 발동: 벌레에게 피해를 입을 시, 해당 웨이브 효과에 무적");
    }

    public override void Cancel(ShopContext ctx) { }
}
