using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO.Response
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public UserDTO UserSession { get; set; }
        public PlayerDTO AssociatedPlayer { get; set; }
    }
}
