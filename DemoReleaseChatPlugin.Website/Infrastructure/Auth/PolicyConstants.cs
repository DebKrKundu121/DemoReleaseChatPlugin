namespace DemoReleaseChatPlugin.Website.Infrastructure.Auth
{
    /// <summary>
    /// Releases and Administration gate on the DATABASE ROLE directly
    /// ([Authorize(Roles = RoleConstants.ItAdmin)]), not on a named policy - by decision. Don't add
    /// AD-group policies here; the AdGroups class of hard-coded AD group GUIDs belongs to the People
    /// module, which is out of scope.
    ///
    /// The only named policy the Releases module needs is ReleaseQueueDashboard, registered inside
    /// AddReleaseMessaging in the Infrastructure project.
    /// </summary>
    public static class PolicyConstants
    {
        public const string ReleaseQueueDashboard = "ReleaseQueueDashboard";
    }
}
