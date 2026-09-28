using System.Text.Json.Serialization;
using DoverPort.Common.Models.ValueObjects;

namespace DemoReleaseChatPlugin.Domain.Releases.Values;

/// <summary>
/// The identifiers of the Releases module, one type per aggregate.
/// <para>
/// Before these existed, <c>Rollback(Guid liveReleaseId, Guid projectId)</c> and
/// <c>AddStakeholder(Guid groupId, Guid projectId)</c> could be called with the arguments the wrong
/// way round and compiled cleanly; the mistake surfaced as a foreign-key violation, or worse, as a
/// row attached to the wrong parent. With these it is a build error.
/// </para>
/// <para>
/// <c>New()</c> returns a version 7 GUID. Note that this is NOT the "sequential key" win the usual
/// advice promises: SQL Server orders <c>uniqueidentifier</c> by the last six bytes, and v7 puts its
/// timestamp in the first six, so clustered-index locality is no better than v4. It is used because
/// the ids sort chronologically in application code and in logs, which is genuinely useful; the
/// index-fragmentation argument does not apply on this database engine.
/// </para>
/// </summary>
[JsonConverter(typeof(GuidIdJsonConverterFactory))]
public sealed record ProjectId(Guid Value) : GuidId<ProjectId>(Value)
{
    public static ProjectId New() => new(Guid.CreateVersion7());
    public static ProjectId From(Guid value) => new(value);
    public static ProjectId? FromNullable(Guid? value) => value is null ? null : new ProjectId(value.Value);

    public static readonly ProjectId Empty = new(Guid.Empty);
}

/// <summary>
/// Covers all three notice kinds. Scheduled, live and rollback notices share one table and one id
/// space (TPH), so a <see cref="ReleaseNoticeId"/> is meaningful for any of them - which is exactly
/// what <c>Rollback.RollbackOfReleaseId</c> and <c>LiveRelease.ScheduledReleaseId</c> need.
/// </summary>
[JsonConverter(typeof(GuidIdJsonConverterFactory))]
public sealed record ReleaseNoticeId(Guid Value) : GuidId<ReleaseNoticeId>(Value)
{
    public static ReleaseNoticeId New() => new(Guid.CreateVersion7());
    public static ReleaseNoticeId From(Guid value) => new(value);
    public static ReleaseNoticeId? FromNullable(Guid? value) => value is null ? null : new ReleaseNoticeId(value.Value);

    public static readonly ReleaseNoticeId Empty = new(Guid.Empty);
}

[JsonConverter(typeof(GuidIdJsonConverterFactory))]
public sealed record StakeholderGroupId(Guid Value) : GuidId<StakeholderGroupId>(Value)
{
    public static StakeholderGroupId New() => new(Guid.CreateVersion7());
    public static StakeholderGroupId From(Guid value) => new(value);
    public static StakeholderGroupId? FromNullable(Guid? value) => value is null ? null : new StakeholderGroupId(value.Value);

    public static readonly StakeholderGroupId Empty = new(Guid.Empty);
}

[JsonConverter(typeof(GuidIdJsonConverterFactory))]
public sealed record StakeholderId(Guid Value) : GuidId<StakeholderId>(Value)
{
    public static StakeholderId New() => new(Guid.CreateVersion7());
    public static StakeholderId From(Guid value) => new(value);

    public static readonly StakeholderId Empty = new(Guid.Empty);
}
