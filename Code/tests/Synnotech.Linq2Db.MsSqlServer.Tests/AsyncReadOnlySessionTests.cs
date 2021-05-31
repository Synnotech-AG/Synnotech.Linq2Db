using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.Linq2Db.MsSqlServer.Tests
{
    public sealed class AsyncReadOnlySessionTests : BaseMsSqlIntegrationTest
    {
        public AsyncReadOnlySessionTests(ITestOutputHelper output) : base(output) { }

        [SkippableFact]
        public async Task LoadData()
        {
            SkipTestIfNecessary();

            await using var session = PrepareContainer().AddTransient<EmployeeSession>()
                                                        .BuildServiceProvider()
                                                        .GetRequiredService<EmployeeSession>();

            var employees = await session.GetEmployeesAsync();

            var expectedEmployees = new[]
            {
                new Employee { Id = 1, Name = "John Doe", Age = 42 },
                new Employee { Id = 2, Name = "Jane Kingsley", Age = 29 },
                new Employee { Id = 3, Name = "Audrey McGinnis", Age = 39 }
            };
            employees.Should().BeEquivalentTo(expectedEmployees, options => options.WithStrictOrdering());
        }

        private sealed class EmployeeSession : AsyncReadOnlySession
        {
            public EmployeeSession(DataConnection dataConnection) : base(dataConnection) { }

            public Task<List<Employee>> GetEmployeesAsync() => DataConnection.GetTable<Employee>().ToListAsync();
        }
    }
}