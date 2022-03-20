using AutoMapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Http;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class HttpMonitor
    {
        [Parameter]
        public HttpMonitorViewModel HttpMonitorModel { get; set; }

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

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new HttpMonitorViewModel
            {
                Name = metaDto.Name,
                Interval = TimeSpan.FromMinutes(5),
                Enabled = true,
                MaxRedirects = 25,
                ResponseTimeRules = new(),
                ResponseBodyRules = new(),
                ResponseHeaderRules = new(),
                SslCertificateRules = new(),
                StatusCodeRules = new()
            };

            var response = await http.SendAsync<HttpMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(HttpMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<HttpMonitorViewModel>(response);
        }

        public class HttpMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            [Url(ErrorMessage = "This is not a valid URL")]
            public string Url { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public HttpVerb Method { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public ushort MaxRedirects { get; set; }

            public string? Headers { get; set; }

            public string? Body { get; set; }

            public HttpAuthenticationScheme AuthenticationScheme { get; set; }

            public string? AuthenticationBase { get; set; }

            public string? AuthenticationAdditional { get; set; }

            public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }

            public List<ResponseBodyRuleDto> ResponseBodyRules { get; set; }

            public List<ResponseHeaderRuleDto> ResponseHeaderRules { get; set; }

            public List<SslCertificateRuleDto> SslCertificateRules { get; set; }

            public List<StatusCodeRuleDto> StatusCodeRules { get; set; }
        }

        public class HttpMonitorDtoMapper : Profile
        {
            public HttpMonitorDtoMapper()
            {
                CreateMap<HttpMonitorDto, HttpMonitorViewModel>().ReverseMap();
            }
        }

        protected void OnRuleSelected(string selectedValue)
        {
            var rules = GetAllRules();

            var newOrderIndex = (ushort)(rules.Count > 0 ? rules.Max(x => x.OrderIndex) + 1 : 0);

            switch (selectedValue)
            {
                case nameof(ResponseTimeRuleDto):
                {
                    HttpMonitorModel.ResponseTimeRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }

                case nameof(ResponseBodyRuleDto):
                {
                    HttpMonitorModel.ResponseBodyRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }

                case nameof(ResponseHeaderRuleDto):
                {
                    HttpMonitorModel.ResponseHeaderRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }

                case nameof(SslCertificateRuleDto):
                {
                    HttpMonitorModel.SslCertificateRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }

                case nameof(StatusCodeRuleDto):
                {
                    HttpMonitorModel.StatusCodeRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }
            }
        }

        protected void RemoveRule(MonitorRuleDto rule)
        {
            switch (rule)
            {
                case ResponseTimeRuleDto responseTimeRule:
                {
                    HttpMonitorModel.ResponseTimeRules.Remove(responseTimeRule);
                    break;
                }

                case ResponseBodyRuleDto responseBodyRule:
                {
                    HttpMonitorModel.ResponseBodyRules.Remove(responseBodyRule);
                    break;
                }

                case ResponseHeaderRuleDto responseHeaderRule:
                {
                    HttpMonitorModel.ResponseHeaderRules.Remove(responseHeaderRule);
                    break;
                }

                case SslCertificateRuleDto sslCertificateRule:
                {
                    HttpMonitorModel.SslCertificateRules.Remove(sslCertificateRule);
                    break;
                }

                case StatusCodeRuleDto statusCodeRule:
                {
                    HttpMonitorModel.StatusCodeRules.Remove(statusCodeRule);
                    break;
                }
            }
        }

        protected void MoveRuleUp(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex - 1;

            var swapRule = GetAllRules().FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected void MoveRuleDown(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex + 1;

            var swapRule = GetAllRules().FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected List<MonitorRuleDto> GetAllRules()
        {
            return new List<MonitorRuleDto>()
                .Concat(HttpMonitorModel.ResponseTimeRules)
                .Concat(HttpMonitorModel.ResponseBodyRules)
                .Concat(HttpMonitorModel.ResponseHeaderRules)
                .Concat(HttpMonitorModel.SslCertificateRules)
                .Concat(HttpMonitorModel.StatusCodeRules)
                .ToList();
        }

        protected static int? SslTimeSpanAsDays(SslCertificateRuleDto sslCertificateRule)
        {
            return sslCertificateRule.MinValidTimespan.HasValue ? (int)sslCertificateRule.MinValidTimespan.Value.TotalDays : null;
        }

        protected static void UpdateSslTimespan(SslCertificateRuleDto sslCertificateRule, int? value)
        {
            if (value.HasValue)
            {
                sslCertificateRule.MinValidTimespan = TimeSpan.FromDays(value.Value);
            }
            else
            {
                sslCertificateRule.MinValidTimespan = null;
            }
        }
    }
}