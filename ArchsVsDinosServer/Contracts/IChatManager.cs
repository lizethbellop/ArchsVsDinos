using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract =typeof(IChatManagerCallback))]
    public interface IChatManager
    {
        [OperationContract]
        void Connect(string username);
        [OperationContract]
        void Disconnect(string username);
        [OperationContract]
        void SendMessageToRoom(string roomId, string message);

        [OperationContract]
        void SendMessageToUser(string targetUser, string message);

    }
}
