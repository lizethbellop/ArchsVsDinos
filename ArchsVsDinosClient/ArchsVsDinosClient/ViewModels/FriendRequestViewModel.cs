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
            friendRequestService.ConnectionError += OnConnectionError;
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

            friendRequestService.Subscribe(username);
        }

        public void Unsubscribe(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                return;
            }

            friendRequestService.Unsubscribe(username);
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            friendRequestService.SendFriendRequest(fromUser, toUser);
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            friendRequestService.AcceptFriendRequest(fromUser, toUser);
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            if (!ValidateInputs(fromUser, toUser))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            friendRequestService.RejectFriendRequest(fromUser, toUser);
        }

        public void LoadPendingRequests(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            friendRequestService.GetPendingRequests(username);
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
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
                friendRequestService.ConnectionError -= OnConnectionError; // 🆕 AGREGAR
                friendRequestService.Dispose();
            }
        }
    }
}
