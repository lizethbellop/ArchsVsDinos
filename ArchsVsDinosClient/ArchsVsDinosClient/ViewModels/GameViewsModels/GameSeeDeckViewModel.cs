using ArchsVsDinosClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameSeeDeckViewModel
    {
        private string playerName;
        private int playerPoints;

        public event PropertyChangedEventHandler PropertyChanged;

        public string PlayerName
        {
            get => playerName;
            set
            {
                playerName = value;
                OnPropertyChanged(nameof(PlayerName));
            }
        }

        public int PlayerPoints
        {
            get => playerPoints;
            set
            {
                playerPoints = value;
                OnPropertyChanged(nameof(PlayerPoints));
            }
        }

        public List<DinoSlotViewModel> DinoSlots { get; } = new List<DinoSlotViewModel>
        {
            new DinoSlotViewModel { SlotNumber = 1 },
            new DinoSlotViewModel { SlotNumber = 2 },
            new DinoSlotViewModel { SlotNumber = 3 },
            new DinoSlotViewModel { SlotNumber = 4 },
            new DinoSlotViewModel { SlotNumber = 5 },
            new DinoSlotViewModel { SlotNumber = 6 }
        };

        public GameSeeDeckViewModel()
        {
        }

        public void LoadPlayerDeck(string playerName, int points, Dictionary<int, DinoBuilder> playerDeck)
        {
            PlayerName = playerName;
            PlayerPoints = points;

            foreach (var slot in DinoSlots)
            {
                slot.Clear();
            }

            int slotIndex = 0;
            foreach (var dinoPair in playerDeck)
            {
                if (slotIndex >= DinoSlots.Count) break;

                var dino = dinoPair.Value;
                DinoSlots[slotIndex].LoadDino(dino);
                slotIndex++;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DinoSlotViewModel : INotifyPropertyChanged
    {
        private Card head;
        private Card chest;
        private Card leftArm;
        private Card rightArm;
        private Card legs;
        private int totalPower;

        public event PropertyChangedEventHandler PropertyChanged;

        public int SlotNumber { get; set; }

        public Card Head
        {
            get => head;
            set
            {
                head = value;
                OnPropertyChanged(nameof(Head));
                UpdateTotalPower();
            }
        }

        public Card Chest
        {
            get => chest;
            set
            {
                chest = value;
                OnPropertyChanged(nameof(Chest));
                UpdateTotalPower();
            }
        }

        public Card LeftArm
        {
            get => leftArm;
            set
            {
                leftArm = value;
                OnPropertyChanged(nameof(LeftArm));
                UpdateTotalPower();
            }
        }

        public Card RightArm
        {
            get => rightArm;
            set
            {
                rightArm = value;
                OnPropertyChanged(nameof(RightArm));
                UpdateTotalPower();
            }
        }

        public Card Legs
        {
            get => legs;
            set
            {
                legs = value;
                OnPropertyChanged(nameof(Legs));
                UpdateTotalPower();
            }
        }

        public int TotalPower
        {
            get => totalPower;
            private set
            {
                totalPower = value;
                OnPropertyChanged(nameof(TotalPower));
            }
        }

        public bool HasCards => Head != null || Chest != null || LeftArm != null || RightArm != null || Legs != null;

        public void LoadDino(DinoBuilder dino)
        {
            if (dino == null)
            {
                Clear();
                return;
            }

            Head = dino.Head;
            Chest = dino.Chest;
            LeftArm = dino.LeftArm;
            RightArm = dino.RightArm;
            Legs = dino.Legs;
        }

        public void Clear()
        {
            Head = null;
            Chest = null;
            LeftArm = null;
            RightArm = null;
            Legs = null;
        }

        private void UpdateTotalPower()
        {
            int power = 0;
            if (Head != null) power += Head.Power;
            if (Chest != null) power += Chest.Power;
            if (LeftArm != null) power += LeftArm.Power;
            if (RightArm != null) power += RightArm.Power;
            if (Legs != null) power += Legs.Power;

            TotalPower = power;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
