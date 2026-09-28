using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace DoverPort.AspNetCore.Common.Configurations
{
    public static class GlobalizationSettings
    {
        /// <summary>
        /// Forces en-GB process-wide: dd/MM/yyyy dates. Every date in this app is dd/MM/yyyy HH:mm;
        /// without this you get US ordering and the date pickers parse wrongly.
        /// </summary>
        public static void UsePodCulture(this IApplicationBuilder app)
        {
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }
    }
}
