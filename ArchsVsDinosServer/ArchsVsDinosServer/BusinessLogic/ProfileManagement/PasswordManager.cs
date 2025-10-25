using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Wrappers;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class PasswordManager : BaseProfileService
    {
        public PasswordManager(
        Func<IDbContext> contextFactory,
        IValidationHelper validationHelper,
        ILoggerHelper loggerHelper,
        ISecurityHelper securityHelper)
        : base(contextFactory, validationHelper, loggerHelper, securityHelper)
        {
        }

        public PasswordManager()
        : base(
            () => new DbContextWrapper(),
            new ValidationHelperWrapper(),
            new LoggerHelperWrapper(),
            new SecurityHelperWrapper())
        {
        }

        public UpdateResponse ChangePassword(string username, string currentPassword, string newPassword)
        {
            try
            {
                UpdateResponse response = new UpdateResponse();

                if (ChangePasswordIsEmpty(username, currentPassword, newPassword))
                {
                    response.success = false;
                    response.resultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }

                if (currentPassword == newPassword)
                {
                    response.success = false;
                    response.resultCode = UpdateResultCode.Profile_SamePasswordValue;
                    return response;
                }

                if (newPassword.Length < 8)
                {
                    response.success = false;
                    response.resultCode = UpdateResultCode.Profile_PasswordTooShort;
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (userAccount == null)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (!VerifyPassword(currentPassword, userAccount.password))
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_InvalidPassword;
                        return response;
                    }

                    userAccount.password = securityHelper.HashPassword(newPassword);
                    context.SaveChanges();

                    response.success = true;
                    response.resultCode = UpdateResultCode.Profile_Success;
                    return response;


                }
            }
            catch (DbEntityValidationException e)
            {
                loggerHelper.LogError("Error de validacion en el metodo ChangePassword", e);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception e)
            {
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        private bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return securityHelper.VerifyPassword(plainPassword, hashedPassword);
        }

        private bool ChangePasswordIsEmpty(string value1, string value2, string value3)
        {
            return validationHelper.IsEmpty(value1) || validationHelper.IsEmpty(value2) || validationHelper.IsEmpty(value3);
        }
    }
}
