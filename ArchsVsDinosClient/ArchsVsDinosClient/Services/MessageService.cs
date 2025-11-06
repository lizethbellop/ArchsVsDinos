using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public class MessageService : IMessageService
    {
        public void ShowMessage (string message)
        {
            MessageBox.Show(message);
        }
    }
}
