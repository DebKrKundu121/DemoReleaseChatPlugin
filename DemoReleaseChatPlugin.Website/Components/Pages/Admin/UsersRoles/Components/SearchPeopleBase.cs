using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization.CreateUser;
using DemoReleaseChatPlugin.Application.Authorization.GetAvailableOptionsForExternalUser;
using DemoReleaseChatPlugin.Application.Entra;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class SearchPeopleBase : AppComponent
    {
        [Microsoft.AspNetCore.Components.Inject] protected IEntraService EntraService { get; set; }

        protected List<EntraUser> People { get; set; } = new();
        protected string SelectedId { get; set; }
        protected EntraUser SelectedPerson { get; set; }
        protected GetAvailableOptionsForExternalUserResult Options { get; set; }
        protected bool IsLoadingOptions { get; set; }

        /// The '@' heuristic picks the search mode.
        protected async Task OnLoadData(LoadDataArgs args)
        {
            var filter = args.Filter;

            if (string.IsNullOrWhiteSpace(filter))
            {
                People = new List<EntraUser>();
                await InvokeAsync(StateHasChanged);
                return;
            }

            var results = filter.Contains('@')
                ? await EntraService.SearchUsersByEmail(filter)
                : await EntraService.SearchUsersByFullName(filter);

            People = results?.ToList() ?? new List<EntraUser>();

            // Required: Radzen's dropdown does not re-render on its own after an async LoadData.
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnChange()
        {
            SelectedPerson = People.FirstOrDefault(x => x.Id == SelectedId);
            Options = null;

            if (SelectedPerson is null) return;

            IsLoadingOptions = true;

            var result = await Mediator.Send(
                new GetAvailableOptionsForExternalUserQuery { ExternalId = SelectedPerson.Id });

            IsLoadingOptions = false;

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Options = result.Value;
        }

        protected async Task CreateUser()
        {
            var created = await Mediator.Send(new CreateUserCommand
            {
                Name = SelectedPerson.Name,
                ExternalId = SelectedPerson.Id
            });

            if (created.IsFailure)
            {
                await DialogService.Alert(created.Message, "Error");
                return;
            }

            // A user with no roles can sign in and see nothing, so the flow pushes you to the next step.
            Notify(NotificationSeverity.Success, "User added",
                $"'{SelectedPerson.Name}' has been added. Assign their roles.");

            Navigation.NavigateTo($"/admin/users-and-roles/{created.Value}");
        }

        protected void ManageUser() =>
            Navigation.NavigateTo($"/admin/users-and-roles/{Options.UserId}");

        private void Notify(NotificationSeverity severity, string summary, string detail) =>
            Notification.Notify(new NotificationMessage
            {
                Severity = severity,
                Summary = summary,
                Detail = detail,
                Duration = 4000
            });
    }
}
