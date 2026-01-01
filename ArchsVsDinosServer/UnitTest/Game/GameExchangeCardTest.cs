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
    public class GameExchangeCardTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession gameSession;
        private PlayerSession playerA;
        private PlayerSession playerB;

        [TestInitialize]
        public void SetupExchangeCardTests()
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

            playerA = new PlayerSession(1, "PlayerA", null);
            playerB = new PlayerSession(2, "PlayerB", null);

            gameSession.AddPlayer(playerA);
            gameSession.AddPlayer(playerB);
            gameSession.StartTurn(1);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(gameSession);
        }

        [TestMethod]
        public void TestExchangeCardReturnsTrue()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestExchangeCardSwapsCardsCorrectly()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(53, playerA.Hand[0].IdCard);
        }

        [TestMethod]
        public void TestExchangeCardPlayerBReceivesCorrectCard()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(52, playerB.Hand[0].IdCard);
        }

        [TestMethod]
        public void TestExchangeCardRemovesCardFromPlayerA()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsNull(playerA.GetCardById(52));
        }

        [TestMethod]
        public void TestExchangeCardRemovesCardFromPlayerB()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsNull(playerB.GetCardById(53));
        }

        [TestMethod]
        public void TestExchangeCardConsumesMoves()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            int initialMoves = gameSession.RemainingMoves;

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(initialMoves - 1, gameSession.RemainingMoves);
        }

        [TestMethod]
        public void TestExchangeCardNotifiesExchange()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardExchanged(It.IsAny<CardExchangedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestExchangeCardNotificationContainsCorrectMatchCode()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardExchanged(
                It.Is<CardExchangedDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestExchangeCardNotificationContainsPlayerAId()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardExchanged(
                It.Is<CardExchangedDTO>(dto => dto.PlayerAUserId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestExchangeCardNotificationContainsPlayerBId()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardExchanged(
                It.Is<CardExchangedDTO>(dto => dto.PlayerBUserId == 2)),
                Times.Once);
        }

        [TestMethod]
        public void TestExchangeCardWithLegsCards()
        {
            // Arrange
            var cardA = CreateLegsCard(79, 0, ArmyType.None);
            var cardB = CreateLegsCard(80, 0, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 79,
                RequestedCardId = 80
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestExchangeCardWithArmsCards()
        {
            // Arrange
            var cardA = CreateArmsCard(69, 0, ArmyType.None);
            var cardB = CreateArmsCard(70, 0, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 69,
                RequestedCardId = 70
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestExchangeCardReturnsFalseWhenNoMovesRemaining()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);
            gameSession.ConsumeMoves(3);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestExchangeCardReturnsFalseWhenCardANotInHand()
        {
            // Arrange
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 999,
                RequestedCardId = 53
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestExchangeCardReturnsFalseWhenCardBNotInHand()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            playerA.AddCard(cardA);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 999
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestExchangeCardReturnsFalseWhenCardTypesDifferent()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateLegsCard(79, 0, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 79
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestExchangeCardReturnsFalseWhenPlayerBNotFound()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            playerA.AddCard(cardA);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 999,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            var result = gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestExchangeCardMaintainsHandSize()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(1, playerA.Hand.Count);
        }

        [TestMethod]
        public void TestExchangeCardPlayerBHandSizeRemainsSame()
        {
            // Arrange
            var cardA = CreateTorsoCard(52, 0, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(1, playerB.Hand.Count);
        }

        [TestMethod]
        public void TestExchangeCardWithMultipleCardsInHand()
        {
            // Arrange
            var cardA1 = CreateTorsoCard(52, 0, ArmyType.None);
            var cardA2 = CreateTorsoCard(54, 1, ArmyType.None);
            var cardB = CreateTorsoCard(53, 1, ArmyType.None);
            playerA.AddCard(cardA1);
            playerA.AddCard(cardA2);
            playerB.AddCard(cardB);

            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 52,
                RequestedCardId = 53
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(2, playerA.Hand.Count);
        }

        [TestMethod]
        public void TestExchangeCardDoesNotNotifyWhenFailed()
        {
            // Arrange
            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 999,
                RequestedCardId = 999
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardExchanged(It.IsAny<CardExchangedDTO>()), Times.Never);
        }

        [TestMethod]
        public void TestExchangeCardDoesNotConsumeMovesWhenFailed()
        {
            // Arrange
            int initialMoves = gameSession.RemainingMoves;
            var exchangeData = new ExchangeCardDTO
            {
                TargetUserId = 2,
                OfferedCardId = 999,
                RequestedCardId = 999
            };

            // Act
            gameLogic.ExchangeCard("TEST-MATCH", 1, exchangeData);

            // Assert
            Assert.AreEqual(initialMoves, gameSession.RemainingMoves);
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
    }
}
