using System.Collections.Generic;
using UnityEngine;

public static class PriceTracker
{
    private static readonly Dictionary<string, int> _buyCounts = new();

    public static int GetCount(string key)
        => _buyCounts.TryGetValue(key, out var c) ? c : 0;

    public static void Inc(string key)
        => _buyCounts[key] = GetCount(key) + 1;

    public static int GetPrice(string key, int basePrice, float factor)
    {
        int n = GetCount(key);
        double p = basePrice * System.Math.Pow(factor, n);
        return Mathf.RoundToInt((float)p);
    }

    // 필요 시 런 시작 때 초기화
    public static void ResetAll() => _buyCounts.Clear();
    public static void Reset(string key) { if (_buyCounts.ContainsKey(key)) _buyCounts.Remove(key); }
}