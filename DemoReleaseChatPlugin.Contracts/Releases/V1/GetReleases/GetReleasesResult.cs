using System;
using System.Collections.Generic;
using DemoReleaseChatPlugin.Contracts.Releases.V1.Common;

namespace DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases
{
    public class GetReleasesResult
    {
        public string ApplicationName { get; set; }
        public IReadOnlyList<ReleaseDto> Releases { get; set; } = new List<ReleaseDto>();
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        /// <summary>
        /// Computed BEFORE date filtering, so a consumer asking for last month's window still learns
        /// which entry is the current one - it just may not be in the returned list.
        /// </summary>
        public Guid? LatestReleaseId { get; set; }
    }
}
