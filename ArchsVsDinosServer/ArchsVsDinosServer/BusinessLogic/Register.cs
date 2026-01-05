using ArchsVsDinosServer.Default;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Management;


namespace ArchsVsDinosServer.BusinessLogic
{
    public class Register
    {
        private readonly ISecurityHelper securityHelper;
        private readonly ILoggerHelper loggerHelper;
        private readonly IEmailService emailService;
        private readonly ICodeGenerator codeGenerator;
        private readonly IVerificationCodeManager codeManager;
        private readonly Func<IDbContext> contextFactory;


        public Register() : this(new RegisterServiceDependencies())
        {
        }
        public Register(RegisterServiceDependencies dependencies)
        {
            this.securityHelper = dependencies.securityHelper;
            this.loggerHelper = dependencies.loggerHelper;
            this.emailService = dependencies.emailService;
            this.codeGenerator = dependencies.codeGenerator;
            this.codeManager = dependencies.codeManager;
            this.contextFactory = dependencies.contextFactory;
        }

        public RegisterResponse RegisterUser(UserAccountDTO userAccountDTO, string code)
        {
            try
            {
                if (!codeManager.ValidateCode(userAccountDTO.Email, code))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        ResultCode = RegisterResultCode.Register_InvalidCode
                    };
                }

                var validationUsernameAndNickname = ValidateUsernameAndNicknameResult(
                    userAccountDTO.Username,
                    userAccountDTO.Nickname);

                if (!validationUsernameAndNickname.Success)
                    return validationUsernameAndNickname;

                using (var context = contextFactory())
                {
                    var player = InitialConfig.InitialPlayer;
                    context.Player.Add(player);
                    context.SaveChanges();

                    var configuration = InitialConfig.InitialConfiguration;
                    context.Configuration.Add(configuration);
                    context.SaveChanges();

                    var userAccount = new UserAccount
                    {
                        email = userAccountDTO.Email,
                        password = securityHelper.HashPassword(userAccountDTO.Password),
                        name = userAccountDTO.Name,
                        username = userAccountDTO.Username,
                        nickname = userAccountDTO.Nickname,
                        idConfiguration = configuration.idConfiguration,
                        idPlayer = player.idPlayer
                    };

                    context.UserAccount.Add(userAccount);
                    context.SaveChanges();

                    return new RegisterResponse
                    {
                        Success = true,
                        ResultCode = RegisterResultCode.Register_Success
                    };
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error at Register", ex);
                return new RegisterResponse
                {
                    Success = false,
                    ResultCode = RegisterResultCode.Register_DatabaseError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error at Register", ex);
                return new RegisterResponse
                {
                    Success = false,
                    ResultCode = RegisterResultCode.Register_UnexpectedError
                };
            }
        }

        public bool SendEmailRegister(string email)
        {
            try
            {
                string verificationCode = codeGenerator.GenerateVerificationCode();
                bool emailSent = emailService.SendVerificationEmail(email, verificationCode);

                if (emailSent)
                {
                    codeManager.AddCode(email, verificationCode, DateTime.Now.AddMinutes(10));
                    return true;
                }

                return false;
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError("Database error at SendEmailRegister", ex);

                throw new FaultException<string>(
                    RegisterResultCode.Register_DatabaseError.ToString(),
                    new FaultReason(RegisterResultCode.Register_DatabaseError.ToString())
                );
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error at SendEmailRegister", ex);

                throw new FaultException<string>(
                    RegisterResultCode.Register_UnexpectedError.ToString(),
                    new FaultReason(RegisterResultCode.Register_UnexpectedError.ToString())
                );
            }

        }

        public RegisterResponse ValidateUsernameAndNicknameResult(string newUsername, string newNickname)
        {
            try
            {
                using (var context = contextFactory())
                {
                    bool usernameExists = context.UserAccount.Any(u => u.username == newUsername);
                    bool nicknameExists = context.UserAccount.Any(u => u.nickname == newNickname);

                    if (usernameExists && nicknameExists)
                    {
                        return new RegisterResponse
                        {
                            Success = false,
                            ResultCode = RegisterResultCode.Register_BothExists
                        };
                    }

                    if (usernameExists)
                    {
                        return new RegisterResponse
                        {
                            Success = false,
                            ResultCode = RegisterResultCode.Register_UsernameExists
                        };
                    }

                    if (nicknameExists)
                    {
                        return new RegisterResponse
                        {
                            Success = false,
                            ResultCode = RegisterResultCode.Register_NicknameExists
                        };
                    }

                    return new RegisterResponse
                    {
                        Success = true
                    };
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Error validating username and nickname : {ex.Message}", ex);
                return new RegisterResponse
                {
                    Success = false,
                    ResultCode = RegisterResultCode.Register_DatabaseError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error in Register: {ex.Message}", ex);
                return new RegisterResponse
                {
                    Success = false,
                    ResultCode = RegisterResultCode.Register_UnexpectedError
                };
            }
        }

    }

}
