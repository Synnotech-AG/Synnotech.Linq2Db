using System;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using Microsoft.Data.SqlClient;

namespace Synnotech.Linq2Db.MsSqlServer
{
    /// <summary>
    /// Provides extension methods for <see cref="DataConnection" />.
    /// </summary>
    public static class DataConnectionExtensions
    {
        /// <summary>
        /// Creates a <see cref="SqlCommand" /> from the DB connection that is associated
        /// with the specified <paramref name="dataConnection" />. If the data connection
        /// is also associated with a transaction, it will be attached to the command.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection that is used to create the command.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        /// <exception cref="TypeCastException">Thrown when <paramref name="dataConnection" /> is not configured to use a <see cref="SqlServerProviderAdapter.SqlConnection" /> internally.</exception>
        public static SqlCommand CreateSqlCommand(this DataConnection dataConnection)
        {
            dataConnection.MustNotBeNull(nameof(dataConnection));
            var sqlConnection = dataConnection.Connection.MustBeOfType<SqlConnection>(nameof(dataConnection), $"{nameof(dataConnection)} is not configured with a {typeof(SqlConnection)}.");

            var command = sqlConnection.CreateCommand();
            if (dataConnection.Transaction != null)
                command.Transaction = (SqlTransaction)dataConnection.Transaction;
            return command;
        }
    }
}