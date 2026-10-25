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

        private const string StatsFile = "stats.json";

        private ModConfig _config = new();
        private UsageStats _stats = new();
        private ContentSetup _content = null!; // set in Entry
        private KeybindList _toggleKeys = KeybindList.Parse("F");

        // Each split-screen player has a flashlight of their own.
        private readonly PerScreen<bool> _on = new();
        private readonly PerScreen<string?> _lightId = new();
        private readonly PerScreen<string?> _lightLocation = new();
        private readonly PerScreen<int> _signalFrom = new();     // the first tick the beam's size is sent in the texture index
        private readonly PerScreen<bool> _warnedLow = new();
        private readonly PerScreen<int> _flickerTicks = new();
        private readonly PerScreen<Tool?> _tool = new();        // the flashlight in use, found again whenever the backpack changes
        private readonly PerScreen<float> _reserve = new();     // the share of a full load left, refreshed twice a second
        private int _lastSpareNotice;                           // the tick of the last "one flashlight" message, so clicking a spare does not fill the screen

        private readonly Texture2D?[] _beams = new Texture2D?[4];
        private readonly BeamMask _mask = new();
        private RemoteBeams _remote = null!; // set in Entry

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
            LoadStats();
            helper.ConsoleCommands.Add("flashlight_report", "Prints a block of text about your settings and how the flashlight has been used, to paste into a bug report or feedback. Nothing is sent anywhere.", (_, _) => Monitor.Log(Report(), LogLevel.Info));

            _content = new ContentSetup(helper.Translation, () => _config);
            _remote = new RemoteBeams(BeamFor, () => _config.BeamShadows, message => Monitor.Log(message, LogLevel.Error));
            FlashlightPatches.Apply(Monitor);
            FlashlightPatches.SpareRefused = () => Notice("message.one-flashlight");
            FlashlightPatches.SecondRefused = () => Notice("message.carry-one");
            FlashlightPatches.MismatchRefused = () => Notice("message.match");
            FlashlightPatches.ChargeText = fraction => helper.Translation.Get("tooltip.charge", new { percent = (int)Math.Round(fraction * 100) });
            FlashlightVisuals.ToolsLoader = () => helper.GameContent.Load<Texture2D>(ContentSetup.ToolsTexture);
            HeldFlashlight.HeldLoader = () => helper.GameContent.Load<Texture2D>(ContentSetup.HeldTexture);
            HeldFlashlight.SheetLoader = male => helper.GameContent.Load<Texture2D>(male ? "Characters/Farmer/farmer_base" : "Characters/Farmer/farmer_girl_base");
            HeldFlashlight.Enabled = () => _config.ShowInHand;
            HeldFlashlight.OnError = reason => Monitor.Log("Could not draw the flashlight in the hand, so it will not be drawn there: " + reason, LogLevel.Error);
            FlashlightVisuals.OnError = reason => Monitor.Log("Could not load a picture for the flashlight, so it will be drawn plainly: " + reason, LogLevel.Error);

            helper.Events.GameLoop.GameLaunched += (_, _) => ConfigMenu.Setup(this, helper, () => _config, OnConfigSaved);
            helper.Events.Content.AssetRequested += (_, e) => _content.OnAssetRequested(e);
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            helper.Events.Content.LocaleChanged += (_, _) => InvalidateContent();
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.UpdateTicked += (_, _) => _remote.Update();
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.SaveLoaded += (_, _) => Reset();
            helper.Events.GameLoop.DayStarted += (_, _) => Reset();
            helper.Events.GameLoop.DayEnding += (_, _) =>
            {
                TurnOff(announce: false);
                _stats.DayEnded();
                SaveStats();
            };
            helper.Events.GameLoop.Saving += (_, _) => SaveStats();
            helper.Events.GameLoop.ReturnedToTitle += (_, _) =>
            {
                ForgetLight();
                _mask.Forget(); // lets go of the place it last looked at
                _remote.Forget();
                SaveStats();
            };
            helper.Events.Multiplayer.PeerDisconnected += (_, e) => DropLights(e.Peer.PlayerID);
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

            FlashlightState.Capacities = new CellCapacities(ChargeModel.CapacityTicks(_config.BatteryHours), ChargeModel.CapacityTicks(_config.BatteryPackHours));
            FlashlightState.PacksAllowed = _config.UseBatteryPacks;
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
            _mask.Forget();
            _remote.Forget();
        }

        private void InvalidateContent()
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Tools");
            Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
            Helper.GameContent.InvalidateCache("Data/Shops");
            Helper.GameContent.InvalidateCache("Data/Mail");
            Helper.GameContent.InvalidateCache("Data/TriggerActions");
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var name in e.NamesWithoutLocale)
            {
                if (name.StartsWith("Characters/Farmer/farmer", true))
                    HeldFlashlight.ForgetTextures();
                if (name.StartsWith("Mods/" + ContentSetup.ModId + "/", true))
                {
                    FlashlightVisuals.ForgetTextures();
                    HeldFlashlight.ForgetTextures();
                    Array.Clear(_beams);
                    _mask.Forget();
                    _remote.Forget();
                    return;
                }
            }
        }

        // ---------------------------------------------------------------- feedback

        private void LoadStats()
        {
            try
            {
                _stats = Helper.Data.ReadJsonFile<UsageStats>(StatsFile) ?? new UsageStats();
            }
            catch (Exception ex)
            {
                // the tally is only for feedback, so a file that cannot be read is started over and never stops the mod
                Monitor.Log($"Could not read {StatsFile}, so the usage tally starts again. ({ex.Message})", LogLevel.Debug);
                _stats = new UsageStats();
            }
        }

        private void SaveStats()
        {
            try
            {
                Helper.Data.WriteJsonFile(StatsFile, _stats);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Could not save {StatsFile}. ({ex.Message})", LogLevel.Debug);
            }
        }

        /// <summary>The block of text printed by the flashlight_report command.</summary>
        private string Report()
        {
            var environment = $"SMAPI {Constants.ApiVersion}, Stardew Valley {Game1.version}, {Constants.TargetPlatform}";
            var flashlight = "no save loaded";
            if (Context.IsWorldReady)
            {
                var tool = FlashlightState.ActiveIn(Game1.player);
                if (tool is null)
                    flashlight = "not carried";
                else
                {
                    Span<CellKind> slots = stackalloc CellKind[FlashlightState.MaxSlots];
                    var count = FlashlightState.ReadSlots(tool, slots);
                    var loaded = string.Join(", ", slots[..count].ToArray().Select(k => k == CellKind.None ? "-" : k.ToString()));
                    flashlight = $"{FlashlightState.Reserve(tool) * 100:0}% of a full load, charge {FlashlightState.ReadCharge(tool)} ticks ({FlashlightState.ReadInUse(tool)}), slots [{loaded}]";
                }
            }
            return ReportText.Build(ModManifest.Version.ToString(), environment, _config, _stats, flashlight);
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
            // while the light is on it belongs to the lit flashlight, and if that was dropped or sold the light goes out
            if (_on.Value && chosen is not null && !FlashlightState.IsLit(chosen))
                chosen = null;
            _tool.Value = chosen;
            return chosen;
        }

        /// <summary>Shows a message about the one-flashlight rule, at most once every few seconds so a pickup that keeps being refused does not fill the screen.</summary>
        private void Notice(string key)
        {
            if (Game1.ticks - _lastSpareNotice < 180 && _lastSpareNotice > 0)
                return;
            _lastSpareNotice = Game1.ticks;
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get(key), HUDMessage.error_type));
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
                // a flashlight always has exactly two slots, whatever an earlier build gave it
                if (tool.AttachmentSlotsCount != FlashlightState.Slots)
                    tool.AttachmentSlotsCount = FlashlightState.Slots;
                // only the flashlight in use can be lit, and only while the light is on
                if (FlashlightState.IsLit(tool) && !(_on.Value && ReferenceEquals(tool, _tool.Value)))
                    FlashlightState.SetLit(tool, false);
            }
        }

        /// <summary>
        /// Takes a player's beam out of every place. The game removes the light of a ring when its player leaves, but not one added
        /// by a mod, so a player who dropped out with the flashlight on would leave a beam behind that shows again when they rejoin.
        /// </summary>
        private static void DropLights(long playerId)
        {
            var id = $"{ContentSetup.ModId}_{playerId}";
            Utility.ForEachLocation(location =>
            {
                location.removeLightSource(id);
                return true;
            });
        }

        private void Reset()
        {
            _on.Value = false;
            _flickerTicks.Value = 0;
            _warnedLow.Value = false;
            if (!Context.IsWorldReady)
                return;
            DropLights(Game1.player.UniqueMultiplayerID); // a beam left from before this player dropped out
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
                Notice("message.one-flashlight"); // a spare flashlight in the hand does nothing
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
            var step = FlashlightState.TurnOn(flashlight);
            if (!step.LightOn)
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.no-battery"), HUDMessage.error_type));
                return;
            }

            FlashlightState.Commit(flashlight, step);
            if (step.CellUsed)
                _stats.PairUsed(step.InUse);
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

            var step = FlashlightState.Tick(flashlight);
            FlashlightState.Commit(flashlight, step);
            if (step.LightOn)
                _stats.Tick();
            if (step.CellUsed)
                _stats.PairUsed(step.InUse);
            _reserve.Value = FlashlightState.Reserve(flashlight);

            if (!step.LightOn)
            {
                TurnOff(announce: false);
                _stats.RanOut++;
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

        /// <summary>
        /// The picture to draw the beam with: the cone, with the shadows of whatever stands in its way cut out of it when that is on
        /// in the settings. The shadows are worked out for the radius the light is drawn with, so this must follow the flicker.
        /// </summary>
        private Texture2D TextureFor(Farmer player, GameLocation location, Vector2 origin, float radius)
        {
            var cone = BeamFor(player.FacingDirection);
            if (!_config.BeamShadows || radius <= 0f)
                return cone;
            try
            {
                return _mask.Build(Game1.graphics.GraphicsDevice, cone, player.FacingDirection, origin, radius, location, player) ?? cone;
            }
            catch (Exception ex)
            {
                // a shadow that cannot be worked out must never stop the light, so it falls back to the plain cone and says so once
                _config.BeamShadows = false;
                Monitor.Log($"Could not work out the beam's shadows, so the beam is drawn without them until the game is restarted. ({ex.Message})", LogLevel.Error);
                return cone;
            }
        }

        private float Radius => _config.BeamLengthTiles * 64f / ConePixels;

        /// <summary>How much brighter than a Dry Cell the beam is with the given share of a Battery Pack's boost (0 to 1).</summary>
        private float Brightness(float boost) => 1f + boost * (_config.BatteryPackBeamPercent / 100f - 1f);

        /// <summary>A Battery Pack gives a longer beam and takes more of the darkness away, a little more than the length alone.</summary>
        private Color ColorFor(float boost)
        {
            var f = 1f + (Brightness(boost) - 1f) * 1.5f;
            return new Color(LightColor.R, (int)Math.Min(255f, LightColor.G * f), (int)Math.Min(255f, LightColor.B * f));
        }

        /// <summary>The spot the beam starts from: the lens of the flashlight in the farmer's hand, whichever way they face.</summary>
        private static Vector2 BeamOrigin(Farmer player)
        {
            // when the flashlight is drawn in the hand, the beam starts at the lens that was drawn, so it follows the hand
            if (HeldFlashlight.TryGetLens(out var lens))
                return lens;

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
            var boost = _tool.Value is { } flashlight ? FlashlightState.Boost(flashlight) : 0f;
            var origin = BeamOrigin(player);
            var radius = Radius * Brightness(boost);
            var light = new LightSource(id, LightSource.lantern, origin, radius, ColorFor(boost), LightSource.LightContext.None, player.UniqueMultiplayerID, location.NameOrUniqueName)
            {
                lightTexture = TextureFor(player, location, origin, radius),
            };
            location.sharedLights.AddLight(light);
            _lightId.Value = id;
            _lightLocation.Value = location.NameOrUniqueName;
            // the light is first sent as the game's lantern, so a player without this mod always has a picture to draw
            _signalFrom.Value = Game1.ticks + 1;
        }

        /// <summary>Tells the other players the true size of the beam, which the game would hold back while it flickers.</summary>
        private void Signal(LightSource light)
        {
            if (Context.HasRemotePlayers && Game1.ticks >= _signalFrom.Value)
                light.textureIndex.Value = BeamSignal.Encode(light.radius.Value);
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
            if (e.IsLocalPlayer)
                _remote.Forget(); // the masks belong to the place that was left
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

            var origin = BeamOrigin(player);
            location.repositionLightSource(_lightId.Value, origin);

            // no beam during a cutscene or a festival, where the farmer is moved by the script
            if (Game1.eventUp)
            {
                light.lightTexture = BeamFor(player.FacingDirection);
                light.radius.Value = 0f;
                Signal(light);
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
            var boost = _tool.Value is { } held ? FlashlightState.Boost(held) : 0f;
            light.color.Value = ColorFor(boost);
            light.radius.Value = Radius * Brightness(boost) * strength;
            light.lightTexture = TextureFor(player, location, origin, light.radius.Value);
            Signal(light);
        }
    }
}
