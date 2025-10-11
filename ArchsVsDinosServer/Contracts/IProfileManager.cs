using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IProfileManager
    {
        [OperationContract]
        PlayerDTO GetProfile(string username);

        [OperationContract]
        UpdateResponse UpdateUsername(string currentUsername, string newUsername);

        [OperationContract]
        bool UpdateNickname(string username, string newNickname);

        [OperationContract]
        bool UpdateFacebook(string username, string newFacebook);

        [OperationContract]
        bool UpdateX(string username, string newX);

        [OperationContract]
        bool UpdateInstagram(string username, string newInstagram);

        [OperationContract] 
        bool UpdateTikTok(string username, string newTikTok);

        [OperationContract]
        bool ChangeProfilePicture(string username);

        [OperationContract]
        bool ChangePassword(string username, string currentPassword, string newPassword);
    }
}
