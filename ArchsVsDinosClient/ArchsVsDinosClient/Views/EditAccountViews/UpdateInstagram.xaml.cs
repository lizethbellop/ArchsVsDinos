﻿using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
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

namespace ArchsVsDinosClient.Views.EditAccountViews
{
    /// <summary>
    /// Lógica de interacción para UpdateInstagram.xaml
    /// </summary>
    public partial class UpdateInstagram : Window
    {
        public UpdateInstagram()
        {
            InitializeComponent();
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Click_BtnSave(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            string currentUsername = UserSession.Instance.currentUser.username;
            string newInstagramLink = TxtB_InstagramLink.Text;

            if (!ValidateInputs(newInstagramLink))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.UpdateInstagram(currentUsername, newInstagramLink);

                string message = UpdateResultCodeHelper.GetMessage(response.resultCode);
                MessageBox.Show(message);

                if (response.success)
                {
                    this.Close();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión con el servidor");
            }
        }

        private bool ValidateInputs(string instagramLink)
        {
            if (ValidationHelper.IsEmpty(instagramLink) || ValidationHelper.IsWhiteSpace(instagramLink))
            {
                return false;
            }

            return true;
        }
    }
}
