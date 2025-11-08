using ArchsVsDinosClient.FriendService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class FriendServiceClient : IFriendServiceClient
    {
        private readonly FriendManagerClient client;

        public FriendServiceClient()
        {
            client = new FriendManagerClient();
        }

        public async Task<FriendResponse> RemoveFriendAsync(string username, string friendUsername)
        {
            return await Task.Run(() => client.RemoveFriend(username, friendUsername));
        }

        public async Task<FriendListResponse> GetFriendsAsync(string username)
        {
            return await Task.Run(() => client.GetFriends(username));
        }

        public async Task<FriendCheckResponse> AreFriendsAsync(string username, string friendUsername)
        {
            return await Task.Run(() => client.AreFriends(username, friendUsername));
        }
    }
}
