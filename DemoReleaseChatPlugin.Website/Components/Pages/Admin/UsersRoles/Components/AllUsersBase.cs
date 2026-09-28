using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization.GetUsersWithRoles;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class AllUsersBase : AppComponent
    {
        protected List<GetUsersWithRolesResult.UserDto> Users { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;

            var result = await Mediator.Send(new GetUsersWithRolesQuery());

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Users ??= [];
                IsBusy = false;
                return;
            }

            Users = result.Value.Users.ToList();
            IsBusy = false;
        }

        protected void ManageUser(Guid userId) => Navigation.NavigateTo($"/admin/users-and-roles/{userId}");
    }
}
