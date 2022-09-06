using System;
using System.Diagnostics;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using LinqToDB.DataProvider.SqlServer;
using Microsoft.Extensions.Configuration;

namespace Synnotech.Linq2Db.MsSqlServer;

/// <summary>
/// Represents the default settings for Linq2Db that are used in a setup with Microsoft SQL Server.
/// </summary>
public class Linq2DbSettings
{
    /// <summary>
    /// The default section name within the <see cref="IConfiguration" /> where settings are loaded from.
    /// </summary>
    public const string DefaultSectionName = "database";

    /// <summary>
    /// Gets or sets the connection string to the target database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target version that the Linq2Db data provider will target. The default value is SqlServerVersion.v2017.
    /// </summary>
    public SqlServerVersion SqlServerVersion { get; set; } = SqlServerVersion.v2017;

    /// <summary>
    /// Gets or sets the value indicating whether SQL statements will be logged. The default value is Off.
    /// </summary>
    public TraceLevel TraceLevel { get; set; } = TraceLevel.Off;

    /// <summary>
    /// Loads the <see cref="Linq2DbSettings"/> settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance where the settings are loaded from.</param>
    /// <param name="sectionName">The name of the section that represents the Linq2Db settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="sectionName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sectionName"/> is an empty string or contains only whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the settings could not be loaded (most likely because the section is not present in the configuration).</exception>
    public static Linq2DbSettings FromConfiguration(IConfiguration configuration, string sectionName = DefaultSectionName) =>
        FromConfiguration<Linq2DbSettings>(configuration, sectionName);

    /// <summary>
    /// Loads the Linq2Db settings from configuration.
    /// </summary>
    /// <typeparam name="T">The type of Linq2Db settings that will be used to load the settings.</typeparam>
    /// <param name="configuration">The configuration instance where the settings are loaded from.</param>
    /// <param name="sectionName">The name of the section that represents the Linq2Db settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="sectionName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sectionName"/> is an empty string or contains only whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the settings could not be loaded (most likely because the section is not present in the configuration).</exception>
    public static T FromConfiguration<T>(IConfiguration configuration, string sectionName = DefaultSectionName)
    {
        configuration.MustNotBeNull(nameof(configuration));
        sectionName.MustNotBeNullOrWhiteSpace(nameof(sectionName));
        return configuration.GetSection(sectionName)
                            .Get<T?>() ?? throw new InvalidConfigurationException($"Linq2Db settings could not be retrieved from configuration section \"{sectionName}\".");
    }
}