using System.Security.Claims;
using Microsoft.Identity.Web;

namespace DoverPort.AspNetCore.Common.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// The Entra object id (the <c>oid</c> claim). This is what Authorization.Users.ExternalId
        /// stores and what the whole role lookup keys on - get it wrong and nobody ever resolves a role.
        /// </summary>
        public static string? GetId(this ClaimsPrincipal principal) =>
            principal.FindFirst(ClaimConstants.ObjectId)?.Value;

        public static bool HasId(this ClaimsPrincipal principal) =>
            principal.FindFirst(ClaimConstants.ObjectId) != null;

        public static bool IsAuthenticated(this ClaimsPrincipal principal) =>
            principal.Identity is { IsAuthenticated: true };

        public static string? GetName(this ClaimsPrincipal principal) =>
            principal.FindFirst("name")?.Value
            ?? principal.Identity?.Name
            ?? principal.FindFirst("preferred_username")?.Value;

        public static string GetDisplayName(this ClaimsPrincipal principal)
        {
            var name = principal.FindFirst("name")?.Value;
            if (!string.IsNullOrWhiteSpace(name)) return name;

            var first = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var last = principal.FindFirst(ClaimTypes.Surname)?.Value;

            var combined = $"{first} {last}".Trim();
            if (!string.IsNullOrWhiteSpace(combined)) return combined;

            return principal.Identity?.Name ?? principal.GetEmail() ?? string.Empty;
        }

        public static string? GetEmail(this ClaimsPrincipal principal) =>
            principal.FindFirst("emails")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;

        public static IEnumerable<string> GetEmails(this ClaimsPrincipal principal) =>
            principal.Claims
                .Where(c => c.Type == "emails" || c.Type == ClaimTypes.Email)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct();

        public static string? GetIdentityProvider(this ClaimsPrincipal principal) =>
            principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/identityprovider");
    }
}
