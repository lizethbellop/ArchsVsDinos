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

        public UserDTO currentUser { get; private set; }
        public PlayerDTO currentPlayer { get; private set; }
        public bool isGuest { get; private set; }
        
        private UserSession()
        {
            isGuest = false;
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
            currentUser = user;
            currentPlayer = player;
            isGuest = false;
        }

        public void LoginAsGuest()
        {
            currentUser = null;
            currentPlayer = null;
            isGuest = true;
        }

        public void Logout()
        {
            currentUser = null;
            currentPlayer = null;
            isGuest = false;
        }

        
        public string GetUsername() => currentUser?.username ?? string.Empty;
        public string GetNickname() => currentUser?.nickname ?? string.Empty;
        public string GetName() => currentUser?.name ?? string.Empty;
        public int GetUserId() => currentUser?.idUser ?? 0;
        public bool HasPlayer() => currentPlayer != null;
        public int GetPlayerId() => currentPlayer?.idPlayer ?? 0;
    }
}
