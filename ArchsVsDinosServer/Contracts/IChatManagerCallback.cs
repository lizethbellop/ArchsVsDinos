using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IChatManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string roomId, string fromUser, string message);

        [OperationContract(IsOneWay = true)]
        void ReceiveSystemNotification(string notification);
    }
}
