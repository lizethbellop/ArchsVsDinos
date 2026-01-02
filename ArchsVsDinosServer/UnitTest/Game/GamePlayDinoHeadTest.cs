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
    public class GamePlayDinoHeadTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession gameSession;
        private PlayerSession playerSession;

        [TestInitialize]
        public void SetupPlayDinoHeadTests()
        {
            BaseSetup();

            mockSessionManager = new Mock<GameSessionManager>(mockLoggerHelper.Object);
            mockSetupHandler = new Mock<GameSetupHandler>();
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();

            gameCoreContext = new GameCoreContext(
                mockSessionManager.Object,
                mockSetupHandler.Object
            );

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            var mockCentralBoard = new CentralBoard();
            gameSession = new GameSession("TEST-MATCH", mockCentralBoard, mockLoggerHelper.Object);
            gameSession.MarkAsStarted();

            playerSession = new PlayerSession(1, "TestPlayer", null);
            gameSession.AddPlayer(playerSession);
            gameSession.StartTurn(1);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(gameSession);
        }

        [TestMethod]
        public void TestPlayDinoHeadReturnsDinoInstance()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestPlayDinoHeadReturnsCorrectDinoInstanceId()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(1, result.DinoInstanceId);
        }

        [TestMethod]
        public void TestPlayDinoHeadReturnsCorrectElement()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(ArmyType.Sand, result.Element);
        }

        [TestMethod]
        public void TestPlayDinoHeadRemovesCardFromHand()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(0, playerSession.Hand.Count);
        }

        [TestMethod]
        public void TestPlayDinoHeadAddsDinoToPlayer()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(1, playerSession.Dinos.Count);
        }

        [TestMethod]
        public void TestPlayDinoHeadDinoHasCorrectHeadCard()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(28, result.HeadCard.IdCard);
        }

        [TestMethod]
        public void TestPlayDinoHeadNotifiesDinoPlayed()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyDinoHeadPlayed(It.IsAny<DinoPlayedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestPlayDinoHeadNotificationContainsCorrectMatchCode()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyDinoHeadPlayed(
                It.Is<DinoPlayedDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestPlayDinoHeadNotificationContainsCorrectPlayerId()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyDinoHeadPlayed(
                It.Is<DinoPlayedDTO>(dto => dto.PlayerUserId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestPlayDinoHeadNotificationContainsCorrectDinoInstanceId()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyDinoHeadPlayed(
                It.Is<DinoPlayedDTO>(dto => dto.DinoInstanceId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestPlayDinoHeadWithWaterElement()
        {
            // Arrange
            var headCard = CreateHeadCard(36, 0, ArmyType.Water);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 36);

            // Assert
            Assert.AreEqual(ArmyType.Water, result.Element);
        }

        [TestMethod]
        public void TestPlayDinoHeadWithWindElement()
        {
            // Arrange
            var headCard = CreateHeadCard(44, 0, ArmyType.Wind);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 44);

            // Assert
            Assert.AreEqual(ArmyType.Wind, result.Element);
        }

        [TestMethod]
        public void TestPlayDinoHeadIncrementsDinoId()
        {
            // Arrange
            var headCard1 = CreateHeadCard(28, 0, ArmyType.Sand);
            var headCard2 = CreateHeadCard(29, 1, ArmyType.Sand);
            playerSession.AddCard(headCard1);
            playerSession.AddCard(headCard2);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 29);

            // Assert
            Assert.AreEqual(2, result.DinoInstanceId);
        }

        [TestMethod]
        public void TestPlayDinoHeadDinoHasCorrectPower()
        {
            // Arrange
            var headCard = CreateHeadCard(35, 3, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 35);

            // Assert
            Assert.AreEqual(3, result.TotalPower);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPlayDinoHeadThrowsWhenCardNotInHand()
        {
            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 999);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPlayDinoHeadThrowsWhenCardIsNotHead()
        {
            // Arrange
            var torsoCard = CreateTorsoCard(52, 0, ArmyType.None);
            playerSession.AddCard(torsoCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 52);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPlayDinoHeadThrowsWhenSessionNotFound()
        {
            // Act
            gameLogic.PlayDinoHead("INVALID-MATCH", 1, 28);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPlayDinoHeadThrowsWhenPlayerNotFound()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 999, 28);
        }

        [TestMethod]
        public void TestPlayDinoHeadMultipleDinosForSamePlayer()
        {
            // Arrange
            var headCard1 = CreateHeadCard(28, 0, ArmyType.Sand);
            var headCard2 = CreateHeadCard(36, 0, ArmyType.Water);
            playerSession.AddCard(headCard1);
            playerSession.AddCard(headCard2);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 36);

            // Assert
            Assert.AreEqual(2, playerSession.Dinos.Count);
        }

        [TestMethod]
        public void TestPlayDinoHeadDinoCanBeRetrievedFromPlayer()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);
            var dino = playerSession.GetDinoByHeadCardId(28);

            // Assert
            Assert.IsNotNull(dino);
        }

        [TestMethod]
        public void TestPlayDinoHeadRetrievedDinoHasCorrectId()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);
            var dino = playerSession.GetDinoByHeadCardId(28);

            // Assert
            Assert.AreEqual(28, dino.HeadCard.IdCard);
        }

        [TestMethod]
        public void TestPlayDinoHeadWithHighPowerCard()
        {
            // Arrange
            var headCard = CreateHeadCard(35, 3, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 35);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestPlayDinoHeadWithZeroPowerCard()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(0, result.TotalPower);
        }

        [TestMethod]
        public void TestPlayDinoHeadPreservesOtherCardsInHand()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            var otherCard = CreateTorsoCard(52, 0, ArmyType.None);
            playerSession.AddCard(headCard);
            playerSession.AddCard(otherCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(1, playerSession.Hand.Count);
        }

        [TestMethod]
        public void TestPlayDinoHeadCardRemovedFromHand()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.IsNull(playerSession.GetCardById(28));
        }

        [TestMethod]
        public void TestPlayDinoHeadDinoHasOnlyHeadCard()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            playerSession.AddCard(headCard);

            // Act
            var result = gameLogic.PlayDinoHead("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(1, result.GetAllCards().Count);
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
    }
}
