﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using Dalamud.Game.Text;

namespace SymbolPicker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<char> Palette { get; set; } = new()
        {
            // (char)SeIconChar.
        };
        
        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}