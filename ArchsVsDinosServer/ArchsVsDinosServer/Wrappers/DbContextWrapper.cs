using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class DbContextWrapper : IDbContext
    {
        private readonly ArchsVsDinosConnection _context;

        public DbContextWrapper()
        {
            _context = new ArchsVsDinosConnection();
        }

        public IQueryable<UserAccount> UserAccount => _context.UserAccount;

        public IQueryable<Player> Player => _context.Player;

        public void Dispose()
        {
            _context.Dispose();
        }

        public int SaveChanges()
        {
            throw new NotImplementedException();
        }
    }
}
