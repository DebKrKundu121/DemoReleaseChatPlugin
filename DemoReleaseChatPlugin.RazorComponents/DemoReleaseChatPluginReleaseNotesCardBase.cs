using Microsoft.AspNetCore.Components;

namespace DemoReleaseChatPlugin.RazorComponents;

public class DemoReleaseChatPluginReleaseNotesCardBase : ComponentBase, IDisposable
{
    [Inject] protected DemoReleaseChatPluginReleaseNotesClient Client { get; set; } = default!;

    /// Override the configured application name.
    [Parameter] public string? ApplicationName { get; set; }

    [Parameter] public string Title { get; set; } = "Release Notes";
    [Parameter] public bool ShowTitle { get; set; } = true;

    /// "none" disables the scroll container.
    [Parameter] public string MaxHeight { get; set; } = "300px";

    /// Cap the number of entries.
    [Parameter] public int? Take { get; set; }

    [Parameter] public DateTime? From { get; set; }
    [Parameter] public DateTime? To { get; set; }
    [Parameter] public bool ShowRollbacks { get; set; } = true;

    /// The host's own card class. Defaults to Radzen's, which is present in every Radzen host at
    /// every version - but this component references no Radzen assembly.
    [Parameter] public string ContainerClass { get; set; } = "rz-card rz-variant-filled";

    [Parameter] public string? Style { get; set; }

    [Parameter] public string EmptyMessage { get; set; } = "No releases have been recorded yet.";
    [Parameter] public string EmptyWindowMessage { get; set; } = "No releases in the selected dates.";
    [Parameter] public string ErrorMessage { get; set; } = "Release notes are unavailable at the moment.";

    [Parameter] public RenderFragment<string>? LineTemplate { get; set; }

    protected const string Muted = "color: var(--rz-text-secondary-color, #6c757d)";
    protected const string LatestMarker =
        "display:inline-block; padding:0 .4em; border-radius:.25rem; font-size:.75em; color:#fff; " +
        "background: var(--rz-success, #198754)";
    protected const string DangerMarker =
        "display:inline-block; padding:0 .4em; border-radius:.25rem; font-size:.75em; color:#fff; " +
        "background: var(--rz-danger, #dc3545)";

    protected bool IsLoaded { get; private set; }
    protected DemoReleaseChatPluginReleasesStatus Status { get; private set; }
    protected IReadOnlyList<ReleaseNoteEntry> Entries { get; private set; } = [];

    protected string ResolvedApplicationName =>
        string.IsNullOrWhiteSpace(ApplicationName) ? Client.ConfiguredApplicationName : ApplicationName;

    protected bool HasWindow => From.HasValue || To.HasValue;

    private DemoReleaseChatPluginReleasesOutcome? _outcome;
    private string? _loadedFor;
    private CancellationTokenSource? _cancellation;

    /// <summary>
    /// Re-fetching is KEYED, not parameter-triggered: changing Take or ShowRollbacks re-projects the
    /// cached result without another HTTP call; changing the application or the date window refetches.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        var request = RequestKey();

        if (request == _loadedFor)
        {
            Project();
            return;
        }

        await LoadAsync(request);
    }

    private string RequestKey() => $"{ResolvedApplicationName}|{Day(From)}|{Day(To)}";

    private static string Day(DateTime? value) => value?.ToString("yyyyMMdd") ?? "all";

    private async Task LoadAsync(string request)
    {
        IsLoaded = false;

        // In-flight requests are cancelled when the parameters change again.
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();

        var token = _cancellation.Token;

        try
        {
            _outcome = await Client.GetReleasesAsync(ResolvedApplicationName, From, To, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;

        _loadedFor = request;
        Status = _outcome.Status;

        Project();

        IsLoaded = true;
    }

    private void Project() =>
        Entries = ReleaseNoteEntry.Build(_outcome?.Value, ShowRollbacks, Take);

    public void Dispose()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        GC.SuppressFinalize(this);
    }
}
