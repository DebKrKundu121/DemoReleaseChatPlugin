namespace DoverPort.Common.Models
{
    /// <summary>
    /// The clock, as a dependency rather than a static call.
    /// <para>
    /// Domain factories and mutating methods take one of these instead of reading
    /// <c>DateTime.Now</c>. A test hands over a clock fixed to a known instant and gets a
    /// deterministic result; reading the ambient clock gives a test that passes today and fails at
    /// midnight, or in another timezone.
    /// </para>
    /// <para>
    /// <see cref="DateTime"/> rather than <see cref="DateTimeOffset"/> on purpose: every timestamp
    /// column in this database is <c>datetime2(3)</c> holding local time, and widening the domain to
    /// offsets would be a schema change, not a refactor.
    /// </para>
    /// </summary>
    public interface IClock
    {
        DateTime Now { get; }
        DateTime Today { get; }
    }

    /// <summary>
    /// The production adapter. It delegates to <see cref="SystemClock"/> rather than calling
    /// <c>DateTime.Now</c> itself, so anything that already relies on <c>SystemClock.Set(...)</c>
    /// keeps working through the injected clock too.
    /// </summary>
    public sealed class SystemClockAdapter : IClock
    {
        public DateTime Now => SystemClock.Now;
        public DateTime Today => SystemClock.Today;
    }
}
