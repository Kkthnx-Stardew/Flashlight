using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace Flashlight
{
    /// <summary>Which tiles of a place stop the beam: the tall things, such as full-grown trees, boulders, buildings and walls. Low things like crops, fences and bushes do not.</summary>
    internal static class LightBlockers
    {
        /// <summary>What stops the beam on one tile: how much of each side is left open, and how many tiles above it the thing is drawn.</summary>
        public readonly record struct Blocker(int Inset, int Rows)
        {
            public static readonly Blocker None = new(-1, 0);
            public bool Blocks => Inset >= 0;
        }

        /// <summary>The pixels left open on each side of a tree's tile, so the trunk blocks 24 of its 64 pixels and not the whole tile.</summary>
        private const int TrunkInset = 20;

        /// <summary>The pixels left open on each side of a boulder's tiles, which fill most of them.</summary>
        private const int BoulderInset = 4;

        public static Blocker Find(GameLocation location, int x, int y)
        {
            if (location.terrainFeatures.TryGetValue(new Vector2(x, y), out var feature))
            {
                // a tree's picture reaches three tiles above the tile it stands on
                if (feature is Tree tree && tree.growthStage.Value >= Tree.treeStage && !tree.stump.Value)
                    return new Blocker(TrunkInset, 3);
                if (feature is FruitTree fruitTree && fruitTree.growthStage.Value >= FruitTree.treeStage && !fruitTree.stump.Value)
                    return new Blocker(TrunkInset, 3);
            }

            foreach (var clump in location.resourceClumps)
            {
                if (clump.occupiesTile(x, y))
                    return new Blocker(BoulderInset, 0);
            }

            foreach (var building in location.buildings)
            {
                // the shipping bin and the pet bowl are low enough to shine over
                if (building is not (ShippingBin or PetBowl) && building.occupiesTile(x, y))
                    return new Blocker(0, 2);
            }

            // walls, cliffs and the like are tiles of the map that cannot be walked through
            var layer = location.map?.GetLayer("Buildings");
            if (layer is not null && layer.IsValidTileLocation(x, y) && layer.Tiles[x, y] is not null
                && location.doesTileHaveProperty(x, y, "Passable", "Buildings") is null)
                return new Blocker(0, 0);

            return Blocker.None;
        }
    }
}
