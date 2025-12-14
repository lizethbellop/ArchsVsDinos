using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public static class SafeCodeGenerator
    {
        private static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        public static int GenerateInt(int minimumValue, int maximumValue)
        {
            if(minimumValue >= maximumValue)
            {
                throw new ArgumentException("Minimum value cannot be greater nor same than maximum value.");
            }

            long diff = (long)maximumValue - (long)minimumValue;
            byte[] uint32Buffer = new byte[4];

            while (true)
            {
                rng.GetBytes(uint32Buffer);
                uint rand = BitConverter.ToUInt32(uint32Buffer, 0);
                
                long max = (1 + (long)uint.MaxValue);
                long remainder = max % diff;
                
                if (rand < max - remainder)
                {
                    return (int)(minimumValue + (rand % diff));
                }
            }

        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(length);
            byte[] uint32Buffer = new byte[4];
            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(uint32Buffer);
                uint rand = BitConverter.ToUInt32(uint32Buffer, 0);
                result.Append(chars[(int)(rand % (uint)chars.Length)]);
            }
            return result.ToString();
        }
    }
}
