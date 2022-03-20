using System;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OpenStatusPage.Server.Tests.Helpers;

public class TestBase
{
    public static readonly TimeSpan _testWaitMax = TimeSpan.FromMinutes(5);

    protected readonly ITestOutputHelper _testOutput;

    public TestBase(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }
}
