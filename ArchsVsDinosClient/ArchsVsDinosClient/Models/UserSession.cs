using ArchsVsDinosClient.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    public class UserSession
    {
        private static UserSession instance;
        private static readonly object lockObject = new object();

        public UserDTO CurrentUser { get; private set; }
        public PlayerDTO CurrentPlayer { get; private set; }
        public bool IsGuest { get; private set; }
        
        private UserSession()
        {
            IsGuest = false;
        }

        
        public static UserSession Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new UserSession();
                        }
                    }
                }
                return instance;
            }
        }

        public void Login(UserDTO user, PlayerDTO player = null)
        {
            CurrentUser = user;
            CurrentPlayer = player;
            IsGuest = false;
        }

        public void LoginAsGuest()
        {
            CurrentUser = null;
            CurrentPlayer = null;
            IsGuest = true;
        }

        public void Logout()
        {
            CurrentUser = null;
            CurrentPlayer = null;
            IsGuest = false;
        }

        
        public string GetUsername() => CurrentUser?.Username ?? string.Empty;
        public string GetNickname() => CurrentUser?.Nickname ?? string.Empty;
        public string GetName() => CurrentUser?.Name ?? string.Empty;
        public int GetUserId() => CurrentUser?.IdUser ?? 0;
        public bool HasPlayer() => CurrentPlayer != null;
        public int GetPlayerId() => CurrentPlayer?.IdPlayer ?? 0;
    }
}
