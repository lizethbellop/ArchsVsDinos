using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using Contracts.DTO.Response;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace UnitTest.ProfileManagementTests
{
    public class ProfileUpdateTikTokTest : ProfileManagementTestBase
    {
        private SocialMediaManager socialMediaManager;

        [TestInitialize]
        public void Setup()
        {
            socialMediaManager = new SocialMediaManager(
                () => mockDbContext.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                mockSecurityHelper.Object
            );
        }

        [TestMethod]
        public void TestUpdateTikTokEmptyFields()
        {
            string username = "";
            string newTikTok = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Los campos son requeridos",
                resultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokUserNotFound()
        {
            string username = "nonExistentUser";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Usuario no encontrado",
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokPlayerNotFound()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

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
                success = false,
                message = "Perfil de jugador no encontrado",
                resultCode = UpdateResultCode.Profile_PlayerNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokSuccess()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@newuser";

            Player player = new Player
            {
                idPlayer = 1,
                tiktok = "tiktok.com/@olduser"
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
                success = true,
                message = "TikTok actualizado exitosamente",
                resultCode = UpdateResultCode.Profile_Success
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokDatabaseValidationError()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error en la base de datos",
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateTikTokUnexpectedError()
        {
            string username = "user123";
            string newTikTok = "tiktok.com/@user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Unexpected error",
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManager.UpdateTikTok(username, newTikTok);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
