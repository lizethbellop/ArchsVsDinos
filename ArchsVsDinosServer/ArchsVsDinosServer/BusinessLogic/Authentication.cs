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

namespace ArchsVsDinosServer.BusinessLogic
{
    public class Authentication
    {

        private readonly ISecurityHelper securityHelper;
        private readonly IValidationHelper validationHelper;
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;

        public Authentication(ISecurityHelper _securityHelper, IValidationHelper _validationHelper,ILoggerHelper _loggerHelper, Func<IDbContext> _contextFactory)
        {
            securityHelper = _securityHelper;
            validationHelper = _validationHelper;
            loggerHelper = _loggerHelper;
            contextFactory = _contextFactory;
        }

        public Authentication() : this(new Wrappers.SecurityHelperWrapper(),
            new Wrappers.ValidationHelperWrapper(), new Wrappers.LoggerHelperWrapper(), 
            () => (IDbContext)new Wrappers.DbContextWrapper())
        {

        }
        public LoginResponse Login(string username, string password) 
        {
            try
            {
                var response = new LoginResponse();
                if(IsEmpty(username, password))
                {
                    response.Success = false;
                    response.Message = "Campos requeridos";
                    return response;
                }
                using (var context = contextFactory())
                {
                    string passwordHash = securityHelper.HashPassword(password);
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username && u.password == passwordHash);
                    
                    if (user == null)
                    {
                        response.Success = false;
                        response.Message = "Credenciales incorrectas";
                        return response;
                    }

                    response.Success = true;
                    response.Message = "Login exitoso";
                    response.UserSession = new UserDTO
                    {
                        idUser = user.idUser,
                        name = user.name,
                        nickname = user.nickname,
                        username = user.username
                    };

                    if(user.Player != null)
                    {
                        response.AssociatedPlayer = new PlayerDTO
                        {
                            idPlayer = user.Player.idPlayer,
                            facebook = user.Player.facebook,
                            instagram = user.Player.instagram,
                            x = user.Player.x,
                            tiktok = user.Player.tiktok,
                            profilePicture = user.Player.profilePicture,
                            totalWins = user.Player.totalWins,
                            totalLosses = user.Player.totalLosses,
                            totalPoints = user.Player.totalPoints
                        };
                    }

                    return response;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Login for user: {username}", ex);
                return null;
            }
            catch (ArgumentException ex)
            {
                LoggerHelper.LogWarn($"Error while hashing the password: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Login: {ex.Message}");
                return null;
            }
        }

        private bool IsEmpty (string username, string password)
        {
            return validationHelper.IsEmpty(username) || validationHelper.IsEmpty(password);
        }
    }
}
