using System.Text.Json.Serialization;
using DoverPort.Common.Models.ValueObjects;

namespace DemoReleaseChatPlugin.Domain.Releases.Values;

/// <summary>
/// How risky a scheduled release is thought to be. Optional - most releases carry no risk flag at
/// all, which is why every use site treats null as "no risk declared" rather than as "low".
/// </summary>
[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]
public sealed class RiskLevel : ValueOptions<RiskLevel>
{
    public static readonly RiskLevel Low = new("Low");
    public static readonly RiskLevel Medium = new("Medium");
    public static readonly RiskLevel High = new("High");

    private RiskLevel(string value) : base(value) { }

    /// <summary>
    /// Behaviour living with the vocabulary rather than as an <c>if</c> at each call site: a
    /// high-risk release is the one the announcement email calls out separately.
    /// </summary>
    public bool NeedsProminentWarning => this == High;
}

/// <summary>
/// One member today. It exists so the column and the concept are already there when staging notices
/// are added; every notice is created with <see cref="Production"/>.
/// </summary>
[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]
public sealed class ReleaseEnvironment : ValueOptions<ReleaseEnvironment>
{
    public static readonly ReleaseEnvironment Production = new("Production");

    private ReleaseEnvironment(string value) : base(value) { }
}

/// <summary>
/// Why a given address is on a release announcement: because they are on the project, or because
/// they are on an organisation-wide mandatory group.
/// </summary>
[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]
public sealed class NotificationAudience : ValueOptions<NotificationAudience>
{
    public static readonly NotificationAudience Mandatory = new("Mandatory");
    public static readonly NotificationAudience Stakeholder = new("Stakeholder");

    private NotificationAudience(string value) : base(value) { }
}

/// <summary>
/// The three kinds of release notice, and the TPH discriminator value for each. Replaces the
/// <c>ReleaseNoticeKinds</c> class of <c>const string</c>s: the members are the same strings, so
/// the discriminator column is untouched, but the set is now closed and carries its own behaviour.
/// </summary>
[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]
public sealed class ReleaseNoticeKind : ValueOptions<ReleaseNoticeKind>
{
    public static readonly ReleaseNoticeKind Scheduled = new("Scheduled");
    public static readonly ReleaseNoticeKind Live = new("Live");
    public static readonly ReleaseNoticeKind Rollback = new("Rollback");

    private ReleaseNoticeKind(string value) : base(value) { }

    /// <summary>A notice describing something that has already happened, rather than a plan.</summary>
    public bool HasAlreadyHappened => this != Scheduled;
}

/// <summary>
/// What kind of file a project document came from. It decides how its text was read: a digital PDF,
/// Word file or text file already contains text, so only <see cref="ScannedPdf"/> and
/// <see cref="Image"/> go through OCR.
/// </summary>
[JsonConverter(typeof(ValueOptionsJsonConverterFactory))]
public sealed class DocumentKind : ValueOptions<DocumentKind>
{
    public static readonly DocumentKind DigitalPdf = new("DigitalPdf");
    public static readonly DocumentKind ScannedPdf = new("ScannedPdf");
    public static readonly DocumentKind Image = new("Image");
    public static readonly DocumentKind Word = new("Word");
    public static readonly DocumentKind Text = new("Text");

    private DocumentKind(string value) : base(value) { }

    public bool NeedsOcr => this == ScannedPdf || this == Image;
}
