using FluentAssertions;
using Synnotech.DatabaseAbstractions;
using Xunit;

namespace Synnotech.Linq2Db.Tests;

public static class AsyncSessionTests
{
    [Fact]
    public static void MustImplementIAsyncSession() =>
        typeof(AsyncSession<>).Should().Implement<IAsyncSession>();

    [Fact]
    public static void MustDeriveFromAsyncReadOnlySession() =>
        typeof(AsyncSession<>).Should().BeDerivedFrom(typeof(AsyncReadOnlySession<>));
}