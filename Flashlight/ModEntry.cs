using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Extensions;

namespace Flashlight
{
    public class ModEntry : Mod
    {
        private const string ChargeKey = ContentSetup.ModId + "/Charge";

        // The beam pictures are 128 pixels wide and the cone ends 62 pixels from the centre. A picture pixel is drawn
        // "radius" world pixels wide, so a beam of N tiles (64 pixels each) needs a radius of N * 64 / 62.
        private const float ConePixels = 62f;

        // The colour is taken away from the darkness, so a warm light removes mostly blue.
        private static readonly Color LightColor = new(0, 40, 140);

        private ModConfig _config = new();
        private ContentSetup _content = null!; // set in Entry
        private SButton _toggleButton = SButton.F;
        private bool _swapping;

        // Each split-screen player has a flashlight of their own.
        private readonly PerScreen<bool> _on = new();
        private readonly PerScreen<string?> _lightId = new();
        private readonly PerScreen<string?> _lightLocation = new();
        private readonly PerScreen<bool> _warnedLow = new();
        private readonly PerScreen<bool> _warnedExtra = new();
        private readonly PerScreen<int> _flickerTicks = new();

        private readonly Texture2D?[] _beams = new Texture2D?[4];

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            _config.Clamp();
            helper.WriteConfig(_config);
            ApplyConfig();

            _content = new ContentSetup(helper.Translation, () => _config);
            FlashlightPatches.Apply(Monitor);

            helper.Events.Content.AssetRequested += (_, e) => _content.OnAssetRequested(e);
            helper.Events.Content.LocaleChanged += (_, _) => InvalidateContent();
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.SaveLoaded += (_, _) => Reset();
            helper.Events.GameLoop.DayStarted += (_, _) => Reset();
            helper.Events.GameLoop.DayEnding += (_, _) => TurnOff(announce: false);
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => ForgetLight();
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Player.InventoryChanged += (_, e) =>
            {
                if (e.IsLocalPlayer)
                    Normalize(e.Player);
            };
        }

        private void ApplyConfig()
        {
            if (!Enum.TryParse(_config.ToggleKey, ignoreCase: true, out _toggleButton))
            {
                Monitor.Log($"'{_config.ToggleKey}' is not a button name. Using F.", LogLevel.Warn);
                _toggleButton = SButton.F;
            }
        }

