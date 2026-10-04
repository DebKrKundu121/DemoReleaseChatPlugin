using System.ComponentModel;
using System.Globalization;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Tools that reach no protected data, so every user gets them.
///
/// The [Description] text IS the contract with the model — it is the only thing telling it when to
/// reach for a tool. Treat a description change as a behaviour change.
public sealed class SystemTools(TimeProvider timeProvider)
{
    [Description("Returns the current date, time and day of the week. Use it whenever the answer depends on " +
                 "the current time, or to work out relative dates such as 'last week' or 'this month'.")]
    public object GetCurrentDateTime()
    {
        var now = timeProvider.GetLocalNow();

        // yyyy-MM-dd is an unambiguous wire format for the model; it writes dates as DD/MM/YYYY for people.
        return new
        {
            date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            time = now.ToString("HH:mm", CultureInfo.InvariantCulture),
            dayOfWeek = now.DayOfWeek.ToString()
        };
    }
}
