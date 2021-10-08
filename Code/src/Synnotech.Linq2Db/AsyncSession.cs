using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// <para>
    /// Represents an asynchronous session via a Linq2Db data connection.
    /// This session wraps a transaction which is started by calling <see cref="IInitializeAsync.InitializeAsync" /> (this is usually done
    /// when you instantiate the session via <see cref="ISessionFactory{TSessionAbstraction}" />).
    /// Calling <see cref="SaveChangesAsync" /> will commit the transaction.
    /// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
    /// </para>
    /// <para>
    /// BEWARE: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// </para>
    /// <para>
    /// To easily set each session up with a DI container, take a look at
    /// <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />.
    /// </para>
    /// </summary>
    /// <typeparam name="TDataConnection">Your database context type that derives from <see cref="DataConnection" />.</typeparam>
    public abstract class AsyncSession<TDataConnection> : AsyncReadOnlySession<TDataConnection>, IAsyncSession, IInitializeAsync
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession{TDataConnection}" />.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <param name="transactionLevel">
        /// The isolation level for the transaction (optional). The default value is <see cref="IsolationLevel.Serializable" />.
        /// When this value is set to <see cref="IsolationLevel.Unspecified" />, no transaction will be started.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncSession(TDataConnection dataConnection, IsolationLevel transactionLevel = IsolationLevel.Serializable)
            : base(dataConnection, transactionLevel) { }

        /// <summary>
        /// Commits the internal transaction if possible.
        /// </summary>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            TransactionLevel != IsolationLevel.Unspecified ?
                DataConnection.CommitTransactionAsync(cancellationToken) :
                Task.CompletedTask;
    }

    /// <summary>
    /// <para>
    /// Represents an asynchronous session via a Linq2Db data connection.
    /// This session wraps a transaction which is started by calling <see cref="IInitializeAsync.InitializeAsync" /> (this is usually done
    /// when you instantiate the session via <see cref="ISessionFactory{TSessionAbstraction}" />).
    /// Calling SaveChangesAsync will commit the transaction.
    /// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
    /// </para>
    /// <para>
    /// BEWARE: you must not derive from this class and introduce other references to disposable objects.
    /// Only the <see cref="DataConnection" /> will be disposed.
    /// </para>
    /// <para>
    /// To easily set each session up with a DI container, take a look at
    /// <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />.
    /// </para>
    /// </summary>
    public abstract class AsyncSession : AsyncSession<DataConnection>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AsyncSession" />. Use this constructor
        /// if you want to use the <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />
        /// method to register your sessions with the DI container.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <param name="transactionLevel">The isolation level for the transaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncSession(DataConnection dataConnection, IsolationLevel transactionLevel = IsolationLevel.Serializable) : base(dataConnection, transactionLevel) { }
    }
}