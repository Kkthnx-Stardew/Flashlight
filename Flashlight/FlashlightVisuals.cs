using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Flashlight
{
    /// <summary>The charge bar under the flashlight's picture and the flashlight in the farmer's hand. Called every frame.</summary>
    internal static class FlashlightVisuals
    {
        private const int HeldCell = 12;                    // the held pictures are 12 by 12 pixels
        private const float Zoom = 4f;                      // the game draws world pictures four times as large
        private const int BarFullWidth = 48;

        private static readonly Color Green = new(88, 190, 60);
        private static readonly Color Yellow = new(240, 200, 40);
        private static readonly Color Orange = new(240, 130, 30);
        private static readonly Color Red = new(220, 50, 40);

        private static readonly Rectangle BarFrame = new(297, 420, 14, 5); // the frame the watering can uses, in the game's cursors picture

        /// <summary>Loads the held pictures and the tool sheet. Set by the mod; cleared when an asset changes so it is fetched again.</summary>
        public static Func<Texture2D?>? HeldLoader { get; set; }

        public static Func<Texture2D?>? ToolsLoader { get; set; }

        /// <summary>Called once with the reason when a picture cannot be loaded, after which nothing more is drawn until the assets change.</summary>
        public static Action<string>? OnError { get; set; }

        private static Texture2D? _held;
        private static Texture2D? _tools;
        private static bool _failed;

        public static void ForgetTextures()
        {
            _held = null;
            _tools = null;
            _failed = false;
        }

        private static Texture2D? Get(ref Texture2D? cache, Func<Texture2D?>? loader)
        {
            if (cache is not null || _failed || loader is null)
                return cache;
            try
            {
                cache = loader();
            }
            catch (Exception ex)
            {
                _failed = true; // one message, not one every frame
                OnError?.Invoke(ex.Message);
            }
            return cache;
        }

        // ---------------------------------------------------------------- the charge bar

        /// <summary>
        /// Draws the bar the way the watering can draws its water: a frame under the picture and a coloured fill inside it.
        /// <paramref name="location"/> is where the picture's slot starts, before the picture was moved up to make room.
        /// </summary>
        public static void DrawBar(SpriteBatch spriteBatch, Vector2 location, float transparency, float layerDepth, Tool flashlight)
        {
            var fraction = FlashlightState.Reserve(flashlight);
            spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(4f, 44f), BarFrame, Color.White * transparency, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth + 0.0001f);

            var width = ChargeBar.FilledWidth(fraction, BarFullWidth);
            if (width <= 0)
                return;
            var color = ChargeBar.StageFor(fraction) switch
            {
                ChargeStage.Green => Green,
                ChargeStage.Yellow => Yellow,
                ChargeStage.Orange => Orange,
                _ => Red,
            };
            spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)location.X + 8, (int)location.Y + 64 - 16, width, 8), color * 0.9f * transparency);
        }

        // ---------------------------------------------------------------- the lit picture

        /// <summary>
        /// Draws the lit picture of the flashlight over the unlit one the game has just drawn. Both fill the same cell and the lit one
        /// only changes the lens and adds rays, so it covers the unlit one completely.
        /// </summary>
        public static void DrawLitIcon(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, Color color)
        {
            var tools = Get(ref _tools, ToolsLoader);
            if (tools is null)
                return;
            spriteBatch.Draw(tools, location + new Vector2(32f, 32f), new Rectangle(16, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth + 0.00005f);
        }

        // ---------------------------------------------------------------- in the hand

        /// <summary>
        /// Draws the flashlight in the hand of a farmer who has it selected. It is drawn in the same pass as the farmer, with a
        /// depth just in front of them (or just behind when they face away), so trees and roofs still cover it correctly.
        /// </summary>
        public static void DrawHeld(SpriteBatch b, Farmer who)
        {
            if (!FlashlightState.IsFlashlight(who.CurrentTool))
                return;
            if (who.hidden.Value || Game1.eventUp || who.UsingTool || who.isRidingHorse() || who.IsSitting() || who.currentLocation != Game1.currentLocation)
                return;

            var held = Get(ref _held, HeldLoader);
            if (held is null)
                return;

            // which picture, where, and which way up
            var lit = FlashlightState.IsLit(who.CurrentTool) ? 1 : 0;
            int view;
            Vector2 offset;
            var effects = SpriteEffects.None;
            var layer = who.getDrawLayer();
            switch (who.FacingDirection)
            {
                case 1: // right
                    view = 0; offset = new Vector2(46f, -12f); layer += 0.0004f; break;
                case 3: // left
                    view = 0; offset = new Vector2(18f, -12f); effects = SpriteEffects.FlipHorizontally; layer += 0.0004f; break;
                case 2: // down: the lens faces the viewer
                    view = 1; offset = new Vector2(48f, 0f); layer += 0.0004f; break;
                default: // up: the end cap faces the viewer, behind the body
                    view = 2; offset = new Vector2(16f, -4f); layer -= 0.0004f; break;
            }

            // follow the walking bob of the farmer's sprite
            var frame = who.FarmerSprite.CurrentFrame;
            var bob = frame >= 0 && frame < FarmerRenderer.featureYOffsetPerFrame.Length ? FarmerRenderer.featureYOffsetPerFrame[frame] * 4f : 0f;

            var position = who.getLocalPosition(Game1.viewport) + who.jitter + new Vector2(0f, who.yJumpOffset + bob) + offset;
            var source = new Rectangle((view * 2 + lit) * HeldCell, 0, HeldCell, HeldCell);
            b.Draw(held, position, source, Color.White, 0f, new Vector2(HeldCell / 2f, HeldCell / 2f), Zoom, effects, layer);
        }
    }
}
