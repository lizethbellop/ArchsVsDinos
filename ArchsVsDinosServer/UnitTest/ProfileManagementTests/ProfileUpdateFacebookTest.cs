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
    public class ProfileUpdateFacebookTest : ProfileManagementTestBase
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
        public void TestUpdateFacebookEmptyFields()
        {
            string username = "";
            string newFacebook = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookUserNotFound()
        {
            string username = "nonExistentUser";
            string newFacebook = "facebook.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            var expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookPlayerNotFound()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

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

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookSuccess()
        {
            string username = "user123";
            string newFacebook = "facebook.com/newuser";

            var player = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/olduser"
            };

            var userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            var expectedResult = new UpdateResponse
            {
                Success = true,
                ResultCode = UpdateResultCode.Profile_UpdateFacebookSuccess
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookDatabaseValidationError()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

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

            UpdateResponse result = socialMediaManagerException.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookUnexpectedError()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

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

            UpdateResponse result = socialMediaManagerException.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
