using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class GameServiceClient : IGameServiceClient
    {
        private readonly GameManagerClient client;
        private readonly InstanceContext context;
        private readonly GameCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;
        private readonly SynchronizationContext syncContext;
        private bool isDisposed;

        public event Action<GameInitializedDTO> GameInitialized;
        public event Action<GameStartedDTO> GameStarted;
        public event Action<TurnChangedDTO> TurnChanged;
        public event Action<CardDrawnDTO> CardDrawn;
        public event Action<DinoPlayedDTO> DinoHeadPlayed;
        public event Action<BodyPartAttachedDTO> BodyPartAttached;
        public event Action<ArchAddedToBoardDTO> ArchAddedToBoard;
        public event Action<ArchArmyProvokedDTO> ArchArmyProvoked;
        public event Action<BattleResultDTO> BattleResolved;
        public event Action<GameEndedDTO> GameEnded;
        public event Action<string, string> ConnectionError;
        public event Action<PlayerExpelledDTO> PlayerExpelled;

        public GameServiceClient()
        {
            syncContext = SynchronizationContext.Current;
            callback = new GameCallbackHandler();

            callback.GameInitialized += OnGameInitialized;
            callback.GameStarted += OnGameStarted;
            callback.TurnChanged += OnTurnChanged;
            callback.CardDrawn += OnCardDrawn;
            callback.DinoHeadPlayed += OnDinoHeadPlayed;
            callback.BodyPartAttached += OnBodyPartAttached;
            callback.ArchAddedToBoard += OnArchAddedToBoard;
            callback.ArchArmyProvoked += OnArchArmyProvoked;
            callback.BattleResolved += OnBattleResolved;
            callback.GameEnded += OnGameEnded;
            callback.PlayerExpelled += OnPlayerExpelled;

            context = new InstanceContext(callback);
            context.SynchronizationContext = syncContext;
            client = new GameManagerClient(context);

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task<GameSetupResultCode> InitializeGameAsync(int matchId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.InitializeGame(matchId))
            );
        }

        public async Task<GameSetupResultCode> StartGameAsync(int matchId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.StartGame(matchId))

            );
        }

        public async Task<DrawCardResultCode> DrawCardAsync(int matchId, int userId, int drawPileNumber)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.DrawCard(matchId, userId, drawPileNumber))
            );
        }

        public async Task<PlayCardResultCode> PlayDinoHeadAsync(int matchId, int userId, int cardId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.PlayDinoHead(matchId, userId, cardId))
            );
        }

        public async Task<PlayCardResultCode> AttachBodyPartToDinoAsync(int matchId, int userId, int cardId, int dinoHeadCardId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.AttachBodyPartToDino(matchId, userId, cardId, dinoHeadCardId))
            );
        }

        public async Task<ProvokeResultCode> ProvokeArchArmyAsync(int matchId, int userId, string armyType)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.ProvokeArchArmy(matchId, userId, armyType))
            );
        }

        public async Task<EndTurnResultCode> EndTurnAsync(int matchId, int userId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.EndTurn(matchId, userId))
            );
        }

        public async Task<GameStateDTO> GetGameStateAsync(int matchId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetGameState(matchId))
            );
        }

        public async Task<PlayerHandDTO> GetPlayerHandAsync(int matchId, int userId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetPlayerHand(matchId, userId))
            );
        }

        public async Task<CentralBoardDTO> GetCentralBoardAsync(int matchId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetCentralBoard(matchId))
            );
        }

        private void OnGameInitialized(GameInitializedDTO data)
        {
            GameInitialized?.Invoke(data);
        }

        private void OnGameStarted(GameStartedDTO data)
        {
            GameStarted?.Invoke(data);
        }

        private void OnTurnChanged(TurnChangedDTO data)
        {
            TurnChanged?.Invoke(data);
        }

        private void OnCardDrawn(CardDrawnDTO data)
        {
            CardDrawn?.Invoke(data);
        }

        private void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            DinoHeadPlayed?.Invoke(data);
        }

        private void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            BodyPartAttached?.Invoke(data);
        }

        private void OnArchAddedToBoard(ArchAddedToBoardDTO data)
        {
            ArchAddedToBoard?.Invoke(data);
        }

        private void OnArchArmyProvoked(ArchArmyProvokedDTO data)
        {
            ArchArmyProvoked?.Invoke(data);
        }

        private void OnBattleResolved(BattleResultDTO data)
        {
            BattleResolved?.Invoke(data);
        }

        private void OnGameEnded(GameEndedDTO data)
        {
            GameEnded?.Invoke(data);
        }

        private void OnPlayerExpelled(PlayerExpelledDTO data)
        {
            PlayerExpelled?.Invoke(data);
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (callback != null)
            {
                callback.GameInitialized -= OnGameInitialized;
                callback.GameStarted -= OnGameStarted;
                callback.TurnChanged -= OnTurnChanged;
                callback.CardDrawn -= OnCardDrawn;
                callback.DinoHeadPlayed -= OnDinoHeadPlayed;
                callback.BodyPartAttached -= OnBodyPartAttached;
                callback.ArchAddedToBoard -= OnArchAddedToBoard;
                callback.ArchArmyProvoked -= OnArchArmyProvoked;
                callback.BattleResolved -= OnBattleResolved;
                callback.GameEnded -= OnGameEnded;
                callback.PlayerExpelled -= OnPlayerExpelled;
            }

            try
            {
                if (client?.State == CommunicationState.Opened)
                    client.Close();
                else if (client?.State == CommunicationState.Faulted)
                    client.Abort();
            }
            catch
            {
                client?.Abort();
            }

            isDisposed = true;
        }
    }
}
