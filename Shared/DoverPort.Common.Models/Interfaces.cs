namespace DoverPort.Common.Models
{
    public interface IAggregateRoot { }

    public interface IBusinessRule
    {
        bool IsBroken();
        string Message { get; }
    }

    public interface IDomainEvent
    {
        Guid Id { get; set; }
        DateTime OccurredOn { get; set; }
        string OccurredBy { get; set; }
    }

    /// <summary>
    /// Not used by this app's own events - Releases declares its own ReleaseDomainEvent /
    /// AdministrationDomainEvent bases with the same three members. Reproduced because it costs
    /// nothing and other code may reference it.
    /// </summary>
    public abstract class DomainEventBase : IDomainEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime OccurredOn { get; set; } = SystemClock.Now;
        public string OccurredBy { get; set; } = string.Empty;
    }

    public interface IRepository { }

    public interface IRepository<TEntity, in TKey> : IRepository
    {
        Task<TEntity> LoadAsync(TKey id, CancellationToken cancellationToken);
        Task SaveAsync(TEntity entity, CancellationToken cancellationToken);
    }

    public class ActionPossible
    {
        public bool IsPossible { get; }
        public string Message { get; }
        public bool IsNotPossible => !IsPossible;

        private ActionPossible(bool isPossible, string message)
        {
            IsPossible = isPossible;
            Message = message;
        }

        public static ActionPossible True() => new(true, "OK");
        public static ActionPossible False(string message) => new(false, message);
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnoreMemberAttribute : Attribute { }
}
