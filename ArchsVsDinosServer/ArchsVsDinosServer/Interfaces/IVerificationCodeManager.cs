using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IVerificationCodeManager
    {
        void AddCode(string email, string code, DateTime expiration);
        bool ValidateCode(string email, string code);
    }
}
