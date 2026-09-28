using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Entra;
using DoverPort.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace DemoReleaseChatPlugin.Website.Services.Entra
{
    /// <summary>
    /// Wraps GraphServiceClient. Needs the delegated Graph permissions User.Read and
    /// User.ReadBasic.All with admin consent - without User.ReadBasic.All, staff search returns
    /// nothing.
    /// </summary>
    public class EntraService : IEntraService
    {
        private const string PlaceholderImage =
            "data:image/svg+xml;utf8," +
            "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='120'>" +
            "<rect width='120' height='120' fill='%23dee2e6'/></svg>";

        private static readonly string[] SearchFields =
            ["id", "displayName", "mail", "jobTitle", "department", "companyName"];

        private static readonly string[] DetailFields =
            ["id", "displayName", "mail", "jobTitle", "businessPhones", "mobilePhone", "department", "companyName"];

        private readonly GraphServiceClient _graph;
        private readonly ILogger<EntraService> _logger;

        public EntraService(GraphServiceClient graph, ILogger<EntraService> logger)
        {
            _graph = graph;
            _logger = logger;
        }

        public async Task<ICollection<EntraUser>> SearchUsersByFullName(string fullName) =>
            await SearchAsync($"startswith(displayName,'{Escape(fullName)}')");

        public async Task<ICollection<EntraUser>> SearchUsersByEmail(string email) =>
            await SearchAsync($"startswith(mail,'{Escape(email)}')");

        public async Task<ICollection<EntraUser>> SearchUsersByLastName(string lastName) =>
            await SearchAsync($"startswith(surname,'{Escape(lastName)}')");

        public async Task<ICollection<EntraUser>> SearchUsersByFirstAndLastName(string firstName, string lastName) =>
            await SearchAsync(
                $"startswith(givenName,'{Escape(firstName)}') and startswith(surname,'{Escape(lastName)}')");

        public async Task<Maybe<EntraUserDetails>> GetUserDetails(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) return Maybe<EntraUserDetails>.None;

            try
            {
                var user = await _graph.Users[objectId].GetAsync(request =>
                    request.QueryParameters.Select = DetailFields);

                if (user is null) return Maybe<EntraUserDetails>.None;

                return Maybe<EntraUserDetails>.From(new EntraUserDetails
                {
                    Id = user.Id,
                    Name = user.DisplayName,
                    Email = user.Mail,
                    JobTitle = user.JobTitle,
                    BusinessPhones = user.BusinessPhones is null
                        ? null
                        : string.Join(", ", user.BusinessPhones),
                    MobilePhone = user.MobilePhone,
                    Department = user.Department,
                    CompanyName = user.CompanyName,
                    MaybeManager = await GetUserManager(objectId),
                    ImageSrc = await GetUserImageUrl(objectId)
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not read directory details for {ObjectId}.", objectId);
                return Maybe<EntraUserDetails>.None;
            }
        }

        /// A user with no manager is a normal 404, not an error.
        public async Task<Maybe<EntraUser>> GetUserManager(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) return Maybe<EntraUser>.None;

            try
            {
                var manager = await _graph.Users[objectId].Manager.GetAsync();

                return manager is User user
                    ? Maybe<EntraUser>.From(Map(user))
                    : Maybe<EntraUser>.None;
            }
            catch (Exception)
            {
                return Maybe<EntraUser>.None;
            }
        }

        /// Falls back to a placeholder image when the person has no photo.
        public async Task<string> GetUserImageUrl(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) return PlaceholderImage;

            try
            {
                await using var stream = await _graph.Users[objectId].Photo.Content.GetAsync();

                if (stream is null) return PlaceholderImage;

                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                return $"data:image/jpeg;base64,{Convert.ToBase64String(memory.ToArray())}";
            }
            catch (Exception)
            {
                return PlaceholderImage;
            }
        }

        private async Task<ICollection<EntraUser>> SearchAsync(string filter)
        {
            try
            {
                var response = await _graph.Users.GetAsync(request =>
                {
                    request.QueryParameters.Filter = filter;
                    request.QueryParameters.Select = SearchFields;
                    request.QueryParameters.Top = 25;
                    request.QueryParameters.Orderby = ["displayName"];
                });

                return response?.Value?.Select(Map).ToList() ?? [];
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Directory search failed for filter {Filter}.", filter);
                return [];
            }
        }

        private static EntraUser Map(User user) => new()
        {
            Id = user.Id,
            Name = user.DisplayName,
            Email = user.Mail,
            JobTitle = user.JobTitle,
            Department = user.Department,
            CompanyName = user.CompanyName
        };

        /// OData string literals escape a single quote by doubling it.
        private static string Escape(string value) => value?.Replace("'", "''") ?? string.Empty;
    }
}
