using BB.Common;

namespace Sprint.Domain;

public sealed class SprintIssue : BaseEntity
{
    public Guid SprintId { get; private set; }
    public Guid IssueId { get; private set; }
    public int Rank { get; private set; }

    private SprintIssue() { }

    public SprintIssue(Guid sprintId, Guid issueId, int rank)
    {
        SprintId = sprintId;
        IssueId = issueId;
        Rank = rank;
    }

    public void SetRank(int rank) => Rank = rank;
}
