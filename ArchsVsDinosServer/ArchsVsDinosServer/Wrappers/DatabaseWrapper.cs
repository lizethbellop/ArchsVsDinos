using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class DatabaseWrapper : IDatabase
    {
        private readonly Database database;

        public DatabaseWrapper(Database database)
        {
            this.database = database;
        }

        public IDbContextTransaction BeginTransaction()
        {
            var transaction = database.BeginTransaction();
            return new DbContextTransactionWrapper(transaction);
        }
    }
}
