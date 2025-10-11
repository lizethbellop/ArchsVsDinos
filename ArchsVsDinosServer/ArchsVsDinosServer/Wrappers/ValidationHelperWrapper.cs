using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class ValidationHelperWrapper : IValidationHelper
    {
        public bool IsEmpty(string value)
        {
            return ValidationHelper.IsEmpty(value);
        }
    }
}
