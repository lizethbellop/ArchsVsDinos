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

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class SettingsInMatch : Window
    {

        public bool RequestLeaveGame { get; private set; } = false;

        public SettingsInMatch()
        {
            InitializeComponent();
            Sl_VolumeMusic.Value = GlobalSettings.MusicVolume * 100;
            Sl_VolumeSound.Value = GlobalSettings.SoundVolume * 100;
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            RequestLeaveGame = false;
            this.Close();
        }

        private void Click_BtnLeaveTheGame(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            var result = MessageBox.Show(
                Properties.Langs.Lang.Match_ConfirmLeaveMessage,
                Properties.Langs.Lang.Match_ConfirmLeaveTitle,
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                RequestLeaveGame = true;
                this.DialogResult = true;
                this.Close();
            }
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
