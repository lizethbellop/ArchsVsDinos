using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;
using System.Text.RegularExpressions;

namespace ArchsVsDinosServer.BusinessLogic
{
    internal class RecoveryInfo
    {
        public string Code { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsUsed { get; set; }
    }

    public class PasswordRecoveryLogic
    {
        private static readonly Dictionary<string, RecoveryInfo> activeCodes = new Dictionary<string, RecoveryInfo>();

        private readonly Func<IDbContext> contextFactory;
        private readonly ISecurityHelper securityHelper;
        private readonly ILoggerHelper logger;

        public PasswordRecoveryLogic(ServiceDependencies dependencies)
        {
            contextFactory = dependencies.contextFactory;
            securityHelper = dependencies.securityHelper;
            logger = dependencies.loggerHelper;
        }

        public PasswordRecoveryLogic() : this(new ServiceDependencies())
        {
        }

        public RecoveryCodeResponse SendCode(string username)
        {
            try
            {
                if (activeCodes.ContainsKey(username))
                {
                    var existing = activeCodes[username];

                    if (existing.ExpirationTime > DateTime.Now && !existing.IsUsed)
                    {
                        return new RecoveryCodeResponse
                        {

                            Result = PasswordRecoveryResult.PasswordRecovery_CooldownActive,
                            RemainingSeconds = (existing.ExpirationTime - DateTime.Now).TotalSeconds
                        };
                    }
                    else
                    {
                        activeCodes.Remove(username);
                    }
                }

                string email = "";

                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        return new RecoveryCodeResponse
                        {
                            Result = PasswordRecoveryResult.PasswordRecovery_UserNotFound
                        };
                    }
                    email = user.email;
                }

                string code = CodeGenerator.GenerateMatchCode();

                EmailService.SendPasswordRecoveryEmail(email, code);

                activeCodes[username] = new RecoveryInfo
                {
                    Code = code,
                    ExpirationTime = DateTime.Now.AddMinutes(10),
                    IsUsed = false
                };

                return new RecoveryCodeResponse
                {
                    Result = PasswordRecoveryResult.PasswordRecovery_Success,
                    RemainingSeconds = 600
                };
            }
            catch (SqlException ex)
            {
                logger.LogError($"SQL Error in SendCode for {username}", ex);
                return new RecoveryCodeResponse { Result = PasswordRecoveryResult.PasswordRecovery_DatabaseError };
            }
            catch (EntityException ex)
            {
                logger.LogError($"Entity Error in SendCode for {username}", ex);
                return new RecoveryCodeResponse { Result = PasswordRecoveryResult.PasswordRecovery_DatabaseError };
            }
            catch (SmtpException ex)
            {
                logger.LogError($"SMTP Error sending email to {username}", ex);
                return new RecoveryCodeResponse { Result = PasswordRecoveryResult.PasswordRecovery_ServerError };
            }
            catch (TimeoutException ex)
            {
                logger.LogError($"Timeout in SendCode for {username}", ex);
                return new RecoveryCodeResponse { Result = PasswordRecoveryResult.PasswordRecovery_ConnectionError };
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected Error in SendCode for {username}", ex);
                return new RecoveryCodeResponse { Result = PasswordRecoveryResult.PasswordRecovery_UnexpectedError };
            }
        }

        public bool ValidateCode(string username, string code)
        {
            if (!activeCodes.ContainsKey(username))
            {
                return false;
            }

            var info = activeCodes[username];

            if (DateTime.Now > info.ExpirationTime || info.IsUsed)
            {
                return false;
            }

            if (info.Code == code)
            {
                return true;
            }

            return false;
        }

        public bool UpdatePassword(string username, string newPassword)
        {
            try
            {
                if (!ValidatePasswordSecurity(newPassword))
                {
                    return false;
                }

                if (!activeCodes.ContainsKey(username))
                {
                    return false;
                }

                var info = activeCodes[username];

                if (DateTime.Now > info.ExpirationTime || info.IsUsed)
                {
                    return false;
                }

                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (user != null)
                    {
                        user.password = securityHelper.HashPassword(newPassword);
                        context.SaveChanges();

                        info.IsUsed = true;
                        return true;
                    }
                }

                return false;
            }
            catch (SqlException ex)
            {
                logger.LogError($"SQL Error updating password for {username}", ex);
                return false;
            }
            catch (EntityException ex)
            {
                logger.LogError($"Entity Error updating password for {username}", ex);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected Error updating password for {username}", ex);
                return false;
            }
        }

        private bool ValidatePasswordSecurity(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            if (password.Length < 8 || password.Length > 20) return false;

            var hasUpper = new Regex(@"[A-Z]+");
            var hasDigit = new Regex(@"[0-9]+");
            var hasSpecial = new Regex(@"[!@#$%^&*(),.?""{}|<>]+");

            return hasUpper.IsMatch(password) && hasDigit.IsMatch(password) && hasSpecial.IsMatch(password);
        }
    }
}