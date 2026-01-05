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
        private static MediaPlayer destroyingRockPlayer = new MediaPlayer();
        private static MediaPlayer movingRockPlayer = new MediaPlayer();

        private static double currentVolume = 0.5;

        public static void SetVolume(double volume)
        {
            currentVolume = volume;
        }

        private static void PlaySound(MediaPlayer player, string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                player.Open(new Uri(fullPath, UriKind.Absolute));
                player.Volume = GlobalSettings.SoundVolume;
                player.Stop(); 
                player.Play();
            }
            catch (Exception e)
            {
                MessageBox.Show(Lang.GlobalSoundNotFound + " " + e.Message);
            }
        }

        public static void PlayDestroyingRockSound()
            => PlaySound(destroyingRockPlayer, "Resources/Sounds/rockSound.wav");

        public static void PlayMovingRockSound()
            => PlaySound(movingRockPlayer, "Resources/Sounds/movingRock.wav");
    }

}
