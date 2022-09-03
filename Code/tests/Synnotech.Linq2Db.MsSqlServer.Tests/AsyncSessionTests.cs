using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Synnotech.DatabaseAbstractions;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.Linq2Db.MsSqlServer.Tests;

public sealed class AsyncSessionTests : BaseMsSqlIntegrationTest
{
    public AsyncSessionTests(ITestOutputHelper output) : base(output) { }

    [SkippableFact]
    public async Task LoadAndUpdateData()
    {
        SkipTestIfNecessary();

        var container = PrepareContainer().AddSessionFactoryFor<IEmployeeSession, EmployeeSession>()
                                          .BuildServiceProvider();
        var sessionFactory = container.GetRequiredService<ISessionFactory<IEmployeeSession>>();

        const string newName = "Margaret Doe";
        await using (var session = await sessionFactory.OpenSessionAsync())
        {
            var noLongerJohn = await session.GetEmployeeAsync(1);
            noLongerJohn.Name = newName;
            await session.UpdateEmployeeAsync(noLongerJohn);
            await session.SaveChangesAsync();
        }

        await using (var session = await sessionFactory.OpenSessionAsync())
        {
            var margaret = await session.GetEmployeeAsync(1);
            margaret.Name.Should().Be(newName);
        }
    }

    private interface IEmployeeSession : IAsyncSession
    {
        Task<Employee> GetEmployeeAsync(int id);

        Task UpdateEmployeeAsync(Employee employee);
    }

    private sealed class EmployeeSession : AsyncSession, IEmployeeSession
    {
        public EmployeeSession(DataConnection dataConnection) : base(dataConnection) { }

        public Task<Employee> GetEmployeeAsync(int id) =>
            DataConnection.GetTable<Employee>().FirstAsync(e => e.Id == id);

        public Task UpdateEmployeeAsync(Employee employee) =>
            DataConnection.UpdateAsync(employee);
    }
}