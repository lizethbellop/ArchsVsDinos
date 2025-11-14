using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class GameManager : IGameManager
    {
        public PlayCardResultCode AttachBodyPartToDino(int matchId, int userId, int cardId, int dinoHeadCardId)
        {
            throw new NotImplementedException();
        }

        public DrawCardResultCode DrawCard(int matchId, int userId, int drawPileNumber)
        {
            throw new NotImplementedException();
        }

        public EndTurnResultCode EndTurn(int matchId, int userId)
        {
            throw new NotImplementedException();
        }

        public CentralBoardDTO GetCentralBoard(int matchId)
        {
            throw new NotImplementedException();
        }

        public GameStateDTO GetGameState(int matchId)
        {
            throw new NotImplementedException();
        }

        public PlayerHandDTO GetPlayerHand(int matchId, int userId)
        {
            throw new NotImplementedException();
        }

        public GameSetupResultCode InitializeGame(int matchId)
        {
            throw new NotImplementedException();
        }

        public PlayCardResultCode PlayDinoHead(int matchId, int userId, int cardId)
        {
            throw new NotImplementedException();
        }

        public ProvokeResultCode ProvokeArchArmy(int matchId, int userId, string armyType)
        {
            throw new NotImplementedException();
        }

        public GameSetupResultCode StartGame(int matchId)
        {
            throw new NotImplementedException();
        }
    }
}
