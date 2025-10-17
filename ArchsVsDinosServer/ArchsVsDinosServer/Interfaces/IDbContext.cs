using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IDbContext : IDisposable
    {
        IQueryable<UserAccount> UserAccount { get; }
        IQueryable<Player> Player { get; }

        int SaveChanges();
    }
}
