using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Default
{
    public class InitialConfig
    {
        public static Configuration InitialConfiguration => new Configuration
        {
            musicVolume = 50;
            soundVolume = 50;
        };

        public static Player InitialPlayer => new Player
        {
            facebook = "";
            instagram = "";
            x = "";
            totalWins = 0;
            totalLosses 0;
            totalPoints = 0;
        };
    }
}
