using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IDbContext : IDisposable
    {
        DbSet<UserAccount> UserAccount { get; }
        DbSet<Player> Player { get; }
        DbSet<Friendship> Friendship { get; }
        DbSet<FriendRequest> FriendRequest { get; }
        DbSet<Strike> Strike { get; } 
        DbSet<StrikeKind> StrikeKind { get; }  
        Database Database { get; }
        int SaveChanges();
    }
}
