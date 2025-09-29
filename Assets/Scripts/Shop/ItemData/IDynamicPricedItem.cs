public interface IDynamicPricedItem
{
    int GetCurrentPrice();
    void OnPurchased(); // 구매 직후 가격카운트 증가 등
}