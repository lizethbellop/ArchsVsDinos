using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views.MatchViews.MatchSeeDeck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
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

    public partial class MainMatch : Window
    {
        private readonly ChatViewModel chatViewModel;
        private readonly string currentUsername;

        public MainMatch(string username)
        {
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be null or empty", nameof(username));
            }

            currentUsername = username;

            try
            {
                chatViewModel = new ChatViewModel(new ChatServiceClient());
                Gr_Chat.DataContext = chatViewModel;
                Loaded += Match_Loaded;
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show("Chat service is not available. The game will continue without chat functionality.",
                    "Chat Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (CommunicationException)
            {
                MessageBox.Show("Failed to initialize chat service. The game will continue without chat functionality.",
                    "Chat Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Match_Loaded(object sender, RoutedEventArgs e)
        {
            if (chatViewModel == null)
            {
                return;
            }

            try
            {
                await chatViewModel.ConnectAsync(currentUsername).ConfigureAwait(true);
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show("Chat server is not reachable.", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Connection to chat server timed out.", "Timeout Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (CommunicationException)
            {
                MessageBox.Show("Failed to connect to chat server.", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Chat service is in an invalid state.", "Service Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void Click_BtnSeeDeckP1(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP2(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP3(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP4(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Click_BtnChat(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Visible;
            Btn_Chat.Visibility = Visibility.Collapsed;
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Collapsed;
            Btn_Chat.Visibility = Visibility.Visible;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (chatViewModel == null)
            {
                base.OnClosing(e);
                return;
            }

            try
            {
                await chatViewModel.DisconnectAsync().ConfigureAwait(true);
            }
            catch (TimeoutException)
            {
            }
            catch (CommunicationException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                try
                {
                    chatViewModel.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            base.OnClosing(e);
        }
    }
}
