using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DoverPort.Common.Models.Users;
using DoverPort.Common.Pipeline.Internal;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace DoverPort.Common.Pipeline;

/// <summary>
/// Registers the four mediator behaviours and a default user provider. The only two methods this
/// app chains are <see cref="WithValidatorsFromAssemblyContaining{T}"/> and
/// <see cref="UseDataAnnotationDisplayNames"/>.
/// </summary>
public class PodPipelineBuilder
{
    private readonly IHostApplicationBuilder _innerBuilder;

    internal PodPipelineBuilder(IHostApplicationBuilder innerBuilder)
    {
        _innerBuilder = innerBuilder;

        HealthChecksBuilder = AddHealthChecks();

        AddDefaultsUserProvider();
        AddMediatorBehaviours();
    }

    public IHealthChecksBuilder HealthChecksBuilder { get; }
    internal IConfiguration Configuration => _innerBuilder.Configuration;
    internal IServiceCollection Services => _innerBuilder.Services;
    internal IHostEnvironment Environment => _innerBuilder.Environment;

    public PodPipelineBuilder WithValidatorsFromAssemblyContaining<T>() where T : class
    {
        Services.AddValidatorsFromAssemblyContaining<T>();
        return this;
    }

    /// Lets FluentValidation use [Display(Name = "...")] for property names in messages.
    public PodPipelineBuilder UseDataAnnotationDisplayNames()
    {
        ValidatorOptions.Global.DisplayNameResolver = (_, memberInfo, _) =>
        {
            if (memberInfo == null) return null;
            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute != null ? displayAttribute.Name : memberInfo.Name;
        };

        return this;
    }

    // TryAdd so a consumer (here, the Website) can override it with a real one.
    private void AddDefaultsUserProvider() =>
        Services.TryAddSingleton<IUserProvider, PipelineUserProvider>();

    private void AddMediatorBehaviours()
    {
        Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPerformanceBehaviour<,>));
        Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggerBehaviour<,>));
        Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionsLoggerBehaviour<,>));

        var loggingSettings = Configuration.GetSection("LoggingSettings");

        Services.AddTransient(_ => new LoggingSettings
        {
            EnableRequestLogger = loggingSettings.GetValue<bool>("EnableRequestLogger"),
            EnableRequestPerformanceLogger = loggingSettings.GetValue<bool>("EnableRequestPerformanceLogger"),
            EnableRichExceptionLogger = loggingSettings.GetValue<bool>("EnableRichExceptionLogger"),
            LongRequestIndicatorThreshold = loggingSettings.GetValue<int>("LongRequestIndicatorThreshold")
        });
    }

    private IHealthChecksBuilder AddHealthChecks()
    {
        Services.AddRequestTimeouts();
        Services.AddOutputCache();

        Services.AddRequestTimeouts(configure: static t => t.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));
        Services.AddOutputCache(configureOptions: static c =>
            c.AddPolicy("HealthChecks", build: static p => p.Expire(TimeSpan.FromSeconds(15))));

        return Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }
}
