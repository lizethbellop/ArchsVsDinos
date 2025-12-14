using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Lobby;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class MatchCreationManager : IMatchCreationManager
    {
        private readonly ILobbyLogic lobbyLogic;
        private readonly ILoggerHelper logger;

        public MatchCreationManager(
        ILobbyLogic lobbyLogic,
        ILoggerHelper logger)
        {
            this.lobbyLogic = lobbyLogic;
            this.logger = logger;
        }
        public async Task<MatchCreationResponse> CreateMatch(MatchSettings settings)
        {
            if (settings == null)
            {
                return new MatchCreationResponse
                {
                    Success = false,
                    Message = "Match settings cannot be null."
                };
            }

            try
            {
                return await lobbyLogic.CreateLobby(settings);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"CreateMatch timeout: {ex.Message}");
                return new MatchCreationResponse
                {
                    Success = false,
                    Message = "Server is taking too long to respond."
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error while creating match.", ex);
                return new MatchCreationResponse
                {
                    Success = false,
                    Message = "Unexpected server error."
                };
            }
        }

        public async Task<MatchJoinResponse> JoinMatch(string lobbyCode, string nickname)
        {
            if(string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(nickname))
            {
                return new MatchJoinResponse
                {
                    Success = false,
                    Message = "Lobby code or nickname invalid"
                };
            }

            try
            {
                return await lobbyLogic.JoinLobby(lobbyCode, nickname);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"CreateMatch timeout: {ex.Message}");
                return new MatchJoinResponse
                {
                    Success = false,
                    Message = "Server is taking too long to respond."
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error while creating match.", ex);
                return new MatchJoinResponse
                {
                    Success = false,
                    Message = "Unexpected server error."
                };
            }
        }

        public async Task<bool> SendInvitation(string lobbyCode, string sender, List<string> guests)
        {
            if(string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(sender) || guests == null || guests.Count == 0)
            {
                return false;
            }

            try
            {
                return await lobbyLogic.SendInvitations(lobbyCode, sender, guests);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"SendInvitation timeout: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error while sending invitations.", ex);
                return false;
            }
        }
    }
}
