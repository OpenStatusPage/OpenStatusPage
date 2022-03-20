using OpenStatusPage.Server.Domain.Entities.Notifications.History;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Notifications.History
{
    public class NotificationHistoryService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public NotificationHistoryService(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public IQueryable<NotificationHistoryRecord> Get() => _applicationDbContext.NotificationHistoryRecords;

        public async Task DeleteAsync(NotificationHistoryRecord historyRecord)
        {
            _applicationDbContext.Remove(historyRecord);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task CreateAsync(NotificationHistoryRecord historyRecord)
        {
            _applicationDbContext.Add(historyRecord);

            await _applicationDbContext.SaveChangesAsync();
        }
    }
}
