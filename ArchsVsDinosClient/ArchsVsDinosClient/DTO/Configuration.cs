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
        public int IdConfiguration { get; set; }
        public int MusicVolume { get; set; }
        public int SoundVolume { get; set; }
    }
}
