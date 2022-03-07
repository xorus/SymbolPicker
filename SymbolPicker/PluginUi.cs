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

        private readonly TextureWrap _goatImage;
        private readonly KeyState _keyState;
        private readonly SigScanner _sigScanner;
        private readonly DalamudPluginInterface _dalamud;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool _visible;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        private bool _settingsVisible;
        private bool _charMapVisible;

        public void OpenCharMap()
        {
            _charMapVisible = true;
        }

        private VirtualKey Modifier = VirtualKey.CONTROL;
        private VirtualKey Key = VirtualKey.OEM_PERIOD;
        private bool pressing = false;

        private bool _pickerVisible = false;

        // passing in the image here just for simplicity
        public PluginUi(Configuration configuration, TextureWrap goatImage, KeyState keyState, SigScanner sigScanner,
            DalamudPluginInterface _dalamud, SortedSymbols symbols)
        {
            _configuration = configuration;
            _goatImage = goatImage;
            _keyState = keyState;
            _sigScanner = sigScanner;
            this._dalamud = _dalamud;
            _symbols = symbols;

            _inputSimulator = new InputSimulator();
            // _dalamud.UiBuilder.BuildFonts += BuildFont;
        }

        public void Dispose()
        {
            _goatImage.Dispose();
            // _dalamud.UiBuilder.BuildFonts -= BuildFont;
            // _dalamud.UiBuilder.RebuildFonts();
        }

        public void Draw()
        {
            // if (!_fontLoaded)
            // {
            //     _dalamud.UiBuilder.RebuildFonts();
            //     return;
            // }

            if (_deferredRemoveCharFromPalette != null)
            {
                _configuration.Palette.Remove((char)_deferredRemoveCharFromPalette);
                _deferredRemoveCharFromPalette = null;
            }

            // DoPaste();
            QuickWindow();
            if (_charMapVisible) PickerWindow();
            DrawSettingsWindow();

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

        private bool _listenForHotkey = true;
        private bool _pasting = false;
        private readonly InputSimulator _inputSimulator;
        private double? _doPaste = null;
        private bool _fontLoaded = false;
        private SortedSymbols _symbols;

        private void DoPaste()
        {
            if (_doPaste == null || !(ImGui.GetTime() - _doPaste > 1.2d)) return;
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            _doPaste = null;
        }

        private void Paste()
        {
            _doPaste = ImGui.GetTime();
        }

        private ImFontPtr _font;

        /**
         * UI font code adapted from ping plugin by karashiiro
         * https://github.com/karashiiro/PingPlugin/blob/feex/PingPlugin/PingUI.cs
         */
        private void BuildFont()
        {
            try
            {
                unsafe
                {
                    ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                    fontConfig.MergeMode = true;
                    fontConfig.PixelSnapH = true;

                    var fontPathGame = Path.Combine(_dalamud.DalamudAssetDirectory.FullName, "UIRes", "gamesym.ttf");
                    var gameRangeHandle = GCHandle.Alloc(new ushort[] { 0xE020, 0xE0DB, 0 }, GCHandleType.Pinned);
                    _font = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathGame, 32.0f, fontConfig,
                        gameRangeHandle.AddrOfPinnedObject());
                    fontConfig.Destroy();
                    gameRangeHandle.Free();
                    _fontLoaded = true;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
            }
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

        private string _a = "";
        private string _b = "";

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

                    if (_currentSelected >= _searchResults.Length) _currentSelected = _searchResults.Length - 1;
                    if (_currentSelected < 0) _currentSelected = 0;

                    // ImGui.Text("" + _currentSelected);
                    // ImGui.SameLine();
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

                    // ImGui.SetWindowFocus();
                    // ImGui.SetItemDefaultFocus();
                    // ImGui.SetItemDefaultFocus();
                    if (_pickerVisible && !_pickerWasDisplayed) ImGui.SetKeyboardFocusHere();


                    /*
                        ImGui.InputText("", ref _a, 50, ImGuiInputTextFlags.CallbackAlways, data =>
                        {
                            _b = Convert(_a, ConvertMode.Boxed);
                            return 0;
                        });
                        */

                    ImGui.SameLine();
                    // ImGui.PushFont(_font);
                    if (_b.Length > 0 && ImGui.Button("" + _b))
                    {
                        CopyPicker();
                        _pickerVisible = false;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.Escape))
                    {
                        _pickerVisible = false;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.UpArrow) || ImGui.IsKeyReleased((int)ImGuiKey.LeftArrow))
                    {
                        _currentSelected--;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.DownArrow) || ImGui.IsKeyReleased((int)ImGuiKey.RightArrow))
                    {
                        _currentSelected++;
                    }

                    if (ImGui.IsKeyReleased((int)ImGuiKey.Enter))
                    {
                        CopyPicker();
                        _pickerVisible = false;
                    }
                    // if ((_keyState[VirtualKey.CONTROL] && _keyState[VirtualKey.C]) || (_keyState[VirtualKey.RETURN]))
                    // {
                    //     CopyPicker();
                    //     _pickerVisible = false;
                    // }

                    if (!ImGui.IsWindowFocused()) _pickerVisible = false;

                    // ImGui.PopFont();
                    _pickerWasDisplayed = true;
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

                ImGui.Text("todo in priority order:");
                ImGui.Text("- unproper unload causes crashes?");
                ImGui.Text("- fix favourite not saving properly");
                ImGui.Text("- auto-paste on click/enter");
                ImGui.Text("- customize open shortcut");
                ImGui.Text("- open main UI button in mini-search");
                ImGui.Text("- ordering for favourites (drag and drop?)");
                ImGui.Text("- better UI");
            }

            ImGui.End();
        }


        public void DrawSettingsWindow()
        {
            return;
            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref _settingsVisible,
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = _configuration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    _configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    _configuration.Save();
                }
            }

            ImGui.End();
        }
    }
}