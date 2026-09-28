using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;

/// <summary>
/// The mapping between the domain's value types and the columns they were always stored in.
/// <para>
/// NONE OF THIS CHANGES THE SCHEMA. A typed id is still a <c>uniqueidentifier</c>, a closed
/// vocabulary is still the same <c>nvarchar</c> string the old <c>enum</c> was converted to, and an
/// <see cref="EmailAddress"/> is still <c>nvarchar(256)</c>. That is the whole reason these are
/// value converters rather than owned types - the existing migration and every existing row stay
/// valid.
/// </para>
/// <para>
/// The trade-off to know about: a converted property is opaque to the SQL translator. Comparing it
/// (<c>x.Id == someProjectId</c>) or projecting it whole (<c>Id = x.Id</c>) works; reading INSIDE it
/// in a query (<c>x.Email.Value</c>, <c>x.RiskLevel.ToString()</c>) does not. Queries that need the
/// raw string project the value object out and unwrap it in memory.
/// </para>
/// </summary>
internal static class ValueConverters
{
    public static ValueConverter<TId, Guid> ForId<TId>(Func<Guid, TId> fromGuid) where TId : GuidId<TId> =>
        new(id => id.Value, value => fromGuid(value));

    public static ValueConverter<TOption, string> ForVocabulary<TOption>() where TOption : ValueOptions<TOption> =>
        new(option => option.Value, value => ValueOptions<TOption>.Parse(value));

    public static readonly ValueConverter<EmailAddress, string> ForEmail =
        new(email => email.Value, value => EmailAddress.FromStorage(value));

    /// <summary>
    /// Records compare by value, so the default comparer is already correct for equality. This is
    /// declared explicitly anyway because EF otherwise falls back to reference comparison for a
    /// converted reference type, and would then report a stakeholder's email as "changed" on every
    /// load where the instance differs but the address does not.
    /// </summary>
    public static readonly ValueComparer<EmailAddress> EmailComparer =
        new((left, right) => left == right,
            email => email.Value.GetHashCode(),
            email => email);

    public static ValueComparer<TId> IdComparer<TId>() where TId : GuidId<TId> =>
        new((left, right) => left == right,
            id => id.Value.GetHashCode(),
            id => id);

    public static ValueComparer<TOption> VocabularyComparer<TOption>() where TOption : ValueOptions<TOption> =>
        new((left, right) => left == right,
            option => option.Value.GetHashCode(),
            option => option);

    /// <summary>A required typed id, stored as its raw Guid.</summary>
    public static PropertyBuilder<TId> HasIdConversion<TId>(
        this PropertyBuilder<TId> builder, Func<Guid, TId> fromGuid) where TId : GuidId<TId>
    {
        builder.HasConversion(ForId(fromGuid), IdComparer<TId>());
        return builder;
    }

    /// <summary>
    /// The same, for a column that is genuinely optional - <c>StakeholderGroup.ProjectId</c> and
    /// <c>Stakeholder.ProjectId</c> are NULL for a mandatory (organisation-wide) group, and
    /// <c>LiveRelease.ScheduledReleaseId</c> is NULL for an unplanned hotfix.
    /// <para>
    /// A separate method rather than an overload: for a reference type <c>TId</c> and <c>TId?</c>
    /// are the same type at runtime, so two overloads would be duplicate signatures. Naming it
    /// separately also makes the nullability a stated decision at each call site rather than
    /// something inferred from the property.
    /// </para>
    /// <para>
    /// EF never hands a null to a converter - it writes NULL and reads it back as null - so the
    /// <c>!</c> below is safe.
    /// </para>
    /// </summary>
    public static PropertyBuilder<TId?> HasOptionalIdConversion<TId>(
        this PropertyBuilder<TId?> builder, Func<Guid, TId> fromGuid) where TId : GuidId<TId>
    {
        builder.HasConversion(
            new ValueConverter<TId?, Guid>(id => id!.Value, value => fromGuid(value)),
            new ValueComparer<TId?>(
                (left, right) => left == right,
                id => id!.Value.GetHashCode(),
                id => id));

        return builder;
    }

    public static PropertyBuilder<TOption> HasVocabularyConversion<TOption>(
        this PropertyBuilder<TOption> builder) where TOption : ValueOptions<TOption>
    {
        builder.HasConversion(ForVocabulary<TOption>(), VocabularyComparer<TOption>());
        return builder;
    }

    public static PropertyBuilder<EmailAddress> HasEmailConversion(this PropertyBuilder<EmailAddress> builder)
    {
        builder.HasConversion(ForEmail, EmailComparer);
        return builder;
    }
}
