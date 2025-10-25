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
    public class ProfileUpdateXTest : ProfileManagementTestBase
    {
        private SocialMediaManager socialMediaManager;

        [TestInitialize]
        public void Setup()
        {
            ServiceDependencies dependencies = new ServiceDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            socialMediaManager = new SocialMediaManager(dependencies);
        }

        [TestMethod]
        public void TestUpdateXEmptyFields()
        {
            string username = "";
            string newX = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateXUserNotFound()
        {
            string username = "nonExistentUser";
            string newX = "x.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateXPlayerNotFound()
        {
            string username = "user123";
            string newX = "x.com/user";

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
                success = false,
                resultCode = UpdateResultCode.Profile_PlayerNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateXSuccess()
        {
            string username = "user123";
            string newX = "x.com/newuser";

            Player player = new Player
            {
                idPlayer = 1,
                x = "x.com/olduser"
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
                success = true,
                resultCode = UpdateResultCode.Profile_Success
            };

            UpdateResponse result = socialMediaManager.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateXDatabaseValidationError()
        {
            string username = "user123";
            string newX = "x.com/user";

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
                success = false,
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManagerException.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateXUnexpectedError()
        {
            string username = "user123";
            string newX = "x.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManager.UpdateX(username, newX);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
