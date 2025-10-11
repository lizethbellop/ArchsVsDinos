using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class ValidationHelper
    {
        public static bool IsEmpty(string value)
        {
            if (string.IsNullOrEmpty(value)) { return true; } return false;
        }
    }
}
