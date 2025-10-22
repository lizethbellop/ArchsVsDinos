using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IRegisterManager
    {
        [OperationContract]
        bool RegisterUser(UserAccountDTO userAccountDTO, string code);

        [OperationContract]
        bool SendEmailRegister(string email);
    }

}
