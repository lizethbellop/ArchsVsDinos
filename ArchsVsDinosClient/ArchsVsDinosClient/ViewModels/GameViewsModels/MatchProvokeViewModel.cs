using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class MatchProvokeViewModel : INotifyPropertyChanged
    {
        private readonly GameBoardManager boardManager;
        private readonly Dictionary<int, string> playerNames;
        private readonly int myUserId;
        private readonly ArmyType selectedArmyType;

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Card> SelectedArmy { get; } = new ObservableCollection<Card>();
        public string SelectedArmyName { get; private set; }
        public int SelectedArmyPower => SelectedArmy.Sum(c => c.Power);
        public List<PlayerDinosViewModel> PlayersDinos { get; } = new List<PlayerDinosViewModel>();
        public int MaxPlayerPower { get; private set; }
        public string MaxPowerPlayerName { get; private set; }

        public MatchProvokeViewModel(
            GameBoardManager boardManager,
            Dictionary<int, string> playerNames,
            int myUserId,
            ArmyType selectedArmyType)
        {
            this.boardManager = boardManager;
            this.playerNames = playerNames;
            this.myUserId = myUserId;
            this.selectedArmyType = selectedArmyType;

            LoadSelectedArmy();
            LoadPlayersDinos();
            CalculateMaxPower();
        }

        private void LoadSelectedArmy()
        {
            SelectedArmy.Clear();

            switch (selectedArmyType)
            {
                case ArmyType.Sand:
                    SelectedArmyName = "SAND ARMY";
                    System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Loading Sand Army: {boardManager.SandArmy.Count} cards");
                    foreach (var card in boardManager.SandArmy)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Adding Sand Arch: {card.IdCard}");
                        SelectedArmy.Add(card); }
                    break;

                case ArmyType.Water:
                    SelectedArmyName = "WATER ARMY";
                    System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Loading Water Army: {boardManager.WaterArmy.Count} cards");
                    foreach (var card in boardManager.WaterArmy)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Adding Water Arch: {card.IdCard}");
                        SelectedArmy.Add(card);
                    }            
                    break;

                case ArmyType.Wind:
                    SelectedArmyName = "WIND ARMY";
                    System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Loading Wind Army: {boardManager.WindArmy.Count} cards");
                    foreach (var card in boardManager.WindArmy)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Adding Wind Arch: {card.IdCard}");
                        SelectedArmy.Add(card);
                    }
                    break;
            }
            System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Final SelectedArmy count: {SelectedArmy.Count}");

            OnPropertyChanged(nameof(SelectedArmyName));
            OnPropertyChanged(nameof(SelectedArmyPower));
        }

        private void LoadPlayersDinos()
        {
            PlayersDinos.Clear();

            foreach (var playerName in playerNames)
            {
                int userId = playerName.Key;
                string name = playerName.Value;

                Dictionary<int, DinoBuilder> dinos = null;
                if (boardManager.PlayerDecks.ContainsKey(userId))
                {
                    dinos = boardManager.PlayerDecks[userId];
                }
                else
                {
                    dinos = new Dictionary<int, DinoBuilder>();
                }

                int totalPower = CalculateTotalPowerForElement(dinos, selectedArmyType);

                var playerDinosVM = new PlayerDinosViewModel
                {
                    UserId = userId,
                    PlayerName = name,
                    TotalPower = totalPower,
                    Dinos = dinos,
                    Element = selectedArmyType
                };

                PlayersDinos.Add(playerDinosVM);
            }

            System.Diagnostics.Debug.WriteLine($"[PROVOKE VM] Total players loaded: {PlayersDinos.Count}");
        }

        private int CalculateTotalPowerForElement(Dictionary<int, DinoBuilder> dinos, ArmyType element)
        {
            int total = 0;

            foreach (var dino in dinos.Values)
            {
                if (dino.Head != null && GetElementFromCard(dino.Head) == element)
                {
                    if (dino.Head != null) total += dino.Head.Power;
                    if (dino.Chest != null) total += dino.Chest.Power;
                    if (dino.LeftArm != null) total += dino.LeftArm.Power;
                    if (dino.RightArm != null) total += dino.RightArm.Power;
                    if (dino.Legs != null) total += dino.Legs.Power;
                }
            }

            return total;
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

        private void CalculateMaxPower()
        {
            if (PlayersDinos.Count == 0)
            {
                MaxPlayerPower = 0;
                MaxPowerPlayerName = string.Empty;
                return;
            }

            var maxPlayer = PlayersDinos.OrderByDescending(p => p.TotalPower).First();
            MaxPlayerPower = maxPlayer.TotalPower;
            MaxPowerPlayerName = maxPlayer.PlayerName;

            OnPropertyChanged(nameof(MaxPlayerPower));
            OnPropertyChanged(nameof(MaxPowerPlayerName));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PlayerDinosViewModel
    {
        public int UserId { get; set; }
        public string PlayerName { get; set; }
        public int TotalPower { get; set; }
        public Dictionary<int, DinoBuilder> Dinos { get; set; }
        public ArmyType Element { get; set; }
    }
}