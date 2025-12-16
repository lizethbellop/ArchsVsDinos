using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class GameFault
    {
        [DataMember]
        public string Code { get; set; } 

        [DataMember]
        public string Detail { get; set; }
    }

}
