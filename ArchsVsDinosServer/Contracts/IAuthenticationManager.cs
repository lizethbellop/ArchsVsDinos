using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace Contracts
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        [OperationContract]
        LoginResponse Login(string username, string password);

        [OperationContract]
        void Logout(string username);

        [OperationContract]
        RecoveryCodeResponse SendRecoveryCode(string username);

        [OperationContract]
        bool ValidateRecoveryCode(string username, string code);

        [OperationContract]
        bool UpdatePassword(string username, string newPassword);
    }

}
