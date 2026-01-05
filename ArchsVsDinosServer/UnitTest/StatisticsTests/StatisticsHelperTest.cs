using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic.Statistics;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.StatisticsTests
{
    [TestClass]
    public class StatisticsHelperTest : BaseTestClass
    {
        private StatisticsHelper statisticsHelper;
        
        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();
            

            ServiceDependencies dependencies = new ServiceDependencies(
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            statisticsHelper = new StatisticsHelper(dependencies);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsPlayerExists()
        {
            int userId = 1;
            int playerId = 10;
            var userAccount = new UserAccount 
            { 
                idUser = userId,
                idPlayer = playerId, 
                username = "testuser" 
            };

            var player = new Player 
            { 
                idPlayer = playerId, 
                totalWins = 10,
                totalMatches = 15,
                totalPoints = 150 
            };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });

            var result = statisticsHelper.GetPlayerStatistics(userId);

            Assert.IsNotNull(result);
            Assert.AreEqual("testuser", result.Username);
            Assert.AreEqual(66.67, result.WinRate);
            Assert.AreEqual(150, result.TotalPoints);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsPlayerNotFound()
        {
            SetupMockPlayerSet(new List<Player>());

            PlayerStatisticsDTO expectedResult = new PlayerStatisticsDTO();

            var result = statisticsHelper.GetPlayerStatistics(999);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsZeroMatches()
        {
            int userId = 1;
            var userAccount = new UserAccount { idUser = userId, idPlayer = 1, username = "newplayer" };
            var player = new Player { idPlayer = 1, totalMatches = 0, totalWins = 0 };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });

            var result = statisticsHelper.GetPlayerStatistics(userId);

            Assert.AreEqual(0, result.WinRate);
            Assert.AreEqual("newplayer", result.Username);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsNoUserAccount()
        {
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());

            var result = statisticsHelper.GetPlayerStatistics(1);
            Assert.AreEqual(0, result.UserId);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsMultipleUserAccounts()
        {
            int userId = 1;
            var userAccounts = new List<UserAccount> {
                new UserAccount 
                {
                    idUser = userId,
                    idPlayer = 1, 
                    username = "user1" 
                },
                new UserAccount 
                { 
                    idUser = 2,
                    idPlayer = 1, 
                    username = "user2" 
                }
            };
            var player = new Player 
            { 
                idPlayer = 1,
                totalPoints = 80 
            };

            SetupMockUserSet(userAccounts);
            SetupMockPlayerSet(new List<Player> { player });

            var result = statisticsHelper.GetPlayerStatistics(userId);

            Assert.AreEqual("user1", result.Username);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsDbUpdateException()
        {
            mockDbContext.Setup(c => c.Player).Throws(new DbUpdateException("Database error"));

            PlayerStatisticsDTO expectedResult = new PlayerStatisticsDTO();

            var result = statisticsHelper.GetPlayerStatistics(1);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsUnexpectedError()
        {
            mockDbContext.Setup(c => c.Player).Throws(new Exception("Unexpected error"));

            PlayerStatisticsDTO expectedResult = new PlayerStatisticsDTO();

            var result = statisticsHelper.GetPlayerStatistics(1);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetMatchHistoryNoMatches()
        {
            SetupMockMatchParticipantsSet(new List<MatchParticipants>());

            var result = statisticsHelper.GetMatchHistory(1, 5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetMatchHistoryGeneralMatchNull()
        {
            var matchWithoutGeneral = new MatchParticipants
            {
                idMatchParticipant = 1,
                idPlayer = 1,
                idGeneralMatch = 1,
                points = 50,
                isWinner = false,
                GeneralMatch = null
            };

            SetupMockMatchParticipantsSet(new List<MatchParticipants> { matchWithoutGeneral });

            var result = statisticsHelper.GetMatchHistory(1, 5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetMatchHistoryDbUpdateException()
        {
            mockDbContext.Setup(c => c.MatchParticipants).Throws(new DbUpdateException("Database error"));

            var result = statisticsHelper.GetMatchHistory(1, 5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetMatchHistoryUnexpectedError()
        {
            mockDbContext.Setup(c => c.MatchParticipants).Throws(new Exception("Unexpected error"));

            var result = statisticsHelper.GetMatchHistory(1, 5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetMultiplePlayerStatisticsAllPlayersExist()
        {
            var userIds = new List<int> { 1, 2 };
            SetupMockUserSet(new List<UserAccount> {
                new UserAccount 
                { 
                    idUser = 1,
                    idPlayer = 1,
                    username = "p1" 
                },
                new UserAccount 
                { 
                    idUser = 2,
                    idPlayer = 2,
                    username = "p2" 
                }
            });
            SetupMockPlayerSet(new List<Player> {
                new Player 
                { 
                    idPlayer = 1,
                    totalPoints = 100,
                    totalMatches = 1 
                },
                new Player
                { 
                    idPlayer = 2, 
                    totalPoints = 80,
                    totalMatches = 1 
                }
            });

            var result = statisticsHelper.GetMultiplePlayerStatistics(userIds);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void TestGetMultiplePlayerStatisticsSomePlayersNotFound()
        {
            var userIds = new List<int> { 1, 999 };
            SetupMockUserSet(new List<UserAccount>
            {
                new UserAccount { idUser = 1, idPlayer = 1, username = "player1" }
            });

            var players = new List<Player>
            {
            new Player
                {
                    idPlayer = 1,
                    totalWins = 10,
                    totalPoints = 100,
                    UserAccount = new List<UserAccount>() 
                }
            };

            SetupMockPlayerSet(players);

            var result = statisticsHelper.GetMultiplePlayerStatistics(userIds);

            Assert.AreEqual(1, result.Count, "Should processd a player registered");
        }

        [TestMethod]
        public void TestGetMultiplePlayerStatisticsEmptyList()
        {
            var userIds = new List<int>();

            var result = statisticsHelper.GetMultiplePlayerStatistics(userIds);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetRecentMatchesMatchesExist()
        {
            var matchParticipants = new List<MatchParticipants>
        {
            new MatchParticipants
            {
                idMatchParticipant = 1,
                idPlayer = 1,
                idGeneralMatch = 1,
                isWinner = true,
                points = 100
            }
        };

            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                date = DateTime.Now,
                MatchParticipants = matchParticipants
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(matchParticipants);

            var result = statisticsHelper.GetRecentMatches(5);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void TestGetRecentMatchesNoMatches()
        {
            SetupMockGeneralMatchSet(new List<GeneralMatch>());

            var result = statisticsHelper.GetRecentMatches(5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetRecentMatchesNoWinnerInMatch()
        {
            var matchParticipants = new List<MatchParticipants>
        {
            new MatchParticipants
            {
                idMatchParticipant = 1,
                idPlayer = 1,
                idGeneralMatch = 1,
                isWinner = false,
                points = 100
            }
        };

            var match = new GeneralMatch
            {
                idGeneralMatch = 1,
                date = DateTime.Now,
                MatchParticipants = matchParticipants
            };

            SetupMockGeneralMatchSet(new List<GeneralMatch> { match });
            SetupMockMatchParticipantsSet(matchParticipants);

            var result = statisticsHelper.GetRecentMatches(5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetRecentMatchesUnexpectedError()
        {
            mockDbContext.Setup(c => c.GeneralMatch).Throws(new Exception("Unexpected error"));

            var result = statisticsHelper.GetRecentMatches(5);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetPlayerStatisticsDbUpdateExceptionCallsLogError()
        {
            int userId = 1;
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbUpdateException("Database error"));

            statisticsHelper.GetPlayerStatistics(userId);

            mockLoggerHelper.Verify(
                l => l.LogError(It.Is<string>(s => s.Contains($"Error for user {userId}")), It.IsAny<Exception>()),
                Times.Once
            );
        }
    }
}
