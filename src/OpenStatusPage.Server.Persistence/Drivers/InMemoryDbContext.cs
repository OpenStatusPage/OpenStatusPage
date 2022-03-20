using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace OpenStatusPage.Server.Persistence.Drivers;

public class InMemoryDbContext : ApplicationDbContext
{
    protected static readonly string _databaseName = Guid.NewGuid().ToString();

    public InMemoryDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options
            .UseInMemoryDatabase(_databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
}
