using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    public class ProfileManagementTestBase
    {
        protected Mock<IValidationHelper> mockValidationHelper;
        protected Mock<ILoggerHelper> mockLoggerHelper;
        protected Mock<ISecurityHelper> mockSecurityHelper;
        protected Mock<IDbContext> mockDbContext;
        protected Mock<DbSet<UserAccount>> mockUserSet;
        protected Mock<DbSet<Player>> mockPlayerSet;
        protected ProfileManagementB profileManagement;

        [TestInitialize]
        public void BaseSetup()
        {
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();
            mockPlayerSet = new Mock<DbSet<Player>>();

            profileManagement = new ProfileManagementB(
                () => mockDbContext.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                mockSecurityHelper.Object
            );
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
    }
}
