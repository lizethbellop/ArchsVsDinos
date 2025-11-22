using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class CodeGenerator
    {
        private static readonly Random random = new Random();
        public static string GenerateCode(int length)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(Chars[random.Next(Chars.Length)]);
            }

            return stringBuilder.ToString();
        }

        public static string GenerateVerificationCode()
        {
            return GenerateCode(6);
        }

        public static string GenerateMatchCode()
        {
            return GenerateCode(5);
        }

    }
}
