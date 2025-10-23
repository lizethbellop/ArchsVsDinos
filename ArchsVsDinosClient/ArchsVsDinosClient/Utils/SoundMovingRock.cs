using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    public class SoundMovingRock
    {
        private static readonly SoundPlayer _player = new SoundPlayer("Resources/Sounds/movingRock.wav");

        public static void PlayClick()
        {
            try
            {
                _player.Stop();
                _player.Play();
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(Lang.GlobalSoundNotFound + " " + e.Message);
            }
        }
    }
}
