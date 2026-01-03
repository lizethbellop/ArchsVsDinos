using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameBoardManager : INotifyPropertyChanged
    {
        public ObservableCollection<Card> PlayerHand { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> SandArmy { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> WaterArmy { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> WindArmy { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> DiscardPile { get; } = new ObservableCollection<Card>();

        public Dictionary<int, Dictionary<int, DinoBuilder>> PlayerDecks { get; } = new Dictionary<int, Dictionary<int, DinoBuilder>>();

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility SandArmyVisibility => SandArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WaterArmyVisibility => WaterArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WindArmyVisibility => WindArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public void InitializePlayerDecks(List<int> playerUserIds)
        {
            foreach (var userId in playerUserIds)
            {
                if (!PlayerDecks.ContainsKey(userId))
                {
                    PlayerDecks[userId] = new Dictionary<int, DinoBuilder>();
                }
            }
        }

        public void RegisterDinoHeadPlayed(int userId, int dinoInstanceId, Card headCard)
        {
            if (!PlayerDecks.ContainsKey(userId))
            {
                PlayerDecks[userId] = new Dictionary<int, DinoBuilder>();
            }

            if (!PlayerDecks[userId].ContainsKey(dinoInstanceId))
            {
                PlayerDecks[userId][dinoInstanceId] = new DinoBuilder();
            }

            PlayerDecks[userId][dinoInstanceId].Head = headCard;
            System.Diagnostics.Debug.WriteLine($"[BOARD] Player {userId} - Dino {dinoInstanceId} - Head registered: {headCard.IdCard}");
        }

        public void RegisterBodyPartAttached(int userId, int dinoInstanceId, Card bodyCard)
        {
            if (!PlayerDecks.ContainsKey(userId))
            {
                return;
            }

            if (!PlayerDecks[userId].ContainsKey(dinoInstanceId))
            {
                return;
            }

            var dino = PlayerDecks[userId][dinoInstanceId];

            switch (bodyCard.BodyPartType)
            {
                case BodyPartType.Chest:
                    dino.Chest = bodyCard;
                    break;
                case BodyPartType.LeftArm:
                    dino.LeftArm = bodyCard;
                    break;
                case BodyPartType.RightArm:
                    dino.RightArm = bodyCard;
                    break;
                case BodyPartType.Legs:
                    dino.Legs = bodyCard;
                    break;
            }
        }

        public Dictionary<int, DinoBuilder> GetPlayerDeck(int userId)
        {
            if (PlayerDecks.ContainsKey(userId))
            {
                return PlayerDecks[userId];
            }
            return new Dictionary<int, DinoBuilder>();
        }

        public void UpdatePlayerHand(List<ArchsVsDinosClient.GameService.PlayerHandDTO> playersHands, int myUserId)
        {
            if (playersHands == null) return;

            var myHand = System.Linq.Enumerable.FirstOrDefault(playersHands, hand => hand.UserId == myUserId);

            if (myHand != null && myHand.Cards != null)
            {
                PlayerHand.Clear();

                foreach (var cardDTO in myHand.Cards)
                {
                    var cardModel = CardRepositoryModel.GetById(cardDTO.IdCard);

                    if (cardModel != null)
                    {
                        PlayerHand.Add(cardModel);
                    }
                    else
                    {
                        var exists = CardRepositoryModel.Cards.Any(card => card.IdCard == cardDTO.IdCard);
                    }
                }
            }
        }

        public void UpdateInitialBoard(ArchsVsDinosClient.GameService.CentralBoardDTO initialBoard)
        {
            if (initialBoard == null) return;

            SandArmy.Clear();
            WaterArmy.Clear();
            WindArmy.Clear();

            AddCardsToArmy(initialBoard.SandArmy, SandArmy);
            AddCardsToArmy(initialBoard.WaterArmy, WaterArmy);
            AddCardsToArmy(initialBoard.WindArmy, WindArmy);

            NotifyVisibilityChanges();
        }

        private void AddCardsToArmy(ArchsVsDinosClient.GameService.CardDTO[] dtoArray, ObservableCollection<Card> targetCollection)
        {
            if (dtoArray == null) return;
            foreach (var dto in dtoArray)
            {
                var card = CardRepositoryModel.GetById(dto.IdCard);
                if (card != null) targetCollection.Add(card);
            }
        }

        private void NotifyVisibilityChanges()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SandArmyVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WaterArmyVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindArmyVisibility)));
        }

        public void ClearPlayerDinosByElement(int userId, ArmyType element)
        {
            if (!PlayerDecks.ContainsKey(userId))
                return;

            var playerDinos = PlayerDecks[userId];
            var dinosToRemove = new List<int>();

            foreach (var dinoPair in playerDinos)
            {
                var dino = dinoPair.Value;

                if (dino.Head != null && GetElementFromCard(dino.Head) == element)
                {
                    dinosToRemove.Add(dinoPair.Key);
                }
            }

            foreach (var dinoId in dinosToRemove)
            {
                playerDinos.Remove(dinoId);
            }

            System.Diagnostics.Debug.WriteLine($"[BOARD MANAGER] Cleared {dinosToRemove.Count} {element} dinos for player {userId}");
        }

        private ArmyType GetElementFromCard(Card card)
        {
            switch (card.Element)
            {
                case ElementType.Sand:
                    return ArmyType.Sand;
                case ElementType.Water:
                    return ArmyType.Water;
                case ElementType.Wind:
                    return ArmyType.Wind;
                default:
                    return ArmyType.None;
            }
        }
    }
}