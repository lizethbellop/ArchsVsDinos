using ArchsVsDinosServer.Utils;
using Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;
using Contracts;
using Contracts.DTO;

namespace ArchsVsDinosServer.Services
{
    public class AuthenticationManager : IAuthenticationManager
    {
        public LoginResponse Login(string username, string password)
        {
            Authentication autentication = new Authentication();
            LoginResponse response = autentication.Login(username, password);
            return response;
        }
    }
}
