using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Stakeholders.UpdateStakeholder;

/// <c>Email</c> stays a raw string on the COMMAND: it is what a textbox on the page is bound to,
/// and it is still unvalidated user input at that point. It becomes an <see cref="EmailAddress"/>
/// the moment the handler hands it to the domain, and never travels as a string past there.
public record UpdateStakeholderCommand : IRequest<Result>
{
    public StakeholderId StakeholderId { get; set; }
    public StakeholderGroupId GroupId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public record GetUpdateStakeholderQuery : IRequest<QueryResult<GetUpdateStakeholderResult>>
{
    public StakeholderId StakeholderId { get; set; }
}

public record GetUpdateStakeholderResult
{
    public UpdateStakeholderCommand Command { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateStakeholderValidator : AbstractValidator<UpdateStakeholderCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public UpdateStakeholderValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.StakeholderId).NotNull();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(EmailAddress.MaxLength)
            .Must(BeAWellFormedAddress).WithMessage("Please give a valid email address.")
            .MustAsync(BeUniqueInScope)
            .WithMessage("This email is already a stakeholder here. A person can only belong to one group.");
    }

    private static bool BeAWellFormedAddress(string email) => EmailAddress.Of(email).IsSuccess;

    /// Resolves scope from the STAKEHOLDER being edited, and excludes that row.
    private async Task<bool> BeUniqueInScope(
        UpdateStakeholderCommand command, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;

        var stakeholder = await _readDb.Stakeholders
            .FirstOrDefaultAsync(x => x.Id == command.StakeholderId, cancellationToken);

        if (stakeholder is null) return true;

        var address = EmailAddress.Of(email);

        if (address.IsFailure) return true;

        return !await _readDb.Stakeholders
            .AnyAsync(
                x => x.ProjectId == stakeholder.ProjectId
                     && x.Email == address.Value
                     && x.Id != command.StakeholderId,
                cancellationToken);
    }
}

public class GetUpdateStakeholderValidator : AbstractValidator<GetUpdateStakeholderQuery>
{
    public GetUpdateStakeholderValidator() => RuleFor(x => x.StakeholderId).NotNull();
}

public class GetUpdateStakeholderHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUpdateStakeholderQuery, QueryResult<GetUpdateStakeholderResult>>
{
    public async ValueTask<QueryResult<GetUpdateStakeholderResult>> Handle(
        GetUpdateStakeholderQuery request, CancellationToken cancellationToken)
    {
        // Projected into the value-object shape first, then flattened to the command's string Email
        // in memory - a value-converted column cannot have .Value read from it inside SQL.
        var stakeholder = await readDb.Stakeholders
            .Where(x => x.Id == request.StakeholderId)
            .Select(x => new { x.Id, x.GroupId, x.Name, x.Email, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (stakeholder is null)
            return QueryResult<GetUpdateStakeholderResult>.NotFound("Stakeholder not found.");

        return QueryResult<GetUpdateStakeholderResult>.Of(new GetUpdateStakeholderResult
        {
            IsActive = stakeholder.IsActive,
            Command = new UpdateStakeholderCommand
            {
                StakeholderId = stakeholder.Id,
                GroupId = stakeholder.GroupId,
                Name = stakeholder.Name,
                Email = stakeholder.Email?.Value
            }
        });
    }
}

public class UpdateStakeholderHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStakeholderCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateStakeholderCommand request, CancellationToken cancellationToken)
    {
        var stakeholder = await dbContext.Stakeholders
            .FirstOrDefaultAsync(x => x.Id == request.StakeholderId, cancellationToken);

        if (stakeholder is null)
            return Result.Error("Stakeholder not found.");

        var email = EmailAddress.Of(request.Email);
        if (email.IsFailure) return Result.Error(email.Message);

        var actor = await currentUser.ProvideAsync();

        var result = stakeholder.UpdateDetails(request.Name, email.Value, actor, clock);
        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
