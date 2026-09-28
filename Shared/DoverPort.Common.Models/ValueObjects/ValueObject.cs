using System.Collections.Concurrent;
using System.Reflection;
using DoverPort.Common.Models.Exceptions;

namespace DoverPort.Common.Models.ValueObjects
{
    /// <summary>
    /// Not used by Releases or Administration - stakeholder emails are plain strings normalised by
    /// Stakeholder.Normalise. Reproduced only so the shim matches the package it stands in for.
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> PropertyCache = new();
        private static readonly ConcurrentDictionary<Type, List<FieldInfo>> FieldCache = new();

        public static bool operator ==(ValueObject? a, ValueObject? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);

        public bool Equals(ValueObject? other) => other is not null && Equals((object)other);

        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType()) return false;

            return GetProperties().All(p => Equals(p.GetValue(this), p.GetValue(obj)))
                && GetFields().All(f => Equals(f.GetValue(this), f.GetValue(obj)));
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach (var prop in GetProperties()) hash.Add(prop.GetValue(this));
            foreach (var field in GetFields()) hash.Add(field.GetValue(this));
            return hash.ToHashCode();
        }

        protected static void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken()) throw new BusinessRuleValidationException(rule);
        }

        private IReadOnlyList<PropertyInfo> GetProperties() =>
            PropertyCache.GetOrAdd(GetType(), t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                 .Where(p => !p.GetIndexParameters().Any() && p.GetCustomAttribute<IgnoreMemberAttribute>() is null)
                 .ToList());

        private IReadOnlyList<FieldInfo> GetFields() =>
            FieldCache.GetOrAdd(GetType(), t =>
                t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 .Where(f => f.GetCustomAttribute<IgnoreMemberAttribute>() is null)
                 .ToList());
    }
}
