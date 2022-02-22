﻿using System;

namespace MilkiBotFramework.Plugins.CommandLine.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OptionAttribute : ParameterAttribute
{
    public OptionAttribute(string name)
    {
        Name = name;
    }

    public char Abbreviate { get; set; }
    public string Name { get; set; }
    public bool IsSwitch { get; set; }
}