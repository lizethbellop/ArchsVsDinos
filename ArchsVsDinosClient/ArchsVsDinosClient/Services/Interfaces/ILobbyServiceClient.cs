using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface ILobbyServiceClient
    {
        event Action<LobbyPlayerDTO, string> LobbyCreated;
        void CreateLobby(UserAccountDTO userAccount);
    }
}
