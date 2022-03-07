namespace SymbolPicker;

public sealed class SymbolChar
{
    public char Char { get; init; }
    public string? String { get; init; }
    public string Name { get; init; } = null!;
    public string[] Search { get; init; } = null!;
    public bool IsSe => Char is >= SortedSymbols.Min and <= SortedSymbols.Max;
}