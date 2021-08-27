using System.Threading.Tasks;
using Light.EmbeddedResources;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Synnotech.MsSqlServer;
using Synnotech.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.Linq2Db.MsSqlServer.Tests
{
    public abstract class BaseMsSqlIntegrationTest : IAsyncLifetime
    {
        protected BaseMsSqlIntegrationTest(ITestOutputHelper output) => Logger = output.CreateTestLogger();

        private static bool AreDatabaseTestsEnabled => TestSettings.Configuration.GetValue<bool>("database:areTestsEnabled");

        private ILogger? Logger { get; }

        protected static string ConnectionString
        {
            get
            {
                var connectionString = TestSettings.Configuration["database:connectionString"];
                if (connectionString.IsNullOrWhiteSpace())
                    throw new InvalidConfigurationException("You must set \"database:connectionString\" when \"database:areTestsEnabled\" is set to true in testsettings.");
                return connectionString;
            }
        }

        public async Task InitializeAsync()
        {
            if (!AreDatabaseTestsEnabled)
                return;

            await Database.DropAndCreateDatabaseAsync(ConnectionString);
            await Database.ExecuteNonQueryAsync(ConnectionString, this.GetEmbeddedResource("Database.sql"));
        }

        public Task DisposeAsync() => Task.CompletedTask;

        protected static void SkipTestIfNecessary() => Skip.IfNot(AreDatabaseTestsEnabled);

        protected IServiceCollection PrepareContainer()
        {
            var services = new ServiceCollection().AddLinq2DbForSqlServer(DatabaseMappings.CreateMappings)
                                                  .AddSingleton(TestSettings.Configuration);
            if (Logger != null)
                services.AddLogging(builder => builder.AddSerilog(Logger));
            return services;
        }
    }
}