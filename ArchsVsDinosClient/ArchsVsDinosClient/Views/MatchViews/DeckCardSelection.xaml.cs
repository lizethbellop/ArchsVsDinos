using ArchsVsDinosClient.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq; 

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class DeckCardSelection : UserControl
    {
        private double originalBottom = -70; 
        private bool isDragging = false;
        private Point clickOffset; 

        public Card Card
        {
            get => (Card)GetValue(CardProperty);
            set => SetValue(CardProperty, value);
        }

        public static readonly DependencyProperty CardProperty =
            DependencyProperty.Register("Card", typeof(Card), typeof(DeckCardSelection),
                new PropertyMetadata(null, (d, e) => {
                    if (d is DeckCardSelection ctrl && e.NewValue is Card c) ctrl.DataContext = c;
                }));

        public DeckCardSelection()
        {
            InitializeComponent();
            this.MouseEnter += (s, e) => { if (!isDragging) { VisualTransform(true); } };
            this.MouseLeave += (s, e) => { if (!isDragging) { VisualTransform(false); } };
            this.MouseLeftButtonDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseUp;
        }

        private void VisualTransform(bool isHover)
        {
            var parent = this.Parent as Canvas;
            if (parent == null) return;

            if (isHover)
            {
                Canvas.SetBottom(this, 20);
                Panel.SetZIndex(this, 1000);
                CardTransform.ScaleX = 1.15; CardTransform.ScaleY = 1.15;
                BorderGlow.BorderThickness = new Thickness(3);
                GlowEffect.Opacity = 1;
            }
            else
            {
                Canvas.SetBottom(this, originalBottom);
                Panel.SetZIndex(this, 1);
                CardTransform.ScaleX = 1.0; CardTransform.ScaleY = 1.0;
                BorderGlow.BorderThickness = new Thickness(0);
                GlowEffect.Opacity = 0;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = this.Parent as Canvas;
            if (parent == null) return;

            isDragging = true;
            clickOffset = e.GetPosition(this);

            this.CaptureMouse();
            Panel.SetZIndex(this, 2000); 
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            var parent = this.Parent as Canvas;
            if (parent == null) return;

            Point mousePosParent = e.GetPosition(parent);
            Canvas.SetLeft(this, mousePosParent.X - clickOffset.X);
            Canvas.SetBottom(this, (parent.ActualHeight - mousePosParent.Y) - (this.ActualHeight - clickOffset.Y));

            var mainMatch = Application.Current.Windows.OfType<MainMatch>().FirstOrDefault();
            if (mainMatch != null)
            {
                Point screenPoint = this.PointToScreen(e.GetPosition(this));
                Point windowPoint = mainMatch.PointFromScreen(screenPoint);
                mainMatch.ManualDragOver(windowPoint, this.Card);
            }
        }

        private async void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;
            this.ReleaseMouseCapture();

            bool success = false;

            var mainMatch = Application.Current.Windows.OfType<MainMatch>().FirstOrDefault();
            if (mainMatch != null)
            {
                Point screenPoint = this.PointToScreen(e.GetPosition(this));
                Point windowPoint = mainMatch.PointFromScreen(screenPoint);
                success = await mainMatch.ManualDrop(windowPoint, this.Card);
            }

            if (!success)
            {
                var parentCanvas = this.Parent as Canvas;
                parentCanvas?.Children.Remove(this);

                if (mainMatch != null)
                {
                    mainMatch.UpdatePlayerHandVisual();
                }
                else
                {
                    VisualTransform(false);
                }
            }
        }

        private double elementStartPositionX;

        public void SetInitialPosition(double left, double bottom)
        {
            originalBottom = bottom;
            elementStartPositionX = left;
            Canvas.SetLeft(this, left);
            Canvas.SetBottom(this, bottom);
        }
    }
}