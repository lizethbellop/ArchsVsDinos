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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FriendsTests
{
    public class FriendTest : BaseTestClass
    {
        private Mock<ISecurityHelper> mockSecurityHelper;
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<DbSet<Friendship>> mockFriendshipSet;
        private Mock<IDatabase> mockDatabase;
        private Mock<IDbContextTransaction> mockTransaction;
        private Friend friend;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockFriendshipSet = new Mock<DbSet<Friendship>>();
            mockDatabase = new Mock<IDatabase>();
            mockTransaction = new Mock<IDbContextTransaction>();

            mockDbContext.Setup(c => c.Database).Returns(mockDatabase.Object);
            mockDatabase.Setup(d => d.BeginTransaction()).Returns(mockTransaction.Object);

            ServiceDependencies dependencies = new ServiceDependencies(
                mockSecurityHelper.Object,
                mockValidationHelper.Object,
                mockLoggerHelper.Object,
                () => mockDbContext.Object
            );

            friend = new Friend(dependencies);
        }

        [TestMethod]
        public void TestRemoveFriendUsernameEmpty()
        {
            string username = "";
            string friendUsername = "friend1";

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendFriendUsernameEmpty()
        {
            string username = "user1";
            string friendUsername = "";

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendBothUsernamesEmpty()
        {
            string username = "";
            string friendUsername = "";

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendCannotRemoveYourself()
        {
            string username = "user1";
            string friendUsername = "user1";

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_CannotAddYourself
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendUserNotFound()
        {
            string username = "nonexistent";
            string friendUsername = "friend1";

            SetupMockUserSet(new List<UserAccount>());

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendFriendUserNotFound()
        {
            string username = "user1";
            string friendUsername = "nonexistent";

            UserAccount user = new UserAccount
            {
                idUser = 1,
                username = "user1"
            };

            SetupMockUserSet(new List<UserAccount> { user });

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendNotFriends()
        {
            string username = "user1";
            string friendUsername = "user2";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "user2" };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship>());

            FriendResponse expectedResult = new FriendResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_NotFriends
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendSuccess()
        {
            string username = "user1";
            string friendUsername = "user2";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "user2" };

            Friendship friendship1 = new Friendship { idUser = 1, idUserFriend = 2 };
            Friendship friendship2 = new Friendship { idUser = 2, idUserFriend = 1 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship> { friendship1, friendship2 });

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            FriendResponse expectedResult = new FriendResponse
            {
                Success = true,
                ResultCode = FriendResultCode.Friend_Success
            };

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRemoveFriendCallsTransactionCommit()
        {
            string username = "user1";
            string friendUsername = "user2";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "user2" };

            Friendship friendship1 = new Friendship { idUser = 1, idUserFriend = 2 };
            Friendship friendship2 = new Friendship { idUser = 2, idUserFriend = 1 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship> { friendship1, friendship2 });

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            friend.RemoveFriend(username, friendUsername);

            mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        [TestMethod]
        public void TestRemoveFriendCallsTransactionRollbackOnError()
        {
            string username = "user1";
            string friendUsername = "user2";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "user2" };

            Friendship friendship1 = new Friendship { idUser = 1, idUserFriend = 2 };
            Friendship friendship2 = new Friendship { idUser = 2, idUserFriend = 1 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship> { friendship1, friendship2 });

            mockDbContext.Setup(c => c.SaveChanges()).Throws(new Exception("Database error"));

            FriendResponse result = friend.RemoveFriend(username, friendUsername);

            mockTransaction.Verify(t => t.Rollback(), Times.Once);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void TestGetFriendsUsernameEmpty()
        {
            string username = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);

            FriendListResponse expectedResult = new FriendListResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_EmptyUsername,
                Friends = new List<string>()
            };

            FriendListResponse result = friend.GetFriends(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetFriendsUserNotFound()
        {
            string username = "nonexistent";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            FriendListResponse expectedResult = new FriendListResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound,
                Friends = new List<string>()
            };

            FriendListResponse result = friend.GetFriends(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetFriendsNoFriends()
        {
            string username = "user1";

            UserAccount user = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user });
            SetupMockFriendshipSet(new List<Friendship>());

            FriendListResponse expectedResult = new FriendListResponse
            {
                Success = true,
                ResultCode = FriendResultCode.Friend_Success,
                Friends = new List<string>()
            };

            FriendListResponse result = friend.GetFriends(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestGetFriendsWithFriends()
        {
            string username = "user1";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "friend1" };
            UserAccount user3 = new UserAccount { idUser = 3, username = "friend2" };

            Friendship friendship1 = new Friendship
            {
                idUser = 1,
                idUserFriend = 2,
                UserAccount1 = user2
            };
            Friendship friendship2 = new Friendship
            {
                idUser = 1,
                idUserFriend = 3,
                UserAccount1 = user3
            };

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });
            SetupMockFriendshipSet(new List<Friendship> { friendship1, friendship2 });

            FriendListResponse expectedResult = new FriendListResponse
            {
                Success = true,
                ResultCode = FriendResultCode.Friend_Success,
                Friends = new List<string> { "friend1", "friend2" }
            };

            FriendListResponse result = friend.GetFriends(username);

            Assert.AreEqual(expectedResult, result);  // ✅ UN SOLO ASSERT
        }

        [TestMethod]
        public void TestGetFriendsDatabaseError()
        {
            string username = "user1";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            FriendListResponse expectedResult = new FriendListResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_DatabaseError,
                Friends = new List<string>()
            };

            FriendListResponse result = friend.GetFriends(username);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsUsernameEmpty()
        {
            string username = "";
            string friendUsername = "friend1";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(friendUsername)).Returns(false);

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_EmptyUsername,
                AreFriends = false
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsFriendUsernameEmpty()
        {
            string username = "user1";
            string friendUsername = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(false);
            mockValidationHelper.Setup(v => v.IsEmpty(friendUsername)).Returns(true);

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_EmptyUsername,
                AreFriends = false
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsUserNotFound()
        {
            string username = "nonexistent";
            string friendUsername = "friend1";

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount>());

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound,
                AreFriends = false
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsFriendUserNotFound()
        {
            string username = "user1";
            string friendUsername = "nonexistent";

            UserAccount user = new UserAccount { idUser = 1, username = "user1" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user });

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = false,
                ResultCode = FriendResultCode.Friend_UserNotFound,
                AreFriends = false
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsTrue()
        {
            string username = "user1";
            string friendUsername = "friend1";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "friend1" };

            Friendship friendship = new Friendship { idUser = 1, idUserFriend = 2 };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship> { friendship });

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = true,
                ResultCode = FriendResultCode.Friend_Success,
                AreFriends = true
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestAreFriendsFalse()
        {
            string username = "user1";
            string friendUsername = "friend1";

            UserAccount user1 = new UserAccount { idUser = 1, username = "user1" };
            UserAccount user2 = new UserAccount { idUser = 2, username = "friend1" };

            mockValidationHelper.Setup(v => v.IsEmpty(It.IsAny<string>())).Returns(false);
            SetupMockUserSet(new List<UserAccount> { user1, user2 });
            SetupMockFriendshipSet(new List<Friendship>());

            FriendCheckResponse expectedResult = new FriendCheckResponse
            {
                Success = true,
                ResultCode = FriendResultCode.Friend_Success,
                AreFriends = false
            };

            FriendCheckResponse result = friend.AreFriends(username, friendUsername);

            Assert.AreEqual(expectedResult, result);
        }

        protected void SetupMockFriendshipSet(List<Friendship> friendships)
        {
            var queryableFriendships = friendships.AsQueryable();
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Provider).Returns(queryableFriendships.Provider);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Expression).Returns(queryableFriendships.Expression);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.ElementType).Returns(queryableFriendships.ElementType);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.GetEnumerator()).Returns(queryableFriendships.GetEnumerator());
            mockDbContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
        }
    }
}
