using ArchsVsDinosServer;
using Contracts.DTO.Response;
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
        [TestMethod]
        public void TestUpdateInstagramEmptyFields()
        {
            string username = "";
            string newInstagram = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Los campos son requeridos"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

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
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

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
                Message = "Perfil de jugador no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

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
                Message = "Instagram actualizado exitosamente"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramDatabaseValidationError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Error en la base de datos"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUnexpectedError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Unexpected error"
            };

            UpdateResponse result = profileManagement.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
