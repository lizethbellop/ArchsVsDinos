using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO;

namespace ArchsVsDinosServer.Utils
{
    public class VerificationCodeManager : IVerificationCodeManager
    {
        private List<VerificationCode> verificationCodes = new List<VerificationCode>();

        public void AddCode(string email, string code, DateTime expiration)
        {
            verificationCodes.Add(new VerificationCode
            {
                Email = email,
                Code = code,
                Expiration = expiration
            });
        }

        public bool ValidateCode(string email, string code)
        {
            var dataCheck = verificationCodes.Find(x => x.Email == email && x.Code == code);

            if (dataCheck != null && dataCheck.Expiration > DateTime.Now)
            {
                verificationCodes.Remove(dataCheck);
                return true;
            }
            return false;
        }
    }
}
