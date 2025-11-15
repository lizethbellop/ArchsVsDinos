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
    public class MatchLobbyCallbackManager
    {

        public event Action<LobbyPlayerDTO, string> OnCreatedMatch;
        private readonly ILoggerHelper loggerHelper;

        public MatchLobbyCallbackManager(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper;
        }

        public void CreatedMatch(LobbyPlayerDTO hostLobbyPlayerDTO, string matchCode)
        {
            try
            {
                if (hostLobbyPlayerDTO == null || string.IsNullOrWhiteSpace(matchCode))
                {
                    loggerHelper.LogWarning("Invalid data to create match.");
                    return;
                }

                OnCreatedMatch?.Invoke(hostLobbyPlayerDTO, matchCode);

                loggerHelper.LogInfo($"Notificación de creación de partida enviada a: {hostLobbyPlayerDTO.Username}, código: {matchCode}");
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

    }
}