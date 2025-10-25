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
        public bool IsLoggedIn { get; private set; }

        
        private UserSession()
        {
            IsLoggedIn = false;
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
            IsLoggedIn = true;
        }

        
        public void Logout()
        {
            CurrentUser = null;
            CurrentPlayer = null;
            IsLoggedIn = false;
        }

        
        public string GetUsername() => CurrentUser?.username ?? string.Empty;
        public string GetNickname() => CurrentUser?.nickname ?? string.Empty;
        public string GetName() => CurrentUser?.name ?? string.Empty;
        public int GetUserId() => CurrentUser?.idUser ?? 0;
        public bool HasPlayer() => CurrentPlayer != null;
        public int GetPlayerId() => CurrentPlayer?.idPlayer ?? 0;
    }
}