        private void InvalidateContent()
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Tools");
            Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
        }

        // ---------------------------------------------------------------- finding things

        /// <summary>
        /// The flashlight to use. A player is meant to have only one, and the shop sells one to a player who has none. If
        /// there are more (found, or kept from before), only the first counts, so charge cannot be stored in spares.
        /// </summary>
        private Tool? FindFlashlight(Farmer player)
        {
            Tool? first = null;
            var count = 0;
            foreach (var tool in player.Items.OfType<Tool>().Where(t => ContentSetup.IsFlashlight(t)))
            {
                first ??= tool;
                count++;
            }
            if (count > 1 && !_warnedExtra.Value)
            {
                _warnedExtra.Value = true;
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.one-flashlight"), HUDMessage.error_type));
            }
            return first;
        }

        /// <summary>The batteries loaded in the flashlight's slots. Those in the backpack are not used until they are loaded.</summary>
        private static int CountBatteries(Tool flashlight) =>
            flashlight.attachments.Where(item => item?.QualifiedItemId == ContentSetup.QualifiedBatteryId).Sum(item => item.Stack);

        /// <summary>Uses up one loaded battery, emptying its slot.</summary>
        private static void UseBattery(Tool flashlight)
        {
            for (var i = 0; i < flashlight.attachments.Count; i++)
            {
                var battery = flashlight.attachments[i];
                if (battery?.QualifiedItemId != ContentSetup.QualifiedBatteryId)
                    continue;
                battery.Stack--;
                if (battery.Stack <= 0)
                    flashlight.attachments[i] = null;
                return;
            }
        }

        private static int ReadCharge(Tool tool) =>
            tool.modData.TryGetValue(ChargeKey, out var text) && int.TryParse(text, out var charge) ? Math.Max(0, charge) : 0;

        private static void WriteCharge(Tool tool, int charge) => tool.modData[ChargeKey] = charge.ToString();

        // ---------------------------------------------------------------- the on and off pictures

        /// <summary>
        /// Swaps the flashlight in the backpack for its lit or unlit twin, keeping its slot and its charge. The two are
        /// separate items only so that the toolbar picture shows whether it is on.
        /// </summary>
        private Tool SetVariant(Farmer player, Tool tool, bool on)
        {
            var id = on ? ContentSetup.FlashlightOnId : ContentSetup.FlashlightId;
            if (tool.ItemId == id)
                return tool;
            var index = player.Items.IndexOf(tool);
            if (index < 0)
                return tool;

            _swapping = true;
            try
            {
                var fresh = ItemRegistry.Create<Tool>("(T)" + id);
                foreach (var pair in tool.modData.Pairs)
                    fresh.modData[pair.Key] = pair.Value;
                for (var i = 0; i < Math.Min(tool.attachments.Count, fresh.attachments.Count); i++)
                    fresh.attachments[i] = tool.attachments[i]; // the loaded batteries go with it
                player.Items[index] = fresh;
                return fresh;
            }
            finally
            {
                _swapping = false;
            }
        }

        /// <summary>A lit flashlight only exists while the light is on, so one found any other time is put back to unlit.</summary>
        private void Normalize(Farmer player)
        {
            if (_swapping)
                return;

            // a flashlight made before the slot count was raised in the settings gets the extra slots
            foreach (var tool in player.Items.OfType<Tool>().Where(t => ContentSetup.IsFlashlight(t)))
            {
                if (tool.AttachmentSlotsCount < _config.BatterySlots)
                    tool.AttachmentSlotsCount = _config.BatterySlots;
            }

            if (_on.Value)
                return;
            foreach (var tool in player.Items.OfType<Tool>().Where(t => t.ItemId == ContentSetup.FlashlightOnId).ToList())
                SetVariant(player, tool, on: false);
        }

        private void Reset()
        {
            _on.Value = false;
            _flickerTicks.Value = 0;
            _warnedExtra.Value = false;
            if (Context.IsWorldReady)
                Normalize(Game1.player);
        }

        // ---------------------------------------------------------------- input

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            var player = Game1.player;
            var flashlight = FindFlashlight(player);
            if (flashlight is null)
                return;

            var usesTheTool = e.Button.IsUseToolButton() && ContentSetup.IsFlashlight(player.CurrentTool);
            if (e.Button != _toggleButton && !usesTheTool)
                return;

            Helper.Input.Suppress(e.Button);
            if (_on.Value)
                TurnOff(announce: true);
            else
                TurnOn(player, flashlight);
        }

        // ---------------------------------------------------------------- switching

        private void TurnOn(Farmer player, Tool flashlight)
        {
            var capacity = ChargeModel.CapacityTicks(_config.BatteryHours);
            var step = ChargeModel.TurnOn(ReadCharge(flashlight), CountBatteries(flashlight), capacity);
            if (!step.LightOn)
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.no-battery"), HUDMessage.error_type));
                return;
            }

            if (step.BatteryUsed)
                UseBattery(flashlight);
            WriteCharge(flashlight, step.Charge);
            SetVariant(player, flashlight, on: true);
            _warnedLow.Value = false;
            _flickerTicks.Value = 0;
            _on.Value = true;
            AddLight(player);
            Game1.playSound("woodyStep");
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.on"), HUDMessage.newQuest_type));
        }

        private void TurnOff(bool announce)
        {
            if (!_on.Value)
                return;
            _on.Value = false;
            RemoveLight();
            if (Context.IsWorldReady)
                Normalize(Game1.player);
            if (announce)
            {
                Game1.playSound("woodyStep");
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.off"), HUDMessage.newQuest_type));
            }
        }

        // ---------------------------------------------------------------- time

        /// <summary>Whether the place the farmer is in is dark enough for a light to matter. Daylight outside is not.</summary>
        private static bool NeedsLight(GameLocation? location) =>
            location is not null && (!location.IsOutdoors || Game1.isDarkOut(location));

        /// <summary>Called every 10 in-game minutes.</summary>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !_on.Value)
                return;

            var player = Game1.player;
            var flashlight = FindFlashlight(player);
            if (flashlight is null)
            {
                TurnOff(announce: false); // it was dropped or sold
                return;
            }

            // a battery is not used up by a light nobody needs, such as one left on in the sun
            if (!NeedsLight(player.currentLocation))
                return;

            var capacity = ChargeModel.CapacityTicks(_config.BatteryHours);
            var step = ChargeModel.Tick(ReadCharge(flashlight), CountBatteries(flashlight), capacity);
            if (step.BatteryUsed)
                UseBattery(flashlight);
            WriteCharge(flashlight, step.Charge);

            if (!step.LightOn)
            {
                TurnOff(announce: false);
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.empty"), HUDMessage.error_type));
            }
            else if (step.Low && !_warnedLow.Value)
            {
                _warnedLow.Value = true;
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.low"), HUDMessage.error_type));
            }
        }

        // ---------------------------------------------------------------- the light

        private Texture2D BeamFor(int facing)
        {
            facing = ((facing % 4) + 4) % 4;
            return _beams[facing] ??= Helper.GameContent.Load<Texture2D>(ContentSetup.BeamAssetName(facing));
        }

        private float Radius => _config.BeamLengthTiles * 64f / ConePixels;

        /// <summary>The spot the beam starts from: at the farmer's chest, a little way in the direction faced.</summary>
        private static Vector2 BeamOrigin(Farmer player)
        {
            var forward = player.FacingDirection switch
            {
                0 => new Vector2(0, -16),
                1 => new Vector2(16, 0),
                2 => new Vector2(0, 16),
                _ => new Vector2(-16, 0),
            };
            return new Vector2(player.Position.X + 32f, player.Position.Y - 16f) + forward;
        }

        private void AddLight(Farmer player)
        {
            var location = player.currentLocation;
            if (location is null)
                return;

            RemoveLight();
            var id = $"{ContentSetup.ModId}_{player.UniqueMultiplayerID}";
            var light = new LightSource(id, LightSource.lantern, BeamOrigin(player), Radius, LightColor, LightSource.LightContext.None, player.UniqueMultiplayerID, location.NameOrUniqueName)
            {
                lightTexture = BeamFor(player.FacingDirection),
            };
            location.sharedLights.AddLight(light);
            _lightId.Value = id;
            _lightLocation.Value = location.NameOrUniqueName;
        }

        private void RemoveLight()
        {
            if (_lightId.Value is null)
                return;
            // the light may be in the place the farmer just left
            var location = Game1.getLocationFromName(_lightLocation.Value) ?? Game1.player.currentLocation;
            location?.removeLightSource(_lightId.Value);
            _lightId.Value = null;
            _lightLocation.Value = null;
        }

        private void ForgetLight()
        {
            _on.Value = false;
            _lightId.Value = null;
            _lightLocation.Value = null;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer || !_on.Value)
                return;
            // move the light into the new place
            e.OldLocation?.removeLightSource(_lightId.Value);
            _lightId.Value = null;
            AddLight(e.Player);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !_on.Value || _lightId.Value is null)
                return;

            var player = Game1.player;
            var location = player.currentLocation;
            var light = location?.getLightSource(_lightId.Value);
            if (location is null || light is null)
                return;

            location.repositionLightSource(_lightId.Value, BeamOrigin(player));
            light.lightTexture = BeamFor(player.FacingDirection);

            // Flicker: now and then at random, like an old flashlight with a loose cell, and often when the battery is nearly flat.
            var flashlight = FindFlashlight(player);
            var charge = flashlight is null ? 0 : ReadCharge(flashlight);
            var low = flashlight is not null && charge <= ChargeModel.LowTicks && CountBatteries(flashlight) == 0;
            if (_flickerTicks.Value <= 0 && FlickerModel.ShouldStart(Game1.random, FlickerModel.Interval(_config.FlickerSecondsBetween, low)))
                _flickerTicks.Value = FlickerModel.NewLength(Game1.random);

            var strength = 1f;
            if (_flickerTicks.Value > 0)
            {
                strength = FlickerModel.Strength(_flickerTicks.Value, Game1.random);
                _flickerTicks.Value--;
            }
            light.radius.Value = Radius * strength;
        }
    }
}
