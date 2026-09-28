using System.Text.Json;
using DoverPort.Common.Models.ValueObjects;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence;

/// <summary>
/// The one set of JSON options used for everything that leaves this module as serialised data:
/// the <c>Audit.DomainEvents.Details</c> column and every CAP message body.
/// <para>
/// Its whole job is to register the two converter factories, so a strongly-typed id serialises as
/// <c>"a3f1..."</c> rather than <c>{"Value":"a3f1..."}</c>, and a closed vocabulary as
/// <c>"Scheduled"</c> rather than <c>{"Value":"Scheduled"}</c>. Without it, introducing those types
/// would silently change the shape of an append-only audit table and break any message still
/// sitting in <c>cap.Published</c> across a deploy.
/// </para>
/// <para>
/// The concrete types also carry <c>[JsonConverter]</c> attributes, which covers anything that
/// serialises them without these options. Both are kept: the attribute is the safety net, this is
/// the explicit contract.
/// </para>
/// </summary>
public static class ReleaseSerializerOptions
{
    /// <summary>For code that owns its own serializer call, such as the audit row.</summary>
    public static readonly JsonSerializerOptions Default = AddConverters(new JsonSerializerOptions());

    /// <summary>
    /// For an options object somebody else owns - CAP exposes its
    /// <c>JsonSerializerOptions</c> read-only, so the converters are added to the instance it
    /// already has. Safe at startup; a <see cref="JsonSerializerOptions"/> only becomes immutable
    /// once it has been used to serialise something.
    /// </summary>
    public static JsonSerializerOptions AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new GuidIdJsonConverterFactory());
        options.Converters.Add(new ValueOptionsJsonConverterFactory());

        return options;
    }
}
