using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Represents a factory that instantiates a session and optionally initializes it
    /// in an asynchronous fashion when the session implements <see cref="IInitializeAsync" />.
    /// </summary>
    /// <typeparam name="T">The abstraction that your session implements.</typeparam>
    public sealed class SessionFactory<T> : ISessionFactory<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SessionFactory{TAbstraction}" />.
        /// </summary>
        /// <param name="getSession">The delegate that resolves the session instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getSession" /> is null.</exception>
        public SessionFactory(Func<T> getSession) =>
            GetSession = getSession.MustNotBeNull(nameof(getSession));

        private Func<T> GetSession { get; }

        /// <summary>
        /// Creates a new data connection, opens a connection to the target database asynchronously
        /// and starts a transaction. The data connection is then passed to a new session instance.
        /// </summary>
        /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
        /// <exception cref="DbException">Thrown when an SQL error occurred when opening the session or starting the transaction.</exception>
        public ValueTask<T> OpenSessionAsync(CancellationToken cancellationToken = default)
        {
            var session = GetSession();
            if (session is IInitializeAsync initializeAsync && !initializeAsync.IsInitialized)
                return InitializeSessionAsync(initializeAsync);
            return new (session);
        }

        private static async ValueTask<T> InitializeSessionAsync(IInitializeAsync session)
        {
            await session.InitializeAsync();
            return (T)session;
        }
    }
}