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
        public int IdConfiguration { get; set; }
        [DataMember]
        public int MusicVolume { get; set; }
        [DataMember]
        public int SoundVolume { get; set; }
    }
}
