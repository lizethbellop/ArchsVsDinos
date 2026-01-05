using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class PasswordManager : BaseProfileService
    {
        private readonly IPasswordValidator passwordValidator;

        public PasswordManager(ServiceDependencies dependencies, IPasswordValidator passwordValidator = null)
            : base(dependencies)
        {
            this.passwordValidator = passwordValidator ?? new PasswordValidator();
        }

        public PasswordManager()
            : base(new ServiceDependencies())
        {
            this.passwordValidator = new PasswordValidator();
        }

        public UpdateResponse ChangePassword(string username, string currentPassword, string newPassword)
        {
            try
            {
                UpdateResponse response = new UpdateResponse();

                if (ChangePasswordIsEmpty(username, currentPassword, newPassword))
                {
                    response.Success = false;
                    response.ResultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }

                if (currentPassword == newPassword)
                {
                    response.Success = false;
                    response.ResultCode = UpdateResultCode.Profile_SamePasswordValue;
                    return response;
                }

                var passwordValidation = passwordValidator.ValidatePassword(newPassword);
                if (!passwordValidation.IsValid)
                {
                    response.Success = false;
                    response.ResultCode = passwordValidation.ResultCode;
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);
                    if (userAccount == null)
                    {
                        response.Success = false;
                        response.ResultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (!VerifyPassword(currentPassword, userAccount.password))
                    {
                        response.Success = false;
                        response.ResultCode = UpdateResultCode.Profile_InvalidPassword;
                        return response;
                    }

                    userAccount.password = securityHelper.HashPassword(newPassword);
                    context.SaveChanges();

                    response.Success = true;
                    response.ResultCode = UpdateResultCode.Profile_ChangePasswordSuccess;
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Validation error in the ChangePassword method", ex);
                return new UpdateResponse { Success = false, ResultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in ChangePassword", ex);
                return new UpdateResponse { Success = false, ResultCode = UpdateResultCode.Profile_UnexpectedError };
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
