using DemoReleaseChatPlugin.Application.Entra;
using DemoReleaseChatPlugin.Application.Releases;
using DemoReleaseChatPlugin.Infrastructure;
using DemoReleaseChatPlugin.RazorComponents;
using DemoReleaseChatPlugin.Website.Infrastructure.Auth;
using DemoReleaseChatPlugin.Website.Services.Entra;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddWebsite(this IServiceCollection services, IConfiguration configuration)
        {
            // Mediator's source generator runs in THIS assembly, so this call can only be here.
            services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Transient);

            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddRadzenComponents();

            // The app is its own first consumer of the release-notes card, which is how the
            // integration stays honest.
            services.AddDemoReleaseChatPluginReleaseNotes(configuration);

            services.AddRazorPages().AddMicrosoftIdentityUI();
            services.AddControllersWithViews().AddMicrosoftIdentityUI();
            services.AddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();

            services.AddAuthorization(options =>
            {
                // Deny by default: any endpoint with no explicit policy needs an authenticated user.
                // This plus @attribute [Authorize] in _Imports.razor is what makes the app
                // deny-by-default, and why the public API controller must be explicitly
                // [AllowAnonymous].
                options.FallbackPolicy = options.DefaultPolicy;
            });

            services.AddScoped<IClaimsTransformation, AddRolesClaimsTransformation>();
            services.AddCascadingAuthenticationState();

            services.AddGraphService(configuration);
            services.AddCustomServices(configuration);
        }

        /// User.ReadBasic.All is what the Administration "Search People" tab and the stakeholder
        /// autocomplete need - without it, staff search returns nothing.
        private static void AddGraphService(this IServiceCollection services, IConfiguration configuration)
        {
            var graphScopes = new[] { "User.Read", "User.ReadBasic.All" };

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi(graphScopes)
                .AddMicrosoftGraph(authProvider => new GraphServiceClient(authProvider), graphScopes)
                .AddInMemoryTokenCaches();
        }

        private static void AddCustomServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            services.AddDemoReleaseChatPluginInfrastructure(configuration);

            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

            // Scrutor. The cached decorator wraps the real service with IMemoryCache at a 1-minute
            // sliding expiration.
            services.AddTransient<IEntraService, EntraService>();
            services.Decorate<IEntraService, EntraCachedService>();
        }
    }
}
