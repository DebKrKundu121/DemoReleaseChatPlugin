using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases.Notices.CreateLiveRelease;
using DemoReleaseChatPlugin.Application.Releases.Notices.Lookups;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases
{
    public class RecordLiveReleaseBase : ReleaseNoticeFormBase
    {
        [Parameter] public Guid? ScheduledReleaseId { get; set; }

        protected CreateLiveReleaseCommand Command { get; set; } = new();
        protected List<GetProjectOptionsResult.Option> Projects { get; set; }
        protected List<GetUnreleasedScheduledNoticesResult.Option> ScheduledNotices { get; set; } = [];
        protected bool IsReady { get; set; }

        // What THIS class last auto-filled, so linking a scheduled notice overwrites its own
        // defaults but never the user's edits.
        private string _filledVersion;
        private string _filledReleaseNotes;
        private string _filledLeadDeveloper;
        private DateTime? _filledDeployedOn;

        protected override async Task OnInitializedAsync()
        {
            // A go-live is recorded right after it happens.
            Command.DeployedOn = _filledDeployedOn = DateTime.Now;
            Command.LeadDeveloper = _filledLeadDeveloper = await ResolveCurrentUserNameAsync();

            var projects = await Mediator.Send(new GetProjectOptionsQuery());

            if (projects.IsFailure)
            {
                await DialogService.Alert(projects.Message, "Error");
                return;
            }

            Projects = projects.Value.Projects.ToList();

            // Converted explicitly at the edge: a route can only carry a raw Guid.
            if (ScheduledReleaseId.HasValue)
                await PreselectFromScheduledNoticeAsync(ReleaseNoticeId.From(ScheduledReleaseId.Value));

            IsReady = true;
        }

        protected async Task OnProjectChangedAsync()
        {
            Command.ScheduledReleaseId = null;
            ScheduledNotices = [];

            if (Command.ProjectId is null) return;

            var notices = await Mediator.Send(
                new GetUnreleasedScheduledNoticesQuery { ProjectId = Command.ProjectId });

            if (notices.IsSuccess)
                ScheduledNotices = notices.Value.Notices.ToList();
        }

        protected void OnScheduledNoticeChanged()
        {
            if (Command.ScheduledReleaseId is null) return;

            var notice = ScheduledNotices.FirstOrDefault(x => x.Id == Command.ScheduledReleaseId);
            if (notice is null) return;

            if (IsUnedited(Command.Version, _filledVersion))
                Command.Version = _filledVersion = notice.Version;

            if (IsUnedited(Command.ReleaseNotes, _filledReleaseNotes))
                Command.ReleaseNotes = _filledReleaseNotes = notice.Summary;

            if (IsUnedited(Command.LeadDeveloper, _filledLeadDeveloper))
                Command.LeadDeveloper = _filledLeadDeveloper = notice.LeadDeveloper;

            if (Command.DeployedOn == _filledDeployedOn)
                Command.DeployedOn = _filledDeployedOn = notice.ScheduledFor;
        }

        private static bool IsUnedited(string current, string filled) =>
            string.IsNullOrWhiteSpace(current) || current == filled;

        /// <summary>
        /// Handles the /from/{id} route. There is no "get scheduled notice by id" query, so this
        /// loops the projects asking each for its unreleased notices until it finds the id.
        ///
        /// That loop is O(projects) round trips. It only runs on the /from/{id} entry path, which is
        /// a single click from the history grid, so it is left as-is. If project counts grow, add a
        /// GetScheduledNoticeQuery { Id } slice and replace the loop.
        /// </summary>
        private async Task PreselectFromScheduledNoticeAsync(ReleaseNoticeId scheduledReleaseId)
        {
            foreach (var project in Projects)
            {
                var notices = await Mediator.Send(
                    new GetUnreleasedScheduledNoticesQuery { ProjectId = project.Id });

                if (notices.IsFailure) continue;

                var match = notices.Value.Notices.FirstOrDefault(x => x.Id == scheduledReleaseId);

                if (match is null) continue;

                Command.ProjectId = project.Id;
                Command.ScheduledReleaseId = scheduledReleaseId;
                ScheduledNotices = notices.Value.Notices.ToList();

                OnScheduledNoticeChanged();
                return;
            }
        }

        protected async Task SubmitAsync()
        {
            if (!await IsValidAsync(Validator)) return;

            IsSubmitting = true;
            var result = await Mediator.Send(Command);
            IsSubmitting = false;

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Could not record the go-live");
                return;
            }

            ReportOutcome(result.Value, "Go-live recorded");

            Navigation.NavigateTo("/releases/history");
        }

        protected void Cancel() => Navigation.NavigateTo("/releases/history");
    }
}
