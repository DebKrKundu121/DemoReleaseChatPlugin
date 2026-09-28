namespace DemoReleaseChatPlugin.Contracts.Releases.V1
{
    /// <summary>
    /// Routes are constants, not string literals in an attribute. The controller, the Refit
    /// interface and the consumer all bind to the same string, so a route change is a compile-time
    /// event on both sides.
    /// </summary>
    public static class ReleaseApiRoutes
    {
        public const string Version = "/v1";
        public const string Base = "/api" + Version + "/applications";

        public const string GetReleases = Base + "/{applicationName}/releases";
        public const string GetLatestRelease = Base + "/{applicationName}/releases/latest";

        public const string FromQueryParameter = "from";
        public const string ToQueryParameter = "to";
    }
}
