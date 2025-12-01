using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class DbContextWrapper : IDbContext
    {
        private readonly ArchsVsDinosConnection context;
        private IDatabase database;

        public DbContextWrapper()
        {
            context = new ArchsVsDinosConnection();
        }

        public DbSet<UserAccount> UserAccount => context.UserAccount;
        public DbSet<Player> Player => context.Player;
        public DbSet<Friendship> Friendship => context.Friendship;
        public DbSet<FriendRequest> FriendRequest => context.FriendRequest;
        public DbSet<Strike> Strike => context.Strike;
        public DbSet<StrikeKind> StrikeKind => context.StrikeKind;
        public DbSet<UserHasStrike> UserHasStrike => context.UserHasStrike;

        public DbSet<CardBody> CardBody => context.CardBody;
        public DbSet<CardCharacter> CardCharacter => context.CardCharacter;

        public DbSet<GeneralMatch> GeneralMatch => context.GeneralMatch;
        public DbSet<MatchParticipants> MatchParticipants => context.MatchParticipants;

        public IDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new DatabaseWrapper(context.Database);
                }
                return database;
            }
        }

        public DbSet<Configuration> Configuration => context.Configuration;

        public void Dispose()
        {
            context.Dispose();
        }

        public int SaveChanges()
        {
            return context.SaveChanges();
        }
    }
}
