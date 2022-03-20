using AutoMapper;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Shared.DataTransferObjects;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors.Types
{
    public partial class MonitorBase
    {
        [Parameter]
        public MonitorViewModel MonitorModel { get; set; }

        public class MonitorViewModel : EntityBaseDto
        {
            private ulong _intervalField;
            private ulong? _retryIntervalField;
            private ulong? _timeoutField;

            [Required(ErrorMessage = "This field is required")]
            public bool Enabled { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public string Name { get; set; }

            [Required(ErrorMessage = "This field is required")]
            public ulong IntervalField
            {
                get => _intervalField;
                set => _intervalField = value;
            }

            public ulong IntervalMuliplier { get; set; } = 1;

            public TimeSpan Interval
            {
                get => TimeSpan.FromSeconds(_intervalField * IntervalMuliplier);
                set
                {
                    if (value.TotalDays >= 1.0)
                    {
                        _intervalField = (ulong)(value.TotalSeconds / 86400);
                        IntervalMuliplier = 86400;
                    }
                    else if (value.TotalHours >= 1.0)
                    {
                        _intervalField = (ulong)(value.TotalSeconds / 3600);
                        IntervalMuliplier = 3600;
                    }
                    else if (value.TotalMinutes >= 1.0)
                    {
                        _intervalField = (ulong)(value.TotalSeconds / 60);
                        IntervalMuliplier = 60;
                    }
                    else
                    {
                        _intervalField = (ulong)(value.TotalSeconds);
                        IntervalMuliplier = 1;
                    }
                }
            }

            public ushort? Retries { get; set; }

            public ulong? RetryIntervalField
            {
                get => _retryIntervalField;
                set => _retryIntervalField = value;
            }

            public ulong RetryIntervalMuliplier { get; set; } = 1;

            public TimeSpan? RetryInterval
            {
                get => _retryIntervalField == null ? null : TimeSpan.FromSeconds(_retryIntervalField.Value * RetryIntervalMuliplier);
                set
                {
                    _retryIntervalField = null;

                    if (!value.HasValue) return;

                    if (value.Value.TotalDays >= 1.0)
                    {
                        _retryIntervalField = (ulong)(value.Value.TotalSeconds / 86400);
                        RetryIntervalMuliplier = 86400;
                    }
                    else if (value.Value.TotalHours >= 1.0)
                    {
                        _retryIntervalField = (ulong)(value.Value.TotalSeconds / 3600);
                        RetryIntervalMuliplier = 3600;
                    }
                    else if (value.Value.TotalMinutes >= 1.0)
                    {
                        _retryIntervalField = (ulong)(value.Value.TotalSeconds / 60);
                        RetryIntervalMuliplier = 60;
                    }
                    else
                    {
                        _retryIntervalField = (ulong)(value.Value.TotalSeconds);
                        RetryIntervalMuliplier = 1;
                    }
                }
            }

            public ulong? TimeoutField
            {
                get => _timeoutField;
                set => _timeoutField = value;
            }

            public ulong TimeoutMuliplier { get; set; } = 1;

            public TimeSpan? Timeout
            {
                get => _timeoutField == null ? null : TimeSpan.FromMilliseconds(_timeoutField.Value * TimeoutMuliplier);
                set
                {
                    _timeoutField = null;

                    if (!value.HasValue) return;

                    if (value.Value.TotalMinutes >= 1.0)
                    {
                        _timeoutField = (ulong?)(value.Value.TotalMilliseconds / 60000);
                        TimeoutMuliplier = 60000;
                    }
                    else if (value.Value.TotalSeconds >= 1.0)
                    {
                        _timeoutField = (ulong?)(value.Value.TotalMilliseconds / 1000);
                        TimeoutMuliplier = 1000;
                    }
                    else
                    {
                        _timeoutField = (ulong?)value.Value.TotalMilliseconds;
                        TimeoutMuliplier = 1;
                    }
                }
            }

            public int? WorkerCount { get; set; }

            public List<string> Tags { get; set; }

            public List<NotificationProviderMetaDto> NotificationProviderMetas { get; set; }
        }

        public class NotificationProviderDtoMapper : Profile
        {
            public NotificationProviderDtoMapper()
            {
                CreateMap<MonitorDto, MonitorViewModel>().ReverseMap();

                CreateMap<string, List<string>>().ConvertUsing(x => x.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

                CreateMap<List<string>, string>().ConvertUsing(x => string.Join(';', x));
            }
        }
    }
}
