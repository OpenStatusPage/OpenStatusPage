using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Domain.Attributes;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Server.Domain.Entities.Configuration;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.History;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;
using OpenStatusPage.Server.Domain.Entities.StatusPages;
using OpenStatusPage.Server.Domain.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OpenStatusPage.Server.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<ApplicationSettings> ApplicationSettings { get; set; }

        public DbSet<MonitorBase> Monitors { get; set; }

        public DbSet<Incident> Incidents { get; set; }

        public DbSet<NotificationProvider> NotificationProviders { get; set; }

        public DbSet<StatusPage> StatusPages { get; set; }

        public DbSet<StatusHistoryRecord> StatusHistoryRecords { get; set; }

        public DbSet<NotificationHistoryRecord> NotificationHistoryRecords { get; set; }

        public DbSet<ClusterMember> ClusterMembers { get; set; }

        public DbSet<RaftLogMetaEntry> RaftLogMetaEntries { get; set; }

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var tableType in typeof(IPolymorph).Assembly.GetTypes().Where(
                x => x.IsAssignableTo(typeof(IPolymorph)) &&
                !x.IsDefined(typeof(PolymorphBaseTypeAttribute), false) &&
                x != typeof(IPolymorph)))
            {
                builder.Entity(tableType).ToTable(IPolymorph.GetTypeAsMulitpleString(tableType));
            }

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public async Task<T> CreateEntityAsync<T>(Expression<Func<ApplicationDbContext, DbSet<T>>> dbSetAccessor, bool attach = true, Type polymorphType = default!) where T : class, new()
        {
            return await CreateEntityAsync<T>(attach, polymorphType);
        }

        public async Task<T> CreateEntityAsync<T>(bool attach = true, Type polymorphType = default!) where T : class, new()
        {
            T? model = null!;

            if (ChangeTracker.LazyLoadingEnabled && attach)
            {
                //model = polymorphType != null ? this.CreateProxy(polymorphType) as T : this.CreateProxy<T>();

                //Proxies are disabled due to serailization in network transport an DTO automapping
                throw new NotImplementedException();
            }
            else
            {
                model = polymorphType != null ? Activator.CreateInstance(polymorphType) as T : Activator.CreateInstance(typeof(T)) as T;
            }

            if (attach && model != null)
            {
                Entry(model).State = EntityState.Added;
            }

            return model;
        }
    }
}
