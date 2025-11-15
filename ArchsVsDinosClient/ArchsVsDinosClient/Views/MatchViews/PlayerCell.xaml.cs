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
    public partial class PlayerCell : UserControl
    {
        public PlayerCell()
        {
            InitializeComponent();
        }

        public CardCell GetCombinationCell(int index)
        {
            switch (index)
            {
                case 1: return CombinationCell_1;
                case 2: return CombinationCell_2;
                case 3: return CombinationCell_3;
                case 4: return CombinationCell_4;
                case 5: return CombinationCell_5;
                case 6: return CombinationCell_6;
                default: return null;
            }
        }
    }
}
