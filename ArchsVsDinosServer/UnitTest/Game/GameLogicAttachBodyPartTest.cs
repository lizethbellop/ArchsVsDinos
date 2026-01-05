using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace UnitTest.Game
{
    [TestClass]
    public class GameLogicAttachBodyPartTest : BaseTestClass
    {
        private GameSessionManager sessionManager;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession testSession;
        private PlayerSession testPlayer;

        [TestInitialize]
        public void SetupAttachBodyPartTests()
        {
            base.BaseSetup();

            sessionManager = new GameSessionManager(mockLoggerHelper.Object);
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();
            mockSetupHandler = new Mock<GameSetupHandler>();

            gameCoreContext = new GameCoreContext(sessionManager, mockSetupHandler.Object);

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            string matchCode = "TEST-MATCH";
            sessionManager.CreateSession(matchCode);
            testSession = sessionManager.GetSession(matchCode);
            testSession.MarkAsStarted();

            testPlayer = new PlayerSession(1, "TestPlayer", null);
            testSession.AddPlayer(testPlayer);
            testSession.StartTurn(1);
        }

        // ==========================================
        // ESCENARIOS DE ÉXITO Y SUMA DE PODER
        // ==========================================

        [TestMethod]
        public void TestAttachBodyPart_IncreasesTotalPower()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var torso = CreateTorsoCard(2, 10, ArmyType.Sand);

            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);

            var data = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            var result = gameLogic.AttachBodyPart("TEST-MATCH", 1, data);

            // Assert
            Assert.IsTrue(result, "Logic should allow attaching the torso");
            Assert.AreEqual(15, dino.TotalPower, "The power should be 5 (head) + 10 (torso) = 15");
        }

        [TestMethod]
        public void TestAttachBodyPart_Success_TorsoToHead()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var torso = CreateTorsoCard(2, 5, ArmyType.Sand);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);

            var data = new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, data);

            // Assert
            Assert.AreEqual(2, dino.GetAllCards().Count, "Dino should have 2 cards now");
        }

        [TestMethod]
        public void TestAttachBodyPart_RemovesCardFromHand()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var torso = CreateTorsoCard(2, 5, ArmyType.Sand);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(torso);

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });

            // Assert
            Assert.AreEqual(0, testPlayer.Hand.Count, "Card must be removed from hand");
        }

        [TestMethod]
        public void TestAttachBodyPart_Success_LegsToTorso()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var torso = CreateTorsoCard(2, 5, ArmyType.Sand);
            var legs = CreateLegsCard(3, 5, ArmyType.Sand);

            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);
            testPlayer.AddCard(legs);

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 3 });

            // Assert
            Assert.IsNotNull(dino.LegsCard);
            Assert.AreEqual(15, dino.TotalPower);
        }

        [TestMethod]
        public void TestAttachBodyPart_AllowsNoneElement()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var neutralTorso = CreateTorsoCard(2, 5, ArmyType.None);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(neutralTorso);

            // Act
            var result = gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });

            // Assert
            Assert.IsTrue(result, "Neutral parts should be compatible with any element");
        }

        // ==========================================
        // ESCENARIOS DE ERROR Y VALIDACIÓN
        // ==========================================

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPart_Throws_WhenElementMismatch()
        {
            // Arrange
            var head = CreateHeadCard(1, 5, ArmyType.Sand);
            var waterTorso = CreateTorsoCard(2, 5, ArmyType.Water);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(waterTorso);

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });
        }

        [TestMethod]
        public void TestAttachBodyPart_ReturnsFalse_WhenDinoIsMissing()
        {
            // Arrange
            testPlayer.AddCard(CreateTorsoCard(2, 5, ArmyType.Sand));

            // Act
            var result = gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 999, CardId = 2 });

            // Assert
            Assert.IsFalse(result);
            mockLoggerHelper.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Dino not found"))), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPart_Throws_WhenCardIsNotBodyPart()
        {
            // Arrange
            testPlayer.AddDino(new DinoInstance(1, CreateHeadCard(1, 5, ArmyType.Sand)));
            testPlayer.AddCard(CreateHeadCard(2, 5, ArmyType.Sand)); // Intentar usar otra cabeza como parte

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });
        }

        // ==========================================
        // ESCENARIOS DE NOTIFICACIÓN
        // ==========================================

        [TestMethod]
        public void TestAttachBodyPart_NotifiesClientsCorrectly()
        {
            // Arrange
            testPlayer.AddDino(new DinoInstance(1, CreateHeadCard(1, 5, ArmyType.Sand)));
            testPlayer.AddCard(CreateTorsoCard(2, 5, ArmyType.Sand));

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });

            // Assert
            mockGameNotifier.Verify(n => n.NotifyBodyPartAttached(It.Is<BodyPartAttachedDTO>(d =>
                d.PlayerUserId == 1 && d.DinoInstanceId == 1
            )), Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPart_LogsActionInEnglish()
        {
            // Arrange
            testPlayer.AddDino(new DinoInstance(1, CreateHeadCard(1, 5, ArmyType.Sand)));
            testPlayer.AddCard(CreateTorsoCard(2, 5, ArmyType.Sand));

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });

            // Assert
            mockLoggerHelper.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("attached card"))), Times.AtLeastOnce);
        }

        // ==========================================
        // HELPERS CON JOINTS (CLAVE PARA QUE SUME EL PODER)
        // ==========================================

        private CardInGame CreateHeadCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id);
            type.GetProperty("Power").SetValue(card, power);
            type.GetProperty("Element").SetValue(card, element);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Head);
            // La cabeza DEBE tener unión hacia abajo
            type.GetProperty("HasBottomJoint").SetValue(card, true);
            return card;
        }

        private CardInGame CreateTorsoCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id);
            type.GetProperty("Power").SetValue(card, power);
            type.GetProperty("Element").SetValue(card, element);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Torso);
            // El torso DEBE tener todas las uniones para aceptar brazos y piernas
            type.GetProperty("HasTopJoint").SetValue(card, true);
            type.GetProperty("HasBottomJoint").SetValue(card, true);
            type.GetProperty("HasLeftJoint").SetValue(card, true);
            type.GetProperty("HasRightJoint").SetValue(card, true);
            return card;
        }

        private CardInGame CreateLegsCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id);
            type.GetProperty("Power").SetValue(card, power);
            type.GetProperty("Element").SetValue(card, element);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Legs);
            type.GetProperty("HasTopJoint").SetValue(card, true);
            return card;
        }
    }
}