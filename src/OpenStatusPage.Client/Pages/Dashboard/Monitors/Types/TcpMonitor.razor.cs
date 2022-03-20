using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Tcp;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class TcpMonitor
    {
        [Parameter]
        public TcpMonitorViewModel TcpMonitorModel { get; set; }

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new TcpMonitorViewModel
            {
                Name = metaDto.Name,
                Interval = TimeSpan.FromMinutes(5),
                Enabled = true,
                ResponseTimeRules = new()
            };

            var response = await http.SendAsync<TcpMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(TcpMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<TcpMonitorViewModel>(response);
        }

        public class TcpMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            public string Hostname { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public ushort? Port { get; set; }

            public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }
        }

        public class TcpMonitorDtoMapper : Profile
        {
            public TcpMonitorDtoMapper()
            {
                CreateMap<TcpMonitorDto, TcpMonitorViewModel>().ReverseMap();
            }
        }

        protected void OnRuleSelected(string selectedValue)
        {
            var newOrderIndex = (ushort)(TcpMonitorModel.ResponseTimeRules.Count > 0 ? TcpMonitorModel.ResponseTimeRules.Max(x => x.OrderIndex) + 1 : 0);

            switch (selectedValue)
            {
                case nameof(ResponseTimeRuleDto):
                {
                    TcpMonitorModel.ResponseTimeRules.Add(new()
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
                    TcpMonitorModel.ResponseTimeRules.Remove(responseTimeRule);
                    break;
                }
            }
        }

        protected void MoveRuleUp(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex - 1;

            var swapRule = TcpMonitorModel.ResponseTimeRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected void MoveRuleDown(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex + 1;

            var swapRule = TcpMonitorModel.ResponseTimeRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }
    }
}
