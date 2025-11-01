using ArchsVsDinosServer;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class Authentication
    {

        private readonly ISecurityHelper securityHelper;
        private readonly IValidationHelper validationHelper;
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;

        public Authentication(ServiceDependencies dependencies)
        {
            securityHelper = dependencies.securityHelper;
            validationHelper = dependencies.validationHelper;
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
        }

        public Authentication() : this(new ServiceDependencies())
        {
        }
        public LoginResponse Login(string username, string password)
        {

            try
            {
                LoginResponse response = new LoginResponse();
                if (IsEmpty(username, password))
                {
                    response.Success = false;
                    response.ResultCode = LoginResultCode.Authentication_EmptyFields;
                    return response;
                }

                using (var context = contextFactory())
                {

                    UserAccount user = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        response.Success = false;
                        response.ResultCode = LoginResultCode.Authentication_InvalidCredentials;
                        return response;
                    }
                    if (!securityHelper.VerifyPassword(password, user.password))
                    {
                        response.Success = false;
                        response.ResultCode = LoginResultCode.Authentication_InvalidCredentials;
                        return response;
                    }
                    response.Success = true;
                    response.ResultCode = LoginResultCode.Authentication_Success;
                    response.UserSession = new UserDTO
                    {
                        IdUser = user.idUser,
                        Name = user.name,
                        Nickname = user.nickname,
                        Username = user.username,
                        Email = user.email,
                    };
                    if (user.Player != null)
                    {
                        response.AssociatedPlayer = new PlayerDTO
                        {
                            IdPlayer = user.Player.idPlayer,
                            Facebook = user.Player.facebook,
                            Instagram = user.Player.instagram,
                            X = user.Player.x,
                            Tiktok = user.Player.tiktok,
                            ProfilePicture = user.Player.profilePicture,
                            TotalWins = user.Player.totalWins,
                            TotalLosses = user.Player.totalLosses,
                            TotalPoints = user.Player.totalPoints
                        };
                    }
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error at Login for user: {username}", ex);
                return new LoginResponse
                {
                    Success = false,
                    ResultCode = LoginResultCode.Authentication_DatabaseError
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Login: {ex.Message}");
                return new LoginResponse
                {
                    Success = false,
                    ResultCode = LoginResultCode.Authentication_UnexpectedError
                };
            }
        }

        private bool IsEmpty (string username, string password)
        {
            return validationHelper.IsEmpty(username) || validationHelper.IsEmpty(password);
        }
    }
}
