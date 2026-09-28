using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases.Values;

/// <summary>
/// A stakeholder's email address: validated once, at creation, and normalised to lower case so the
/// uniqueness index and the value stored always agree.
/// <para>
/// This replaces a plain <c>string Email</c> plus a <c>Stakeholder.Normalise(...)</c> helper that
/// three separate validators had to remember to call. Anywhere the code now holds an
/// <see cref="EmailAddress"/>, it is a trimmed, lower-cased, non-empty address containing exactly
/// one <c>@</c> with something either side of it - no re-checking, because there is no way to build
/// one that is not.
/// </para>
/// <para>
/// A record, so equality is by value, and with a private constructor so <see cref="Of"/> is the only
/// way in. <see cref="Of"/> returns a <see cref="Result{T}"/> rather than throwing: a mistyped
/// address is a normal thing for a user to do, not a bug.
/// </para>
/// </summary>
public sealed record EmailAddress
{
    public const int MaxLength = 256;

    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Of(string value)
    {
        var candidate = Normalise(value);

        if (string.IsNullOrEmpty(candidate))
            return Result<EmailAddress>.Error("An email address is required.");

        if (candidate.Length > MaxLength)
            return Result<EmailAddress>.Error($"An email address cannot be longer than {MaxLength} characters.");

        var at = candidate.IndexOf('@');

        if (at <= 0 || at != candidate.LastIndexOf('@') || at == candidate.Length - 1)
            return Result<EmailAddress>.Error($"'{value?.Trim()}' is not a valid email address.");

        return Result<EmailAddress>.OK(new EmailAddress(candidate));
    }

    /// <summary>
    /// For values already known to be good - a row read back out of the database, which was
    /// validated on the way in. Throws otherwise, because that would mean corrupt data.
    /// </summary>
    public static EmailAddress FromStorage(string value)
    {
        var result = Of(value);

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(
                $"The stored email address '{value}' is not valid: {result.Message}");
    }

    /// <summary>
    /// The normalisation on its own, for the uniqueness checks that compare a user's raw input
    /// against stored values before an <see cref="EmailAddress"/> has been built.
    /// </summary>
    public static string Normalise(string value) => value?.Trim()?.ToLowerInvariant() ?? string.Empty;

    public override string ToString() => Value;
}
