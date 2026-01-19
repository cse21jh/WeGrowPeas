// Assets/Scripts/Common/WeightedRandomUtil.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Util
{
    /// <summary>
    /// ����ġ ���� ��ƿ (UnityEngine.Random ���)
    /// - ����/NaN/Infinity�� 0���� ���
    /// - ��ü ���� 0�̸� TryPick*�� false�� ��ȯ (���� fallback ó��)
    /// - System.Random ������ ����(������ �ʿ� ��)
    /// </summary>
    public static class WeightedRandom
    {
        // ---------- Index ��� ----------

        public static bool TryPickIndex(IReadOnlyList<float> weights, out int index)
            => TryPickIndex(weights, out index, minClamp: 0f);

        public static bool TryPickIndex(IReadOnlyList<float> weights, out int index, float minClamp)
        {
            index = -1;
            if (weights == null || weights.Count == 0) return false;

            float total = 0f;
            var safe = new float[weights.Count];
            for (int i = 0; i < weights.Count; i++)
            {
                float w = weights[i];
                if (float.IsNaN(w) || float.IsInfinity(w) || w < minClamp) w = 0f;
                safe[i] = w;
                total += w;
            }

            if (total <= 0f) return false;

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            for (int i = 0; i < safe.Length; i++)
            {
                acc += safe[i];
                if (r <= acc) { index = i; return true; }
            }

            index = safe.Length - 1; // �ε��Ҽ��� ���� ��ȣ
            return true;
        }

        public static int PickIndex(IReadOnlyList<float> weights, float minClamp = 0f)
        {
            if (!TryPickIndex(weights, out int idx, minClamp))
                throw new ArgumentException("All weights are zero or invalid.");
            return idx;
        }

        // ---------- Item + selector ��� ----------

        public static bool TryPick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector, out T chosen, float minClamp = 0f)
        {
            chosen = default;
            if (items == null || items.Count == 0) return false;

            var w = new float[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                float ww = weightSelector(items[i]);
                if (float.IsNaN(ww) || float.IsInfinity(ww) || ww < minClamp) ww = 0f;
                w[i] = ww;
            }
            if (!TryPickIndex(w, out int idx, minClamp)) return false;
            chosen = items[idx];
            return true;
        }

        public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector, float minClamp = 0f)
        {
            if (!TryPick(items, weightSelector, out var chosen, minClamp))
                throw new ArgumentException("All weights are zero or invalid.");
            return chosen;
        }

        /// <summary>
        /// ����ġ ��� '�ߺ� ����' N�� ����.
        /// �� ���ܸ��� ���� �׸���� ���� ����ġ�� �ٽ� ����.
        /// </summary>
        public static List<T> PickWithoutReplacement<T>(IList<T> items, Func<T, float> weightSelector, int count, float minClamp = 0f)
        {
            count = Mathf.Clamp(count, 0, items?.Count ?? 0);
            var result = new List<T>(count);
            if (items == null || items.Count == 0 || count == 0) return result;

            var pool = new List<T>(items);
            for (int k = 0; k < count; k++)
            {
                if (!TryPick(pool, weightSelector, out var picked, minClamp)) break;
                result.Add(picked);
                pool.Remove(picked);
            }
            return result;
        }

        // ---------- Deterministic(System.Random) ���� ----------

        public static bool TryPickIndex(IReadOnlyList<float> weights, System.Random rng, out int index, float minClamp = 0f)
        {
            index = -1;
            if (weights == null || weights.Count == 0) return false;

            double total = 0.0;
            var safe = new double[weights.Count];
            for (int i = 0; i < weights.Count; i++)
            {
                double w = weights[i];
                if (double.IsNaN(w) || double.IsInfinity(w) || w < minClamp) w = 0.0;
                safe[i] = w;
                total += w;
            }

            if (total <= 0.0) return false;

            double r = rng.NextDouble() * total;
            double acc = 0.0;
            for (int i = 0; i < safe.Length; i++)
            {
                acc += safe[i];
                if (r <= acc) { index = i; return true; }
            }

            index = safe.Length - 1;
            return true;
        }

        /// <summary>
        /// System.Random을 사용하는 TryPick 오버로드
        /// </summary>
        public static bool TryPick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector, System.Random rng, out T chosen, float minClamp = 0f)
        {
            chosen = default;
            if (items == null || items.Count == 0) return false;

            var w = new float[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                float ww = weightSelector(items[i]);
                if (float.IsNaN(ww) || float.IsInfinity(ww) || ww < minClamp) ww = 0f;
                w[i] = ww;
            }
            if (!TryPickIndex(w, rng, out int idx, minClamp)) return false;
            chosen = items[idx];
            return true;
        }

        /// <summary>
        /// System.Random을 사용하는 PickWithoutReplacement 오버로드 (시드 기반)
        /// </summary>
        public static List<T> PickWithoutReplacement<T>(IList<T> items, Func<T, float> weightSelector, System.Random rng, int count, float minClamp = 0f)
        {
            count = Mathf.Clamp(count, 0, items?.Count ?? 0);
            var result = new List<T>(count);
            if (items == null || items.Count == 0 || count == 0) return result;

            var pool = new List<T>(items);
            for (int k = 0; k < count; k++)
            {
                if (!TryPick(pool, weightSelector, rng, out var picked, minClamp)) break;
                result.Add(picked);
                pool.Remove(picked);
            }
            return result;
        }
    }
}