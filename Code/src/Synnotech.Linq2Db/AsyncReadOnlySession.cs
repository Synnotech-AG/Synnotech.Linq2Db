using System;
using System.Data;
using System.Threading.Tasks;
using Light.GuardClauses;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// <para>
    /// Represents an asynchronous database session via a Linq2Db data connection. This session
    /// is only used to read data (i.e. no data is inserted, updated, or deleted), thus SaveChangesAsync
    /// is not available. No transaction is needed while this session is active.
    /// </para>
    /// <para>
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// </para>
    /// </summary>
    /// <typeparam name="TDataConnection">Your database context type that derives from <see cref="DataConnection" />.</typeparam>
    public abstract class AsyncReadOnlySession<TDataConnection> : IAsyncReadOnlySession, IInitializeAsync
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession{TDataConnection}" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <param name="transactionLevel">
        /// The isolation level for the transaction (optional). The default value is <see cref="IsolationLevel.Unspecified" />.
        /// When this value is set to <see cref="IsolationLevel.Unspecified" />, no transaction will be started.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(TDataConnection dataConnection, IsolationLevel transactionLevel = IsolationLevel.Unspecified)
        {
            DataConnection = dataConnection.MustNotBeNull(nameof(dataConnection));
            TransactionLevel = transactionLevel;
        }

        /// <summary>
        /// Gets the Linq2Db data connection.
        /// </summary>
        protected TDataConnection DataConnection { get; }

        /// <summary>
        /// Gets the isolation level of the transaction.
        /// </summary>
        protected IsolationLevel TransactionLevel { get; }

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public void Dispose() => DataConnection.Dispose();

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public ValueTask DisposeAsync() => DataConnection.DisposeAsync();

        bool IInitializeAsync.IsInitialized =>
            TransactionLevel == IsolationLevel.Unspecified || DataConnection.Transaction != null;

        Task IInitializeAsync.InitializeAsync() =>
            TransactionLevel != IsolationLevel.Unspecified ?
                DataConnection.BeginTransactionAsync(TransactionLevel) :
                Task.CompletedTask;
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
        /// Initializes a new instance of <see cref="AsyncReadOnlySession" />. Use this constructor
        /// if you want to pass in the <see cref="DataConnection" /> directly.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <param name="transactionLevel">
        /// The isolation level for the transaction (optional). The default value is <see cref="IsolationLevel.Unspecified" />.
        /// When this value is set to <see cref="IsolationLevel.Unspecified" />, no transaction will be started.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(DataConnection dataConnection, IsolationLevel transactionLevel = IsolationLevel.Unspecified)
            : base(dataConnection, transactionLevel) { }
    }
}