using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class VerificationCode
    {
        public string email { get; set; }
        public string code { get; set; }
        public DateTime expiration { get; set; }
    }

}
