using System;
using UnityEngine;

namespace LeTai.Common
{
[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public sealed class MaxAttribute : PropertyAttribute
{
    public readonly float max;

    public MaxAttribute(float max)
    {
        this.max = max;
    }
}
}