using FluentAssertions;
using Synnotech.DatabaseAbstractions;
using Xunit;

namespace Synnotech.Linq2Db.Tests
{
    public static class AsyncTransactionalSessionTests
    {
        [Fact]
        public static void MustImplementITransactionalSession() =>
            typeof(AsyncTransactionalSession<>).Should().Implement<IAsyncTransactionalSession>();
    }
}