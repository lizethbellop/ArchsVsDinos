using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class UserDTO
    {
        public int idUser { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public string nickname { get; set; }
        public string email { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (UserDTO)obj;
            return idUser == other.idUser &&
                   username == other.username &&
                   name == other.name &&
                   nickname == other.nickname &&
                   email == other.email;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + idUser.GetHashCode();
                hash = hash * 23 + (username?.GetHashCode() ?? 0);
                hash = hash * 23 + (name?.GetHashCode() ?? 0);
                hash = hash * 23 + (nickname?.GetHashCode() ?? 0);
                hash = hash * 23 + (email?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
