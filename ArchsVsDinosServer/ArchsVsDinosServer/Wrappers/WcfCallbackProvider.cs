using ArchsVsDinosServer.Interfaces;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class WcfCallbackProvider : ICallbackProvider
    {
        public IChatManagerCallback GetCallback()
        {
            return OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
        }
    }
}
