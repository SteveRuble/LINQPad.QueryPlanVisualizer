using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Threading.Tasks;

namespace ExecutionPlanVisualizer.Helpers
{
    internal class EntityFrameworkDatabaseHelper : DatabaseHelper
    {
        protected override DbCommand CreateCommand(IQueryable queryable)
        {
            var interceptor = new CommandCapturingInterceptor();

            DbInterception.Add(interceptor);

            try
            {
                var result = queryable.Provider.Execute(queryable.Expression);
            }
            catch (Exception ex)
            {
                if (ex is CommandCapturedException || ex.InnerException is CommandCapturedException)
                {
                    // ignore
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                DbInterception.Remove(interceptor);
            }

            if (interceptor.Command == null)
            {
                throw new InvalidOperationException("DbInterception failed to capture DbCommand.");
            }

            Connection = interceptor.Command.Connection;

            var command = Connection.CreateCommand();

            command.CommandText = interceptor.Command.CommandText;
            var copiedParameters = interceptor.Command.Parameters.OfType<DbParameter>()
                                              .Select(parameter =>
                                              {
                                                  var parameterCopy = command.CreateParameter();
                                                  parameterCopy.ParameterName = parameter.ParameterName;
                                                  parameterCopy.DbType = parameter.DbType;
                                                  parameterCopy.Value = parameter.Value;
                                                  return parameterCopy;
                                              })
                                              .ToArray();

            command.Parameters.AddRange(copiedParameters);


            return command;
        }

        private class CommandCapturedException : Exception { }

        private sealed class CommandCapturingInterceptor : IDbCommandInterceptor
        {
            public DbCommand Command { get; set; }

            public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
            {
            }

            public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
            {
            }

            public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
            {
                Command = command;
                throw new CommandCapturedException();
            }

            public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
            {
            }

            public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
            {
            }

            public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
            {
            }
        }
    }
}