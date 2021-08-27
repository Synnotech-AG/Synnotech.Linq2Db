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
        private TDataConnection? _dataConnection;

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession{TDataConnection}" />. Use this constructor if
        /// you want to use <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation,TDataConnection}" />
        /// method to register your session with the DI container.
        /// </summary>
        protected AsyncReadOnlySession() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession{TDataConnection}" />. 
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(TDataConnection dataConnection) => SetDataConnection(dataConnection);

        /// <summary>
        /// Gets the Linq2Db data connection.
        /// </summary>
        protected TDataConnection DataConnection =>
            _dataConnection ?? throw new InvalidOperationException("You must not retrieve the data connection before it is set. Check the constructors for more details.");

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public void Dispose() => _dataConnection?.Dispose();

        /// <summary>
        /// Disposes the Linq2Db data connection.
        /// </summary>
        public ValueTask DisposeAsync() => _dataConnection?.DisposeAsync() ?? default;

        internal void SetDataConnection(TDataConnection dataConnection) =>
            _dataConnection = dataConnection.MustNotBeNull(nameof(dataConnection));
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
        /// Initializes a new instance of <see cref="AsyncReadOnlySession" />. Use this constructor if
        /// you want to use <see cref="ServiceCollectionExtensions.AddSessionFactoryFor{TAbstraction,TImplementation}" />
        /// method to register your session with the DI container.
        /// </summary>
        protected AsyncReadOnlySession() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncReadOnlySession" />. Use this constructor
        /// if you want to pass in the <see cref="DataConnection" /> directly.
        /// </summary>
        /// <param name="dataConnection">The Linq2Db data connection used for database access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataConnection" /> is null.</exception>
        protected AsyncReadOnlySession(DataConnection dataConnection) : base(dataConnection) { }
    }
}