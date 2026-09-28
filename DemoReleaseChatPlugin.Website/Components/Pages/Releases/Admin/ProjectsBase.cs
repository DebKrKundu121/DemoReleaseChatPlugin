using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Application.Releases.Projects.CreateProject;
using DemoReleaseChatPlugin.Application.Releases.Projects.GetProjects;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases.Admin
{
    public class ProjectsBase : AppComponent
    {
        protected List<GetProjectsResult.ProjectDto> Projects { get; set; }
        protected bool IsBusy { get; set; }
        protected bool ShowArchived { get; set; }

        protected bool IsCreating { get; set; }
        protected CreateProjectCommand Command { get; set; } = new();
        protected FluentValidationValidator Validator { get; set; }

        protected override async Task OnInitializedAsync() => await LoadAsync();

        protected async Task LoadAsync()
        {
            IsBusy = true;

            var result = await Mediator.Send(new GetProjectsQuery { IncludeArchived = ShowArchived });

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                Projects ??= [];
                IsBusy = false;
                return;
            }

            Projects = result.Value.Projects.ToList();
            IsBusy = false;
        }

        protected async Task OnShowArchivedChanged(bool value)
        {
            ShowArchived = value;
            await LoadAsync();
        }

        protected void StartCreating()
        {
            Command = new CreateProjectCommand();
            IsCreating = true;
        }

        protected void CancelCreating() => IsCreating = false;

        protected async Task SubmitAsync()
        {
            if (!await IsValidAsync(Validator)) return;

            var name = Command.Name;

            var result = await Mediator.Send(Command);

            if (result.IsFailure)
            {
                await DialogService.Alert(result.Message, "Error");
                return;
            }

            Notification.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Project created",
                Detail = $"'{name}' is ready to record releases against.",
                Duration = 4000
            });

            IsCreating = false;

            await LoadAsync();
        }

        protected void Manage(Guid projectId) =>
            Navigation.NavigateTo($"/releases/admin/projects/{projectId}");
    }
}
