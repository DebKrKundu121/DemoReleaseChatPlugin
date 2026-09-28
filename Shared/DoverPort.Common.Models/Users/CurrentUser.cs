namespace DoverPort.Common.Models.Users;

public class CurrentUser
{
    public string Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string PreferredEmail { get; }
    public bool IsAuthenticated { get; }

    public CurrentUser(string id, string name, string displayName, string preferredEmail, bool isAuthenticated)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        PreferredEmail = preferredEmail;
        IsAuthenticated = isAuthenticated;
    }

    public UserIdentity GenerateIdentity() => new(Id, DisplayName);
}

public class AnonymousUser : CurrentUser
{
    public AnonymousUser() : base("Unknown", "Unknown", "Unknown", string.Empty, true) { }
}

public class SystemUser : CurrentUser
{
    public SystemUser() : base("System", "System", "System", string.Empty, true) { }
}

public record UserIdentity(string Id, string Name);

/// Resolved by the pipeline so log lines can name who made a request.
public interface IUserProvider
{
    Task<CurrentUser> ProvideAsync(CancellationToken cancellationToken = default);
}
