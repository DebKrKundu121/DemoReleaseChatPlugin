using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Application.Releases.Notices.CreateScheduledRelease;
using DemoReleaseChatPlugin.Application.Releases.Notices.Lookups;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases
{
    public class ScheduleReleaseBase : ReleaseNoticeFormBase
    {
        protected CreateScheduledReleaseCommand Command { get; set; } = new();
        protected List<GetProjectOptionsResult.Option> Projects { get; set; }
        protected bool IsReady { get; set; }

        /// RiskLevel is a closed vocabulary rather than an enum now, so the dropdown's options come
        /// from the type's own declared members instead of Enum.GetValues.
        protected IEnumerable<RiskLevel> RiskLevels => RiskLevel.All;

        protected void OnRiskLevelChanged()
        {
            if (Command.RiskLevel is null)
                Command.RiskNotes = null;   // clearing the level clears the notes
        }

        protected override async Task OnInitializedAsync()
        {
            // Scheduled notices are advance warning, so today is almost never right.
            Command.ScheduledFor = DateTime.Now.Date.AddDays(1).AddHours(9);
            Command.LeadDeveloper = await ResolveCurrentUserNameAsync();

            var projects = await Mediator.Send(new GetProjectOptionsQuery());

            if (projects.IsFailure)
            {
                await DialogService.Alert(projects.Message, "Error");
                return;   // IsReady stays false, so the Loader spins
            }

            Projects = projects.Value.Projects.ToList();
            IsReady = true;
        }

        protected async Task SubmitAsync()
        {
            if (!await IsValidAsync(Validator)) return;

            IsSubmitting = true;
            var result = await Mediator.Send(Command);
            IsSubmitting = false;

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Could not schedule the release");
                return;
            }

            ReportOutcome(result.Value, "Release scheduled");

            Navigation.NavigateTo("/releases/history");
        }

        protected void Cancel() => Navigation.NavigateTo("/releases/history");
    }
}
