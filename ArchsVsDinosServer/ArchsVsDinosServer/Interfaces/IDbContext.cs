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
        DbSet<UserHasStrike> UserHasStrike { get; }
        DbSet<CardBody> CardBody { get; }
        DbSet<CardCharacter> CardCharacter { get; }

        DbSet<GeneralMatch> GeneralMatch { get; }
        DbSet<MatchParticipants> MatchParticipants { get; }

        DbSet<Configuration> Configuration { get; }
        Database Database { get; }
        int SaveChanges();
    }
}
