using ArchsVsDinosClient.GameService;
using System;
using ArchsVsDinosClient.DTO;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IGameServiceClient
    {
        Task ConnectToGameAsync(string matchCode, int userId);
        Task InitializeGameAsync(string matchCode);
        Task StartGameAsync(string matchCode);
        Task LeaveGameAsync(string matchCode, int userId);

        Task<DrawCardResultCode> DrawCardAsync(string matchCode, int userId);

        Task<PlayCardResultCode> PlayDinoHeadAsync(string matchCode, int userId, int cardId);

        Task<PlayCardResultCode> AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData);

        Task<ProvokeResultCode> ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType);

        Task<EndTurnResultCode> EndTurnAsync(string matchCode, int userId);

        Task<DrawCardResultCode> TakeCardFromDiscardPileAsync(string matchCode, int userId, int cardId);

        event Action<GameInitializedDTO> GameInitialized;
        event Action<GameStartedDTO> GameStarted;
        event Action<TurnChangedDTO> TurnChanged;
        event Action<CardDrawnDTO> CardDrawn;
        event Action<DinoPlayedDTO> DinoHeadPlayed;
        event Action<BodyPartAttachedDTO> BodyPartAttached;
        event Action<ArchAddedToBoardDTO> ArchAdded;
        event Action<ArchArmyProvokedDTO> ArchProvoked;
        event Action<BattleResultDTO> BattleResolved;
        event Action<GameEndedDTO> GameEnded;
        event Action<PlayerExpelledDTO> PlayerExpelled;
        event Action<CardTakenFromDiscardDTO> CardTakenFromDiscard;

        event Action<string, string> ServiceError;
    }
}