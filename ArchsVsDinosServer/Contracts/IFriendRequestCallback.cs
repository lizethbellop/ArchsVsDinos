 using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IFriendRequestCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnFriendRequestSent(bool success);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestAccepted(bool success);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestRejected(bool success);

        [OperationContract(IsOneWay = true)]
        void OnPendingRequestsReceived(List<string> requests);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived(string fromUser);

        [OperationContract(IsOneWay = true)]
        void OnSentRequestsReceived(string[] requests);
    }
}
