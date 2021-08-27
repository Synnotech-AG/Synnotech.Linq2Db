using System;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using LinqToDB.Data;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents an <see cref="ISessionFactory{TSessionAbstraction}" /> that injects a new custom <typeparamref name="TDataConnection" />
    /// into a new session instance.
    /// </summary>
    /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
    /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O.</typeparam>
    /// <typeparam name="TDataConnection">Your custom data connection subtype that you use in your solution.</typeparam>
    public class ReadOnlySessionFactory<TAbstraction, TImplementation, TDataConnection> : ISessionFactory<TAbstraction>
        where TAbstraction : IAsyncReadOnlySession
        where TImplementation : AsyncReadOnlySession<TDataConnection>, TAbstraction, new()
        where TDataConnection : DataConnection
    {
        /// <summary>
        /// Initializes a new instance  of <see cref="ReadOnlySessionFactory{TAbstraction,TImplementation, TDataConnection}" />.
        /// </summary>
        /// <param name="createDataConnection">The delegate that initializes a new <typeparamref name="TDataConnection" /> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        public ReadOnlySessionFactory(Func<TDataConnection> createDataConnection) =>
            CreateDataConnection = createDataConnection.MustNotBeNull(nameof(createDataConnection));

        private Func<TDataConnection> CreateDataConnection { get; }

        /// <summary>
        /// Instantiates the session, injects a data connection into it and returns it.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to cancel this asynchronous operation (optional).
        /// This parameter will be ignored as the session is instantiated in a synchronous fashion.
        /// </param>
        public ValueTask<TAbstraction> OpenSessionAsync(CancellationToken cancellationToken = default)
        {
            var dataConnection = CreateDataConnection();
            var session = new TImplementation();
            session.SetDataConnection(dataConnection);
            return new (session);
        }
    }

    /// <summary>
    /// Represents an <see cref="ISessionFactory{TSessionAbstraction}" /> that injects a new <see cref="DataConnection" />
    /// into a new session instance.
    /// </summary>
    /// <typeparam name="TAbstraction">The abstraction that your session implements.</typeparam>
    /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O.</typeparam>
    public class ReadOnlySessionFactory<TAbstraction, TImplementation> : ReadOnlySessionFactory<TAbstraction, TImplementation, DataConnection>
        where TAbstraction : IAsyncReadOnlySession
        where TImplementation : AsyncReadOnlySession, TAbstraction, new()
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ReadOnlySessionFactory{TAbstraction,TImplementation}" />.
        /// </summary>
        /// <param name="createDataConnection">The delegate that initializes a new <see cref="DataConnection"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="createDataConnection" /> is null.</exception>
        public ReadOnlySessionFactory(Func<DataConnection> createDataConnection) : base(createDataConnection) { }
    }
}