using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
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

            authentication = new Authentication(mockSecurityHelper.Object, mockValidationHelper.Object,
                mockLoggerHelper.Object, () => mockDbContext.Object);
        }


        [TestMethod]
        public void TestLoginEmptyFields()
        {
            //Arrange
            string username = "";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(true);

            //Act
            var result = authentication.Login(username, password);

            //Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Campos requeridos", result.Message);

            mockValidationHelper.Verify(v => v.IsEmpty(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [TestMethod]
        public void TestLoginEmptyUsernameField()
        {
            string username = "";
            string password = "password123";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(false);

            var result = authentication.Login(username, password);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Campos requeridos", result.Message);

            mockValidationHelper.Verify(v => v.IsEmpty(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [TestMethod]
        public void TestLoginEmptyPasswordField()
        {
            string username = "user123";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(true);

            var result = authentication.Login(username, password);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Campos requeridos", result.Message);
        }

        [TestMethod]
        public void TestLoginIncorrectCredentials()
        {
            string username = "nonExistentUser";
            string password = "incorrectPassword";
            string passwordHash = "hashedPassword123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(password)).Returns(passwordHash);

            SetupMockUserSet(new List<UserAccount>());

            var result = authentication.Login(username, password);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Credenciales incorrectas", result.Message);
            Assert.IsNull(result.UserSession);
            Assert.IsNull(result.AssociatedPlayer);


        }

        [TestMethod]
        public void TestLoginCorrectCredentials()
        {
            string username = "user123";
            string password = "password 123";
            string passwordHash = "hashedPassword123";

            var expectedPlayer = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/csainz",
                instagram = "instagram.com/csainz",
                x = "x.com/csainz",
                tiktok = "tiktok.com/@csainz",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "yourpfp.com"
            };

            var expectedUser = new UserAccount
            {
                idUser = 1,
                username = username,
                password = passwordHash,
                name = "Carlos Sainz",
                nickname = "chilli55",
                Player = expectedPlayer
            };

            expectedPlayer.UserAccount = new List<UserAccount> { expectedUser };


            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockSecurityHelper.Setup(s => s.HashPassword(password)).Returns(passwordHash);

            SetupMockUserSet(new List<UserAccount> { expectedUser });

            var result = authentication.Login(username, password);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Login exitoso", result.Message);
            Assert.IsNotNull(result.UserSession);
            Assert.IsNotNull(result.AssociatedPlayer);
            Assert.AreEqual(expectedUser.idUser, result.UserSession.idUser);
            Assert.AreEqual(expectedUser.name, result.UserSession.name);
            Assert.AreEqual(expectedUser.nickname, result.UserSession.nickname);
            Assert.AreEqual(expectedUser.username, result.UserSession.username);
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
