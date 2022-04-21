using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Logging;

namespace SymbolPicker;

public sealed class SortedSymbols
{
    public class Category
    {
        public string Label { init; get; } = "";
        public SymbolChar[]? Chars { get; init; } = null;
    }

    public List<Category> Symbols { get; } = new();

    public const char Min = (char)0xE020;
    public const char Max = (char)0xE0DB;

    private SymbolChar[] CharsToSymbolChars(char[] chars)
    {
        var list = new List<SymbolChar>();
        foreach (var c in chars)
        {
            var search = ((SeIconChar)c).ToString();
            var fullName = ((SeIconChar)c).ToString();
            if (NamedCharacters.ContainsKey(c)) fullName = NamedCharacters[c];

            var shortName = fullName;
            var parenthesis = fullName.IndexOf('(');
            if (parenthesis > 0) shortName = shortName[..(parenthesis - 1)]; // remove space before (

            list.Add(new SymbolChar()
            {
                Char = c,
                Name = shortName,
                Search = Regex.Replace(fullName.Replace("(", "").Replace(")", ""),
                    "(\\B[A-Z])", " $1").ToLowerInvariant().Split(" ").Append(search).Distinct().ToArray()
            });
        }

        return list.ToArray();
    }

    private static bool IsEmpty(int i)
    {
        switch (i)
        {
            case >= 0xE02C and <= 0xE02F:
            case 0xE030:
            case 0xE036:
            case 0xE037:
            case >= 0xE045 and <= 0xE047:
            case >= 0xE08B and <= 0xE08E:
            case >= 0xE0C7 and <= 0xE0CF:
                return true;
            default:
                return false;
        }
    }

    private readonly List<char>? _allCategorized;

    private IEnumerable<char> FromTo(char from, char to)
    {
        var chars = new List<char>();
        for (var i = from; i <= to; i++)
        {
            chars.Add(i);
        }

        _allCategorized?.AddRange(chars);
        return chars;
    }

    private IEnumerable<char> FromTo(SeIconChar from, SeIconChar to)
    {
        return FromTo((char)from, (char)to);
    }

    public readonly Dictionary<char, string> NamedCharacters;

    public Dictionary<char, SymbolChar> AllSymbols = new();

    public SortedSymbols()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SymbolPicker.characters.json");
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            NamedCharacters = JsonSerializer.Deserialize<Dictionary<char, string>>(reader.ReadToEnd()) ??
                              throw new InvalidOperationException();
        }

        if (NamedCharacters == null) throw new Exception("Cannot find characters");

        PluginLog.Log("" + Assembly.GetExecutingAssembly().GetManifestResourceNames());

        _allCategorized = new List<char>();
        var chars = FromTo(SeIconChar.BoxedLetterA, SeIconChar.BoxedLetterZ)
            .Concat(FromTo(SeIconChar.BoxedNumber0, SeIconChar.BoxedNumber31))
            .Append((char)SeIconChar.BoxedQuestionMark)
            .Append((char)SeIconChar.BoxedPlus);
        Symbols.Add(new Category()
        {
            Label = "Boxed letters and numbers",
            Chars = CharsToSymbolChars(chars.ToArray())
        });
        chars = FromTo(SeIconChar.Number0, SeIconChar.Number9)
            .Concat(FromTo(SeIconChar.LevelEn, SeIconChar.LevelFr))
            .Concat(FromTo(SeIconChar.Instance1, SeIconChar.InstanceMerged))
            .Concat(FromTo(SeIconChar.BoxedStar, SeIconChar.BoxedRoman6));
        Symbols.Add(new Category()
        {
            Label = "Numbers",
            Chars = CharsToSymbolChars(chars.ToArray())
        });
        chars = FromTo(SeIconChar.Circle, SeIconChar.GlamouredDyed)
            .Concat(FromTo((char)SeIconChar.Hexagon, (char)0xE044))
            .Append((char)SeIconChar.HighQuality)
            .Append((char)SeIconChar.Clock);
        _allCategorized.AddRange(chars);
        Symbols.Add(new Category()
        {
            Label = "Commonly used shapes",
            Chars = CharsToSymbolChars(chars.ToArray())
        });

        // AllCategorized.AddRange(chars);
        Symbols.Add(new Category()
        {
            Label = "Standard symobls",
            Chars = CharsToSymbolChars(NamedCharacters.Keys.ToArray())
        });

        var allUncategorized = new List<char>();
        for (var i = Min; i < Max; i++)
        {
            if (IsEmpty(i) || _allCategorized.Contains(i)) continue;
            allUncategorized.Add(i);
        }

        Symbols.Add(new Category()
        {
            Label = "Uncategorized game symbols",
            Chars = CharsToSymbolChars(allUncategorized.ToArray())
        });

        foreach (var category in Symbols)
        {
            if (category.Chars == null) continue;
            foreach (var categoryChar in category.Chars)
            {
                AllSymbols.TryAdd(categoryChar.Char, categoryChar);
            }
        }

        _allCategorized = null;
    }

    public SymbolChar[] Search(string input, int limit = 100)
    {
        if (input.Trim().Length == 0) return Array.Empty<SymbolChar>();

        var allResults = new Dictionary<SymbolChar, int>();
        foreach (var s in Regex.Replace(input, "(\\B[A-Z])", " $1").ToLowerInvariant().Split(" "))
        {
            var results = new Dictionary<SymbolChar, int>();
            if (s.Trim() == "") continue;
            foreach (var keyValuePair in AllSymbols)
            {
                var w = 0;
                foreach (var s1 in keyValuePair.Value.Search)
                {
                    if (s1.Contains(s)) w++;
                    if (s1 == (s)) w += 10;
                }

                if (w > 0)
                {
                    if (results.ContainsKey(keyValuePair.Value))
                    {
                        w += results[keyValuePair.Value];
                        results.Remove(keyValuePair.Value);
                    }

                    results.Add(keyValuePair.Value, w);
                }
            }

            if (allResults.Count > 0)
            {
                foreach (var keyValuePair in results)
                {
                    if (allResults.ContainsKey(keyValuePair.Key))
                    {
                        var w = keyValuePair.Value + allResults[keyValuePair.Key];
                        allResults.Remove(keyValuePair.Key);
                        allResults.Add(keyValuePair.Key, w);
                    }
                }
            }
            else allResults = results;
        }

        return (from entry in allResults orderby entry.Value descending select entry.Key).Take(limit).ToArray();
    }
}