using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;
using Contracts;
using Contracts.DTO;

namespace ArchsVsDinosServer.Services
{
    public class RegisterManager : IRegisterManager
    {
        public bool RegisterUser(UserAccountDTO userAccountDto, string code)
        {
            Register register = new Register();
            return register.RegisterUser(userAccountDto, code);
        }

        public bool SendEmailRegister(string email)
        {
            return new Register().SendEmailRegister(email);
        }

        public ValiUserNickResultDTO ValidateUsernameAndNickname(string username, string nickname)
        {
            return new Register().ValidateUserameAndNicknameResult(username, nickname);
        }
    }

}
}
