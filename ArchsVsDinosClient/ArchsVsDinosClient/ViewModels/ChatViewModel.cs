using ArchsVsDinosClient.ChatManager;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArchsVsDinosClient.Commands;

namespace ArchsVsDinosClient.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IChatServiceClient chatService;
        private string currentUsername;
        private string messageInput;
        private bool isConnected;
        private bool isBusy;
        private bool isDisposed;

        public ObservableCollection<string> Messages { get; }
        public ObservableCollection<string> OnlineUsers { get; }

        // ✅ NUEVO: Evento para notificar cierre de ventana
        public event Action<string, string> RequestWindowClose;

        public string MessageInput
        {
            get => messageInput;
            set
            {
                if (messageInput != value)
                {
                    messageInput = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SendMessageCommand { get; }

        public ChatViewModel(IChatServiceClient chatService)
        {
            this.chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

            Messages = new ObservableCollection<string>();
            OnlineUsers = new ObservableCollection<string>();

            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);

            chatService.MessageReceived += OnMessageReceived;
            chatService.SystemNotificationReceived += OnSystemNotificationReceived;
            chatService.UserListUpdated += OnUserListUpdated;
            chatService.ConnectionError += OnConnectionError;

            // ✅ Suscribirse a eventos críticos
            chatService.UserBanned += OnUserBanned;
            chatService.UserExpelled += OnUserExpelled;
            chatService.LobbyClosed += OnLobbyClosed;
        }

        public ChatViewModel() : this(new ChatServiceClient()) { }

        // ------------------------------------------------------
        //  CONNECTION
        // ------------------------------------------------------

        // ✅ NUEVO: Ahora acepta context y matchCode
        public async Task ConnectAsync(string username, int context = 0, string matchCode = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                AddSystemMessage("Username cannot be empty");
                return;
            }

            IsBusy = true;
            currentUsername = username;

            await chatService.ConnectAsync(username, context, matchCode);
            IsConnected = true;
            IsBusy = false;
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected) return;

            IsBusy = true;
            await chatService.DisconnectAsync(currentUsername);
            IsConnected = false;
            IsBusy = false;
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageInput) || !IsConnected)
                return;

            string messageToSend = MessageInput;
            MessageInput = string.Empty;

            await chatService.SendMessageAsync(messageToSend, currentUsername);
        }

        private bool CanSendMessage()
        {
            return IsConnected && !string.IsNullOrWhiteSpace(MessageInput) && !IsBusy;
        }

        // ✅ NUEVO: Manejar cuando el usuario actual es baneado
        private void OnUserBanned(string username, int strikes)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (username == currentUsername)
                {
                    AddSystemMessage($"⚠️ Has sido expulsado del chat ({strikes} strikes)");
                    IsConnected = false;

                    // Notificar cierre después de mostrar mensaje
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            RequestWindowClose?.Invoke(
                                "Expulsado del chat",
                                "Has sido expulsado por comportamiento inapropiado."
                            );
                        });
                    });
                }
                else
                {
                    AddSystemMessage($"⚠️ {username} fue expulsado del chat");
                }
            });
        }

        // ✅ NUEVO: Manejar expulsión de otros jugadores
        private void OnUserExpelled(string username, string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddSystemMessage($"⚠️ {username} fue expulsado: {reason}");
            });
        }

        // ✅ NUEVO: Manejar cierre de lobby/game
        private void OnLobbyClosed(string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddSystemMessage($"⚠️ {reason}");
                IsConnected = false;

                // Notificar cierre
                Task.Delay(2000).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RequestWindowClose?.Invoke(
                            "Lobby cerrado",
                            reason
                        );
                    });
                });
            });
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = false;
                AddSystemMessage($"❌ {title}: {message}");

                // Notificar error crítico
                RequestWindowClose?.Invoke(title, message);
            });
        }

        private void OnMessageReceived(string roomId, string fromUser, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add($"[{fromUser}]: {message}");
            });
        }

        private void OnSystemNotificationReceived(ChatResultCode code, string notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddSystemMessage($"[{code}] {notification}");
            });
        }

        private void OnUserListUpdated(List<string> users)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnlineUsers.Clear();

                if (users != null)
                {
                    foreach (var user in users)
                    {
                        OnlineUsers.Add(user);
                    }
                }
            });
        }

        private void AddSystemMessage(string message)
        {
            Messages.Add($"[SYSTEM]: {message}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                chatService.MessageReceived -= OnMessageReceived;
                chatService.SystemNotificationReceived -= OnSystemNotificationReceived;
                chatService.UserListUpdated -= OnUserListUpdated;
                chatService.ConnectionError -= OnConnectionError;

                chatService.UserBanned -= OnUserBanned;
                chatService.UserExpelled -= OnUserExpelled;
                chatService.LobbyClosed -= OnLobbyClosed;

                chatService.Dispose();
            }

            isDisposed = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
