using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateTikTokTest : ProfileManagementTestBase
    {
        private SocialMediaManager socialMediaManager;

        [TestInitialize]
        public void Setup()
        {
            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );


            socialMediaManager = new SocialMediaManager(dependencies);
        }

        [TestMethod]
        public void TestUpdateTikTokEmptyFields()
        {
            string username = "";
            string newTikTok = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokUserNotFound()
        {
            string username = "nonExistentUser";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokPlayerNotFound()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PlayerNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokSuccess()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@newuser";

            Player player = new Player
            {
                idPlayer = 1,
                tiktok = "tiktok.com/@olduser"
            };

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                ResultCode = UpdateResultCode.Profile_UpdateTikTokSuccess
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTiktokUpdatesPlayerTiktok()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@newuser";
            
            Player player = new Player
            {
                idPlayer = 1,
                tiktok = "tiktok.com/@olduser"
            };
            
            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };
            
            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);
            socialMediaManager.UpdateTikTok(username, newTikTok);
            
            Assert.AreEqual(newTikTok, player.tiktok);
        }

        [TestMethod]
        public void TestUpdateTiktokCallsSaveChanges()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@newuser";
            
            Player player = new Player
            {
                idPlayer = 1,
                tiktok = "tiktok.com/@olduser"
            };
            
            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };
            
            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);
            socialMediaManager.UpdateTikTok(username, newTikTok);
            
            mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void TestUpdateTikTokDatabaseValidationError()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
             );

            SocialMediaManager socialMediaManagerException = new SocialMediaManager(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManagerException.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokUnexpectedError()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
             );

            SocialMediaManager socialMediaManagerException = new SocialMediaManager(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManagerException.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
