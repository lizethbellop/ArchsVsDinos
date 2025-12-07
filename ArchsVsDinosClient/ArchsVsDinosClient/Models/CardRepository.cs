using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    public static class CardRepositoryModel
    {
        private const string BasePath = "pack://application:,,,/ArchsVsDinosClient;component/Resources/";

        public static List<Card> Cards { get; } = new List<Card>()
        {
            // ARCHS
            // -- Sand Archs Cards
            new Card(1, BasePath + "Images/Cards/Archs/Sand/Power0/A_Sand0_1.png", 0),
            new Card(2, BasePath + "Images/Cards/Archs/Sand/Power1/A_Sand1_1.png", 1),
            new Card(3, BasePath + "Images/Cards/Archs/Sand/Power2/A_Sand2_1.png", 2),
            new Card(4, BasePath + "Images/Cards/Archs/Sand/Power3/A_Sand3_1.png", 3),
            new Card(5, BasePath + "Images/Cards/Archs/Sand/Power3/A_Sand3_2.png", 3),
            new Card(6, BasePath + "Images/Cards/Archs/Sand/Power4/A_Sand4_1.png", 4),
            new Card(7, BasePath + "Images/Cards/Archs/Sand/Power4/A_Sand4_2.png", 4),
            new Card(8, BasePath + "Images/Cards/Archs/Sand/Power5/A_Sand5_1.png", 5),
            new Card(9, BasePath + "Images/Cards/Archs/Sand/Power5/A_Sand5_2.png", 5),
            
            // -- Sea Archs Cards
            new Card(10, BasePath + "Images/Cards/Archs/Sea/Power0/A_Sea0_1.png", 0),
            new Card(11, BasePath + "Images/Cards/Archs/Sea/Power1/A_Sea1_1.png", 1),
            new Card(12, BasePath + "Images/Cards/Archs/Sea/Power2/A_Sea2_1.png", 2),
            new Card(13, BasePath + "Images/Cards/Archs/Sea/Power3/A_Sea3_1.png", 3),
            new Card(14, BasePath + "Images/Cards/Archs/Sea/Power3/A_Sea3_2.png", 3),
            new Card(15, BasePath + "Images/Cards/Archs/Sea/Power4/A_Sea4_1.png", 4),
            new Card(16, BasePath + "Images/Cards/Archs/Sea/Power4/A_Sea4_2.png", 4),
            new Card(17, BasePath + "Images/Cards/Archs/Sea/Power5/A_Sea5_1.png", 5),
            new Card(18, BasePath + "Images/Cards/Archs/Sea/Power5/A_Sea5_2.png", 5),
            
            // -- Wind Archs Cards
            new Card(19, BasePath + "Images/Cards/Archs/Wind/Power0/A_Wind0_1.png", 0),
            new Card(20, BasePath + "Images/Cards/Archs/Wind/Power1/A_Wind1_1.png", 1),
            new Card(21, BasePath + "Images/Cards/Archs/Wind/Power2/A_Wind2_1.png", 2),
            new Card(22, BasePath + "Images/Cards/Archs/Wind/Power3/A_Wind3_1.png", 3),
            new Card(23, BasePath + "Images/Cards/Archs/Wind/Power3/A_Wind3_2.png", 3),
            new Card(24, BasePath + "Images/Cards/Archs/Wind/Power4/A_Wind4_1.png", 4),
            new Card(25, BasePath + "Images/Cards/Archs/Wind/Power4/A_Wind4_2.png", 4),
            new Card(26, BasePath + "Images/Cards/Archs/Wind/Power5/A_Wind5_1.png", 5),
            new Card(27, BasePath + "Images/Cards/Archs/Wind/Power5/A_Wind5_2.png", 5),

            // DINOS
            // -- Sand Dino Cards
            new Card(28, BasePath + "Images/Cards/Dinos/Sand/Power0/D_Sand0_1.png", 0),
            new Card(29, BasePath + "Images/Cards/Dinos/Sand/Power1/D_Sand1_1.png", 1),
            new Card(30, BasePath + "Images/Cards/Dinos/Sand/Power1/D_Sand1_2.png", 1),
            new Card(31, BasePath + "Images/Cards/Dinos/Sand/Power1/D_Sand1_3.png", 1),
            new Card(32, BasePath + "Images/Cards/Dinos/Sand/Power2/D_Sand2_1.png", 2),
            new Card(33, BasePath + "Images/Cards/Dinos/Sand/Power2/D_Sand2_2.png", 2),
            new Card(34, BasePath + "Images/Cards/Dinos/Sand/Power3/D_Sand3_1.png", 3),
            new Card(35, BasePath + "Images/Cards/Dinos/Sand/Power3/D_Sand3_2.png", 3),
            
            // -- Sea Dino Cards
            new Card(36, BasePath + "Images/Cards/Dinos/Sea/Power0/D_Sea0_1.png", 0),
            new Card(37, BasePath + "Images/Cards/Dinos/Sea/Power1/D_Sea1_1.png", 1),
            new Card(38, BasePath + "Images/Cards/Dinos/Sea/Power1/D_Sea1_2.png", 1),
            new Card(39, BasePath + "Images/Cards/Dinos/Sea/Power1/D_Sea1_3.png", 1),
            new Card(40, BasePath + "Images/Cards/Dinos/Sea/Power2/D_Sea2_1.png", 2),
            new Card(41, BasePath + "Images/Cards/Dinos/Sea/Power2/D_Sea2_2.png", 2),
            new Card(42, BasePath + "Images/Cards/Dinos/Sea/Power3/D_Sea3_1.png", 3),
            new Card(43, BasePath + "Images/Cards/Dinos/Sea/Power3/D_Sea3_2.png", 3),
            
            // -- Wind Dino Cards
            new Card(44, BasePath + "Images/Cards/Dinos/Wind/Power0/D_Wind0_1.png", 0), 
            new Card(45, BasePath + "Images/Cards/Dinos/Wind/Power1/D_Wind1_1.png", 1),
            new Card(46, BasePath + "Images/Cards/Dinos/Wind/Power1/D_Wind1_2.png", 1),
            new Card(47, BasePath + "Images/Cards/Dinos/Wind/Power1/D_Wind1_3.png", 1),
            new Card(48, BasePath + "Images/Cards/Dinos/Wind/Power2/D_Wind2_1.png", 2),
            new Card(49, BasePath + "Images/Cards/Dinos/Wind/Power2/D_Wind2_2.png", 2),
            new Card(50, BasePath + "Images/Cards/Dinos/Wind/Power3/D_Wind3_1.png", 3),
            new Card(51, BasePath + "Images/Cards/Dinos/Wind/Power3/D_Wind3_2.png", 3),

            // CHEST
            // -- Chest Arms
            new Card(52, BasePath + "Images/Cards/Chest/ChestArms/Power0/ChestArms_Power0_1.png", 0),
            new Card(53, BasePath + "Images/Cards/Chest/ChestArms/Power1/ChestArms_Power1_1.png", 1),
            new Card(54, BasePath + "Images/Cards/Chest/ChestArms/Power1/ChestArms_Power1_2.png", 1),
            new Card(55, BasePath + "Images/Cards/Chest/ChestArms/Power1/ChestArms_Power1_3.png", 1),
            new Card(56, BasePath + "Images/Cards/Chest/ChestArms/Power2/ChestArms_Power2_1.png", 2),
            
            // -- Chest Arms Legs
            new Card(57, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power0/ChestParts_Power0_1.png", 0),
            new Card(58, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power0/ChestParts_Power0_2.png", 0),
            new Card(59, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power0/ChestParts_Power0_3.png", 0),
            new Card(60, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power0/ChestParts_Power0_4.png", 0),
            new Card(61, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power0/ChestParts_Power0_5.png", 0),
            new Card(62, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power1/ChestParts_Power1_1.png", 1),
            new Card(63, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power1/ChestParts_Power1_2.png", 1),
            new Card(64, BasePath + "Images/Cards/Chest/ChestArmsLegs/Power1/ChestParts_Power1_3.png", 1),
            
            // -- Chest Complete
            new Card(65, BasePath + "Images/Cards/Chest/ChestComplete/Power0/ChestComplete_Power0_1.png", 0),
            new Card(66, BasePath + "Images/Cards/Chest/ChestComplete/Power1/ChestComplete_Power1_1.png", 1),
            new Card(67, BasePath + "Images/Cards/Chest/ChestComplete/Power2/ChestComplete_Power2_1.png", 2),
            new Card(68, BasePath + "Images/Cards/Chest/ChestComplete/Power3/ChestComplete_Power3_1.png", 3),

            // LEFT ARM
            new Card(69, BasePath + "Images/Cards/LeftArm/Power0/Left_Power0_1.png", 0),
            new Card(70, BasePath + "Images/Cards/LeftArm/Power0/Left_Power0_2.png", 0),
            new Card(71, BasePath + "Images/Cards/LeftArm/Power0/Left_Power0_3.png", 0),
            new Card(72, BasePath + "Images/Cards/LeftArm/Power1/Left_Power1_1.png", 1),
            new Card(73, BasePath + "Images/Cards/LeftArm/Power1/Left_Power1_2.png", 1),
            new Card(74, BasePath + "Images/Cards/LeftArm/Power1/Left_Power1_3.png", 1),
            new Card(75, BasePath + "Images/Cards/LeftArm/Power1/Left_Power1_4.png", 1),
            new Card(76, BasePath + "Images/Cards/LeftArm/Power1/Left_Power1_5.png", 1),
            new Card(77, BasePath + "Images/Cards/LeftArm/Power2/Left_Power2_1.png", 2),
            new Card(78, BasePath + "Images/Cards/LeftArm/Power3/Left_Power3_1.png", 3),

            // LEGS
            new Card(79, BasePath + "Images/Cards/Legs/Power0/Legs_Power0_1.png", 0),
            new Card(80, BasePath + "Images/Cards/Legs/Power0/Legs_Power0_2.png", 0),
            new Card(81, BasePath + "Images/Cards/Legs/Power1/Legs_Power1_1.png", 1),
            new Card(82, BasePath + "Images/Cards/Legs/Power1/Legs_Power1_2.png", 1),
            new Card(83, BasePath + "Images/Cards/Legs/Power1/Legs_Power1_3.png", 1),
            new Card(84, BasePath + "Images/Cards/Legs/Power2/Legs_Power2_1.png", 2),
            new Card(85, BasePath + "Images/Cards/Legs/Power2/Legs_Power2_2.png", 2),
            new Card(86, BasePath + "Images/Cards/Legs/Power3/Legs_Power3_1.png", 3),

            // RIGHT ARM
            new Card(87, BasePath + "Images/Cards/RightArm/Power0/Right_Power0_1.png", 0),
            new Card(88, BasePath + "Images/Cards/RightArm/Power0/Right_Power0_2.png", 0),
            new Card(89, BasePath + "Images/Cards/RightArm/Power0/Right_Power0_3.png", 0),
            new Card(90, BasePath + "Images/Cards/RightArm/Power1/Right_Power1_1.png", 1),
            new Card(91, BasePath + "Images/Cards/RightArm/Power1/Right_Power1_2.png", 1),
            new Card(92, BasePath + "Images/Cards/RightArm/Power1/Right_Power1_3.png", 1),
            new Card(93, BasePath + "Images/Cards/RightArm/Power1/Right_Power1_4.png", 1),
            new Card(94, BasePath + "Images/Cards/RightArm/Power1/Right_Power1_5.png", 1),
            new Card(95, BasePath + "Images/Cards/RightArm/Power2/Right_Power2_1.png", 2),
            new Card(96, BasePath + "Images/Cards/RightArm/Power3/Right_Power3_1.png", 3),
        };

        public static Card GetById(int id)
            => Cards.FirstOrDefault(card => card.IdCard == id);
    }
}