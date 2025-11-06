using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class ProfileServiceClient : IProfileServiceClient
    {
        private readonly ProfileManagerClient client;

        public ProfileServiceClient()
        {
            client = new ProfileManagerClient();
        }

        public async Task<UpdateResponse> ChangePassworsAsync(string currentUsername, string currentPassword, string newPassword)
        {
            return await Task.Run(() => client.ChangePassword(currentUsername, currentPassword, newPassword));
        }

        public async Task<UpdateResponse> UpdateFacebookAsync(string currentUsername, string newFacebookLink)
        {
            return await Task.Run(() => client.UpdateFacebook(currentUsername, newFacebookLink));
        }

        public async Task<UpdateResponse> UpdateInstagramAsync(string currentUsername, string newInstagramLink)
        {
            return await Task.Run(() => client.UpdateInstagram(currentUsername, newInstagramLink));
        }

        public async Task<UpdateResponse> UpdateNicknameAsync(string currentUsername, string newNickname)
        {
            return await Task.Run(() => client.UpdateNickname(currentUsername, newNickname));
        }

        public async Task<UpdateResponse> UpdateTikTokAsync(string currentUsername, string newTikTokLink)
        {
            return await Task.Run(() => client.UpdateTikTok(currentUsername, newTikTokLink));
        }

        public async Task<UpdateResponse> UpdateUsernameAsync(string currentUsername, string newUsername)
        {
            return await Task.Run(() => client.UpdateUsername(currentUsername, newUsername));
        }

        public async Task<UpdateResponse> UpdateXAsync(string currentUsername, string newXLink)
        {
            return await Task.Run(() => client.UpdateX(currentUsername, newXLink));
        }
    }
}
