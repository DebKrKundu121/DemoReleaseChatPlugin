using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Application.Releases.Projects.SetProjectArchiveState;
using DemoReleaseChatPlugin.Application.Releases.Projects.UpdateProject;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetProjectStakeholderGroups;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Values = DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases.Admin
{
    public class ManageProjectBase : AppComponent
    {
        /// The route parameter. A URL can only carry a raw Guid, so the conversion to the typed id
        /// happens once, here at the edge, and everything below this line is typed.
        [Parameter] public Guid ProjectId { get; set; }

        private ProjectId Project => Values.ProjectId.From(ProjectId);

        protected GetProjectStakeholderGroupsResult Result { get; set; }
        protected UpdateProjectCommand EditCommand { get; set; }
        protected FluentValidationValidator Validator { get; set; }

        protected bool IsBusy { get; set; }
        protected bool ShowInactive { get; set; }

        /// Set when the read reported the project is legitimately not there, so the page can render
        /// an empty state instead of an error.
        protected bool NotFound { get; set; }

        /// OnParametersSetAsync, not OnInitializedAsync, so the route parameter is honoured.
        protected override async Task OnParametersSetAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;

            var result = await Mediator.Send(new GetProjectStakeholderGroupsQuery
            {
                ProjectId = Project,
                IncludeInactive = ShowInactive
            });

            // Three outcomes, not two: a project that is not there is an answer the page can show
            // plainly, rather than an error dialog that reads like something broke.
            if (result.IsNotFound)
            {
                NotFound = true;
                IsBusy = false;
                return;
            }

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                IsBusy = false;
                return;
            }

            NotFound = false;
            Result = result.Value;

            // Only reload the form when it has been cleared: toggling "Show inactive" reloads the
            // grid without throwing away half-typed edits.
            if (EditCommand is null)
                await LoadEditCommandAsync();

            IsBusy = false;
        }

        protected async Task LoadEditCommandAsync()
        {
            var edit = await Mediator.Send(new GetUpdateProjectQuery { ProjectId = Project });

            if (edit.IsSuccess)
                EditCommand = edit.Value.Command;
        }

        protected async Task OnShowInactiveChanged(bool value)
        {
            ShowInactive = value;
            await LoadAsync();
        }

        protected async Task SaveDetailsAsync()
        {
            if (!await IsValidAsync(Validator)) return;

            var result = await Mediator.Send(EditCommand);

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Notify("Project updated", "The project details have been saved.");

            EditCommand = null;   // so the next load refreshes it

            await LoadAsync();
        }

        protected async Task ResetEditAsync()
        {
            EditCommand = null;
            await LoadEditCommandAsync();
        }

        protected async Task SetArchivedAsync(bool isArchived)
        {
            var verb = isArchived ? "Archive" : "Restore";

            var message = isArchived
                ? $"Archive '{Result.ProjectName}'? It will no longer be selectable for new releases, " +
                  "but its history is kept."
                : $"Restore '{Result.ProjectName}' so releases can be recorded against it again?";

            var confirmed = await DialogService.Confirm(
                message, verb, new ConfirmOptions { OkButtonText = verb, CancelButtonText = "Cancel" });

            if (confirmed != true) return;

            var result = await Mediator.Send(new SetProjectArchiveStateCommand
            {
                ProjectId = Project,
                IsArchived = isArchived
            });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            EditCommand = null;

            await LoadAsync();
        }

        protected async Task AddGroupAsync()
        {
            var changed = await DialogService.OpenAsync<StakeholderGroupDialog>(
                "Add stakeholder group",
                new Dictionary<string, object>
                {
                    [nameof(StakeholderGroupDialog.IsMandatory)] = false,
                    [nameof(StakeholderGroupDialog.ProjectId)] = ProjectId
                },
                new DialogOptions { Width = "760px" });

            if (changed is true)
                await LoadAsync();
        }

        protected async Task ManageGroupAsync(StakeholderGroupDto group)
        {
            var changed = await DialogService.OpenAsync<StakeholderGroupDialog>(
                group.Name,
                new Dictionary<string, object>
                {
                    [nameof(StakeholderGroupDialog.IsMandatory)] = false,
                    [nameof(StakeholderGroupDialog.GroupId)] = group.Id
                },
                new DialogOptions { Width = "860px" });

            if (changed is true)
                await LoadAsync();
        }

        protected async Task ViewProjectHistoryAsync() => await ViewHistory(ProjectId);

        protected void BackToProjects() => Navigation.NavigateTo("/releases/admin/projects");

        private void Notify(string summary, string detail) =>
            Notification.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = summary,
                Detail = detail,
                Duration = 4000
            });
    }
}
