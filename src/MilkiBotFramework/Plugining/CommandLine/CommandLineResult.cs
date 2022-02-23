﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MilkiBotFramework.Plugining.CommandLine;

public sealed class CommandLineResult
{
    public CommandLineAuthority Authority { get; init; }
    public ReadOnlyMemory<char>? Command { get; init; }
    public Dictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>?>? Options { get; init; }
    public List<ReadOnlyMemory<char>>? Arguments { get; init; }
    public ReadOnlyMemory<char>? SimpleArgument { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Command != null)
        {
            sb.Append(GetArgumentString(Command) + " ");
        }

        if (Arguments is { Count: > 0 })
        {
            sb.Append(string.Join(" ", Arguments.Select(k => GetArgumentString(k))));
            sb.Append(' ');
        }

        if (Options is { Count: > 0 })
            sb.Append(string.Join(" ", Options
                .OrderBy(k => k.Key.ToString())
                .Select(k =>
                {
                    return k.Value == null
                        ? $"-{GetArgumentString(k.Key)}"
                        : $"-{GetArgumentString(k.Key)} {GetArgumentString(k.Value)}";
                })
            ));

        if (sb.Length == 0)
            return "";

        if (sb[^1] == ' ')
            sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }

    private static string GetArgumentString(ReadOnlyMemory<char>? k)
    {
        if (k == null) return "";
        return (k.Value.Span.Contains(' ') || k.Value.Span.Contains(':')) ? $"\"{k}\"" : k.Value.ToString();
    }
}