using DemoReleaseChatPlugin.Application.Releases.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.CreateProject;

/// <summary>
/// Shape and uniqueness only. "Is this name already taken?" is a question about every OTHER row, so
/// it cannot be answered by the aggregate and belongs here. Rules about a project's own state - an
/// archived one refusing edits, for instance - live on <c>Project</c> instead.
/// </summary>
public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateProjectValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(BeUniqueName)
            .WithMessage("A project with this name already exists.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;

        var candidate = name.Trim();

        return !await _readDb.Projects.AnyAsync(x => x.Name == candidate, cancellationToken);
    }
}
