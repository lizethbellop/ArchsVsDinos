using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views
{
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            Sl_VolumeMusic.Value = GlobalSettings.MusicVolume * 100;
            Sl_VolumeSound.Value = GlobalSettings.SoundVolume * 100;
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Sl_VolumeMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded) 
            {
                double vol = e.NewValue / 100;
                GlobalSettings.MusicVolume = vol; 
                MusicPlayer.Instance.SetBackgroundMusicVolume(vol);
            }
        }

        private void Sl_VolumeSound_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                double vol = e.NewValue / 100;
                GlobalSettings.SoundVolume = vol;
                SoundButton.SetVolume(vol);
            }
        }

    }
}
