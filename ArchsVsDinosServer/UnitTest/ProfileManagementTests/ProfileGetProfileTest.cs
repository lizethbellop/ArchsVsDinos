using ArchsVsDinosServer;
using Contracts.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileGetProfileTest : ProfileManagementTestBase
    {
        [TestMethod]
        public void TestGetProfileUserNotFound()
        {
            string username = "nonExistentUser";

            SetupMockUserSet(new List<UserAccount>());

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfilePlayerNotFound()
        {
            string username = "user123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player>());

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfileSuccess()
        {
            string username = "user123";

            var player = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/user",
                instagram = "instagram.com/user",
                x = "x.com/user",
                tiktok = "tiktok.com/@user",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "picture.jpg"
            };

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });

            PlayerDTO expectedResult = new PlayerDTO
            {
                idPlayer = 1,
                facebook = "facebook.com/user",
                instagram = "instagram.com/user",
                x = "x.com/user",
                tiktok = "tiktok.com/@user",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "picture.jpg"
            };

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetProfileDatabaseError()
        {
            string username = "user123";

            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfileUnexpectedError()
        {
            string username = "user123";

            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }
    }
}
