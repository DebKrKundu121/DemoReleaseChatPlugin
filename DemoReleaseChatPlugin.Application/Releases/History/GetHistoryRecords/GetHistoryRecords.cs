using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.History.GetHistoryRecords;

/// <summary>
/// <c>CorrelationId</c> stays a raw <see cref="Guid"/> - it is a project id OR a stakeholder-group
/// id depending on which screen asked, so no single typed id is the honest type for it.
/// </summary>
public record GetHistoryRecordsQuery : IRequest<Result<GetHistoryRecordsResult>>
{
    public Guid CorrelationId { get; set; }
}

public record GetHistoryRecordsResult
{
    public IReadOnlyList<RecordDto> Records { get; set; } = [];

    public record RecordDto
    {
        public string Description { get; set; }
        public DateTime HappenedOn { get; set; }
        public string HappenedBy { get; set; }
    }
}

public class GetHistoryRecordsValidator : AbstractValidator<GetHistoryRecordsQuery>
{
    public GetHistoryRecordsValidator()
    {
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}

public class GetHistoryRecordsHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetHistoryRecordsQuery, Result<GetHistoryRecordsResult>>
{
    public async ValueTask<Result<GetHistoryRecordsResult>> Handle(
        GetHistoryRecordsQuery request, CancellationToken cancellationToken)
    {
        var records = await readDb.HistoryRecords
            .Where(x => x.CorrelationId == request.CorrelationId)
            .OrderByDescending(x => x.HappenedOn)
            .Select(x => new GetHistoryRecordsResult.RecordDto
            {
                Description = x.Description,
                HappenedOn = x.HappenedOn,
                HappenedBy = x.HappenedBy
            })
            .ToListAsync(cancellationToken);

        return Result<GetHistoryRecordsResult>.OK(new GetHistoryRecordsResult { Records = records });
    }
}
