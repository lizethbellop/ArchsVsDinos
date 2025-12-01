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
    public class FriendRequestRejectTest : FriendRequestBaseTest
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
        public void TestRejectFriendRequestFromUserEmpty()
        {
            string fromUser = "";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(fromUser)).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestToUserEmpty()
        {
            string fromUser = "user1";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(fromUser)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(toUser)).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestBothUsersEmpty()
        {
            string fromUser = "";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestSenderNotFound()
        {
            string fromUser = "nonexistent";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestReceiverNotFound()
        {
            string fromUser = "user1";
            string toUser = "nonexistent";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestNotFound()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendRequestSet(new List<FriendRequest>());

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_RequestNotFound
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestNotPending()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            FriendRequest request = new FriendRequest
            {
                idUser = 1,
                idReceiverUser = 2,
                status = "Accepted"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendRequestSet(new List<FriendRequest> { request });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_RequestNotFound
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestSuccess()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            FriendRequest request = new FriendRequest
            {
                idUser = 1,
                idReceiverUser = 2,
                status = "Pending"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendRequestSet(new List<FriendRequest> { request });

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = true,
                ResultCode = FriendRequestResultCode.FriendRequest_Success
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestUpdatesStatus()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            FriendRequest request = new FriendRequest
            {
                idUser = 1,
                idReceiverUser = 2,
                status = "Pending"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendRequestSet(new List<FriendRequest> { request });

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual("Rejected", request.status);
        }

        [TestMethod]
        public void TestRejectFriendRequestEntityException()
        {
            string fromUser = "user1";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("DB Error"));

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestInvalidOperationException()
        {
            string fromUser = "user1";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new InvalidOperationException("Invalid Op"));

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRejectFriendRequestUnexpectedException()
        {
            string fromUser = "user1";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected"));

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
            };

            FriendRequestResponse result = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
