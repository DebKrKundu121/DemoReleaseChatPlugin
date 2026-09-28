using System;

namespace DemoReleaseChatPlugin.Contracts.Releases.V1.Common
{
    /// <summary>
    /// Only two kinds cross the wire. There is no <c>Scheduled</c>: a scheduled notice has not
    /// happened yet, so it is not something another system should act on. This is deliberately
    /// NOT the domain's ReleaseNoticeKinds.
    /// </summary>
    public static class ReleaseKinds
    {
        public const string Live = "Live";
        public const string Rollback = "Rollback";
    }

    public class ReleaseDto
    {
        public Guid Id { get; set; }
        public string Kind { get; set; }                 // Live | Rollback
        public string ApplicationName { get; set; }
        public string Version { get; set; }
        public DateTime OccurredOn { get; set; }         // DeployedOn | RolledBackOn
        public string Detail { get; set; }               // ReleaseNotes | RollbackReason
        public string LeadDeveloper { get; set; }
        public bool IsRolledBack { get; set; }           // Live only
        public Guid? RollbackOfReleaseId { get; set; }   // Rollback only
        public DateTime RecordedOn { get; set; }         // when it was filed
    }
}
