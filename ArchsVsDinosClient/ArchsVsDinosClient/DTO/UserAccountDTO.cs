using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    public class UserAccountDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public int IdConfiguration { get; set; }
        public int IdPlayer { get; set; }
    }
}
