namespace DemoReleaseChatPlugin.Application.Releases.Notices;

/// <summary>
/// The shared return for all three create commands. It carries ONLY the id, and the page says
/// "Stakeholders will be notified by email shortly." - no recipient count. The handler never looked
/// at the mailing list, and nothing has been sent when the page responds, so a number there would
/// be invented.
/// </summary>
public record FileNoticeResult
{
    public Guid ReleaseNoticeId { get; set; }
}
