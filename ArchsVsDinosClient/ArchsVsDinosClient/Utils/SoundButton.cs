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
    internal class SoundButton
    {

        public void playButtonSound()
        {
            SoundPlayer rockSound = new SoundPlayer("ArchsVsDinosClient\\Resources\\Sounds\\rockSound.wav");
            rockSound.Play(); 
        }
    }
}
