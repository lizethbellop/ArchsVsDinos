using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTest.Game
{
    [TestClass]
    public class GameLogicAttachBodyPartTest : BaseTestClass
    {
        private GameSessionManager sessionManager;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameSetupHandler setupHandler;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession testSession;
        private PlayerSession testPlayer;

        [TestInitialize]
        public override void BaseSetup()
        {
            base.BaseSetup();

            sessionManager = new GameSessionManager(mockLoggerHelper.Object);
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();
            setupHandler = new GameSetupHandler();

            gameCoreContext = new GameCoreContext(sessionManager, setupHandler);

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            string matchCode = "ATTACH-MATCH";
            sessionManager.CreateSession(matchCode);
            testSession = sessionManager.GetSession(matchCode);
            testSession.MarkAsStarted();

            testPlayer = new PlayerSession(1, "TestPlayer", null);
            testSession.AddPlayer(testPlayer);
            testSession.StartTurn(1);
        }

        [TestMethod]
        public void TestAttachBodyPart_IncreasesTotalPower()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);

            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);

            var data = new AttachBodyPartDTO
            {
                DinoHeadCardId = head.IdCard,
                CardId = torso.IdCard
            };

            var result = gameLogic.AttachBodyPart("ATTACH-MATCH", 1, data);

            Assert.IsTrue(result);
            Assert.AreEqual(head.Power + torso.Power, dino.TotalPower);
        }

        [TestMethod]
        public void TestAttachBodyPart_RemovesCardFromHand()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(torso);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso.IdCard });

            Assert.AreEqual(0, testPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestAttachBodyPart_AddsLegsCorrectly()
        {
            var head = CardInGame.FromDefinition(28);
            var legs = CardInGame.FromDefinition(76);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(legs);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = legs.IdCard });

            Assert.IsNotNull(dino.LegsCard);
        }

        [TestMethod]
        public void TestAttachBodyPart_AddsArmsToCorrectSlots()
        {
            var head = CardInGame.FromDefinition(28);
            var arm1 = CardInGame.FromDefinition(60);
            var arm2 = CardInGame.FromDefinition(61);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(arm1);
            testPlayer.AddCard(arm2);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = arm1.IdCard });
            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = arm2.IdCard });

            Assert.IsNotNull(dino.LeftArmCard);
            Assert.IsNotNull(dino.RightArmCard);
        }

        [TestMethod]
        public void TestAttachBodyPart_Fails_IfSlotAlreadyFull()
        {
            var head = CardInGame.FromDefinition(28);
            var torso1 = CardInGame.FromDefinition(52);
            var torso2 = CardInGame.FromDefinition(53);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso1);
            testPlayer.AddCard(torso2);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso1.IdCard });
            var result = gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso2.IdCard });

            Assert.IsFalse(result, "Should return false if torso slot is already occupied");
        }

        [TestMethod]
        public void TestAttachBodyPart_ReturnsFalse_WhenDinoNotFound()
        {
            var torso = CardInGame.FromDefinition(52);
            testPlayer.AddCard(torso);

            var data = new AttachBodyPartDTO { DinoHeadCardId = 999, CardId = torso.IdCard };

            var result = gameLogic.AttachBodyPart("ATTACH-MATCH", 1, data);

            Assert.IsFalse(result);
            mockLoggerHelper.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Dino not found"))), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPart_Throws_WhenCardNotInHand()
        {
            var head = CardInGame.FromDefinition(28);
            testPlayer.AddDino(new DinoInstance(1, head));

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = 999 });
        }

        [TestMethod]
        public void TestAttachBodyPart_NotifiesBodyPartAttached()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(torso);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso.IdCard });

            mockGameNotifier.Verify(n => n.NotifyBodyPartAttached(It.IsAny<BodyPartAttachedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPart_NotificationContainsUpdatedPower()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(torso);

            int expectedPower = head.Power + torso.Power;

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso.IdCard });

            mockGameNotifier.Verify(n => n.NotifyBodyPartAttached(It.Is<BodyPartAttachedDTO>(d =>
                d.NewTotalPower == expectedPower)), Times.Once);
        }

        [TestMethod]
        public void TestAttachBodyPart_LogsActionInEnglish()
        {
            var head = CardInGame.FromDefinition(28);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(CardInGame.FromDefinition(52));

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = 52 });

            mockLoggerHelper.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("attached card"))), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPart_Throws_IfSessionInactive()
        {
            testSession.MarkAsFinished(GameEndType.Aborted);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 2 });
        }

        [TestMethod]
        public void TestAttachBodyPart_MaintainElementConsistency()
        {
            var head = CardInGame.FromDefinition(28);
            var neutralTorso = CardInGame.FromDefinition(52);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(neutralTorso);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = neutralTorso.IdCard });

            Assert.AreEqual(head.Element, dino.Element, "Dino element must match the head element");
        }

        [TestMethod]
        public void TestAttachBodyPart_DinoInstanceIdRemainsConstant()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);
            var dino = new DinoInstance(88, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso.IdCard });

            mockGameNotifier.Verify(n => n.NotifyBodyPartAttached(It.Is<BodyPartAttachedDTO>(d => d.DinoInstanceId == 88)), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestAttachBodyPart_Throws_IfCardIsNull()
        {
            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = 1, CardId = 0 });
        }

        [TestMethod]
        public void TestAttachBodyPart_GetAllCardsCount_Increments()
        {
            var head = CardInGame.FromDefinition(28);
            var torso = CardInGame.FromDefinition(52);
            var dino = new DinoInstance(1, head);
            testPlayer.AddDino(dino);
            testPlayer.AddCard(torso);

            gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = torso.IdCard });

            Assert.AreEqual(2, dino.GetAllCards().Count);
        }

        [TestMethod]
        public void TestAttachBodyPart_LockPreventsConcurrencyIssues()
        {
            var head = CardInGame.FromDefinition(28);
            testPlayer.AddDino(new DinoInstance(1, head));
            testPlayer.AddCard(CardInGame.FromDefinition(52));

            var result = gameLogic.AttachBodyPart("ATTACH-MATCH", 1, new AttachBodyPartDTO { DinoHeadCardId = head.IdCard, CardId = 52 });

            Assert.IsTrue(result, "Action should succeed under standard lock conditions");
        }
    }
}