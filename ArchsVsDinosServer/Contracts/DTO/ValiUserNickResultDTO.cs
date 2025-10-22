using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class ValiUserNickResultDTO
    {

        [DataMember]
        public bool isValid {  get; set; }


        [DataMember]
        public ReturnContent ReturnCont { get; set; }

    }

    [DataContract]
    public enum ReturnContent
    {
        [EnumMember] Success,
        [EnumMember] UsernameExists,
        [EnumMember] NicknameExists,
        [EnumMember] BothExists,
        [EnumMember] DatabaseError
    }

}
