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
            ServiceDependencies dependencies = new ServiceDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
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
                success = false,
                resultCode = UpdateResultCode.Profile_EmptyFields
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
                success = false,
                resultCode = UpdateResultCode.Profile_UserNotFound
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNickname_SameValue()
        {
            string username = "user123";
            string newNickname = "currentNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);

            UserAccount existingUser = new UserAccount
            {
                idUser = 1,
                username = username,
                nickname = "currentNick" 
            };

            SetupMockUserSet(new List<UserAccount> { existingUser });

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_SameNicknameValue
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
                success = true,
                resultCode = UpdateResultCode.Profile_ChangeNicknameSuccess
            };

            UpdateResponse result = profileInformation.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestUpdateNicknameDatabaseValidationError()
        {
            string username = "user123";
            string newNickname = "newNick";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new DbEntityValidationException("Validation error"));

            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
                );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_DatabaseError
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
            
            ServiceDependencies dependencies = new ServiceDependencies(
                    mockSecurityHelper.Object,
                    mockValidationHelper.Object,
                    mockLoggerHelper.Object,
                    () => mockDbContext.Object
             );

            ProfileInformation profileInfo = new ProfileInformation(dependencies);

            UpdateResponse expectedResult = new UpdateResponse
            {
                success = false,
                resultCode = UpdateResultCode.Profile_UnexpectedError
            };

            UpdateResponse result = profileInfo.UpdateNickname(username, newNickname);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
