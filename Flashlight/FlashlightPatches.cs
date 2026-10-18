using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Gives the flashlight battery slots that work like the bait slot on a fishing rod. The game already has the slots,
    /// their drawing and the clicking for any tool, but a plain tool accepts any item in them and a whole stack at once.
    /// These two patches make the flashlight take only Dry Cells, one to a slot, and touch no other tool.
    /// </summary>
    internal static class FlashlightPatches
    {
        public static void Apply(IMonitor monitor)
        {
            var harmony = new Harmony(ContentSetup.ModId);
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Tool), "canThisBeAttached", new[] { typeof(StardewValley.Object), typeof(int) }),
                    postfix: new HarmonyMethod(typeof(FlashlightPatches), nameof(CanThisBeAttachedPostfix)));
                harmony.Patch(
                    original: AccessTools.Method(typeof(Tool), nameof(Tool.attach), new[] { typeof(StardewValley.Object) }),
                    prefix: new HarmonyMethod(typeof(FlashlightPatches), nameof(AttachPrefix)));
            }
            catch (Exception ex)
            {
                monitor.Log("Could not add the battery slots to the flashlight, so it may take any item in them: " + ex.Message, LogLevel.Error);
            }
        }

        /// <summary>A slot of the flashlight takes a Dry Cell and nothing else.</summary>
        private static void CanThisBeAttachedPostfix(Tool __instance, StardewValley.Object o, ref bool __result)
        {
            if (!ContentSetup.IsFlashlight(__instance))
                return;
            __result = o?.QualifiedItemId == ContentSetup.QualifiedBatteryId;
        }

        /// <summary>
        /// Loading puts one Dry Cell in the first empty slot and hands the rest of the stack back, so the flashlight can never
        /// hold more cells than it has slots. Taking out (an empty hand) gives back the first loaded cell.
        /// </summary>
        private static bool AttachPrefix(Tool __instance, StardewValley.Object o, ref StardewValley.Object __result)
        {
            if (!ContentSetup.IsFlashlight(__instance))
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
    }
}
