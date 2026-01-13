using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using Contracts;
using System;

namespace ArchsVsDinosServer.Utils
{
    public sealed class GameLogicDependencies
    {
        public GameCoreContext CoreContext { get; }
        public ILoggerHelper Logger { get; }
        public IGameNotifier Notifier { get; }
        public IStatisticsManager StatisticsManager { get; }

        public GameLogicDependencies(
            GameCoreContext coreContext,
            ILoggerHelper logger,
            IGameNotifier notifier,
            IStatisticsManager statisticsManager)
        {
            CoreContext = coreContext ?? throw new ArgumentNullException(nameof(coreContext));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            StatisticsManager = statisticsManager ?? throw new ArgumentNullException(nameof(statisticsManager));
        }
    }
}