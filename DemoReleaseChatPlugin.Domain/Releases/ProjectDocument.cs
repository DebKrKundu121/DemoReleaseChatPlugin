using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// A document read by the Assistant's intake pipeline (OCR, then NER) and attached to a project.
/// <para>
/// Its own aggregate holding a <see cref="ProjectId"/>, like <see cref="ReleaseNotice"/>, rather than
/// a collection inside <see cref="Project"/>: the extracted text can be long, and loading a project to
/// rename it should not drag every document's text along.
/// </para>
/// <para>
/// The original file is not kept - only what was read from it. <see cref="Entities"/> is the NER
/// output as JSON, for the same reason <see cref="DomainEventRecord.Details"/> is: it is for reading,
/// not for querying by field, and its shape belongs to the pipeline, not to the database.
/// </para>
/// </summary>
public sealed class ProjectDocument : Entity<ProjectDocumentId>, IAggregateRoot
{
    public ProjectId ProjectId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public DocumentKind Kind { get; private set; }
    public int PageCount { get; private set; }

    public string Summary { get; private set; }
    public string ExtractedText { get; private set; }
    public string Entities { get; private set; }

    /// Null when the text was read directly (digital PDF, Word, text) and OCR never ran.
    public string OcrProvider { get; private set; }
    public string NerProvider { get; private set; }

    /// How sure the classifier was, 0 to 1, and why. Null when the user picked the project themselves.
    public decimal? MatchConfidence { get; private set; }
    public string MatchReason { get; private set; }

    public DateTime AddedOn { get; private set; }
    public string AddedBy { get; private set; }
    public string AddedByUserId { get; private set; }

    private ProjectDocument() { }   // EF

    private ProjectDocument(
        ProjectDocumentId id, ProjectId projectId, string fileName, string contentType, long sizeBytes,
        DocumentKind kind, int pageCount, string summary, string extractedText, string entities,
        string ocrProvider, string nerProvider, decimal? matchConfidence, string matchReason,
        Actor actor, IClock clock)
    {
        Id = id;
        ProjectId = projectId;
        FileName = fileName?.Trim();
        ContentType = contentType?.Trim();
        SizeBytes = sizeBytes;
        Kind = kind;
        PageCount = pageCount;
        Summary = summary?.Trim();
        ExtractedText = extractedText;
        Entities = entities;
        OcrProvider = ocrProvider;
        NerProvider = nerProvider;
        MatchConfidence = matchConfidence;
        MatchReason = matchReason?.Trim();
        AddedOn = clock.Now;
        AddedBy = actor.DisplayName;
        AddedByUserId = actor.UserId;
    }

    /// <summary>
    /// Takes the whole <see cref="Project"/> for the same reason the release notice factories do: the
    /// project is asked whether it is still open, then only its id is kept.
    /// </summary>
    public static Result<ProjectDocument> Attach(
        Project project, ProjectDocumentId id, string fileName, string contentType, long sizeBytes,
        DocumentKind kind, int pageCount, string summary, string extractedText, string entities,
        string ocrProvider, string nerProvider, decimal? matchConfidence, string matchReason,
        Actor actor, IClock clock)
    {
        var canAttach = project.EnsureCanTakeDocuments();
        if (canAttach.IsFailure) return Result<ProjectDocument>.Error(canAttach.Message);

        if (string.IsNullOrWhiteSpace(extractedText))
            return Result<ProjectDocument>.Error("No text could be read from this document.");

        return Result<ProjectDocument>.OK(new ProjectDocument(
            id, project.Id, fileName, contentType, sizeBytes, kind, pageCount, summary, extractedText,
            entities, ocrProvider, nerProvider, matchConfidence, matchReason, actor, clock));
    }
}
