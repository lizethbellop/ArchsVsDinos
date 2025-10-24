using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO.Response;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateUsernameTest : ProfileManagementTestBase
    {
        private ProfileInformation profileInformation;

        [TestInitialize]
        public void Setup()
        {
            profileInformation = new ProfileInformation(
                () => mockDbContext.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                mockSecurityHelper.Object
            );
        }

        [TestMethod]
        public void TestUpdateUsernameEmptyFields()
        {
            string currentUsername = "";
            string newUsername = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Los campos son obligatorios",
                resultCode = UpdateResultCode.Profile_EmptyFields
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
                success = false,
                message = "El nuevo username debe ser diferente al actual",
                resultCode = UpdateResultCode.Profile_SameUsernameValue
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
                success = false,
                message = "El username ya está en uso",
                resultCode = UpdateResultCode.Profile_UsernameExists
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
                success = false,
                message = "Usuario no encontrado",
                resultCode = UpdateResultCode.Profile_UserNotFound
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
                success = true,
                message = "Username actualizado",
                resultCode = UpdateResultCode.Profile_Success
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameDatabaseError()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error: Database error",
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = profileInformation.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }
    }
}
