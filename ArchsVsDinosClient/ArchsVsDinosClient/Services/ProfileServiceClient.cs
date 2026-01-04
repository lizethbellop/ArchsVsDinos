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
        private ProfileManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private readonly ILogger logger;
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public bool IsServerAvailable => guardian.IsServerAvailable;
        public string LastErrorTitle => guardian.LastErrorTitle;
        public string LastErrorMessage => guardian.LastErrorMessage;

        public ProfileServiceClient()
        {
            logger = new Logger(typeof(ProfileServiceClient));
            client = new ProfileManagerClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) =>
                {
                    logger.LogError($"🔴 Guardian reportó error: {title} - {msg}");
                    ConnectionError?.Invoke(title, msg);
                },
                logger: logger
            );

            guardian.MonitorClientState(client);
        }

        public Task<UpdateResponse> UpdateNicknameAsync(string currentUsername, string newNickname)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateNickname(currentUsername, newNickname)),
                operationName: "Actualizar apodo"
            );
        }

        public Task<UpdateResponse> UpdateUsernameAsync(string currentUsername, string newUsername)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateUsername(currentUsername, newUsername)),
                operationName: "Actualizar username"
            );
        }

        public Task<UpdateResponse> ChangePassworsAsync(string currentUsername, string currentPassword, string newPassword)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.ChangePassword(currentUsername, currentPassword, newPassword)),
                operationName: "Cambiar contraseña"
            );
        }

        public Task<UpdateResponse> UpdateFacebookAsync(string currentUsername, string newFacebookLink)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateFacebook(currentUsername, newFacebookLink)),
                operationName: "Actualizar Facebook"
            );
        }

        public Task<UpdateResponse> UpdateInstagramAsync(string currentUsername, string newInstagramLink)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateInstagram(currentUsername, newInstagramLink)),
                operationName: "Actualizar Instagram"
            );
        }

        public Task<UpdateResponse> UpdateXAsync(string currentUsername, string newXLink)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateX(currentUsername, newXLink)),
                operationName: "Actualizar X"
            );
        }

        public Task<UpdateResponse> UpdateTikTokAsync(string currentUsername, string newTikTokLink)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.UpdateTikTok(currentUsername, newTikTokLink)),
                operationName: "Actualizar TikTok"
            );
        }

        public Task<UpdateResponse> ChangeProfilePictureAsync(string username, string avatarPath)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.ChangeProfilePicture(username, avatarPath)),
                operationName: "Cambiar avatar"
            );
        }

        public Task<string> GetProfilePictureAsync(string username)
        {
            return guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.GetProfilePicture(username)),
                operationName: "Obtener avatar"
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
