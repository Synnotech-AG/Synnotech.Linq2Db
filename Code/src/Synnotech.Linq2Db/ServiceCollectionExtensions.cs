using System;
using System.Diagnostics;
using Light.GuardClauses;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.Linq2Db
{
    /// <summary>
    /// Provides extensions methods to setup LinqToDB in a DI Container that supports <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ISessionFactory{TSessionAbstraction}" /> for the specified session. You can inject this session factory
        /// into client code to resolve your session asynchronously. When resolved, a new data connection is created, a connection to
        /// the target database is opened asynchronously, and a transaction is started. The data connection is then passed to your
        /// custom session type. See <see cref="SessionFactory{TAbstraction,TImplementation,TDataConnection}" /> for details.
        /// <code>
        /// public class MySessionClient
        /// {
        ///     public MySessionClient(ISessionFactory&lt;IMySession> sessionFactory) =>
        ///         SessionFactory = sessionFactory;
        /// 
        ///     // IMySession must derive from IAsyncSession
        ///     private ISessionFactory&lt;IMySession> SessionFactory { get; }
        /// 
        ///     public async Task SomeMethod()
        ///     {
        ///         await using var session = await SessionFactory.OpenSessionAsync();
        ///         // do something useful with your session
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="TAbstraction">The interface that your session implements. It must implement <see cref="IAsyncSession" />.</typeparam>
        /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O. It must derive from <see cref="AsyncSession{TDataConnection}" />.</typeparam>
        /// <typeparam name="TDataConnection">Your custom data connection subtype that you use in your solution.</typeparam>
        /// <param name="services">The collection that holds all registrations for the DI container.</param>
        /// <param name="factoryLifetime">The lifetime for the session factory. It's usually ok for them to be a singleton.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
        public static IServiceCollection AddSessionFactoryFor<TAbstraction, TImplementation, TDataConnection>(this IServiceCollection services, ServiceLifetime factoryLifetime = ServiceLifetime.Singleton)
            where TAbstraction : IAsyncSession
            where TImplementation : AsyncSession<TDataConnection>, TAbstraction, new()
            where TDataConnection : DataConnection
        {
            services.MustNotBeNull(nameof(services))
                    .Add(new ServiceDescriptor(typeof(ISessionFactory<TAbstraction>), typeof(SessionFactory<TAbstraction, TImplementation, TDataConnection>), factoryLifetime));
            return services;
        }

        /// <summary>
        /// Registers an <see cref="ISessionFactory{TAbstraction}" /> for the specified session. You can inject this session factory
        /// into client code to resolve your session asynchronously. When resolved, a new data connection is created, a connection to
        /// the target database is opened asynchronously, and a transaction is started. The data connection is then passed to your
        /// custom session type. See <see cref="SessionFactory{TAbstraction,TImplementation,TDataConnection}" /> for details.
        /// <code>
        /// public class MySessionClient
        /// {
        ///     public MySessionClient(ISessionFactory&lt;IMySession> sessionFactory) =>
        ///         SessionFactory = sessionFactory;
        /// 
        ///     // IMySession must derive from IAsyncSession
        ///     private ISessionFactory&lt;IMySession> SessionFactory { get; }
        /// 
        ///     public async Task SomeMethod()
        ///     {
        ///         await using var session = await SessionFactory.OpenSessionAsync();
        ///         // do something useful with your session
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="TAbstraction">The interface that your session implements. It must implement <see cref="IAsyncSession" />.</typeparam>
        /// <typeparam name="TImplementation">The Linq2Db session implementation that performs the actual database I/O. It must derive from <see cref="AsyncSession{TDataConnection}" />.</typeparam>
        /// <param name="services">The collection that holds all registrations for the DI container.</param>
        /// <param name="factoryLifetime">The lifetime for the session factory. It's usually ok for them to be a singleton.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
        public static IServiceCollection AddSessionFactoryFor<TAbstraction, TImplementation>(this IServiceCollection services, ServiceLifetime factoryLifetime = ServiceLifetime.Singleton)
            where TAbstraction : IAsyncSession
            where TImplementation : AsyncSession, TAbstraction, new() =>
            services.AddSessionFactoryFor<TAbstraction, TImplementation, DataConnection>(factoryLifetime);

        /// <summary>
        /// Uses an <see cref="ILogger" /> instance to log a Linq2Db data connection trace message.
        /// The different trace levels are mapped to the different log levels.
        /// </summary>
        public static void LogLinq2DbMessage(this ILogger logger, string? message, string? category, TraceLevel traceLevel)
        {
            logger.MustNotBeNull(nameof(logger));
            if (message.IsNullOrWhiteSpace())
                return;

            switch (traceLevel)
            {
                case TraceLevel.Off:
                    break;
                case TraceLevel.Error:
                    logger.LogError(message);
                    break;
                case TraceLevel.Warning:
                    logger.LogWarning(message);
                    break;
                case TraceLevel.Info:
                    logger.LogInformation(message);
                    break;
                case TraceLevel.Verbose:
                    logger.LogDebug(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(traceLevel), traceLevel, $"The trace level \"{traceLevel}\" is unknown.");
            }
        }

        /// <summary>
        /// Creates the default <see cref="LinqToDbConnectionOptions" />. <paramref name="traceLevel" /> and <paramref name="logger" />
        /// are optional but need to be set together if a level other than <see cref="TraceLevel.Off" /> is used.
        /// </summary>
        /// <param name="dataProvider">The Linq2Db data provider used to create database-specific queries.</param>
        /// <param name="connectionString">The connection string for the target database.</param>
        /// <param name="traceLevel">The level that is used to log data connection messages (optional). Defaults to <see cref="TraceLevel.Off" />.</param>
        /// <param name="logger">The logger for <see cref="DataConnection" /> when <paramref name="traceLevel" /> is set to a value other than <see cref="TraceLevel.Off" />.</param>
        /// <exception cref="NullReferenceException">Thrown when <paramref name="dataProvider" /> or <paramref name="connectionString" /> are null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="traceLevel" /> is set to a value other than <see cref="TraceLevel.Off" /> and <paramref name="logger" /> is null -
        /// or when <paramref name="connectionString" /> is an empty string or contains only white space.
        /// </exception>
        public static LinqToDbConnectionOptions CreateLinq2DbConnectionOptions(IDataProvider dataProvider,
                                                                               string connectionString,
                                                                               TraceLevel traceLevel = TraceLevel.Off,
                                                                               ILogger<DataConnection>? logger = null)
        {
            dataProvider.MustNotBeNull(nameof(dataProvider));
            connectionString.MustNotBeNullOrWhiteSpace(nameof(connectionString));

            var optionsBuilder = new LinqToDbConnectionOptionsBuilder().UseConnectionString(dataProvider, connectionString)
                                                                       .WithTraceLevel(traceLevel);

            if (traceLevel == TraceLevel.Off)
                return optionsBuilder.Build();

            if (logger == null)
                throw new ArgumentException($"You must provide a logger when traceLevel is set to \"{traceLevel}\".", nameof(logger));

            return optionsBuilder.WriteTraceWith(logger.LogLinq2DbMessage)
                                 .Build();
        }
    }
}