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
    IReadOnlyList<JqlCustomFieldFilterClause> CustomFieldClauses);

public static class JqlLiteParser
{
    private static readonly Regex RxSplitAnd = new(@"\s+AND\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAssigneeCurrent = new(@"^assignee\s*=\s*currentUser\s*\(\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAssigneeEmpty = new(@"^assignee\s*=\s*empty\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxStatus = new(@"^status\s*=\s*""([^""]+)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxText = new(@"^text\s*~\s*""([^""]*)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCfString = new(@"^cf\[([^\]]+)]\s*=\s*""([^""]*)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCfNumber = new(@"^cf\[([^\]]+)]\s*=\s*(-?\d+(?:\.\d+)?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Result<JqlLiteResult> Parse(string? jql, Guid? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(jql))
        {
            return Result.Success(new JqlLiteResult(
                false, false, null,
                false, null, null,
                false, null,
                Array.Empty<JqlCustomFieldFilterClause>()));
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

            return Result.Failure<JqlLiteResult>(ErrorType.Validation, "issue.search.jql.unrecognized_clause");
        }

        return Result.Success(new JqlLiteResult(
            hasAssignee, assigneeUnassigned, assigneeId,
            hasStatus, statusId, statusName,
            hasText, textContains,
            cfClauses));
    }
}
