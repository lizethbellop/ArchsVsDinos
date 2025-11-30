using ArchsVsDinosServer;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FriendsTests
{
    public class FriendRequestBaseTest : BaseTestClass
    {
        protected Mock<DbSet<FriendRequest>> mockFriendRequestSet;
        protected Mock<DbSet<Friendship>> mockFriendshipSet;

        protected void BaseSetupFriendRequest()
        {
            mockFriendRequestSet = new Mock<DbSet<FriendRequest>>();
            mockFriendshipSet = new Mock<DbSet<Friendship>>();
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
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Provider).Returns(queryableFriendships.Provider);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Expression).Returns(queryableFriendships.Expression);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.ElementType).Returns(queryableFriendships.ElementType);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.GetEnumerator()).Returns(queryableFriendships.GetEnumerator());
            mockDbContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
        }
    }
}
