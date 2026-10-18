using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Draws the flashlight in the hand of a farmer who has it selected. The game draws the arm as its own picture and moves it
    /// from frame to frame, so the hand is found in that picture (once per frame, then remembered) and the flashlight is drawn
    /// there, just under the arm so the fingers close over the handle. Called from a patch on the farmer's drawing.
    /// </summary>
    internal static class HeldFlashlight
    {
        private const int HeldCell = 12;     // the held pictures are 12 by 12 pixels
        private const int ArmWidth = 16;     // one cell of the farmer sheet
        private const int ArmHeight = 32;

        /// <summary>Loads the held pictures. Set by the mod.</summary>
        public static Func<Texture2D?>? HeldLoader { get; set; }

        /// <summary>Loads the farmer sheet for a farmer, by gender. Set by the mod.</summary>
        public static Func<bool, Texture2D?>? SheetLoader { get; set; }

        /// <summary>Whether to draw the flashlight in the hand at all. Set by the mod from the settings.</summary>
        public static Func<bool>? Enabled { get; set; }

        /// <summary>Called once with the reason when a picture cannot be loaded, after which nothing more is drawn until the assets change.</summary>
        public static Action<string>? OnError { get; set; }

        private static Vector2 _lensWorld;
        private static int _lensTick = -100;

        /// <summary>
        /// Where the lens of the flashlight in the local farmer's hand was drawn last, in world pixels, so the beam can start there.
        /// False when it was not drawn in the last few updates (it is hidden, off in the settings, or the farmer is doing something else).
        /// </summary>
        public static bool TryGetLens(out Vector2 world)
        {
            world = _lensWorld;
            return Game1.ticks - _lensTick <= 3;
        }

        private static Texture2D? _held;
        private static readonly Texture2D?[] Sheets = new Texture2D?[2];
        private static bool _failed;
        private static readonly Dictionary<int, HandLocator.Hand?> Hands = new();
        private static readonly Color[] Pixels = new Color[ArmWidth * ArmHeight];
        private static readonly byte[] Alpha = new byte[ArmWidth * ArmHeight];

        public static void ForgetTextures()
        {
            _held = null;
            Array.Clear(Sheets);
            Hands.Clear();
            _failed = false;
        }

        /// <summary>Whether this farmer should be drawn with the flashlight: it is the selected item, and they are not doing something that fills the hand.</summary>
        private static bool ShouldShow(Farmer who) =>
            FlashlightState.IsFlashlight(who.CurrentTool)
            && !who.UsingTool && !who.hidden.Value && !who.swimming.Value && !who.isRidingHorse() && !who.IsSitting() && !who.isInBed.Value
            && !who.FarmerSprite.PauseForSingleAnimation && !Game1.eventUp;

        /// <summary>
        /// Called after the game has drawn a farmer. <paramref name="position"/>, <paramref name="origin"/> and the frame are the
        /// ones the game used, so the arm can be found where it was just drawn.
        /// </summary>
        public static void Draw(SpriteBatch b, Farmer who, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float scale)
        {
            // the glow and shadow passes draw the farmer again in another colour, and menus draw them small: neither gets a flashlight
            if (FarmerRenderer.isDrawingForUI || overrideColor != Color.White || Enabled?.Invoke() == false || animationFrame.armOffset <= 0 || !ShouldShow(who))
                return;

            var held = Get(ref _held, HeldLoader);
            var sheet = SheetFor(who.IsMale);
            if (held is null || sheet is null)
                return;

            var cellX = currentFrame * 16 % 96 + animationFrame.armOffset * 16;
            var cellY = currentFrame * 16 / 96 * 32;
            // the front and back views draw both arms in one cell: hold it in the farmer's right hand, which is the left of the
            // picture when they face the viewer and the right of it when they face away. The side views draw one arm.
            var side = facingDirection switch { 2 => 1, 0 => 2, _ => 0 };
            if (animationFrame.flip && side != 0)
                side = 3 - side; // a turned-round picture has its halves swapped
            if (FindHand(sheet, who.IsMale, cellX, cellY, side) is not { } hand)
                return;

            // where the arm picture was drawn: its top left is the point the game gave minus the origin the picture turns around
            var drawScale = 4f * scale;
            var anchor = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y)) + origin
                + new Vector2(animationFrame.xOffset * 4, animationFrame.positionOffset * 4) + who.armOffset;
            var topLeft = anchor - origin * drawScale;
            var handAt = topLeft + new Vector2(animationFrame.flip ? ArmWidth - hand.X : hand.X, hand.Y) * drawScale;

            View(facingDirection, lit: FlashlightState.IsLit(who.CurrentTool), out var cell, out var grip, out var effects, out var lens);

            var layer = facingDirection == 0 ? FarmerRenderer.FarmerSpriteLayers.ArmsUp : FarmerRenderer.FarmerSpriteLayers.Arms;
            var step = Game1.isUsingBackToFrontSorting ? -1f : 1f;
            var depth = FarmerRenderer.GetLayerDepth(layerDepth, layer) - 0.5e-6f * step;
            b.Draw(held, handAt, new Rectangle(cell * HeldCell, 0, HeldCell, HeldCell), Color.White, 0f, grip, drawScale, effects, depth);

            if (who.IsLocalPlayer)
            {
                _lensWorld = handAt + (lens - grip) * drawScale + new Vector2(Game1.viewport.X, Game1.viewport.Y);
                _lensTick = Game1.ticks;
            }
        }

        /// <summary>Which picture to use for the way the farmer faces, and the spot of it that sits in the hand (in picture pixels, before any flip).</summary>
        private static void View(int facing, bool lit, out int cell, out Vector2 grip, out SpriteEffects effects, out Vector2 lens)
        {
            var on = lit ? 1 : 0;
            effects = SpriteEffects.None;
            switch (facing)
            {
                case 1: // right: from the side, the lens ahead
                    cell = on; grip = new Vector2(1.5f, 5.5f); lens = new Vector2(11f, 5.5f); break;
                case 3: // left: the same picture turned round
                    cell = on; grip = new Vector2(HeldCell - 1.5f, 5.5f); effects = SpriteEffects.FlipHorizontally; lens = new Vector2(HeldCell - 11f, 5.5f); break;
                case 0: // up: the end cap faces the viewer and the beam goes away from it
                    cell = 4 + on; grip = new Vector2(6f, 8f); lens = new Vector2(6f, 3f); break;
                default: // down: the lens faces the viewer
                    cell = 2 + on; grip = new Vector2(6f, 8f); lens = new Vector2(6f, 8f); break;
            }
        }

        private static Texture2D? SheetFor(bool male)
        {
            var index = male ? 0 : 1;
            if (Sheets[index] is null && !_failed)
            {
                try
                {
                    Sheets[index] = SheetLoader?.Invoke(male);
                }
                catch (Exception ex)
                {
                    _failed = true; // one message, not one every frame
                    OnError?.Invoke(ex.Message);
                }
            }
            return Sheets[index];
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
                _failed = true;
                OnError?.Invoke(ex.Message);
            }
            return cache;
        }

        /// <summary>The hand in one arm cell of the sheet, measured the first time and remembered.</summary>
        private static HandLocator.Hand? FindHand(Texture2D sheet, bool male, int cellX, int cellY, int side)
        {
            var key = (male ? 1 : 0) | (cellX << 1) | (cellY << 12) | (side << 24);
            if (Hands.TryGetValue(key, out var known))
                return known;

            HandLocator.Hand? hand = null;
            if (cellX >= 0 && cellY >= 0 && cellX + ArmWidth <= sheet.Width && cellY + ArmHeight <= sheet.Height)
            {
                sheet.GetData(0, new Rectangle(cellX, cellY, ArmWidth, ArmHeight), Pixels, 0, Pixels.Length);
                for (var i = 0; i < Pixels.Length; i++)
                    Alpha[i] = Pixels[i].A;
                hand = side switch
                {
                    1 => HandLocator.Find(Alpha, ArmWidth, ArmHeight, 0, ArmWidth / 2),
                    2 => HandLocator.Find(Alpha, ArmWidth, ArmHeight, ArmWidth / 2, ArmWidth),
                    _ => HandLocator.Find(Alpha, ArmWidth, ArmHeight),
                };
                // a cell with an arm on one side only has nothing on the other, so use whatever it does have
                hand ??= side != 0 ? HandLocator.Find(Alpha, ArmWidth, ArmHeight) : null;
            }
            Hands[key] = hand;
            return hand;
        }
    }
}
