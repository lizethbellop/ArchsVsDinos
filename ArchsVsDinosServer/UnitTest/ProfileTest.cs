using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO.Response;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class ProfileTest
    {
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<ILoggerHelper> mockLoggerHelper;
        private Mock<ISecurityHelper> mockSecurityHelper;
        private Mock<IDbContext> mockDbContext;
        private Mock<DbSet<UserAccount>> mockUserSet;
        private Mock<DbSet<Player>> mockPlayerSet;
        private ProfileManagement profileManagement;

        [TestInitialize]
        public void Setup()
        {
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();
            mockPlayerSet = new Mock<DbSet<Player>>();

            profileManagement = new ProfileManagement(() => mockDbContext.Object, mockValidationHelper.Object, 
                mockLoggerHelper.Object, mockSecurityHelper.Object);
        }

        [TestMethod]
        public void TestProfileChangePasswordEmptyFields()
        {
            string username = "";
            string currentPassword = "";
            string newPassword = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Todos los campos son obligatorios"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestProfileChangePasswordSameAsCurrent()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "La nueva contraseña debe ser diferente a la actual"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestProfileChangePasswordTooShort()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "short";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "La nueva contraseña debe tener al menos 8 caracteres"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestProfileChangePasswordUserNotFound()
        {
            string username = "nonExistentUser";
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestProfileChangePasswordIncorrectCurrentPassword()
        {
            string username = "user123";
            string currentPassword = "wrongPassword";
            string newPassword = "newPassword123";
            string hashedCurrentPassword = "hashedWrong";
            string storedHashedPassword = "hashedCorrect";

            UserAccount userAccount = new UserAccount()
            {
                idUser = 1,
                username = username,
                password = storedHashedPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(currentPassword)).Returns(hashedCurrentPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "La contraseña actual es incorrecta"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestProfileChangePasswordSuccess()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "newPassword123";
            string hashedCurrentPassword = "hashedCurrent";
            string hashedNewPassword = "hashedNew";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(currentPassword)).Returns(hashedCurrentPassword);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                Message = "Contraseña actualizada exitosamente"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);

        }


        private void SetupMockUserSet(List<UserAccount> users)
        {
            var queryableUsers = users.AsQueryable();

            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.GetEnumerator()).Returns(queryableUsers.GetEnumerator());

            mockDbContext.Setup(c => c.UserAccount).Returns(mockUserSet.Object);
        }

        private void SetupMockPlayerSet(List<Player> players)
        {
            var queryablePlayers = players.AsQueryable();

            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.Provider).Returns(queryablePlayers.Provider);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.Expression).Returns(queryablePlayers.Expression);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.ElementType).Returns(queryablePlayers.ElementType);
            mockPlayerSet.As<IQueryable<Player>>().Setup(m => m.GetEnumerator()).Returns(queryablePlayers.GetEnumerator());

            mockDbContext.Setup(c => c.Player).Returns(mockPlayerSet.Object);
        }
    }
}
