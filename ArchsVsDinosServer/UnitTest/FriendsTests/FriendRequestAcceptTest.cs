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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_EmptyUsername, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestToUserEmpty()
        {
            string fromUser = "user1";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(fromUser)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(toUser)).Returns(true);

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_EmptyUsername, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestBothUsersEmpty()
        {
            string fromUser = "";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_EmptyUsername, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestSenderNotFound()
        {
            string fromUser = "nonexistent";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_UserNotFound, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestReceiverNotFound()
        {
            string fromUser = "user1";
            string toUser = "nonexistent";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender });

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_UserNotFound, result.ResultCode);
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_RequestNotFound, result.ResultCode);
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_RequestNotFound, result.ResultCode);
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_Success, result.ResultCode);
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
            // IMPORTANTE: Usar Returns con función para crear nueva instancia cada vez
            mockFriendshipSet.Setup(m => m.Create()).Returns(() => new Friendship());
            mockFriendshipSet.Setup(m => m.Add(It.IsAny<Friendship>()))
                .Callback<Friendship>(f =>
                {
                    // Crear una copia con los valores actuales en el momento del Add
                    addedFriendships.Add(new Friendship
                    {
                        idUser = f.idUser,
                        idUserFriend = f.idUserFriend,
                        status = f.status
                    });
                });
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.AreEqual(2, addedFriendships.Count, "Debería haber agregado exactamente 2 friendships");

            var friendship1 = addedFriendships.FirstOrDefault(f => f.idUser == 1 && f.idUserFriend == 2);
            var friendship2 = addedFriendships.FirstOrDefault(f => f.idUser == 2 && f.idUserFriend == 1);

            Assert.IsNotNull(friendship1, "Debería existir friendship de user1 a user2");
            Assert.AreEqual("Active", friendship1.status, "El status de friendship1 debería ser Active");

            Assert.IsNotNull(friendship2, "Debería existir friendship de user2 a user1");
            Assert.AreEqual("Active", friendship2.status, "El status de friendship2 debería ser Active");
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

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_DatabaseError, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestInvalidOperationException()
        {
            string fromUser = "user1";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new InvalidOperationException("Invalid Op"));

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_UnexpectedError, result.ResultCode);
        }

        [TestMethod]
        public void TestAcceptFriendRequestUnexpectedException()
        {
            string fromUser = "user1";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected"));

            FriendRequestResponse result = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FriendRequestResultCode.FriendRequest_UnexpectedError, result.ResultCode);
        }
    }
}
