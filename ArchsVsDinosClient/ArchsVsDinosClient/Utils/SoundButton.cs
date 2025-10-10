using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;

namespace ArchsVsDinosClient.Utils
{
    public static class SoundButton
    {
        private static string soundPatch;

        static SoundButton()
        {
            soundPatch = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds", "rockSound.wav");
        }
        public static void playButtonSound()
        {
            SoundPlayer player = new SoundPlayer(soundPatch);
            player.Play();
        }
    }
}
