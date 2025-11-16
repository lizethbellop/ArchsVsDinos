using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views.MatchViews
{

    public partial class DeckCardSelection : UserControl
    {

        private double originalTop;
        private bool topSaved = false;

        public DeckCardSelection()
        {
            InitializeComponent();

            this.MouseEnter += CardControl_MouseEnter;
            this.MouseLeave += CardControl_MouseLeave;
        }

        private void CardControl_MouseEnter(object sender, MouseEventArgs e)
        {
            var parent = this.Parent as Canvas;
            if (parent == null) return;

            if (!topSaved)
            {
                originalTop = Canvas.GetTop(this);
                topSaved = true;
            }


            Canvas.SetTop(this, originalTop - 25);
            BorderGlow.BorderThickness = new Thickness(4);
            this.RenderTransform = new ScaleTransform(1.1, 1.1);
            this.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void CardControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (topSaved)
                Canvas.SetTop(this, originalTop);

            BorderGlow.BorderThickness = new Thickness(0);
            this.RenderTransform = new ScaleTransform(1.0, 1.0);
        }
    }
}
