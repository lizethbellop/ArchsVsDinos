using ArchsVsDinosServer;
using ArchsVsDinosServer.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class BaseTestClass
    {
        protected Mock<ILoggerHelper> mockLoggerHelper;
        protected Mock<IDbContext> mockDbContext;
        protected Mock<DbSet<UserAccount>> mockUserSet;
        protected Mock<DbSet<Player>> mockPlayerSet;
        protected Mock<DbSet<MatchParticipants>> mockMatchParticipantSet;
        protected Mock<DbSet<GeneralMatch>> mockGeneralMatchSet;


        [TestInitialize]
        public void BaseSetup()
        {
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();
            mockPlayerSet = new Mock<DbSet<Player>>();
            mockMatchParticipantSet = new Mock<DbSet<MatchParticipants>>();
            mockGeneralMatchSet = new Mock<DbSet<GeneralMatch>>();
        }

        protected void SetupMockUserSet(List<UserAccount> users)
        {
            var queryableUsers = users.AsQueryable();
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.GetEnumerator()).Returns(queryableUsers.GetEnumerator());
            mockDbContext.Setup(c => c.UserAccount).Returns(mockUserSet.Object);
        }

        protected void SetupMockPlayerSet(List<Player> players)
        {
            var queryablePlayers = players.AsQueryable();
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.Provider).Returns(queryablePlayers.Provider);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.Expression).Returns(queryablePlayers.Expression);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.ElementType).Returns(queryablePlayers.ElementType);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.GetEnumerator()).Returns(queryablePlayers.GetEnumerator());
            mockDbContext.Setup(c => c.Player).Returns(mockPlayerSet.Object);
        }

        protected void SetupMockMatchParticipantsSet(List<MatchParticipants> participants)
        {
            var queryable = participants.AsQueryable();

            mockMatchParticipantSet.As<IQueryable<MatchParticipants>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockMatchParticipantSet.As<IQueryable<MatchParticipants>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockMatchParticipantSet.As<IQueryable<MatchParticipants>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockMatchParticipantSet.As<IQueryable<MatchParticipants>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            mockDbContext.Setup(c => c.MatchParticipants).Returns(mockMatchParticipantSet.Object);
        }

        protected void SetupMockGeneralMatchSet(List<GeneralMatch> matches)
        {
            var queryable = matches.AsQueryable();

            mockGeneralMatchSet.As<IQueryable<GeneralMatch>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockGeneralMatchSet.As<IQueryable<GeneralMatch>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockGeneralMatchSet.As<IQueryable<GeneralMatch>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockGeneralMatchSet.As<IQueryable<GeneralMatch>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            mockDbContext.Setup(c => c.GeneralMatch).Returns(mockGeneralMatchSet.Object);
        }
    }
}
