using StardewModdingAPI;

namespace Flashlight
{
    /// <summary>Adds the settings to Generic Mod Config Menu, when it is installed.</summary>
    internal static class ConfigMenu
    {
        public static void Setup(IMod mod, IModHelper helper, Func<ModConfig> config, Action saved)
        {
            var menu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (menu is null)
                return;

            var manifest = mod.ModManifest;
            var t = helper.Translation;

            menu.Register(
                mod: manifest,
                reset: () => config().CopyFrom(new ModConfig()),
                save: saved);

            menu.AddSectionTitle(manifest, () => t.Get("gmcm.use"));
            menu.AddTextOption(manifest, () => config().ToggleKey, v => config().ToggleKey = v, () => t.Get("gmcm.key.name"), () => t.Get("gmcm.key.tip"));
            menu.AddNumberOption(manifest, () => config().BeamLengthTiles, v => config().BeamLengthTiles = v, () => t.Get("gmcm.beam.name"), () => t.Get("gmcm.beam.tip"), 3, 12, 1);
            menu.AddNumberOption(manifest, () => config().BatteryHours, v => config().BatteryHours = v, () => t.Get("gmcm.hours.name"), () => t.Get("gmcm.hours.tip"), 1, 12, 1);
            menu.AddNumberOption(manifest, () => config().FlickerSecondsBetween, v => config().FlickerSecondsBetween = v, () => t.Get("gmcm.flicker.name"), () => t.Get("gmcm.flicker.tip"), 0, 600, 5,
                seconds => seconds == 0 ? t.Get("gmcm.off") : $"{seconds} s");

            menu.AddSectionTitle(manifest, () => t.Get("gmcm.shop"), () => t.Get("gmcm.shop.tip"));
            menu.AddBoolOption(manifest, () => config().SoldAtCarpenter, v => config().SoldAtCarpenter = v, () => t.Get("gmcm.sold.name"), () => t.Get("gmcm.sold.tip"));
            menu.AddNumberOption(manifest, () => config().FlashlightPrice, v => config().FlashlightPrice = v, () => t.Get("gmcm.price.name"), null, 0, 5000, 50);
            menu.AddNumberOption(manifest, () => config().BatteryPrice, v => config().BatteryPrice = v, () => t.Get("gmcm.cellprice.name"), null, 0, 500, 5);
            menu.AddNumberOption(manifest, () => config().BatteryDailyStock, v => config().BatteryDailyStock = v, () => t.Get("gmcm.stock.name"), () => t.Get("gmcm.stock.tip"), 1, 20, 1);
            menu.AddNumberOption(manifest, () => config().MaxBatteriesCarried, v => config().MaxBatteriesCarried = v, () => t.Get("gmcm.carry.name"), () => t.Get("gmcm.carry.tip"), 1, 20, 1);
            menu.AddNumberOption(manifest, () => config().BatteryOutOfStockPercent, v => config().BatteryOutOfStockPercent = v, () => t.Get("gmcm.out.name"), () => t.Get("gmcm.out.tip"), 0, 90, 5, p => $"{p}%");
            menu.AddNumberOption(manifest, () => config().BatteryLowStockPercent, v => config().BatteryLowStockPercent = v, () => t.Get("gmcm.low.name"), () => t.Get("gmcm.low.tip"), 0, 90, 5, p => $"{p}%");
            menu.AddNumberOption(manifest, () => config().BatterySlots, v => config().BatterySlots = v, () => t.Get("gmcm.slots.name"), () => t.Get("gmcm.slots.tip"), 1, 4, 1);
        }
    }
}
