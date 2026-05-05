using System.Globalization;
using System.Text.RegularExpressions;
using BB.Common;

namespace Issue.Application;

public sealed record JqlCustomFieldFilterClause(
    string FieldKey,
    string? StringEquals,
    decimal? NumberEquals,
    DateTimeOffset? DateEquals);

public sealed record JqlLiteResult(
    bool HasAssigneeClause,
    bool AssigneeUnassignedOnly,
    Guid? AssigneeId,
    bool HasStatusClause,
    Guid? StatusId,
    string? StatusName,
    bool HasTextClause,
    string? TextContains,
    IReadOnlyList<JqlCustomFieldFilterClause> CustomFieldClauses,
    int? Priority,
    string? IssueTypeKey,
    IReadOnlyList<string> Labels);

public static class JqlLiteParser
{
    private static readonly Regex RxSplitAnd = new(@"\s+AND\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAssigneeCurrent = new(@"^assignee\s*=\s*currentUser\s*\(\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAssigneeEmpty = new(@"^assignee\s*=\s*empty\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxStatus = new(@"^status\s*=\s*""([^""]+)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxText = new(@"^text\s*~\s*""([^""]*)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCfString = new(@"^cf\[([^\]]+)]\s*=\s*""([^""]*)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCfNumber = new(@"^cf\[([^\]]+)]\s*=\s*(-?\d+(?:\.\d+)?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Priority: hỗ trợ tên (Lowest/Low/Medium/High/Highest) hoặc số 1-5.
    private static readonly Regex RxPriorityName = new(@"^priority\s*=\s*""?(Lowest|Low|Medium|High|Highest)""?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxPriorityNumber = new(@"^priority\s*=\s*([1-5])\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Type: theo issue type key (ví dụ "BUG", "STORY"). Resolve sang IssueTypeId ở Service layer (cần ProjectId).
    private static readonly Regex RxType = new(@"^type\s*=\s*""([A-Za-z0-9_\-]+)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Label: equality 1 label hoặc IN list (label in ("a","b","c")).
    private static readonly Regex RxLabelEquals = new(@"^label\s*=\s*""([^""]+)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxLabelIn = new(@"^label\s+in\s*\(\s*(.*)\s*\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxLabelInItem = new(@"""([^""]+)""", RegexOptions.Compiled);

    public static Result<JqlLiteResult> Parse(string? jql, Guid? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(jql))
        {
            return Result.Success(new JqlLiteResult(
                false, false, null,
                false, null, null,
                false, null,
                Array.Empty<JqlCustomFieldFilterClause>(),
                null, null, Array.Empty<string>()));
        }

        string[] parts = RxSplitAnd.Split(jql.Trim());
        bool hasAssignee = false;
        bool assigneeUnassigned = false;
        Guid? assigneeId = null;
        bool hasStatus = false;
        Guid? statusId = null;
        string? statusName = null;
        bool hasText = false;
        string? textContains = null;
        List<JqlCustomFieldFilterClause> cfClauses = new();
        HashSet<string> cfKeysSeen = new(StringComparer.OrdinalIgnoreCase);
        int? priority = null;
        string? issueTypeKey = null;
        List<string> labels = new();

        foreach (string part in parts)
        {
            string clause = part.Trim();
            if (clause.Length == 0)
                continue;

            if (RxAssigneeCurrent.IsMatch(clause))
            {
                if (hasAssignee)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_assignee");

                if (currentUserId is null)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.current_user_required");

                hasAssignee = true;
                assigneeId = currentUserId;
                assigneeUnassigned = false;
                continue;
            }

            if (RxAssigneeEmpty.IsMatch(clause))
            {
                if (hasAssignee)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_assignee");

                hasAssignee = true;
                assigneeUnassigned = true;
                assigneeId = null;
                continue;
            }

            Match mStatus = RxStatus.Match(clause);
            if (mStatus.Success)
            {
                if (hasStatus)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_status");

                string raw = mStatus.Groups[1].Value.Trim();
                if (Guid.TryParse(raw, out Guid sid))
                {
                    statusId = sid;
                    statusName = null;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.status_empty");

                    statusId = null;
                    statusName = raw;
                }

                hasStatus = true;
                continue;
            }

            Match mText = RxText.Match(clause);
            if (mText.Success)
            {
                if (hasText)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_text");

                hasText = true;
                textContains = mText.Groups[1].Value;
                continue;
            }

            Match mCfStr = RxCfString.Match(clause);
            if (mCfStr.Success)
            {
                string key = mCfStr.Groups[1].Value.Trim();
                if (key.Length == 0)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.cf_key_invalid");

                if (!cfKeysSeen.Add(key))
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_cf");

                cfClauses.Add(new JqlCustomFieldFilterClause(key, mCfStr.Groups[2].Value, null, null));
                continue;
            }

            Match mCfNum = RxCfNumber.Match(clause);
            if (mCfNum.Success)
            {
                string key = mCfNum.Groups[1].Value.Trim();
                if (key.Length == 0)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.cf_key_invalid");

                if (!cfKeysSeen.Add(key))
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_cf");

                if (!decimal.TryParse(mCfNum.Groups[2].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal num))
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.cf_number_invalid");

                cfClauses.Add(new JqlCustomFieldFilterClause(key, null, num, null));
                continue;
            }

            // Priority — tên hoặc số 1-5.
            Match mPriName = RxPriorityName.Match(clause);
            if (mPriName.Success)
            {
                if (priority.HasValue)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_priority");
                priority = MapPriorityName(mPriName.Groups[1].Value);
                continue;
            }
            Match mPriNum = RxPriorityNumber.Match(clause);
            if (mPriNum.Success)
            {
                if (priority.HasValue)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_priority");
                priority = int.Parse(mPriNum.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }

            // Type — theo issue type key (resolve sang Id ở Service layer cùng ProjectId).
            Match mType = RxType.Match(clause);
            if (mType.Success)
            {
                if (issueTypeKey is not null)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.duplicate_type");
                issueTypeKey = mType.Groups[1].Value.Trim();
                continue;
            }

            // Label — equality 1 label.
            Match mLabel = RxLabelEquals.Match(clause);
            if (mLabel.Success)
            {
                string lbl = mLabel.Groups[1].Value.Trim();
                if (lbl.Length > 0 && !labels.Contains(lbl, StringComparer.OrdinalIgnoreCase))
                    labels.Add(lbl);
                continue;
            }

            // Label IN ("a", "b") — tất cả phải có (AND across labels).
            Match mLabelIn = RxLabelIn.Match(clause);
            if (mLabelIn.Success)
            {
                string inner = mLabelIn.Groups[1].Value;
                MatchCollection items = RxLabelInItem.Matches(inner);
                if (items.Count == 0)
                    return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.label_in_empty");
                foreach (Match it in items)
                {
                    string lbl = it.Groups[1].Value.Trim();
                    if (lbl.Length > 0 && !labels.Contains(lbl, StringComparer.OrdinalIgnoreCase))
                        labels.Add(lbl);
                }
                continue;
            }

            return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.unrecognized_clause");
        }

        return Result.Success(new JqlLiteResult(
            hasAssignee, assigneeUnassigned, assigneeId,
            hasStatus, statusId, statusName,
            hasText, textContains,
            cfClauses,
            priority, issueTypeKey, labels));
    }

    private static int MapPriorityName(string name) => name.ToLowerInvariant() switch
    {
        "lowest" => 1,
        "low" => 2,
        "medium" => 3,
        "high" => 4,
        "highest" => 5,
        _ => 3
    };
}
