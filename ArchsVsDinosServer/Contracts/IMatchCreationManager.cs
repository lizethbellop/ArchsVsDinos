using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IMatchCreationManager
    {
        [OperationContract]
        Task<MatchCreationResponse> CreateMatch(MatchSettings settings);

        [OperationContract]
        Task<MatchJoinResponse> JoinMatch(string lobbyCode, int userId ,string nickname);

        [OperationContract]
        Task<bool> SendInvitation(string lobbyCode, string sender, List<string> guests);

    }
}
