using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases.History.GetHistoryRecords;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Shared
{
    public class HistoryBase : AppComponent
    {
        [Parameter] public Guid CorrelationId { get; set; }

        protected List<GetHistoryRecordsResult.RecordDto> Records { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            var result = await Mediator.Send(new GetHistoryRecordsQuery { CorrelationId = CorrelationId });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Records = [];
                return;
            }

            Records = result.Value.Records.ToList();
        }
    }
}
