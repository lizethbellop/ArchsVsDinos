using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public static class ArmyTypeHelper
    {
        public const string Land = "land";
        public const string Sea = "sea";
        public const string Sky = "sky";

        public const string DinoLand = "dinoLand";
        public const string DinoSea = "dinoSea";
        public const string DinoSky = "dinoSky";

        public const string ArchLand = "archLand";
        public const string ArchSea = "archSea";
        public const string ArchSky = "archSky";

        public static bool IsArch(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return false;
            }

            return armyType == ArchLand || armyType == ArchSea || armyType == ArchSky;
        }

        public static bool IsDino(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return false;
            }

            return armyType == DinoLand || armyType == DinoSea || armyType == DinoSky;
        }

        public static string GetBaseType(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return null;
            }

            var lower = armyType.ToLower();

            if (lower.Contains("land")) return Land;
            if (lower.Contains("sea")) return Sea;
            if (lower.Contains("sky")) return Sky;

            return null;
        }

        public static string ToDinoType(string baseType)
        {
            if (IsEmpty(baseType))
            {
                return null;
            }

            switch (baseType.ToLower())
            {
                case Land:
                    return DinoLand;
                case Sea:
                    return DinoSea;
                case Sky:
                    return DinoSky;
                default:
                    return null;
            }
        }

        public static string ToArchType(string baseType)
        {
            if (IsEmpty(baseType))
            {
                return null;
            }

            switch (baseType.ToLower())
            {
                case Land:
                    return ArchLand;
                case Sea:
                    return ArchSea;
                case Sky:
                    return ArchSky;
                default:
                    return null;
            }
        }

        public static bool IsValidBaseType(string baseType)
        {
            if (IsEmpty(baseType))
            {
                return false;
            }

            var lower = baseType.ToLower();
            return lower == Land || lower == Sea || lower == Sky;
        }

        public static bool IsValidArmyType(string armyType)
        {
            return IsArch(armyType) || IsDino(armyType);
        }

        private static bool IsEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}
