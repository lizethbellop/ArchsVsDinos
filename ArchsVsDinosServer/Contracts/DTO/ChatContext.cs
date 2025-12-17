using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public enum ChatContext
    {
        [EnumMember]
        Lobby,

        [EnumMember]
        InGame
    }

}
