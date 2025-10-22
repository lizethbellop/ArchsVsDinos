using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
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
using UnitTest.Util;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateUsernameTest : ProfileManagementTestBase
    {
        [TestMethod]
        public void TestUpdateUsernameEmptyFields()
        {
            string currentUsername = "";
            string newUsername = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Los campos son obligatorios"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
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
                Message = "El nuevo username debe ser diferente al actual"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
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
                Message = "El username ya está en uso"
            };
            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
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
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
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
                Message = "Username actualizado"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
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
                Success = false,
                Message = "Error: Database error"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }
    }
}
