namespace DoverPort.Common.Models
{
    /// <summary>
    /// An optional value. A struct, so <c>default</c> is <see cref="None"/> - which is what lets an
    /// Entra lookup for a person who has no manager return a normal value rather than a null or an
    /// exception.
    /// </summary>
    [Serializable]
    public readonly struct Maybe<T> : IEquatable<Maybe<T>>
    {
        public bool HasValue { get; }
        private readonly T? _value;

        public T Value => HasValue ? _value! : throw new InvalidOperationException("No value present.");
        public bool HasNoValue => !HasValue;
        public static Maybe<T> None => default;

        private Maybe(T value)
        {
            if (value == null) { HasValue = false; _value = default; }
            else { HasValue = true; _value = value; }
        }

        public static Maybe<T> From(T value) => new(value);
        public static implicit operator Maybe<T>(T value) => new(value);

        public bool Equals(Maybe<T> other)
        {
            if (HasNoValue && other.HasNoValue) return true;
            if (HasNoValue || other.HasNoValue) return false;
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);
        public override int GetHashCode() => HasValue ? EqualityComparer<T>.Default.GetHashCode(_value!) : 0;
        public static bool operator ==(Maybe<T> a, Maybe<T> b) => a.Equals(b);
        public static bool operator !=(Maybe<T> a, Maybe<T> b) => !a.Equals(b);
        public override string ToString() => HasValue ? _value!.ToString()! : "No value";
    }
}
