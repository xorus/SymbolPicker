using System;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using ImGuiNET;

namespace SymbolPicker.UI;

public class QuickSearch
{
    private bool _pressing;
    private bool _listenForHotkey = true;
    private readonly SortedSymbols _symbols;
    private bool _pickerVisible;

    private bool _pickerWasDisplayed;
    private string _currentSearch = "";
    private string _lastConvertSource = "";
    private string _lastConvertResult = "";
    private int _currentSelected = 0;
    private SymbolChar[] _searchResults = Array.Empty<SymbolChar>();

    private bool _visible = false;
    private readonly Configuration _configuration;
    private readonly KeyState _keyState;

    private bool _ensureFocus = false;

    public QuickSearch(SortedSymbols symbols, Configuration configuration, KeyState keyState)
    {
        _configuration = configuration;
        _keyState = keyState;
        _symbols = symbols;
    }

    public void Draw()
    {
        if (_listenForHotkey && _keyState[_configuration.PickerModifier] && _keyState[_configuration.PickerKey])
        {
            if (_pressing) return;
            _keyState[_configuration.PickerModifier] = false;
            _keyState[_configuration.PickerKey] = false;
            _pressing = true;
            _pickerVisible = true;
            _visible = true;
            PluginLog.Information("show picker 1");
            return;
        }

        _pressing = false;

        if (!_visible) return;
        if (_pickerVisible && !_pickerWasDisplayed)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos());
            PluginLog.Information("show picker next");
            _currentSearch = "";
            _searchResults = Array.Empty<SymbolChar>();
            _lastConvertSource = "";
            _lastConvertResult = "";
            _currentSelected = 0;
        }

        if (_pickerVisible)
        {
            if (ImGui.Begin("game symbols popup###SymbolPicker-popup",
                    ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoBackground))
            {
                var size = new Vector2(30, 30);

                ImGui.SetNextFrameWantCaptureKeyboard(true);

                // must be done after the first init
                if (_ensureFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _ensureFocus = false;
                }

                if (_pickerVisible && !_pickerWasDisplayed)
                {
                    ImGui.SetKeyboardFocusHere();
                    _ensureFocus = true;
                }

                unsafe
                {
                    ImGui.InputText("", ref _currentSearch, 50, ImGuiInputTextFlags.CallbackAlways, data =>
                    {
                        if (_currentSearch.StartsWith("!"))
                        {
                            if (_currentSearch == _lastConvertSource) return 0;
                            _lastConvertSource = _currentSearch[1..];
                            _lastConvertResult =
                                Converter.Convert(_currentSearch[1..], Converter.ConvertMode.Boxed);
                            _searchResults = new SymbolChar[]
                            {
                                new()
                                {
                                    Char = '0',
                                    Name = "Converted to box characters",
                                    Search = Array.Empty<string>(),
                                    String = _lastConvertResult
                                }
                            };
                        }
                        else
                        {
                            _searchResults = _symbols.Search(_currentSearch, 10);
                        }

                        return 0;
                    });
                }

                if (_currentSelected >= _searchResults.Length) _currentSelected = _searchResults.Length - 1;
                if (_currentSelected < 0) _currentSelected = 0;

                if (_searchResults.Length > 0)
                {
                    var i = 0;
                    foreach (var symbol in _searchResults)
                    {
                        if (i > 0) ImGui.SameLine();
                        var s = size;
                        if (symbol.String != null)
                        {
                            s = new Vector2(0, 30);
                        }

                        Common.Button(_configuration, symbol, ref s, i == _currentSelected);
                        i++;
                    }
                }
                else
                {
                    if (_currentSearch.Length > 0 || _configuration.Palette.Count == 0)
                    {
                        ImGui.Button(_currentSearch.Length > 0 ? "No results" : "Search something...");
                    }

                    if (_currentSearch.Length == 0)
                        foreach (var symbol in _configuration.Palette)
                        {
                            if (!_symbols.AllSymbols.ContainsKey(symbol)) continue;
                            var symbolChar = _symbols.AllSymbols[symbol];
                            Common.Button(_configuration, symbolChar, ref size);
                            ImGui.SameLine();
                        }
                }

                if (ImGui.IsKeyReleased(ImGuiKey.Escape))
                {
                    Hide();
                    PluginLog.Error("bruh");
                }

                if (ImGui.IsKeyReleased(ImGuiKey.UpArrow) || ImGui.IsKeyReleased(ImGuiKey.LeftArrow))
                {
                    _currentSelected--;
                }

                if (ImGui.IsKeyReleased(ImGuiKey.Tab) || ImGui.IsKeyReleased(ImGuiKey.DownArrow) ||
                    ImGui.IsKeyReleased(ImGuiKey.RightArrow))
                {
                    _currentSelected++;
                }

                if (ImGui.IsKeyReleased(ImGuiKey.Enter))
                {
                    CopyPicker();
                    Hide();
                }
                // if ((_keyState[VirtualKey.CONTROL] && _keyState[VirtualKey.C]) || (_keyState[VirtualKey.RETURN]))
                // {
                //     CopyPicker();
                //     _pickerVisible = false;
                // }

                _pickerWasDisplayed = true;
                if (!ImGui.IsWindowFocused()) Hide();
            }
            else _pickerWasDisplayed = false;

            ImGui.End();
        }
        else _pickerWasDisplayed = false;
    }

    private void Hide()
    {
        ImGui.SetNextFrameWantCaptureKeyboard(false);
        _pickerVisible = false;
    }

    private void CopyPicker()
    {
        ImGui.SetWindowFocus(null);
        if (_searchResults.Length <= 0) return;
        ImGui.SetClipboardText(_searchResults[0].String ?? _searchResults[0].Char.ToString());
    }
}