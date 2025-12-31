using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Model;

namespace ArchsVsDinosServer.Interfaces.Game
{
    public interface IGameLogic
    {
        Task<bool> InitializeMatch(string matchCode, List<GamePlayerInitDTO> players);

        CardInGame DrawCard(string matchCode, int userId);

        bool ExchangeCard(string matchCode, int userId, ExchangeCardDTO data);
        DinoInstance PlayDinoHead(string matchCode, int userId, int cardId);

        void TakeCardFromDiscardPile(string matchCode, int userId, int cardId);

        bool AttachBodyPart(string matchCode, int userId, AttachBodyPartDTO data);

        Task<bool> Provoke(string matchCode, int userId, ArmyType type);

        bool EndTurn(string matchCode, int userId);

        GameEndResult EndGame(string matchCode, GameEndType gameEnd, string reason);
        void LeaveGame(string matchCode, int userId);
    }
}
