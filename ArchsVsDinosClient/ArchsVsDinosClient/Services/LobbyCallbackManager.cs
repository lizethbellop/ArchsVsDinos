using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;

namespace ArchsVsDinosClient.Services
{
    public sealed class LobbyCallbackManager : ILobbyManagerCallback
    {
        private GameConnectionTimer connectionTimer;

        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> OnCreatedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnJoinedLobby;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> OnPlayerLeftLobby;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> OnPlayerListUpdated;
        public event Action<string, string> OnPlayerKicked;
        public event Action<string, bool> OnPlayerReady;
        public event Action<LobbyInvitationDTO> OnLobbyInvitationReceived;
        public event Action OnGameStart;

        public void SetConnectionTimer(GameConnectionTimer timer)
        {
            connectionTimer = timer;
            MarkActivity();
        }

        public void PlayerJoinedLobby(string nickname)
        {
            MarkActivity();

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
            MarkActivity();

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
            MarkActivity();

            SafeInvoke(() =>
            {
                if (servicePlayers == null)
                {
                    return;
                }

                List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> players = servicePlayers
                    .Select(p => new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                    {
                        IdPlayer = p.UserId,
                        Username = p.Username,
                        Nickname = p.Nickname,
                        IsReady = p.IsReady,
                        IsHost = p.IsHost,
                        ProfilePicture = p.ProfilePicture
                    })
                    .ToList();

                OnPlayerListUpdated?.Invoke(players);
            }, nameof(UpdateListOfPlayers));
        }

        public void PlayerReadyStatusChanged(string nickname, bool isReady)
        {
            MarkActivity();

            SafeInvoke(() =>
            {
                OnPlayerReady?.Invoke(nickname, isReady);
            }, nameof(PlayerReadyStatusChanged));
        }

        public void GameStarting()
        {
            MarkActivity();

            SafeInvoke(() =>
            {
                OnGameStart?.Invoke();
            }, nameof(GameStarting));
        }

        public void PlayerKicked(string nickname, string reason)
        {
            MarkActivity();

            SafeInvoke(() =>
            {
                OnPlayerKicked?.Invoke(nickname, reason);
            }, nameof(PlayerKicked));
        }

        public void LobbyInvitationReceived(LobbyInvitationDTO invitation)
        {
            MarkActivity();

            SafeInvoke(() =>
            {
                OnLobbyInvitationReceived?.Invoke(invitation);
            }, nameof(LobbyInvitationReceived));
        }

        private void MarkActivity()
        {
            connectionTimer?.NotifyActivity();
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