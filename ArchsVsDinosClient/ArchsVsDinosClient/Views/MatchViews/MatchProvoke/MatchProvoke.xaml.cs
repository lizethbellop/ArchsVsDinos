using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.Views.MatchViews.MatchProvoke
{
    public partial class MatchProvoke : Window
    {
        private readonly MatchProvokeViewModel viewModel;
        private int maxPlayerPower = 0;
        private int totalArmyPower = 0;
        private int winnerUserId = 0;

        private Label lblPlayer1Name;
        private Label lblPlayer1Power;
        private Label lblPlayer2Name;
        private Label lblPlayer2Power;
        private Label lblPlayer3Name;
        private Label lblPlayer3Power;
        private Label lblPlayer4Name;
        private Label lblPlayer4Power;
        private Label lblArmyPowerTotal;

        private Grid gridPlayer1;
        private Grid gridPlayer2;
        private Grid gridPlayer3;
        private Grid gridPlayer4;

        public MatchProvoke(MatchProvokeViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;

            MusicPlayer.Instance.StopBackgroundMusic();
            MusicPlayer.Instance.PlayBackgroundMusic(MusicTracks.Battle);

            InitializeReferences();
            LoadPlayersDinos();
            LoadArchCards();
            HideInactivePlayers();

            Loaded += (s, e) => StartBattleSequence();
        }

        private void InitializeReferences()
        {
            gridPlayer1 = Gr_Player1Info;
            gridPlayer2 = Gr_Player2Info;
            gridPlayer3 = Gr_Player3Info;
            gridPlayer4 = Gr_Player4Info;

            lblPlayer1Name = Lb_Player1Name;
            lblPlayer1Power = Lb_Player1Power;

            lblPlayer2Name = Lb_Player2Name;
            lblPlayer2Power = Lb_Player2Power;

            lblPlayer3Name = Lb_Player3Name;
            lblPlayer3Power = Lb_Player3Power;

            lblPlayer4Name = Lb_Player4Name;
            lblPlayer4Power = Lb_Player4Power;

            lblArmyPowerTotal = Lb_ArmyPowerTotal;

            lblPlayer1Power.Content = "0";
            lblPlayer2Power.Content = "0";
            lblPlayer3Power.Content = "0";
            lblPlayer4Power.Content = "0";
            lblArmyPowerTotal.Content = "0";
        }

        private void LoadPlayersDinos()
        {
            var players = viewModel.PlayersDinos.Take(4).ToList();

            ClearPlayerDeck(P1Deck);
            ClearPlayerDeck(P2Deck);
            ClearPlayerDeck(P3Deck);
            ClearPlayerDeck(P4Deck);

            if (players.Count > 0)
            {
                LoadPlayerDeck(P1Deck, players[0]);
                lblPlayer1Name.Content = players[0].PlayerName;
            }

            if (players.Count > 1)
            {
                LoadPlayerDeck(P2Deck, players[1]);
                lblPlayer2Name.Content = players[1].PlayerName;
            }

            if (players.Count > 2)
            {
                LoadPlayerDeck(P3Deck, players[2]);
                lblPlayer3Name.Content = players[2].PlayerName;
            }

            if (players.Count > 3)
            {
                LoadPlayerDeck(P4Deck, players[3]);
                lblPlayer4Name.Content = players[3].PlayerName;
            }
        }

        private void ClearPlayerDeck(PlayerCell playerCell)
        {
            for (int i = 1; i <= 6; i++)
            {
                var cell = playerCell.GetCombinationCell(i);
                if (cell != null) ClearCell(cell);
            }
        }

        private void ClearCell(CardCell cell)
        {
            cell.Part_Head.Background = Brushes.Transparent;
            cell.Part_Chest.Background = Brushes.Transparent;
            cell.Part_LeftArm.Background = Brushes.Transparent;
            cell.Part_RightArm.Background = Brushes.Transparent;
            cell.Part_Legs.Background = Brushes.Transparent;
        }

        private void LoadPlayerDeck(PlayerCell playerCell, PlayerDinosViewModel player)
        {
            int slotIndex = 1;

            foreach (var dinoPair in player.Dinos)
            {
                if (slotIndex > 6) break;

                var dino = dinoPair.Value;

                if (dino.Head != null && GetElementFromCard(dino.Head) == player.Element)
                {
                    var cell = playerCell.GetCombinationCell(slotIndex);
                    if (cell != null)
                    {
                        PlaceCardsInCell(cell, dino);
                        slotIndex++;
                    }
                }
            }
        }

        private void LoadArchCards()
        {
            var archCards = viewModel.SelectedArmy.ToList();

            if (archCards.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[PROVOKE] No Arch cards to display");
                return;
            }

            var archsGrid = (Grid)this.FindName("Gr_ArchsRow");

            if (archsGrid == null)
            {
                var mainGrid = (Grid)this.Content;
                foreach (var child in mainGrid.Children)
                {
                    if (child is Grid grid && Grid.GetRow(grid) == 1)
                    {
                        archsGrid = grid;
                        break;
                    }
                }
            }

            if (archsGrid == null)
            {
                System.Diagnostics.Debug.WriteLine("[PROVOKE] Could not find Archs grid");
                return;
            }

            int columnIndex = 0;
            foreach (var archCard in archCards)
            {
                if (columnIndex >= 9) break;

                var targetColumn = columnIndex * 2; 

                Grid archCardGrid = null;
                foreach (var child in archsGrid.Children)
                {
                    if (child is Grid grid && Grid.GetColumn(grid) == targetColumn)
                    {
                        archCardGrid = grid;
                        break;
                    }
                }

                if (archCardGrid != null)
                {
                    archCardGrid.Children.Clear();
                    archCardGrid.Background = null;
                    archCardGrid.Opacity = 1.0;

                    var cardImage = new Image
                    {
                        Source = new BitmapImage(new Uri(archCard.CardRoute)),
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    archCardGrid.Children.Add(cardImage);

                }

                columnIndex++;
            }

            System.Diagnostics.Debug.WriteLine($"[PROVOKE] Loaded {columnIndex} Arch cards");
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

        private void PlaceCardsInCell(CardCell cell, DinoBuilder dino)
        {
            if (dino.Head != null) cell.Part_Head.Background = CreateCardBrush(dino.Head);
            if (dino.Chest != null) cell.Part_Chest.Background = CreateCardBrush(dino.Chest);
            if (dino.LeftArm != null) cell.Part_LeftArm.Background = CreateCardBrush(dino.LeftArm);
            if (dino.RightArm != null) cell.Part_RightArm.Background = CreateCardBrush(dino.RightArm);
            if (dino.Legs != null) cell.Part_Legs.Background = CreateCardBrush(dino.Legs);
        }

        private ImageBrush CreateCardBrush(Card card)
        {
            try
            {
                return new ImageBrush(new BitmapImage(new Uri(card.CardRoute)));
            }
            catch
            {
                return new ImageBrush();
            }
        }

        private void StartBattleSequence()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            int step = 0;

            timer.Tick += (s, e) =>
            {
                step++;

                switch (step)
                {
                    case 1: 
                        AnimatePlayerDeck(P1Deck, gridPlayer1, lblPlayer1Power, 0);
                        break;

                    case 2: 
                        AnimatePlayerDeck(P2Deck, gridPlayer2, lblPlayer2Power, 1);
                        break;

                    case 3: 
                        AnimatePlayerDeck(P3Deck, gridPlayer3, lblPlayer3Power, 2);
                        break;

                    case 4:
                        AnimatePlayerDeck(P4Deck, gridPlayer4, lblPlayer4Power, 3);
                        break;

                    case 5: 
                        ShowMaxPower();
                        break;

                    case 6: 
                        RevealArmyCards();
                        break;

                    case 9: 
                        ShowBattleResult();
                        timer.Stop();
                        break;
                }
            };

            timer.Start();
        }

        private void AnimatePlayerDeck(PlayerCell deck, Grid playerGrid, Label powerLabel, int playerIndex)
        {
            AddGlowEffect(deck, Colors.Gold);
            AddGlowEffect(playerGrid, Colors.Gold);

            var players = viewModel.PlayersDinos.ToList();
            if (playerIndex < players.Count)
            {
                int targetPower = players[playerIndex].TotalPower;
                AnimateCounter(powerLabel, 0, targetPower, 1.0);

                if (targetPower > maxPlayerPower)
                {
                    maxPlayerPower = targetPower;
                    winnerUserId = players[playerIndex].UserId;
                }
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                RemoveGlowEffect(deck);
                RemoveGlowEffect(playerGrid);
                timer.Stop();
            };
            timer.Start();
        }

        private void ShowMaxPower()
        {
            var maxPlayer = viewModel.PlayersDinos.FirstOrDefault(player => player.UserId == winnerUserId);

            if (maxPlayer != null)
            {
                Grid championGrid = null;
                PlayerCell championDeck = null;

                var players = viewModel.PlayersDinos.ToList();
                int index = players.IndexOf(maxPlayer);

                switch (index)
                {
                    case 0:
                        championGrid = gridPlayer1;
                        championDeck = P1Deck;
                        break;
                    case 1:
                        championGrid = gridPlayer2;
                        championDeck = P2Deck;
                        break;
                    case 2:
                        championGrid = gridPlayer3;
                        championDeck = P3Deck;
                        break;
                    case 3:
                        championGrid = gridPlayer4;
                        championDeck = P4Deck;
                        break;
                }

                if (championGrid != null && championDeck != null)
                {
                    AddGlowEffect(championGrid, Colors.Purple);
                    AddGlowEffect(championDeck, Colors.Purple);
                }

            }
        }

        private void RevealArmyCards()
        {
            totalArmyPower = viewModel.SelectedArmyPower;

            AnimateCounter(lblArmyPowerTotal, 0, totalArmyPower, 2.0);
        }

        private void ShowBattleResult()
        {
            bool playersWin = maxPlayerPower >= totalArmyPower;
            int difference = Math.Abs(maxPlayerPower - totalArmyPower);

            string resultMessage;

            var maxPlayer = viewModel.PlayersDinos.FirstOrDefault(p => p.UserId == winnerUserId);

            if (maxPlayer == null)
            {

                resultMessage = $"{Lang.Match_ProvokeDefeatMessage}\n\n" +
                               $"{Lang.Match_ProvokeDefeatMessage2} {totalArmyPower} {Lang.Match_ProvokeDefeatMessage3}\n\n" +
                               $"{Lang.Match_ProvokeDefeatMessage6} {viewModel.SelectedArmyName}";

                MessageBox.Show(resultMessage, Lang.Match_ProvokeBattleResultTitle);
                DialogResult = true;
                Close();
                return;
            }

            string championName = maxPlayer.PlayerName;

            if (playersWin)
            {
                resultMessage = $"{Lang.Match_ProvokeVictoryMessage}\n\n" +
                                $"{championName} {Lang.Match_ProvokeVictoryMessage2} {maxPlayerPower} {Lang.Match_ProvokeVictoryMessage3}\n" +
                                $"{Lang.Match_ProvokeVictoryMessage4} {totalArmyPower} {Lang.Match_ProvokeVictoryMessage5}";
            }
            else
            {
                resultMessage = $"{Lang.Match_ProvokeDefeatMessage}\n\n" +
                                $"{Lang.Match_ProvokeDefeatMessage2} {totalArmyPower} {Lang.Match_ProvokeDefeatMessage3}\n\n" +
                                $"{Lang.Match_ProvokeDefeatMessage6} {viewModel.SelectedArmyName}";
            }

            MessageBox.Show(resultMessage, Lang.Match_ProvokeBattleResultTitle);

            DialogResult = true;
            Close();
        }

        private void AnimateCounter(Label label, int from, int to, double durationSeconds)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            int current = from;
            int increment = (int)Math.Ceiling((to - from) / (durationSeconds * 20));

            if (increment == 0) increment = 1;

            timer.Tick += (s, e) =>
            {
                current += increment;
                if (current >= to)
                {
                    current = to;
                    timer.Stop();
                }

                label.Content = current.ToString();
            };

            timer.Start();
        }

        private void AddGlowEffect(UIElement element, Color color)
        {
            element.Effect = new DropShadowEffect
            {
                Color = color,
                ShadowDepth = 0,
                BlurRadius = 40,
                Opacity = 1
            };

            var pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            var scaleTransform = new ScaleTransform(1, 1);
            element.RenderTransform = scaleTransform;

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
        }

        private void RemoveGlowEffect(UIElement element)
        {
            element.Effect = null;
            element.RenderTransform = null;
        }

        private void HideInactivePlayers()
        {
            var players = viewModel.PlayersDinos.ToList();
            int playerCount = players.Count;

            System.Diagnostics.Debug.WriteLine($"[PROVOKE] Active players: {playerCount}");

            P1Deck.Visibility = Visibility.Collapsed;
            gridPlayer1.Visibility = Visibility.Collapsed;
            P2Deck.Visibility = Visibility.Collapsed;
            gridPlayer2.Visibility = Visibility.Collapsed;
            P3Deck.Visibility = Visibility.Collapsed;
            gridPlayer3.Visibility = Visibility.Collapsed;
            P4Deck.Visibility = Visibility.Collapsed;
            gridPlayer4.Visibility = Visibility.Collapsed;

            if (playerCount >= 1)
            {
                P1Deck.Visibility = Visibility.Visible;
                gridPlayer1.Visibility = Visibility.Visible;
            }

            if (playerCount >= 2)
            {
                P2Deck.Visibility = Visibility.Visible;
                gridPlayer2.Visibility = Visibility.Visible;
            }

            if (playerCount >= 3)
            {
                P3Deck.Visibility = Visibility.Visible;
                gridPlayer3.Visibility = Visibility.Visible;
            }

            if (playerCount >= 4)
            {
                P4Deck.Visibility = Visibility.Visible;
                gridPlayer4.Visibility = Visibility.Visible;
            }
        }
    }
}