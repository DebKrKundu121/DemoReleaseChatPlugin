namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// Who did something. Attribution is resolved server-side through <c>ICurrentUserProvider</c>,
/// never taken from a command - <c>CreatedBy</c> is never a command property.
/// <c>LeadDeveloper</c> is, because it is a business field: someone may file a notice on the lead
/// developer's behalf.
/// </summary>
public sealed record Actor(string UserId, string DisplayName);
