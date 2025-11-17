using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class UnregisteredPlayerGenerator
    {
        private static readonly Random random = new Random();
        public static string GenerateIdUnregisteredPlayer()
        {
            const int length = 5;
            const string Chars = "123456789";

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(Chars[random.Next(Chars.Length)]);
            }

            return "D" + stringBuilder.ToString();
        }

        public static string GenerateNicknameUnregisteredPlayer()
        {

            const int length = 5;
            const string Chars = "123456789";

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(Chars[random.Next(Chars.Length)]);
            }

            return "Dino" + stringBuilder.ToString();
        }


    }
}
