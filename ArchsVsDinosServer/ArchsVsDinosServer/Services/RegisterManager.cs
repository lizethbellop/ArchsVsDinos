using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;
using Contracts;
using Contracts.DTO.Result_Codes;
using Contracts.DTO;
using Contracts.DTO.Response;

namespace ArchsVsDinosServer.Services
{
    public class RegisterManager : IRegisterManager
    {
        public RegisterResponse RegisterUser(UserAccountDTO userAccountDto, string code)
        {
            Register register = new Register();
            RegisterResponse response = register.RegisterUser(userAccountDto, code);
            return response;
        }

        public bool SendEmailRegister(string email)
        {
            return new Register().SendEmailRegister(email);
        }

    }

}
