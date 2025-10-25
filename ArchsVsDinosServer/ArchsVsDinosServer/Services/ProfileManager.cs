using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using ArchsVsDinosServer.BusinessLogic.ProfileManagement;

namespace ArchsVsDinosServer.Services
{
    public class ProfileManager : IProfileManager
    {
        private ProfileInformation profileInformation;
        private SocialMediaManager socialMediaManager;

        public ProfileManager()
        {
            profileInformation = new ProfileInformation();
            socialMediaManager = new SocialMediaManager();
        }

        public UpdateResponse ChangePassword(string username, string currentPassword, string newPassword)
        {
            PasswordManager passwordManager = new PasswordManager();
            UpdateResponse passwordUpdateResponse = passwordManager.ChangePassword(username, currentPassword, newPassword);
            return passwordUpdateResponse;
        }

        public UpdateResponse ChangeProfilePicture(string username, byte[] profilePhoto, string fileExtension)
        {
            UpdateResponse profilePictureResponse = profileInformation.ChangeProfilePicture(username, profilePhoto, fileExtension);
            return profilePictureResponse;
        }

        public byte[] GetProfilePicture(string username)
        {
            return profileInformation.GetProfilePicture(username);
        }

        public UpdateResponse UpdateFacebook(string username, string newFacebook)
        {
            UpdateResponse facebookUpdateResponse = socialMediaManager.UpdateFacebook(username, newFacebook);
            return facebookUpdateResponse;
        }

        public UpdateResponse UpdateInstagram(string username, string newInstagram)
        {
            UpdateResponse instagramUpdateResponse = socialMediaManager.UpdateInstagram(username, newInstagram);
            return instagramUpdateResponse;
        }

        public UpdateResponse UpdateNickname(string username, string newNickname)
        {
            UpdateResponse nicknameUpdateResponse = profileInformation.UpdateNickname(username, newNickname);
            return nicknameUpdateResponse;
        }

        public UpdateResponse UpdateTikTok(string username, string newTikTok)
        {
            UpdateResponse tiktokUpdateResponse = socialMediaManager.UpdateTikTok(username, newTikTok);
            return tiktokUpdateResponse;
        }

        public UpdateResponse UpdateUsername(string currentUsername, string newUsername)
        {
            UpdateResponse usernameUpdateResponse = profileInformation.UpdateUsername(currentUsername, newUsername);
            return usernameUpdateResponse;
        }

        public UpdateResponse UpdateX(string username, string newX)
        {
            UpdateResponse xUpdateResponse = socialMediaManager.UpdateX(username, newX);
            return xUpdateResponse;
        }
    }
}
