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
        public PasswordManager(ServiceDependencies dependencies)
        : base(dependencies)
        {
        }

        public PasswordManager()
            : base(new ServiceDependencies())
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
                    response.resultCode = UpdateResultCode.Profile_ChangePasswordSuccess;
                    return response;


                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Error de validacion en el metodo ChangePassword", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
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
