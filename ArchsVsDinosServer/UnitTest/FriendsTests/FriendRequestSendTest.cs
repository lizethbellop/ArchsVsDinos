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
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FriendsTests
{
    public class FriendRequestSendTest : BaseTestClass
    {
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<DbSet<FriendRequest>> mockFriendRequestSet;
        private FriendRequestLogic friendRequestLogic;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockValidationHelper = new Mock<IValidationHelper>();
            mockFriendRequestSet = new Mock<DbSet<FriendRequest>>();

            ServiceDependencies dependencies = new ServiceDependencies(
                null,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            friendRequestLogic = new FriendRequestLogic(dependencies);
        }

        [TestMethod]
        public void TestSendFriendRequestFromUserEmpty()
        {
            string fromUser = "";
            string toUser = "user2";

            mockValidationHelper.Setup(v => v.IsEmpty(fromUser)).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestToUserEmpty()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestBothUsersEmpty()
        {
            string fromUser = "";
            string toUser = "";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(true);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestSenderNotFound()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestReceiverNotFound()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestToYourself()
        {
            string fromUser = "user1";
            string toUser = "user1";

            UserAccount user = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_CannotSendToYourself
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestAlreadyFriends()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            Friendship friendship = new Friendship { idUser = 1, idUserFriend = 2 };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendshipSet(new List<Friendship> { friendship });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_AlreadyFriends
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestAlreadySent()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };
            FriendRequest existingRequest = new FriendRequest
            {
                idUser = 1,
                idReceiverUser = 2,
                status = "Pending"
            };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendshipSet(new List<Friendship>());
            SetupMockFriendRequestSet(new List<FriendRequest> { existingRequest });

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = false,
                ResultCode = FriendRequestResultCode.FriendRequest_RequestAlreadySent
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestSuccess()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendshipSet(new List<Friendship>());
            SetupMockFriendRequestSet(new List<FriendRequest>());

            mockFriendRequestSet.Setup(m => m.Create()).Returns(new FriendRequest());
            mockFriendRequestSet.Setup(m => m.Add(It.IsAny<FriendRequest>()));
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            FriendRequestResponse expectedResult = new FriendRequestResponse
            {
                Success = true,
                ResultCode = FriendRequestResultCode.FriendRequest_Success
            };

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestAddsCorrectData()
        {
            string fromUser = "user1";
            string toUser = "user2";

            UserAccount sender = new UserAccount { idUser = 1, username = "user1" };
            UserAccount receiver = new UserAccount { idUser = 2, username = "user2" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { sender, receiver });
            SetupMockFriendshipSet(new List<Friendship>());
            SetupMockFriendRequestSet(new List<FriendRequest>());

            FriendRequest capturedRequest = null;
            mockFriendRequestSet.Setup(m => m.Create()).Returns(new FriendRequest());
            mockFriendRequestSet.Setup(m => m.Add(It.IsAny<FriendRequest>()))
                .Callback<FriendRequest>(fr => capturedRequest = fr);
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.IsNotNull(capturedRequest);
            Assert.AreEqual(1, capturedRequest.idUser);
            Assert.AreEqual(2, capturedRequest.idReceiverUser);
            Assert.AreEqual("Pending", capturedRequest.status);
        }

        [TestMethod]
        public void TestSendFriendRequestEntityException()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestInvalidOperationException()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendFriendRequestUnexpectedException()
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

            FriendRequestResponse result = friendRequestLogic.SendFriendRequest(fromUser, toUser);

            Assert.AreEqual(expectedResult, result);
        }

        protected void SetupMockFriendRequestSet(List<FriendRequest> requests)
        {
            var queryableRequests = requests.AsQueryable();
            mockFriendRequestSet.As<IQueryable<FriendRequest>>().Setup(m => m.Provider).Returns(queryableRequests.Provider);
            mockFriendRequestSet.As<IQueryable<FriendRequest>>().Setup(m => m.Expression).Returns(queryableRequests.Expression);
            mockFriendRequestSet.As<IQueryable<FriendRequest>>().Setup(m => m.ElementType).Returns(queryableRequests.ElementType);
            mockFriendRequestSet.As<IQueryable<FriendRequest>>().Setup(m => m.GetEnumerator()).Returns(queryableRequests.GetEnumerator());
            mockDbContext.Setup(c => c.FriendRequest).Returns(mockFriendRequestSet.Object);
        }

        protected void SetupMockFriendshipSet(List<Friendship> friendships)
        {
            var queryableFriendships = friendships.AsQueryable();
            var mockFriendshipSet = new Mock<DbSet<Friendship>>();
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Provider).Returns(queryableFriendships.Provider);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Expression).Returns(queryableFriendships.Expression);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.ElementType).Returns(queryableFriendships.ElementType);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.GetEnumerator()).Returns(queryableFriendships.GetEnumerator());
            mockDbContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
        }
    }
}
