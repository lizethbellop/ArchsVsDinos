using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    public class SlotLobby : INotifyPropertyChanged
    {

        private string username;
        private string nickname;
        private string profilePicture;
        private bool isFriend;
        private bool canKick;
        private bool isLocalPlayer;
        private bool isReady;
        private bool isGuest;
        private bool localUserIsGuest;
        private int idPlayer;

        public string Username
        {
            get => username;
            set 
            { 
                username = value; 
                OnPropertyChanged(nameof(Username)); 
                OnPropertyChanged(nameof(IsOccupied)); 
            }
        }

        public string Nickname
        {
            get => nickname;
            set 
            { 
                nickname = value; 
                OnPropertyChanged(nameof(Nickname)); 
            }
        }

        public bool IsFriend
        {
            get => isFriend;
            set 
            { 
                isFriend = value; 
                OnPropertyChanged(nameof(IsFriend)); 
            }
        }

        public string ProfilePicture
        {
            get => profilePicture;
            set
            {
                profilePicture = value;
                OnPropertyChanged(nameof(ProfilePicture));
            }
        }

        public bool CanKick
        {
            get => canKick;
            set 
            { 
                canKick = value; 
                OnPropertyChanged(nameof(CanKick)); 
            }
        }

        public bool IsLocalPlayer
        {
            get => isLocalPlayer;
            set 
            { 
                isLocalPlayer = value; 
                OnPropertyChanged(nameof(IsLocalPlayer)); 
            }
        }

        public bool IsReady
        {
            get => isReady;
            set 
            { 
                isReady = value; 
                OnPropertyChanged(nameof(IsReady)); 
            }
        }

        public bool IsGuest
        {
            get => isGuest;
            set
            {
                isGuest = value;
                OnPropertyChanged(nameof(IsGuest));
            }
        }

        public bool LocalUserIsGuest
        {
            get => localUserIsGuest;
            set { localUserIsGuest = value; OnPropertyChanged(nameof(LocalUserIsGuest)); }
        }

        public int IdPlayer
        {
            get => idPlayer;
            set
            {
                idPlayer = value;
                OnPropertyChanged(nameof(IdPlayer));
            }
        }

        public bool IsOccupied => !string.IsNullOrWhiteSpace(Username);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
