using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;

namespace ArchsVsDinosClient.Services
{
    public class LobbyCallbackManager : ILobbyManagerCallback
    {
        private GameConnectionTimer connectionTimer; // ← NUEVO

        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> OnCreatedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnJoinedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnPlayerLeftLobby;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> OnPlayerListUpdated;
        public event Action<string, string> OnPlayerKicked;
        public event Action<string, bool> OnPlayerReady;
        public event Action<LobbyInvitationDTO> OnLobbyInvitationReceived;
        public event Action OnGameStart;

        /// <summary>
        /// Establece el timer que se reseteará en cada callback
        /// </summary>
        public void SetConnectionTimer(GameConnectionTimer timer)
        {
            this.connectionTimer = timer;
        }

        public void PlayerJoinedLobby(string nickname)
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                var player = new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Nickname = nickname,
                    IsReady = false
                };
                OnJoinedLobby?.Invoke(player);
            }, nameof(PlayerJoinedLobby));
        }

        public void PlayerLeftLobby(string nickname)
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                var player = new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Nickname = nickname
                };
                OnPlayerLeftLobby?.Invoke(player);
            }, nameof(PlayerLeftLobby));
        }

        public void UpdateListOfPlayers(ArchsVsDinosClient.LobbyService.LobbyPlayerDTO[] servicePlayers)
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                if (servicePlayers == null) return;
                var players = servicePlayers.Select(p => new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    IdPlayer = p.UserId,
                    Username = p.Username,
                    Nickname = p.Nickname,
                    IsReady = p.IsReady,
                    IsHost = p.IsHost,
                    ProfilePicture = p.ProfilePicture
                }).ToList();
                OnPlayerListUpdated?.Invoke(players);
            }, nameof(UpdateListOfPlayers));
        }

        public void PlayerReadyStatusChanged(string nickname, bool isReady)
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                OnPlayerReady?.Invoke(nickname, isReady);
            }, nameof(PlayerReadyStatusChanged));
        }

        public void GameStarting()
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                OnGameStart?.Invoke();
            }, nameof(GameStarting));
        }

        public void PlayerKicked(string nickname, string reason)
        {
            connectionTimer?.Reset(); // ← AGREGAR
            SafeInvoke(() =>
            {
                OnPlayerKicked?.Invoke(nickname, reason);
            }, nameof(PlayerKicked));
        }

        public void LobbyInvitationReceived(LobbyInvitationDTO invitation)
        {
            connectionTimer?.Reset();
            SafeInvoke(() =>
            {
                OnLobbyInvitationReceived?.Invoke(invitation);
            }, nameof(LobbyInvitationReceived));
        }

        private void SafeInvoke(Action action, string methodName)
        {
            try
            {
                action();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[CALLBACK] CommunicationException in {methodName}: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[CALLBACK] TimeoutException in {methodName}: {ex.Message}");
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"[CALLBACK] ObjectDisposedException in {methodName}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[CALLBACK] InvalidOperationException in {methodName}: {ex.Message}");
            }
        }
    }

}