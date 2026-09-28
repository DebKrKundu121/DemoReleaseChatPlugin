using System;
using System.Threading;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Contracts.Releases.V1;
using DemoReleaseChatPlugin.Contracts.Releases.V1.Common;
using DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases;
using DemoReleaseChatPlugin.Application.Releases.Notices.GetApplicationReleases;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoReleaseChatPlugin.Website.Api
{
    /// <summary>
    /// The whole integration surface: two read endpoints. Other applications ASK; this app does not
    /// announce.
    ///
    /// [AllowAnonymous] is a decision AND load-bearing. The decision: this app is internal-network
    /// only, and requiring tokens would mean an Entra app registration per consumer - anyone who can
    /// reach it can read every release note. REVISIT THIS BEFORE PUBLISHING THE APP MORE WIDELY.
    /// The mechanism: the app's FallbackPolicy denies any endpoint carrying no authorization
    /// metadata, so without the attribute every API call redirects to the sign-in page - which a
    /// service-to-service consumer experiences as an HTML login page with a 200 status, the most
    /// confusing failure available.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    public class ReleasesController(IMediator mediator) : ControllerBase
    {
        [HttpGet(ReleaseApiRoutes.GetReleases)]
        [ProducesResponseType(typeof(GetReleasesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetReleasesResult>> GetReleases(
            string applicationName,
            [FromQuery(Name = ReleaseApiRoutes.FromQueryParameter)] DateTime? from,
            [FromQuery(Name = ReleaseApiRoutes.ToQueryParameter)] DateTime? to,
            CancellationToken cancellationToken)
        {
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid date range",
                    Detail = $"'{ReleaseApiRoutes.FromQueryParameter}' is after " +
                             $"'{ReleaseApiRoutes.ToQueryParameter}', so the range covers no days at all."
                });

            var result = await mediator.Send(
                new GetApplicationReleasesQuery { ApplicationName = applicationName, From = from, To = to },
                cancellationToken);

            // Three outcomes, three status codes. Before, every non-success became a 404, so a
            // broken read told a consumer its application name was wrong.
            if (result.IsNotFound)
                return NotFound(new ProblemDetails { Title = "Application not found", Detail = result.Message });

            if (result.IsFailure)
                return Problem(title: "Could not read releases", detail: result.Message);

            return Ok(result.Value);
        }

        [HttpGet(ReleaseApiRoutes.GetLatestRelease)]
        [ProducesResponseType(typeof(ReleaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ReleaseDto>> GetLatestRelease(
            string applicationName, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new GetLatestApplicationReleaseQuery { ApplicationName = applicationName }, cancellationToken);

            if (result.IsNotFound)
                return NotFound(new ProblemDetails { Title = "No release found", Detail = result.Message });

            if (result.IsFailure)
                return Problem(title: "Could not read the latest release", detail: result.Message);

            return Ok(result.Value);
        }
    }
}
