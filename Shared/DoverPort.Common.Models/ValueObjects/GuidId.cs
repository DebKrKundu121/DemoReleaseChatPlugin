using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoverPort.Common.Models.ValueObjects
{
    /// <summary>
    /// A strongly-typed identifier wrapping a <see cref="Guid"/>.
    /// <para>
    /// The point is the compiler. <c>Ship(ProjectId, StakeholderId)</c> cannot be called with its
    /// arguments the wrong way round, where <c>Ship(Guid, Guid)</c> happily compiles and fails at
    /// runtime. There is a one-way implicit conversion TO <see cref="Guid"/> so that a typed id can
    /// still be handed to something that wants a raw one (the Contracts DTOs, a route value); there
    /// is deliberately NO conversion the other way, because that would give the mistake back.
    /// </para>
    /// <para>
    /// Derive as <c>public sealed record ProjectId(Guid Value) : GuidId&lt;ProjectId&gt;(Value)</c>.
    /// Record equality gives value semantics, and <c>TSelf</c> keeps two ids over the same
    /// underlying Guid from being interchangeable.
    /// </para>
    /// </summary>
    public abstract record GuidId<TSelf>(Guid Value) where TSelf : GuidId<TSelf>
    {
        public bool IsEmpty => Value == Guid.Empty;

        public static implicit operator Guid(GuidId<TSelf>? id) => id?.Value ?? Guid.Empty;

        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Serialises a <see cref="GuidId{TSelf}"/> as a bare GUID string rather than as an object with
    /// a <c>Value</c> property.
    /// <para>
    /// This is load-bearing, not cosmetic. Domain events carry these ids and are serialised twice:
    /// into <c>Audit.DomainEvents.Details</c>, which is an append-only table already holding rows in
    /// the flat shape, and into CAP's <c>cap.Published</c> outbox, which may still hold undelivered
    /// messages across a deploy. Without this converter both formats would change and old rows would
    /// no longer round-trip.
    /// </para>
    /// Apply it with <c>[JsonConverter(typeof(GuidIdJsonConverterFactory))]</c> on each concrete id
    /// type. System.Text.Json does not reliably inherit the attribute from an open generic base, so
    /// the attribute goes on the derived record, not here.
    /// </summary>
    public sealed class GuidIdJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => IsGuidId(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(GuidIdJsonConverter<>).MakeGenericType(typeToConvert))!;

        private static bool IsGuidId(Type type)
        {
            for (var candidate = type.BaseType; candidate is not null; candidate = candidate.BaseType)
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(GuidId<>))
                    return true;
            }

            return false;
        }
    }

    internal sealed class GuidIdJsonConverter<TId> : JsonConverter<TId> where TId : GuidId<TId>
    {
        public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? null
                // Every GuidId exposes a single-Guid constructor - the positional record parameter.
                : (TId)Activator.CreateInstance(typeToConvert, reader.GetGuid())!;

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }

            writer.WriteStringValue(value.Value);
        }
    }
}
