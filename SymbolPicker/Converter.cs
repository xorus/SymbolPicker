using System.Text;
using Dalamud.Game.Text;

namespace SymbolPicker;

static class Converter
{
    public enum ConvertMode
    {
        Boxed
    }

    public static string Convert(string text, ConvertMode mode)
    {
        var newText = text.ToUpper();

        var sb = new StringBuilder();
        foreach (var ch in newText)
        {
            if (mode == ConvertMode.Boxed && ch is >= 'A' and <= 'Z')
            {
                var offset = ch - 'A';
                sb.Append((char)(SeIconChar.BoxedLetterA + offset));
            }
            else if (mode == ConvertMode.Boxed && ch is >= '0' and <= '9')
            {
                var offset = ch - '0';
                sb.Append((char)(SeIconChar.BoxedNumber0 + offset));
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}