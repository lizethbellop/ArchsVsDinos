using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IInvitationSendHelper
    {
        Task<bool> SendInvitation(string lobbyCode, string senderUsername, List<string> guests);
    }
}
