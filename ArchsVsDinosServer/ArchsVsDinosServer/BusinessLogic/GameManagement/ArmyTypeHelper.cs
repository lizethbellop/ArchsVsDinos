using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public static class ArmyTypeHelper
    {
        public const string Sand = "sand";
        public const string Water = "water";
        public const string Wind = "wind";
        public const string None = "None";

        public const string DinoSand = "dinoSand";
        public const string DinoWater = "dinoWater";
        public const string DinoWind = "dinoWind";

        public const string ArchSand = "archSand";
        public const string ArchWater = "archWater";
        public const string ArchWind = "archWind";

        public static bool IsArch(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return false;
            }
            return armyType == ArchSand || armyType == ArchWater || armyType == ArchWind;
        }

        public static bool IsDino(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return false;
            }
            return armyType == DinoSand || armyType == DinoWater || armyType == DinoWind;
        }

        public static string GetBaseType(string armyType)
        {
            if (IsEmpty(armyType))
            {
                return null;
            }
            var lower = armyType.ToLower();
            if (lower.Contains("sand")) return Sand;
            if (lower.Contains("water")) return Water;
            if (lower.Contains("wind")) return Wind;
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
                case Sand:
                    return DinoSand;
                case Water:
                    return DinoWater;
                case Wind:
                    return DinoWind;
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
                case Sand:
                    return ArchSand;
                case Water:
                    return ArchWater;
                case Wind:
                    return ArchWind;
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
            return lower == Sand || lower == Water || lower == Wind;
        }

        public static bool IsValidArmyType(string armyType)
        {
            return IsArch(armyType) || IsDino(armyType);
        }

        public static string NormalizeElement(string element)
        {
            if (IsEmpty(element))
            {
                return None;
            }

            switch (element)
            {
                case "Sand":
                    return Sand;
                case "Water":
                    return Water;
                case "Wind":
                    return Wind;
                default:
                    var lower = element.ToLower();
                    if (lower == Sand || lower == Water || lower == Wind)
                        return lower;
                    return None;
            }
        }

        private static bool IsEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}