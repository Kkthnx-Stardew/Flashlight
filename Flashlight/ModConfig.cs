namespace Flashlight
{
    /// <summary>Settings read from config.json. This file has no game dependencies so the tests can compile it.</summary>
    public class ModConfig
    {
        /// <summary>The key that switches the flashlight on and off, as a SMAPI button name.</summary>
        public string ToggleKey { get; set; } = "F";

        /// <summary>How far the beam reaches, in tiles.</summary>
        public int BeamLengthTiles { get; set; } = 7;

        /// <summary>How long a matched pair of Dry Cells lasts, in in-game hours.</summary>
        public int BatteryHours { get; set; } = 2;

        /// <summary>Whether the game's Battery Pack can be loaded as a longer lasting, brighter cell.</summary>
        public bool UseBatteryPacks { get; set; } = true;

        /// <summary>How long a matched pair of Battery Packs lasts, in in-game hours.</summary>
        public int BatteryPackHours { get; set; } = 4;

        /// <summary>How long the beam is while a Battery Pack is in use, as a percentage of the normal beam.</summary>
        public int BatteryPackBeamPercent { get; set; } = 120;

        /// <summary>Whether the farmer is drawn holding the flashlight while it is the selected tool.</summary>
        public bool ShowInHand { get; set; } = true;

        /// <summary>The price of the flashlight in the shop.</summary>
        public int FlashlightPrice { get; set; } = 600;

        /// <summary>The price of one battery in the shop.</summary>
        public int BatteryPrice { get; set; } = 15;

        /// <summary>Whether Robin's Carpenter's Shop sells the flashlight and batteries.</summary>
        public bool SoldAtCarpenter { get; set; } = true;

        /// <summary>How many batteries the shop sells to one player each day.</summary>
        public int BatteryDailyStock { get; set; } = 16;

        /// <summary>The chance, in percent, that on a given day Robin has no Dry Cells at all. It is the same all day.</summary>
        public int BatteryOutOfStockPercent { get; set; } = 15;

        /// <summary>The chance, in percent, that on a given day Robin has only half the usual stock of Dry Cells.</summary>
        public int BatteryLowStockPercent { get; set; } = 25;

        /// <summary>The shop stops selling batteries to a player who already carries this many.</summary>
        public int MaxBatteriesCarried { get; set; } = 16;

        /// <summary>
        /// On average, how many seconds of use pass between random flickers, like an old flashlight with a loose cell.
        /// 0 turns the random flicker off.
        /// </summary>
        public int FlickerSecondsBetween { get; set; } = 40;

        /// <summary>Copies every setting from another instance. Used to restore the defaults from the config menu.</summary>
        public void CopyFrom(ModConfig other)
        {
            foreach (var property in typeof(ModConfig).GetProperties())
            {
                if (property.CanRead && property.CanWrite)
                    property.SetValue(this, property.GetValue(other));
            }
        }

        /// <summary>Keeps every value in a range the game and the balance can cope with.</summary>
        public void Clamp()
        {
            if (string.IsNullOrWhiteSpace(ToggleKey))
                ToggleKey = "F";
            BeamLengthTiles = Math.Clamp(BeamLengthTiles, 3, 12);
            BatteryHours = Math.Clamp(BatteryHours, 1, 12);
            BatteryPackHours = Math.Clamp(BatteryPackHours, 1, 48);
            BatteryPackBeamPercent = Math.Clamp(BatteryPackBeamPercent, 100, 200);
            FlashlightPrice = Math.Clamp(FlashlightPrice, 0, 100_000);
            BatteryPrice = Math.Clamp(BatteryPrice, 0, 10_000);
            MaxBatteriesCarried = Math.Clamp(MaxBatteriesCarried, 1, 20);
            BatteryDailyStock = Math.Clamp(BatteryDailyStock, 1, MaxBatteriesCarried);
            BatteryOutOfStockPercent = Math.Clamp(BatteryOutOfStockPercent, 0, 90);
            BatteryLowStockPercent = Math.Clamp(BatteryLowStockPercent, 0, 90);
            FlickerSecondsBetween = FlickerSecondsBetween <= 0 ? 0 : Math.Clamp(FlickerSecondsBetween, 5, 600);
        }
    }
}
