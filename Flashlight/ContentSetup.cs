using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Tools;

namespace Flashlight
{
    /// <summary>Adds the flashlight, the battery, the shop entries, the recipe and the pictures to the game's data.</summary>
    internal sealed class ContentSetup
    {
        public const string ModId = "Kkthnx.Flashlight";
        public const string FlashlightId = ModId + "_Flashlight";
        public const string BatteryId = ModId + "_Battery";
        public const string QualifiedFlashlightId = "(T)" + FlashlightId;
        public const string QualifiedBatteryId = "(O)" + BatteryId;

        public static readonly string[] BeamNames = { "Up", "Right", "Down", "Left" }; // the game's facing numbers: 0 up, 1 right, 2 down, 3 left

        private const string ObjectsTexture = "Mods/" + ModId + "/Objects";
        private const string ToolsTexture = "Mods/" + ModId + "/Tools";
        private const string BeamTexturePrefix = "Mods/" + ModId + "/Beam";

        private readonly ITranslationHelper _i18n;
        private readonly Func<ModConfig> _config;

        public ContentSetup(ITranslationHelper i18n, Func<ModConfig> config)
        {
            _i18n = i18n;
            _config = config;
        }

        public static string BeamAssetName(int facing) => BeamTexturePrefix + BeamNames[facing];

        public void OnAssetRequested(AssetRequestedEventArgs e)
        {
            var config = _config();

            if (e.NameWithoutLocale.IsEquivalentTo(ObjectsTexture))
                e.LoadFromModFile<Texture2D>("assets/objects.png", AssetLoadPriority.Medium);
            else if (e.NameWithoutLocale.IsEquivalentTo(ToolsTexture))
                e.LoadFromModFile<Texture2D>("assets/tools.png", AssetLoadPriority.Medium);
            else if (e.NameWithoutLocale.StartsWith(BeamTexturePrefix, true))
            {
                for (var facing = 0; facing < BeamNames.Length; facing++)
                {
                    if (e.NameWithoutLocale.IsEquivalentTo(BeamAssetName(facing)))
                        e.LoadFromModFile<Texture2D>($"assets/beam-{BeamNames[facing].ToLowerInvariant()}.png", AssetLoadPriority.Medium);
                }
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset => asset.AsDictionary<string, ObjectData>().Data[BatteryId] = new ObjectData
                {
                    Name = BatteryId,
                    DisplayName = _i18n.Get("item.battery.name"),
                    Description = _i18n.Get("item.battery.description"),
                    Type = "Basic",
                    Category = StardewValley.Object.metalResources, // a resource: it cannot be placed in the world
                    Price = 30,
                    Texture = ObjectsTexture,
                    SpriteIndex = 0,
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset => asset.AsDictionary<string, ToolData>().Data[FlashlightId] = new ToolData
                {
                    ClassName = "GenericTool",
                    Name = FlashlightId,
                    DisplayName = _i18n.Get("item.flashlight.name"),
                    Description = _i18n.Get("item.flashlight.description"),
                    Texture = ToolsTexture,
                    SpriteIndex = 0,
                    MenuSpriteIndex = 0,
                    SalePrice = config.FlashlightPrice,
                    CanBeLostOnDeath = false,
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                // ingredients / where it is made / result and count / is it a big craftable / how it is learned / display name
                // 334 is a Copper Bar and 382 is Coal. The recipe is learned at Mining level 2.
                e.Edit(asset => asset.AsDictionary<string, string>().Data[BatteryId] =
                    $"334 1 382 1/Home/{BatteryId} 3/false/s Mining 2/{_i18n.Get("recipe.battery.name")}");
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops") && config.SoldAtCarpenter)
            {
                e.Edit(asset =>
                {
                    var shops = asset.AsDictionary<string, ShopData>().Data;
                    if (!shops.TryGetValue("Carpenter", out var shop))
                        return; // Robin's shop is missing or renamed by another mod; sell nothing instead of breaking
                    shop.Items.Add(new ShopItemData { Id = FlashlightId, ItemId = QualifiedFlashlightId, Price = config.FlashlightPrice });
                    shop.Items.Add(new ShopItemData { Id = BatteryId, ItemId = QualifiedBatteryId, Price = config.BatteryPrice });
                });
            }
        }
    }
}
