using BB.Common;

namespace Sprint.Domain;

/// <summary>Ảnh chụp phạm vi burndown khi sprint chuyển Active (story points commit).</summary>
public sealed class SprintCommitLine : AuditableEntity
{
    public Guid SprintId { get; private set; }
    public Guid IssueId { get; private set; }
    /// <summary>Điểm tính vào burndown; 0 nếu issue đã Done lúc start hoặc không có story points.</summary>
    public decimal BurndownPoints { get; private set; }

    private SprintCommitLine() { }

    public SprintCommitLine(Guid sprintId, Guid issueId, decimal burndownPoints)
    {
        SprintId = sprintId;
        IssueId = issueId;
        BurndownPoints = burndownPoints < 0 ? 0 : burndownPoints;
    }
}
