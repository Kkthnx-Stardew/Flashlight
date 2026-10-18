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
        // The beam pictures are 128 pixels wide and the cone ends 62 pixels from the centre. A picture pixel is drawn
        // "radius" world pixels wide, so a beam of N tiles (64 pixels each) needs a radius of N * 64 / 62.
        private const float ConePixels = 62f;

        // The colour is taken away from the darkness, so a warm light removes mostly blue.
        private static readonly Color LightColor = new(0, 40, 140);

        private ModConfig _config = new();
        private ContentSetup _content = null!; // set in Entry
        private KeybindList _toggleKeys = KeybindList.Parse("F");

        // Each split-screen player has a flashlight of their own.
        private readonly PerScreen<bool> _on = new();
        private readonly PerScreen<string?> _lightId = new();
        private readonly PerScreen<string?> _lightLocation = new();
        private readonly PerScreen<bool> _warnedLow = new();
        private readonly PerScreen<int> _flickerTicks = new();
        private readonly PerScreen<Tool?> _tool = new();        // the flashlight in use, found again whenever the backpack changes
        private readonly PerScreen<float> _reserve = new();     // the share of a full load left, refreshed twice a second
        private int _lastSpareNotice;                           // the tick of the last "one flashlight" message, so clicking a spare does not fill the screen

        private readonly Texture2D?[] _beams = new Texture2D?[4];

        public override void Entry(IModHelper helper)
        {
            var readable = true;
            try
            {
                _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            }
            catch (Exception ex)
            {
                // a config.json that cannot be read (a typo, text where a number belongs) must not stop the mod from loading
                readable = false;
                _config = new ModConfig();
                Monitor.Log($"Could not read config.json, so the default settings are used and the file is left as it is. Fix it or delete it. ({ex.Message})", LogLevel.Warn);
            }
            ApplyConfig(save: readable);

            _content = new ContentSetup(helper.Translation, () => _config);
            FlashlightPatches.Apply(Monitor);
            FlashlightPatches.SpareRefused = NoticeSpare;
            FlashlightPatches.ChargeText = fraction => helper.Translation.Get("tooltip.charge", new { percent = (int)Math.Round(fraction * 100) });
            FlashlightVisuals.ToolsLoader = () => helper.GameContent.Load<Texture2D>(ContentSetup.ToolsTexture);
            FlashlightVisuals.OnError = reason => Monitor.Log("Could not load a picture for the flashlight, so it will be drawn plainly: " + reason, LogLevel.Error);

            helper.Events.GameLoop.GameLaunched += (_, _) => ConfigMenu.Setup(this, helper, () => _config, OnConfigSaved);
            helper.Events.Content.AssetRequested += (_, e) => _content.OnAssetRequested(e);
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            helper.Events.Content.LocaleChanged += (_, _) => InvalidateContent();
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
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
                {
                    RefreshTool(e.Player);
                    Normalize(e.Player);
                }
            };
        }

        // ---------------------------------------------------------------- settings

        private void ApplyConfig(bool save)
        {
            _config.Clamp();
            if (save)
                Helper.WriteConfig(_config);

            FlashlightState.Capacity = ChargeModel.CapacityTicks(_config.BatteryHours);
            if (!KeybindList.TryParse(_config.ToggleKey, out var keys, out var errors))
            {
                Monitor.Log($"The toggle key '{_config.ToggleKey}' is not valid ({string.Join("; ", errors)}). Using F.", LogLevel.Warn);
                keys = KeybindList.Parse("F");
            }
            _toggleKeys = keys;
        }

        /// <summary>Called when the settings are saved in the config menu.</summary>
        private void OnConfigSaved()
        {
            ApplyConfig(save: true);
            InvalidateContent();
            Array.Clear(_beams);
        }

        private void InvalidateContent()
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Tools");
            Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
            Helper.GameContent.InvalidateCache("Data/Shops");
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var name in e.NamesWithoutLocale)
            {
                if (name.StartsWith("Mods/" + ContentSetup.ModId + "/", true))
                {
                    FlashlightVisuals.ForgetTextures();
                    Array.Clear(_beams);
                    return;
                }
            }
        }

        // ---------------------------------------------------------------- finding the flashlight

        /// <summary>
        /// Finds the flashlight to use: the first one in the backpack. A player is meant to have only one, and the shop sells one
        /// to a player who has none, but others can turn up (found, or given by another mod). Those are inert spares: they take no
        /// cells and cannot be lit, so owning several can never store extra light or give a second beam.
        /// Runs when the backpack changes or a key is pressed, not every update.
        /// </summary>
        private Tool? RefreshTool(Farmer player)
        {
            var chosen = FlashlightState.ActiveIn(player);
            // while the light is on it belongs to the lit flashlight; if that was dropped or sold the light goes out
            if (_on.Value && chosen is not null && !FlashlightState.IsLit(chosen))
                chosen = null;
            _tool.Value = chosen;
            return chosen;
        }

        /// <summary>Tells the player a spare flashlight does nothing, at most once every few seconds.</summary>
        private void NoticeSpare()
        {
            if (Game1.ticks - _lastSpareNotice < 180 && _lastSpareNotice > 0)
                return;
            _lastSpareNotice = Game1.ticks;
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.one-flashlight"), HUDMessage.error_type));
        }

        // ---------------------------------------------------------------- keeping every flashlight in order

        /// <summary>
        /// Puts every flashlight in the backpack in the state it should be in: unlit unless the light is on (the lit flag is saved
        /// with the item, so one can be found lit after a save or in a chest), and with the slots the settings ask for.
        /// </summary>
        private void Normalize(Farmer player)
        {
            var items = player.Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is not Tool tool || !FlashlightState.IsFlashlight(tool))
                    continue;
                // a flashlight made before the slot count was raised in the settings gets the extra slots
                if (tool.AttachmentSlotsCount < _config.BatterySlots)
                    tool.AttachmentSlotsCount = _config.BatterySlots;
                // only the flashlight in use can be lit, and only while the light is on
                if (FlashlightState.IsLit(tool) && !(_on.Value && ReferenceEquals(tool, _tool.Value)))
                    FlashlightState.SetLit(tool, false);
            }
        }

        private void Reset()
        {
            _on.Value = false;
            _flickerTicks.Value = 0;
            _warnedLow.Value = false;
            if (!Context.IsWorldReady)
                return;
            RefreshTool(Game1.player);
            Normalize(Game1.player);
        }

        // ---------------------------------------------------------------- input

        /// <summary>Whether a key or click is for the flashlight: the world is up, the farmer is free to move, and no one is typing in the chat or a text box.</summary>
        private static bool CanAct() => Context.IsWorldReady && Context.IsPlayerFree && Game1.keyboardDispatcher?.Subscriber is null;

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!_toggleKeys.JustPressed() || !CanAct())
                return;
            if (RefreshTool(Game1.player) is null)
                return;

            Helper.Input.SuppressActiveKeybinds(_toggleKeys);
            Toggle(Game1.player);
        }

        /// <summary>Using the tool (clicking with it selected) switches it too, with no swing.</summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!CanAct() || !e.Button.IsUseToolButton())
                return;
            var player = Game1.player;
            if (!FlashlightState.IsFlashlight(player.CurrentTool))
                return;

            Helper.Input.Suppress(e.Button);
            if (RefreshTool(player) is null)
                return;
            if (!ReferenceEquals(player.CurrentTool, _tool.Value))
                NoticeSpare(); // a spare flashlight in the hand does nothing
            else
                Toggle(player);
        }

        private void Toggle(Farmer player)
        {
            if (_on.Value)
                TurnOff(announce: true);
            else if (_tool.Value is { } flashlight)
                TurnOn(player, flashlight);
        }

        // ---------------------------------------------------------------- switching

        private void TurnOn(Farmer player, Tool flashlight)
        {
            var step = ChargeModel.TurnOn(FlashlightState.ReadCharge(flashlight), FlashlightState.CountCells(flashlight), FlashlightState.Capacity);
            if (!step.LightOn)
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.no-battery"), HUDMessage.error_type));
                return;
            }

            if (step.CellUsed)
                FlashlightState.UseCell(flashlight);
            FlashlightState.WriteCharge(flashlight, step.Charge);
            FlashlightState.SetLit(flashlight, true);
            _reserve.Value = FlashlightState.Reserve(flashlight);
            _warnedLow.Value = _reserve.Value < ChargeBar.LowFraction;
            _flickerTicks.Value = 0;
            _on.Value = true;
            AddLight(player);
            Game1.playSound("smallSelect");
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.on"), HUDMessage.newQuest_type));
        }

        private void TurnOff(bool announce)
        {
            if (!_on.Value)
                return;
            _on.Value = false;
            RemoveLight();
            if (Context.IsWorldReady)
                Normalize(Game1.player); // the flashlight is no longer lit
            if (announce)
            {
                Game1.playSound("toolSwap");
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.off"), HUDMessage.newQuest_type));
            }
        }

        // ---------------------------------------------------------------- time

        /// <summary>Whether the place the farmer is in is dark enough for a light to matter: indoors, in the mines, outside after dark, or in the rain. Daylight outside is not.</summary>
        private static bool NeedsLight(GameLocation? location) =>
            location is not null && (!location.IsOutdoors || Game1.isDarkOut(location) || Game1.IsRainingHere(location));

        /// <summary>Called every 10 in-game minutes.</summary>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !_on.Value)
                return;

            var player = Game1.player;
            var flashlight = _tool.Value ?? RefreshTool(player);
            if (flashlight is null)
            {
                TurnOff(announce: false); // it was dropped or sold
                return;
            }

            // a cell is not used up by a light nobody needs, such as one left on in the sun
            if (!NeedsLight(player.currentLocation))
                return;

            var step = ChargeModel.Tick(FlashlightState.ReadCharge(flashlight), FlashlightState.CountCells(flashlight), FlashlightState.Capacity);
            if (step.CellUsed)
                FlashlightState.UseCell(flashlight);
            FlashlightState.WriteCharge(flashlight, step.Charge);
            _reserve.Value = FlashlightState.Reserve(flashlight);

            if (!step.LightOn)
            {
                TurnOff(announce: false);
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.empty"), HUDMessage.error_type));
            }
            else if (_reserve.Value < ChargeBar.LowFraction && !_warnedLow.Value)
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

        /// <summary>The spot the beam starts from: the lens of the flashlight in the farmer's hand, whichever way they face.</summary>
        private static Vector2 BeamOrigin(Farmer player)
        {
            var from = player.FacingDirection switch
            {
                0 => new Vector2(22, -18),  // up: past the shoulder
                1 => new Vector2(66, -12),  // right: just ahead of the farmer
                2 => new Vector2(48, 10),   // down: in front of the hip
                _ => new Vector2(-2, -12),  // left
            };
            return player.Position + from;
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
            _tool.Value = null;
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
            if (!_on.Value || _lightId.Value is null || !Context.IsWorldReady)
                return;

            var player = Game1.player;
            var location = player.currentLocation;
            var light = location?.getLightSource(_lightId.Value);
            if (location is null || light is null)
                return;

            // twice a second: check the flashlight is still in the backpack and refresh the reserve, which loading cells changes
            if (e.IsMultipleOf(30))
            {
                var flashlight = RefreshTool(player);
                if (flashlight is null)
                {
                    TurnOff(announce: false);
                    return;
                }
                _reserve.Value = FlashlightState.Reserve(flashlight);
                if (_reserve.Value >= ChargeBar.LowFraction)
                    _warnedLow.Value = false;
            }

            location.repositionLightSource(_lightId.Value, BeamOrigin(player));
            light.lightTexture = BeamFor(player.FacingDirection);

            // no beam during a cutscene or a festival, where the farmer is moved by the script
            if (Game1.eventUp)
            {
                light.radius.Value = 0f;
                return;
            }

            // Flicker: now and then at random, like an old flashlight with a loose cell, and more and more as the reserve runs out.
            if (_flickerTicks.Value <= 0 && FlickerModel.ShouldStart(Game1.random, FlickerModel.Interval(_config.FlickerSecondsBetween, _reserve.Value)))
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
