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
    public class ProfileUpdateInstagramTest : ProfileManagementTestBase
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
        public void TestUpdateInstagramEmptyFields()
        {
            string username = "";
            string newInstagram = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUserNotFound()
        {
            string username = "nonExistentUser";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramPlayerNotFound()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

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

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramSuccess()
        {
            string username = "user123";
            string newInstagram = "instagram.com/newuser";

            Player player = new Player
            {
                idPlayer = 1,
                instagram = "instagram.com/olduser"
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
                ResultCode = UpdateResultCode.Profile_UpdateInstagramSuccess
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUpdatesPlayerInstagram()
        {
            string username = "user123";
            string newInstagram = "instagram.com/newuser";

            Player player = new Player
            {
                idPlayer = 1,
                instagram = "instagram.com/olduser"
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

            socialMediaManager.UpdateInstagram(username, newInstagram);
            Assert.AreEqual(newInstagram, player.instagram);
        }

        [TestMethod]
        public void TestUpdateInstagramCallsSaveChanges()
        {
            string username = "user123";
            string newInstagram = "instagram.com/newuser";
            Player player = new Player
            {
                idPlayer = 1,
                instagram = "instagram.com/olduser"
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
            
            socialMediaManager.UpdateInstagram(username, newInstagram);
            mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void TestUpdateInstagramDatabaseValidationError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            SocialMediaManager socialMediaManagerException = new SocialMediaManager(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManagerException.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUnexpectedError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            SocialMediaManager socialMediaManagerException = new SocialMediaManager(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManagerException.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
