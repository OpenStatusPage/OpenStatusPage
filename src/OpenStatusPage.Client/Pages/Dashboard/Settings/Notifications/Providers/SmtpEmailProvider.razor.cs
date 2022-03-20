using AutoMapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers
{
    public partial class SmtpEmailProvider : NotificationProvider
    {
        [Parameter]
        public SmtpEmailProviderViewModel SmtpEmailProviderModel { get; set; }

        protected bool _showPassword;

        protected InputType _passwordInput = InputType.Password;

        protected string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        protected async Task PasswordToggleShowAsync()
        {
            if (_showPassword)
            {
                _showPassword = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _showPassword = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }

            await InvokeAsync(StateHasChanged);
        }

        public static async Task<ProviderViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, NotificationProviderMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new SmtpEmailProviderViewModel
            {
                Name = metaDto.Name,
                Enabled = true
            };

            var response = await http.SendAsync<SmtpEmailProviderDto>(HttpMethod.Get, $"api/v1/NotificationProviders/{metaDto.Id}?typename={nameof(SmtpEmailProviderDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<SmtpEmailProviderViewModel>(response);
        }

        public class SmtpEmailProviderViewModel : ProviderViewModel
        {


            [Required(ErrorMessage = "This field is required.")]
            public string Hostname { get; set; }

            public ushort? Port { get; set; }

            [Required(ErrorMessage = "This field is required.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public string Password { get; set; }

            public string? DisplayName { get; set; }

            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string? FromAddress { get => _fromAddress; set => _fromAddress = string.IsNullOrWhiteSpace(value) ? null : value; }
            private string? _fromAddress;

            public string? ReceiversDirect { get; set; }

            public string? ReceiversCC { get; set; }

            public string? ReceiversBCC { get; set; }
        }

        public class SmtpEmailProviderDtoMapper : Profile
        {
            public SmtpEmailProviderDtoMapper()
            {
                CreateMap<SmtpEmailProviderDto, SmtpEmailProviderViewModel>().ReverseMap();
            }
        }
    }
}