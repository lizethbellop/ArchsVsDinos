using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchsVsDinosClient.Services
{
    public class LobbyCallbackManager : ILobbyManagerCallback
    {
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> OnCreatedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnJoinedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnPlayerLeftLobby;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> OnPlayerListUpdated;
        public event Action<string, string> OnPlayerKicked;
        public event Action<string, bool> OnPlayerReady;
        public event Action OnGameStart;

        public void PlayerJoinedLobby(string nickname)
        {
            var localPlayerDto = new ArchsVsDinosClient.DTO.LobbyPlayerDTO
            {
                Nickname = nickname,
                IsReady = false
            };

            OnJoinedLobby?.Invoke(localPlayerDto);
        }

        public void PlayerLeftLobby(string nickname)
        {
            var localPlayerDto = new ArchsVsDinosClient.DTO.LobbyPlayerDTO
            {
                Nickname = nickname
            };

            OnPlayerLeftLobby?.Invoke(localPlayerDto);
        }

        public void UpdateListOfPlayers(ArchsVsDinosClient.LobbyService.LobbyPlayerDTO[] servicePlayersArray)
        {
            if (servicePlayersArray == null) return;

            var localPlayersList = servicePlayersArray.Select(servicePlayer => new ArchsVsDinosClient.DTO.LobbyPlayerDTO
            {
                IdPlayer = servicePlayer.UserId,
                Nickname = servicePlayer.Nickname,
                IsReady = servicePlayer.IsReady,
                IsHost = servicePlayer.IsHost
            }).ToList();

            OnPlayerListUpdated?.Invoke(localPlayersList);
        }

        public void PlayerReadyStatusChanged(string nickname, bool isReady)
        {
            OnPlayerReady?.Invoke(nickname, isReady);
        }

        public void GameStarting()
        {
            OnGameStart?.Invoke();
        }

        public void PlayerKicked(string nickname, string reason)
        {
            OnPlayerKicked?.Invoke(nickname, reason);
        }
    }
}