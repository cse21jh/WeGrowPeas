using UnityEngine;

public enum GoldFeedbackReason
{
    None,
    PlantSale,
    ShopPurchase,
    ShopReroll,
    Tax,
    Reward,
    Other
}

public enum PlantValueChangeReason
{
    None,
    WaveSurvived,
    FreeTimePassed,
    SprinklerBonus,
    Rooted,
    UpgradeBonus,
    Moved,
    Other
}

public readonly struct GoldFeedbackData
{
    public int Delta { get; }
    public int BalanceAfter { get; }
    public GoldFeedbackReason Reason { get; }
    public bool HasWorldOrigin { get; }
    public Vector3 WorldOrigin { get; }

    public GoldFeedbackData(int delta, int balanceAfter, GoldFeedbackReason reason)
    {
        Delta = delta;
        BalanceAfter = balanceAfter;
        Reason = reason;
        HasWorldOrigin = false;
        WorldOrigin = default;
    }

    public GoldFeedbackData(int delta, int balanceAfter, GoldFeedbackReason reason, Vector3 worldOrigin)
    {
        Delta = delta;
        BalanceAfter = balanceAfter;
        Reason = reason;
        HasWorldOrigin = true;
        WorldOrigin = worldOrigin;
    }

    public static GoldFeedbackData HudOnly(int delta, int balanceAfter, GoldFeedbackReason reason)
        => new GoldFeedbackData(delta, balanceAfter, reason);

    public static GoldFeedbackData AtWorldOrigin(int delta, int balanceAfter, GoldFeedbackReason reason, Vector3 worldOrigin)
        => new GoldFeedbackData(delta, balanceAfter, reason, worldOrigin);
}

public readonly struct PlantValueFeedbackData
{
    public int PlantInstanceId { get; }
    public int PreviousValue { get; }
    public int CurrentValue { get; }
    public int Delta => CurrentValue - PreviousValue;
    public PlantValueChangeReason Reason { get; }
    public Vector3 WorldPosition { get; }

    public PlantValueFeedbackData(
        int plantInstanceId,
        int previousValue,
        int currentValue,
        PlantValueChangeReason reason,
        Vector3 worldPosition)
    {
        PlantInstanceId = plantInstanceId;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        Reason = reason;
        WorldPosition = worldPosition;
    }
}

public readonly struct BreedCountFeedbackData
{
    public int IncrementAmount { get; }
    public int PreviousRemainingCount { get; }
    public int CurrentRemainingCount { get; }
    public bool CounterWasActive { get; }

    public BreedCountFeedbackData(
        int incrementAmount,
        int previousRemainingCount,
        int currentRemainingCount,
        bool counterWasActive)
    {
        IncrementAmount = incrementAmount;
        PreviousRemainingCount = previousRemainingCount;
        CurrentRemainingCount = currentRemainingCount;
        CounterWasActive = counterWasActive;
    }
}
