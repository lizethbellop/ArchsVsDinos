using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(IFriendRequestCallback))]
    public interface IFriendRequestManager
    {
        [OperationContract(IsOneWay = true)]
        void SendFriendRequest(string fromUser, string toUser);

        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequest(string fromUser, string toUser);

        [OperationContract(IsOneWay = true)]
        void RejectFriendRequest(string fromUser, string toUser);

        [OperationContract(IsOneWay = true)]
        void GetPendingRequests(string username);

        [OperationContract]
        void Subscribe(string username);

        [OperationContract]
        void Unsubscribe(string username);

        [OperationContract(IsOneWay = true)]
        void GetSentRequests(string username);
    }
}
