﻿using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace SymbolPicker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "FancySymbols";

        private const string PickerCommand = "/pcharmap";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUi PluginUi { get; init; }

        private WindowSystem WindowSystem { get; init; }
        
        // public delegate IntPtr TestDelegate(IntPtr)

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            SigScanner scanner,
            KeyState ks)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            // WindowSystem = new WindowSystem(Name);

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            var symbols = new SortedSymbols();
            PluginUi = new PluginUi(Configuration, ks, symbols);
            // fw.Update += (_) => PluginUi.update();

            CommandManager.AddHandler(PickerCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "A (very) useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
        }

        public void Dispose()
        {
            PluginUi.Dispose();
            CommandManager.RemoveHandler(PickerCommand);
        }

        private void OnCommand(string command, string args)
        {
            PluginUi.OpenCharMap();
        }

        private void DrawUi()
        {
            PluginUi.Draw();
        }

        private void DrawConfigUi()
        {
            PluginUi.OpenCharMap();
        }
    }
}