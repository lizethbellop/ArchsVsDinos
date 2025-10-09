using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class ConfigurationDTO
    {
        [DataMember]
        public int idConfiguration { get; set; }
        [DataMember]
        public int musicVolume { get; set; }
        [DataMember]
        public int soundVolume { get; set; }
    }
}
