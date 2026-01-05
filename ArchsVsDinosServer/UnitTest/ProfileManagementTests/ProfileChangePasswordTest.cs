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
using ArchsVsDinosServer.Utils;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileChangePasswordTest : ProfileManagementTestBase
    {
        private PasswordManager passwordManager;
        private Mock<IPasswordValidator> mockPasswordValidator;

        [TestInitialize]
        public void Setup()
        {
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();
            mockPasswordValidator = new Mock<IPasswordValidator>();

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );

            passwordManager = new PasswordManager(dependencies, mockPasswordValidator.Object);
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
                Success = false,
                ResultCode = UpdateResultCode.Profile_EmptyFields
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
                Success = false,
                ResultCode = UpdateResultCode.Profile_SamePasswordValue
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordTooShort()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "short";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordTooShort));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordTooShort
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordMissingUppercase()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "password123!";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsUppercase));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordNeedsUppercase
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordMissingLowercase()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "PASSWORD123!";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsLowercase));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordNeedsLowercase
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordMissingNumber()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "Password!";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsNumber));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordNeedsNumber
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordMissingSpecialCharacter()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "Password123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsSpecialCharacter));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordNeedsSpecialCharacter
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordUserNotFound()
        {
            string username = "nonExistentUser";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordIncorrectCurrentPassword()
        {
            string username = "user123";
            string currentPassword = "wrongPassword";
            string newPassword = "NewPassword123!";
            string storedHashedPassword = "hashedCorrect";

            UserAccount userAccount = new UserAccount()
            {
                idUser = 1,
                username = username,
                password = storedHashedPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, storedHashedPassword)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_InvalidPassword
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordSuccess()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";
            string hashedNewPassword = "hashedNew";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                ResultCode = UpdateResultCode.Profile_ChangePasswordSuccess
            };

            UpdateResponse result = passwordManager.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordUpdatesUserPassword()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";
            string hashedNewPassword = "hashedNew";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            passwordManager.ChangePassword(username, currentPassword, newPassword);

            Assert.AreEqual(hashedNewPassword, userAccount.password);
        }

        [TestMethod]
        public void TestChangePasswordCallsSaveChanges()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";
            string hashedNewPassword = "hashedNew";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            passwordManager.ChangePassword(username, currentPassword, newPassword);

            mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void TestChangePasswordDatabaseValidationException()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            // Configurar para que pase todas las validaciones previas
            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);

            // Configurar el UserSet correctamente
            SetupMockUserSet(new List<UserAccount> { userAccount });

            // Lanzar excepción al intentar guardar cambios
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new DbEntityValidationException("Validation error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );

            PasswordManager passwordManagerException = new PasswordManager(dependencies, mockPasswordValidator.Object);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = passwordManagerException.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordUnexpectedError()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            // Configurar para que pase todas las validaciones previas
            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);

            // Configurar el UserSet correctamente
            SetupMockUserSet(new List<UserAccount> { userAccount });

            // Lanzar excepción al intentar guardar cambios
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new Exception("Unexpected error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );

            PasswordManager passwordManagerException = new PasswordManager(dependencies, mockPasswordValidator.Object);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = passwordManagerException.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestChangePasswordCallsHashPasswordWithNewPassword()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";
            string hashedCurrentPassword = "hashedCurrent";
            string hashedNewPassword = "hashedNew";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hashedCurrentPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess));
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            passwordManager.ChangePassword(username, currentPassword, newPassword);

            mockSecurityHelper.Verify(s => s.HashPassword(newPassword), Times.Once);
        }

        [TestMethod]
        public void TestChangePasswordCallsValidatorWithNewPassword()
        {
            string username = "user123";
            string currentPassword = "Password123!";
            string newPassword = "NewPassword123!";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockPasswordValidator.Setup(v => v.ValidatePassword(newPassword))
                .Returns(new ValidationResult(false, UpdateResultCode.Profile_PasswordTooShort));

            passwordManager.ChangePassword(username, currentPassword, newPassword);

            mockPasswordValidator.Verify(v => v.ValidatePassword(newPassword), Times.Once);
        }
    }
}
