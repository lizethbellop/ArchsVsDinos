using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;
using System.ServiceModel.Channels;
using ArchsVsDinosClient.Properties.Langs;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    public class SoundButton
    {

        private static readonly SoundPlayer destroyingRockSound = new SoundPlayer("Resources/Sounds/rockSound.wav");
        private static readonly SoundPlayer movingRockSound = new SoundPlayer("Resources/Sounds/movingRock.wav");

        private static void PlaySound(SoundPlayer sound)
        {

            try
            {
                sound.Stop();
                sound.Play();
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(Lang.GlobalSoundNotFound + " " + e.Message);
            }
        }

        public static void PlayDestroyingRockSound()
        {
            PlaySound(destroyingRockSound);
        }

        public static void PlayMovingRockSound()
        {
            PlaySound(movingRockSound);
        }

    }

}
