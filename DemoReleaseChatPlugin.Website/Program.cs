using DemoReleaseChatPlugin.Application;
using DemoReleaseChatPlugin.Infrastructure;
using DemoReleaseChatPlugin.Website.Components;
using DemoReleaseChatPlugin.Website.Infrastructure;
using DoverPort.AspNetCore.Common.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// The original layers Azure Key Vault and Azure App Configuration on top of appsettings.json:
//
//     builder.AddPodKeyVault("DemoReleaseChatPlugin");
//     builder.AddPodConfigurationStorage("DemoReleaseChatPlugin");
//
// Both are optional conveniences and both are omitted here, so everything comes from
// appsettings.json + appsettings.Development.json + user-secrets. Nothing else in the app depends
// on them. (AddPodConfigurationStorage in particular THROWS when
// ConnectionStrings:AzureConfiguration is missing.)

var configuration = builder.Configuration;

builder.AddDemoReleaseChatPluginApplication();
builder.Services.AddWebsite(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Services.EnsureDemoReleaseChatPluginMigrations();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Forces en-GB process-wide. Every date in this app is dd/MM/yyyy HH:mm; without this you get US
// ordering and the date pickers parse wrongly.
app.UsePodCulture();

app.UseHttpsRedirection();

// Order matters: UseAntiforgery before UseAuthentication/UseAuthorization.
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// After MapRazorComponents, so the API controller and the Blazor router coexist. Miss this line and
// the API routes 404 with no other symptom.
app.MapControllers();

app.Run();
