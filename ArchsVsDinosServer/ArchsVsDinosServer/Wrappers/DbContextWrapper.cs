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

        public DbContextWrapper()
        {
            context = new ArchsVsDinosConnection();
        }

        public DbSet<UserAccount> UserAccount => context.UserAccount;
        public DbSet<Player> Player => context.Player;
        public DbSet<Friendship> Friendship => context.Friendship;
        public DbSet<FriendRequest> FriendRequest => context.FriendRequest;

        public Database Database => context.Database;


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
