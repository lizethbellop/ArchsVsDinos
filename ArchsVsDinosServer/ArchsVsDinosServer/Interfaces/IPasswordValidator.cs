using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Utils;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IPasswordValidator
    {
        ValidationResult ValidatePassword(string password);
        bool ContainsSpecialCharacter(string password);
    }
}
