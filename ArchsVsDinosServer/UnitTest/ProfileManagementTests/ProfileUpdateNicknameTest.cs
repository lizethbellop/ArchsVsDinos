using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO.Response;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTest.Util;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateNicknameTest : ProfileManagementTestBase
    {

        [TestMethod]
        public void TestUpdateNicknameEmptyFields()
        {
            string username = "";
            string newNickname = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Los campos son obligatorios"
            };

            UpdateResponse result = profileManagement.UpdateNickname(username, newNickname);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameUserNotFound()
        {
            string username = "nonExistentUser";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameSuccess()
        {
            string username = "user123";
            string newNickname = "coolNickname";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                nickname = "oldNickname"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                Message = "Nickname actualizado exitosamente"
            };

            UpdateResponse result = profileManagement.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameDatabaseValidationError()
        {
            string username = "user123";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Error: Validation error"
            };

            UpdateResponse result = profileManagement.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameUnexpectedError()
        {
            string username = "user123";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Error: Unexpected error"
            };

            UpdateResponse result = profileManagement.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
