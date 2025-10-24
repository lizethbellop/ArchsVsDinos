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
    [TestClass]
    public class ProfileUpdateFacebookTest : ProfileManagementTestBase
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
        public void TestUpdateFacebookEmptyFields()
        {
            string username = "";
            string newFacebook = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Los campos son requeridos",
                resultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

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
                success = false,
                message = "Usuario no encontrado",
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

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
                success = false,
                message = "Perfil de jugador no encontrado",
                resultCode = UpdateResultCode.Profile_PlayerNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

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
                success = true,
                message = "Facebook actualizado exitosamente",
                resultCode = UpdateResultCode.Profile_Success
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookDatabaseValidationError()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error en la base de datos",
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateFacebookUnexpectedError()
        {
            string username = "user123";
            string newFacebook = "facebook.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Unexpected error",
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManager.UpdateFacebook(username, newFacebook);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
