# Synnotech.Linq2Db

*Extensions for LinkToDB that make your data access code easier*

[![Synnotech Logo](synnotech-large-logo.png)](https://www.synnotech.de/)


[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/Synnotech-AG/Synnotech.Linq2Db/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-4.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages?q=Synnotech.Linq2Db/)

# How to install

The Synnotech.Linq2Db packages are compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major plattforms like .NET 5, .NET Core, .NET Framework 4.6.1 or newer, Mono, Xamarin, UWP, or Unity.

There are several packages available:

- Synnotech.Linq2Db implements the abstractions of [Synnotech.DatabaseAbstractions 2.x](https://github.com/synnotech-AG/synnotech.DatabaseAbstractions) in a database-agnostic way.
- [Synnotech.Linq2Db.MsSqlServer](https://github.com/Synnotech-AG/Synnotech.Linq2Db/tree/main/Code/src/Synnotech.Linq2Db.MsSqlServer) provides additional features that target Microsoft SQL Server and offers better integration, especially with apps that use `IServiceCollection`.

We recommend to use a package that targets your specific database server - if there is none for your purpuse, please [create an issue](https://github.com/Synnotech-AG/Synnotech.Linq2Db/issues) so that we can add it to our code base. Alternatively, you can simply reference Synnotech.Linq2Db and compose it by yourself.

# Writing custom sessions

When writing code that performs I/O, we usually write custom abstractions, containing a single method for each I/O request. The following sections show you how to design abstractions, implement them, and call them in client code.

## Sessions that only read data

The following code snippets show the example for an ASP.NET Core controller that represents an HTTP GET operation for contacts.

Your I/O abstraction should simply derive from `IAsyncReadOnlySession` and offer the corresponding I/O call to load contacts:

```csharp
public interface IGetContactsSession : IAsyncDisposable
{
    Task<List<Contact>> GetContactsAsync(int skip, int take);
}
```

To implement this interface, you should derive from the `AsyncReadOnlySession` class of Synnotech.Linq2Db.MsSqlServer:

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

`AsyncReadOnlySession` implements `IAsyncReadOnlySession`, `IDisposable` and `IAsyncDisposable` for you and provides LinqToDb's `DataConnection` via a protected property. This reduces the code you need to write in your session for your specific use case.

You can then consume your session via the abstraction in client code. Check out the following ASP.NET Core controller for example:

```csharp
[ApiController]
[Route("api/contacts")]
public sealed class GetContactsController : ControllerBase
{
    public GetContactsController(Func<IGetContactsSession> createSession) =>
        CreateSession = createSession;
        
    private Func<IGetContractsSession> CreateSession { get; }
    
    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetContacts(int skip, int take)
    {
        if (this.CheckPagingParametersForErrors(skip, take, out var badResult))
            return badResult;
        
        await using var session = CreateSession();
        var contacts = await session.GetContactsAsync(skip, take);
        return ContactDto.FromContacts(contacts);
    }
}
```

In this example, a `Func<IGetContactsSession>` is injected into the controller. This factory delegate is used to instantiate the session once the parameters are validated. We recommend that you do not register your session as "scoped", but rather as transient with your DI container (because it's the controllers responsibility to properly open and close the session). This allows you to test if the session is disposed correctly without setting up the whole ASP.NET Core ecosystem to instantiate the controller.

For this to work, we suggest that you use a DI container like [LightInject](https://github.com/seesharper/LightInject) that automatically provides you with [function factories](https://www.lightinject.net/#function-factories) once you have registered a type. If you use a DI container that does not support this feature, you can simply register the function factory yourself (typically as a singleton).

```csharp
services.AddTransient<IGetContactsSession, LinqToDbGetContactsSession>();
// The next call is not necessary if your DI container can automatically resolve
// Func<T> when T is already registered. LightInject is able to do this.
services.AddSingleton<Func<IGetContactsSession>>(container => container.GetRequiredService<IGetContactsSession>);
```

## Sessions that manipulate data

If your session requires the `SaveChangesAsync` method, or you want to handle individual transactions, you can derive from the `IAsyncSession` interface or `IAsyncTransactionalSession` interface, respectively. We recommend that you open sessions that derive from `IAsyncSession` via an `ISessionFactory<T>`. All of these APIs support aborting async operations via cancellation tokens.

### Example for updating an existing record with IAsyncSession

The abstraction might look like this:

```csharp
public interface IUpdateContactSession : IAsyncSession
{
    Task<Contact?> GetContactAsync(int id);

    Task UpdateContactAsync(Contact contact);
}
```

The class that implements this interface should derive from `AsyncSession`, which provides the same members as `AsyncReadOnlySession` plus a `SaveChangesAsync` method that commits the internal transaction:

```csharp
public sealed class LinqToDbUpdateContactSession : AsyncSession, IUpdateContactSession
{
    public Task<Contact?> GetContactAsync(int id) =>
#nullable disable
        DataConnection.GetTable<Contact>()
                      .FirstOrDefaultAsync(contact => contact.Id == id);
#nullable restore

    public Task UpdateContactAsync(Contact contact) => DataConnection.UpdateAsync(contact);
}
```

You should register a factory for your session with your DI container:

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

Please note the following things about the session factory:

- `LinqToDbUpdateContactSession` does not have a constructor that takes a `DataConnection`. This is done because the data connection needs to be initialized asynchronously (open the connection asynchronously, start the transaction asynchronously) before being passed
to the `AsyncSession`. However, constructors in C# / .NET cannot run asynchronously. This job is performed by the implementation of `ISessionFactory<T>`. You can opt out of this feature by using the constructor of `AsyncSession` that actually takes a data connection. This data connection must be open and reference a transaction before being passed.
- The implementation for `ISessionFactory<T>` will always create a transient instance of the target session (i.e. your session must have a default constructor). If you don't want transient instances, you need to opt out of `ISessionFactory` (although we do not recommend this - we think it is the controller's responsibility to handle the session during an HTTP request).
- You should only use `ISessionFactory<T>` in scenarios where you use `IAsyncSession`. If your session is read-only (i.e. no transaction required) or when you handle transactions yourself via `IAsyncTransactionalSession`, you will not be able to use the session factory (as asynchronous opening is not required).

### Handling Transactions Individually

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
    public UpdateAllProductsJob(Func<IUpdateProductsSession> createSession, ILogger logger)
    {
        CreateSession = createSession;
        Logger = logger;
    }
    
    private Func<IUpdateProductsSession> CreateSession { get; }
    private ILogger Logger { get; }

    public async Task UpdateProductsAsync()
    {
        await using var session = CreateSession();
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

In the example above, the job gets a delegate `Func<IUpdateProductsSession>` injected that can be used to create a session. In `UpdateProductsAsync`, the session is created and the number of products is determined. The products are then updated in batches with size 100. In each batch, a new transaction is started and committed at the end. The transaction is disposed in the finally block before a new batch begins.

*Please keep in mind*: neither LinqToDB nor Synnotech.DatabaseAbstractions support nested transactions. You should always start a single transaction, commit and dispose it, and only afterwards create a new transaction. If you create a new transaction before committing it, your current transaction will be disposed (and implicitly rolled back).

# General recommendations

1. All I/O should be abstracted. You should create abstractions that are specific for your use cases.
2. Your custom abstractions should derive from `IAsyncReadOnlySession` (when they only read data from SQL Server) or from `IAsyncSession` (when they also manipulate data and therefore need a transaction). Only use `IAsyncTransactionalSession` when you need to handle several transactions within a single session.
3. Prefer async I/O over sync I/O. Threads that wait for a database query to complete can handle other requests in the meantime when the query is performed asynchronously. This prevents thread starvation under high load and allows your web service to scale better. Synnotech.Linq2Db currently does not support synchronous sessions for this reason.
4. In case of web apps, we do not recommend using the DI container to dispose of the session. Instead, it is the controller's responsibility to do that. This way you can easily test the controller without running the whole ASP.NET Core infrastructure in your tests. To make your life easier, use an appropriate DI container like [LightInject](https://github.com/seesharper/LightInject) instead of Microsoft.Extensions.DependencyInjection. These more sophisticated DI containers provide you with more features, e.g. [Function Factories](https://www.lightinject.net/#function-factories).
