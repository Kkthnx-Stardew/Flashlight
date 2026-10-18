using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Tools;

namespace Flashlight
{
    /// <summary>Adds the flashlight, the battery, the shop entries, the recipe and the pictures to the game's data.</summary>
    internal sealed class ContentSetup
    {
        public const string ModId = "Kkthnx.Flashlight";

        /// <summary>The flashlight when it is off. It is the one the shop sells.</summary>
        public const string FlashlightId = ModId + "_Flashlight";

        /// <summary>The same flashlight while it is on. It only differs in its picture, so the toolbar shows the state.</summary>
        public const string FlashlightOnId = ModId + "_FlashlightOn";

        public const string BatteryId = ModId + "_Battery";
        public const string QualifiedFlashlightId = "(T)" + FlashlightId;
        public const string QualifiedFlashlightOnId = "(T)" + FlashlightOnId;
        public const string QualifiedBatteryId = "(O)" + BatteryId;

        public static readonly string[] BeamNames = { "Up", "Right", "Down", "Left" }; // the game's facing numbers: 0 up, 1 right, 2 down, 3 left

        private const string ObjectsTexture = "Mods/" + ModId + "/Objects";
        private const string ToolsTexture = "Mods/" + ModId + "/Tools";
        private const string BeamTexturePrefix = "Mods/" + ModId + "/Beam";

        /// <summary>What a battery sells for. It is well under the shop price and under the cost of the recipe's ingredients.</summary>
        private const int BatterySellPrice = 15;

        private readonly ITranslationHelper _i18n;
        private readonly Func<ModConfig> _config;

        public ContentSetup(ITranslationHelper i18n, Func<ModConfig> config)
        {
            _i18n = i18n;
            _config = config;
        }

        public static string BeamAssetName(int facing) => BeamTexturePrefix + BeamNames[facing];

        public static bool IsFlashlight(Item? item) => item?.ItemId is FlashlightId or FlashlightOnId;

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
                    Price = BatterySellPrice,
                    Texture = ObjectsTexture,
                    SpriteIndex = 0,
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset =>
                {
                    var tools = asset.AsDictionary<string, ToolData>().Data;
                    tools[FlashlightId] = NewFlashlight(FlashlightId, 0, config.FlashlightPrice);
                    tools[FlashlightOnId] = NewFlashlight(FlashlightOnId, 1, -1);
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
                    shop.Items.Add(FlashlightShopItem(config));
                    shop.Items.Add(BatteryShopItem(config));
                });
            }
        }

        private ToolData NewFlashlight(string id, int spriteIndex, int salePrice) => new()
        {
            ClassName = "GenericTool",
            Name = id,
            DisplayName = _i18n.Get("item.flashlight.name"),
            Description = _i18n.Get("item.flashlight.description"),
            Texture = ToolsTexture,
            SpriteIndex = spriteIndex,
            MenuSpriteIndex = spriteIndex,
            SalePrice = salePrice,
            AttachmentSlots = _config().BatterySlots,
            CanBeLostOnDeath = false,
        };

        /// <summary>One flashlight per player: it is only on sale to someone who has none, switched on or off.</summary>
        internal static ShopItemData FlashlightShopItem(ModConfig config) => new()
        {
            Id = FlashlightId,
            ItemId = QualifiedFlashlightId,
            Price = config.FlashlightPrice,
            Condition = $"!PLAYER_HAS_ITEM Current {QualifiedFlashlightId}, !PLAYER_HAS_ITEM Current {QualifiedFlashlightOnId}",
        };

        /// <summary>Batteries are limited each day, and are not sold to someone who is already carrying the most.</summary>
        internal static ShopItemData BatteryShopItem(ModConfig config) => new()
        {
            Id = BatteryId,
            ItemId = QualifiedBatteryId,
            Price = config.BatteryPrice,
            AvailableStock = config.BatteryDailyStock,
            AvailableStockLimit = LimitedStockMode.Player,
            // A random roll that is the same all day for every player however often the shop is opened, and does not depend on what else is for sale.
            AvailableStockModifiers = new List<QuantityModifier>
            {
                new() { Id = "OutOfStock", Condition = $"SYNCED_RANDOM day {ModId}_BatteryOut {config.BatteryOutOfStockPercent / 100.0:0.##}", Modification = QuantityModifier.ModificationType.Set, Amount = 0 },
                new() { Id = "LowStock", Condition = $"SYNCED_RANDOM day {ModId}_BatteryLow {config.BatteryLowStockPercent / 100.0:0.##}", Modification = QuantityModifier.ModificationType.Multiply, Amount = 0.5f },
            },
            Condition = $"PLAYER_HAS_ITEM Current {QualifiedBatteryId} 0 {config.MaxBatteriesCarried - 1}",
        };
    }
}
