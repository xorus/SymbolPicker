using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin;

namespace SymbolPicker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public VirtualKey PickerModifier = VirtualKey.CONTROL;
        public VirtualKey PickerKey = VirtualKey.OEM_PERIOD;

        public List<char> Palette { get; set; } = new()
        {
            // (char)SeIconChar.
        };

        [NonSerialized] private DalamudPluginInterface? _pluginInterface;

        [NonSerialized] public char? ScheduledPaletteRemoval = null;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface!.SavePluginConfig(this);
        }
    }
}