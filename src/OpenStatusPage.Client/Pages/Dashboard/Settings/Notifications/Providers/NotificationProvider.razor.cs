using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Shared.DataTransferObjects;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers
{
    public partial class NotificationProvider
    {
        [Parameter]
        public ProviderViewModel ProviderModel { get; set; }

        public class ProviderViewModel : EntityBaseDto
        {
            [Required(ErrorMessage = "This field is required")]
            public string Name { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public bool Enabled { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public bool DefaultForNewMonitors { get; set; }
        }

        public class NotificationProviderDtoMapper : Profile
        {
            public NotificationProviderDtoMapper()
            {
                CreateMap<NotificationProviderDto, ProviderViewModel>().ReverseMap();
            }
        }
    }
}