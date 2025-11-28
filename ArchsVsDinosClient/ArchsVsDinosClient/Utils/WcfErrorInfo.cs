using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class WcfErrorInfo
    {
        public string Title { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public string OperationName { get; }

        public WcfErrorInfo(string title, string message, Exception exception, string operationName)
        {
            Title = title;
            Message = message;
            Exception = exception;
            OperationName = operationName;
        }
    }

}
