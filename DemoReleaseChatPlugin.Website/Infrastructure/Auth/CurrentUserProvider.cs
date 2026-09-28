using System;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases;
using DemoReleaseChatPlugin.Domain.Releases;
using DoverPort.AspNetCore.Common.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace DemoReleaseChatPlugin.Website.Infrastructure.Auth
{
    public class CurrentUserProvider : ICurrentUserProvider
    {
        private const string SystemUserId = "system";
        private const string SystemDisplayName = "System";

        private readonly AuthenticationStateProvider _provider;

        public CurrentUserProvider(AuthenticationStateProvider provider) => _provider = provider;

        public async Task<Actor> ProvideAsync()
        {
            if (_provider is null)
                return new Actor(SystemUserId, SystemDisplayName);

            try
            {
                var state = await _provider.GetAuthenticationStateAsync();

                if (state?.User?.Identity is null || !state.User.Identity.IsAuthenticated)
                    return new Actor(SystemUserId, SystemDisplayName);

                var userId = state.User.GetId();
                var displayName = state.User.GetDisplayName();

                return new Actor(
                    string.IsNullOrWhiteSpace(userId) ? SystemUserId : userId,
                    string.IsNullOrWhiteSpace(displayName) ? SystemDisplayName : displayName);
            }
            catch (Exception)
            {
                // Intentionally broad: attribution failing must never fail the write.
                return new Actor(SystemUserId, SystemDisplayName);
            }
        }
    }
}
