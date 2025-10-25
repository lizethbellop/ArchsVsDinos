using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO.Response
{
    public class LoginResponse
    {
        public bool success { get; set; }
        public UserDTO userSession { get; set; }
        public PlayerDTO associatedPlayer { get; set; }
    }
}
