# `Shared/` — stand-ins for internal DoverPort NuGet packages

Nothing in this folder is a layer of the application. Each project here reproduces one internal
Port of Dover NuGet package that does not exist outside the Port's network, **under that package's
original namespace**. On a machine with access to the internal feed, every one of these would be
deleted and replaced by a single line:

```xml
<PackageReference Include="DoverPort.Common.Models" Version="..." />
```

The project names are the package names on purpose, so that swap is a one-line change per
reference rather than a restructure. Do not rename the namespaces for tidiness — the hundreds of
`using DoverPort.*;` lines across the solution are written against the real packages.

| Project | Depends on | Used by |
|---|---|---|
| `DoverPort.Common.Models` | **nothing** | Domain, Application, Infrastructure, Website |
| `DoverPort.Common.Pipeline` | `Common.Models`, ASP.NET Core, Mediator, FluentValidation | Application |
| `DoverPort.Emails` | **nothing** | Infrastructure |
| `DoverPort.AspNetCore.Common` | ASP.NET Core, Microsoft.Identity.Web | Website |

`DoverPort.Common.Models` having no dependencies at all is the point of the split. It is what
`DemoReleaseChatPlugin.Domain` references, so the domain project's reference graph now contains no
ASP.NET Core, no authentication library and no serialisation package — only the BCL.
