using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class GameServiceClient : IGameServiceClient
    {
        private readonly GameManagerClient client;
        private readonly GameCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;

        public event Action<GameInitializedDTO> GameInitialized;
        public event Action<GameStartedDTO> GameStarted;
        public event Action<TurnChangedDTO> TurnChanged;
        public event Action<CardDrawnDTO> CardDrawn;
        public event Action<DinoPlayedDTO> DinoHeadPlayed;
        public event Action<BodyPartAttachedDTO> BodyPartAttached;
        public event Action<ArchAddedToBoardDTO> ArchAdded;
        public event Action<ArchArmyProvokedDTO> ArchProvoked;
        public event Action<BattleResultDTO> BattleResolved;
        public event Action<GameEndedDTO> GameEnded;
        public event Action<PlayerExpelledDTO> PlayerExpelled;
        public event Action<CardTakenFromDiscardDTO> CardTakenFromDiscard;
        public event Action<string, string> ServiceError;

        public GameServiceClient()
        {
            callback = new GameCallbackHandler();

            callback.OnGameInitializedEvent += (d) => GameInitialized?.Invoke(d);
            callback.OnGameStartedEvent += (d) => GameStarted?.Invoke(d);
            callback.OnTurnChangedEvent += (d) => TurnChanged?.Invoke(d);
            callback.OnCardDrawnEvent += (d) => CardDrawn?.Invoke(d);
            callback.OnDinoPlayedEvent += (d) => DinoHeadPlayed?.Invoke(d);
            callback.OnBodyPartAttachedEvent += (d) => BodyPartAttached?.Invoke(d);
            callback.OnArchAddedEvent += (d) => ArchAdded?.Invoke(d);
            callback.OnArchProvokedEvent += (d) => ArchProvoked?.Invoke(d);
            callback.OnBattleResolvedEvent += (d) => BattleResolved?.Invoke(d);
            callback.OnGameEndedEvent += (d) => GameEnded?.Invoke(d);
            callback.OnPlayerExpelledEvent += (d) => PlayerExpelled?.Invoke(d);
            callback.OnCardTakenFromDiscardEvent += (d) => CardTakenFromDiscard?.Invoke(d);

            var context = new InstanceContext(callback);

            context.SynchronizationContext = null;

            client = new GameManagerClient(context);

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ServiceError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task ConnectToGameAsync(string matchCode, int userId)
        {
            await ExecuteAsync(() => client.ConnectToGameAsync(matchCode, userId));
        }

        public async Task LeaveGameAsync(string matchCode, int userId)
        {
            await ExecuteAsync(() => client.LeaveGameAsync(matchCode, userId));
        }

        public async Task InitializeGameAsync(string matchCode) => await Task.CompletedTask;
        public async Task StartGameAsync(string matchCode) => await Task.CompletedTask;

        public async Task<DrawCardResultCode> DrawCardAsync(string matchCode, int userId)
        {
            return await ExecuteAsyncWithResult(
                () => client.DrawCardAsync(matchCode, userId),
                DrawCardResultCode.DrawCard_UnexpectedError
            );
        }

        public async Task<PlayCardResultCode> PlayDinoHeadAsync(string matchCode, int userId, int cardId)
        {
            return await ExecuteAsyncWithResult(
                () => client.PlayDinoHeadAsync(matchCode, userId, cardId),
                PlayCardResultCode.PlayCard_UnexpectedError
            );
        }

        public async Task<PlayCardResultCode> AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            return await ExecuteAsyncWithResult(
                () => client.AttachBodyPartToDinoAsync(matchCode, userId, attachmentData),
                PlayCardResultCode.PlayCard_UnexpectedError
            );
        }

        public async Task<ProvokeResultCode> ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType)
        {
            return await ExecuteAsyncWithResult(
                () => client.ProvokeArchArmyAsync(matchCode, userId, armyType),
                ProvokeResultCode.Provoke_UnexpectedError
            );
        }

        public async Task<EndTurnResultCode> EndTurnAsync(string matchCode, int userId)
        {
            return await ExecuteAsyncWithResult(
                () => client.EndTurnAsync(matchCode, userId),
                EndTurnResultCode.EndTurn_UnexpectedError
            );
        }

        public async Task<DrawCardResultCode> TakeCardFromDiscardPileAsync(string matchCode, int userId, int cardId)
        {
            return await ExecuteAsyncWithResult(
                () => client.TakeCardFromDiscardPileAsync(matchCode, userId, cardId),
                DrawCardResultCode.DrawCard_UnexpectedError
            );
        }

        private async Task ExecuteAsync(Func<Task> action)
        {
            try
            {
                await guardian.ExecuteAsync(async () => await action());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Service Error: {ex.Message}");
            }
        }

        private async Task<T> ExecuteAsyncWithResult<T>(Func<Task<T>> action, T defaultErrorValue)
        {
            return await guardian.ExecuteAsync(async () =>
            {
                return await action();
            },
            defaultValue: defaultErrorValue,
            operationName: "GameAction");
        }
    }
}