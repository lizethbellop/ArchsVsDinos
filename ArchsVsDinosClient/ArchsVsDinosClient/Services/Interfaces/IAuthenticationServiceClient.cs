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
        Task LogoutAsync(string username);
        Task<RecoveryCodeResponse> SendRecoveryCodeAsync(string username);
        Task<bool> ValidateRecoveryCodeAsync(string username, string code);
        Task<bool> UpdatePasswordAsync(string username, string newPassword);

        bool IsServerAvailable { get; }
        string LastErrorTitle { get; }
        string LastErrorMessage { get; }
    }

}
