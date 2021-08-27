﻿using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using LinqToDB.Data;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Provides extension methods for <see cref="DataConnection" />.
    /// </summary>
    public static class DataConnectionExtensions
    {
        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
        /// <typeparam name="TImplementation">
        /// The LinqToDB session implementation that performs the actual database I/O. It must derive
        /// from <see cref="AsyncSession{TDataConnection}" /> and must have a default constructor.
        /// </typeparam>
        /// <typeparam name="TDataConnection">Your custom data connection subtype that you use in your solution.</typeparam>
        /// <param name="createDataConnection">The delegate that creates a new data connection instance.</param>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        /// <exception cref="DbException">Thrown when an error occurs during opening the connection or starting the transaction.</exception>
        public static async ValueTask<TAbstraction> CreateAndOpenSessionAsync<TAbstraction, TImplementation, TDataConnection>(this Func<TDataConnection> createDataConnection,
                                                                                                                              CancellationToken cancellationToken = default)
            where TImplementation : AsyncSession<TDataConnection>, TAbstraction, new()
            where TDataConnection : DataConnection
        {
            createDataConnection.MustNotBeNull(nameof(createDataConnection));

            var dataConnection = createDataConnection();
            var session = new TImplementation();
            await dataConnection.BeginTransactionAsync(session.TransactionLevel, cancellationToken);
            session.SetDataConnection(dataConnection);
            return session;
        }

        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <typeparam name="TAsyncSession">
        /// The LinqToDB session implementation that performs the actual database I/O. It must derive
        /// from <see cref="AsyncSession{TDataConnection}" /> and must have a default constructor.
        /// </typeparam>
        /// <typeparam name="TDataConnection">
        /// Your custom data connection subtype that you use in your solution.
        /// </typeparam>
        /// <param name="createDataConnection">The delegate that creates a new data connection instance.</param>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        /// <exception cref="DbException">Thrown when an error occurs during opening the connection or starting the transaction.</exception>
        public static ValueTask<TAsyncSession> CreateAndOpenSessionAsync<TAsyncSession, TDataConnection>(this Func<TDataConnection> createDataConnection,
                                                                                                         CancellationToken cancellationToken = default)
            where TAsyncSession : AsyncSession<TDataConnection>, new()
            where TDataConnection : DataConnection =>
            createDataConnection.CreateAndOpenSessionAsync<TAsyncSession, TAsyncSession, TDataConnection>(cancellationToken);

        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
        /// <typeparam name="TImplementation">
        /// The LinqToDB session implementation that performs the actual database I/O. It must derive
        /// from <see cref="AsyncSession" /> and must have a default constructor.
        /// </typeparam>
        /// <param name="createDataConnection">The delegate that creates a new data connection instance.</param>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        /// <exception cref="DbException">Thrown when an error occurs during opening the connection or starting the transaction.</exception>
        public static ValueTask<TAbstraction> CreateAndOpenSessionAsync<TAbstraction, TImplementation>(this Func<DataConnection> createDataConnection,
                                                                                                       CancellationToken cancellationToken = default)
            where TImplementation : AsyncSession, TAbstraction, new() =>
            createDataConnection.CreateAndOpenSessionAsync<TAbstraction, TImplementation, DataConnection>(cancellationToken);

        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <typeparam name="TAsyncSession">
        /// The LinqToDB session implementation that performs the actual database I/O. It must derive
        /// from <see cref="AsyncSession" /> and must have a default constructor.
        /// </typeparam>
        /// <param name="createDataConnection">The delegate that creates a new data connection instance.</param>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        /// <exception cref="DbException">Thrown when an error occurs during opening the connection or starting the transaction.</exception>
        public static ValueTask<TAsyncSession> CreateAndOpenSessionAsync<TAsyncSession>(this Func<DataConnection> createDataConnection,
                                                                                        CancellationToken cancellationToken = default)
            where TAsyncSession : AsyncSession, new() =>
            createDataConnection.CreateAndOpenSessionAsync<TAsyncSession, TAsyncSession, DataConnection>(cancellationToken);

        /// <summary>
        /// Creates a command from the DB connection that is associated
        /// with the specified <paramref name="dataConnection" />. If the data connection
        /// is also associated with a transaction, it will be attached to the command.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection that is used to create the command.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        /// <exception cref="TypeCastException">Thrown when the command cannot be cast to <typeparamref name="T" />.</exception>
        public static T CreateCommand<T>(this DataConnection dataConnection)
            where T : IDbCommand
        {
            dataConnection.MustNotBeNull(nameof(dataConnection));

            var command = dataConnection.CreateCommand();
            if (dataConnection.Transaction != null)
                command.Transaction = dataConnection.Transaction;
            return (T)command;
        }
    }
}