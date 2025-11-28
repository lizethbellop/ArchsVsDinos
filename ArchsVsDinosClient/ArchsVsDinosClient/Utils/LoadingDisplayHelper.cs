using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArchsVsDinosClient.Utils
{
    public static class LoadingDisplayHelper
    {
        public static void ShowLoading(Grid overlay)
        {
            overlay.Visibility = Visibility.Visible;
        }

        public static void HideLoading(Grid overlay)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }
}
