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
    public class LeaderboardCalculatorGetTopPlayersByWinsTest : BaseTestClass
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

        [TestMethod]
        public void TestGetTopPlayersByWinsExcludesZeroWins()
        {
            var players = new List<Player>
            {
                new Player
                {
                    idPlayer = 1,
                    totalPoints = 1000,
                    totalWins = 10,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player1" } }
                },
                new Player
                {
                    idPlayer = 2,
                    totalPoints = 800,
                    totalWins = 0,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player2" } }
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
                    TotalPoints = 1000,
                    TotalWins = 10
                }
            };

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsSortsByWinsThenPoints()
        {
            var players = new List<Player>
            {
                new Player
                {
                    idPlayer = 1,
                    totalPoints = 800,
                    totalWins = 10,
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
                    totalWins = 15,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player3" } }
                }
            };

            SetupMockPlayerSet(players);

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>
            {
                new LeaderboardEntryDTO
                {
                    Position = 1,
                    UserId = 3,
                    Username = "player3",
                    TotalPoints = 600,
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
                    UserId = 1,
                    Username = "player1",
                    TotalPoints = 800,
                    TotalWins = 10
                }
            };

            var result = leaderboardCalculator.GetTopPlayersByWins(3);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsPlayerWithMultipleUserAccounts()
        {
            var players = new List<Player>
    {
        new Player
        {
            idPlayer = 1,
            totalPoints = 1000,
            totalWins = 10,
            UserAccount = new List<UserAccount>
            {
                new UserAccount { username = "user1" },
                new UserAccount { username = "user2" }
            }
        }
    };

            SetupMockPlayerSet(players);

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            Assert.AreEqual(1, result.Count, "There should be multiple user accounts");
            Assert.AreEqual(1, result[0].UserId, "The UserId doesn't match");
            Assert.AreEqual("Unknown", result[0].Username, "Username shoul to be 'Unknown' for the excpetion multiple accounts");
            Assert.AreEqual(1000, result[0].TotalPoints);
            Assert.AreEqual(10, result[0].TotalWins);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsArgumentNullException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new ArgumentNullException("parameter"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsDbUpdateException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new DbUpdateException("Database error"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersByWinsUnexpectedException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new Exception("Unexpected error"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayersByWins(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

    }
}
