using ArchsVsDinosServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PRUEBAS DE ENTITY FRAMEWORK ===");

            var authService = new AuthenticationService();

            while (true)
            {
                Console.WriteLine("\n--- MENÚ ---");
                Console.WriteLine("1. Registrar usuario");
                Console.WriteLine("2. Hacer login");
                Console.WriteLine("3. Salir");
                Console.Write("Opción: ");

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        TestRegister(authService);
                        break;
                    case "2":
                        TestLogin(authService);
                        break;
                    case "3":
                        return;
                        break;
                    default:
                        Console.WriteLine("Opción inválida");
                        break;
                }
            }

            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }

        static void TestRegister(AuthenticationService authService)
        {
            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Nickname: ");
            string nickname = Console.ReadLine();

            bool success = authService.RegisterUser(username, password,
                email, name, nickname);

            Console.WriteLine(success ? "✓ Registro exitoso" : "✗ Error en registro");
        }

        static void TestLogin(AuthenticationService authService)
        {
            
            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            var user = authService.LoginUser(username, password);

            if (user != null)
            {
                Console.WriteLine($"✓ Login exitoso - Bienvenido {user.username}!");
                Console.WriteLine($"   ID: {user.idUser}");
            }
            else
            {
                Console.WriteLine("✗ Login fallido");
            }

        }
    }
}
