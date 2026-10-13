using System;
using System.Linq;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DemoReleaseChatPlugin.Website.Services.Assistant
{
    /// Turns the assistant's Markdown replies into HTML for the panel.
    ///
    /// The output is put on the page as a MarkupString, which is only safe because of two guards here.
    /// Model output can repeat text people wrote (release notes, project descriptions), so treat it as
    /// untrusted. If you swap the renderer, keep both guards or add a sanitiser.
    public static class MarkdownRenderer
    {
        private static readonly string[] SafeSchemes = { "http", "https", "mailto" };

        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .DisableHtml()          // guard 1: raw HTML in a reply is shown as text, never run
            .Build();

        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            var document = Markdown.Parse(markdown, Pipeline);

            // Guard 2: DisableHtml doesn't cover Markdown's own links, and [x](javascript:...) runs
            // script when clicked. Unsafe targets go nowhere. Images become plain links, so a planted
            // image can't call an outside server just by being displayed.
            foreach (var link in document.Descendants<LinkInline>())
            {
                link.IsImage = false;

                if (!IsSafeUrl(link.Url))
                {
                    link.Url = "#";
                }
            }

            return document.ToHtml(Pipeline);
        }

        /// http, https, mailto, or a link within this app. Whitespace and control characters are
        /// ignored first, because browsers ignore them too ("java\tscript:" still runs).
        private static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return true;
            }

            var cleaned = new string(url.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());
            var colon = cleaned.IndexOf(':');
            var pathStart = cleaned.IndexOfAny(new[] { '/', '?', '#' });

            // No scheme, or a ':' that comes after a path, query or fragment: a relative link.
            if (colon < 0 || (pathStart >= 0 && pathStart < colon))
            {
                return true;
            }

            var scheme = cleaned[..colon];
            return SafeSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
        }
    }
}
