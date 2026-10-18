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

        // Each split-screen player has a flashlight of their own.
        private readonly PerScreen<bool> _on = new();
        private readonly PerScreen<string?> _lightId = new();
        private readonly PerScreen<string?> _lightLocation = new();
        private readonly PerScreen<bool> _warnedLow = new();

        private readonly Texture2D?[] _beams = new Texture2D?[4];

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            _config.Clamp();
            helper.WriteConfig(_config);
            ApplyConfig();

            _content = new ContentSetup(helper.Translation, () => _config);

            helper.Events.Content.AssetRequested += (_, e) => _content.OnAssetRequested(e);
            helper.Events.Content.LocaleChanged += (_, _) => InvalidateContent();
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.DayEnding += (_, _) => TurnOff(announce: false);
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => ForgetLight();
            helper.Events.Player.Warped += OnWarped;
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

        private static Tool? FindFlashlight(Farmer player) =>
            player.Items.OfType<Tool>().FirstOrDefault(tool => tool.ItemId == ContentSetup.FlashlightId);

        private static int CountBatteries(Farmer player) =>
            player.Items.Where(item => item?.QualifiedItemId == ContentSetup.QualifiedBatteryId).Sum(item => item.Stack);

        private static void UseBattery(Farmer player)
        {
            var battery = player.Items.FirstOrDefault(item => item?.QualifiedItemId == ContentSetup.QualifiedBatteryId);
            if (battery is null)
                return;
            battery.Stack--;
            if (battery.Stack <= 0)
                player.removeItemFromInventory(battery);
        }

        private static int ReadCharge(Tool tool) =>
            tool.modData.TryGetValue(ChargeKey, out var text) && int.TryParse(text, out var charge) ? Math.Max(0, charge) : 0;

        private static void WriteCharge(Tool tool, int charge) => tool.modData[ChargeKey] = charge.ToString();

        // ---------------------------------------------------------------- input

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            var player = Game1.player;
            var flashlight = FindFlashlight(player);
            if (flashlight is null)
                return;

            var usesTheTool = e.Button.IsUseToolButton() && player.CurrentTool?.ItemId == ContentSetup.FlashlightId;
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
            var step = ChargeModel.TurnOn(ReadCharge(flashlight), CountBatteries(player), capacity);
            if (!step.LightOn)
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.no-battery"), HUDMessage.error_type));
                return;
            }

            if (step.BatteryUsed)
                UseBattery(player);
            WriteCharge(flashlight, step.Charge);
            _warnedLow.Value = false;
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
            if (announce)
            {
                Game1.playSound("woodyStep");
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.off"), HUDMessage.newQuest_type));
            }
        }

        // ---------------------------------------------------------------- time

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

            var capacity = ChargeModel.CapacityTicks(_config.BatteryHours);
            var step = ChargeModel.Tick(ReadCharge(flashlight), CountBatteries(player), capacity);
            if (step.BatteryUsed)
                UseBattery(player);
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

            // a nearly empty battery makes the beam flicker
            var flashlight = FindFlashlight(player);
            var charge = flashlight is null ? 0 : ReadCharge(flashlight);
            var low = charge <= ChargeModel.LowTicks && CountBatteries(player) == 0;
            light.radius.Value = low && Game1.random.NextDouble() < 0.12 ? Radius * 0.6f : Radius;
        }
    }
}
