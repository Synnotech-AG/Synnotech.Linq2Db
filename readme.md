# Synnotech.Linq2Db

*Extensions for LinkToDB that make your data access code easier*

[![Synnotech Logo](synnotech-large-logo.png)](https://www.synnotech.de/)


[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/Synnotech-AG/Synnotech.Linq2Db/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-7.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages?q=Synnotech.Linq2Db/)

# How to install

The Synnotech.Linq2Db packages are compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)and .NET Framework 4.6.2, thus supporting all major plattforms like .NET 6, .NET Core, .NET Framework 4.6.2 or newer, Mono, Xamarin, UWP, or Unity.

There are several packages available:

- Synnotech.Linq2Db implements the abstractions of [Synnotech.DatabaseAbstractions 3.x](https://github.com/synnotech-AG/synnotech.DatabaseAbstractions) in a database-agnostic way.
- [Synnotech.Linq2Db.MsSqlServer](https://github.com/Synnotech-AG/Synnotech.Linq2Db/tree/main/Code/src/Synnotech.Linq2Db.MsSqlServer) provides additional features that target Microsoft SQL Server and offers better integration, especially with apps that use `IServiceCollection`.

We recommend to use a package that targets your specific database server - if there is none for your purpuse, please [create an issue](https://github.com/Synnotech-AG/Synnotech.Linq2Db/issues) so that we can add it to our code base. Alternatively, you can simply reference Synnotech.Linq2Db and compose it by yourself.

# Writing custom sessions

When writing code that performs I/O, we usually write custom abstractions, containing a single method for each I/O request. The following sections show you how to design abstractions, implement them, and call them in client code.

## Sessions that only read data

The following code snippets show the example for an ASP.NET Core controller that represents an HTTP GET operation for contacts.

Your I/O abstraction should simply derive from `IAsyncReadOnlySession` and offer the corresponding I/O call to load contacts:

```csharp
public interface IGetContactsSession : IAsyncReadOnlySession
{
    Task<List<Contact>> GetContactsAsync(int skip, int take);
}
```

To implement this interface, you should derive from the `AsyncReadOnlySession` class of Synnotech.Linq2Db:

```csharp
public sealed class LinqToDbGetContactsSession : AsyncReadOnlySession, IGetContactsSession
{
    public LinqToDbGetContactsSession(DataConnection dataConnection) : base(dataConnection) { }

    public Task<List<Contact>> GetContactsAsync(int skip, int take) =>
        DataConnection.GetTable<Contacts>()
                      .OrderBy(contact => contact.LastName)
                      .Skip(skip)
                      .Take(take)
                      .ToListAsync();
}
```

`AsyncReadOnlySession` implements `IAsyncReadOnlySession`, `IDisposable` and `IAsyncDisposable` for you and provides LinqToDb's `DataConnection` via a protected property (the one that is passed in via constructor injection). This reduces the code you need to write in your session for your specific use case.

You can then consume your session via the abstraction in client code. Check out the following ASP.NET Core controller for example:

```csharp
[ApiController]
[Route("api/contacts")]
public sealed class GetContactsController : ControllerBase
{
    public GetContactsController(ISessionFactory<IGetContactsSession> sessionFactory) =>
        SessionFactory = sessionFactory;
        
    private ISessionFactory<IGetContactsSession> SessionFactory { get; }
    
    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetContacts(int skip, int take)
    {
        if (this.CheckPagingParametersForErrors(skip, take, out var badResult))
            return badResult;
        
        await using var session = await SessionFactory.OpenSessionAsync();
        var contacts = await session.GetContactsAsync(skip, take);
        return ContactDto.FromContacts(contacts);
    }
}
```

In this example, an `ISessionFactory<IGetContactsSession>` is injected into the controller. This factory is used to instantiate the session once the parameters are validated. After that, the contacts are retrieved via `await session.GetContactsAsync(skip, take)`, transformed to DTOs and returned from the controller.

For this to work, you must register your session factory with the DI container:

```csharp
// This call will perform the following registrations (with the default sttings):
// services.AddTransient<IGetContactsSession, LinqToDbGetContactsSession>()>();
// services.AddSingleton<ISessionFactory<IGetContactsSession>, SessionFactory<IGetContactsSession>>();
// services.AddSingleton<Func<IGetContactsSession>>(c => c.GetRequiredService<IGetContactsSession>);
services.AddSessionFactoryFor<IGetContactsSession, LinqToDbGetContactsSession>();
```

## Sessions that use a single transaction

If you want to insert, update or delete data, then you usually want to use a single transaction for your database commands. You can use the `IAsyncSession` interface for these scenarios and implement your custom session by deriving from `AsyncSession`.

The abstraction might look like this:

```csharp
public interface IUpdateContactSession : IAsyncSession
{
    Task<Contact?> GetContactAsync(int id);

    Task UpdateContactAsync(Contact contact);
}
```

The class that implements this interface should derive from `AsyncSession` which provides the same members as `AsyncReadOnlySession` plus a `SaveChangesAsync` method that commits the internal transaction:

```csharp
public sealed class LinqToDbUpdateContactSession : AsyncSession, IUpdateContactSession
{
    public LinqToDbUpdateContactSession(DataConnection dataConnection) : base(dataConnection) { }

    public Task<Contact?> GetContactAsync(int id) =>
#nullable disable
        DataConnection.GetTable<Contact>()
                      .FirstOrDefaultAsync(contact => contact.Id == id);
#nullable restore

    public Task UpdateContactAsync(Contact contact) => DataConnection.UpdateAsync(contact);
}
```

You should register a factory for your session with your DI container, the same way as we did it for the read-only session:

```csharp
services.AddSessionFactoryFor<IUpdateContactSession, LinqToDbUpdateContactSession>();
```

Your controller could then use the factory to open the session asynchronously:

```csharp
[ApiController]
[Route("api/contacts/update")]
public sealed class UpdateContactController : ControllerBase
{
    public UpdateContactController(ISessionFactory<IUpdateContactSession> sessionFactory,
                                   ContactValidator validator)
    {
        SessionFactory = sessionFactory;
        Validator = validator;
    }
    
    private ISessionFactory<IUpdateContactSession> SessionFactory { get; }
    private ContactValidator Validator { get; }
    
    [HttpPut]
    public async Task<IActionResult> UpdateContact(ContactDto contactDto)
    {
        if (this.CheckForErrors(contactDto, Validator, out var badResult))
            return badResult;
            
        // The session factory opens the connection asynchronously and starts a transaction.
        // Disposing the session will automatically rollback the transaction if SaveChangesAsync
        // is not called.
        await using var session = await SessionFactory.OpenSessionAsync();
        var contact = await session.GetContactAsync(contactDto.Id);
        if (contact == null)
            return NotFound();
        contactDto.UpdateContact(contact); // Or use an object-to-object mapper
        await session.UpdateContactAsync(contact);
        await session.SaveChangesAsync(); // This commits the underlying transaction
        return NoContent();
    }
}
```

Please note: the session factory will resolve a session instance (from the DI container), asynchronously open a connection to the target database and then start a transaction asynchronously. The session factory also supports scenarios when the session is registered with a scoped lifetime (the session is then only initialized once). However, we recommend that you use a transient lifetime as we argue that it is the controller's responsibility to begin and end the database session.

## Sessions with several transactions

If you need to handle transactions individually, (e.g. because you want to handle a large amount of data in batches and have a transaction per batch), you can derive from the `IAsyncTransactionalSession` interface:

```csharp
public interface IUpdateProductsSession : IAsyncTransactionalSession
{
    Task<int> GetProductCountAsync();

    Task<List<Product>> GetProductBatchAsync(int skip, int take);

    Task UpdateProductAsync(Product product);
}
```

`IAsyncTransactionalSession` has no `SaveChangesAsync` method, but a `BeginTransactionAsync` method that you can use to start individual transactions.

The implementation of this session could look like this:

```csharp
public sealed class LinqToDbUpdateProductsSession : AsyncTransactionalSession, IUpdateProductsSession
{
    public LinqToDbUpdateProductsSession(DataConnection dataConnection) : base(dataConnection) { }

    public Task<int> GetProductsCountAsync() => DataConnection.GetTable<Product>().CountAsync();

    public Task<List<Product>> GetProductBatchAsync(int skip, int take) =>
        DataConnection.GetTable<Product>()
                      .OrderBy(product => product.Id)
                      .Skip(skip)
                      .Take(take)
                      .ToListAsync();
    
    public Task UpdateProductAsync(Product product) => DataConnection.UpdateAsync(product);
}
```

Your job that updates all products might look like this:

```csharp
public sealed class UpdateAllProductsJob
{
    public UpdateAllProductsJob(ISessionFactory<IUpdateProductsSession> sessionFactory, ILogger logger)
    {
        SessionFactory = sessionFactory;
        Logger = logger;
    }
    
    private ISessionFactory<IUpdateProductsSession> SessionFactory { get; }
    private ILogger Logger { get; }

    public async Task UpdateProductsAsync()
    {
        await using var session = await SessionFactory.OpenSessionAsync();
        var numberOfProducts = await session.GetProductsCountAsync();
        const int batchSize = 100;
        var skip = 0;
        while (skip < numberOfProducts)
        {
            IAsyncTransaction? transaction = null;
            try
            {
                transaction = session.BeginTransactionAsync();
                var products = session.GetProductBatchAsync(skip, batchSize);
                foreach (var product in products)
                {
                    if (product.TryPerformDailyUpdate(Logger))
                        await session.UpdateProductAsync(product);
                }

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Batch {From} to {To} could not be updated properly", skip + 1, batchSize + skip);
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }

            skip += batchSize;
        }
    }
}
```

In the example above, the job get an `ISessionFactory<IUpdateProductsSession>` that can be used to create a session. In `UpdateProductsAsync`, the session is created and the number of products is determined. The products are then updated in batches with size 100. In each batch, a new transaction is started and committed at the end. The transaction is disposed in the finally block before a new batch begins.

For this to work, you must register the session factory with the DI container:

```csharp
services.AddSessionFactoryFor<IUpdateProductsSession, LinqToDbUpdateProductsSession>();
```

*Please keep in mind*: neither LinqToDB nor Synnotech.DatabaseAbstractions support nested transactions. You should always start a single transaction, commit and/or dispose it, and only afterwards create a new transaction. If you create a new transaction before committing it, your current transaction will be disposed (and implicitly rolled back, this is how LinqToDB 3.4.3 is implemented internally).

# Customizing Synnotech.Linq2Db

## Deriving from DataConnection

Some projects like to derive from LinqToDB's `DataConnection`, e.g. to provide properties for each entity. This pattern is similar to Entity Frameowork's `DbContext`.

```csharp
public sealed class MyContext : DataConnection
{
    public MyContext(LinqToDbConnectionOptions options) : base(options) { }

    public ITable<Contact> Contacts => GetTable<Contact>();

    public ITable<Product> Products => GetTable<Product>();
}
```

If you want to write custom sessions, you can derive from `AsyncReadOnlySession<MyContext>`, `AsyncSession<MyContext>` or `AsyncTransactionalSession<MyContext>`. This will allow you to use an instance of your subclass when querying the database.

*Please note*: we do not recommend that you use this pattern. Simply programming against `DataConnection` and calling `GetTable` within your custom sessions is easier.

## Customizing the Session Factory

When you register a session factory using `services.AddSessionFactoryFor`, you have the following parameters at your disposal:

- `sessionLifetime`: the lifetime that is used to register your session with the DI container. The default is transient. You can also choose scoped if you want the DI container to dispose of the session and inject the same intance several times during a DI container scope. `SessionFactory<T>` supports these scenarios.
- `factoryLifetime`: the life time of the `SessionFactory<T>`. The default value is singleton. You could choose another lifetime if you want the GC to grab a session factory when it is not in use.
- `registerCreateSessionDelegate`: the value indicating if a `Func<TSessionAbstraction>` should also be registered with the DI container. This delegate is necessary for the session factory to resolve the session from the DI container. If you use a sophisticated DI container like [LightInject](https://www.lightinject.net/) that offers [Function Factories](https://www.lightinject.net/#function-factories), you can (and should) set this parameter to false.

## Using a transaction in AsyncReadOnlySession

By default, `AsyncReadOnlySession<T>` will not create a transaction explicitly. However, in some scenarios, you might want to create a transaction nonetheless even if you only read data. You can do this by deriving from `AsyncReadOnlySession<T>` (or `AsyncReadOnlySession`) and supply an isolation value as the second parameter to the base constructor call:

```csharp
public class MySession : AsyncReadOnlySession, IMySession
{
    public MySession(DataConnection dataConnection) : base(dataConnection, IsolationLevel.ReadUncommitted) { }

    // Other members omitted for brevity's sake
}
```

In the code sample above, the `AsyncReadOnlySession` will create a transaction when instatiated via `ISessionFactory<IMySession>`:

```csharp
// In your composition root:
services.AddSessionFactoryFor<IMySession, MySession>();

// When instantiating your session:
await using var session = await SessionFactory.OpenSessionAsync(); // this call will start the transaction asynchronously
```

The transaction will always be rolled back when your session goes out of scope.

A scenario where you might want to do this is the following: consider that you are having a long running transaction in MS SQL Server that also updates or inserts data. All other read-only calls to the database will be blocked as long as they touch one or more records that were also created / manipulated in the long running transaction. This will block every call, unless these read-only database calls are wrapped in read-uncommited transactions themselves. However, in MS SQL Server, this is not the default behavior: if you do not specify a dedicated transaction, each statement / command will be wrapped in a read-committed transaction.

In general, we recommend to avoid this setting. Only use it if you have a special use case for it.

# General recommendations

1. All I/O should be abstracted. You should create abstractions that are specific for your use cases.
2. Your custom abstractions should derive from `IAsyncReadOnlySession` (when they only read data from SQL Server) or from `IAsyncSession` (when they also manipulate data and therefore need a transaction). Only use `IAsyncTransactionalSession` when you need to handle several transactions within a single session.
3. Prefer async I/O over sync I/O. Threads that wait for a database query to complete can handle other requests in the meantime when the query is performed asynchronously. This prevents thread starvation under high load and allows your web service to scale better. Synnotech.Linq2Db currently does not support synchronous sessions for this reason.
4. In case of web apps, we do not recommend using the DI container to dispose of the session. Instead, it is the controller's responsibility to do that. This way you can easily test the controller without running the whole ASP.NET Core infrastructure in your tests. To make your life easier, use an appropriate DI container like [LightInject](https://github.com/seesharper/LightInject) instead of Microsoft.Extensions.DependencyInjection. These more sophisticated DI containers provide you with more features, e.g. [Function Factories](https://www.lightinject.net/#function-factories).