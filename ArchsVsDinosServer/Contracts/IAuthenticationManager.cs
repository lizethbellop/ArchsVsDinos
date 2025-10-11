using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO;
namespace Contracts
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        [OperationContract]
        LoginResponse Login(string username, string password);
    }

}
