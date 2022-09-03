using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.Linq2Db.MsSqlServer.Tests;

public sealed class AsyncTransactionalSessionTests : BaseMsSqlIntegrationTest
{
    public AsyncTransactionalSessionTests(ITestOutputHelper output) : base(output) { }

    [SkippableFact]
    public async Task LoadAndSaveData()
    {
        SkipTestIfNecessary();

        var container = PrepareContainer().AddTransient<Session>()
                                          .BuildServiceProvider();

        int newEmployeeId;
        await using (var session = container.GetRequiredService<Session>())
        await using (var transaction = await session.BeginTransactionAsync())
        {
            var newEmployee = new Employee { Name = "Mr. X", Age = 142 };
            newEmployeeId = await session.InsertEmployeeAsync(newEmployee);
            await transaction.CommitAsync();
        }

        await using (var session = container.GetRequiredService<Session>())
        {
            var employee = await session.GetEmployeeAsync(newEmployeeId);

            var expectedEmployee = new Employee { Name = "Mr. X", Age = 142, Id = newEmployeeId };
            employee.Should().BeEquivalentTo(expectedEmployee);
        }
    }

    private sealed class Session : AsyncTransactionalSession
    {
        public Session(DataConnection dataConnection) : base(dataConnection) { }

        public Task<Employee> GetEmployeeAsync(int id) =>
            DataConnection.GetTable<Employee>().FirstAsync(e => e.Id == id);

        public Task<int> InsertEmployeeAsync(Employee employee) =>
            DataConnection.InsertWithInt32IdentityAsync(employee);
    }
}