using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateUsernameTest : ProfileManagementTestBase
    {
        private ProfileInformation profileInformation;

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


            profileInformation = new ProfileInformation(dependencies);
        }

        [TestMethod]
        public void TestUpdateUsernameEmptyFields()
        {
            string currentUsername = "";
            string newUsername = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameSameAsCurrentUsername()
        {
            string currentUsername = "user123";
            string newUsername = "user123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_SameUsernameValue
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUserAlreadyExists()
        {
            string currentUsername = "user123";
            string newUsername = "existingUser";

            UserAccount existingUser = new UserAccount
            {
                idUser = 2,
                username = newUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { existingUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UsernameExists
            };
            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameUserNotFound()
        {
            string currentUsername = "nonExistentUser";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameExistsCaseInsensitive()
        {
            string currentUsername = "user123";
            string newUsername = "existinguser";

            UserAccount currentUser = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            UserAccount existingUser = new UserAccount
            {
                idUser = 2,
                username = "ExistingUser"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { currentUser, existingUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UsernameExists
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameSuccess()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                ResultCode = UpdateResultCode.Profile_ChangeUsernameSuccess
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameUpdatesUserUsername()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            profileInformation.UpdateUsername(currentUsername, newUsername);

            Assert.AreEqual(newUsername, userAccount.username);
        }

        [TestMethod]
        public void TestUpdateUsernameCallsSaveChanges()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            profileInformation.UpdateUsername(currentUsername, newUsername);

            mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void TestUpdateUsernameDatabaseError()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
                );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = profileInfo.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameUnexpectedError()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = profileInfo.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameAllowsChangingCapitalization()
        {
            string currentUsername = "user123";
            string newUsername = "User123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                ResultCode = UpdateResultCode.Profile_ChangeUsernameSuccess
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }
    }
}
