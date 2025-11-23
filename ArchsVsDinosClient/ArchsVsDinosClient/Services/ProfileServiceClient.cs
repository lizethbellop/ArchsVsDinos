using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class ProfileServiceClient : IProfileServiceClient
    {
        private readonly ProfileManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public ProfileServiceClient()
        {
            client = new ProfileManagerClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task<UpdateResponse> UpdateNicknameAsync(string currentUsername, string newNickname)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateNickname(currentUsername, newNickname)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar apodo"
            );
        }

        public async Task<UpdateResponse> UpdateUsernameAsync(string currentUsername, string newUsername)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateUsername(currentUsername, newUsername)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar nombre de usuario"
            );
        }

        public async Task<UpdateResponse> ChangePassworsAsync(string currentUsername, string currentPassword, string newPassword)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.ChangePassword(currentUsername, currentPassword, newPassword)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "cambiar contraseña"
            );
        }

        public async Task<UpdateResponse> UpdateFacebookAsync(string currentUsername, string newFacebookLink)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateFacebook(currentUsername, newFacebookLink)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar Facebook"
            );
        }

        public async Task<UpdateResponse> UpdateInstagramAsync(string currentUsername, string newInstagramLink)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateInstagram(currentUsername, newInstagramLink)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar Instagram"
            );
        }

        public async Task<UpdateResponse> UpdateXAsync(string currentUsername, string newXLink)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateX(currentUsername, newXLink)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar X"
            );
        }

        public async Task<UpdateResponse> UpdateTikTokAsync(string currentUsername, string newTikTokLink)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.UpdateTikTok(currentUsername, newTikTokLink)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "actualizar TikTok"
            );
        }

        public async Task<UpdateResponse> ChangeProfilePictureAsync(string username, string avatarPath)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.ChangeProfilePicture(username, avatarPath)),
                defaultValue: new UpdateResponse { Success = false },
                operationName: "cambiar avatar"
            );
        }

        public async Task<string> GetProfilePictureAsync(string username)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetProfilePicture(username)),
                defaultValue: "/Resources/Images/Avatars/default_avatar_01.png",
                operationName: "obtener avatar"
            );
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                if (client?.State == CommunicationState.Opened)
                    client.Close();
                else if (client?.State == CommunicationState.Faulted)
                    client.Abort();
            }
            catch
            {
                client?.Abort();
            }

            isDisposed = true;
        }
    }
}
