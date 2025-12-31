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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Game
{
    [TestClass]
    public class GameLogicAttachBodyPartTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession testSession;
        private PlayerSession testPlayer;

        [TestInitialize]
        public void SetupAttachBodyPartTests()
        {
            BaseSetup();

            mockSessionManager = new Mock<GameSessionManager>(mockLoggerHelper.Object);
            mockSetupHandler = new Mock<GameSetupHandler>();
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();

            // Create GameCoreContext
            gameCoreContext = new GameCoreContext(
                mockSessionManager.Object,
                mockSetupHandler.Object
            );

            // Setup game logic dependencies using constructor
            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            // Setup test session
            var mockCentralBoard = new CentralBoard();
            testSession = new GameSession("TEST-MATCH", mockCentralBoard, mockLoggerHelper.Object);
            testSession.MarkAsStarted();

            testPlayer = new PlayerSession(1, "TestPlayer", null);
            testSession.AddPlayer(testPlayer);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(testSession);
        }

        [TestMethod]
        public void TestAttachBodyPartSuccess()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            var result = gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestAttachBodyPartRemovesCardFromHand()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            Assert.AreEqual(0, testPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestAttachBodyPartAddsCardToDino()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            Assert.AreEqual(2, dino.GetAllCards().Count);
        }

        [TestMethod]
        public void TestAttachBodyPartNotifiesClients()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyBodyPartAttached(It.IsAny<BodyPartAttachedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPartNotificationContainsCorrectMatchCode()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyBodyPartAttached(
                It.Is<BodyPartAttachedDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPartNotificationContainsCorrectUserId()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyBodyPartAttached(
                It.Is<BodyPartAttachedDTO>(dto => dto.PlayerUserId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPartNotificationContainsCorrectDinoId()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyBodyPartAttached(
                It.Is<BodyPartAttachedDTO>(dto => dto.DinoInstanceId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPartNotificationContainsUpdatedPower()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyBodyPartAttached(
                It.Is<BodyPartAttachedDTO>(dto => dto.NewTotalPower == 8)),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestAttachBodyPartThrowsWhenDataIsNull()
        {
            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPartThrowsWhenCardNotInHand()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 999
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPartThrowsWhenDinoNotFound()
        {
            // Arrange
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);
            testPlayer.AddCard(bodyCard);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 999,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPartThrowsWhenCardIsArchCard()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var archCard = CreateArchCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(archCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPartThrowsWhenSessionNotFound()
        {
            // Arrange
            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("INVALID-MATCH", 1, attachData);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPartThrowsWhenPlayerNotFound()
        {
            // Arrange
            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 999, attachData);
        }

        [TestMethod]
        public void TestAttachBodyPartLogsSuccessfulAttachment()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("attached card") &&
                s.Contains("TEST-MATCH"))),
                Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPartIncreasesTotalPower()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var bodyCard = CreateTorsoCard(2, 3, ArmyType.Sand);

            testPlayer.AddCard(bodyCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 2
            };

            // Act
            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            // Assert
            Assert.AreEqual(8, dino.TotalPower);
        }

        [TestMethod]
        public void TestAttachBodyPartWithLegsCard()
        {
            // Arrange
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var legsCard = CreateLegsCard(3, 4, ArmyType.Sand);

            testPlayer.AddCard(legsCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 3
            };

            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            Assert.AreEqual(3, dino.LegsCard.IdCard);
        }

        [TestMethod]
        public void TestAttachBodyPartWithArmsCard()
        {
            var headCard = CreateHeadCard(1, 5, ArmyType.Sand);
            var armsCard = CreateArmsCard(4, 2, ArmyType.Sand);

            testPlayer.AddCard(armsCard);
            var dino = new DinoInstance(1, headCard);
            testPlayer.AddDino(dino);

            var attachData = new AttachBodyPartDTO
            {
                DinoHeadCardId = 1,
                CardId = 4
            };

            gameLogic.AttachBodyPart("TEST-MATCH", 1, attachData);

            Assert.IsTrue(dino.LeftArmCard != null || dino.RightArmCard != null);
        }

        private CardInGame CreateHeadCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Head, null);
            type.GetProperty("HasBottomJoint").SetValue(card, true, null);
            return card;
        }

        private CardInGame CreateTorsoCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Torso, null);
            type.GetProperty("HasTopJoint").SetValue(card, true, null);
            type.GetProperty("HasBottomJoint").SetValue(card, true, null);
            type.GetProperty("HasLeftJoint").SetValue(card, true, null);
            type.GetProperty("HasRightJoint").SetValue(card, true, null);
            return card;
        }

        private CardInGame CreateLegsCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Legs, null);
            type.GetProperty("HasTopJoint").SetValue(card, true, null);
            return card;
        }

        private CardInGame CreateArmsCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Arms, null);
            type.GetProperty("HasLeftJoint").SetValue(card, true, null);
            type.GetProperty("HasRightJoint").SetValue(card, true, null);
            return card;
        }

        private CardInGame CreateArchCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.None, null);
            return card;
        }
    }
}
