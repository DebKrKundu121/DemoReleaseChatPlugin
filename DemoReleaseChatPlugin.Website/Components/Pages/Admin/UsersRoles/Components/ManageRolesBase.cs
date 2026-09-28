using System;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Application.Authorization.UpdateUserRoles;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class ManageRolesBase : AppComponent
    {
        [Parameter] public Guid UserId { get; set; }
        [Parameter] public string UserName { get; set; }

        protected UpdateUserRolesCommand Command { get; set; }
        protected FluentValidationValidator Validator { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var result = await Mediator.Send(new GetUpdateUserRolesQuery { UserId = UserId });

            // IsNotFound is checked separately from IsFailure: a user who has been deleted since the
            // page was opened is a different situation from a read that could not be completed, and
            // only the second one is an error.
            if (result.IsNotFound)
            {
                await DialogService.Alert(result.Message, "User not found");
                return;
            }

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Command = result.Value.Command;
        }

        protected async Task Submit()
        {
            if (!await IsValidAsync(Validator)) return;

            var result = await Mediator.Send(Command);

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Notification.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Roles updated",
                Detail = $"{UserName}'s roles have been saved.",
                Duration = 4000
            });

            Navigation.NavigateTo("/admin/users-and-roles");
        }
    }
}
