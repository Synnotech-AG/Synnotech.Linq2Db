using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents an asynchronous session to MS SQL Server via a Linq2Db data connection.
    /// This session wraps a transaction which should be already started before the <see cref="DataConnection" />
    /// is passed to this constructor. Calling <see cref="SaveChangesAsync" /> will commit the transaction.
    /// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// To easily set each session up, take a look at <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation,TDataConnection}" />
    /// and use the <see cref="AsyncSession{TDataConnection}(IsolationLevel)" /> constructor to define the transaction level (default is <see cref="IsolationLevel.Serializable" />.
    /// </summary>
    /// <typeparam name="TDataConnection">Your database context type that derives from <see cref="DataConnection" />.</typeparam>
    public abstract class AsyncSession<TDataConnection> : IAsyncSession
        where TDataConnection : DataConnection
    {
        private TDataConnection? _dataConnection;

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession{TDataConnection}" />. Use this constructor if you want
        /// to pass the initialized data connection with an associated transaction directly from your subclass.
        /// We do not recommend calling this constructor in DI scenarios, instead use the other constructor
        /// and use <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation,TDataConnection}" />
        /// to register your session with the DI container.
        /// </summary>
        /// <param name="dataConnection">
        /// The Linq2Db data connection used for database access. There already must be an transaction associated with this data connection
        /// when this constructor is called (i.e. you should call DataConnection.BeginTransactionAsync beforehand).
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncSession(TDataConnection dataConnection) => SetDataConnection(dataConnection);

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession{TDataConnection}" />. Use this constructor
        /// if you want to use the <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation,TDataConnection}" />
        /// method to register your sessions with the DI container.
        /// </summary>
        /// <param name="transactionLevel">The transaction level that should be applied to the underlying transaction (optional). Defaults to <see cref="IsolationLevel.Serializable" />.</param>
        protected AsyncSession(IsolationLevel transactionLevel = IsolationLevel.Serializable) =>
            TransactionLevel = transactionLevel;

        /// <summary>
        /// Gets the Linq2Db data connection.
        /// </summary>
        protected TDataConnection DataConnection =>
            _dataConnection ?? throw new InvalidOperationException("You must not retrieve the data connection before it is set. Check the constructors for more details.");

        internal IsolationLevel TransactionLevel { get; private set; }

        /// <summary>
        /// Disposes the Linq2Db data connection. If <see cref="SaveChangesAsync" /> has not been called,
        /// then the internal transaction will be rolled back implicitly by Linq2Db.
        /// </summary>
        public void Dispose() => _dataConnection?.Dispose();

        /// <summary>
        /// Disposes the Linq2Db data connection. If <see cref="SaveChangesAsync" /> has not been called,
        /// then the internal transaction will be rolled back implicitly by Linq2Db.
        /// </summary>
        public ValueTask DisposeAsync() => _dataConnection?.DisposeAsync() ?? default;

        /// <summary>
        /// Commits the internal transaction.
        /// </summary>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => DataConnection.CommitTransactionAsync(cancellationToken);

        internal void SetDataConnection(TDataConnection dataConnection)
        {
            _dataConnection = dataConnection.MustNotBeNull(nameof(dataConnection));
            Check.InvalidOperation(dataConnection.Transaction == null, "A transaction must have been started before the data connection is passed to AsyncSession");
            TransactionLevel = dataConnection.Transaction!.IsolationLevel;
        }
    }

    /// <summary>
    /// Represents an asynchronous session to MS SQL Server via a Linq2Db data connection.
    /// This session wraps a transaction which should be already started before the <see cref="DataConnection" />
    /// is passed to this constructor. Calling SaveChangesAsync will commit the transaction.
    /// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
    /// Beware: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// To easily set each session up, take a look at <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />
    /// and use the <see cref="AsyncSession(IsolationLevel)" /> constructor to define the transaction level (default is <see cref="IsolationLevel.Serializable" />.
    /// </summary>
    public abstract class AsyncSession : AsyncSession<DataConnection>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession" />. Use this constructor if you want
        /// to pass the initialized data connection with an associated transaction directly from your subclass.
        /// We do not recommend calling this constructor in DI scenarios, instead use the other constructor
        /// and use <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />
        /// to register your session with the DI container.
        /// </summary>
        /// <param name="dataConnection">
        /// The Linq2Db data connection used for database access. There already must be an transaction associated with this data connection
        /// when this constructor is called (i.e. you should call DataConnection.BeginTransactionAsync beforehand).
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncSession(DataConnection dataConnection) : base(dataConnection) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession" />. Use this constructor
        /// if you want to use the <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />
        /// method to register your sessions with the DI container.
        /// </summary>
        /// <param name="transactionLevel">The transaction level that should be applied to the underlying transaction (optional). Defaults to <see cref="IsolationLevel.Serializable" />.</param>
        protected AsyncSession(IsolationLevel transactionLevel = IsolationLevel.Serializable) : base(transactionLevel) { }
    }
}