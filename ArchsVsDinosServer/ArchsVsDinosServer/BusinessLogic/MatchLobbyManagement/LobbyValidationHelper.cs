using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Lobby;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyValidationHelper : ILobbyValidationHelper
    {
        private const int MaxPlayersLimit = 4;
        private const int MinPlayersLimit = 2;
            
        private readonly ILoggerHelper logger;

        public LobbyValidationHelper(ILoggerHelper logger)
        {
            this.logger = logger;
        }
        public void ValidateCreateLobby(MatchSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.HostNickname))
            {
                throw new ArgumentException("Host username cannot be null or empty.");
            }

            if (settings.MaxPlayers < MinPlayersLimit || settings.MaxPlayers > MaxPlayersLimit)
            {
                throw new ArgumentOutOfRangeException($"Max players must be between {MinPlayersLimit} and {MaxPlayersLimit}.");
            }
        }

        public void ValidateInviteGuests(List<string> guests)
        {
            if(guests == null || guests.Count == 0)
            {
                throw new ArgumentException("Guests list cannot be null or empty");
            }
        }

        public void ValidateJoinLobby(string matchCode, string username)
        {
            if(string.IsNullOrEmpty(matchCode))
            {
                throw new ArgumentException("Match code cannot be null or empty.");
            }

            if(string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be null or empty.");
            }
        }
    }
}
