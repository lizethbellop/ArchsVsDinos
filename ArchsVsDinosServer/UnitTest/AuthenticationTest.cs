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


        [TestMethod]
        public void TestLoginEmptyFields()
        {
            string username = "";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(true);

            
            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_EmptyFields
            };

            
            LoginResponse result = authentication.Login(username, password);

            
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginEmptyUsernameField()
        {
            string username = "";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(false);

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_EmptyFields
            };


            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginEmptyPasswordField()
        {
            string username = "user123";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(true);
            
            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_EmptyFields
            };

            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginIncorrectCredentials()
        {
            string username = "nonExistentUser";
            string password = "incorrectPassword";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            SetupMockUserSet(new List<UserAccount>());

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_InvalidCredentials
            };

            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginCorrectCredentials()
        {
            string username = "user123";
            string password = "password 123";
            string passwordHash = "hashedPassword123";

            Player expectedPlayer = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/csainz",
                instagram = "instagram.com/csainz",
                x = "x.com/csainz",
                tiktok = "tiktok.com/@csainz",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "/profilepictures/yourpfp"
            };

            UserAccount expectedUser = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Carlos Sainz",
                nickname = "chilli55",
                Player = expectedPlayer,
                email = "csainz@f1.com"
            };

            expectedPlayer.UserAccount = new List<UserAccount> { expectedUser };


            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(true);

            SetupMockUserSet(new List<UserAccount> { expectedUser });

            LoginResponse expectedResult = new LoginResponse
            {
                Success = true,
                UserSession = new UserDTO
                {
                    IdUser = 1,
                    Username = "user123",
                    Name = "Carlos Sainz",
                    Nickname = "chilli55",
                    Email = "csainz@f1.com"
                },
                AssociatedPlayer = new PlayerDTO
                {
                    IdPlayer = 1,
                    Facebook = "facebook.com/csainz",
                    Instagram = "instagram.com/csainz",
                    X = "x.com/csainz",
                    Tiktok = "tiktok.com/@csainz",
                    TotalWins = 10,
                    TotalLosses = 5,
                    TotalPoints = 100,
                    ProfilePicture = "/profilepictures/yourpfp"
                },
                ResultCode = LoginResultCode.Authentication_Success
            };

            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginIncorrectPassword()
        {
            string username = "user123";
            string password = "wrongPassword";
            string passwordHash = "hashedPassword123";

            UserAccount existingUser = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Carlos Sainz",
                nickname = "chilli55"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { existingUser });

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_InvalidCredentials
            };

            LoginResponse result = authentication.Login(username, password);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginDatabaseConnectionError()
        {
            string username = "user123";
            string password = "1234";
           
            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database connection failed"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );
            
            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            Authentication authenticationException = new Authentication(dependencies);

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_DatabaseError
            };

            LoginResponse result = authenticationException.Login(username, password);
            Assert.AreEqual(expectedResult, result);

        }

        [TestMethod]
        public void TestLoginUnexpectedError()
        {
            string username = "user123";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(password)).Throws(new Exception("Unexpected error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            Authentication authenticationException = new Authentication(dependencies);

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_UnexpectedError
            };

            LoginResponse result = authenticationException.Login(username, password);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginUserBanned()
        {
            string username = "banneduser";
            string password = "password123";
            string passwordHash = "hashedPassword123";

            UserAccount bannedUser = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Banned User",
                nickname = "banned",
                email = "banned@test.com"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(true);

            SetupMockUserSet(new List<UserAccount> { bannedUser });

            LoginResponse expectedResult = new LoginResponse
            {
                Success = false,
                UserSession = null,
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_UserBanned
            };

            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginCallsIsUserBanned()
        {
            string username = "user123";
            string password = "password123";
            string passwordHash = "hashedPassword123";

            Player expectedPlayer = new Player
            {
                idPlayer = 1,
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100
            };

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Test User",
                nickname = "testnick",
                email = "test@test.com",
                Player = expectedPlayer
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            authentication.Login(username, password);

            mockStrikeManager.Verify(s => s.IsUserBanned(1), Times.Once);
        }

        [TestMethod]
        public void TestLoginCallsVerifyPassword()
        {
            string username = "user123";
            string password = "password123";
            string passwordHash = "hashedPassword123";

            Player expectedPlayer = new Player
            {
                idPlayer = 1,
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100
            };

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Test User",
                nickname = "testnick",
                email = "test@test.com",
                Player = expectedPlayer
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            authentication.Login(username, password);

            mockSecurityHelper.Verify(s => s.VerifyPassword(password, passwordHash), Times.Once);
        }

        [TestMethod]
        public void TestLoginCallsIsEmptyForUsername()
        {
            string username = "user123";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            authentication.Login(username, password);

            mockValidationHelper.Verify(v => v.IsEmpty(username), Times.AtLeastOnce);  // UN SOLO VERIFY
        }

        [TestMethod]
        public void TestLoginCallsIsEmptyForPassword()
        {
            string username = "user123";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            authentication.Login(username, password);

            mockValidationHelper.Verify(v => v.IsEmpty(password), Times.AtLeastOnce);  // UN SOLO VERIFY
        }

        [TestMethod]
        public void TestLoginUserWithoutAssociatedPlayer()
        {
            string username = "user123";
            string password = "password123";
            string passwordHash = "hashedPassword123";

            UserAccount userWithoutPlayer = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "User Without Player",
                nickname = "noPlayer",
                email = "user@test.com",
                Player = null
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(true);
            mockStrikeManager.Setup(s => s.IsUserBanned(1)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { userWithoutPlayer });

            LoginResponse expectedResult = new LoginResponse
            {
                Success = true,
                UserSession = new UserDTO
                {
                    IdUser = 1,
                    Username = "user123",
                    Name = "User Without Player",
                    Nickname = "noPlayer",
                    Email = "user@test.com"
                },
                AssociatedPlayer = null,
                ResultCode = LoginResultCode.Authentication_Success
            };

            LoginResponse result = authentication.Login(username, password);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestLoginDoesNotCallVerifyPasswordWhenUserNotFound()
        {
            string username = "nonexistent";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            authentication.Login(username, password);

            mockSecurityHelper.Verify(s => s.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);  // UN SOLO VERIFY
        }

        [TestMethod]
        public void TestLoginDoesNotCheckBanStatusWhenCredentialsInvalid()
        {
            string username = "user123";
            string password = "wrongpassword";
            string passwordHash = "hashedPassword123";

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.VerifyPassword(password, passwordHash)).Returns(false);

            SetupMockUserSet(new List<UserAccount> { user });

            authentication.Login(username, password);

            mockStrikeManager.Verify(s => s.IsUserBanned(It.IsAny<int>()), Times.Never);  // UN SOLO VERIFY
        }

        [TestMethod]
        public void TestLoginDoesNotCheckBanStatusWhenFieldsEmpty()
        {
            string username = "";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            authentication.Login(username, password);

            mockStrikeManager.Verify(s => s.IsUserBanned(It.IsAny<int>()), Times.Never);  // UN SOLO VERIFY
        }

    }
}
