using AutoMapper;
using OpenStatusPage.Server.Domain.Entities;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

namespace OpenStatusPage.Server.Application.Misc
{
    public class ApplicationMapperProfile : Profile
    {
        public ApplicationMapperProfile()
        {
            foreach (var type in typeof(EntityBase).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(EntityBase))))
            {
                var map = CreateMap(type, type);

                if (type.IsAssignableTo(typeof(MonitorBase)))
                {
                    map.ForMember("Rules", opt => opt.Ignore()).ForSourceMember("Rules", opt => opt.DoNotValidate());
                    map.ForMember("InvolvedInIncidents", opt => opt.Ignore()).ForSourceMember("InvolvedInIncidents", opt => opt.DoNotValidate());
                    map.ForMember("NotificationProviders", opt => opt.Ignore()).ForSourceMember("NotificationProviders", opt => opt.DoNotValidate());
                }

                if (type.IsAssignableTo(typeof(NotificationProvider)))
                {
                    map.ForMember("UsedByMonitors", opt => opt.Ignore()).ForSourceMember("UsedByMonitors", opt => opt.DoNotValidate());
                }

                if (type.IsAssignableTo(typeof(Incident)))
                {
                    map.ForMember("AffectedServices", opt => opt.Ignore()).ForSourceMember("AffectedServices", opt => opt.DoNotValidate());
                }
            }
        }
    }
}
