using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases.Notices.CreateRollback;
using DemoReleaseChatPlugin.Application.Releases.Notices.Lookups;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases
{
    public class LogRollbackBase : ReleaseNoticeFormBase
    {
        [Parameter] public Guid? LiveReleaseId { get; set; }

        protected CreateRollbackCommand Command { get; set; } = new();
        protected List<GetRollbackCandidatesResult.Candidate> Candidates { get; set; }
        protected bool IsReady { get; set; }

        /// The form never asks for a version, so the page shows which one is being reverted.
        protected GetRollbackCandidatesResult.Candidate SelectedCandidate =>
            Candidates?.FirstOrDefault(x => x.Id == Command.LiveReleaseId);

        protected override async Task OnInitializedAsync()
        {
            Command.RolledBackOn = DateTime.Now;

            var candidates = await Mediator.Send(new GetRollbackCandidatesQuery());

            if (candidates.IsFailure)
            {
                await DialogService.Alert(candidates.Message, "Error");
                return;
            }

            Candidates = candidates.Value.Candidates.ToList();

            // The route parameter is a raw Guid - that is all a URL can carry - so it is converted
            // explicitly here, at the edge, rather than by an implicit operator that would let any
            // Guid stand in for any id.
            if (LiveReleaseId.HasValue && Candidates.Any(x => x.Id == ReleaseNoticeId.From(LiveReleaseId.Value)))
                Command.LiveReleaseId = ReleaseNoticeId.From(LiveReleaseId.Value);

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
                await DialogService.Alert(result.Message, "Could not log the rollback");
                return;
            }

            ReportOutcome(result.Value, "Rollback logged");

            Navigation.NavigateTo("/releases/history");
        }

        protected void Cancel() => Navigation.NavigateTo("/releases/history");
    }
}
