using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ArchsVsDinosClient.Utils
{
    public class MusicPlayer
    {
        private static MusicPlayer instance;

        private MediaPlayer musicPlayer = new MediaPlayer();

        public static MusicPlayer Instance
        {
            get
            {
                if (instance == null)
                    instance = new MusicPlayer();

                return instance;
            }
        }

        private MusicPlayer()
        {
            musicPlayer.Volume = 0.6;
            musicPlayer.MediaEnded += OnMusicFinishedRestart;
        }

        private void OnMusicFinishedRestart(object sender, EventArgs e)
        {
            musicPlayer.Position = TimeSpan.Zero; 
            musicPlayer.Play();                   
        }

        public void PlayBackgroundMusic(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                relativePath
            );

            musicPlayer.Open(new Uri(fullPath, UriKind.Absolute));
            musicPlayer.Play();
        }

        public void StopBackgroundMusic()
        {
            musicPlayer.Stop();
        }

        public void SetBackgroundMusicVolume(double volume)
        {
            musicPlayer.Volume = volume;
        }
    }
}
