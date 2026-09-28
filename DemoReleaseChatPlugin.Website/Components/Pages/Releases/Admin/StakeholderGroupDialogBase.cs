using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Application.Entra;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.CreateStakeholderGroup;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetStakeholderGroup;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.SetStakeholderGroupActiveState;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.UpdateStakeholderGroup;
using DemoReleaseChatPlugin.Application.Releases.Stakeholders.AddStakeholder;
using DemoReleaseChatPlugin.Application.Releases.Stakeholders.SetStakeholderActiveState;
using DemoReleaseChatPlugin.Application.Releases.Stakeholders.UpdateStakeholder;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Values = DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases.Admin
{
    /// <summary>
    /// One component, TWO completely different modes, chosen by whether GroupId was supplied.
    /// Closes with DialogService.Close(HasChanged) so the caller knows whether to reload.
    /// </summary>
    public class StakeholderGroupDialogBase : AppComponent
    {
        /// Route / caller parameters arrive as raw Guids; they are converted to typed ids once,
        /// here at the edge, and everything below works in typed ids.
        [Parameter] public Guid? ProjectId { get; set; }
        [Parameter] public bool IsMandatory { get; set; }
        [Parameter] public Guid? GroupId { get; set; }

        private ProjectId Project => Values.ProjectId.FromNullable(ProjectId);
        private StakeholderGroupId Group_ => StakeholderGroupId.FromNullable(GroupId);

        [Inject] protected IEntraService EntraService { get; set; }

        protected bool IsManaging => GroupId.HasValue;

        protected bool HasChanged { get; set; }

        // ----- create mode -------------------------------------------------------------------

        protected CreateStakeholderGroupCommand CreateCommand { get; set; }
        protected FluentValidationValidator CreateValidator { get; set; }

        // ----- manage mode -------------------------------------------------------------------

        protected StakeholderGroupDto Group { get; set; }
        protected UpdateStakeholderGroupCommand EditCommand { get; set; }
        protected FluentValidationValidator EditValidator { get; set; }
        protected List<GroupStakeholderDto> Stakeholders { get; set; }
        protected RadzenDataGrid<GroupStakeholderDto> StakeholderGrid { get; set; }

        /// Disables Add and every Edit button while one row is open, which is what
        /// DataGridEditMode.Single requires to behave.
        protected bool IsEditingStakeholder { get; set; }

        // ----- staff search ------------------------------------------------------------------

        protected List<EntraUser> People { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            if (IsManaging)
            {
                await LoadAsync();
                return;
            }

            CreateCommand = new CreateStakeholderGroupCommand
            {
                ProjectId = Project,
                IsMandatory = IsMandatory,
                Stakeholders = [new NewStakeholderInput()]   // start with one blank row
            };
        }

        protected async Task LoadAsync()
        {
            var result = await Mediator.Send(new GetStakeholderGroupQuery { GroupId = Group_ });

            // "Not found" is its own outcome now: the group was deleted or the link is stale, which
            // is worth saying plainly rather than dressing up as an error.
            if (result.IsNotFound)
            {
                await DialogService.Alert(result.Message, "Stakeholder group not found");
                Close();
                return;
            }

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Group = result.Value.Group;
            Stakeholders = Group.Stakeholders.ToList();

            var edit = await Mediator.Send(new GetUpdateStakeholderGroupQuery { GroupId = Group_ });

            if (edit.IsSuccess)
                EditCommand = edit.Value.Command;

            IsEditingStakeholder = false;
        }

        // ----- create mode -------------------------------------------------------------------

        protected void AddRow() => CreateCommand.Stakeholders.Add(new NewStakeholderInput());

        protected void RemoveRow(NewStakeholderInput row)
        {
            if (CreateCommand.Stakeholders.Count <= 1) return;
            CreateCommand.Stakeholders.Remove(row);
        }

        protected async Task SubmitCreateAsync()
        {
            CreateCommand.Stakeholders = CreateCommand.Stakeholders.Where(IsFilledIn).ToList();

            if (!await IsValidAsync(CreateValidator))
            {
                // Never leave the form with no rows at all.
                if (CreateCommand.Stakeholders.Count == 0)
                    CreateCommand.Stakeholders.Add(new NewStakeholderInput());

                return;
            }

            var result = await Mediator.Send(CreateCommand);

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                CreateCommand.Stakeholders.Add(new NewStakeholderInput());
                return;
            }

            Notify("Stakeholder group created", $"'{CreateCommand.Name}' is ready.");

            DialogService.Close(true);
        }

        /// A group can be created with NO stakeholders - every row blank is legitimate.
        private static bool IsFilledIn(NewStakeholderInput row) =>
            !string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Email);

        // ----- manage mode: group details -----------------------------------------------------

        protected async Task SubmitEditAsync()
        {
            if (!await IsValidAsync(EditValidator)) return;

            var result = await Mediator.Send(EditCommand);

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Notify("Group updated", $"'{EditCommand.Name}' has been saved.");

            HasChanged = true;

            EditCommand = null;

            await LoadAsync();
        }

        /// The wording describes the EFFECT ("stop emailing"), not the mechanism, and says the change
        /// is reversible.
        protected async Task SetGroupActiveAsync(bool isActive)
        {
            var verb = isActive ? "Reactivate" : "Deactivate";

            var message = isActive
                ? $"Start emailing everyone in '{Group.Name}' again?"
                : $"Stop emailing everyone in '{Group.Name}'? Individual members keep their own status.";

            var confirmed = await DialogService.Confirm(
                message, verb, new ConfirmOptions { OkButtonText = verb, CancelButtonText = "Cancel" });

            if (confirmed != true) return;

            var result = await Mediator.Send(new SetStakeholderGroupActiveStateCommand
            {
                GroupId = Group.Id,
                IsActive = isActive
            });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            HasChanged = true;

            await LoadAsync();
        }

        // ----- manage mode: the inline-editable stakeholder grid --------------------------------

        protected async Task StartAddStakeholderAsync()
        {
            IsEditingStakeholder = true;
            await StakeholderGrid.InsertRow(new GroupStakeholderDto { GroupId = Group.Id, IsActive = true });
        }

        protected async Task StartEditStakeholderAsync(GroupStakeholderDto stakeholder)
        {
            IsEditingStakeholder = true;
            await StakeholderGrid.EditRow(stakeholder);
        }

        protected void CancelStakeholderEdit(GroupStakeholderDto stakeholder)
        {
            IsEditingStakeholder = false;
            StakeholderGrid.CancelEditRow(stakeholder);
        }

        protected async Task SaveStakeholderRowAsync(GroupStakeholderDto stakeholder) =>
            await StakeholderGrid.UpdateRow(stakeholder);

        /// <summary>
        /// Radzen's inline grid editing has no FluentValidationValidator in front of it, so this row
        /// reaches the mediator unvalidated. That used to mean catching ValidationException here;
        /// now the pipeline returns the failure on the Result like any other, so there is one path
        /// out of this method instead of two.
        /// </summary>
        protected async Task OnCreateStakeholderRowAsync(GroupStakeholderDto stakeholder)
        {
            var name = stakeholder.Name;

            var result = await Mediator.Send(new AddStakeholderCommand
            {
                GroupId = Group.Id,
                Name = stakeholder.Name,
                Email = stakeholder.EmailText
            });

            if (result.IsFailure)
            {
                await ReopenAsync(stakeholder, result.Message, isNew: true);
                return;
            }

            Notify("Stakeholder added", $"'{name}' has been added to '{Group.Name}'.");

            HasChanged = true;

            await LoadAsync();
        }

        /// Same as OnCreateStakeholderRowAsync: no try/catch, because a rejected row now comes back
        /// as a failed Result.
        protected async Task OnUpdateStakeholderRowAsync(GroupStakeholderDto stakeholder)
        {
            var name = stakeholder.Name;

            var result = await Mediator.Send(new UpdateStakeholderCommand
            {
                StakeholderId = stakeholder.Id,
                GroupId = Group.Id,
                Name = stakeholder.Name,
                Email = stakeholder.EmailText
            });

            if (result.IsFailure)
            {
                await ReopenAsync(stakeholder, result.Message, isNew: false);
                return;
            }

            Notify("Stakeholder updated", $"'{name}' has been saved.");

            HasChanged = true;

            await LoadAsync();
        }

        protected async Task SetStakeholderActiveAsync(GroupStakeholderDto stakeholder, bool isActive)
        {
            var verb = isActive ? "Reactivate" : "Deactivate";

            var message = isActive
                ? $"Start emailing '{stakeholder.Name}' again?"
                : $"Stop emailing '{stakeholder.Name}'? They stay in the group and can be reactivated later.";

            var confirmed = await DialogService.Confirm(
                message, verb, new ConfirmOptions { OkButtonText = verb, CancelButtonText = "Cancel" });

            if (confirmed != true) return;

            var result = await Mediator.Send(new SetStakeholderActiveStateCommand
            {
                StakeholderId = stakeholder.Id,
                IsActive = isActive
            });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            HasChanged = true;

            await LoadAsync();
        }

        /// Puts the row back into edit mode WITH THE TYPED VALUES INTACT after showing the error, so
        /// a duplicate email doesn't lose the user's work.
        private async Task ReopenAsync(GroupStakeholderDto stakeholder, string message, bool isNew)
        {
            await DialogService.Alert(message, "Error");

            IsEditingStakeholder = true;

            if (isNew)
                await StakeholderGrid.InsertRow(stakeholder);
            else
                await StakeholderGrid.EditRow(stakeholder);
        }

        // ----- staff search, shared by both modes -----------------------------------------------

        protected async Task SearchPeopleAsync(LoadDataArgs args)
        {
            var filter = args.Filter;

            if (string.IsNullOrWhiteSpace(filter)) { People = []; return; }

            var results = filter.Contains('@')
                ? await EntraService.SearchUsersByEmail(filter)
                : await EntraService.SearchUsersByFullName(filter);

            People = results?.ToList() ?? [];
        }

        /// Two near-identical pick methods, because the two modes bind different row types.
        protected void PickPerson(NewStakeholderInput row, object person)
        {
            if (person is not EntraUser user) return;

            row.Name = user.Name;

            if (!string.IsNullOrWhiteSpace(user.Email))
                row.Email = user.Email;
        }

        protected void PickPersonForRow(GroupStakeholderDto row, object person)
        {
            if (person is not EntraUser user) return;

            row.Name = user.Name;

            if (!string.IsNullOrWhiteSpace(user.Email))
                row.EmailText = user.Email;
        }

        protected void Close() => DialogService.Close(HasChanged);

        protected async Task ViewGroupHistoryAsync() => await ViewHistory(Group.Id);

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
