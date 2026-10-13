using System.ComponentModel;
using DemoReleaseChatPlugin.Assistant.Canvas;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Tools that put results in the workspace — a full page beside the chat. Every user gets them: they
/// reach no data of their own, they only render what the lookup tools already returned.
///
/// Each returns a sentence telling the model what happened, including whether the table is verified,
/// so it can tell the user the truth about where the figures came from.
public sealed class PresentationTools(IWorkspaceDataStore dataStore, IAssistantWorkspace workspace, TimeProvider timeProvider)
{
    [Description("Starts a new, empty report in the workspace. Call it FIRST when you are about to present " +
                 "results on a DIFFERENT subject from the report already there. Do NOT call it when the user " +
                 "asks to add to or extend the current report.")]
    public string StartNewReport()
    {
        workspace.StartNewReport();

        return "A new report will start with the next thing you present.";
    }

    [Description("Shows a table in the workspace (a full page beside the chat). Use it for a list of records " +
                 "the user wants to look through — not for a single value or a short answer. PREFERRED: pass " +
                 "'source', the exact token a lookup tool returned (e.g. 'src_3'), and no columns or rows; " +
                 "the table is then built from the system's own data and marked Verified. Only when no single " +
                 "token covers what is needed, omit 'source' and pass 'columns' and 'rows' yourself; that " +
                 "table is marked Unverified.")]
    public string PresentTable(
        [Description("A short title for the table, e.g. 'Releases rolled back in 2026'.")]
        string title,
        [Description("The 'source' token from a lookup tool's result. Omit only when passing columns and rows.")]
        string source = null,
        [Description("Column headers. Only when there is no source token.")]
        string[] columns = null,
        [Description("One array of cell values per row, in column order. Only when there is no source token.")]
        string[][] rows = null)
    {
        title = string.IsNullOrWhiteSpace(title) ? "Table" : title.Trim();

        // Preferred path: render the table the lookup tool stashed. The model supplies no values.
        if (!string.IsNullOrWhiteSpace(source))
        {
            var table = dataStore.Get(source);

            if (table is null)
            {
                return $"Could not find source \"{source}\". Use the exact \"source\" token from the tool result " +
                       "(older ones expire — call the lookup tool again if needed), or omit it and pass the " +
                       "columns and rows directly.";
            }

            Show(title, table.Columns, table.Rows, verified: true);

            return $"Rendered a verified {table.Rows.Count}-row table titled \"{title}\" from the system's own data. " +
                   "Tell the user it is ready in the workspace; do not repeat the rows in the chat.";
        }

        // Fallback path: the model assembled the table itself — allowed, but flagged unverified.
        if (columns is null || columns.Length == 0)
        {
            return "Could not render the table: pass a 'source' token from a lookup tool, or provide column headers and rows.";
        }

        rows ??= Array.Empty<string[]>();

        // Every row padded or cut to the column count, so the grid never renders ragged — the model
        // occasionally miscounts, and tolerating it beats failing.
        var normalisedRows = rows
            .Select(row => (IReadOnlyList<string>)Enumerable
                .Range(0, columns.Length)
                .Select(i => i < (row?.Length ?? 0) ? row[i] ?? string.Empty : string.Empty)
                .ToList())
            .ToList();

        Show(title, columns.Select(c => c ?? string.Empty).ToList(), normalisedRows, verified: false);

        return $"Rendered an UNVERIFIED {normalisedRows.Count}-row table titled \"{title}\" from values you supplied. " +
               "Tell the user it is ready in the workspace, and that it was assembled by you rather than taken " +
               "directly from the system, so they should check it.";
    }

    private void Show(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, bool verified)
        => workspace.Show(new AssistantPresentation
        {
            Title = title,
            Kind = PresentationKind.Table,
            CreatedAt = timeProvider.GetLocalNow(),
            Table = new TablePayload { Columns = columns, Rows = rows, Verified = verified }
        });
}
