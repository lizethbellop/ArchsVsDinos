using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;

namespace ArchsVsDinosServer.Services
{
    public class RegisterManager : IRegisterManager
    {
        public bool Register(UserAccount userAccount)
        {
            Register register = new Register();
            bool confirmation = register.Register(userAccount);
        }
    }
}
