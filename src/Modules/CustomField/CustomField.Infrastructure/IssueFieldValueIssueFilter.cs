using CustomField.Application;
using CustomField.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomField.Infrastructure;

public sealed class IssueFieldValueIssueFilter : IIssueFieldValueIssueFilter
{
    private readonly CustomFieldDbContext _db;

    public IssueFieldValueIssueFilter(CustomFieldDbContext db) => _db = db;

    public async Task<IReadOnlySet<Guid>?> MatchingIssueIdsAsync(
        IReadOnlyList<IssueFieldIndexedCriterion> criteria,
        CancellationToken ct = default)
    {
        if (criteria.Count == 0)
            return null;

        HashSet<Guid>? intersection = null;
        bool anyApplied = false;

        foreach (IssueFieldIndexedCriterion c in criteria)
        {
            IQueryable<IssueFieldValue> q = _db.IssueFieldValues.AsNoTracking()
                .Where(v => v.CustomFieldId == c.CustomFieldId);

            bool hasPredicate = false;
            if (c.IndexedStringEquals is not null)
            {
                q = q.Where(v => v.IndexedString == c.IndexedStringEquals);
                hasPredicate = true;
            }
            else if (c.IndexedNumberEquals is not null)
            {
                decimal n = c.IndexedNumberEquals.Value;
                q = q.Where(v => v.IndexedNumber == n);
                hasPredicate = true;
            }
            else if (c.IndexedDateEquals is not null)
            {
                DateTimeOffset d = c.IndexedDateEquals.Value;
                q = q.Where(v => v.IndexedDate == d);
                hasPredicate = true;
            }

            if (!hasPredicate)
                continue;

            anyApplied = true;
            List<Guid> ids = await q.Select(v => v.IssueId).Distinct().ToListAsync(ct);
            HashSet<Guid> set = ids.ToHashSet();
            intersection = intersection is null ? set : intersection.Intersect(set).ToHashSet();
            if (intersection.Count == 0)
                return intersection;
        }

        if (!anyApplied)
            return null;

        return intersection ?? new HashSet<Guid>();
    }
}
