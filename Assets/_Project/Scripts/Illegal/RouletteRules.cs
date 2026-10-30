using System;

namespace KanBagi.Illegal
{
    /// <summary>Avrupa ruleti kurallari (0-36, tek sifir). Sadece bilgi; para/tur mantigi GamblingTable'da.</summary>
    public static class RouletteRules
    {
        public const int PocketCount = 37;

        private static readonly int[] RedNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        /// <summary>Avrupa carkindaki sayilarin saat yonundeki dizilisi (0'dan baslar).</summary>
        public static readonly int[] WheelOrder =
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
            5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
        };

        public static bool IsRed(int number) => Array.IndexOf(RedNumbers, number) >= 0;

        public static bool IsBlack(int number) => number != 0 && !IsRed(number);

        /// <summary>Sayinin carktaki dilim sirasi (aci = sira * 360 / 37).</summary>
        public static int PocketIndex(int number) => Array.IndexOf(WheelOrder, number);
    }
}
