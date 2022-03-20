using AutoMapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ssh;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class SshMonitor
    {
        [Parameter]
        public SshMonitorViewModel SshMonitorModel { get; set; }

        protected bool _showPassword;
        protected bool _showPrivateKey;

        protected InputType _passwordInput = InputType.Password;

        protected string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
        protected string _privateKeyInputIcon = Icons.Material.Filled.VisibilityOff;

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

        protected async Task PrivateKeyToggleShowAsync()
        {
            if (_showPrivateKey)
            {
                _showPrivateKey = false;
                _privateKeyInputIcon = Icons.Material.Filled.VisibilityOff;
            }
            else
            {
                _showPrivateKey = true;
                _privateKeyInputIcon = Icons.Material.Filled.Visibility;
            }

            await InvokeAsync(StateHasChanged);
        }

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new SshMonitorViewModel
            {
                Name = metaDto.Name,
                Interval = TimeSpan.FromMinutes(5),
                Enabled = true,
                CommandResultRules = new()
            };

            var response = await http.SendAsync<SshMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(SshMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<SshMonitorViewModel>(response);
        }

        public class SshMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            public string Hostname { get; set; }

            public ushort? Port { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public string Username { get; set; }

            public string? Password { get; set; }

            public string? PrivateKey { get; set; }

            public string? Command { get; set; }

            public List<SshCommandResultRuleDto> CommandResultRules { get; set; }
        }

        public class SshMonitorDtoMapper : Profile
        {
            public SshMonitorDtoMapper()
            {
                CreateMap<SshMonitorDto, SshMonitorViewModel>().ReverseMap();
            }
        }

        protected void OnRuleSelected(string selectedValue)
        {
            var newOrderIndex = (ushort)(SshMonitorModel.CommandResultRules.Count > 0 ? SshMonitorModel.CommandResultRules.Max(x => x.OrderIndex) + 1 : 0);

            switch (selectedValue)
            {
                case nameof(SshCommandResultRuleDto):
                {
                    SshMonitorModel.CommandResultRules.Add(new()
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
                case SshCommandResultRuleDto sshCommandResultRule:
                {
                    SshMonitorModel.CommandResultRules.Remove(sshCommandResultRule);
                    break;
                }
            }
        }

        protected void MoveRuleUp(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex - 1;

            var swapRule = SshMonitorModel.CommandResultRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected void MoveRuleDown(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex + 1;

            var swapRule = SshMonitorModel.CommandResultRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }
    }
}
