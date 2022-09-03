using System;
using System.Data;
using System.Data.Common;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using LinqToDB.Data;

namespace Synnotech.Linq2Db;

/// <summary>
/// Provides extension methods for <see cref="DataConnection" />.
/// </summary>
public static class DataConnectionExtensions
{
    /// <summary>
    /// Creates a command from the DB connection that is associated
    /// with the specified <paramref name="dataConnection" />. If the data connection
    /// is also associated with a transaction, it will be attached to the command.
    /// </summary>
    /// <param name="dataConnection">The Linq2Db data connection that is used to create the command.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
    /// <exception cref="TypeCastException">Thrown when the command cannot be cast to <typeparamref name="T" />.</exception>
    public static T CreateCommand<T>(this DataConnection dataConnection)
        where T : DbCommand
    {
        dataConnection.MustNotBeNull(nameof(dataConnection));

        var command = dataConnection.CreateCommand();
        if (dataConnection.Transaction != null)
            command.Transaction = dataConnection.Transaction;
        return (T) command;
    }
}