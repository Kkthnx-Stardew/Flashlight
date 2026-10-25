using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Makes the beam picture the game draws for one frame. The game lights everything inside a light's picture, with no sense of
    /// what stands in the way, so a beam would shine through a tree and light the far side of it. This takes the cone picture and
    /// darkens every spot whose straight line to the lens is cut by a tall thing, so the tree throws a shadow and the beam stops at it.
    /// </summary>
    internal sealed class BeamMask
    {
        private const int Size = 128;
        private const float Tile = 64f;

        /// <summary>The most tiles above its own tile that anything is drawn, so the picture of a tree covers the tiles behind it on the screen.</summary>
        private const int BodyRows = 3;

        private const int South = 2;
        private const int North = 0;

        private readonly Color[]?[] _source = new Color[4][];
        private readonly int[]?[] _lit = new int[4][];
        private readonly Color[] _pixels = new Color[Size * Size];
        private readonly byte[] _visible = new byte[Size * Size];
        private readonly ushort[] _scratch = new ushort[Size * Size];
        private byte[] _known = Array.Empty<byte>();   // per tile: 0 not looked up yet, else the rows above it that it is drawn over in the high bits and the open pixels plus 2 in the low five
        private Texture2D? _texture;

        // the window of tiles looked at for the picture being built, read by Lookup and Margin
        private GameLocation? _location;
        private int _left;
        private int _top;
        private int _width;

        /// <summary>Margin as a delegate made once: turning a method into a new delegate for every pixel was most of what this class allocated.</summary>
        private readonly Func<int, int, float> _margin;

        /// <summary>
        /// What the last picture was made for. While none of it has changed the same picture is drawn again, so a farmer standing
        /// still with the light on does not pay for a new one every frame.
        /// </summary>
        private int _builtFacing = -1;
        private float _builtShown;
        private bool _builtShadows;

        /// <summary>The cone is never drawn more than this much larger than the picture was made for.</summary>
        private const float MaxRatio = 1.5f;

        private readonly Color[] _scaledSource = new Color[Size * Size];
        private readonly int[] _scaledLit = new int[Size * Size];
        private Vector2 _builtOrigin;
        private float _builtRadius;
        private Vector2 _builtPlayer;
        private GameLocation? _builtLocation;
        private int _builtTick;

        /// <summary>A tree chopped down or planted while the farmer stands still shows within this many updates, about a fifth of a second.</summary>
        private const int RefreshTicks = 12;

        public BeamMask()
        {
            _margin = Margin;
        }

        public void Forget()
        {
            Array.Clear(_source);
            Array.Clear(_lit);
            _texture = null;
            _builtFacing = -1;
            _builtLocation = null;
        }

        /// <summary>
        /// The beam picture for this frame. <paramref name="radius"/> is the world pixels one picture pixel is drawn across, and
        /// <paramref name="origin"/> the world position the picture is centred on: the same two numbers the game draws it with.
        /// </summary>
        public Texture2D? Build(GraphicsDevice device, Texture2D beam, int facing, Vector2 origin, float radius, GameLocation location, Farmer player,
            float shownRadius = -1f, bool shadows = true)
        {
            if (beam.Width != Size || beam.Height != Size || radius <= 0f)
                return null;
            facing = ((facing % 4) + 4) % 4;

            if (_texture is not null && facing == _builtFacing && ReferenceEquals(location, _builtLocation) && origin == _builtOrigin
                && radius == _builtRadius && shownRadius == _builtShown && shadows == _builtShadows && player.Position == _builtPlayer
                && Game1.ticks >= _builtTick && Game1.ticks - _builtTick < RefreshTicks)
                return _texture;

            var source = _source[facing];
            var lit = _lit[facing];
            if (source is null || lit is null)
            {
                source = new Color[Size * Size];
                beam.GetData(source);
                lit = Enumerable.Range(0, source.Length).Where(i => source[i].A > 0).ToArray();
                _source[facing] = source;
                _lit[facing] = lit;
            }

            // The size the game draws the picture at can differ from the size the beam really is, for another player's flashlight
            // whose flicker has not reached this machine as a radius. Then the cone is drawn smaller or larger inside the picture.
            var litList = lit;
            var litCount = lit.Length;
            var pixelsFrom = source;
            var ratio = shownRadius < 0f ? 1f : Math.Clamp(shownRadius / radius, 0f, MaxRatio);
            if (Math.Abs(ratio - 1f) > 0.01f)
            {
                litCount = Rescale(source, ratio, _scaledSource, _scaledLit);
                litList = _scaledLit;
                pixelsFrom = _scaledSource;
            }

            // The lens is held above the ground, but a tree stands on it. The picture is drawn flat on the screen, so the shadows
            // are worked out as if the lens were at the farmer's feet: every point is moved down by the same amount first.
            var drop = player.Position.Y + Tile / 2f - origin.Y;

            // a window of tiles around the lens, each looked up once at most
            var reach = (int)Math.Ceiling(radius * Size / 2f / Tile) + BodyRows + 2;
            var width = reach * 2 + 1;
            _location = location;
            _width = width;
            _left = (int)Math.Floor(origin.X / Tile) - reach;
            _top = (int)Math.Floor((origin.Y + drop) / Tile) - reach;
            if (_known.Length != width * width)
                _known = new byte[width * width];
            else
                Array.Clear(_known);

            var fromX = origin.X / Tile;
            var fromY = (origin.Y + drop) / Tile;
            var bodyLeft = player.Position.X + Tile / 2f - 26f;
            var bodyRight = player.Position.X + Tile / 2f + 26f;
            var bodyTop = player.Position.Y - 70f;
            var bodyBottom = player.Position.Y + 10f;

            Array.Clear(_visible);
            for (var n = 0; n < litCount; n++)
            {
                var i = litList[n];
                if (!shadows)
                {
                    _visible[i] = 255;
                    continue;
                }

                var worldX = origin.X + (i % Size + 0.5f - Size / 2f) * radius;
                var worldY = origin.Y + (i / Size + 0.5f - Size / 2f) * radius;

                // facing away from the screen, the farmer's own back is in front of the lens and takes no light
                if (facing == North && worldX > bodyLeft && worldX < bodyRight && worldY > bodyTop && worldY < bodyBottom)
                    continue;

                var toX = worldX / Tile;
                var toY = (worldY + drop) / Tile;
                var tileX = (int)Math.Floor(toX);
                var tileY = (int)Math.Floor(toY);

                // Is this spot part of the picture of a tall thing, which stands on a tile below it on the screen?
                var bodyTile = -1;
                for (var row = 0; row <= BodyRows && bodyTile < 0; row++)
                {
                    var state = Lookup(tileX, tileY + row);
                    if ((state & 31) != 1 && (state >> 5) >= row)
                        bodyTile = tileY + row;
                }

                bool lightReaches;
                if (bodyTile < 0)
                    lightReaches = BeamShadow.IsClear(fromX, fromY, toX, toY, _margin);
                else if (facing == South)
                    lightReaches = false; // the beam comes from behind it, and the side the screen shows is the dark one
                else
                    lightReaches = BeamShadow.IsClear(fromX, fromY, tileX + 0.5f, bodyTile + 0.5f, _margin, ignoreEnd: true);

                if (lightReaches)
                    _visible[i] = 255;
            }
            if (shadows)
                BeamShadow.Soften(_visible, _scratch, Size);

            Array.Clear(_pixels);
            for (var n = 0; n < litCount; n++)
            {
                var i = litList[n];
                _pixels[i] = pixelsFrom[i] * (_visible[i] / 255f);
            }

            _texture ??= new Texture2D(device, Size, Size);
            _texture.SetData(_pixels);
            _location = null; // the window is only for this build
            _builtFacing = facing;
            _builtOrigin = origin;
            _builtRadius = radius;
            _builtShown = shownRadius;
            _builtShadows = shadows;
            _builtPlayer = player.Position;
            _builtLocation = location;
            _builtTick = Game1.ticks;
            return _texture;
        }

        /// <summary>
        /// Draws the cone smaller (a ratio under 1) or larger (over 1) around the lens at the centre of the picture, by taking each
        /// picture pixel from the place in the cone it would have at that size. Returns how many pixels have light in them, listed in <paramref name="lit"/>.
        /// </summary>
        private static int Rescale(Color[] source, float ratio, Color[] into, int[] lit)
        {
            Array.Clear(into);
            if (ratio <= 0.01f)
                return 0;
            var count = 0;
            const float half = Size / 2f;
            for (var y = 0; y < Size; y++)
            {
                var sy = (int)MathF.Floor(half + (y + 0.5f - half) / ratio);
                if ((uint)sy >= Size)
                    continue;
                for (var x = 0; x < Size; x++)
                {
                    var sx = (int)MathF.Floor(half + (x + 0.5f - half) / ratio);
                    if ((uint)sx >= Size)
                        continue;
                    var pixel = source[sy * Size + sx];
                    if (pixel.A == 0)
                        continue;
                    into[y * Size + x] = pixel;
                    lit[count++] = y * Size + x;
                }
            }
            return count;
        }

        /// <summary>What is known about a tile of the window, looked up once at most.</summary>
        private byte Lookup(int x, int y)
        {
            var cx = x - _left;
            var cy = y - _top;
            if (cx < 0 || cy < 0 || cx >= _width || cy >= _width)
                return 1; // outside the window, so nothing
            ref var state = ref _known[cy * _width + cx];
            if (state == 0)
            {
                var found = LightBlockers.Find(_location!, x, y);
                state = (byte)((found.Rows << 5) | (found.Inset + 2)); // the low part is 1 for nothing, 2 and up for a wall or a trunk
            }
            return state;
        }

        /// <summary>How much of each side of a tile is left open (a fraction of the tile), or Open when it blocks nothing.</summary>
        private float Margin(int x, int y)
        {
            var low = Lookup(x, y) & 31;
            return low == 1 ? BeamShadow.Open : (low - 2) / Tile;
        }
    }
}
