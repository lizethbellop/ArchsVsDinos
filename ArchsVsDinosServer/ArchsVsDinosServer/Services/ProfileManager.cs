using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class ProfileManager : IProfileManager
    {
        public UpdateResponse ChangePassword(string username, string currentPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public bool ChangeProfilePicture(string username)
        {
            throw new NotImplementedException();
        }

        public PlayerDTO GetProfile(string username)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateFacebook(string username, string newFacebook)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateInstagram(string username, string newInstagram)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateNickname(string username, string newNickname)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateTikTok(string username, string newTikTok)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateUsername(string currentUsername, string newUsername)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateX(string username, string newX)
        {
            throw new NotImplementedException();
        }
    }
}
