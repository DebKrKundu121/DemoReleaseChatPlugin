using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Entra;
using DoverPort.Common.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DemoReleaseChatPlugin.Website.Services.Entra
{
    /// <summary>
    /// Registered with Scrutor: services.Decorate&lt;IEntraService, EntraCachedService&gt;().
    ///
    /// One minute is enough to stop an autocomplete hammering Graph while a user types, and short
    /// enough that nobody notices staleness. EMPTY AND NULL RESULTS ARE NEVER CACHED, so a person
    /// added to the directory shows up on the next keystroke rather than a minute later.
    ///
    /// When you add a method to IEntraService, add it here too. A missing method is a compile error,
    /// but a pass-through that forgets to cache is silent.
    /// </summary>
    public class EntraCachedService : IEntraService
    {
        private static readonly MemoryCacheEntryOptions CacheOptions =
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1));

        private readonly IEntraService _service;
        private readonly IMemoryCache _cache;

        public EntraCachedService(IEntraService service, IMemoryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        public async Task<string> GetUserImageUrl(string objectId)
        {
            var key = $"img-for-objectId-{objectId}";

            if (_cache.TryGetValue(key, out string? fromCache) && !string.IsNullOrWhiteSpace(fromCache))
                return fromCache;

            var result = await _service.GetUserImageUrl(objectId);

            if (!string.IsNullOrWhiteSpace(result))
                _cache.Set(key, result, CacheOptions);

            return result;
        }

        public async Task<Maybe<EntraUser>> GetUserManager(string objectId)
        {
            var key = $"manager-for-objectId-{objectId}";

            if (_cache.TryGetValue(key, out Maybe<EntraUser> fromCache) && fromCache.HasValue)
                return fromCache;

            var result = await _service.GetUserManager(objectId);

            if (result.HasValue)
                _cache.Set(key, result, CacheOptions);

            return result;
        }

        public async Task<Maybe<EntraUserDetails>> GetUserDetails(string objectId)
        {
            var key = $"user-details-for-objectId-{objectId}";

            if (_cache.TryGetValue(key, out Maybe<EntraUserDetails> fromCache) && fromCache.HasValue)
                return fromCache;

            var result = await _service.GetUserDetails(objectId);

            if (result.HasValue)
                _cache.Set(key, result, CacheOptions);

            return result;
        }

        public Task<ICollection<EntraUser>> SearchUsersByEmail(string email) =>
            SearchAsync($"search-results-for-email-{email}", () => _service.SearchUsersByEmail(email));

        public Task<ICollection<EntraUser>> SearchUsersByFullName(string fullName) =>
            SearchAsync($"search-results-for-fullName-{fullName}", () => _service.SearchUsersByFullName(fullName));

        public Task<ICollection<EntraUser>> SearchUsersByLastName(string lastName) =>
            SearchAsync($"search-results-for-lastName-{lastName}", () => _service.SearchUsersByLastName(lastName));

        public Task<ICollection<EntraUser>> SearchUsersByFirstAndLastName(string firstName, string lastName) =>
            SearchAsync(
                $"search-results-for-names-{firstName}:{lastName}",
                () => _service.SearchUsersByFirstAndLastName(firstName, lastName));

        private async Task<ICollection<EntraUser>> SearchAsync(
            string key, Func<Task<ICollection<EntraUser>>> search)
        {
            if (_cache.TryGetValue(key, out ICollection<EntraUser>? fromCache) && fromCache is not null)
                return fromCache;

            var results = await search();

            if (results is not null && results.Any())
                _cache.Set(key, results, CacheOptions);

            return results;
        }
    }
}
