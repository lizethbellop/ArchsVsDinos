using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO;
using Contracts.DTO.Response;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Validation;
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
        public void TestChangePasswordEmptyFields()
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
        public void TestChangePasswordSameAsCurrent()
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
        public void TestChangePasswordTooShort()
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
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
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
                Success = false,
                Message = "La contraseña actual es incorrecta"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
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
                Success = true,
                Message = "Contraseña actualizada exitosamente"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
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
                Success = false,
                Message = "Error en la base de datos"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
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
                Success = false,
                Message = "Error: Unexpected error"
            };

            UpdateResponse result = profileManagement.ChangePassword(username, currentPassword, newPassword);
            Assert.AreEqual(expectedResult, result);
            
        }

        [TestMethod]
        public void TestUpdateUsernameEmptyFields()
        {
            string currentUsername = "";
            string newUsername = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Los campos son obligatorios"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameSameAsCurrentUsername()
        {
            string currentUsername = "user123";
            string newUsername = "user123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "El nuevo username debe ser diferente al actual"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUserAlreadyExists()
        {
            string currentUsername = "user123";
            string newUsername = "existingUser";

            UserAccount existingUser = new UserAccount
            {
                idUser = 2,
                username = newUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { existingUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "El username ya está en uso"
            };
            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameUserNotFound()
        {
            string currentUsername = "nonExistentUser";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameSuccess()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = currentUsername
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = true,
                Message = "Username actualizado"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateUsernameDatabaseError()
        {
            string currentUsername = "user123";
            string newUsername = "newUser123";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Error: Database error"
            };

            UpdateResponse result = profileManagement.UpdateUsername(currentUsername, newUsername);
            Assert.AreEqual(expectedResult, result);
        }

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

        [TestMethod]
        public void TestGetProfileUserNotFound()
        {
            string username = "nonExistentUser";

            SetupMockUserSet(new List<UserAccount>());

            PlayerDTO result = profileManagement.GetProfile(username);

             Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfilePlayerNotFound()
        {
            string username = "user123";

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player>());

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfileSuccess()
        {
            string username = "user123";

            var player = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/user",
                instagram = "instagram.com/user",
                x = "x.com/user",
                tiktok = "tiktok.com/@user",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "picture.jpg"
            };

            UserAccount userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });

            PlayerDTO expectedResult = new PlayerDTO
            {
                idPlayer = 1,
                facebook = "facebook.com/user",
                instagram = "instagram.com/user",
                x = "x.com/user",
                tiktok = "tiktok.com/@user",
                totalWins = 10,
                totalLosses = 5,
                totalPoints = 100,
                profilePicture = "picture.jpg"
            };

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetProfileDatabaseError()
        {
            string username = "user123";

            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetProfileUnexpectedError()
        {
            string username = "user123";

            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            PlayerDTO result = profileManagement.GetProfile(username);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestUpdateFacebookEmptyFields()
        {
            string username = "";
            string newFacebook = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Los campos son requeridos"
            };

            UpdateResponse result = profileManagement.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookUserNotFound()
        {
            string username = "nonExistentUser";
            string newFacebook = "facebook.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            var expectedResult = new UpdateResponse
            {
                Success = false,
                Message = "Usuario no encontrado"
            };

            UpdateResponse result = profileManagement.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookPlayerNotFound()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

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

            UpdateResponse result = profileManagement.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookSuccess()
        {
            string username = "user123";
            string newFacebook = "facebook.com/newuser";

            var player = new Player
            {
                idPlayer = 1,
                facebook = "facebook.com/olduser"
            };

            var userAccount = new UserAccount
            {
                idUser = 1,
                username = username,
                idPlayer = 1
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { userAccount });
            SetupMockPlayerSet(new List<Player> { player });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            var expectedResult = new UpdateResponse
            {
                Success = true,
                Message = "Facebook actualizado exitosamente"
            };

            UpdateResponse result = profileManagement.UpdateFacebook(username, newFacebook);

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
