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
    public partial class CardCell : UserControl
    {
        private Border[] subCells;

        public CardCell()
        {
            InitializeComponent();
            CacheSubCells();
        }

        public string CellId
        {
            get => (string)GetValue(CellIdProperty);
            set => SetValue(CellIdProperty, value);
        }

        public static readonly DependencyProperty CellIdProperty =
            DependencyProperty.Register(nameof(CellId), typeof(string),
            typeof(CardCell), new PropertyMetadata(string.Empty));

        private void CacheSubCells()
        {
            subCells = new Border[]
            {
                Part_1, Part_Head, Part_3,
                Part_LeftArm, Part_Chest, Part_RightArm,
                Part_7, Part_Legs, Part_9
            };
        }

        public Border GetSubCell(int index)
        {
            if (index < 1 || index > 9)
                return null;

            return subCells[index - 1];
        }

        public void SetSubCellBackground(int index, Brush brush)
        {
            var cell = GetSubCell(index);
            if (cell != null)
                cell.Background = brush;
        }
    }
}
