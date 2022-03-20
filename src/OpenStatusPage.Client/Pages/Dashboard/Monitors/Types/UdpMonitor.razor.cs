using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class UdpMonitor
    {
        [Parameter]
        public UdpMonitorViewModel UdpMonitorModel { get; set; }

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new UdpMonitorViewModel
            {
                Name = metaDto.Name,
                Interval = TimeSpan.FromMinutes(5),
                Enabled = true,
                ResponseTimeRules = new(),
                ResponseBytesRules = new()
            };

            var response = await http.SendAsync<UdpMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(UdpMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<UdpMonitorViewModel>(response);
        }

        public class UdpMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            public string Hostname { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public ushort? Port { get; set; }

            public string RequestBytes { get; set; }

            public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }

            public List<ResponseBytesRuleDto> ResponseBytesRules { get; set; }
        }

        public class UdpMonitorDtoMapper : Profile
        {
            public UdpMonitorDtoMapper()
            {
                CreateMap<UdpMonitorDto, UdpMonitorViewModel>().ReverseMap();
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
                    UdpMonitorModel.ResponseTimeRules.Add(new()
                    {
                        OrderIndex = newOrderIndex,
                        ViolationStatus = ServiceStatus.Unavailable
                    });
                    break;
                }

                case nameof(ResponseBytesRuleDto):
                {
                    UdpMonitorModel.ResponseBytesRules.Add(new()
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
                    UdpMonitorModel.ResponseTimeRules.Remove(responseTimeRule);
                    break;
                }

                case ResponseBytesRuleDto responseBytesRule:
                {
                    UdpMonitorModel.ResponseBytesRules.Remove(responseBytesRule);
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
                .Concat(UdpMonitorModel.ResponseTimeRules)
                .Concat(UdpMonitorModel.ResponseBytesRules)
                .ToList();
        }
    }
}