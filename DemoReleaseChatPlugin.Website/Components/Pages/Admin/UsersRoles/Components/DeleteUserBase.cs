using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen;
using DeleteUserSlice = DemoReleaseChatPlugin.Application.Authorization.DeleteUser;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class DeleteUserBase : AppComponent
    {
        [Parameter] public Guid UserId { get; set; }
        [Parameter] public string UserName { get; set; }

        protected bool IsActionConfirmed { get; set; }

        protected async Task DeleteUser()
        {
            var result = await Mediator.Send(new DeleteUserSlice.DeleteUserCommand { UserId = UserId });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Notification.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "User removed",
                Detail = $"'{UserName}' has been removed.",
                Duration = 4000
            });

            Navigation.NavigateTo("/admin/users-and-roles");
        }
    }
}
