using System.Globalization;
using System.Text;

namespace Flashlight
{
    /// <summary>Builds the block of text a player can paste into a bug report or balance feedback. No game dependencies, so it can be tested.</summary>
    public static class ReportText
    {
        /// <summary>The settings that differ from the defaults, as "Name=value" pairs. Empty when nothing was changed.</summary>
        public static List<string> ChangedSettings(ModConfig config)
        {
            var defaults = new ModConfig();
            var changed = new List<string>();
            foreach (var property in typeof(ModConfig).GetProperties())
            {
                var now = property.GetValue(config);
                if (!Equals(now, property.GetValue(defaults)))
                    changed.Add($"{property.Name}={Convert.ToString(now, CultureInfo.InvariantCulture)}");
            }
            return changed;
        }

        public static string Build(string modVersion, string environment, ModConfig config, UsageStats stats, string flashlight)
        {
            var text = new StringBuilder();
            text.AppendLine($"Flashlight {modVersion} | {environment}");

            var changed = ChangedSettings(config);
            text.AppendLine(changed.Count == 0 ? "Settings: all defaults" : "Settings changed: " + string.Join(", ", changed));

            text.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"Use so far: {stats.NightsUsed} nights, {stats.HoursLit:0.#} hours lit ({stats.HoursPerNight:0.#} a night), " +
                $"{stats.DryCellsUsed} Dry Cells ({stats.DryCellsPerNight:0.##} a night), {stats.PacksUsed} Battery Packs, ran out {stats.RanOut} times"));
            text.Append("Flashlight now: " + flashlight);
            return text.ToString();
        }
    }
}
