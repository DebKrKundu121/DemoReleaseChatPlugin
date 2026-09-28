using System;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization.GetUser;
using DemoReleaseChatPlugin.Application.Authorization.UpdateUserRoles;
using Microsoft.AspNetCore.Components;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles
{
    public class UserDetailBase : AppComponent
    {
        [Parameter] public Guid UserId { get; set; }

        protected GetUserResult User { get; set; }
        protected bool NotFound { get; set; }
        protected bool IsBusy { get; set; }
        protected bool HasAnyRole { get; set; }

        /// Clears the loader on both paths.
        protected bool IsLoaded => !IsBusy && (User is not null || NotFound);

        protected override async Task OnParametersSetAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;
            NotFound = false;

            var userResult = await Mediator.Send(new GetUserQuery { UserId = UserId });

            if (userResult.IsFailure)
            {
                NotFound = true;
                IsBusy = false;
                return;
            }

            User = userResult.Value;

            var rolesResult = await Mediator.Send(new GetUpdateUserRolesQuery { UserId = UserId });

            if (rolesResult.IsSuccess)
                HasAnyRole = rolesResult.Value.Command.Roles.Any(x => x.IsAssigned);

            IsBusy = false;
        }

        protected void Back() => Navigation.NavigateTo("/admin/users-and-roles");
    }
}
