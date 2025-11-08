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
    public class FriendRequestViewModel
    {
        private readonly IFriendRequestServiceClient friendRequestService;
        private readonly IMessageService messageService;
        private readonly string currentUsername;

        public string[] PendingRequests { get; set; }

        public event EventHandler RequestsLoaded;
        public event EventHandler RequestSent;
        public event EventHandler RequestAccepted;
        public event EventHandler RequestRejected;
        public event EventHandler<string> NewRequestReceived;

        public FriendRequestViewModel(string username)
        {
            currentUsername = username;
            friendRequestService = new FriendRequestServiceClient();
            messageService = new MessageService();
            PendingRequests = new string[0];

            SubscribeToCallbacks();
        }

        private void SubscribeToCallbacks()
        {
            friendRequestService.FriendRequestSent += OnFriendRequestSent;
            friendRequestService.FriendRequestAccepted += OnFriendRequestAccepted;
            friendRequestService.FriendRequestRejected += OnFriendRequestRejected;
            friendRequestService.PendingRequestsReceived += OnPendingRequestsReceived;
            friendRequestService.FriendRequestReceived += OnFriendRequestReceived;
        }

        public void Subscribe(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                return;
            }

            try
            {
                friendRequestService.Subscribe(username);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        public void Unsubscribe(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                return;
            }

            try
            {
                friendRequestService.Unsubscribe(username);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            // ❌ VALIDACIÓN ELIMINADA - Ya está en el code-behind

            try
            {
                friendRequestService.SendFriendRequest(fromUser, toUser);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                friendRequestService.AcceptFriendRequest(fromUser, toUser);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                friendRequestService.RejectFriendRequest(fromUser, toUser);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        public void LoadPendingRequests(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                friendRequestService.GetPendingRequests(username);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException)
            {
                // TODO: Logger cliente
            }
            catch (Exception)
            {
            }
        }

        private void OnFriendRequestSent(bool success)
        {
            if (success)
            {
                messageService.ShowMessage(Lang.FriendRequest_SentSuccess);
                RequestSent?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnFriendRequestAccepted(bool success)
        {
            if (success)
            {
                messageService.ShowMessage(Lang.FriendRequest_AcceptedSuccess);
                RequestAccepted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                messageService.ShowMessage(Lang.FriendRequest_AcceptedError);
            }
        }

        private void OnFriendRequestRejected(bool success)
        {
            if (success)
            {
                messageService.ShowMessage(Lang.FriendRequest_RejectedSuccess);
                RequestRejected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                messageService.ShowMessage(Lang.FriendRequest_RejectedError);
            }
        }

        private void OnPendingRequestsReceived(string[] requests)
        {
            PendingRequests = requests;
            RequestsLoaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnFriendRequestReceived(string fromUser)
        {
            if (string.Equals(fromUser, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            messageService.ShowMessage(string.Format(Lang.FriendRequest_NewRequestFrom, fromUser));
            NewRequestReceived?.Invoke(this, fromUser);
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

        public void Dispose()
        {
            if (friendRequestService != null)
            {
                friendRequestService.FriendRequestSent -= OnFriendRequestSent;
                friendRequestService.FriendRequestAccepted -= OnFriendRequestAccepted;
                friendRequestService.FriendRequestRejected -= OnFriendRequestRejected;
                friendRequestService.PendingRequestsReceived -= OnPendingRequestsReceived;
                friendRequestService.FriendRequestReceived -= OnFriendRequestReceived;
                friendRequestService.Dispose();
            }
        }
    }
}
