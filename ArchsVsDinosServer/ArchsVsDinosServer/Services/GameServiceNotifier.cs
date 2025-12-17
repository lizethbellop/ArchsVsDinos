using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class GameServiceNotifier : IGameServiceNotifier
    {
        private readonly IGameLogic gameLogic;

        public GameServiceNotifier(IGameLogic gameLogic)
        {
            this.gameLogic = gameLogic;
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
