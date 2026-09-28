namespace DemoReleaseChatPlugin.Domain.Authorization;

/// <summary>
/// Plain POCOs with public setters, deliberately NOT Entity&lt;Guid&gt;: they raise no domain events
/// and produce no Audit.DomainEvents rows. User administration is configuration, not a business
/// transaction with a story to tell.
/// </summary>
public class User
{
    /// Database-generated here (plain HasKey, no ValueGeneratedNever), unlike every Releases entity.
    public Guid Id { get; set; }

    public string Name { get; set; }

    /// The Entra object id - the value ClaimsPrincipal.GetId() returns, and the join between the
    /// identity provider and this database.
    public string ExternalId { get; set; }

    public ICollection<Role> Roles { get; set; }
}

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<User> Users { get; set; }
}
