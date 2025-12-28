 // Para CentralBoardDTO
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

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility SandArmyVisibility => SandArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WaterArmyVisibility => WaterArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WindArmyVisibility => WindArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public void UpdatePlayerHand(List<ArchsVsDinosClient.GameService.PlayerHandDTO> playersHands, int myUserId)
        {
            if (playersHands == null) return;

            var myHand = System.Linq.Enumerable.FirstOrDefault(playersHands, h => h.UserId == myUserId);

            if (myHand != null && myHand.Cards != null)
            {
                PlayerHand.Clear();

                System.Diagnostics.Debug.WriteLine($"[HAND] Received {myHand.Cards.Length} cards for user {myUserId}");

                foreach (var cardDTO in myHand.Cards)
                {
                    var cardModel = CardRepositoryModel.GetById(cardDTO.IdCard);

                    if (cardModel != null)
                    {
                        PlayerHand.Add(cardModel);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HAND] ❌ Card {cardDTO.IdCard} NOT FOUND - Repository has {CardRepositoryModel.Cards.Count} cards");
                        var exists = CardRepositoryModel.Cards.Any(c => c.IdCard == cardDTO.IdCard);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[HAND] Total added to PlayerHand: {PlayerHand.Count}");
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
    }
}