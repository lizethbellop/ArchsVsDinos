using ArchsVsDinosClient.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    internal class UserSession
    {

        private static UserSession instance;
        private static readonly object lockObject = new object();

        public UserDTO CurrentUser { get; private set; }
        public PlayerDTO CurrentPlayer { get; private set; }
        public bool IsLoggedIn { get; private set; }

        // Constructor privado
        private UserSession()
        {
            IsLoggedIn = false;
        }

        // Instancia única
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

        // Iniciar sesión
        public void Login(UserDTO user, PlayerDTO player = null)
        {
            CurrentUser = user;
            CurrentPlayer = player;
            IsLoggedIn = true;
        }

        // Cerrar sesión
        public void Logout()
        {
            CurrentUser = null;
            CurrentPlayer = null;
            IsLoggedIn = false;
        }

        // Métodos helper
        public string GetUsername()
        {
            return CurrentUser?.username ?? string.Empty;
        }

        public string GetNickname()
        {
            return CurrentUser?.nickname ?? string.Empty;
        }

        public string GetName()
        {
            return CurrentUser?.name ?? string.Empty;
        }

        public int GetUserId()
        {
            return CurrentUser?.idUser ?? 0;
        }

        public bool HasPlayer()
        {
            return CurrentPlayer != null;
        }

        public int GetPlayerId()
        {
            return CurrentPlayer?.idPlayer ?? 0;
        }
    }   
}
