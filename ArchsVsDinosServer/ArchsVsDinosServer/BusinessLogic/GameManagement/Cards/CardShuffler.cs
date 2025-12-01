using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public static class CardShuffler
    {

        public static List<int> ShuffleCards(List<int> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return new List<int>();
            }

            var shuffled = new List<int>(cardIds);
            var n = shuffled.Count;

            using (var rng = RandomNumberGenerator.Create())
            {
                for (var i = n - 1; i > 0; i--)
                {
                    var j = GetSecureRandomNumber(rng, i + 1);
                    var temp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = temp;
                }
            }

            return shuffled;
        }

        private static int GetSecureRandomNumber(RandomNumberGenerator rng, int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }

            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            return (int)(value % (uint)maxValue);
        }

    }
}
