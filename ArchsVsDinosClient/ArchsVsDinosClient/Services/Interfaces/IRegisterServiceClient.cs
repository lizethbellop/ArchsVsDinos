using ArchsVsDinosClient.RegisterService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IRegisterServiceClient : IDisposable
    {
        event Action<string, string> ConnectionError;

        Task<bool> SendEmailRegisterAsync(string email);
        Task<RegisterResponse> RegisterUserAsync(UserAccountDTO user, string code);

        bool IsServerAvailable { get; }
    }

}
