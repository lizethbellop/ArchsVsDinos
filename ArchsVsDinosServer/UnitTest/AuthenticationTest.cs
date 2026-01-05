using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
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

namespace UnitTest
{
    [TestClass]
    public class AuthenticationTest : BaseTestClass
    {
        private Mock<ISecurityHelper> mockSecurityHelper;
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<IStrikeManager> mockStrikeManager;
        private Authentication authentication;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockStrikeManager = new Mock<IStrikeManager>();

            SessionManager.Instance.ClearAllUsers();

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );

            authentication = new Authentication(dependencies, mockStrikeManager.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Instance.ClearAllUsers();
        }

        [TestMethod]
        public void TestLoginUserAlreadyLoggedIn()
        {
            string username = "activeUser";
            string password = "password";

            SessionManager.Instance.RegisterUser(username);

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_UserAlreadyLoggedIn,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginEmptyFields()
        {
            string username = "";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_EmptyFields,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginInvalidCredentials()
        {
            string username = "user";
            string password = "wrong";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_InvalidCredentials,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginSuccessFlow()
        {
            string username = "user";
            string password = "pass";
            string hash = "hash";

            Player player = new Player { idPlayer = 1 };
            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hash,
                email = "user@test.com",
                nickname = "nick",
                Player = player
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, hash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = true,
                ResultCode = LoginResultCode.Authentication_Success,
                UserSession = new UserDTO
                {
                    IdUser = 1,
                    Username = username,
                    Email = "user@test.com",
                    Nickname = "nick"
                },
                AssociatedPlayer = new PlayerDTO
                {
                    IdPlayer = 1
                }
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginUserBanned()
        {
            string username = "banned";
            string password = "pass";
            string hash = "hash";

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hash
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, hash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(true);

            SetupMockUserSet(new List<UserAccount> { user });

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_UserBanned,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginDatabaseException()
        {
            string username = "user";
            string password = "pass";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount)
                         .Throws(new EntityException());

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_DatabaseError,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginUnexpectedException()
        {
            string username = "user";
            string password = "pass";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(password))
                              .Throws(new Exception());

            LoginResponse result = authentication.Login(username, password);

            LoginResponse expected = new LoginResponse
            {
                Success = false,
                ResultCode = LoginResultCode.Authentication_UnexpectedError,
                UserSession = null,
                AssociatedPlayer = null
            };

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLoginCallsVerifyPassword()
        {
            string username = "user";
            string password = "pass";
            string hash = "hash";

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hash
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, hash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            authentication.Login(username, password);

            mockSecurityHelper.Verify(
                s => s.VerifyPassword(password, hash),
                Times.Once);
        }

        [TestMethod]
        public void TestLoginCallsIsUserBanned()
        {
            string username = "user";
            string password = "pass";
            string hash = "hash";

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = hash
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, hash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            authentication.Login(username, password);

            mockStrikeManager.Verify(
                s => s.IsUserBanned(1),
                Times.Once);
        }

        [TestMethod]
        public void TestLogoutSuccess()
        {
            string username = "logoutUser";
            SessionManager.Instance.RegisterUser(username);

            authentication.Logout(username);

            bool result = SessionManager.Instance.IsUserOnline(username);

            Assert.IsFalse(result);
        }
    }

}
