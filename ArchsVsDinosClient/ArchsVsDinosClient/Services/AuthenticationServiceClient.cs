using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class AuthenticationServiceClient : IAuthenticationServiceClient
    {

        private readonly AuthenticationManagerClient client;

        public AuthenticationServiceClient()
        {
            client = new AuthenticationManagerClient();
        }
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            return await client.LoginAsync(username, password);
        }
    }
}
