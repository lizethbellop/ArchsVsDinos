using ArchsVsDinosClient.ProfileManagerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IProfileServiceClient
    {
        Task<UpdateResponse> UpdateNicknameAsync(string currentUsername, string newNickname);
        Task<UpdateResponse> UpdateUsernameAsync(string currentUsername, string newUsername);
        Task<UpdateResponse> ChangePassworsAsync(string currentUsername, string currentPassword, string newPassword);
        Task<UpdateResponse> UpdateFacebookAsync(string currentUsername, string newFacebookLink);
        Task<UpdateResponse> UpdateInstagramAsync(string currentUsername, string newInstagramLink);
        Task<UpdateResponse> UpdateXAsync(string currentUsername, string newXLink);
        Task<UpdateResponse> UpdateTikTokAsync(string currentUsername, string newTikTokLink);

    }
}
