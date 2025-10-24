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
using Contracts.DTO.Result_Codes;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileChangePasswordTest : ProfileManagementTestBase
    {
        private PasswordManager passwordManager;

        [TestInitialize]
        public void Setup()
        {
            passwordManager = new PasswordManager(
                () => mockDbContext.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                mockSecurityHelper.Object
            );
        }

        [TestMethod]
        public void TestChangePasswordEmptyFields()
        {
            string username = "";
            string currentPassword = "";
            string newPassword = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Todos los campos son obligatorios",
                resultCode =UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordSameAsCurrent()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "La nueva contraseña debe ser diferente a la actual",
                resultCode = UpdateResultCode.Profile_SamePasswordValue
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordTooShort()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "short";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "La nueva contraseña debe tener al menos 8 caracteres",
                resultCode = UpdateResultCode.Profile_PasswordTooShort
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordUserNotFound()
        {
            string username = "nonExistentUser";
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Usuario no encontrado",
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordIncorrectCurrentPassword()
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
                success = false,
                message = "La contraseña actual es incorrecta",
                resultCode = UpdateResultCode.Profile_SamePasswordValue
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordSuccess()
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
                success = true,
                message = "Contraseña actualizada exitosamente",
                resultCode =UpdateResultCode.Profile_Success
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);

        }

        [TestMethod]
        public void TestChangePasswordDatabaseValidationException()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error en la base de datos",
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordUnexpectedError()
        {
            string username = "user123";
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error: Unexpected error",
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);

        }


    }
}
