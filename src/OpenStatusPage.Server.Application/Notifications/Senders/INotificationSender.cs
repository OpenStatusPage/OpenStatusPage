using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;

namespace OpenStatusPage.Server.Application.Notifications.Senders
{
    public interface INotificationSender
    {
        public Task SendNotificationAsync(MonitorBase monitor, StatusHistoryRecord? previous, StatusHistoryRecord current);
    }
}
