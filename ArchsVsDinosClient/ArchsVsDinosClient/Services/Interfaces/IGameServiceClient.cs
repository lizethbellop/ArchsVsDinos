using ArchsVsDinosClient.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IGameServiceClient : IDisposable
    {
        event Action<GameInitializedDTO> GameInitialized;
        event Action<GameStartedDTO> GameStarted;
        event Action<TurnChangedDTO> TurnChanged;
        event Action<CardDrawnDTO> CardDrawn;
        event Action<DinoPlayedDTO> DinoHeadPlayed;
        event Action<BodyPartAttachedDTO> BodyPartAttached;
        event Action<ArchAddedToBoardDTO> ArchAddedToBoard;
        event Action<ArchArmyProvokedDTO> ArchArmyProvoked;
        event Action<BattleResultDTO> BattleResolved;
        event Action<GameEndedDTO> GameEnded;
        event Action<string, string> ConnectionError;

        event Action<PlayerExpelledDTO> PlayerExpelled;

        Task<GameSetupResultCode> InitializeGameAsync(int matchId);
        Task<GameSetupResultCode> StartGameAsync(int matchId);
        Task<DrawCardResultCode> DrawCardAsync(int matchId, int userId, int drawPileNumber);
        Task<PlayCardResultCode> PlayDinoHeadAsync(int matchId, int userId, int cardId);
        Task<PlayCardResultCode> AttachBodyPartToDinoAsync(int matchId, int userId, int cardId, int dinoHeadCardId);
        Task<ProvokeResultCode> ProvokeArchArmyAsync(int matchId, int userId, string armyType);
        Task<EndTurnResultCode> EndTurnAsync(int matchId, int userId);
        Task<GameStateDTO> GetGameStateAsync(int matchId);
        Task<PlayerHandDTO> GetPlayerHandAsync(int matchId, int userId);
        Task<CentralBoardDTO> GetCentralBoardAsync(int matchId);
    }
}
