using DoverPort.Common.Models.Exceptions;

namespace DoverPort.Common.Models
{
    /// <summary>
    /// The load-bearing type of the whole Releases module. Entities collect the domain events they
    /// raise in their own constructors and methods; the unit of work drains them on commit.
    /// </summary>
    public abstract class Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        /// <summary>
        /// Raise an event and stamp the moment it happened from the supplied clock, rather than
        /// letting the event read the ambient <c>DateTime.Now</c> in its own initialiser. The
        /// timestamp ends up in <c>Audit.DomainEvents.OccurredOn</c> and in the human-readable
        /// history timeline, so it has to be as controllable in a test as the rest of the entity.
        /// <para>
        /// <see cref="IDomainEvent.Id"/> is still generated here. It is a technical correlation id
        /// that never reaches a business decision, so it is not worth a second injected dependency.
        /// </para>
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent, IClock clock)
        {
            if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
            if (clock == null) throw new ArgumentNullException(nameof(clock));

            if (domainEvent.Id == Guid.Empty) domainEvent.Id = Guid.CreateVersion7();

            domainEvent.OccurredOn = clock.Now;

            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        protected void CheckRule(IBusinessRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (rule.IsBroken()) throw new BusinessRuleValidationException(rule);
        }
    }

    public abstract class Entity<T> : Entity, IEquatable<Entity<T>>
    {
        public T Id { get; protected set; } = default!;

        public bool IsTransient => EqualityComparer<T>.Default.Equals(Id, default!);

        public override bool Equals(object? obj) => obj is Entity<T> other && Equals(other);

        public virtual bool Equals(Entity<T>? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;
            if (IsTransient || other.IsTransient) return false;
            return EqualityComparer<T>.Default.Equals(Id, other.Id);
        }

        public static bool operator ==(Entity<T>? a, Entity<T>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(Entity<T>? a, Entity<T>? b) => !(a == b);

        public override int GetHashCode() =>
            IsTransient
                ? base.GetHashCode()
                : HashCode.Combine(GetUnproxiedType(this).GetHashCode(), EqualityComparer<T>.Default.GetHashCode(Id!));

        /// Unwraps EF Core / Castle lazy-loading proxies so a proxied entity still equals its plain counterpart.
        internal static Type GetUnproxiedType(object obj)
        {
            var type = obj.GetType();
            if (type.Namespace == "Castle.Proxies" || type.Name.EndsWith("Proxy"))
                return type.BaseType!;
            return type;
        }
    }
}
