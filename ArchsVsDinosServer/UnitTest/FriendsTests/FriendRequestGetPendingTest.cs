using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FriendsTests
{
    [TestClass]
    public class FriendRequestGetPendingTest : FriendRequestBaseTest
    {
        private Mock<IValidationHelper> mockValidationHelper;
        private FriendRequestLogic friendRequestLogic;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();
            base.BaseSetupFriendRequest();

            mockValidationHelper = new Mock<IValidationHelper>();

            ServiceDependencies dependencies = new ServiceDependencies(
                null,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            friendRequestLogic = new FriendRequestLogic(dependencies);
        }

        [TestMethod]
        public void TestGetPendingRequestsUsernameEmpty()
        {
            string username = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPendingRequestsUserNotFound()
        {
            string username = "nonexistent";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPendingRequestsNoPendingRequests()
        {
            string username = "user1";

            UserAccount user = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user });
            SetupMockFriendRequestSet(new List<FriendRequest>());

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = true,
                ResultCode = FriendRequestResultCode.FriendRequest_Success,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPendingRequestsWithPendingRequests()
        {
            string username = "user1";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "friend1" };
            UserAccount user3 = new UserAccount { idUser = 3, username = "friend2" };

            FriendRequest request1 = new FriendRequest
            {
                idUser = 2,
                idReceiverUser = 1,
                status = "Pending"
            };
            FriendRequest request2 = new FriendRequest
            {
                idUser = 3,
                idReceiverUser = 1,
                status = "Pending"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });
            SetupMockFriendRequestSet(new List<FriendRequest> { request1, request2 });

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_Success, result.ResultCode);
            Assert.AreEqual(2, result.Requests.Count);
            Assert.IsTrue(result.Requests.Contains("friend1"));
            Assert.IsTrue(result.Requests.Contains("friend2"));
        }

        [TestMethod]
        public void TestGetPendingRequestsIgnoresNonPendingRequests()
        {
            string username = "user1";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "friend1" };
            UserAccount user3 = new UserAccount { idUser = 3, username = "friend2" };

            FriendRequest request1 = new FriendRequest
            {
                idUser = 2,
                idReceiverUser = 1,
                status = "Pending"
            };
            FriendRequest request2 = new FriendRequest
            {
                idUser = 3,
                idReceiverUser = 1,
                status = "Rejected"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });
            SetupMockFriendRequestSet(new List<FriendRequest> { request1, request2 });

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Requests.Count);
            Assert.IsTrue(result.Requests.Contains("friend1"));
            Assert.IsFalse(result.Requests.Contains("friend2"));
        }

        [TestMethod]
        public void TestGetPendingRequestsEntityException()
        {
            string username = "user1";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("DB Error"));

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPendingRequestsInvalidOperationException()
        {
            string username = "user1";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new InvalidOperationException("Invalid Op"));

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetPendingRequestsUnexpectedException()
        {
            string username = "user1";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected"));

            FriendRequestListResponse expectedResult = new FriendRequestListResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError,
                Requests = new List<string>()
            };

            FriendRequestListResponse result = friendRequestLogic.GetPendingRequests(username);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
