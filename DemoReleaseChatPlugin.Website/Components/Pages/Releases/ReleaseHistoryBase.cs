using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases.Notices.GetReleaseHistory;
using DemoReleaseChatPlugin.Website.Components.Pages.Releases.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases
{
    public class ReleaseHistoryBase : AppComponent
    {
        protected List<GetReleaseHistoryResult.NoticeDto> Notices { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsBusy = true;

            // No filters - the grid filters client-side.
            var result = await Mediator.Send(new GetReleaseHistoryQuery());

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Notices ??= [];
                IsBusy = false;
                return;
            }

            Notices = result.Value.Notices.ToList();
            IsBusy = false;
        }

        protected void GoToSchedule() => Navigation.NavigateTo("/releases/scheduled/new");

        protected void GoToRecordLive() => Navigation.NavigateTo("/releases/live/new");

        protected void RecordGoLiveFrom(GetReleaseHistoryResult.NoticeDto notice) =>
            Navigation.NavigateTo($"/releases/live/new/from/{notice.Id}");

        protected void RollBack(GetReleaseHistoryResult.NoticeDto notice) =>
            Navigation.NavigateTo($"/releases/rollbacks/new/{notice.Id}");

        protected async Task ShowDetailsAsync(GetReleaseHistoryResult.NoticeDto notice)
        {
            await DialogService.OpenAsync<ReleaseNoticeDetailsDialog>(
                $"{ReleaseHistoryGrid.BadgeTextFor(notice.Kind)} - {notice.ProjectName} {notice.Version}",
                new Dictionary<string, object> { [nameof(ReleaseNoticeDetailsDialog.Notice)] = notice },
                new DialogOptions { Width = "640px", CloseDialogOnOverlayClick = true });
        }
    }
}
