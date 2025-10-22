using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Utils;
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
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para VerificationCode.xaml
    /// </summary>
    public partial class ConfirmCode : Window
    {

        public string EnteredCode { get; private set; }
        public bool IsCancelled { get; private set; } = true;
       
        public ConfirmCode()
        {
            InitializeComponent();
        }

        private void Btn_Accept(object sender, RoutedEventArgs e)
        {

            EnteredCode = txtBoxCode.Text;

            if(ValidationHelper.isEmpty(EnteredCode))
            {
                MessageBox.Show(Lang.Global_EmptyField);
                return;
            }

            IsCancelled = false;
            this.Close();

        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            this.Close();
        }
    }
}
