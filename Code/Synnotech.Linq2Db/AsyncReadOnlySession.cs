using System;
using System.Threading.Tasks;
using Light.GuardClauses;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents an asynchronous database session via a Linq2Db data connection. This session
    /// is only used to read data (i.e. no data is inserted or updated), thus SaveChangesAsync
    /// is not available. No transaction is needed while this session is active.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// </summary>
    /// <typeparam name="TDataConnection">Your database context type that derives from <see cref="DataConnection" />.</typeparam>
    public abstract class AsyncReadOnlySession<TDataConnection> : IAsyncReadOnlySession
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession{TDataConnection}" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(TDataConnection dataConnection) =>
            DataConnection = dataConnection.MustNotBeNull(nameof(dataConnection));

        /// <summary>
        /// Gets the Linq2Db data connection.
        /// </summary>
        protected TDataConnection DataConnection { get; }

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public void Dispose() => DataConnection.Dispose();

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public ValueTask DisposeAsync() => DataConnection.DisposeAsync();
    }

    /// <summary>
    /// Represents an asynchronous database session via a Linq2Db data connection. This session
    /// is only used to read data (i.e. no data is inserted or updated), thus SaveChangesAsync
    /// is not available. No transaction is needed while this session is active.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only the DataConnection will be disposed.
    /// </summary>
    public abstract class AsyncReadOnlySession : AsyncReadOnlySession<DataConnection>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(DataConnection dataConnection) : base(dataConnection) { }
    }
}