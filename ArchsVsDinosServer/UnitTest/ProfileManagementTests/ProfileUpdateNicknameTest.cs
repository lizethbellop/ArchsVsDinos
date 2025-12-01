using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileUpdateNicknameTest : ProfileManagementTestBase
    {

        private ProfileInformation profileInformation;

        [TestInitialize]
        public void Setup()
        {
            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                coreDeps,
                () => mockDbContext.Object
            );


            profileInformation = new ProfileInformation(dependencies);
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
                ResultCode = UpdateResultCode.Profile_EmptyFields
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);
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
                ResultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameAlreadyExists()
        {
            string username = "user123";
            string newNickname = "existingNick";

            UserAccount currentUser = new UserAccount
            {
                idUser = 1,
                username = username,
                nickname = "oldNick"
            };

            UserAccount otherUser = new UserAccount
            {
                idUser = 2,
                username = "otherUser",
                nickname = "existingNick"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { currentUser, otherUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_NicknameExists
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

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
                ResultCode = UpdateResultCode.Profile_ChangeNicknameSuccess
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameExistsCaseInsensitive()
        {
            string username = "user123";
            string newNickname = "coolnick";

            UserAccount currentUser = new UserAccount
            {
                idUser = 1,
                username = username,
                nickname = "oldNick"
            };

            UserAccount otherUser = new UserAccount
            {
                idUser = 2,
                username = "otherUser",
                nickname = "CoolNick"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { currentUser, otherUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_NicknameExists
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameUpdatesUserNickname()
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

            profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(newNickname, userAccount.nickname);
        }

        [TestMethod]
        public void TestUpdateNicknameCallsSaveChanges()
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

            profileInformation.UpdateNickname(username, newNickname);

            mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void TestUpdateNicknameDatabaseValidationError()
        {
            string username = "user123";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_DatabaseError
            };

            UpdateResponse result = profileInfo.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameUnexpectedError()
        {
            string username = "user123";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            CoreDependencies coreDeps = new CoreDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object
            );

            ServiceDependencies dependencies = new ServiceDependencies(
                    coreDeps,
                    () => mockDbContext.Object
             );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                Success = false,
                ResultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = profileInfo.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
