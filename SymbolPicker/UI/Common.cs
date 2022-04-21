using System.Numerics;
using ImGuiNET;

namespace SymbolPicker.UI;

public static class Common
{
    /**
     * Symbol button
     */
    public static void Button(SymbolChar symbol, ref Vector2 size, bool active = false)
    {
        // var useFont = symbol.IsSe;
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

        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        if (symbol.String == null)
        {
            ImGui.Text($"0x{(int)symbol.Char:X} " + symbol.Name + "\nRight-click to add/remove from palette\n" +
                       string.Join(";", symbol.Search));
        }
        else ImGui.Text(symbol.String);

        ImGui.EndTooltip();
    }

    /**
     * Symbol button with remove action support
     */
    public static void Button(Configuration configuration, SymbolChar symbol, ref Vector2 size, bool active = false)
    {
        Button(symbol, ref size, active);
        if (symbol.String != null || !ImGui.IsItemClicked(ImGuiMouseButton.Right)) return;
        // can't remove now because the list might be iterated at the moment
        if (configuration.Palette.Contains(symbol.Char)) configuration.ScheduledPaletteRemoval = symbol.Char;
        else configuration.Palette.Add(symbol.Char);
        configuration.Save();
    }
}