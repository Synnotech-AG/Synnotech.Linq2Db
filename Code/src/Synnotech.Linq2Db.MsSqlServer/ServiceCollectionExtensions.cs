using System;
using Light.GuardClauses;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synnotech.Core.DependencyInjection;
using static Synnotech.Linq2Db.ServiceCollectionExtensions;

namespace Synnotech.Linq2Db.MsSqlServer;

/// <summary>
/// Provides extension methods to setup Linq2Db in a DI Container that supports <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers several Linq2Db types with the DI container, especially a <see cref="DataConnection" /> (using a transient lifetime by default). The data connection
    /// is instantiated by passing a singleton instance of <see cref="LinqToDBConnectionOptions" /> which is created from <see cref="Linq2DbSettings" />.
    /// The latter is also available as a singleton and retrieved from the <see cref="IConfiguration" /> instance (which should already be registered with the DI container).
    /// Then a <see cref="IDataProvider" /> using Microsoft.Data.SqlClient internally is created and registered as a singleton as well.
    /// </summary>
    /// <param name="services">The collection that is used to register all necessary types with the DI container.</param>
    /// <param name="mappingSchema">
    /// The mapping schema that describes how entities are mapped to the database.
    /// </param>
    /// <param name="configurationSectionName">The name of the configuration section that is used to retrieve the <see cref="Linq2DbSettings"/>.</param>
    /// <param name="sqlServerProvider">
    /// The underlying provider that is used (optional). You can choose between System.Data.SqlClient (the legacy provider, also part of the .NET Base Class Library)
    /// or Microsoft.Data.SqlClient (the newest version which will also receive new updates).
    /// </param>
    /// <param name="dataConnectionLifetime">
    /// The lifetime that is used for the data connection (optional). The default value is <see cref="ServiceLifetime.Transient" />. If you want to, you
    /// can exchange it with <see cref="ServiceLifetime.Scoped" />.
    /// </param>
    /// <param name="registerFactoryDelegateForDataConnection">
    /// The value indicating whether a Func&lt;DataConnection> should also be registered with the DI container (optional). The default value is true.
    /// You can set this value to false if you use a proper DI container like LightInject that offers function factories. https://www.lightinject.net/#function-factories
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddLinq2DbForSqlServer(this IServiceCollection services,
                                                            MappingSchema? mappingSchema = null,
                                                            SqlServerProvider sqlServerProvider = SqlServerProvider.SystemDataSqlClient,
                                                            string configurationSectionName = Linq2DbSettings.DefaultSectionName,
                                                            ServiceLifetime dataConnectionLifetime = ServiceLifetime.Transient,
                                                            bool? registerFactoryDelegateForDataConnection = null)
    {
        services.MustNotBeNull(nameof(services));

        services.AddSingleton(container => Linq2DbSettings.FromConfiguration(container.GetRequiredService<IConfiguration>(), configurationSectionName))
                .AddSingleton(container => CreateSqlServerDataProvider(container.GetRequiredService<Linq2DbSettings>().SqlServerVersion, sqlServerProvider))
                .AddSingleton(container =>
                 {
                     var settings = container.GetRequiredService<Linq2DbSettings>();
                     return CreateLinq2DbConnectionOptions(container.GetRequiredService<IDataProvider>(),
                                                           mappingSchema,
                                                           settings.ConnectionString,
                                                           settings.TraceLevel,
                                                           container.GetService<ILogger<DataConnection>>());
                 })
                .Add(new ServiceDescriptor(typeof(DataConnection), container => new DataConnection(container.GetRequiredService<LinqToDBConnectionOptions>()), dataConnectionLifetime));
        if (ContainerSettingsContext.Settings.CheckIfFactoryDelegateShouldBeRegistered(registerFactoryDelegateForDataConnection))
            services.AddSingleton<Func<DataConnection>>(container => container.GetRequiredService<DataConnection>);
        return services;
    }

    /// <summary>
    /// Creates an <see cref="IDataProvider" /> that uses Microsoft.Data.SqlClient internally.
    /// </summary>
    /// <param name="sqlServerVersion">The SQL Server version of the target database (optional). Defaults to <see cref="SqlServerVersion.v2017" />.</param>
    /// <param name="sqlServerProvider">
    /// The underlying provider that is used (optional). You can choose between System.Data.SqlClient (the legacy provider, also part of the .NET Base Class Library)
    /// or Microsoft.Data.SqlClient (the newest version which will also receive new updates). We recommend that you use the latter unless you have known issues
    /// about it.
    /// </param>
    public static IDataProvider CreateSqlServerDataProvider(SqlServerVersion sqlServerVersion = SqlServerVersion.v2017,
                                                            SqlServerProvider sqlServerProvider = SqlServerProvider.SystemDataSqlClient)
    {
        var dataProvider = SqlServerTools.GetDataProvider(sqlServerVersion, sqlServerProvider);
        return dataProvider;
    }
}