using System.Text;

namespace DoverPort.Emails;

/// <summary>
/// Builds the HTML body of an outbound email. The only type from DoverPort.Emails that Releases
/// uses - ReleaseEmailComposer drives it.
/// </summary>
public class PodEmailBuilder
{
    // Substitute your own brand assets here. The originals pointed at an internal CDN and carried
    // a registered-address footer that does not belong in a rebuild elsewhere.
    private const string LogoUrl = "https://via.placeholder.com/180x48?text=Logo";
    private const string FooterText =
        "This is an automated message. Please do not reply to it.";

    private readonly StringBuilder _sb = new();

    public PodEmailBuilder() => RenderHeading();

    public PodEmailBuilder AppendFromHeading(string from)
    {
        _sb.Append($"<div style=\"font-size: 20px\">Port of Dover | {from}</div>");
        return this;
    }

    public PodEmailBuilder AppendTitle(string title)
    {
        _sb.Append($"<div style=\"font-size: 30px\">{title}</div>");
        return this;
    }

    public PodEmailBuilder AppendParagraph(string text)
    {
        _sb.Append($"<p>{text}</p>");
        return this;
    }

    // Nothing in Releases calls this; a simple anchor is enough.
    public PodEmailBuilder AppendButtonLink(string link, string text)
    {
        _sb.Append($"<p><a href=\"{link}\" target=\"_blank\">{text}</a></p>");
        return this;
    }

    public PodEmailBuilder AppendSignature(string text, string from)
    {
        _sb.Append($"<div>{text}</div>");
        _sb.Append($"<div>{from}</div>");
        return this;
    }

    public string GenerateContent()
    {
        RenderFooter();
        return _sb.ToString();
    }

    private void RenderHeading()
    {
        _sb.Append("<p>");
        _sb.Append($"<img width=\"180\" src=\"{LogoUrl}\">");
        _sb.Append("</p>");
    }

    private void RenderFooter()
    {
        _sb.Append("<br><br><hr>");
        _sb.Append($"<div style=\"font-size: 10px\">{FooterText}</div>");
    }
}
