using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class DbContextWrapper
    {
        private readonly ArchsVsDinosConnection _context;

        public DbContextWrapper()
        {
            _context = new ArchsVsDinosConnection();
        }

        public IQueryable<UserAccount> UserAccount => _context.UserAccount;

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
