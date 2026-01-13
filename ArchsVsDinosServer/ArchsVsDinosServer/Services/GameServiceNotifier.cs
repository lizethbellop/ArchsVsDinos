
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Services.Interfaces;
using Contracts.DTO.Game_DTO.Enums;
using System;

namespace ArchsVsDinosServer.Services
{
    public sealed class GameServiceNotifier : IGameServiceNotifier
    {
        private readonly IGameLogic gameLogic;

        public GameServiceNotifier(IGameLogic gameLogic)
        {
            this.gameLogic = gameLogic ?? throw new ArgumentNullException(nameof(gameLogic));
        }

        public void NotifyPlayerExpelled(string matchCode, int userId, string reason)
        {
            gameLogic.LeaveGame(matchCode, userId);
        }

        public void NotifyGameClosure(string matchCode, GameEndType gameType, string reason)
        {
            gameLogic.EndGame(matchCode, gameType, reason);
        }
    }
}