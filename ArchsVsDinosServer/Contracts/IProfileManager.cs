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
        UpdateResponse UpdateUsername(string currentUsername, string newUsername);

        [OperationContract]
        UpdateResponse UpdateNickname(string username, string newNickname);

        [OperationContract]
        UpdateResponse UpdateFacebook(string username, string newFacebook);

        [OperationContract]
        UpdateResponse UpdateX(string username, string newX);

        [OperationContract]
        UpdateResponse UpdateInstagram(string username, string newInstagram);

        [OperationContract] 
        UpdateResponse UpdateTikTok(string username, string newTikTok);

        [OperationContract]
        UpdateResponse ChangeProfilePicture(string username, string avatarPath);

        [OperationContract]
        string GetProfilePicture(string username);

        [OperationContract]
        UpdateResponse ChangePassword(string username, string currentPassword, string newPassword);
    }
}
