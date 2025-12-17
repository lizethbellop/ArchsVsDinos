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
        Task DrawCardAsync(string matchCode, int userId, int drawPileNumber);
        Task PlayDinoHeadAsync(string matchCode, int userId, int cardId);
        Task AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData);
        Task SwapCardWithPlayerAsync(string matchCode, int initiatorUserId, ExchangeCardDTO request);
        Task ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType);
        Task EndTurnAsync(string matchCode, int userId);

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
        event Action<CardExchangedDTO> CardExchanged;

        event Action<string, string> ServiceError;
    }
}