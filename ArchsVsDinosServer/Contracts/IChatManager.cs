using Contracts.DTO.Result_Codes;
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
        
        [OperationContract(IsOneWay = true)]
        void Disconnect(string username);
        
        [OperationContract(IsOneWay = true)]
        void SendMessageToRoom(string message, string username);


    }
}
