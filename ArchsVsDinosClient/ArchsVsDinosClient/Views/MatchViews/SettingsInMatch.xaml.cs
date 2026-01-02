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

    }

}
