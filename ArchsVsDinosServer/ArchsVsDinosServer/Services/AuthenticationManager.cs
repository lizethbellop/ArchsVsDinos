using ArchsVsDinosServer.Utils;
using Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;

namespace ArchsVsDinosServer.Services
{
    public class AuthenticationManager : IAuthenticationManager
    {
        public bool Login(string username, string password)
        {
            Authentication autentication = new Authentication();
            return autentication.Login(username, password);
        }
    }
}
