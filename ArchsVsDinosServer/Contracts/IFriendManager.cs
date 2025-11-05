using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Response;

namespace Contracts
{
    [ServiceContract]
    public interface IFriendManager
    {
        [OperationContract]
        FriendResponse RemoveFriend(string username, string friendUsername);

        [OperationContract]
        FriendListResponse GetFriends(string username);

        [OperationContract]
        FriendCheckResponse AreFriends(string username, string friendUsername);
    }
}
