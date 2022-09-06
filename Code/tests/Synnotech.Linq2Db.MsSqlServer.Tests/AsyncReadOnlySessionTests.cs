using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Synnotech.DatabaseAbstractions;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.Linq2Db.MsSqlServer.Tests;

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

        CheckLoadedEmployees(employees);
    }

    [SkippableFact]
    public async Task LoadDataWithSessionFactory()
    {
        SkipTestIfNecessary();

        var sessionFactory = PrepareContainer().AddSessionFactoryFor<IEmployeeSession, EmployeeSession>()
                                               .BuildServiceProvider()
                                               .GetRequiredService<ISessionFactory<IEmployeeSession>>();
        await using var session = await sessionFactory.OpenSessionAsync();
        var employees = await session.GetEmployeesAsync();

        CheckLoadedEmployees(employees);
    }

    [SkippableFact]
    public async Task LoadDataWithExplicitTransaction()
    {
        SkipTestIfNecessary();

        var sessionFactory = PrepareContainer().AddSessionFactoryFor<IEmployeeSession, SessionWithTransactions>()
                                               .BuildServiceProvider()
                                               .GetRequiredService<ISessionFactory<IEmployeeSession>>();
        await using var session = await sessionFactory.OpenSessionAsync();
        var employees = await session.GetEmployeesAsync();

        CheckLoadedEmployees(employees);
    }

    private static void CheckLoadedEmployees(List<Employee>? employees)
    {
        var expectedEmployees = new[]
        {
            new Employee { Id = 1, Name = "John Doe", Age = 42 },
            new Employee { Id = 2, Name = "Jane Kingsley", Age = 29 },
            new Employee { Id = 3, Name = "Audrey McGinnis", Age = 39 }
        };
        employees.Should().BeEquivalentTo(expectedEmployees, options => options.WithStrictOrdering());
    }

    private interface IEmployeeSession : IAsyncReadOnlySession
    {
        Task<List<Employee>> GetEmployeesAsync();
    }

    private sealed class EmployeeSession : AsyncReadOnlySession, IEmployeeSession
    {
        public EmployeeSession(DataConnection dataConnection) : base(dataConnection) { }

        public Task<List<Employee>> GetEmployeesAsync() => DataConnection.GetTable<Employee>().ToListAsync();
    }

    private sealed class SessionWithTransactions : AsyncReadOnlySession, IEmployeeSession
    {
        public SessionWithTransactions(DataConnection dataConnection) : base(dataConnection, IsolationLevel.ReadUncommitted) { }

        public Task<List<Employee>> GetEmployeesAsync() => DataConnection.GetTable<Employee>().ToListAsync();
    }
}