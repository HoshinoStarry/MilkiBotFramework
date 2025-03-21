﻿namespace MilkiBotFramework.Plugining.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginIdentifierAttribute(string guid, string? name = null) : Attribute
{
    public PluginIdentifierAttribute(string? name = null) : this(System.Guid.NewGuid().ToString(), name)
    {
    }

    public string? Scope { get; init; }
    public string Guid { get; } = guid;
    public string? Name { get; } = name;
    public string? Authors { get; init; }

    /// <summary>
    /// 插件优先级，越小则优先级越高
    /// </summary>
    public int Index { get; init; }

    public bool AllowDisable { get; init; } = true;
}