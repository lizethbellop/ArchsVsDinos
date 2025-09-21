using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class ValidationHelper
    {

        public static bool isEmpty(string input)
        {
            return string.IsNullOrEmpty(input);
        }
    }
}
