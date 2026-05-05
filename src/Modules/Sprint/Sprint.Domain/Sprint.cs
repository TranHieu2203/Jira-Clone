using BB.Common;

namespace Sprint.Domain;

public sealed class Sprint : AuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public SprintStatus Status { get; private set; }

    private readonly List<SprintIssue> _items = new();
    public IReadOnlyList<SprintIssue> Items => _items;

    private Sprint() { }

    public Sprint(Guid projectId, string name, DateTimeOffset startDate, DateTimeOffset endDate, string? goal)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("NAME_REQUIRED", SprintErrors.MsgNameRequired);
        if (endDate <= startDate)
            throw new DomainException("INVALID_DATES", SprintErrors.MsgInvalidDates);

        ProjectId = projectId;
        Name = name.Trim();
        Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = SprintStatus.Planned;
    }

    public void Rename(string name, string? goal, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("NAME_REQUIRED", SprintErrors.MsgNameRequired);
        if (endDate <= startDate)
            throw new DomainException("INVALID_DATES", SprintErrors.MsgInvalidDates);
        Name = name.Trim();
        Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
        StartDate = startDate;
        EndDate = endDate;
    }

    public SprintIssue AddIssue(Guid issueId)
    {
        EnsureMutable();
        if (_items.Any(x => x.IssueId == issueId))
            throw new DomainException("DUP", SprintErrors.MsgIssueDuplicate);
        int rank = _items.Count == 0 ? 0 : _items.Max(x => x.Rank) + 1;
        var row = new SprintIssue(Id, issueId, rank);
        _items.Add(row);
        return row;
    }

    public void RemoveIssue(Guid issueId)
    {
        EnsureMutable();
        var row = _items.FirstOrDefault(x => x.IssueId == issueId)
                  ?? throw new DomainException("MISSING", SprintErrors.MsgIssueNotInSprint);
        _items.Remove(row);
    }

    public void ReorderIssues(IReadOnlyList<Guid> orderedIssueIds)
    {
        EnsureMutable();
        if (orderedIssueIds.Count != _items.Count)
            throw new DomainException("COUNT", SprintErrors.MsgReorderInvalid);
        var set = orderedIssueIds.ToHashSet();
        if (set.Count != orderedIssueIds.Count || !_items.All(x => set.Contains(x.IssueId)))
            throw new DomainException("MISMATCH", SprintErrors.MsgReorderInvalid);
        for (int i = 0; i < orderedIssueIds.Count; i++)
        {
            var row = _items.First(x => x.IssueId == orderedIssueIds[i]);
            row.SetRank(i);
        }
    }

    public void Start()
    {
        if (Status != SprintStatus.Planned)
            throw new DomainException("STATE", SprintErrors.MsgWrongStateStart);
        if (_items.Count == 0)
            throw new DomainException("EMPTY", SprintErrors.MsgEmptySprint);
        Status = SprintStatus.Active;
    }

    public void Complete()
    {
        if (Status != SprintStatus.Active)
            throw new DomainException("STATE", SprintErrors.MsgWrongStateComplete);
        Status = SprintStatus.Completed;
    }

    private void EnsureMutable()
    {
        if (Status == SprintStatus.Completed)
            throw new DomainException("DONE", SprintErrors.MsgCompletedImmutable);
    }

    /// <summary>Xóa link issue khỏi sprint sau khi complete — issue quay lại backlog.</summary>
    public void ClearIssueLinks()
    {
        _items.Clear();
    }
}
