using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class DbContextTransactionWrapper : IDbContextTransaction
    {
        private readonly DbContextTransaction transaction;

        public DbContextTransactionWrapper(DbContextTransaction transaction)
        {
            this.transaction = transaction;
        }

        public void Commit()
        {
            transaction.Commit();
        }

        public void Rollback()
        {
            transaction.Rollback();
        }

        public void Dispose()
        {
            transaction?.Dispose();
        }
    }
}
