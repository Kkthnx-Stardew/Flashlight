namespace Flashlight
{
    /// <summary>Settings read from config.json. This file has no game dependencies so the tests can compile it.</summary>
    public class ModConfig
    {
        /// <summary>The key that switches the flashlight on and off, as a SMAPI button name.</summary>
        public string ToggleKey { get; set; } = "F";

        /// <summary>How far the beam reaches, in tiles.</summary>
        public int BeamLengthTiles { get; set; } = 7;

        /// <summary>How long one battery lasts, in in-game hours.</summary>
        public int BatteryHours { get; set; } = 4;

        /// <summary>The price of the flashlight in the shop.</summary>
        public int FlashlightPrice { get; set; } = 600;

        /// <summary>The price of one battery in the shop.</summary>
        public int BatteryPrice { get; set; } = 60;

        /// <summary>Whether Robin's Carpenter's Shop sells the flashlight and batteries.</summary>
        public bool SoldAtCarpenter { get; set; } = true;

        /// <summary>Keeps every value in a range the game and the balance can cope with.</summary>
        public void Clamp()
        {
            if (string.IsNullOrWhiteSpace(ToggleKey))
                ToggleKey = "F";
            BeamLengthTiles = Math.Clamp(BeamLengthTiles, 3, 12);
            BatteryHours = Math.Clamp(BatteryHours, 1, 12);
            FlashlightPrice = Math.Clamp(FlashlightPrice, 0, 100_000);
            BatteryPrice = Math.Clamp(BatteryPrice, 0, 10_000);
        }
    }
}
