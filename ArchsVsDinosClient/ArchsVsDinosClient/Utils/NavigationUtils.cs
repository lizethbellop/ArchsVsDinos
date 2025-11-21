using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    public static class NavigationUtils
    {
        public static void GoToMainMenu()
        {
            var main = new MainWindow();
            main.Show();

            Application.Current.Windows[0].Close();
        }
    }

}
