using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Application.Entra;

public interface IPerson
{
    string Name { get; }
}

/// <summary>
/// The directory lookup behind Administration's "Search People" tab and the stakeholder
/// autocomplete. The interface lives in Core; the implementation lives in the Website because it
/// needs GraphServiceClient.
///
/// When you add a method here, add it to BOTH EntraService and EntraCachedService. A missing method
/// is a compile error, but a pass-through that forgets to cache is silent.
/// </summary>
public interface IEntraService
{
    Task<string> GetUserImageUrl(string objectId);
    Task<Maybe<EntraUser>> GetUserManager(string objectId);
    Task<Maybe<EntraUserDetails>> GetUserDetails(string objectId);
    Task<ICollection<EntraUser>> SearchUsersByEmail(string email);
    Task<ICollection<EntraUser>> SearchUsersByFullName(string fullName);
    Task<ICollection<EntraUser>> SearchUsersByLastName(string lastName);
    Task<ICollection<EntraUser>> SearchUsersByFirstAndLastName(string firstName, string lastName);
}

public record EntraUser : IPerson
{
    /// The Entra object id.
    public string Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string JobTitle { get; init; }
    public string Department { get; init; }
    public string CompanyName { get; init; }
}

public record EntraUserDetails
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string JobTitle { get; init; }

    /// Flattened from Graph's array into a single display string.
    public string BusinessPhones { get; init; }

    public string MobilePhone { get; init; }
    public Maybe<EntraUser> MaybeManager { get; init; }
    public string Department { get; init; }
    public string CompanyName { get; init; }
    public string ImageSrc { get; init; }
}
