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
    public class ProfileUpdateInstagramTest : ProfileManagementTestBase
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
        public void TestUpdateInstagramEmptyFields()
        {
            string username = "";
            string newInstagram = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Los campos son requeridos",
                resultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUserNotFound()
        {
            string username = "nonExistentUser";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Usuario no encontrado",
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramPlayerNotFound()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

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

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        public void TestUpdateInstagramSuccess()
        {
            string username = "user123";
            string newInstagram = "instagram.com/newuser";

            Player player = new Player
            {
                idPlayer = 1,
                instagram = "instagram.com/olduser"
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
                message = "Instagram actualizado exitosamente",
                resultCode = UpdateResultCode.Profile_Success
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramDatabaseValidationError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Error en la base de datos",
                resultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateInstagramUnexpectedError()
        {
            string username = "user123";
            string newInstagram = "instagram.com/user";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                message = "Unexpected error",
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = socialMediaManager.UpdateInstagram(username, newInstagram);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
