using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Logout(string username)
        {
            Authentication authenticationLogic = new Authentication();
            authenticationLogic.Logout(username);
        }
    }
}
