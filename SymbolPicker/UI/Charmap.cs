using System;
using System.Numerics;
using ImGuiNET;

namespace SymbolPicker.UI;

public class Charmap
{
    private bool _charMapVisible;
    private bool pressing = false;
    private bool _listenForHotkey = true;
    private SortedSymbols _symbols;
    private readonly Configuration _configuration;
    private bool _pickerVisible = false;

    private bool _pickerWasDisplayed = false;
    private string _currentSearch = "";
    private string _lastConvertSource = "";
    private string _lastConvertResult = "";
    private int _currentSelected = 0;
    private SymbolChar[] _searchResults = Array.Empty<SymbolChar>();

    public Charmap(SortedSymbols symbols, Configuration configuration)
    {
        _symbols = symbols;
        _configuration = configuration;
    }

    public void Open()
    {
        _charMapVisible = true;
    }

    public void Close()
    {
        _charMapVisible = false;
    }

    public void Draw()
    {
        if (!_charMapVisible) return;
        if (ImGui.Begin("charmap.exe###SymbolPicker-main", ref _charMapVisible))
        {
            ImGui.TextWrapped("For now, clicking a symbol will copy it to your clipboard. " +
                              "The window will then unfocus, allowing you to paste it in an in-game text box using " +
                              "control-v.");

            ImGui.TextWrapped(
                $"Use the quick picker by pressing {_configuration.PickerModifier} + {_configuration.PickerKey}\n" +
                "It will pop a text field on your cursor that will let you search for a symbol. Click one or " +
                "press enter to copy it to your clipboard!\n\n(configure shortcut in json config file for now)");

            var style = ImGui.GetStyle();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            var prevNull = false;
            var size = new Vector2(30, 30);
            var visibleX = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;

            unsafe
            {
                ImGui.Text("search: ");
                ImGui.SameLine();
                ImGui.InputText("", ref _currentSearch, 50, ImGuiInputTextFlags.CallbackAlways, data =>
                {
                    _searchResults = _symbols.Search(_currentSearch);
                    return 0;
                });
            }

            foreach (var symbol in _searchResults)
            {
                Common.Button(_configuration, symbol, ref size);
                AutoLine(style, size, visibleX);
            }

            if (_searchResults.Length > 0) ImGui.NewLine();

            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
            if (ImGui.CollapsingHeader("Favourites"))
            {
                if (_configuration.Palette.Count == 0)
                    ImGui.Text("Nothing here! Add symbols by right clicking them.");
                foreach (var symbol in _configuration.Palette)
                {
                    if (!_symbols.AllSymbols.ContainsKey(symbol)) continue;
                    var symbolChar = _symbols.AllSymbols[symbol];
                    Common.Button(_configuration, symbolChar, ref size);
                    AutoLine(style, size, visibleX);
                }

                ImGui.NewLine();
            }

            foreach (var category in _symbols.Symbols)
            {
                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                if (!ImGui.CollapsingHeader(category.Label)) continue;
                if (category.Chars != null)
                    foreach (var categoryChar in category.Chars)
                    {
                        Common.Button(_configuration, categoryChar, ref size);
                        AutoLine(style, size, visibleX);
                    }

                ImGui.NewLine();
            }

            ImGui.PopStyleVar();
            ImGui.NewLine();
        }

        ImGui.End();
    }

    private void AutoLine(ImGuiStylePtr style, Vector2 nextButtonSize, float visibleX)
    {
        var lastBtn = ImGui.GetItemRectMax().X;
        var nextBtn = lastBtn + style.ItemSpacing.X + nextButtonSize.X;
        if (nextBtn < visibleX) ImGui.SameLine();
    }
}