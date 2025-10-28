using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IFriendManager
    {
        [OperationContract]
        bool AddFriend(string username, string friendUsername);

        [OperationContract]
        bool RemoveFriend(string username, string friendUsername);

        [OperationContract]
        List<string> GetFriends(string username);

        [OperationContract]
        bool AreFriends(string username, string friendUsername);
    }
}
