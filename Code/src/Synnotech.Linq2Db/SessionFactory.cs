using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents a factory that creates a data connection, opens a connection to
    /// the target database asynchronously and then starts a transaction asynchronously.
    /// </summary>
    /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
    /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O.</typeparam>
    /// <typeparam name="TDataConnection">Your custom data connection subtype that you use in your solution.</typeparam>
    public class SessionFactory<TAbstraction, TImplementation, TDataConnection> : ISessionFactory<TAbstraction>
        where TAbstraction : IAsyncSession
        where TImplementation : AsyncSession<TDataConnection>, TAbstraction, new()
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SessionFactory{TAbstraction,TImplementation,TDataConnection}"/>.
        /// </summary>
        /// <param name="createDataConnection">The delegate that initializes a new data connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection"/> is null.</exception>
        public SessionFactory(Func<TDataConnection> createDataConnection) =>
            CreateDataConnection = createDataConnection.MustNotBeNull(nameof(createDataConnection));

        private Func<TDataConnection> CreateDataConnection { get; }

        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="DbException">Thrown when an SQL error occurred when opening the session or starting the transaction.</exception>
        public Task<TAbstraction> OpenSessionAsync(CancellationToken cancellationToken = default) =>
            CreateDataConnection.CreateAndOpenSessionAsync<TAbstraction, TImplementation, TDataConnection>(cancellationToken);
    }

    /// <summary>
    /// Represents a factory that creates a data connection, opens a connection to
    /// the target database asynchronously and then starts a transaction asynchronously.
    /// </summary>
    /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
    /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O.</typeparam>
    public class SessionFactory<TAbstraction, TImplementation> : SessionFactory<TAbstraction, TImplementation, DataConnection>
        where TAbstraction : IAsyncSession
        where TImplementation : AsyncSession, TAbstraction, new()
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SessionFactory{TAbstraction,TImplementation}"/>.
        /// </summary>
        /// <param name="createDataConnection">The delegate that initializes a new data connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection"/> is null.</exception>
        public SessionFactory(Func<DataConnection> createDataConnection) : base(createDataConnection) { }
    }
}