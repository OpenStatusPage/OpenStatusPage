using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Dns;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class DnsMonitor
    {
        [Parameter]
        public DnsMonitorViewModel DnsMonitorModel { get; set; }

        public static async Task<MonitorViewModel> LoadDataToModelAsync(TransparentHttpClient http, HeaderEntry accessToken, IMapper mapper, MonitorMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new DnsMonitorViewModel
            {
                Name = metaDto.Name,
                Interval = TimeSpan.FromMinutes(5),
                Enabled = true,
                RecordType = DnsRecordType.A,
                DnsRecordRules = new()
            };

            var response = await http.SendAsync<DnsMonitorDto>(HttpMethod.Get, $"api/v1/Monitors/{metaDto.Id}?typename={nameof(DnsMonitorDto)}", accessToken);

            if (response == null) return null;

            return mapper.Map<DnsMonitorViewModel>(response);
        }

        public class DnsMonitorViewModel : MonitorViewModel
        {
            [Required(ErrorMessage = "This field is required")]
            public string Hostname { get; set; }

            public string? Resolvers { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public DnsRecordType RecordType { get; set; }

            public List<DnsRecordRuleDto> DnsRecordRules { get; set; }
        }

        public class DnsMonitorDtoMapper : Profile
        {
            public DnsMonitorDtoMapper()
            {
                CreateMap<DnsMonitorDto, DnsMonitorViewModel>().ReverseMap();
            }
        }

        protected void OnRuleSelected(string selectedValue)
        {
            var newOrderIndex = (ushort)(DnsMonitorModel.DnsRecordRules.Count > 0 ? DnsMonitorModel.DnsRecordRules.Max(x => x.OrderIndex) + 1 : 0);

            switch (selectedValue)
            {
                case nameof(DnsRecordRuleDto):
                {
                    DnsMonitorModel.DnsRecordRules.Add(new DnsRecordRuleDto
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
                case DnsRecordRuleDto dnsRecord:
                {
                    DnsMonitorModel.DnsRecordRules.Remove(dnsRecord);
                    break;
                }
            }
        }

        protected void MoveRuleUp(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex - 1;

            var swapRule = DnsMonitorModel.DnsRecordRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }

        protected void MoveRuleDown(MonitorRuleDto rule)
        {
            var moveToIndex = rule.OrderIndex + 1;

            var swapRule = DnsMonitorModel.DnsRecordRules.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapRule == null) return;

            swapRule.OrderIndex = rule.OrderIndex;

            rule.OrderIndex = (ushort)moveToIndex;
        }
    }
}
