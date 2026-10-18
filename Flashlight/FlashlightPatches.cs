using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Small Harmony patches that only act on the flashlight and leave every other item alone.
    ///
    /// Battery slots work like the bait slot of a fishing rod. The game already has the slots, their drawing and the clicking
    /// for any tool, but a plain tool accepts any item in them and a whole stack at once, so two patches make the flashlight
    /// take only Dry Cells, one to a slot.
    ///
    /// The charge bar is drawn under the picture like the water bar of the watering can, and the tooltip says how much charge is left.
    ///
    /// A farmer can only carry one flashlight: the game's add-to-inventory methods are told a second one does not fit.
    /// </summary>
    internal static class FlashlightPatches
    {
        /// <summary>How far the picture is lifted to make room for the charge bar under it, in pixels. The watering can uses the same.</summary>
        private const float BarLift = 12f;

        /// <summary>Builds the line of text about the charge for the tooltip, from a share of a full load. Set by the mod.</summary>
        public static Func<float, string>? ChargeText { get; set; }

        /// <summary>Called when someone tries to load a cell into a spare flashlight. Set by the mod.</summary>
        public static Action? SpareRefused { get; set; }

        /// <summary>Called when a cell is refused because it does not match the one already loaded. Set by the mod.</summary>
        public static Action? MismatchRefused { get; set; }

        /// <summary>Called when a second flashlight is refused because the farmer already carries one. Set by the mod.</summary>
        public static Action? SecondRefused { get; set; }

        public static void Apply(IMonitor monitor)
        {
            var harmony = new Harmony(ContentSetup.ModId);
            Patch(monitor, "battery slots (what they accept)", () => harmony.Patch(
                original: AccessTools.Method(typeof(Tool), "canThisBeAttached", new[] { typeof(StardewValley.Object), typeof(int) }),
                postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(CanThisBeAttachedPostfix))));
            Patch(monitor, "battery slots (loading and unloading)", () => harmony.Patch(
                original: AccessTools.Method(typeof(Tool), nameof(Tool.attach), new[] { typeof(StardewValley.Object) }),
                prefix: new HarmonyMethod(typeof(FlashlightPatches), nameof(AttachPrefix))));
            Patch(monitor, "charge bar", () => harmony.Patch(
                original: AccessTools.Method(typeof(Tool), nameof(Tool.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(FlashlightPatches), nameof(DrawInMenuPrefix)),
                postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(DrawInMenuPostfix))));
            Patch(monitor, "one flashlight in the backpack", () =>
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventory), new[] { typeof(Item), typeof(List<Item>) }),
                    prefix: new HarmonyMethod(typeof(FlashlightPatches), nameof(AddItemPrefix)));
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventory), new[] { typeof(Item), typeof(int) }),
                    prefix: new HarmonyMethod(typeof(FlashlightPatches), nameof(AddItemPrefix)));
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.couldInventoryAcceptThisItem), new[] { typeof(Item) }),
                    postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(CouldAcceptPostfix)));
            });
            Patch(monitor, "flashlight in the hand", () => harmony.Patch(
                original: AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.draw), new[]
                {
                    typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2),
                    typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer),
                }),
                postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(FarmerDrawPostfix))));
            Patch(monitor, "tooltip charge line", () => harmony.Patch(
                original: AccessTools.Method(typeof(Tool), nameof(Tool.getDescription), Type.EmptyTypes),
                postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(GetDescriptionPostfix))));
        }

        private static void Patch(IMonitor monitor, string what, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception ex)
            {
                monitor.Log($"Could not add the {what} to the flashlight, so that part will not work: {ex.Message}", LogLevel.Error);
            }
        }

        // ---------------------------------------------------------------- battery slots

        /// <summary>
        /// A slot of the flashlight in use takes a Dry Cell or a Battery Pack and nothing else, and only one that matches the cell in the
        /// other slot. A spare flashlight takes nothing.
        /// </summary>
        private static void CanThisBeAttachedPostfix(Tool __instance, StardewValley.Object o, int slot, ref bool __result)
        {
            if (!FlashlightState.IsFlashlight(__instance))
                return;
            __result = FlashlightState.CanLoadInto(__instance, o, slot) && FlashlightState.IsActive(__instance);
        }

        /// <summary>
        /// Loading puts one cell in the first empty slot and hands the rest of the stack back, so the flashlight can never hold more
        /// cells than it has slots. The two cells have to match. Taking out (an empty hand) gives back the first loaded cell.
        /// </summary>
        private static bool AttachPrefix(Tool __instance, StardewValley.Object o, ref StardewValley.Object __result)
        {
            if (!FlashlightState.IsFlashlight(__instance))
                return true; // every other tool keeps the game's own logic

            var slots = __instance.attachments;
            if (o is null)
            {
                for (var i = 0; i < slots.Count; i++)
                {
                    if (slots[i] is null)
                        continue;
                    __result = slots[i];
                    slots[i] = null;
                    Game1.playSound("dwop");
                    return false;
                }
                __result = null!;
                return false;
            }

            __result = o;
            if (!FlashlightState.CanLoad(o))
                return false;
            if (!FlashlightState.IsActive(__instance))
            {
                SpareRefused?.Invoke(); // a spare flashlight takes no cells, so owning several stores nothing extra
                return false;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] is not null)
                    continue;
                if (!FlashlightState.CanLoadInto(__instance, o, i))
                {
                    MismatchRefused?.Invoke(); // the two batteries must be the same kind
                    return false;
                }
                var one = (StardewValley.Object)o.getOne();
                one.Stack = 1;
                slots[i] = one;
                __result = (StardewValley.Object)o.ConsumeStack(1)!;
                Game1.playSound("button1");
                return false;
            }
            return false; // every slot is full, so the cell stays in the hand
        }

        // ---------------------------------------------------------------- one flashlight in the backpack

        /// <summary>Whether this is a second flashlight for a farmer who already has one in the backpack.</summary>
        private static bool IsSecondFlashlight(Farmer farmer, Item? item) =>
            FlashlightState.IsFlashlight(item) && FlashlightState.ActiveIn(farmer) is { } have && !ReferenceEquals(have, item);

        /// <summary>
        /// A farmer who carries a flashlight cannot take a second one. The game is told the item did not fit, so a pickup stays
        /// where it is and a chest keeps it, which means nothing is lost and nothing can bounce back and forth.
        /// </summary>
        private static bool AddItemPrefix(Farmer __instance, Item item, ref Item __result)
        {
            if (!IsSecondFlashlight(__instance, item))
                return true;
            __result = item;
            if (__instance.IsLocalPlayer)
                SecondRefused?.Invoke();
            return false;
        }

        /// <summary>Menus that ask whether an item would fit (crafting, rewards) get the same answer.</summary>
        private static void CouldAcceptPostfix(Farmer __instance, Item item, ref bool __result)
        {
            if (__result && IsSecondFlashlight(__instance, item))
                __result = false;
        }

        // ---------------------------------------------------------------- how it looks

        /// <summary>
        /// Lifts the picture a little to make room for the bar, as the watering can does, but only where the bar will be drawn.
        /// Remembers where the slot began.
        /// </summary>
        private static void DrawInMenuPrefix(Tool __instance, ref Vector2 location, StackDrawType drawStackNumber, out Vector2 __state)
        {
            __state = location;
            if (drawStackNumber != StackDrawType.Hide && FlashlightState.IsFlashlight(__instance))
                location.Y -= BarLift;
        }

        private static void DrawInMenuPostfix(Tool __instance, SpriteBatch spriteBatch, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, Vector2 __state)
        {
            if (!FlashlightState.IsFlashlight(__instance))
                return;
            var withBar = drawStackNumber != StackDrawType.Hide;
            if (FlashlightState.IsLit(__instance))
                FlashlightVisuals.DrawLitIcon(spriteBatch, withBar ? __state - new Vector2(0f, BarLift) : __state, scaleSize, transparency, layerDepth, color);
            if (withBar)
                FlashlightVisuals.DrawBar(spriteBatch, __state, transparency, layerDepth, __instance);
        }

        private static bool _heldFailed;

        /// <summary>Draws the flashlight in the hand once the game has drawn the farmer. It must never stop the game from drawing.</summary>
        private static void FarmerDrawPostfix(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float scale, Farmer who)
        {
            if (_heldFailed)
                return;
            try
            {
                HeldFlashlight.Draw(b, who, animationFrame, currentFrame, position, origin, layerDepth, facingDirection, overrideColor, scale);
            }
            catch (Exception ex)
            {
                _heldFailed = true; // one message, not one every frame
                HeldFlashlight.OnError?.Invoke(ex.ToString());
            }
        }

        private static void GetDescriptionPostfix(Tool __instance, ref string __result)
        {
            if (ChargeText is not null && FlashlightState.IsFlashlight(__instance))
                __result += "\n\n" + ChargeText(FlashlightState.Reserve(__instance));
        }
    }
}
