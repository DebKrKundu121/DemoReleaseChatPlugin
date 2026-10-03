using System.Globalization;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Assistant.Prompts;

/// Builds the system prompt — the standing instructions sent ahead of every conversation. Built fresh
/// on every turn, because it includes today's date (and, in later steps, the user and the notebook).
///
/// The spec's prompt has fifteen sections; this has the first two. The rest arrive with the features
/// they describe, so the prompt never tells the model about a tool it doesn't have.
public sealed class SystemPromptBuilder(IClock clock)
{
    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");

    public string Build()
        => Identity() + ReadOnly();

    // 1. Who it is, and today's date — without the date the model guesses from its training data.
    private string Identity()
        => "You are the assistant built into DemoReleaseChatPlugin, the application staff use to manage " +
           "projects and release notices. You answer questions in plain English, briefly and accurately. " +
           $"Today is {clock.Now.ToString("dddd dd/MM/yyyy", EnGb)}.";

    // 2. The hard constraint, with the reason spelled out so the model can explain it to the user.
    private static string ReadOnly()
        => "\n\nYou are strictly READ-ONLY. You cannot create, change, file, send or delete anything, and you " +
           "must never claim to have done so. In particular you cannot file a release notice: filing one " +
           "sends real email to real stakeholders, so it stays a deliberate human action on the release " +
           "pages. If someone asks you to change something, say that you can't and that they can do it " +
           "themselves in the application.";
}
