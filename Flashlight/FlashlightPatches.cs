using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Four small Harmony patches that only act on the flashlight and leave every other item alone.
    ///
    /// Battery slots work like the bait slot of a fishing rod. The game already has the slots, their drawing and the clicking
    /// for any tool, but a plain tool accepts any item in them and a whole stack at once, so two patches make the flashlight
    /// take only Dry Cells, one to a slot.
    ///
    /// The charge bar is drawn under the picture like the water bar of the watering can, the flashlight is drawn in the hand
    /// of a farmer who has it selected, and the tooltip says how much charge is left.
    /// </summary>
    internal static class FlashlightPatches
    {
        /// <summary>Builds the line of text about the charge for the tooltip, from a share of a full load. Set by the mod.</summary>
        public static Func<float, string>? ChargeText { get; set; }

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
            Patch(monitor, "flashlight in hand", () => harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.draw), new[] { typeof(SpriteBatch) }),
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

        /// <summary>A slot of the flashlight takes a Dry Cell and nothing else.</summary>
        private static void CanThisBeAttachedPostfix(Tool __instance, StardewValley.Object o, ref bool __result)
        {
            if (!FlashlightState.IsFlashlight(__instance))
                return;
            __result = o?.QualifiedItemId == ContentSetup.QualifiedBatteryId;
        }

        /// <summary>
        /// Loading puts one Dry Cell in the first empty slot and hands the rest of the stack back, so the flashlight can never
        /// hold more cells than it has slots. Taking out (an empty hand) gives back the first loaded cell.
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
            if (o.QualifiedItemId != ContentSetup.QualifiedBatteryId)
                return false;

            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] is not null)
                    continue;
                var one = (StardewValley.Object)o.getOne();
                one.Stack = 1;
                slots[i] = one;
                __result = (StardewValley.Object)o.ConsumeStack(1)!;
                Game1.playSound("button1");
                return false;
            }
            return false; // every slot is full; the cell stays in the hand
        }

        // ---------------------------------------------------------------- how it looks

        /// <summary>Lifts the picture a little to make room for the bar, as the watering can does, and remembers where the slot began.</summary>
        private static void DrawInMenuPrefix(Tool __instance, ref Vector2 location, out Vector2 __state)
        {
            __state = location;
            if (FlashlightState.IsFlashlight(__instance))
                location.Y -= 12f;
        }

        private static void DrawInMenuPostfix(Tool __instance, SpriteBatch spriteBatch, float transparency, float layerDepth, StackDrawType drawStackNumber, Vector2 __state)
        {
            if (drawStackNumber != StackDrawType.Hide && FlashlightState.IsFlashlight(__instance))
                FlashlightVisuals.DrawBar(spriteBatch, __state, transparency, layerDepth, __instance);
        }

        private static void FarmerDrawPostfix(Farmer __instance, SpriteBatch b) => FlashlightVisuals.DrawHeld(b, __instance);

        private static void GetDescriptionPostfix(Tool __instance, ref string __result)
        {
            if (ChargeText is not null && FlashlightState.IsFlashlight(__instance))
                __result += "\n\n" + ChargeText(FlashlightState.Reserve(__instance));
        }
    }
}
