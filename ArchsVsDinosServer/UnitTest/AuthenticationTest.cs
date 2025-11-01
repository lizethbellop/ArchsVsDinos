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
    public class AuthenticationTest
    {
        private Mock<ISecurityHelper> mockSecurityHelper;
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<ILoggerHelper> mockLoggerHelper;
        private Mock<IDbContext> mockDbContext;
        private Mock<DbSet<UserAccount>> mockUserSet;
        private Authentication authentication;

        [TestInitialize]
        public void Setup()
        {
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockDbContext = new Mock<IDbContext>();
            mockUserSet = new Mock<DbSet<UserAccount>>();

            var dependencies = new ServiceDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            authentication = new Authentication(dependencies);
        }


        [TestMethod]
        public void TestLoginEmptyFields()
        {
            //Arrange
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

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
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

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
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


        private void SetupMockUserSet(List<UserAccount> users)
        {
            var queryableUsers = users.AsQueryable();

            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
            mockUserSet.As<IQueryable<UserAccount>>().Setup(m => m.GetEnumerator()).Returns(queryableUsers.GetEnumerator());

            mockDbContext.Setup(c => c.UserAccount).Returns(mockUserSet.Object);
        }

    }
}
