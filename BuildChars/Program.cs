// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Unicode;

var chars = new char[]
{
    '※', '《', '》', '↑', '→', '↓', '←',
    '♀', '♂', '★', '♠', '♣', '♥', '♦', '♪', '☂',
    '♯', '○', '●', '■', '□', '▲', '▼',
    '─', '│', '┌', '┐', '└', '┘', '├', '┤', '┬', '┴', '┼',
    '…', '–', '—', '―', '«', '»', '¦', '÷'
};

var data = new Dictionary<char, string>();
foreach (var c in chars)
{
    var info = UnicodeInfo.GetCharInfo(c);
    var str = info.Name.ToLowerInvariant();
    if (info.OldName != null) str += " (" + info.OldName.ToLowerInvariant() + ")";

    data.Add(c, str);
}

string TryGetSolutionDirectoryInfo()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory != null && !directory.GetFiles("*.sln").Any())
    {
        directory = directory.Parent;
    }

    return directory?.ToString() ?? "./";
}

File.WriteAllText(TryGetSolutionDirectoryInfo() + "/Data/characters.json", JsonSerializer.Serialize(data));