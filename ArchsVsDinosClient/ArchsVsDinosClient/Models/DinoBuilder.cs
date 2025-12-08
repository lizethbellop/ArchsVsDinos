using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    public class DinoBuilder
    {

        public Card Head { get; set; }
        public Card Chest { get; set; }
        public Card LeftArm { get; set; }
        public Card RightArm { get; set; }
        public Card Legs { get; set; }

        public bool HasHead => Head != null;
        public bool HasChest => Chest != null;

        public bool CanAcceptLimbs()
        {
            if (!HasChest)
            {
                return false;
            }
            return Chest.ChestSubtype != ChestSubtype.Complete;
        }

        public bool CanAcceptLegs()
        {
            if (!HasChest)
            {
                return false;
            }
            return Chest.ChestSubtype == ChestSubtype.ArmsLegs;
        }

    }
}
