using System;

namespace LeTai.Common
{
[AttributeUsage(AttributeTargets.Field)]
public sealed class FoldAttribute : Attribute
{
    public readonly string name;

    public FoldAttribute(string name)
    {
        this.name = name;
    }
}
}