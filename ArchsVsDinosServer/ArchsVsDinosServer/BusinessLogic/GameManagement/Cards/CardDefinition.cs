using System.Collections.Generic;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public class CardDefinition
    {
        public int IdCard { get; set; }
        public int Power { get; set; }
        public string Type { get; set; }      
        public string Element { get; set; }   
        public string BodyPart { get; set; } 
    }

    public static class CardDefinitions
    {
        private static readonly Dictionary<int, CardDefinition> cards = new Dictionary<int, CardDefinition>
        {
            // ARCHS 
            // Sand
            { 1, new CardDefinition { IdCard = 1, Power = 0, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 2, new CardDefinition { IdCard = 2, Power = 1, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 3, new CardDefinition { IdCard = 3, Power = 2, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 4, new CardDefinition { IdCard = 4, Power = 3, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 5, new CardDefinition { IdCard = 5, Power = 3, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 6, new CardDefinition { IdCard = 6, Power = 4, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 7, new CardDefinition { IdCard = 7, Power = 4, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 8, new CardDefinition { IdCard = 8, Power = 5, Type = "arch", Element = "Sand", BodyPart = "None" } },
            { 9, new CardDefinition { IdCard = 9, Power = 5, Type = "arch", Element = "Sand", BodyPart = "None" } },
            
            // Water
            { 10, new CardDefinition { IdCard = 10, Power = 0, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 11, new CardDefinition { IdCard = 11, Power = 1, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 12, new CardDefinition { IdCard = 12, Power = 2, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 13, new CardDefinition { IdCard = 13, Power = 3, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 14, new CardDefinition { IdCard = 14, Power = 3, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 15, new CardDefinition { IdCard = 15, Power = 4, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 16, new CardDefinition { IdCard = 16, Power = 4, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 17, new CardDefinition { IdCard = 17, Power = 5, Type = "arch", Element = "Water", BodyPart = "None" } },
            { 18, new CardDefinition { IdCard = 18, Power = 5, Type = "arch", Element = "Water", BodyPart = "None" } },
            
            // Wind
            { 19, new CardDefinition { IdCard = 19, Power = 0, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 20, new CardDefinition { IdCard = 20, Power = 1, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 21, new CardDefinition { IdCard = 21, Power = 2, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 22, new CardDefinition { IdCard = 22, Power = 3, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 23, new CardDefinition { IdCard = 23, Power = 3, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 24, new CardDefinition { IdCard = 24, Power = 4, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 25, new CardDefinition { IdCard = 25, Power = 4, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 26, new CardDefinition { IdCard = 26, Power = 5, Type = "arch", Element = "Wind", BodyPart = "None" } },
            { 27, new CardDefinition { IdCard = 27, Power = 5, Type = "arch", Element = "Wind", BodyPart = "None" } },

            // DINOS 
            // Sand  
            { 28, new CardDefinition { IdCard = 28, Power = 0, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 29, new CardDefinition { IdCard = 29, Power = 1, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 30, new CardDefinition { IdCard = 30, Power = 1, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 31, new CardDefinition { IdCard = 31, Power = 1, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 32, new CardDefinition { IdCard = 32, Power = 2, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 33, new CardDefinition { IdCard = 33, Power = 2, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 34, new CardDefinition { IdCard = 34, Power = 3, Type = "head", Element = "Sand", BodyPart = "None" } },
            { 35, new CardDefinition { IdCard = 35, Power = 3, Type = "head", Element = "Sand", BodyPart = "None" } },
            
            // Water
            { 36, new CardDefinition { IdCard = 36, Power = 0, Type = "head", Element = "Water", BodyPart = "None" } },
            { 37, new CardDefinition { IdCard = 37, Power = 1, Type = "head", Element = "Water", BodyPart = "None" } },
            { 38, new CardDefinition { IdCard = 38, Power = 1, Type = "head", Element = "Water", BodyPart = "None" } },
            { 39, new CardDefinition { IdCard = 39, Power = 1, Type = "head", Element = "Water", BodyPart = "None" } },
            { 40, new CardDefinition { IdCard = 40, Power = 2, Type = "head", Element = "Water", BodyPart = "None" } },
            { 41, new CardDefinition { IdCard = 41, Power = 2, Type = "head", Element = "Water", BodyPart = "None" } },
            { 42, new CardDefinition { IdCard = 42, Power = 3, Type = "head", Element = "Water", BodyPart = "None" } },
            { 43, new CardDefinition { IdCard = 43, Power = 3, Type = "head", Element = "Water", BodyPart = "None" } },
            
            // Wind
            { 44, new CardDefinition { IdCard = 44, Power = 0, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 45, new CardDefinition { IdCard = 45, Power = 1, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 46, new CardDefinition { IdCard = 46, Power = 1, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 47, new CardDefinition { IdCard = 47, Power = 1, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 48, new CardDefinition { IdCard = 48, Power = 2, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 49, new CardDefinition { IdCard = 49, Power = 2, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 50, new CardDefinition { IdCard = 50, Power = 3, Type = "head", Element = "Wind", BodyPart = "None" } },
            { 51, new CardDefinition { IdCard = 51, Power = 3, Type = "head", Element = "Wind", BodyPart = "None" } },

            // BODY PARTS
            // Chest
            { 52, new CardDefinition { IdCard = 52, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 53, new CardDefinition { IdCard = 53, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 54, new CardDefinition { IdCard = 54, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 55, new CardDefinition { IdCard = 55, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 56, new CardDefinition { IdCard = 56, Power = 2, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 57, new CardDefinition { IdCard = 57, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 58, new CardDefinition { IdCard = 58, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 59, new CardDefinition { IdCard = 59, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 60, new CardDefinition { IdCard = 60, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 61, new CardDefinition { IdCard = 61, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 62, new CardDefinition { IdCard = 62, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 63, new CardDefinition { IdCard = 63, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 64, new CardDefinition { IdCard = 64, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 65, new CardDefinition { IdCard = 65, Power = 0, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 66, new CardDefinition { IdCard = 66, Power = 1, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 67, new CardDefinition { IdCard = 67, Power = 2, Type = "body", Element = "None", BodyPart = "Chest" } },
            { 68, new CardDefinition { IdCard = 68, Power = 3, Type = "body", Element = "None", BodyPart = "Chest" } },
            
            // Left Arm
            { 69, new CardDefinition { IdCard = 69, Power = 0, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 70, new CardDefinition { IdCard = 70, Power = 0, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 71, new CardDefinition { IdCard = 71, Power = 0, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 72, new CardDefinition { IdCard = 72, Power = 1, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 73, new CardDefinition { IdCard = 73, Power = 1, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 74, new CardDefinition { IdCard = 74, Power = 1, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 75, new CardDefinition { IdCard = 75, Power = 1, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 76, new CardDefinition { IdCard = 76, Power = 1, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 77, new CardDefinition { IdCard = 77, Power = 2, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            { 78, new CardDefinition { IdCard = 78, Power = 3, Type = "body", Element = "None", BodyPart = "LeftArm" } },
            
            // Legs
            { 79, new CardDefinition { IdCard = 79, Power = 0, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 80, new CardDefinition { IdCard = 80, Power = 0, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 81, new CardDefinition { IdCard = 81, Power = 1, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 82, new CardDefinition { IdCard = 82, Power = 1, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 83, new CardDefinition { IdCard = 83, Power = 1, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 84, new CardDefinition { IdCard = 84, Power = 2, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 85, new CardDefinition { IdCard = 85, Power = 2, Type = "body", Element = "None", BodyPart = "Legs" } },
            { 86, new CardDefinition { IdCard = 86, Power = 3, Type = "body", Element = "None", BodyPart = "Legs" } },
            
            // Right Arm
            { 87, new CardDefinition { IdCard = 87, Power = 0, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 88, new CardDefinition { IdCard = 88, Power = 0, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 89, new CardDefinition { IdCard = 89, Power = 0, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 90, new CardDefinition { IdCard = 90, Power = 1, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 91, new CardDefinition { IdCard = 91, Power = 1, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 92, new CardDefinition { IdCard = 92, Power = 1, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 93, new CardDefinition { IdCard = 93, Power = 1, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 94, new CardDefinition { IdCard = 94, Power = 1, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 95, new CardDefinition { IdCard = 95, Power = 2, Type = "body", Element = "None", BodyPart = "RightArm" } },
            { 96, new CardDefinition { IdCard = 96, Power = 3, Type = "body", Element = "None", BodyPart = "RightArm" } },
        };

        public static CardDefinition GetCard(int id)
        {
            return cards.TryGetValue(id, out var card) ? card : null;
        }
        
        public static List<int> GetAllCardIds()
        {
            return new List<int>(cards.Keys);
        }
    }
}