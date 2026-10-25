using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Gives the beams of the other players their cone. A light is shared with everyone in the place, but its picture is not: it
    /// travels as the plain round glow of a lantern. The position, size, colour and owner of the light do travel, and so does the
    /// way its owner faces, so each machine draws the cone itself for every other flashlight it can see. A player without this mod
    /// keeps seeing the round glow, which is harmless.
    /// </summary>
    internal sealed class RemoteBeams
    {
        private const string LightPrefix = ContentSetup.ModId + "_";

        private readonly Func<int, Texture2D> _beamFor;
        private readonly Func<bool> _shadows;
        private readonly Action<string> _onError;

        /// <summary>One mask for each other player, because a mask remembers the picture it last made.</summary>
        private readonly Dictionary<long, BeamMask> _masks = new();

        private bool _failed;

        public RemoteBeams(Func<int, Texture2D> beamFor, Func<bool> shadows, Action<string> onError)
        {
            _beamFor = beamFor;
            _shadows = shadows;
            _onError = onError;
        }

        public void Forget()
        {
            _masks.Clear();
        }

        /// <summary>Called every update. Does nothing unless there is another player in the game.</summary>
        public void Update()
        {
            if (_failed || !Context.IsWorldReady || !Context.HasRemotePlayers)
                return;
            var location = Game1.currentLocation;
            if (location is null)
                return;

            var local = Game1.player.UniqueMultiplayerID;
            foreach (var light in location.sharedLights.Values)
            {
                // only the flashlights of other players: the owner draws their own
                if (light.PlayerID == 0 || light.PlayerID == local || light.Id is not { } id || !id.StartsWith(LightPrefix, StringComparison.Ordinal))
                    continue;
                if (Game1.GetPlayer(light.PlayerID) is not { } owner || !ReferenceEquals(owner.currentLocation, location))
                    continue;
                light.lightTexture = TextureFor(owner, location, light);
            }
        }

        private Texture2D TextureFor(Farmer owner, GameLocation location, LightSource light)
        {
            var facing = owner.FacingDirection;
            var cone = _beamFor(facing);

            // the size the game draws the picture at: the radius it sent, which arrives late while the beam flickers
            var drawn = light.radius.Value;
            if (drawn <= 0f || _failed)
                return cone;
            // the size the beam really is right now, which the owner sends in the texture index, or the plain radius from a player
            // whose copy of the mod does not send it
            var shown = BeamSignal.TryDecode(light.textureIndex.Value, out var signalled) ? signalled : drawn;
            try
            {
                if (!_masks.TryGetValue(owner.UniqueMultiplayerID, out var mask))
                    _masks[owner.UniqueMultiplayerID] = mask = new BeamMask();
                return mask.Build(Game1.graphics.GraphicsDevice, cone, facing, light.position.Value, drawn, location, owner, shown, _shadows()) ?? cone;
            }
            catch (Exception ex)
            {
                _failed = true; // one message, not one every frame
                _onError($"Could not draw the beam of another player, so those beams are drawn as a plain cone until the game is restarted. ({ex.Message})");
                return cone;
            }
        }
    }
}
