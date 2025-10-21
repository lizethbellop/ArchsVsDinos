using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    internal class Configuration
    {
        public int idConfiguration { get; set; }
        public int musicVolume { get; set; }
        public int soundVolume { get; set; }
    }
}
