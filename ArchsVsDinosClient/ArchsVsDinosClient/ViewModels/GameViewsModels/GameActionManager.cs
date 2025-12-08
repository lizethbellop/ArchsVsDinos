using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameActionManager
    {
        private readonly Dictionary<string, DinoBuilder> dinoSlots = new Dictionary<string, DinoBuilder>();

        public GameActionManager()
        {
            for (int i = 1; i <= 6; i++)
            {
                dinoSlots[$"IdCombinationCell_{i}"] = new DinoBuilder();
            }
        }

        public string ValidateDrop(Card card, string cellId, int remainingMoves, bool isMyTurn)
        {
            if (!isMyTurn)
            {
                return Lang.Match_NotYourTurn;
            }

            if (!dinoSlots.ContainsKey(cellId))
            {
                return Lang.Match_InvalidCell;
            }

            var dino = dinoSlots[cellId];

            if (card.Category == CardCategory.DinoHead)
            {
                if (dino.HasHead)
                {
                    return Lang.Match_AlreadyHeadInSpace; 
                }
                return null; 
            }

            if (!dino.HasHead)
            {
                return Lang.Match_CellNeedDinoHead;
            }

            switch (card.Category)
            {
                case CardCategory.BodyPart:
                    return ValidateBodyPart(card, dino);

                default:
                    return Lang.Match_CardCannotPlacedHere;
            }
        }

        private string ValidateBodyPart(Card card, DinoBuilder dino)
        {
            if (card.BodyPartType == BodyPartType.Chest)
            {
                if (dino.HasChest)
                {
                    return Lang.Match_DinosaurAlreadyHasChest;
                }
                return null; 
            }

            if (!dino.HasChest)
            {
                return Lang.Match_AttachChestBeforeLimbs;
            }

            switch (card.BodyPartType)
            {
                case BodyPartType.LeftArm:
                    if (dino.LeftArm != null)
                    {
                        return Lang.Match_DinosaurAlreadyHasLeftArm;
                    }
                    if (!dino.CanAcceptLimbs())
                    {
                        return Lang.Match_ChestNotSupportArms;
                    }
                    break;

                case BodyPartType.RightArm:
                    if (dino.RightArm != null)
                    {
                        return Lang.Match_DinosaurAlreadyHasRightArm;
                    }
                    if (!dino.CanAcceptLimbs())
                    {
                        return Lang.Match_ChestNotSupportArms;
                    }
                    break;

                case BodyPartType.Legs:
                    if (dino.Legs != null)
                    {
                        return Lang.Match_DinosaurAlreadyHasLegs;
                    }
                    if (!dino.CanAcceptLegs())
                    {
                        return Lang.Match_ChestNotSupportLegs;
                    }
                    break;
            }

            return null; 
        }

        public void RegisterSuccessfulMove(Card card, string cellId)
        {
            if (dinoSlots.ContainsKey(cellId))
            {
                var dino = dinoSlots[cellId];

                switch (card.Category)
                {
                    case CardCategory.DinoHead:
                        dino.Head = card;
                        break;

                    case CardCategory.BodyPart:
                        switch (card.BodyPartType)
                        {
                            case BodyPartType.Chest:
                                dino.Chest = card;
                                break;
                            case BodyPartType.LeftArm:
                                dino.LeftArm = card;
                                break;
                            case BodyPartType.RightArm:
                                dino.RightArm = card;
                                break;
                            case BodyPartType.Legs:
                                dino.Legs = card;
                                break;
                        }
                        break;
                }
            }
        }
    }
}
