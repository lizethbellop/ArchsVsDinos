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
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FriendsTests
{
    [TestClass]
    public class FriendRequestAcceptTest : FriendRequestBaseTest
    {
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<IDatabase> mockDatabase;
        private Mock<IDbContextTransaction> mockTransaction;
        private FriendRequestLogic friendRequestLogic;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();
            base.BaseSetupFriendRequest();

            mockValidationHelper = new Mock<IValidationHelper>();
            mockDatabase = new Mock<IDatabase>();
            mockTransaction = new Mock<IDbContextTransaction>();

            mockDbContext.Setup(c => c.Database).Returns(mockDatabase.Object);
            mockDatabase.Setup(d => d.BeginTransaction()).Returns(mockTransaction.Object);

            ServiceDependencies dependencies = new ServiceDependencies(
                null,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            friendRequestLogic = new FriendRequestLogic(dependencies);
        }

        [TestMethod]
        public void TestAcceptFriendRequestFromUserEmpty()
        {
            string fromUser = "";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(fromUser)).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestToUserEmpty()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestBothUsersEmpty()
        {
            string fromUser = "";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestSenderNotFound()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestReceiverNotFound()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestNotFound()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestNotPending()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            FriendRequest request = new FriendRequest
            {
                idUser = 1,
                idReceiverUser = 2,
                status = "Rejected"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendRequestSet(new List<FriendRequest> { request });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_RequestNotFound
            };

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestSuccess()
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
            SetupMockFriendshipSet(new List<Friendship>());

            mockFriendshipSet.Setup(m => m.Create()).Returns(new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()));
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = true,
                ResultCode = FriendRequestResultCode.FriendRequest_Success
            };

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestCreatesCorrectFriendships()
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
            SetupMockFriendshipSet(new List<Friendship>());

            List<Friendship> addedFriendships = new List<Friendship>();
            mockFriendshipSet.Setup(m => m.Create()).Returns(new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()))
                .Callback<Friendship>(f => addedFriendships.Add(f));
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(2, addedFriendships.Count);
            Assert.IsTrue(addedFriendships.Any(f => f.idUser == 1 && f.idUserFriend == 2));
            Assert.IsTrue(addedFriendships.Any(f => f.idUser == 2 && f.idUserFriend == 1));
        }

        [TestMethod]
        public void TestAcceptFriendRequestUpdatesStatus()
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
            SetupMockFriendshipSet(new List<Friendship>());

            mockFriendshipSet.Setup(m => m.Create()).Returns(new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()));
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual("Accepted", request.status);
        }

        [TestMethod]
        public void TestAcceptFriendRequestCommitsTransaction()
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
            SetupMockFriendshipSet(new List<Friendship>());

            mockFriendshipSet.Setup(m => m.Create()).Returns(new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()));
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        [TestMethod]
        public void TestAcceptFriendRequestRollbackOnError()
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
            SetupMockFriendshipSet(new List<Friendship>());

            mockFriendshipSet.Setup(m => m.Create()).Returns(new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()));
            mockDbContext.Setup(c => c.SaveChanges()).Throws(new Exception("DB Error"));

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            mockTransaction.Verify(t => t.Rollback(), Times.Once);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void TestAcceptFriendRequestEntityException()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestInvalidOperationException()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestUnexpectedException()
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
