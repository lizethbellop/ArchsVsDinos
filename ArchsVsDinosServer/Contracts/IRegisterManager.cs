using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    internal class IRegisterManager
    {
        [ServiceContract]
        public interface IRegisterManager
        {
            [OperationContract]
            bool Register(UserAccount userAccount);

            [OperationContract]
            bool SendEmailRegister(string email);

            [OperationContract]
            bool checkCode(string code);


        }
    }
}
