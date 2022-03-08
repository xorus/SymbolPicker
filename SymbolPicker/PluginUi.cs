using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;

namespace SymbolPicker
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    internal class PluginUi : IDisposable
    {
        private readonly Configuration _configuration;

        private readonly KeyState _keyState;

        private bool _settingsVisible;
        private bool _charMapVisible;
        private VirtualKey Modifier = VirtualKey.CONTROL;
        private VirtualKey Key = VirtualKey.OEM_PERIOD;
        private bool pressing = false;
        private bool _listenForHotkey = true;
        private SortedSymbols _symbols;
        private bool _pickerVisible = false;

        public void OpenCharMap()
        {
            _charMapVisible = true;
        }

        // passing in the image here just for simplicity
        public PluginUi(Configuration configuration, KeyState keyState, SortedSymbols symbols)
        {
            _configuration = configuration;
            _keyState = keyState;
            _symbols = symbols;
            // _inputSimulator = new InputSimulator();
            // _dalamud.UiBuilder.BuildFonts += BuildFont;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (_deferredRemoveCharFromPalette != null)
            {
                _configuration.Palette.Remove((char)_deferredRemoveCharFromPalette);
                _deferredRemoveCharFromPalette = null;
            }

            QuickWindow();
            if (_charMapVisible) PickerWindow();

            if (_listenForHotkey && _keyState[Modifier] && _keyState[Key])
            {
                if (pressing) return;
                _keyState[Modifier] = false;
                _keyState[Key] = false;
                pressing = true;
                _pickerVisible = !_pickerVisible;
                PluginLog.Information("show picker");
                return;
            }

            pressing = false;
        }

        private char? _deferredRemoveCharFromPalette = null;

        private void Button(SymbolChar symbol, ref Vector2 size, bool active = false)
        {
            var useFont = symbol.IsSe;

            // if (useFont) ImGui.PushFont(_font);
            if (active) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
            if (ImGui.Button(symbol.String ?? symbol.Char.ToString(), size))
            {
                ImGui.SetWindowFocus(null);
                ImGui.SetClipboardText(symbol.String ?? symbol.Char.ToString());
                // Paste();
            }

            if (active) ImGui.PopStyleColor();
            // if (useFont) ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (symbol.String == null)
                {
                    ImGui.Text($"0x{(int)symbol.Char:X} " + symbol.Name + "\nRight-click to add/remove from palette\n" +
                               string.Join(";", symbol.Search));
                }
                else ImGui.Text(symbol.String);

                ImGui.EndTooltip();
            }


            if (symbol.String == null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // can't remove now because the list might be iterated at the moment
                if (_configuration.Palette.Contains(symbol.Char)) _deferredRemoveCharFromPalette = symbol.Char;
                else _configuration.Palette.Add(symbol.Char);
                _configuration.Save();
            }
        }

        private void AutoLine(ImGuiStylePtr style, Vector2 nextButtonSize, float visibleX)
        {
            var lastBtn = ImGui.GetItemRectMax().X;
            var nextBtn = lastBtn + style.ItemSpacing.X + nextButtonSize.X;
            if (nextBtn < visibleX) ImGui.SameLine();
        }

        private void CopyPicker()
        {
            ImGui.SetWindowFocus(null);
            if (_searchResults.Length <= 0) return;
            ImGui.SetClipboardText(_searchResults[0].String ?? _searchResults[0].Char.ToString());
        }

        private bool _pickerWasDisplayed = false;
        private string _currentSearch = "";
        private string _lastConvertSource = "";
        private string _lastConvertResult = "";
        private int _currentSelected = 0;
        private SymbolChar[] _searchResults = Array.Empty<SymbolChar>();

        private void HideQW()
        {
            ImGui.CaptureKeyboardFromApp(false);
            _pickerVisible = false;
        }

        private void QuickWindow()
        {
            if (_pickerVisible && !_pickerWasDisplayed)
            {
                ImGui.SetNextWindowPos(ImGui.GetMousePos());
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

                    ImGui.CaptureKeyboardFromApp(true);

                    if (_currentSelected >= _searchResults.Length) _currentSelected = _searchResults.Length - 1;
                    if (_currentSelected < 0) _currentSelected = 0;

                    if (_searchResults.Length > 0)
                    {
                        var i = 0;
                        foreach (var symbol in _searchResults)
                        {
                            ImGui.SameLine();
                            var s = size;
                            if (symbol.String != null)
                            {
                                s = new Vector2(0, 30);
                            }

                            Button(symbol, ref s, i == _currentSelected);
                            i++;
                        }
                    }
                    else
                    {
                        if (_currentSearch.Length > 0 || _configuration.Palette.Count == 0)
                        {
                            ImGui.SameLine();
                            ImGui.Button(_currentSearch.Length > 0 ? "No results" : "Search something...");
                        }

                        if (_currentSearch.Length == 0)
                            foreach (var symbol in _configuration.Palette)
                            {
                                if (!_symbols.AllSymbols.ContainsKey(symbol)) continue;
                                var symbolChar = _symbols.AllSymbols[symbol];
                                ImGui.SameLine();
                                Button(symbolChar, ref size);
                            }
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

                    if (_pickerVisible && !_pickerWasDisplayed) ImGui.SetKeyboardFocusHere();
                    ImGui.SameLine();

                    if (ImGui.IsKeyReleased((int)ImGuiKey.Escape))
                    {
                        HideQW();
                        PluginLog.Error("bruh");
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.UpArrow) || ImGui.IsKeyReleased((int)ImGuiKey.LeftArrow))
                    {
                        _currentSelected--;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.Tab) || ImGui.IsKeyReleased((int)ImGuiKey.DownArrow) ||
                        ImGui.IsKeyReleased((int)ImGuiKey.RightArrow))
                    {
                        _currentSelected++;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.Enter))
                    {
                        CopyPicker();
                        HideQW();
                    }
                    // if ((_keyState[VirtualKey.CONTROL] && _keyState[VirtualKey.C]) || (_keyState[VirtualKey.RETURN]))
                    // {
                    //     CopyPicker();
                    //     _pickerVisible = false;
                    // }

                    _pickerWasDisplayed = true;
                    if (!ImGui.IsWindowFocused()) HideQW();
                }
                else _pickerWasDisplayed = false;

                ImGui.End();
            }
            else _pickerWasDisplayed = false;
        }

        private void PickerWindow()
        {
            if (ImGui.Begin("charmap.exe###SymbolPicker-main", ref _charMapVisible))
            {
                ImGui.TextWrapped("For now, clicking a symbol will copy it to your clipboard. " +
                                  "The window will then unfocus, allowing you to paste it in an in-game text box using " +
                                  "control-v.");

                ImGui.TextWrapped(
                    "Use the quick picker by pressing Control+.\n" +
                    "It will pop a text field on your cursor that will let you search for a symbol. Click one or " +
                    "press enter to copy it to your clipboard!");

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
                    Button(symbol, ref size);
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
                        Button(symbolChar, ref size);
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
                            Button(categoryChar, ref size);
                            AutoLine(style, size, visibleX);
                        }

                    ImGui.NewLine();
                }

                ImGui.PopStyleVar();
                ImGui.NewLine();
            }

            ImGui.End();
        }
    }
}