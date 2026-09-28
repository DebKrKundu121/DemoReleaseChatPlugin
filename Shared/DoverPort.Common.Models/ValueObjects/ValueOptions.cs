using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoverPort.Common.Models.ValueObjects
{
    /// <summary>
    /// A closed vocabulary - the "type-safe enum" or "smart enum" pattern. An alternative to a C#
    /// <c>enum</c> for a fixed set of named values, with three advantages that matter here:
    /// <list type="number">
    /// <item>It persists as a stable string. An <c>enum</c> persists as an integer, so reordering
    /// the members silently re-labels every row already written.</item>
    /// <item>It validates. <c>(RiskLevel)47</c> is a legal <c>enum</c> value; there is no way to
    /// hold a <see cref="ValueOptions{TSelf}"/> that is not one of the declared members.</item>
    /// <item>It can carry behaviour, so a rule like <c>IsClosed</c> lives with the vocabulary
    /// instead of being re-written as an <c>if</c> at every call site.</item>
    /// </list>
    /// <para>
    /// Declare members as <c>public static readonly</c> fields of the derived type, built
    /// through its own private constructor. Those fields are the only instances that exist, so
    /// reference equality and value equality agree.
    /// </para>
    /// </summary>
    public abstract class ValueOptions<TSelf> : IEquatable<ValueOptions<TSelf>>
        where TSelf : ValueOptions<TSelf>
    {
        private static readonly Lazy<IReadOnlyList<TSelf>> Declared = new(Reflect);

        public string Value { get; }

        protected ValueOptions(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A vocabulary member must have a value.", nameof(value));

            Value = value;
        }

        /// <summary>Every declared member, in declaration order.</summary>
        public static IReadOnlyList<TSelf> All => Declared.Value;

        public static bool TryParse(string? value, out TSelf? option)
        {
            option = All.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
            return option is not null;
        }

        /// <summary>
        /// Throws when <paramref name="value"/> is not a declared member. That is deliberate: a
        /// value outside the vocabulary means corrupt data or a member that was deleted without a
        /// migration, which is a bug, not an expected failure.
        /// </summary>
        public static TSelf Parse(string? value) =>
            TryParse(value, out var option)
                ? option!
                : throw new InvalidOperationException(
                    $"'{value}' is not a member of {typeof(TSelf).Name}. Declared members: " +
                    $"{string.Join(", ", All.Select(x => x.Value))}.");

        /// <summary>Null-tolerant <see cref="Parse"/>, for optional columns.</summary>
        public static TSelf? ParseOrNull(string? value) => value is null ? null : Parse(value);

        public bool Equals(ValueOptions<TSelf>? other) =>
            other is not null
            && other.GetType() == GetType()
            && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as ValueOptions<TSelf>);

        public override int GetHashCode() => HashCode.Combine(GetType(), Value);

        public static bool operator ==(ValueOptions<TSelf>? a, ValueOptions<TSelf>? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(ValueOptions<TSelf>? a, ValueOptions<TSelf>? b) => !(a == b);

        public override string ToString() => Value;

        private static IReadOnlyList<TSelf> Reflect() =>
            typeof(TSelf)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.IsInitOnly && typeof(TSelf).IsAssignableFrom(f.FieldType))
                .Select(f => (TSelf)f.GetValue(null)!)
                .ToList();
    }

    /// <summary>
    /// Serialises a <see cref="ValueOptions{TSelf}"/> as its bare string value.
    /// <para>
    /// Same reasoning as <see cref="GuidIdJsonConverterFactory"/>: these types travel inside CAP
    /// messages that may already be sitting in <c>cap.Published</c> when a new build starts, so the
    /// on-the-wire shape has to stay a plain string. Apply with
    /// <c>[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]</c> on the concrete type.
    /// </para>
    /// </summary>
    public sealed class ValueOptionsJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => IsValueOptions(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(ValueOptionsJsonConverter<>).MakeGenericType(typeToConvert))!;

        private static bool IsValueOptions(Type type)
        {
            for (var candidate = type.BaseType; candidate is not null; candidate = candidate.BaseType)
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(ValueOptions<>))
                    return true;
            }

            return false;
        }
    }

    internal sealed class ValueOptionsJsonConverter<TOption> : JsonConverter<TOption>
        where TOption : ValueOptions<TOption>
    {
        public override TOption? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? null
                : ValueOptions<TOption>.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }

            writer.WriteStringValue(value.Value);
        }
    }
}
