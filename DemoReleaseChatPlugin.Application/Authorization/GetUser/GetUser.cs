using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.GetUser;

public record GetUserQuery : IRequest<Result<GetUserResult>>
{
    public Guid UserId { get; set; }
}

public record GetUserResult
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ExternalId { get; set; }
}

public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class GetUserHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUserQuery, Result<GetUserResult>>
{
    public async ValueTask<Result<GetUserResult>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await readDb.Users
            .Where(x => x.Id == request.UserId)
            .Select(x => new GetUserResult { Id = x.Id, Name = x.Name, ExternalId = x.ExternalId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<GetUserResult>.Error("User not found.");

        return Result<GetUserResult>.OK(user);
    }
}
