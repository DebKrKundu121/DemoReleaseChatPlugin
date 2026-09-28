using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization.GetRolesWithUsers;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class AllRolesBase : AppComponent
    {
        protected List<GetRolesWithUsersResult.RoleDto> Roles { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;

            var result = await Mediator.Send(new GetRolesWithUsersQuery());

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Roles ??= [];
                IsBusy = false;
                return;
            }

            Roles = result.Value.Roles.ToList();
            IsBusy = false;
        }

        protected void ManageUser(Guid userId) => Navigation.NavigateTo($"/admin/users-and-roles/{userId}");
    }
}
