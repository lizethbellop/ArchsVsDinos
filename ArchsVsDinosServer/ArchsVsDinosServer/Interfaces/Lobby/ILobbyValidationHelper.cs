using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Lobby
{
    public interface ILobbyValidationHelper
    {
        void ValidateCreateLobby(MatchSettings settings);
        void ValidateJoinLobby(string matchCode, string username);
        void ValidateInviteGuests(List<string> guests);
    }
}
