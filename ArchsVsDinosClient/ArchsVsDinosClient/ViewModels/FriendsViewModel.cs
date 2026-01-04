using ArchsVsDinosClient.FriendService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class FriendsViewModel
    {
        private IFriendServiceClient friendService;
        private readonly IMessageService messageService;

        public string CurrentUsername { get; set; }
        public string[] Friends { get; set; }

        public event EventHandler FriendsLoaded;
        public event EventHandler FriendRemoved;

        public FriendsViewModel()
        {
            friendService = new FriendServiceClient();
            messageService = new MessageService();
            Friends = new string[0];
            friendService.ConnectionError += OnConnectionError;
        }

        private void ResetFriendService()
        {
            if (friendService is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted)
                        comm.Abort();
                    else
                        comm.Close();
                }
                catch
                {
                    comm.Abort();
                }
            }

            friendService = new FriendServiceClient();
            friendService.ConnectionError += OnConnectionError;
        }

        public async Task LoadFriendsAsync(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                var response = await friendService.GetFriendsAsync(username);

                if (response == null || !response.Success)
                {
                    string message = FriendResultCodeHelper.GetMessage(response?.ResultCode ?? FriendResultCode.Friend_UnexpectedError);
                    messageService.ShowMessage(message);
                    return;
                }

                Friends = response.Friends;
                FriendsLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (CommunicationException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
            }
            catch (TimeoutException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerTimeout);
            }
            catch (Exception)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
            }
        }

        public async Task RemoveFriendAsync(string username, string friendUsername)
        {
            if (!ValidateInputs(username, friendUsername))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                var response = await friendService.RemoveFriendAsync(username, friendUsername);

                if (response == null)
                {
                    return;
                }

                string message = FriendResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);

                if (response.Success)
                {
                    await LoadFriendsAsync(username);
                    FriendRemoved?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (CommunicationException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
            }
            catch (TimeoutException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerTimeout);
            }
            catch (Exception)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
            }
        }

        public async Task<bool> AreFriendsAsync(string username, string friendUsername)
        {
            if (!ValidateInputs(username, friendUsername))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return false;
            }

            try
            {
                var response = await friendService.AreFriendsAsync(username, friendUsername);

                if (response == null || !response.Success)
                {
                    if (response != null)
                    {
                        string message = FriendResultCodeHelper.GetMessage(response.ResultCode);
                        messageService.ShowMessage(message);
                    }
                    return false;
                }

                return response.AreFriends;
            }
            catch (CommunicationException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
                return false;
            }
            catch (TimeoutException)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerTimeout);
                return false;
            }
            catch (Exception)
            {
                ResetFriendService();
                messageService.ShowMessage(Lang.GlobalServerUnavailable);
                return false;
            }
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
        }

        private bool ValidateInputs(string username, string friendUsername)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username) ||
                ValidationHelper.IsEmpty(friendUsername) || ValidationHelper.IsWhiteSpace(friendUsername))
            {
                return false;
            }
            return true;
        }
    }
}
