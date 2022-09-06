using System;
using FluentAssertions;
using Synnotech.Core.Initialization;
using Synnotech.DatabaseAbstractions;
using Xunit;

namespace Synnotech.Linq2Db.Tests;

public static class AsyncReadOnlySessionTests
{
    [Fact]
    public static void MustImplementIDisposable() =>
        typeof(AsyncReadOnlySession<>).Should().Implement<IDisposable>();

    [Fact]
    public static void MustImplementIAsyncDisposable() =>
        typeof(AsyncReadOnlySession<>).Should().Implement<IAsyncDisposable>();

    [Fact]
    public static void MustImplementIAsyncReadOnlySession() =>
        typeof(AsyncReadOnlySession<>).Should().Implement<IAsyncReadOnlySession>();

    [Fact]
    public static void MustImplementIInitializeAsync() =>
        typeof(AsyncReadOnlySession<>).Should().Implement<IInitializeAsync>();
}