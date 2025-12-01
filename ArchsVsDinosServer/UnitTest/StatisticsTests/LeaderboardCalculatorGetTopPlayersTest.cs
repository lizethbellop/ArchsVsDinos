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
    public class LeaderboardCalculatorGetTopPlayersTest : BaseTestClass
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
        public void TestGetTopPlayersSuccessfulRetrieval()
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
                    totalWins = 8,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player2" } }
                },
                new Player
                {
                    idPlayer = 3,
                    totalPoints = 600,
                    totalWins = 6,
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
                    TotalPoints = 1000,
                    TotalWins = 10
                },
                new LeaderboardEntryDTO
                {
                    Position = 2,
                    UserId = 2,
                    Username = "player2",
                    TotalPoints = 800,
                    TotalWins = 8
                },
                new LeaderboardEntryDTO
                {
                    Position = 3,
                    UserId = 3,
                    Username = "player3",
                    TotalPoints = 600,
                    TotalWins = 6
                }
            };

            var result = leaderboardCalculator.GetTopPlayers(3);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersNoPlayers()
        {
            SetupMockPlayerSet(new List<Player>());

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersExcludesZeroPoints()
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
                    totalPoints = 0,
                    totalWins = 5,
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

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersSortsByPointsThenWins()
        {
            var players = new List<Player>
            {
                new Player
                {
                    idPlayer = 1,
                    totalPoints = 1000,
                    totalWins = 5,
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
                    totalPoints = 800,
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
                    UserId = 2,
                    Username = "player2",
                    TotalPoints = 1000,
                    TotalWins = 10
                },
                new LeaderboardEntryDTO
                {
                    Position = 2,
                    UserId = 1,
                    Username = "player1",
                    TotalPoints = 1000,
                    TotalWins = 5
                },
                new LeaderboardEntryDTO
                {
                    Position = 3,
                    UserId = 3,
                    Username = "player3",
                    TotalPoints = 800,
                    TotalWins = 15
                }
            };

            var result = leaderboardCalculator.GetTopPlayers(3);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersLimitsByTopN()
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
                    totalWins = 8,
                    UserAccount = new List<UserAccount> { new UserAccount { username = "player2" } }
                },
                new Player
                {
                    idPlayer = 3,
                    totalPoints = 600,
                    totalWins = 6,
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
                    TotalPoints = 1000,
                    TotalWins = 10
                },
                new LeaderboardEntryDTO
                {
                    Position = 2,
                    UserId = 2,
                    Username = "player2",
                    TotalPoints = 800,
                    TotalWins = 8
                }
            };

            var result = leaderboardCalculator.GetTopPlayers(2);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersPlayerWithNoUserAccount()
        {
            var players = new List<Player>
            {
                new Player
                {
                    idPlayer = 1,
                    totalPoints = 1000,
                    totalWins = 10,
                    UserAccount = new List<UserAccount>()
                }
            };

            SetupMockPlayerSet(players);

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>
            {
                new LeaderboardEntryDTO
                {
                    Position = 1,
                    UserId = 1,
                    Username = "Unknown",
                    TotalPoints = 1000,
                    TotalWins = 10
                }
            };

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersPlayerWithMultipleUserAccounts()
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

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>
            {
                new LeaderboardEntryDTO
                {
                    Position = 1,
                    UserId = 1,
                    Username = "Unknown",
                    TotalPoints = 1000,
                    TotalWins = 10
                }
            };

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersArgumentNullException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new ArgumentNullException("parameter"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersDbUpdateException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new DbUpdateException("Database error"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetTopPlayersUnexpectedException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new Exception("Unexpected error"));

            List<LeaderboardEntryDTO> expectedResult = new List<LeaderboardEntryDTO>();

            var result = leaderboardCalculator.GetTopPlayers(10);

            CollectionAssert.AreEqual(expectedResult, result);
        }

    }
}
