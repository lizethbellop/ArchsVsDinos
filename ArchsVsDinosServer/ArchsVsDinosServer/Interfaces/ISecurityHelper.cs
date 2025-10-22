using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface ISecurityHelper
    {
        string HashPassword(string password);
        bool VerifyPassword(string plainPassword, string hashedPasswordFromDB);
    }

    }
