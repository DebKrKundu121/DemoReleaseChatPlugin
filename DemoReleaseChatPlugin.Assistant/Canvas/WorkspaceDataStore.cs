namespace DemoReleaseChatPlugin.Assistant.Canvas;

/// Keeps the 20 most recent tables, so a long conversation can't grow it without limit. An evicted
/// token returns null and present_table tells the model to look the data up again.
///
/// Not thread-safe: a circuit drives it one turn at a time.
public sealed class WorkspaceDataStore : IWorkspaceDataStore
{
    public const int MaxEntries = 20;

    private readonly Dictionary<string, ReportTable> _tables = new();
    private readonly Queue<string> _order = new();
    private int _sequence;

    public string Stash(ReportTable table)
    {
        var token = "src_" + (++_sequence);
        _tables[token] = table;
        _order.Enqueue(token);

        while (_order.Count > MaxEntries)
        {
            _tables.Remove(_order.Dequeue());
        }

        return token;
    }

    public ReportTable Get(string token)
        => token is not null && _tables.TryGetValue(token.Trim(), out var table) ? table : null;
}
