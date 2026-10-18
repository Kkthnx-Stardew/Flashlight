using System.Text.Json;
using System.Text.RegularExpressions;

namespace Flashlight.Tests;

/// <summary>Checks the files that ship with the mod: the manifest, the translations and the pictures.</summary>
public class ShippedFilesTests
{
    private static string ProjectDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Flashlight.sln")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir!, "Flashlight");
    }

    [Fact]
    public void ManifestHasTheRequiredFields()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectDir(), "manifest.json")));
        var root = doc.RootElement;
        Assert.Equal("Kkthnx.Flashlight", root.GetProperty("UniqueID").GetString());
        Assert.Equal("Flashlight.dll", root.GetProperty("EntryDll").GetString());
        Assert.Equal("%ProjectVersion%", root.GetProperty("Version").GetString());
        Assert.True(Version.TryParse(root.GetProperty("MinimumGameVersion").GetString(), out var min) && min >= new Version(1, 6, 9));
    }

    [Fact]
    public void EveryTranslationFileHasTheSameKeysAsTheDefault()
    {
        var i18n = Path.Combine(ProjectDir(), "i18n");
        var keys = Keys(Path.Combine(i18n, "default.json"));
        foreach (var file in Directory.GetFiles(i18n, "*.json"))
            Assert.Equal(keys, Keys(file));
    }

    [Theory]
    [InlineData("objects.png", 16, 16)]
    [InlineData("tools.png", 32, 16)]
    [InlineData("beam-up.png", 128, 128)]
    [InlineData("beam-right.png", 128, 128)]
    [InlineData("beam-down.png", 128, 128)]
    [InlineData("beam-left.png", 128, 128)]
    public void PicturesHaveTheSizeTheCodeExpects(string name, int width, int height)
    {
        var bytes = File.ReadAllBytes(Path.Combine(ProjectDir(), "assets", name));
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
        Assert.Equal(width, ReadInt(bytes, 16));  // the IHDR chunk starts at byte 8; its width and height follow the length and type
        Assert.Equal(height, ReadInt(bytes, 20));
    }

    [Fact]
    public void EveryTextKeyUsedByTheCodeExists()
    {
        var keys = Keys(Path.Combine(ProjectDir(), "i18n", "default.json"));
        var used = 0;
        foreach (var file in Directory.GetFiles(ProjectDir(), "*.cs"))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(file), "\\.Get\\(\"([^\"]+)\"\\)"))
            {
                Assert.Contains(m.Groups[1].Value, keys);
                used++;
            }
        }
        Assert.True(used > 0, "The check found no translation keys, so it is not looking in the right place.");
    }

    private static int ReadInt(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];

    private static List<string> Keys(string file)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        return doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
    }
}
