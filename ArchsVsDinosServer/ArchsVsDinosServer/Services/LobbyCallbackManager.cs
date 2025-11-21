using ArchsVsDinosServer.Interfaces;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace ArchsVsDinosServer.Services
{
    public class LobbyCallbackManager
    {

        public event Action<LobbyPlayerDTO, string> OnCreatedMatch;
        private readonly ILoggerHelper loggerHelper;

        public LobbyCallbackManager(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper;
        }

        public void CreatedLobby(LobbyPlayerDTO hostLobbyPlayerDTO, string matchCode)
        {
            try
            {
                if (hostLobbyPlayerDTO == null || string.IsNullOrWhiteSpace(matchCode))
                {
                    loggerHelper.LogWarning("Invalid data to create match.");
                    return;
                }

                OnCreatedMatch?.Invoke(hostLobbyPlayerDTO, matchCode);

            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while creating match", ex);
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation while creating match", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error while creating match", ex);
            }
        }

        public void JoinedLobby(LobbyPlayerDTO userAccountDTO)
        {
            try
            {
                if (userAccountDTO == null)
                {
                    loggerHelper.LogWarning("Invalid player joined lobby.");
                    return;
                }

                loggerHelper.LogInfo($"Player {userAccountDTO.Username} joined lobby.");
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while joining to the lobby", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Error in JoinedLobby notification.", ex);
            }
        }
        public void LobbyCancelled(string matchCode)
        {
            try
            {
                loggerHelper.LogInfo($"Lobby {matchCode} was cancelled.");
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while notifying lobby cancellation", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in LobbyCancelled notification.", ex);
            }
        }

        public void LeftLobby(LobbyPlayerDTO playerWhoLeft)
        {
            try
            {
                if (playerWhoLeft == null)
                {
                    loggerHelper.LogWarning("Invalid player leaving lobby.");
                    return;
                }

                loggerHelper.LogInfo(
                    $"Player {playerWhoLeft.Username} left the lobby."
                );
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while notifying player leaving lobby", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in LeftLobby notification.", ex);
            }
        }

        public void ExpelledFromLobby(LobbyPlayerDTO expelledPlayer)
        {
            try
            {
                if (expelledPlayer == null)
                {
                    loggerHelper.LogWarning("Invalid expelled player.");
                    return;
                }

                loggerHelper.LogInfo(
                    $"Player {expelledPlayer.Username} was expelled from the lobby."
                );
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while notifying expelled player", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in ExpelledPlayerFromLobby notification.", ex);
            }
        }

    }
    
}