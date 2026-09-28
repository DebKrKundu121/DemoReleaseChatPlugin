using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups;
using DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetMandatoryStakeholderGroups;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases.Admin
{
    public class MandatoryStakeholdersBase : AppComponent
    {
        protected List<StakeholderGroupDto> Groups { get; set; }
        protected bool IsBusy { get; set; }
        protected bool ShowInactive { get; set; }

        protected override async Task OnInitializedAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;

            var result = await Mediator.Send(new GetMandatoryStakeholderGroupsQuery
            {
                IncludeInactive = ShowInactive
            });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Groups ??= [];
                IsBusy = false;
                return;
            }

            Groups = result.Value.Groups.ToList();
            IsBusy = false;
        }

        protected async Task OnShowInactiveChanged(bool value)
        {
            ShowInactive = value;
            await LoadAsync();
        }

        protected async Task AddGroupAsync()
        {
            var changed = await DialogService.OpenAsync<StakeholderGroupDialog>(
                "Add mandatory stakeholder group",
                new Dictionary<string, object>
                {
                    [nameof(StakeholderGroupDialog.IsMandatory)] = true
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
                    [nameof(StakeholderGroupDialog.IsMandatory)] = true,
                    [nameof(StakeholderGroupDialog.GroupId)] = group.Id
                },
                new DialogOptions { Width = "860px" });

            if (changed is true)
                await LoadAsync();
        }
    }
}
