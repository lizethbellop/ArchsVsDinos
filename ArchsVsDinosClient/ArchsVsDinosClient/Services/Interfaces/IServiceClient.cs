using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IServiceClient
    {
        bool IsServerAvailable { get; }
        string LastErrorTitle { get; }
        string LastErrorMessage { get; }
    }
}
