using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IDbContextTransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }
}
