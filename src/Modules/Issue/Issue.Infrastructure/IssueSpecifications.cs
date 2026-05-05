using BB.Persistence.Specification;
using Issue.Application.Repositories;
using Issue.Domain;

namespace Issue.Infrastructure;

internal static class IssueSpecifications
{
    internal static ISpecification<Domain.Issue> From(IssueSearchCriteria criteria)
    {
        ISpecification<Domain.Issue>? spec = null;

        void Add(ISpecification<Domain.Issue> next) =>
            spec = spec is null ? next : spec.And(next);

        if (criteria.ProjectId.HasValue)
            Add(new Specification<Domain.Issue>(i => i.ProjectId == criteria.ProjectId.Value));
        if (criteria.IssueTypeId.HasValue)
            Add(new Specification<Domain.Issue>(i => i.IssueTypeId == criteria.IssueTypeId.Value));
        if (criteria.AssigneeId.HasValue)
            Add(new Specification<Domain.Issue>(i => i.AssigneeId == criteria.AssigneeId.Value));
        if (criteria.ReporterId.HasValue)
            Add(new Specification<Domain.Issue>(i => i.ReporterId == criteria.ReporterId.Value));
        if (criteria.CurrentStatusIds is { Count: > 0 })
            Add(new Specification<Domain.Issue>(i => criteria.CurrentStatusIds.Contains(i.CurrentStatusId)));
        else if (criteria.CurrentStatusId.HasValue)
            Add(new Specification<Domain.Issue>(i => i.CurrentStatusId == criteria.CurrentStatusId.Value));
        if (criteria.Priority.HasValue)
            Add(new Specification<Domain.Issue>(i => (int)i.Priority == criteria.Priority.Value));
        if (criteria.IncludeArchived != true)
            Add(new Specification<Domain.Issue>(i => !i.IsArchived));

        if (criteria.AssigneeUnassignedOnly)
            Add(new Specification<Domain.Issue>(i => i.AssigneeId == null));

        if (criteria.RestrictToIssueIds is not null)
        {
            IReadOnlySet<Guid> ids = criteria.RestrictToIssueIds;
            Add(new Specification<Domain.Issue>(i => ids.Contains(i.Id)));
        }

        if (criteria.ExcludeIssueIds is { Count: > 0 })
        {
            IReadOnlySet<Guid> ex = criteria.ExcludeIssueIds;
            Add(new Specification<Domain.Issue>(i => !ex.Contains(i.Id)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TextSearch))
        {
            string s = criteria.TextSearch.Trim().ToLower();
            Add(new Specification<Domain.Issue>(i =>
                i.Summary.ToLower().Contains(s) || i.Key.ToLower().Contains(s)));
        }

        return spec ?? new Specification<Domain.Issue>(_ => true);
    }
}
