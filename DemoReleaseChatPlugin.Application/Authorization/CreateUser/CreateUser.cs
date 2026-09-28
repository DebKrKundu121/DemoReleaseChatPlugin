using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.CreateUser;

public record CreateUserCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; }
    public string ExternalId { get; set; }
}

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateUserValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUnique)
            .WithMessage("This person is already a user.");
    }

    private async Task<bool> BeUnique(string externalId, CancellationToken cancellationToken) =>
        !await _readDb.Users.AnyAsync(x => x.ExternalId == externalId, cancellationToken);
}

/// No ICurrentUserProvider - nothing here is attributed. Still commits through the unit of work,
/// because consistency of the write path matters more than the micro-optimisation.
public class CreateUserHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Name = request.Name, ExternalId = request.ExternalId };

        dbContext.Users.Add(user);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.OK(user.Id);   // populated by the DB after commit
    }
}
