using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization.GetRolesByExternalId;
using DoverPort.AspNetCore.Common.Auth;
using Mediator;
using Microsoft.AspNetCore.Authentication;

namespace DemoReleaseChatPlugin.Website.Infrastructure.Auth
{
    /// <summary>
    /// Entra ID says WHO YOU ARE; this turns the database's roles into claims so
    /// [Authorize(Roles = ...)] and &lt;AuthorizeView Roles="..."&gt; say WHAT YOU MAY DO.
    ///
    /// Role changes take effect at the user's NEXT SIGN-IN, not immediately.
    /// </summary>
    public class AddRolesClaimsTransformation : IClaimsTransformation
    {
        private readonly IMediator _mediator;

        public AddRolesClaimsTransformation(IMediator mediator) => _mediator = mediator;

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Clone FIRST, always. IClaimsTransformation can run more than once per request;
            // mutating the original accumulates duplicate claims.
            var clone = principal.Clone();

            var externalId = clone.GetId();

            if (string.IsNullOrWhiteSpace(externalId))
                return clone;

            var result = await _mediator.Send(new GetRolesByExternalIdQuery { ExternalId = externalId });

            // A failure returns the principal unchanged rather than throwing - the user signs in
            // with no roles and sees an empty menu, instead of a 500 at the auth layer.
            if (result.IsFailure)
                return clone;

            AddRoles((ClaimsIdentity)clone.Identity!, result.Value);

            return clone;
        }

        private static void AddRoles(ClaimsIdentity identity, IEnumerable<string> roles)
        {
            foreach (var role in roles)
                if (!identity.HasClaim(ClaimTypes.Role, role))   // second guard against duplicates
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }
}
