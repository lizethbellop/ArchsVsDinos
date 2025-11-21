using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels
{
    public class AvatarSelectionViewModel
    {
        public string SelectedAvatarPath { get; private set; }

        private readonly Dictionary<int, string> avatarPaths;

        public AvatarSelectionViewModel()
        {
            avatarPaths = new Dictionary<int, string>
            {
                { 1, "/Resources/Images/Avatars/default_avatar_01.png" },
                { 2, "/Resources/Images/Avatars/default_avatar_02.png" },
                { 3, "/Resources/Images/Avatars/default_avatar_03.png" },
                { 4, "/Resources/Images/Avatars/default_avatar_04.png" },
                { 5, "/Resources/Images/Avatars/default_avatar_05.png" }
            };
        }

        public void SelectAvatar(int avatarId)
        {
            if (avatarPaths.ContainsKey(avatarId))
            {
                SelectedAvatarPath = avatarPaths[avatarId];
            }
        }

        public string GetSelectedAvatarPath()
        {
            return SelectedAvatarPath;
        }

        /// <summary>
        /// Verifica si hay un avatar seleccionado
        /// </summary>
        public bool HasSelectedAvatar()
        {
            return !string.IsNullOrEmpty(SelectedAvatarPath);
        }

        /// <summary>
        /// Obtiene la ruta de un avatar específico por ID
        /// </summary>
        public string GetAvatarPath(int avatarId)
        {
            return avatarPaths.ContainsKey(avatarId) ? avatarPaths[avatarId] : null;
        }
    }
}
