using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Flashlight
{
    /// <summary>The charge bar under the flashlight's picture and its lit picture. Called every frame.</summary>
    internal static class FlashlightVisuals
    {
        private const int BarFullWidth = 48;

        private static readonly Color Green = new(88, 190, 60);
        private static readonly Color Yellow = new(240, 200, 40);
        private static readonly Color Orange = new(240, 130, 30);
        private static readonly Color Red = new(220, 50, 40);

        private static readonly Rectangle BarFrame = new(297, 420, 14, 5); // the frame the watering can uses, in the game's cursors picture

        /// <summary>Loads the tool sheet. Set by the mod; cleared when an asset changes so it is fetched again.</summary>
        public static Func<Texture2D?>? ToolsLoader { get; set; }

        /// <summary>Called once with the reason when a picture cannot be loaded, after which nothing more is drawn until the assets change.</summary>
        public static Action<string>? OnError { get; set; }

        private static Texture2D? _tools;
        private static bool _failed;

        public static void ForgetTextures()
        {
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
    }
}
