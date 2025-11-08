using ArchsVsDinosClient.FriendService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IFriendServiceClient
    {
        Task<FriendResponse> RemoveFriendAsync(string username, string friendUsername);
        Task<FriendListResponse> GetFriendsAsync(string username);
        Task<FriendCheckResponse> AreFriendsAsync(string username, string friendUsername);
    }
}
