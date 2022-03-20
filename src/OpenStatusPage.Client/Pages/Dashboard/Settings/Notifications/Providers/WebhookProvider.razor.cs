using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers
{
    public partial class WebhookProvider : NotificationProvider
    {
        [Parameter]
        public WebhookProviderViewModel WebhookProviderModel { get; set; }

        public static async Task<ProviderViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, NotificationProviderMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new WebhookProviderViewModel
            {
                Name = metaDto.Name,
                Enabled = true
            };

            var response = await http.SendAsync<WebhookProviderDto>(HttpMethod.Get, $"api/v1/NotificationProviders/{metaDto.Id}?typename={nameof(WebhookProviderDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<WebhookProviderViewModel>(response);
        }

        public class WebhookProviderViewModel : ProviderViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            [Url(ErrorMessage = "Please enter a valid url. e.g https://example.org")]
            public string Url { get; set; }

            public string Headers { get; set; }
        }

        public class WebhookProviderDtoMapper : Profile
        {
            public WebhookProviderDtoMapper()
            {
                CreateMap<WebhookProviderDto, WebhookProviderViewModel>().ReverseMap();
            }
        }
    }
}