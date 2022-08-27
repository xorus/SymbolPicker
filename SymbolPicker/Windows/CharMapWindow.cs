using Dalamud.Interface.Windowing;
using ImGuiNET;
using PullLogger;

namespace SymbolPicker.Windows;

public class CharMapWindow: Window
{
    private readonly Container _container;

    public CharMapWindow(
        Container container,
        string name, 
        ImGuiWindowFlags flags = ImGuiWindowFlags.None, 
        bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {
        _container = container;
    }

    public override void Draw()
    {
        throw new System.NotImplementedException();
    }
}