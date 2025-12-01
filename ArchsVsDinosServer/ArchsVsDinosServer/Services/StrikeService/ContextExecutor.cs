using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class ContextExecutor
    {
        private readonly Func<IDbContext> contextFactory;
        private readonly ILoggerHelper logger;

        public ContextExecutor(Func<IDbContext> contextFactory, ILoggerHelper logger)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        public T Exec<T>(string method, int userId, Func<IDbContext, T> func)
        {
            try
            {
                using (var context = contextFactory())
                {
                    return func(context);
                }
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"{method}: Database update error for userId {userId}", ex);
                return default(T);
            }
            catch (SqlException ex)
            {
                logger.LogError($"{method}: SQL Server error for userId {userId}", ex);
                return default(T);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"{method}: Invalid operation for userId {userId}", ex);
                return default(T);
            }
            catch (Exception ex)
            {
                logger.LogError($"{method}: Unexpected error for userId {userId}", ex);
                return default(T);
            }
        }
    }

}
