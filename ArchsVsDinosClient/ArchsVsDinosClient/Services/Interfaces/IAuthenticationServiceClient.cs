using ArchsVsDinosClient.AuthenticationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IAuthenticationServiceClient : IDisposable
    {
        event Action<string, string> ConnectionError;
        Task<LoginResponse> LoginAsync(string username, string password);
    }
}
