using DemoReleaseChatPlugin.Application.Releases.Emails;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Application.Releases.Publishing;
using DemoReleaseChatPlugin.Infrastructure.Emails;
using DemoReleaseChatPlugin.Infrastructure.Messaging;
using DemoReleaseChatPlugin.Infrastructure.Persistence;
using DemoReleaseChatPlugin.Infrastructure.Publishing;
using DoverPort.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Savorboard.CAP.InMemoryMessageQueue;
using SendGrid;

namespace DemoReleaseChatPlugin.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DemoReleaseChatPluginConnection";

    /// The CAP dashboard is mounted here and linked from the left menu as an EXTERNAL link
    /// (Target="_blank"), because it is CAP middleware, not a Razor @page - a normal Blazor Path
    /// routes it through the router and renders blank.
    public const string QueueDashboardPath = "/releases/queue";

    private const string QueueDashboardPolicy = "ReleaseQueueDashboard";

    public static void AddDemoReleaseChatPluginInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Migrations deliberately live in Infrastructure, not the Website, so the schema is not
        // owned by the web app.
        services.AddDbContext<DemoReleaseChatPluginDbContext>(options => options
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(DemoReleaseChatPluginDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            }));

        services.AddScoped<IDemoReleaseChatPluginDbContext>(x =>
            x.GetRequiredService<DemoReleaseChatPluginDbContext>());

        // The read side shares the same scoped context - one connection per request - but exposes
        // only no-tracking queryables, so a query handler has no way to write.
        services.AddScoped<IDemoReleaseChatPluginReadDb, DemoReleaseChatPluginReadDb>();

        services.AddScoped<IDemoReleaseChatPluginUnitOfWork, DemoReleaseChatPluginUnitOfWork>();

        // The clock, as a dependency. Domain factories and mutating methods take one instead of
        // reading DateTime.Now, which is what makes them testable at a fixed instant.
        services.AddSingleton<IClock, SystemClockAdapter>();

        services.AddReleaseNotifications(configuration);
        services.AddReleaseMessaging();
    }

    private static void AddReleaseNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new ReleaseEmailSettings
        {
            ApiKey = configuration["SendGrid:ApiKey"],
            FromEmail = configuration["EmailSettings:FromEmail"],
            FromName = configuration["EmailSettings:FromName"],
            IsTestMode = configuration.GetValue("EmailSettings:IsTestMode", true)
        };

        services.AddSingleton(settings);
        services.AddSingleton<IReleaseEmailComposer, ReleaseEmailComposer>();

        // Catches the "{ KEY VAULT | ENVIRONMENT VARIABLE }" placeholder left in appsettings.json
        // when nothing overrode it. Without this, SendGridClient would be constructed with a
        // nonsense key and every send would fail with a confusing 401 instead of the clear
        // "SendGrid is not configured" message.
        var apiKey = settings.ApiKey;
        var hasRealKey = !string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("KEY VAULT");

        if (!hasRealKey)
            settings.ApiKey = null;

        // Still a non-null string because SendGridClient's constructor rejects null - but
        // IsConfigured is false, so SendAsync throws before it is ever used.
        services.AddSingleton<ISendGridClient>(x => new SendGridClient(settings.ApiKey ?? "unconfigured"));

        services.AddScoped<IReleaseAudienceResolver, ReleaseAudienceResolver>();
        services.AddScoped<IReleaseNotificationSender, SendGridReleaseNotificationSender>();

        services.AddScoped<IReleaseNotesPublisher, NoOpReleaseNotesPublisher>();
    }

    private static void AddReleaseMessaging(this IServiceCollection services)
    {
        // CAP discovers ICapSubscribe implementations through the container. A missing AddTransient
        // means the messages publish and nothing ever handles them, SILENTLY. All three are required.
        services.AddTransient<OnReleaseNoticeFiled_QueueAnnouncement>();
        services.AddTransient<OnAdministrationChanged_RecordHistory>();
        services.AddTransient<SendReleaseEmailConsumer>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(QueueDashboardPolicy, policy => policy.RequireAuthenticatedUser());
        });

        services.AddCap(options =>
        {
            // Message bodies use the same converters as the audit table, so a typed id serialises as
            // a bare GUID string and a closed vocabulary as its plain name. Without this, upgrading
            // to those types would change the shape of every message - including ones already
            // sitting in cap.Published from before the deploy.
            ReleaseSerializerOptions.AddConverters(options.JsonSerializerOptions);

            // CAP owns cap.Published, cap.Received and a lock table, and creates them itself on
            // startup. They are NOT in your migrations and must not be added.
            options.UseEntityFramework<DemoReleaseChatPluginDbContext>();

            // Publisher and consumer are the same process and nothing outside this app consumes
            // these messages, so a broker would be an external dependency for nothing. Durability
            // comes from cap.Published, not the transport: the row is committed before dispatch and
            // re-dispatched on startup, so a restart mid-send still delivers.
            options.UseInMemoryMessageQueue();

            options.FailedRetryCount = 10;

            // Retention IS the tracking policy - there is no notification table, by decision.
            options.SucceedMessageExpiredAfter = (int)TimeSpan.FromDays(1).TotalSeconds;
            options.FailedMessageExpiredAfter = (int)TimeSpan.FromDays(15).TotalSeconds;

            options.UseDashboard(dashboard =>
            {
                dashboard.PathMatch = QueueDashboardPath;
                dashboard.AuthorizationPolicy = QueueDashboardPolicy;

                // Load-bearing: CAP would otherwise mark its endpoints AllowAnonymous, which slips
                // past the app's FallbackPolicy. The dashboard shows recipient addresses and full
                // message bodies.
                dashboard.AllowAnonymousExplicit = false;
            });
        });
    }

    /// <summary>
    /// Called only in Development, and failures are LOGGED AND SWALLOWED so a developer without SQL
    /// Server can still run the rest of the app.
    /// </summary>
    public static void EnsureDemoReleaseChatPluginMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoReleaseChatPluginDbContext>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DemoReleaseChatPluginDbContext>();

            dbContext.Database.Migrate();

            if (!environment.IsProduction())
            {
                dbContext.Database.ExecuteSqlRaw(
                    FileReader.ReadFileFromAssemblyContaining<DemoReleaseChatPluginDbContext>(
                        "seed-authorization-users.sql"));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not apply DemoReleaseChatPlugin migrations, so the Releases module will not " +
                "work in this session. The rest of the application is unaffected. Check that SQL " +
                "Server is reachable on the ConnectionStrings:{ConnectionStringName} connection. If " +
                "the error mentions that a database file already exists, an earlier database was " +
                "dropped but its .mdf/.ldf files remain - delete them or re-attach them, then restart.",
                ConnectionStringName);
        }
    }
}
