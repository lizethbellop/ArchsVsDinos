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
        private readonly IFriendServiceClient friendService;
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

                if (response.Success)
                {
                    Friends = response.Friends;
                    FriendsLoaded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    string message = FriendResultCodeHelper.GetMessage(response.ResultCode);
                    messageService.ShowMessage(message);
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
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
                string message = FriendResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);

                if (response.Success)
                {
                    await LoadFriendsAsync(username);
                    FriendRemoved?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
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

                if (response.Success)
                {
                    return response.AreFriends;
                }

                string message = FriendResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);
                return false;
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
                return false;
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
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
