using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class UserDTO
    {
        public int IdUser { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Email { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (UserDTO)obj;
            return IdUser == other.IdUser &&
                   Username == other.Username &&
                   Name == other.Name &&
                   Nickname == other.Nickname &&
                   Email == other.Email;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + IdUser.GetHashCode();
                hash = hash * 23 + (Username?.GetHashCode() ?? 0);
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + (Nickname?.GetHashCode() ?? 0);
                hash = hash * 23 + (Email?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
