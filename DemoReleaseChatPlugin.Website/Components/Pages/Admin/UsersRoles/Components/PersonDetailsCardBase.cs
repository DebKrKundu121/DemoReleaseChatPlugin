using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Entra;
using DoverPort.Common.Models;
using Microsoft.AspNetCore.Components;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Admin.UsersRoles.Components
{
    public class PersonDetailsCardBase : AppComponent
    {
        [Parameter] public string ExternalId { get; set; }

        [Inject] protected IEntraService Entra { get; set; }

        protected bool Loaded { get; set; }
        protected Maybe<EntraUserDetails> Details { get; set; }
        protected EntraUserDetails Person => Details.Value;

        private string _loadedFor;

        /// <summary>
        /// The _loadedFor guard matters. OnParametersSetAsync fires on every parent re-render;
        /// without it, each keystroke elsewhere on the page triggers a Graph call.
        /// </summary>
        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrWhiteSpace(ExternalId) || ExternalId == _loadedFor)
                return;

            _loadedFor = ExternalId;
            Loaded = false;

            Details = await Entra.GetUserDetails(ExternalId);

            Loaded = true;
        }
    }
}
