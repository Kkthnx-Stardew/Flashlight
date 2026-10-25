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

        public const string FlashlightId = ModId + "_Flashlight";

        public const string BatteryId = ModId + "_Battery";
        public const string QualifiedFlashlightId = "(T)" + FlashlightId;
        public const string QualifiedBatteryId = "(O)" + BatteryId;

        public static readonly string[] BeamNames = { "Up", "Right", "Down", "Left" }; // the game's facing numbers: 0 up, 1 right, 2 down, 3 left

        private const string ObjectsTexture = "Mods/" + ModId + "/Objects";
        public const string ToolsTexture = "Mods/" + ModId + "/Tools";
        public const string HeldTexture = "Mods/" + ModId + "/Held";
        private const string BeamTexturePrefix = "Mods/" + ModId + "/Beam";

        /// <summary>What a battery sells for. It is well under the shop price and under the cost of the recipe's ingredients.</summary>
        private const int BatterySellPrice = 3;

        public const string MailId = ModId + "_RobinAd";

        /// <summary>The ids of Robin's and Clint's shops in Data/Shops.</summary>
        internal const string CarpenterShopId = "Carpenter";
        internal const string BlacksmithShopId = "Blacksmith";

        /// <summary>Robin's letter waits until the save is this many days old, so it does not land on the first morning.</summary>
        private const int MailAfterDays = 4;

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
            else if (e.NameWithoutLocale.IsEquivalentTo(HeldTexture))
                e.LoadFromModFile<Texture2D>("assets/held.png", AssetLoadPriority.Medium);
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
                    // it is not a collectible: Perfection must not ask for it, and no merchant or cart may sell it past the shop's limits
                    ExcludeFromShippingCollection = true,
                    ExcludeFromFishingCollection = true,
                    ExcludeFromRandomSale = true,
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset =>
                {
                    var tools = asset.AsDictionary<string, ToolData>().Data;
                    tools[FlashlightId] = NewFlashlight(config.FlashlightPrice);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                // ingredients / where it is made / result and count / is it a big craftable / how it is learned / display name
                // 334 is a Copper Bar and 382 is Coal. The recipe is learned at Mining level 2.
                e.Edit(asset => asset.AsDictionary<string, string>().Data[BatteryId] =
                    $"334 1 382 1/Home/{BatteryId} 12/false/s Mining 2/{_i18n.Get("recipe.battery.name")}");
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail") && MailWanted(config))
            {
                // the text, then [#] and the title shown on the envelope
                e.Edit(asset => asset.AsDictionary<string, string>().Data[MailId] =
                    $"{_i18n.Get("mail.robin.text", new { price = config.FlashlightPrice, cell = config.BatteryPrice })}[#]{_i18n.Get("mail.robin.title")}");
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/TriggerActions") && MailWanted(config))
            {
                e.Edit(asset => asset.GetData<List<TriggerActionData>>().Add(new TriggerActionData
                {
                    Id = MailId,
                    Trigger = "DayStarted",
                    Condition = ShopRules.MailAd(QualifiedFlashlightId, MailAfterDays),
                    Action = $"AddMail Current {MailId} Tomorrow",
                    MarkActionApplied = true, // once for each player in a save, however often the day starts
                }));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops") && (config.SoldAtCarpenter || config.SoldAtBlacksmith))
            {
                e.Edit(asset =>
                {
                    var shops = asset.AsDictionary<string, ShopData>().Data;
                    if (config.SoldAtCarpenter)
                        AddToShop(shops, CarpenterShopId, config);
                    if (config.SoldAtBlacksmith)
                        AddToShop(shops, BlacksmithShopId, config);
                });
            }
        }

        /// <summary>Adds the flashlight and the Dry Cell to a shop. A shop that is missing or renamed by another mod sells nothing instead of breaking.</summary>
        private static void AddToShop(IDictionary<string, ShopData> shops, string shopId, ModConfig config)
        {
            if (!shops.TryGetValue(shopId, out var shop))
                return;
            shop.Items.Add(FlashlightShopItem(config));
            shop.Items.Add(BatteryShopItem(config));
        }

        /// <summary>The letter advertises the shop, so it is only sent while the shop sells the flashlight.</summary>
        internal static bool MailWanted(ModConfig config) => config.SendMail && config.SoldAtCarpenter;

        private ToolData NewFlashlight(int salePrice) => new()
        {
            ClassName = "GenericTool",
            Name = FlashlightId,
            DisplayName = _i18n.Get("item.flashlight.name"),
            Description = _i18n.Get("item.flashlight.description"),
            Texture = ToolsTexture,
            SpriteIndex = 0,
            MenuSpriteIndex = 0,
            SalePrice = salePrice,
            AttachmentSlots = FlashlightState.Slots,
            CanBeLostOnDeath = false,
        };

        /// <summary>One flashlight per player: it is only on sale to someone who has none.</summary>
        internal static ShopItemData FlashlightShopItem(ModConfig config) => new()
        {
            Id = FlashlightId,
            ItemId = QualifiedFlashlightId,
            Price = config.FlashlightPrice,
            Condition = ShopRules.OneFlashlight(QualifiedFlashlightId),
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
                new() { Id = "OutOfStock", Condition = ShopRules.DailyRoll(ModId + "_BatteryOut", config.BatteryOutOfStockPercent), Modification = QuantityModifier.ModificationType.Set, Amount = 0 },
                new() { Id = "LowStock", Condition = ShopRules.DailyRoll(ModId + "_BatteryLow", config.BatteryLowStockPercent), Modification = QuantityModifier.ModificationType.Multiply, Amount = 0.5f },
            },
            Condition = ShopRules.CarryLimit(QualifiedBatteryId, config.MaxBatteriesCarried),
        };
    }
}
