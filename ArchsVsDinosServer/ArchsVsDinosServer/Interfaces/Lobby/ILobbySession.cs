using ArchsVsDinosServer.Model;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Lobby
{
    public interface ILobbySession
    {
        void CreateLobby(string lobbyCode, ActiveLobbyData lobbyData);

        ActiveLobbyData GetLobby(string lobbyCode);

        bool LobbyExists(string lobbyCode);

        void RemoveLobby(string lobbyCode);

        void ConnectPlayerCallback(string lobbyCode, string playerName, ILobbyManagerCallback callback);
        
        void DisconnectPlayerCallback(string lobbyCode, string playerName);

        void Broadcast(string lobbyCode, Action<ILobbyManagerCallback> broadcastAction);

        IEnumerable<ActiveLobbyData> GetAllLobbies();
        ILobbyManagerCallback GetPlayerCallbackByUsername(string lobbyCode, string username);
        ILobbyManagerCallback FindUserCallbackInAnyLobby(string username);

    }
}
