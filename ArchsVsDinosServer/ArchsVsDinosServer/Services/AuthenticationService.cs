using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Models;

namespace ArchsVsDinosServer.Services
{
    public class AuthenticationService
    {
        public bool RegisterUser(string username, string password, string email, string name, string nickname)
        {
            try
            {
                using (var context = new ArchsVsDinosConnection())
                {
                    if(context.UserAccount.Any(u => u.username== username))
                    {
                        Console.WriteLine("El usuario ya existe");
                        return false;
                    }

                    string hashedPassword = HassPassword(password);

                    var newUser = new UserAccount
                    {
                        username = username,
                        email = email,
                        password = hashedPassword,
                        name = name,
                        nickname = nickname
                    };

                    context.UserAccount.Add(newUser);
                    int result = context.SaveChanges();

                    Console.WriteLine($"Usuario registrado: {username}");
                    return result > 0;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public UserDTO LoginUser(string username, string password)
        {
            try
            {
                using (var context = new ArchsVsDinosConnection())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        Console.WriteLine("No se encuentra el usuario con esas credenciales");
                        return null;
                    }

                    string inputHash = HassPassword(password);

                    if (inputHash == user.password)
                    {
                        Console.WriteLine($"Login exitoso: {username}");

                        return new UserDTO
                        {
                            idUser = user.idUser,
                            username = user.username,
                            name = user.name,
                            nickname = user.nickname
                        };
                    }
                    else
                    {
                        Console.WriteLine("Credenciales incorrectas");
                        return null;
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                return null;
            }
        }
        private string HassPassword(string password)
        {
            using(SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
