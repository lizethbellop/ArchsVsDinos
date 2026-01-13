using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Wrappers;
using Contracts;

namespace ArchsVsDinosServer.Utils
{
    public static class ServiceContext
    {
        public static readonly ILoggerHelper Logger =
            new LoggerHelperWrapper();

        public static readonly GameSessionManager GameSessions =
            new GameSessionManager(Logger);

        private static readonly GameSetupHandler GameSetup =
            new GameSetupHandler();

        private static readonly GameCoreContext GameCore =
            new GameCoreContext(GameSessions, GameSetup);

        public static readonly IStatisticsManager Statistics =
            new StatisticsManager();

        private static readonly IGameNotifier GameNotifier =
            new GameNotifier(Logger, GameSessions, () => GameLogic);

        public static readonly IGameLogic GameLogic =
            new GameLogic(new GameLogicDependencies(
                coreContext: GameCore,
                logger: Logger,
                notifier: GameNotifier,
                statisticsManager: Statistics
            ));

        private static readonly LobbySession LobbySession =
            LobbySession.Instance;

        private static readonly LobbyCoreContext LobbyCore =
            new LobbyCoreContext(
                LobbySession,
                new LobbyValidationHelper(Logger),
                new LobbyCodeGeneratorHelper()
            );

        public static readonly IInvitationSendHelper InvitationSender =
            new InvitationSendHelper(
                new EmailNotificationSender(),
                Logger
            );

        public static readonly ILobbyLogic LobbyLogic =
            new LobbyLogic(
                LobbyCore,
                Logger,
                GameLogic,
                InvitationSender
            );
    }
}