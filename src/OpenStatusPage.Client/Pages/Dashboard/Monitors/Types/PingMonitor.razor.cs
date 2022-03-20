using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ping;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class PingMonitor
    {
        [Parameter]
        public PingMonitorViewModel PingMonitorModel { get; set; }

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new PingMonitorViewModel
            {
                Name = metaDto.Name,
                Enabled = true,
                Interval = TimeSpan.FromMinutes(5),
                ResponseTimeRules = new(),
            };

            var response = await http.SendAsync<PingMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(PingMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<PingMonitorViewModel>(response);
        }

        public class PingMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            public string Hostname { get; set; }

            public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }
        }

        public class PingMonitorDtoMapper : Profile
        {
            public PingMonitorDtoMapper()
            {
                CreateMap<PingMonitorDto, PingMonitorViewModel>().ReverseMap();
            }
        }

        protected void OnRuleSelected(string selectedValue)
        {
            var newOrderIndex = (ushort)(PingMonitorModel.ResponseTimeRules.Count > 0 ? PingMonitorModel.ResponseTimeRules.Max(x => x.OrderIndex) + 1 : 0);

            switch (selectedValue)
            {
                case nameof(ResponseTimeRuleDto):
                {
                    PingMonitorModel.ResponseTimeRules.Add(new ResponseTimeRuleDto
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
                case ResponseTimeRuleDto responseTime:
                {
                    PingMonitorModel.ResponseTimeRules.Remove(responseTime);
                    break;
                }
            }
        }

        protected void MoveRuleUp(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex - 1;

            var swapRule = PingMonitorModel.ResponseTimeRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected void MoveRuleDown(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex + 1;

            var swapRule = PingMonitorModel.ResponseTimeRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }
    }
}