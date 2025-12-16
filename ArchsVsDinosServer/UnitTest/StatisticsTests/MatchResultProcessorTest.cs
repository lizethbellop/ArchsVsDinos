using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic.Statistics;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.StatisticsTests
{
    [TestClass]
    public class MatchResultProcessorTest : BaseTestClass
    {
        /*private MatchResultProcessor matchResultProcessor;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            ServiceDependencies dependencies = new ServiceDependencies(
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            matchResultProcessor = new MatchResultProcessor(dependencies);
        }

        [TestMethod]
        public void TestProcessMatchResultsMatchNotFound()
        {
            var matchResult = new MatchResultDTO
            {
                MatchId = "NOTFOUND",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>()
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch>());

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsSuccessfulProcessing()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = DateTime.Now.AddDays(-1)
            };

            var player1 = new Player
            {
                idPlayer = 1,
                totalMatches = 5,
                totalWins = 3,
                totalLosses = 2,
                totalPoints = 500
            };

            var player2 = new Player
            {
                idPlayer = 2,
                totalMatches = 5,
                totalWins = 2,
                totalLosses = 3,
                totalPoints = 400
            };

            var participant1 = new MatchParticipants
            {
                idMatchParticipant = 1,
                idGeneralMatch = 1,
                idPlayer = 1,
                points = 0,
                isWinner = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            var participant2 = new MatchParticipants
            {
                idMatchParticipant = 2,
                idGeneralMatch = 1,
                idPlayer = 2,
                points = 0,
                isWinner = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = matchDate,
                PlayerResults = new List<PlayerMatchResult>
            {
                new PlayerMatchResult
                {
                    UserId = 1,
                    Points = 150,
                    IsWinner = true,
                    ArchaeologistsEliminated = 5,
                    SupremeBossesEliminated = 2
                },
                new PlayerMatchResult
                {
                    UserId = 2,
                    Points = 100,
                    IsWinner = false,
                    ArchaeologistsEliminated = 3,
                    SupremeBossesEliminated = 1
                }
            }
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant1, participant2 });
            SetupMockPlayerSet(new List<Player> { player1, player2 });

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsParticipantNotFound()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = DateTime.Now.AddDays(-1)
            };

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>
            {
                new PlayerMatchResult
                {
                    UserId = 999,
                    Points = 150,
                    IsWinner = true
                }
            }
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants>());
            SetupMockPlayerSet(new List<Player>());

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsPlayerNotFoundInStatisticsUpdate()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = DateTime.Now.AddDays(-1)
            };

            var participant = new MatchParticipants
            {
                idMatchParticipant = 1,
                idGeneralMatch = 1,
                idPlayer = 1,
                points = 0,
                isWinner = false
            };

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>
            {
                new PlayerMatchResult
                {
                    UserId = 1,
                    Points = 150,
                    IsWinner = true
                }
            }
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant });
            SetupMockPlayerSet(new List<Player>());

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsInvalidOperationException()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001"
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new InvalidOperationException("Invalid operation"));

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>()
            };

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsEntityException()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001"
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new EntityException("Entity error"));

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>()
            };

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsArgumentNullException()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001"
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new ArgumentNullException("parameter"));

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>()
            };

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestProcessMatchResultsUnexpectedException()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001"
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new Exception("Unexpected error"));

            var matchResult = new MatchResultDTO
            {
                MatchId = "MATCH001",
                MatchDate = DateTime.Now,
                PlayerResults = new List<PlayerMatchResult>()
            };

            bool result = matchResultProcessor.ProcessMatchResults(matchResult);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsMatchNotFound()
        {
            SetupMockGeneralMatchSet(new List<GeneralMatch>());

            GameStatisticsDTO expectedResult = new GameStatisticsDTO();

            var result = matchResultProcessor.GetMatchStatistics("NOTFOUND");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsNoParticipants()
        {
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = DateTime.Now
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants>());

            GameStatisticsDTO expectedResult = new GameStatisticsDTO();

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsSuccessfulRetrieval()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var player1 = new Player
            {
                idPlayer = 1,
                UserAccount = new List<UserAccount>
            {
                new UserAccount { idUser = 1, username = "player1" }
            }
            };

            var player2 = new Player
            {
                idPlayer = 2,
                UserAccount = new List<UserAccount>
            {
                new UserAccount { idUser = 2, username = "player2" }
            }
            };

            var participant1 = new MatchParticipants
            {
                idMatchParticipant = 1,
                idGeneralMatch = 1,
                idPlayer = 1,
                points = 150,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 5,
                supremeBossesEliminated = 2
            };

            var participant2 = new MatchParticipants
            {
                idMatchParticipant = 2,
                idGeneralMatch = 1,
                idPlayer = 2,
                points = 100,
                isWinner = false,
                isDefeated = false,
                archaeologistsEliminated = 3,
                supremeBossesEliminated = 1
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant1, participant2 });
            SetupMockPlayerSet(new List<Player> { player1, player2 });

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 1,
                    Username = "player1",
                    Position = 1,
                    Points = 150,
                    IsWinner = true,
                    ArchaeologistsEliminated = 5,
                    SupremeBossesEliminated = 2
                },
                new PlayerMatchStatsDTO
                {
                    UserId = 2,
                    Username = "player2",
                    Position = 2,
                    Points = 100,
                    IsWinner = false,
                    ArchaeologistsEliminated = 3,
                    SupremeBossesEliminated = 1
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsWithTiedPositions()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var player1 = new Player
            {
                idPlayer = 1,
                UserAccount = new List<UserAccount> { new UserAccount { username = "player1" } }
            };

            var player2 = new Player
            {
                idPlayer = 2,
                UserAccount = new List<UserAccount> { new UserAccount { username = "player2" } }
            };

            var player3 = new Player
            {
                idPlayer = 3,
                UserAccount = new List<UserAccount> { new UserAccount { username = "player3" } }
            };

            var participant1 = new MatchParticipants
            {
                idPlayer = 1,
                idGeneralMatch = 1,
                points = 100,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            var participant2 = new MatchParticipants
            {
                idPlayer = 2,
                idGeneralMatch = 1,
                points = 100,
                isWinner = false,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            var participant3 = new MatchParticipants
            {
                idPlayer = 3,
                idGeneralMatch = 1,
                points = 50,
                isWinner = false,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant1, participant2, participant3 });
            SetupMockPlayerSet(new List<Player> { player1, player2, player3 });

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 1,
                    Username = "player1",
                    Position = 1,
                    Points = 100,
                    IsWinner = true,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                },
                new PlayerMatchStatsDTO
                {
                    UserId = 2,
                    Username = "player2",
                    Position = 1,
                    Points = 100,
                    IsWinner = false,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                },
                new PlayerMatchStatsDTO
                {
                    UserId = 3,
                    Username = "player3",
                    Position = 3,
                    Points = 50,
                    IsWinner = false,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsExcludesDefeatedPlayers()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var player1 = new Player
            {
                idPlayer = 1,
                UserAccount = new List<UserAccount> { new UserAccount { username = "player1" } }
            };

            var participant1 = new MatchParticipants
            {
                idPlayer = 1,
                idGeneralMatch = 1,
                points = 100,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            var participant2 = new MatchParticipants
            {
                idPlayer = 2,
                idGeneralMatch = 1,
                points = 50,
                isWinner = false,
                isDefeated = true,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant1, participant2 });
            SetupMockPlayerSet(new List<Player> { player1 });

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 1,
                    Username = "player1",
                    Position = 1,
                    Points = 100,
                    IsWinner = true,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsPlayerNotFound()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var participant = new MatchParticipants
            {
                idPlayer = 999,
                idGeneralMatch = 1,
                points = 100,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant });
            SetupMockPlayerSet(new List<Player>());

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 999,
                    Username = "Unknown",
                    Position = 1,
                    Points = 100,
                    IsWinner = true,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsPlayerWithNoUserAccount()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var player = new Player
            {
                idPlayer = 1,
                UserAccount = new List<UserAccount>()
            };

            var participant = new MatchParticipants
            {
                idPlayer = 1,
                idGeneralMatch = 1,
                points = 100,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant });
            SetupMockPlayerSet(new List<Player> { player });

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 1,
                    Username = "Unknown",
                    Position = 1,
                    Points = 100,
                    IsWinner = true,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsPlayerWithMultipleUserAccounts()
        {
            var matchDate = DateTime.Now;
            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                matchCode = "MATCH001",
                date = matchDate
            };

            var player = new Player
            {
                idPlayer = 1,
                UserAccount = new List<UserAccount>
            {
                new UserAccount { username = "user1" },
                new UserAccount { username = "user2" }
            }
            };

            var participant = new MatchParticipants
            {
                idPlayer = 1,
                idGeneralMatch = 1,
                points = 100,
                isWinner = true,
                isDefeated = false,
                archaeologistsEliminated = 0,
                supremeBossesEliminated = 0
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(new List<MatchParticipants> { participant });
            SetupMockPlayerSet(new List<Player> { player });

            GameStatisticsDTO expectedResult = new GameStatisticsDTO
            {
                MatchCode = "MATCH001",
                MatchDate = matchDate,
                PlayerStats = new PlayerMatchStatsDTO[]
                {
                new PlayerMatchStatsDTO
                {
                    UserId = 1,
                    Username = "Unknown",
                    Position = 1,
                    Points = 100,
                    IsWinner = true,
                    ArchaeologistsEliminated = 0,
                    SupremeBossesEliminated = 0
                }
                }
            };

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsNullReferenceException()
        {
            mockDbContext.Setup(c => c.GeneralMatch).Throws(new NullReferenceException("Null reference"));

            GameStatisticsDTO expectedResult = new GameStatisticsDTO();

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsInvalidOperationException()
        {
            mockDbContext.Setup(c => c.GeneralMatch).Throws(new InvalidOperationException("Invalid operation"));

            GameStatisticsDTO expectedResult = new GameStatisticsDTO();

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchStatisticsUnexpectedException()
        {
            mockDbContext.Setup(c => c.GeneralMatch).Throws(new Exception("Unexpected error"));

            GameStatisticsDTO expectedResult = new GameStatisticsDTO();

            var result = matchResultProcessor.GetMatchStatistics("MATCH001");

            Assert.AreEqual(expectedResult, result);
        }*/

    }
}
