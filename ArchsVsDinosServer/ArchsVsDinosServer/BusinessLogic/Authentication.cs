using ArchsVsDinosServer;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class Authentication
    {
        private readonly ISecurityHelper securityHelper;
        private readonly IValidationHelper validationHelper;
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;
        private readonly IStrikeManager strikeManager;

        public Authentication(ServiceDependencies dependencies, IStrikeManager strikeManager)
        {
            securityHelper = dependencies.securityHelper;
            validationHelper = dependencies.validationHelper;
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
            this.strikeManager = strikeManager;
        }

        public Authentication()
        : this(new ServiceDependencies(), new StrikeManager(new StrikeServiceDependencies()))
        {
        }

        public Authentication(ServiceDependencies dependencies)
            : this(dependencies, new StrikeManager(new StrikeServiceDependencies()))
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

                    if (strikeManager.IsUserBanned(user.idUser))
                    {
                        response.Success = false;
                        response.ResultCode = LoginResultCode.Authentication_UserBanned;
                        loggerHelper.LogInfo($"Banned user {username} attempted to login");
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
                loggerHelper.LogError($"Unexpected error at Login for user: {username}", ex);
                return new LoginResponse
                {
                    Success = false,
                    ResultCode = LoginResultCode.Authentication_UnexpectedError
                };
            }
        }

        private bool IsEmpty(string username, string password)
        {
            return validationHelper.IsEmpty(username) || validationHelper.IsEmpty(password);
        }
    }
}
