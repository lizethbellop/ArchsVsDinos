using ArchsVsDinosClient.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class DeckCardSelection : UserControl
    {
        private double originalBottom = -80; 
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point elementStartPosition;

        public Card Card
        {
            get => (Card)GetValue(CardProperty);
            set => SetValue(CardProperty, value);
        }

        public static readonly DependencyProperty CardProperty =
            DependencyProperty.Register("Card", typeof(Card), typeof(DeckCardSelection),
                new PropertyMetadata(null, OnCardChanged));

        private static void OnCardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DeckCardSelection control && e.NewValue is Card card)
            {
                control.DataContext = card;
            }
        }

        public DeckCardSelection()
        {
            InitializeComponent();

            this.MouseEnter += CardControl_MouseEnter;
            this.MouseLeave += CardControl_MouseLeave;
            this.MouseLeftButtonDown += CardControl_MouseLeftButtonDown;
            this.MouseMove += CardControl_MouseMove;
            this.MouseLeftButtonUp += CardControl_MouseLeftButtonUp;
        }

        private void CardControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (isDragging) return;

            var parent = this.Parent as Canvas;
            if (parent == null) return;

            Canvas.SetBottom(this, 20);
            Panel.SetZIndex(this, 1000);

            CardTransform.ScaleX = 1.15;
            CardTransform.ScaleY = 1.15;

            BorderGlow.BorderThickness = new Thickness(3);
            GlowEffect.BlurRadius = 20;
            GlowEffect.Opacity = 1;
        }

        private void CardControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isDragging) return;

            var parent = this.Parent as Canvas;
            if (parent != null)
            {
                Canvas.SetBottom(this, originalBottom); 
                Panel.SetZIndex(this, 1);
            }

            CardTransform.ScaleX = 1.0;
            CardTransform.ScaleY = 1.0;

            BorderGlow.BorderThickness = new Thickness(0);
            GlowEffect.BlurRadius = 0;
            GlowEffect.Opacity = 0;
        }

        private void CardControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;

            var parent = this.Parent as Canvas;
            if (parent == null) return;

            dragStartPoint = e.GetPosition(parent);
            elementStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetBottom(this));

            this.CaptureMouse();

            CardTransform.ScaleX = 1.0;
            CardTransform.ScaleY = 1.0;
            Canvas.SetBottom(this, Canvas.GetBottom(this)); 

            Panel.SetZIndex(this, 2000);
            e.Handled = true;
        }

        private void CardControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            var parent = this.Parent as Canvas;
            if (parent == null) return;

            var currentPosition = e.GetPosition(parent);
            var offset = currentPosition - dragStartPoint;

            var newLeft = elementStartPosition.X + offset.X;
            var newBottom = elementStartPosition.Y - offset.Y;

            Canvas.SetLeft(this, newLeft);
            Canvas.SetBottom(this, newBottom);
        }

        private void CardControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;

            isDragging = false;
            this.ReleaseMouseCapture();
            ResetToOriginalPosition();

            e.Handled = true;
        }

        private void ResetToOriginalPosition()
        {
            var parent = this.Parent as Canvas;
            if (parent != null)
            {
                Canvas.SetLeft(this, elementStartPosition.X);
                Canvas.SetBottom(this, originalBottom);
                Panel.SetZIndex(this, 1);
            }
        }

        public void SetInitialPosition(double left, double bottom)
        {
            originalBottom = bottom;
            elementStartPosition = new Point(left, bottom);
            Canvas.SetLeft(this, left);
            Canvas.SetBottom(this, bottom);
        }
    }
}