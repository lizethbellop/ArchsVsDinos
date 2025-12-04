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
        private readonly ILoggerHelper loggerHelper;

        // Constructor que recibe el logger
        public VerificationCodeManager(ILoggerHelper logger)
        {
            this.loggerHelper = logger;
        }

        public void AddCode(string email, string code, DateTime expiration)
        {
            verificationCodes.Add(new VerificationCode
            {
                Email = email,
                Code = code,
                Expiration = expiration
            });

            loggerHelper.LogInfo($"✓ Code added - Email: {email}, Code: {code}, Total codes: {verificationCodes.Count}");
        }

        public bool ValidateCode(string email, string code)
        {
            loggerHelper.LogInfo($"→ Validating - Email: {email}, Code: {code}, Total codes in list: {verificationCodes.Count}");

            var dataCheck = verificationCodes.Find(x => x.Email == email && x.Code == code);

            if (dataCheck != null)
            {
                if (dataCheck.Expiration > DateTime.Now)
                {
                    verificationCodes.Remove(dataCheck);
                    loggerHelper.LogInfo($"✓ Code validated successfully for {email}");
                    return true;
                }
                else
                {
                    loggerHelper.LogWarning($"✗ Code EXPIRED for {email}. Expiration: {dataCheck.Expiration}, Now: {DateTime.Now}");
                    return false;
                }
            }

            loggerHelper.LogWarning($"✗ Code NOT FOUND for {email}. Available codes: {verificationCodes.Count}");
            return false;
        }
    }
}
