using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.GameService;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Game_DTO.Swap;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class GameManager : IGameManager
    {
        private readonly GameLogic gameLogic;
        private readonly IGameManagerCallback callback;
        private readonly GameNotifier notifier;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();
            logger = new LoggerHelperWrapper();
            var sessions = new GameSessionManager(logger);
            var setupHandler = new GameSetupHandler();
            var core = new GameCoreContext(sessions, setupHandler);
            notifier = new GameNotifier(logger);
            var statistics = new StatisticsManager();

            gameLogic = new GameLogic(core, logger, notifier, statistics);
        }

        public void AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            ExecuteSafe(() =>
            {
                gameLogic.AttachBodyPart(matchCode, userId, attachmentData);
            });
        }


        public void ConnectToGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
                throw new ArgumentException(nameof(matchCode));

            var callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();

            GameCallbackRegistry.Instance.RegisterCallback(userId, callback);

            logger.LogInfo($"User {userId} connected to match {matchCode}");
        }


        public void DrawCard(string matchCode, int userId, int drawPileNumber)
        {
            ExecuteSafe(() =>
            {
                gameLogic.DrawCard(matchCode, userId, drawPileNumber);
            });
        }



        public void EndTurn(string matchCode, int userId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.EndTurn(matchCode, userId);
            });
        }


        public void LeaveGame(string matchCode, int userId)
        {
            try
            {
                gameLogic.LeaveGame(matchCode, userId);
            }
            finally
            {
                GameCallbackRegistry.Instance.UnregisterCallback(userId);
                logger.LogInfo($"User {userId} left match {matchCode}");
            }
        }


        public void PlayDinoHead(string matchCode, int userId, int cardId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.PlayDinoHead(matchCode, userId, cardId);
            });
        }


        public void ProvokeArchArmy(string matchCode, int userId, ArmyType armyType)
        {
            ExecuteSafe(() =>
            {
                gameLogic.Provoke(matchCode, userId, armyType);
            });
        }


        public void SwapCardWithPlayer(string matchCode, int initiatorUserId, ExchangeCardDTO request)
        {
            ExecuteSafe(() =>
            {
                gameLogic.ExchangeCard(matchCode, initiatorUserId, request);
            });
        }


        private void ExecuteSafe(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                throw CreateFault(GameFaultCodes.InvalidParameter, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw CreateFault(GameFaultCodes.InvalidCard, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error", ex);
                throw CreateFault(GameFaultCodes.InternalError, "Unexpected server error");
            }
        }

        private FaultException<GameFault> CreateFault(string code, string detail)
        {
            return new FaultException<GameFault>(
                new GameFault
                {
                    Code = code,
                    Detail = detail
                },
                new FaultReason(code)
            );
        }

    }
}
