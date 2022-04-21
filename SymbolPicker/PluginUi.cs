using System;
using Dalamud.Game.ClientState.Keys;
using SymbolPicker.UI;

namespace SymbolPicker
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    internal class PluginUi : IDisposable
    {
        private readonly Configuration _configuration;
        private readonly QuickSearch _quickSearch;
        private readonly Charmap _charmap;

        public void OpenCharMap()
        {
            _charmap.Open();
        }

        // passing in the image here just for simplicity
        public PluginUi(Configuration configuration, KeyState keyState, SortedSymbols symbols)
        {
            _configuration = configuration;
            _quickSearch = new QuickSearch(symbols, configuration, keyState);
            _charmap = new Charmap(symbols, configuration);
            // _inputSimulator = new InputSimulator();
            // _dalamud.UiBuilder.BuildFonts += BuildFont;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (_configuration.ScheduledPaletteRemoval != null)
            {
                _configuration.Palette.Remove((char)_configuration.ScheduledPaletteRemoval);
                _configuration.ScheduledPaletteRemoval = null;
            }

            _quickSearch.Draw();
            _charmap.Draw();
        }
    }
}