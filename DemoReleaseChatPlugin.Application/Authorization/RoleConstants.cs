namespace DemoReleaseChatPlugin.Application.Authorization;

/// <summary>
/// These strings must match Authorization.Roles.Name EXACTLY. The seed script inserts them; these
/// constants are what [Authorize(Roles = ...)] and &lt;AuthorizeView Roles="..."&gt; compare
/// against. There is no compile-time link between the two - a typo in either just means nobody ever
/// passes.
/// </summary>
public static class RoleConstants
{
    public const string ItAdmin = "IT Admin";
    public const string ItOperations = "IT Operations";
}
