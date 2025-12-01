using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic.Statistics;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.StatisticsTests
{
    [TestClass]
    public class LeaderboardCalculatorGetPlayerRankTest : BaseTestClass
    {
        private LeaderboardCalculator leaderboardCalculator;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            ServiceDependencies dependencies = new ServiceDependencies(
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            leaderboardCalculator = new LeaderboardCalculator(dependencies);
        }

        [TestMethod]
        public void TestGetPlayerRankPlayerExists()
        {
            var players = new List<Player>
            {
                new Player { idPlayer = 1, totalPoints = 1000, totalWins = 10 },
                new Player { idPlayer = 2, totalPoints = 800, totalWins = 8 },
                new Player { idPlayer = 3, totalPoints = 600, totalWins = 6 }
            };

            SetupMockPlayerSet(players);

            int result = leaderboardCalculator.GetPlayerRank(2);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void TestGetPlayerRankPlayerNotFound()
        {
            SetupMockPlayerSet(new List<Player>());

            int result = leaderboardCalculator.GetPlayerRank(999);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestGetPlayerRankFirstPlace()
        {
            var players = new List<Player>
            {
                new Player { idPlayer = 1, totalPoints = 1000, totalWins = 10 },
                new Player { idPlayer = 2, totalPoints = 800, totalWins = 8 }
            };

            SetupMockPlayerSet(players);

            int result = leaderboardCalculator.GetPlayerRank(1);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestGetPlayerRankTiedPointsHigherWins()
        {
            var players = new List<Player>
            {
                new Player { idPlayer = 1, totalPoints = 1000, totalWins = 15 },
                new Player { idPlayer = 2, totalPoints = 1000, totalWins = 10 },
                new Player { idPlayer = 3, totalPoints = 800, totalWins = 8 }
            };

            SetupMockPlayerSet(players);

            int result = leaderboardCalculator.GetPlayerRank(2);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void TestGetPlayerRankArgumentNullException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new ArgumentNullException("parameter"));

            int result = leaderboardCalculator.GetPlayerRank(1);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestGetPlayerRankInvalidOperationException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new InvalidOperationException("Invalid operation"));

            int result = leaderboardCalculator.GetPlayerRank(1);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestGetPlayerRankDbUpdateException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new DbUpdateException("Database error"));

            int result = leaderboardCalculator.GetPlayerRank(1);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestGetPlayerRankUnexpectedException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new Exception("Unexpected error"));

            int result = leaderboardCalculator.GetPlayerRank(1);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsSuccessfulRetrieval()
        {
            var players = new List<Player>
            {
                new Player
                {
                    idPlayer = 1,
                    totalPoints = 800,
                    totalWins = 15,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player1" } }
                },
                new Player
                {
                    idPlayer = 2,
                    totalPoints = 1000,
                    totalWins = 10,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player2" } }
                },
                new Player
                {
                    idPlayer = 3,
                    totalPoints = 600,
                    totalWins = 5,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player3" } }
                }
            };

            SetupMockPlayerSet(players);

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>
            {
                new LeaderboardEntryDTO
                {
                    Position = 1,
                    UserId = 1,
                    Username = "player1",
                    TotalPoints = 800,
                    TotalWins = 15
                },
                new LeaderboardEntryDTO
                {
                    Position = 2,
                    UserId = 2,
                    Username = "player2",
                    TotalPoints = 1000,
                    TotalWins = 10
                },
                new LeaderboardEntryDTO
                {
                    Position = 3,
                    UserId = 3,
                    Username = "player3",
                    TotalPoints = 600,
                    TotalWins = 5
                }
            };

            var result = leaderboardCalculator.GetTopPlayersByWins(3);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsNoPlayers()
        {
            SetupMockPlayerSet(new List<Player>());

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }


    }
}
