using BB.Common;
using Issue.Domain.Events;

namespace Issue.Domain;

public sealed class Issue : AggregateRoot, ISoftDeletable
{
    public const int SummaryMaxLength = 500;

    public Guid ProjectId { get; private set; }
    public string Key { get; private set; } = string.Empty;       // PRJ-123
    public int Number { get; private set; }
    public Guid IssueTypeId { get; private set; }

    public Guid WorkflowId { get; private set; }
    public Guid CurrentStatusId { get; private set; }

    public string Summary { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; } = Priority.Medium;

    public Guid ReporterId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? ParentIssueId { get; private set; }

    public List<string> Labels { get; private set; } = new();
    public DateTimeOffset? DueDate { get; private set; }
    public decimal? StoryPoints { get; private set; }

    // Time tracking (đơn vị: minutes)
    public int? OriginalEstimateMinutes { get; private set; }
    public int? RemainingEstimateMinutes { get; private set; }
    public int? TimeSpentMinutes { get; private set; }

    public bool IsArchived { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<IssueWatcher> _watchers = new();
    public IReadOnlyList<IssueWatcher> Watchers => _watchers;

    private Issue() { }

    public Issue(Guid projectId, string key, int number, Guid issueTypeId, Guid workflowId, Guid initialStatusId,
        string summary, Guid reporterId, string? description = null, Priority priority = Priority.Medium,
        Guid? parentIssueId = null, Guid? assigneeId = null, DateTimeOffset? dueDate = null, decimal? storyPoints = null,
        IReadOnlyCollection<string>? labels = null)
    {
        EnsureSummary(summary);

        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException(IssueErrors.KeyInvalid, IssueErrors.MsgKeyInvalid);

        ProjectId = projectId;
        Key = key;
        Number = number;
        IssueTypeId = issueTypeId;
        WorkflowId = workflowId;
        CurrentStatusId = initialStatusId;
        Summary = summary.Trim();
        Description = description;
        Priority = priority;
        ReporterId = reporterId;
        AssigneeId = assigneeId;
        ParentIssueId = parentIssueId;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        if (labels is not null) Labels.AddRange(labels.Distinct(StringComparer.OrdinalIgnoreCase));

        // Reporter mặc định là watcher
        _watchers.Add(new IssueWatcher(Id, reporterId, DateTimeOffset.UtcNow));

        RaiseDomainEvent(new IssueCreated(Id, projectId, issueTypeId, key, summary, reporterId));
    }

    private static void EnsureSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new DomainException(IssueErrors.SummaryRequired, IssueErrors.MsgSummaryRequired);
        if (summary.Length > SummaryMaxLength)
            throw new DomainException(IssueErrors.SummaryTooLong, IssueErrors.MsgSummaryTooLong);
    }

    // ========== Mutators ==========
    public void UpdateSummary(string summary)
    {
        EnsureSummary(summary);
        if (Summary == summary.Trim()) return;
        var old = Summary;
        Summary = summary.Trim();
        RaiseDomainEvent(new IssueUpdated(Id, Key, nameof(Summary), old, Summary));
    }

    public void UpdateDescription(string? description) => Description = description;

    public void ChangePriority(Priority priority)
    {
        if (Priority == priority) return;
        var old = Priority;
        Priority = priority;
        RaiseDomainEvent(new IssueUpdated(Id, Key, nameof(Priority), old, priority));
    }

    public void Assign(Guid? assigneeId)
    {
        if (AssigneeId == assigneeId) return;
        var old = AssigneeId;
        AssigneeId = assigneeId;
        RaiseDomainEvent(new IssueAssigneeChanged(Id, old, assigneeId));

        // Auto-watch assignee
        if (assigneeId.HasValue && !IsWatching(assigneeId.Value))
        {
            _watchers.Add(new IssueWatcher(Id, assigneeId.Value, DateTimeOffset.UtcNow));
            RaiseDomainEvent(new IssueWatcherAdded(Id, assigneeId.Value));
        }
    }

    public void SetParent(Guid? parentIssueId)
    {
        if (parentIssueId == Id)
            throw new DomainException(IssueErrors.ParentSelf, IssueErrors.MsgParentSelf);
        if (ParentIssueId == parentIssueId) return;
        var old = ParentIssueId;
        ParentIssueId = parentIssueId;
        RaiseDomainEvent(new IssueParentChanged(Id, old, parentIssueId));
    }

    public void SetDueDate(DateTimeOffset? dueDate) => DueDate = dueDate;
    public void SetStoryPoints(decimal? storyPoints) => StoryPoints = storyPoints;

    public void SetLabels(IReadOnlyCollection<string>? labels)
    {
        Labels.Clear();
        if (labels is null) return;
        Labels.AddRange(labels
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public void SetTimeTracking(int? originalMinutes, int? remainingMinutes, int? spentMinutes)
    {
        if (originalMinutes is < 0 || remainingMinutes is < 0 || spentMinutes is < 0)
            throw new DomainException(IssueErrors.EstimateNegative, IssueErrors.MsgEstimateNegative);
        OriginalEstimateMinutes = originalMinutes;
        RemainingEstimateMinutes = remainingMinutes;
        TimeSpentMinutes = spentMinutes;
    }

    public void TransitionTo(Guid newStatusId, Guid transitionId)
    {
        if (CurrentStatusId == newStatusId) return;
        var old = CurrentStatusId;
        CurrentStatusId = newStatusId;
        RaiseDomainEvent(new IssueStatusChanged(Id, old, newStatusId, transitionId));
    }

    // ========== Watchers ==========
    public bool IsWatching(Guid userId) => _watchers.Any(w => w.UserId == userId);

    public IssueWatcher AddWatcher(Guid userId)
    {
        if (IsWatching(userId))
            throw new DomainException(IssueErrors.WatcherDuplicated, IssueErrors.MsgWatcherDup);
        var w = new IssueWatcher(Id, userId, DateTimeOffset.UtcNow);
        _watchers.Add(w);
        RaiseDomainEvent(new IssueWatcherAdded(Id, userId));
        return w;
    }

    public void RemoveWatcher(Guid userId)
    {
        var w = _watchers.FirstOrDefault(x => x.UserId == userId)
                ?? throw new DomainException(IssueErrors.WatcherNotFound, IssueErrors.MsgWatcherNotFound);
        _watchers.Remove(w);
        RaiseDomainEvent(new IssueWatcherRemoved(Id, userId));
    }

    public void Archive()
    {
        if (IsArchived)
            throw new DomainException(IssueErrors.AlreadyArchived, IssueErrors.MsgAlreadyArchived);
        IsArchived = true;
        RaiseDomainEvent(new IssueArchived(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived)
            throw new DomainException(IssueErrors.NotArchived, IssueErrors.MsgNotArchived);
        IsArchived = false;
    }
}
