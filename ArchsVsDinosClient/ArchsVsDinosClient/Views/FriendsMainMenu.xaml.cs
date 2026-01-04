using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
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
    public partial class FriendsMainMenu : Window
    {
        private readonly FriendsViewModel friendsViewModel;
        private readonly FriendRequestViewModel friendRequestViewModel;
        private string currentUsername;

        public FriendsMainMenu(string username)
        {
            InitializeComponent();

            currentUsername = username;
            friendsViewModel = new FriendsViewModel();
            friendRequestViewModel = new FriendRequestViewModel(username);

            SubscribeToEvents();
            InitializeData();
        }

        private void SubscribeToEvents()
        {
            friendsViewModel.FriendsLoaded += OnFriendsLoaded;

            friendRequestViewModel.RequestsLoaded += OnRequestsLoaded;
            friendRequestViewModel.RequestSent += OnRequestSent;
            friendRequestViewModel.RequestAccepted += OnRequestAccepted;
            friendRequestViewModel.RequestRejected += OnRequestRejected;
            friendRequestViewModel.NewRequestReceived += OnNewRequestReceived;

            BtnSearchFriend.Click += Click_BtnSearchFriend;
            BtnSendRequest.Click += Click_BtnSendRequest;
        }

        private void InitializeData()
        {
            friendRequestViewModel.SubscribeAsync(currentUsername);
            friendsViewModel.LoadFriendsAsync(currentUsername);
            friendRequestViewModel.LoadPendingRequestsAsync(currentUsername);
        }

        private void OnFriendsLoaded(object sender, EventArgs e)
        {
            LbFriends.ItemsSource = null;
            LbFriends.ItemsSource = friendsViewModel.Friends;
        }

        private void OnRequestsLoaded(object sender, EventArgs e)
        {
            LbPendingRequests.ItemsSource = null;
            LbPendingRequests.ItemsSource = friendRequestViewModel.PendingRequests;
        }

        private void OnRequestSent(object sender, EventArgs e)
        {
            TxtBSearchUsername.Clear();
            SearchResultPanel.Visibility = Visibility.Collapsed;
        }

        private async void OnRequestAccepted(object sender, EventArgs e)
        {
            await friendsViewModel.LoadFriendsAsync(currentUsername);
            friendRequestViewModel.LoadPendingRequestsAsync(currentUsername);
        }

        private void OnRequestRejected(object sender, EventArgs e)
        {
            friendRequestViewModel.LoadPendingRequestsAsync(currentUsername);
        }

        private void OnNewRequestReceived(object sender, string fromUser)
        {
            friendRequestViewModel.LoadPendingRequestsAsync(currentUsername);
        }

        private async void Click_BtnSearchFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            string searchUsername = TxtBSearchUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchUsername))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            if (searchUsername.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(Lang.Friend_CannotAddYourself);
                SearchResultPanel.Visibility = Visibility.Collapsed;
                return;
            }

            bool areFriends = await friendsViewModel.AreFriendsAsync(currentUsername, searchUsername);

            if (areFriends)
            {
                TxtSearchResult.Text = $"👥 {searchUsername}";
                TxtFriendshipStatus.Text = Lang.Friend_AlreadyFriends;
                BtnSendRequest.IsEnabled = false;
                BtnSendRequest.Opacity = 0.5;
            }
            else
            {
                TxtSearchResult.Text = $"👤 {searchUsername}";
                TxtFriendshipStatus.Text = Lang.Friend_NotFriends;
                BtnSendRequest.IsEnabled = true;
                BtnSendRequest.Opacity = 1.0;
                BtnSendRequest.Tag = searchUsername;
            }

            SearchResultPanel.Visibility = Visibility.Visible;
        }

        private void Click_BtnSendRequest(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            Button button = sender as Button;
            if (button?.Tag == null)
            {
                return;
            }

            string receiverUsername = button.Tag.ToString();
            friendRequestViewModel.SendFriendRequestAsync(currentUsername, receiverUsername);
        }

        private void Click_BtnAcceptRequest(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            string senderUsername = button.Tag as string;
            if (string.IsNullOrWhiteSpace(senderUsername))
            {
                return;
            }

            friendRequestViewModel.AcceptFriendRequestAsync(senderUsername, currentUsername);
        }

        private void Click_BtnRejectRequest(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            string senderUsername = button.Tag as string;
            if (string.IsNullOrWhiteSpace(senderUsername))
            {
                return;
            }

            friendRequestViewModel.RejectFriendRequestAsync(senderUsername, currentUsername);
        }

        private async void Click_BtnRemoveFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            if (sender is Button button && button.Tag is string friendUsername)
            {
                var result = MessageBox.Show(
                    Lang.Friends_RemoveConfirmation,
                    "",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    string currentUsername = UserSession.Instance.CurrentUser.Username;
                    await friendsViewModel.RemoveFriendAsync(currentUsername, friendUsername);
                }
            }
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            UnsubscribeFromEvents();
            friendRequestViewModel.UnsubscribeAsync(currentUsername);
            friendRequestViewModel.Dispose();

            this.Close();
        }

        private void UnsubscribeFromEvents()
        {
            friendsViewModel.FriendsLoaded -= OnFriendsLoaded;

            friendRequestViewModel.RequestsLoaded -= OnRequestsLoaded;
            friendRequestViewModel.RequestSent -= OnRequestSent;
            friendRequestViewModel.RequestAccepted -= OnRequestAccepted;
            friendRequestViewModel.RequestRejected -= OnRequestRejected;
            friendRequestViewModel.NewRequestReceived -= OnNewRequestReceived;
        }
    }
}
