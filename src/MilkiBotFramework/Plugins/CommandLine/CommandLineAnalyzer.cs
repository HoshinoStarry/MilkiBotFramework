﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MilkiBotFramework.Plugins.CommandLine;

public class CommandLineAnalyzer : ICommandLineAnalyzer
{
    private static readonly HashSet<char> OptionFlags = new() { '-' };
    private static readonly HashSet<char> QuoteFlags = new() { '\"', '\'', '`' };
    private static readonly HashSet<char> SplitterFlags = new() { ' ', ':' };

    public bool TryAnalyze(string input,
        [NotNullWhen(true)] out CommandLineResult? result,
        [NotNullWhen(false)] out CommandLineException? exception)
    {
        var memory = input.AsMemory().Trim();
        int index = 0;
        int count = 0;

        ReadOnlyMemory<char>? command = null;
        char? currentQuote = null;

        var options = new Dictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>?>();
        var arguments = new List<ReadOnlyMemory<char>>();

        ReadOnlyMemory<char>? currentOption = null;

        foreach (var c in memory.Span)
        {
            if (currentQuote == null && SplitterFlags.Contains(c) ||
                c == currentQuote)
            {
                currentQuote = null;
                if (count > 0)
                {
                    var currentWord = memory.Slice(index, count);
                    try
                    {
                        AddOperation(currentWord);
                    }
                    catch (CommandLineException ex)
                    {
                        exception = ex;
                        result = null;
                        return false;
                    }
                }

                index += count + 1;
                count = 0;
            }
            else if (currentQuote == null && QuoteFlags.Contains(c))
            {
                currentQuote = c;
                index += count + 1;
                count = 0;
            }
            else
            {
                count++;
            }
        }

        if (count > 0)
        {
            var currentWord = memory.Slice(index, count);
            try
            {
                AddOperation(currentWord, true);
            }
            catch (CommandLineException ex)
            {
                exception = ex;
                result = null;
                return false;
            }
        }

        result = new CommandLineResult
        {
            Command = command,
            Arguments = arguments,
            Options = options
        };
        exception = null;
        return true;

        void AddOperation(ReadOnlyMemory<char> currentWord, bool isEnd = false)
        {
            if (OptionFlags.Contains(currentWord.Span[0])) // Option key
            {
                if (command == null)
                    throw new CommandLineException("Command should be declared before any options.");

                if (currentOption != null) // Previous is a switch
                    options.Add(currentOption.Value, null);

                if (isEnd)
                    options.Add(currentWord[1..], null);
                else
                    currentOption = currentWord[1..];
            }
            else if (command == null)
            {
                command = currentWord;
            }
            else if (currentOption != null) // Option value
            {
                options.Add(currentOption.Value, currentWord);
                currentOption = null;
            }
            else // Argument
            {
                arguments.Add(currentWord);
            }
        }
    }
}

public class CommandLineException : Exception
{
    public CommandLineException(string? message) : base(message)
    {

    }
}