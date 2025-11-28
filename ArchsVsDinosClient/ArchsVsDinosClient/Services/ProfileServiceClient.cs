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
        private readonly ILogger logger;
        private bool isDisposed;

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
                },
                logger: logger
            );
            guardian.MonitorClientState(client);
        }

        public async Task<UpdateResponse> UpdateNicknameAsync(string currentUsername, string newNickname)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateNickname(currentUsername, newNickname)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar apodo"
                );
            });
        }

        public async Task<UpdateResponse> UpdateUsernameAsync(string currentUsername, string newUsername)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateUsername(currentUsername, newUsername)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar nombre de usuario"
                );
            });
        }

        public async Task<UpdateResponse> ChangePassworsAsync(string currentUsername, string currentPassword, string newPassword)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.ChangePassword(currentUsername, currentPassword, newPassword)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "cambiar contraseña"
                );
            });
        }

        public async Task<UpdateResponse> UpdateFacebookAsync(string currentUsername, string newFacebookLink)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateFacebook(currentUsername, newFacebookLink)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar Facebook"
                );
            });
        }

        public async Task<UpdateResponse> UpdateInstagramAsync(string currentUsername, string newInstagramLink)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateInstagram(currentUsername, newInstagramLink)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar Instagram"
                );
            });
        }

        public async Task<UpdateResponse> UpdateXAsync(string currentUsername, string newXLink)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateX(currentUsername, newXLink)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar X"
                );
            });
        }

        public async Task<UpdateResponse> UpdateTikTokAsync(string currentUsername, string newTikTokLink)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.UpdateTikTok(currentUsername, newTikTokLink)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "actualizar TikTok"
                );
            });
        }

        public async Task<UpdateResponse> ChangeProfilePictureAsync(string username, string avatarPath)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.ChangeProfilePicture(username, avatarPath)),
                    defaultValue: new UpdateResponse { Success = false },
                    operationName: "cambiar avatar"
                );
            });
        }

        public async Task<string> GetProfilePictureAsync(string username)
        {
            return await Task.Run(async () =>
            {
                return await guardian.ExecuteAsync(
                    () => Task.FromResult(client.GetProfilePicture(username)),
                    defaultValue: "/Resources/Images/Avatars/default_avatar_01.png",
                    operationName: "obtener avatar"
                );
            });
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
