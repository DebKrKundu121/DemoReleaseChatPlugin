using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.DeleteUser;

public record DeleteUserCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
}

public class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator() => RuleFor(x => x.UserId).NotEmpty();
}

/// <summary>
/// A HARD DELETE, unlike everything in Releases. Nothing references Users.Id - attribution on
/// release notices stores the Entra id and display name as plain strings - so deleting a user
/// cannot orphan history. The join rows in Authorization.UserRoles go with it via the cascade.
/// </summary>
public class DeleteUserHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Error("User not found.");

        dbContext.Users.Remove(user);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
