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

        [TestInitialize]
        public void Setup()
        {
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );

            passwordManager = new PasswordManager(dependencies);
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
                ResultCode =UpdateResultCode.Profile_EmptyFields
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
            string currentPassword = "password123";
            string newPassword = "short";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_PasswordTooShort
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
            string newPassword = "newPassword123";
            string storedHashedPassword = "hashedCorrect";

            UserAccount userAccount = new UserAccount()
            {
                idUser = 1,
                username = username,
                password = storedHashedPassword
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
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
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
             );

            PasswordManager passwordManagerException = new PasswordManager(dependencies);

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
            string currentPassword = "password123";
            string newPassword = "newPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
             );

            PasswordManager passwordManagerException = new PasswordManager(dependencies);

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
            mockSecurityHelper.Setup(s => s.VerifyPassword(currentPassword, hashedCurrentPassword))
                .Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(newPassword)).Returns(hashedNewPassword);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            passwordManager.ChangePassword(username, currentPassword, newPassword);

            mockSecurityHelper.Verify(s => s.HashPassword(newPassword), Times.Once);
        }
    }
}
