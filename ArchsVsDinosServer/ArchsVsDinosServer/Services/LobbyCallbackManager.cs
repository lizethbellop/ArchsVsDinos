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

        }

    }
    
}