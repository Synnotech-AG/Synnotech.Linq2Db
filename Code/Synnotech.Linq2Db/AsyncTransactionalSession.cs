using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents an asynchronous session to MS SQL Server via a Linq2Db data connection.
    /// This session can be used to start and commit several transactions individually by
    /// calling <see cref="BeginTransactionAsync" />. Disposing this session will also
    /// dispose the underlying data connection.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only DataConnection will be disposed.
    /// </summary>
    /// <typeparam name="TDataConnection">Your database context type that derives from <see cref="DataConnection" />.</typeparam>
    public abstract class AsyncTransactionalSession<TDataConnection> : AsyncReadOnlySession<TDataConnection>, IAsyncTransactionalSession
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncTransactionalSession{TDataConnection}" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncTransactionalSession(TDataConnection dataConnection) : base(dataConnection) { }

        /// <summary>
        /// Begins a new transaction asynchronously. You must dispose the returned transaction by yourself.
        /// The session will not track any of the transactions that are created via this method.
        /// Furthermore, you should ensure that a previous transaction has been committed before
        /// calling this method again - Linq2Db will dispose the active transaction and create a
        /// new one internally.
        /// </summary>
        public async Task<IAsyncTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var dataConnectionTransaction = await DataConnection.BeginTransactionAsync(cancellationToken);
            return new Linq2DbTransaction(dataConnectionTransaction);
        }
    }

    /// <summary>
    /// Represents an asynchronous session to MS SQL Server via a Linq2Db data connection.
    /// This session can be used to start and commit several transactions individually by
    /// calling BeginTransactionAsync. Disposing this session will also dispose the underlying data connection.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only DataConnection will be disposed.
    /// </summary>
    public abstract class AsyncTransactionalSession : AsyncTransactionalSession<DataConnection>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncTransactionalSession" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncTransactionalSession(DataConnection dataConnection) : base(dataConnection) { }
    }
}