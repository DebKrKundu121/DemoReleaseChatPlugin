using System.Reflection;

namespace DemoReleaseChatPlugin.Infrastructure;

public static class FileReader
{
    /// Reads an embedded resource by filename suffix.
    public static string ReadFileFromAssemblyContaining<TSource>(string embeddedFileName) where TSource : class
    {
        var assembly = typeof(TSource).GetTypeInfo().Assembly;

        var resourceName = assembly.GetManifestResourceNames()
            .First(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
            throw new InvalidOperationException("Could not load the file.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
