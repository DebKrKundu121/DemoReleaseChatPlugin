namespace DemoReleaseChatPlugin.Assistant.Canvas;

/// Circuit-scoped cache of the real tables the lookup tools produced during a conversation.
///
/// A tool stashes its result and hands the model a token ("src_1"); present_table renders the table by
/// that token. The model never supplies the values, so a table rendered this way is exactly what the
/// system returned — that is what the workspace's "Verified" chip means.
public interface IWorkspaceDataStore
{
    string Stash(ReportTable table);

    /// Null when the token is unknown or has been evicted.
    ReportTable Get(string token);
}

public static class WorkspaceDataStoreExtensions
{
    /// Null for an empty set — a token for an empty table would only render an empty grid.
    public static string StashRows<T>(this IWorkspaceDataStore store, IReadOnlyCollection<T> rows, params string[] hiddenFields)
        => rows is null || rows.Count == 0 ? null : store.Stash(ReportTable.From(rows, hiddenFields));
}
